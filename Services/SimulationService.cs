
using OSMemoryAllocationSimulator.Algorithms;
using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Services;

public sealed class SimulationService
{
    // ==========================================
    // 1. FIELDS
    // ==========================================

    private readonly SimulationConfig _config;
    private readonly IFitAlgorithm _algorithm;
    private readonly IReadOnlyList<WorkloadOperation> _workload;

    private MemoryManager _memoryManager;
    private int _nextOperationIndex;

    private readonly List<TraceEntry> _trace = new();


    // ==========================================
    // 2. PROPERTIES
    // ==========================================

    public SimulationConfig Config => _config;

    public string AlgorithmName => _algorithm.Name;

    public IReadOnlyList<MemoryRegion> MemoryMap =>
        _memoryManager.Regions;

    public IReadOnlyList<TraceEntry> Trace => _trace;

    public MemoryMetrics CurrentMetrics =>
        _memoryManager.GetMetrics();

    public bool IsCompleted =>
        _nextOperationIndex >= _workload.Count;

    public int CompletedOperationCount =>
        _nextOperationIndex;

    public int TotalOperationCount =>
        _workload.Count;

    public SimulationResult? Result { get; private set; }


    // ==========================================
    // 3. CONSTRUCTOR
    // ==========================================

    public SimulationService(
        SimulationConfig config,
        IFitAlgorithm algorithm,
        IEnumerable<WorkloadOperation> workload)
    {
        // Kiểm tra dữ liệu đầu vào
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(workload);

        // Lưu cấu hình và thuật toán
        _config = config;
        _algorithm = algorithm;

        // Sắp xếp workload theo Sequence tăng dần
        _workload = workload
            .OrderBy(operation => operation.Sequence)
            .ToList();

        // Kiểm tra Sequence có bị trùng hay không
        if (_workload
            .Select(operation => operation.Sequence)
            .Distinct()
            .Count() != _workload.Count)
        {
            throw new ArgumentException(
                "Sequence trong workload phải là duy nhất.",
                nameof(workload));
        }

        // Khởi tạo bộ nhớ ban đầu
        _memoryManager = new MemoryManager(_config);

        // Vị trí thao tác ban đầu
        _nextOperationIndex = 0;
    }


    // ==========================================
    // 4. STEP - THỰC HIỆN TỪNG THAO TÁC
    // ==========================================

    public TraceEntry? Step()
    {
        // Kiểm tra đã hoàn thành chưa
        if (IsCompleted)
        {
            TryComplete();
            return null;
        }

        // Lấy thao tác tiếp theo
        var operation = _workload[_nextOperationIndex];

        // Thực hiện thao tác thông qua MemoryManager
        var entry = _memoryManager.ExecuteOperation(
            operation,
            _algorithm);

        // Lưu lịch sử thao tác
        _trace.Add(entry);

        // Chuyển sang thao tác tiếp theo
        _nextOperationIndex++;

        // Kiểm tra hoàn thành mô phỏng
        TryComplete();

        // Trả về kết quả thao tác vừa thực hiện
        return entry;
    }


    // ==========================================
    // 5. RUN ALL - CHẠY TOÀN BỘ WORKLOAD
    // ==========================================

    public SimulationResult RunAll()
    {
        // Tiếp tục chạy cho đến khi hết workload
        while (!IsCompleted)
        {
            Step();
        }

        // Tổng kết kết quả
        TryComplete();

        return Result!;
    }


    // ==========================================
    // 6. RESET - KHỞI TẠO LẠI MÔ PHỎNG
    // ==========================================

    public void Reset()
    {
        // Tạo lại trạng thái bộ nhớ ban đầu
        _memoryManager = new MemoryManager(_config);

        // Đưa vị trí thao tác về đầu
        _nextOperationIndex = 0;

        // Xóa toàn bộ lịch sử
        _trace.Clear();

        // Xóa kết quả mô phỏng cũ
        Result = null;
    }


    // ==========================================
    // 7. TRY COMPLETE - TỔNG KẾT KẾT QUẢ
    // ==========================================

    private void TryComplete()
    {
        // Chỉ tổng kết khi đã chạy hết workload
        // và chưa có kết quả trước đó
        if (!IsCompleted || Result is not null)
        {
            return;
        }

        // Tạo kết quả mô phỏng
        Result = new SimulationResult(
            _config,
            _trace.ToArray(),
            _memoryManager.Regions.ToArray(),
            _memoryManager.GetMetrics());
    }
}
