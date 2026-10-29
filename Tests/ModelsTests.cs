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
}
