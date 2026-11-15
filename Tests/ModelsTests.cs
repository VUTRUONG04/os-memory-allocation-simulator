using OSMemoryAllocationSimulator.Models;
using Xunit;

namespace OSMemoryAllocationSimulator.Tests;

public class ModelsTests
{
    [Fact]
    public void SimulationConfig_ShouldHoldValidValues()
    {
        var config = new SimulationConfig(1024, 64);
        Assert.Equal(1024, config.MemorySize);
        Assert.Equal(64, config.AllocationUnit);
    }

    [Fact]
    public void WorkloadOperation_Allocate_ShouldStoreRequestedSize()
    {
        var op = new WorkloadOperation(1, MemoryOperationType.Allocate, "P1", 100);
        Assert.Equal(1, op.Sequence);
        Assert.Equal(MemoryOperationType.Allocate, op.Operation);
        Assert.Equal("P1", op.ProcessId);
        Assert.Equal(100, op.RequestedSize);
    }

    [Fact]
    public void WorkloadOperation_Free_CanHaveNullRequestedSize()
    {
        var op = new WorkloadOperation(2, MemoryOperationType.Free, "P1", null);
        Assert.Equal(2, op.Sequence);
        Assert.Equal(MemoryOperationType.Free, op.Operation);
        Assert.Equal("P1", op.ProcessId);
        Assert.Null(op.RequestedSize);
    }

    [Fact]
    public void MemoryRegion_InitialState_ShouldBeFree()
    {
        var region = new MemoryRegion(0, 1024, true);
        Assert.Equal(0, region.StartAddress);
        Assert.Equal(1024, region.Size);
        Assert.True(region.IsFree);
        Assert.Null(region.ProcessId);
    }

    [Fact]
    public void ComparisonRecord_ShouldCalculateAverageRuntimeAutomatically()
    {
        var record = new ComparisonRecord(
            workloadName: "Workload1",
            algorithm: "FirstFit",
            operationCount: 10,
            memorySize: 1024,
            allocationUnit: 64,
            totalAllocationRequests: 8,
            successfulAllocations: 7,
            failedAllocations: 1,
            successRate: 0.875,
            usedMemory: 512,
            freeMemory: 512,
            freeBlockCount: 2,
            largestFreeBlock: 384,
            totalInternalFragmentation: 20,
            runtimeRun1Ms: 10.0,
            runtimeRun2Ms: 12.0,
            runtimeRun3Ms: 14.0,
            runtimeRun4Ms: 16.0,
            runtimeRun5Ms: 18.0);

        Assert.Equal("Workload1", record.WorkloadName);
        Assert.Equal("FirstFit", record.Algorithm);
        Assert.Equal(14.0, record.AverageRuntimeMs); // (10+12+14+16+18)/5 = 14.0
    }
}
