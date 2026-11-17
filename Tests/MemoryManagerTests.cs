using OSMemoryAllocationSimulator.Algorithms;
using OSMemoryAllocationSimulator.Models;
using OSMemoryAllocationSimulator.Services;
using Xunit;

namespace OSMemoryAllocationSimulator.Tests;

public class MemoryManagerTests
{
    // Mock Fit Algorithm đơn giản để test luồng
    private sealed class SimpleFirstFitMock : IFitAlgorithm
    {
        public string Name => "FirstFitMock";
        public MemoryRegion? FindFit(IReadOnlyList<MemoryRegion> freeRegions, int requiredSize)
        {
            return freeRegions.FirstOrDefault(r => r.Size >= requiredSize);
        }
    }

    [Fact]
    public void Flow_AllocateThenFree_ShouldWorkCorrectly()
    {
        // 1. Cấu hình
        var config = new SimulationConfig(1000, 64);
        var manager = new MemoryManager(config);
        var algo = new SimpleFirstFitMock();

        // Ban đầu toàn bộ là 1000 KB free
        var initialMetrics = manager.GetMetrics();
        Assert.Equal(1000, initialMetrics.FreeMemory);
        Assert.Equal(0, initialMetrics.UsedMemory);

        // 2. Thao tác 1: Cấp phát 100 KB cho P1 -> làm tròn lên ceil(100/64)*64 = 128 KB
        var op1 = new WorkloadOperation(1, MemoryOperationType.Allocate, "P1", 100);
        var trace1 = manager.ExecuteOperation(op1, algo);

        Assert.Equal(OperationStatus.Success, trace1.Status);
        Assert.Equal("SUCCESS", trace1.Reason);
        Assert.Equal(128, trace1.AllocatedSize);
        Assert.Equal(1000 - 128, trace1.TotalFreeMemory);
        Assert.Equal(128 - 100, trace1.InternalFragmentation); // 28 KB

        // 3. Thao tác 2: Thu hồi P1
        var op2 = new WorkloadOperation(2, MemoryOperationType.Free, "P1", null);
        var trace2 = manager.ExecuteOperation(op2, algo);

        Assert.Equal(OperationStatus.Success, trace2.Status);
        Assert.Equal(128, trace2.AllocatedSize); // Kích thước vùng thu hồi trước khi gộp
        Assert.Equal(1000, trace2.TotalFreeMemory); // Đã gộp lại đủ 1000 KB
        Assert.Equal(0, trace2.InternalFragmentation);
    }

    [Fact]
    public void Flow_DuplicateActivePid_ShouldFailWithoutChangingMemory()
    {
        var config = new SimulationConfig(1000, 64);
        var manager = new MemoryManager(config);
        var algo = new SimpleFirstFitMock();

        var op1 = new WorkloadOperation(1, MemoryOperationType.Allocate, "P1", 100);
        manager.ExecuteOperation(op1, algo);

        // Cố tình ALLOCATE tiếp với PID "P1"
        var op2 = new WorkloadOperation(2, MemoryOperationType.Allocate, "P1", 50);
        var trace2 = manager.ExecuteOperation(op2, algo);

        Assert.Equal(OperationStatus.Failed, trace2.Status);
        Assert.Equal("DUPLICATE_ACTIVE_PID", trace2.Reason);
        Assert.Null(trace2.AllocatedSize);
        Assert.Equal(1000 - 128, trace2.TotalFreeMemory); // Trạng thái bộ nhớ không bị thay đổi
    }
}
