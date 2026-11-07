
using OSMemoryAllocationSimulator.Algorithms;
using OSMemoryAllocationSimulator.Models;
using OSMemoryAllocationSimulator.Services;

namespace OSMemoryAllocationSimulator.Tests;

public class SimulationServiceTests
{
    private static SimulationService CreateService()
    {
        var config = new SimulationConfig(
            MemorySize: 4096,
            AllocationUnit: 64
        );

        var workload = new List<WorkloadOperation>
        {
            new(1, MemoryOperationType.Allocate, "P1", 100),
            new(2, MemoryOperationType.Allocate, "P2", 180),
            new(3, MemoryOperationType.Free, "P1", null),
            new(4, MemoryOperationType.Free, "P2", null)
        };

        return new SimulationService(
            config,
            new FirstFitMock(),
            workload
        );
    }

    private sealed class FirstFitMock : IFitAlgorithm
    {
        public string Name => "FirstFit Mock";

        public MemoryRegion? FindFit(
            IReadOnlyList<MemoryRegion> freeRegions,
            int requiredSize)
        {
            return freeRegions.FirstOrDefault(
                region => region.Size >= requiredSize
            );
        }
    }

    [Fact]
    public void Step_ShouldExecuteOnlyOneOperation()
    {
        var service = CreateService();

        var entry = service.Step();

        Assert.NotNull(entry);
        Assert.Equal(1, service.CompletedOperationCount);
        Assert.Single(service.Trace);
        Assert.Equal(128, entry.AllocatedSize);
    }

    [Fact]
    public void RunAll_ShouldCompleteAllOperations()
    {
        var service = CreateService();

        var result = service.RunAll();

        Assert.NotNull(result);
        Assert.Equal(4, result.Trace.Count);
        Assert.Equal(4, service.CompletedOperationCount);
        Assert.True(service.IsCompleted);

        Assert.Equal(4096, result.FinalMetrics.FreeMemory);
        Assert.Equal(0, result.FinalMetrics.UsedMemory);
    }

    [Fact]
    public void Reset_ShouldRestoreInitialMemory()
    {
        var service = CreateService();

        service.Step();
        service.Step();

        service.Reset();

        Assert.Empty(service.Trace);
        Assert.Equal(0, service.CompletedOperationCount);
        Assert.False(service.IsCompleted);
        Assert.Null(service.Result);

        Assert.Single(service.MemoryMap);
        Assert.Equal(4096, service.MemoryMap[0].Size);
        Assert.True(service.MemoryMap[0].IsFree);
    }

    [Fact]
    public void Result_ShouldBeAvailableOnlyAfterCompletion()
    {
        var service = CreateService();

        Assert.Null(service.Result);

        service.RunAll();

        Assert.NotNull(service.Result);
        Assert.Equal(4, service.Result.Trace.Count);
    }
}
