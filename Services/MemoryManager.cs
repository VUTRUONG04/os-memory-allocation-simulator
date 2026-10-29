using OSMemoryAllocationSimulator.Algorithms;
using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Services;

/// <summary>
/// Quản lý trạng thái bộ nhớ và điều phối thực thi các thao tác cấp phát / thu hồi.
/// </summary>
public class MemoryManager
{
    private readonly List<MemoryRegion> _regions;

    public SimulationConfig Config { get; }
    public IReadOnlyList<MemoryRegion> Regions => _regions;

    public MemoryManager(SimulationConfig config)
    {
        if (config.MemorySize <= 0 || config.AllocationUnit <= 0)
            throw new ArgumentException("MemorySize và AllocationUnit phải là số nguyên dương.");

        if (config.MemorySize < config.AllocationUnit)
            throw new ArgumentException("MemorySize phải lớn hơn hoặc bằng AllocationUnit.");

        Config = config;
        _regions = new List<MemoryRegion>
        {
            new MemoryRegion(0, config.MemorySize, true)
        };
    }

    /// <summary>
    /// Xử lý một thao tác (ALLOCATE hoặc FREE) theo thuật toán chỉ định.
    /// Trả về một TraceEntry ghi nhận kết quả và metrics sau thao tác.
    /// Thao tác thất bại không làm thay đổi trạng thái bộ nhớ.
    /// </summary>
    public TraceEntry ExecuteOperation(WorkloadOperation operation, IFitAlgorithm algorithm)
    {
        if (operation.Operation == MemoryOperationType.Allocate)
        {
            return HandleAllocate(operation, algorithm);
        }
        else
        {
            return HandleFree(operation);
        }
    }

    private TraceEntry HandleAllocate(WorkloadOperation op, IFitAlgorithm algorithm)
    {
        int requestedSize = op.RequestedSize ?? 0;
        if (requestedSize <= 0)
        {
            var m = GetMetrics();
            return new TraceEntry(op.Sequence, op.Operation, op.ProcessId, requestedSize, null,
                OperationStatus.Failed, "INVALID_REQUESTED_SIZE", m.FreeMemory, m.LargestFreeBlock, m.TotalInternalFragmentation);
        }

        // Kiểm tra duplicate active PID
        if (_regions.Any(r => !r.IsFree && string.Equals(r.ProcessId, op.ProcessId, StringComparison.Ordinal)))
        {
            var m = GetMetrics();
            return new TraceEntry(op.Sequence, op.Operation, op.ProcessId, requestedSize, null,
                OperationStatus.Failed, "DUPLICATE_ACTIVE_PID", m.FreeMemory, m.LargestFreeBlock, m.TotalInternalFragmentation);
        }

        // Làm tròn kích thước: ceil(R / U) * U
        int unit = Config.AllocationUnit;
        int allocatedSize = (int)Math.Ceiling((double)requestedSize / unit) * unit;

        // Vượt quá tổng bộ nhớ
        if (allocatedSize > Config.MemorySize)
        {
            var m = GetMetrics();
            return new TraceEntry(op.Sequence, op.Operation, op.ProcessId, requestedSize, null,
                OperationStatus.Failed, "REQUEST_EXCEEDS_MEMORY", m.FreeMemory, m.LargestFreeBlock, m.TotalInternalFragmentation);
        }

        // Lấy danh sách vùng trống (đã theo StartAddress tăng dần)
        var freeRegions = _regions.Where(r => r.IsFree).ToList();
        var chosen = algorithm.FindFit(freeRegions, allocatedSize);

        if (chosen == null)
        {
            var m = GetMetrics();
            return new TraceEntry(op.Sequence, op.Operation, op.ProcessId, requestedSize, null,
                OperationStatus.Failed, "INSUFFICIENT_MEMORY", m.FreeMemory, m.LargestFreeBlock, m.TotalInternalFragmentation);
        }

        // Cấp phát: thay thế chosen block
        int index = _regions.IndexOf(chosen);
        if (chosen.Size == allocatedSize)
        {
            _regions[index] = new MemoryRegion(chosen.StartAddress, allocatedSize, false, op.ProcessId, requestedSize);
        }
        else
        {
            // Tách vùng
            var allocatedRegion = new MemoryRegion(chosen.StartAddress, allocatedSize, false, op.ProcessId, requestedSize);
            var remainingFree = new MemoryRegion(chosen.StartAddress + allocatedSize, chosen.Size - allocatedSize, true);
            _regions.RemoveAt(index);
            _regions.Insert(index, remainingFree);
            _regions.Insert(index, allocatedRegion);
        }

        var metricsAfter = GetMetrics();
        return new TraceEntry(op.Sequence, op.Operation, op.ProcessId, requestedSize, allocatedSize,
            OperationStatus.Success, "SUCCESS", metricsAfter.FreeMemory, metricsAfter.LargestFreeBlock, metricsAfter.TotalInternalFragmentation);
    }

    private TraceEntry HandleFree(WorkloadOperation op)
    {
        var targetIndex = _regions.FindIndex(r => !r.IsFree && string.Equals(r.ProcessId, op.ProcessId, StringComparison.Ordinal));
        if (targetIndex < 0)
        {
            var m = GetMetrics();
            return new TraceEntry(op.Sequence, op.Operation, op.ProcessId, op.RequestedSize, null,
                OperationStatus.Failed, "PID_NOT_FOUND", m.FreeMemory, m.LargestFreeBlock, m.TotalInternalFragmentation);
        }

        var target = _regions[targetIndex];
        int reclaimedSize = target.Size;

        // Chuyển sang free
        _regions[targetIndex] = new MemoryRegion(target.StartAddress, target.Size, true);

        // Gộp các vùng free liền kề
        Coalesce();

        var metricsAfter = GetMetrics();
        return new TraceEntry(op.Sequence, op.Operation, op.ProcessId, op.RequestedSize, reclaimedSize,
            OperationStatus.Success, "SUCCESS", metricsAfter.FreeMemory, metricsAfter.LargestFreeBlock, metricsAfter.TotalInternalFragmentation);
    }

    private void Coalesce()
    {
        for (int i = 0; i < _regions.Count - 1;)
        {
            if (_regions[i].IsFree && _regions[i + 1].IsFree)
            {
                var merged = new MemoryRegion(_regions[i].StartAddress, _regions[i].Size + _regions[i + 1].Size, true);
                _regions[i] = merged;
                _regions.RemoveAt(i + 1);
            }
            else
            {
                i++;
            }
        }
    }

    public MemoryMetrics GetMetrics()
    {
        int used = _regions.Where(r => !r.IsFree).Sum(r => r.Size);
        int free = _regions.Where(r => r.IsFree).Sum(r => r.Size);
        int count = _regions.Count(r => r.IsFree);
        int largest = _regions.Where(r => r.IsFree).Select(r => r.Size).DefaultIfEmpty(0).Max();
        int internalFrag = _regions.Where(r => !r.IsFree)
            .Sum(r => r.Size - (r.RequestedSize ?? r.Size));

        return new MemoryMetrics(used, free, count, largest, internalFrag);
    }
}
