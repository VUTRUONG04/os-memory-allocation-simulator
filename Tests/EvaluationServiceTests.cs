using OSMemoryAllocationSimulator.Models;
using OSMemoryAllocationSimulator.Services;
using Xunit;

namespace OSMemoryAllocationSimulator.Tests;

public class EvaluationServiceTests
{
    private static readonly SimulationConfig Config = new(100, 10);

    private static List<WorkloadOperation> Workload() => new()
    {
        new(1, MemoryOperationType.Allocate, "P1", 30),
        new(2, MemoryOperationType.Allocate, "P2", 50),
        new(3, MemoryOperationType.Allocate, "P3", 40), // thất bại: chỉ còn 20 trống
        new(4, MemoryOperationType.Free, "P1", null)
    };

    [Fact]
    public void Run_ReturnsThreeAlgorithms_InOrder()
    {
        var result = new EvaluationService().Run(Config, Workload());

        Assert.Equal(new[] { "FirstFit", "BestFit", "WorstFit" },
            result.Algorithms.Select(a => a.Algorithm));
        Assert.Equal(4, result.OperationCount);
    }

    [Fact]
    public void Run_MeasuresFiveRuntimes_AndAverageIsMean()
    {
        var result = new EvaluationService().Run(Config, Workload());

        foreach (var a in result.Algorithms)
        {
            Assert.Equal(5, a.RuntimesMs.Count);
            Assert.Equal(a.RuntimesMs.Average(), a.AverageRuntimeMs, 9);
        }
    }

    [Fact]
    public void Run_ComputesSuccessCountsAndRate()
    {
        var result = new EvaluationService().Run(Config, Workload());

        foreach (var a in result.Algorithms)
        {
            Assert.Equal(3, a.TotalAllocationRequests);
            Assert.Equal(2, a.SuccessfulAllocations);
            Assert.Equal(1, a.FailedAllocations);
            Assert.Equal(2.0 / 3.0, a.SuccessRate!.Value, 9);
        }
    }

    [Fact]
    public void Run_NoAllocateOperations_SuccessRateIsNull()
    {
        var workload = new List<WorkloadOperation>
        {
            new(1, MemoryOperationType.Free, "P1", null)
        };

        var result = new EvaluationService().Run(Config, workload);

        foreach (var a in result.Algorithms)
        {
            Assert.Equal(0, a.TotalAllocationRequests);
            Assert.Null(a.SuccessRate);
        }
    }

    [Fact]
    public void Run_EachAlgorithmStartsFromFreshMemory()
    {
        var result = new EvaluationService().Run(Config, Workload());

        foreach (var a in result.Algorithms)
        {
            Assert.Equal(4, a.Result.Trace.Count);
            Assert.Equal(OperationStatus.Success, a.Result.Trace[0].Status);
            Assert.Equal(100, a.Result.FinalMetrics.UsedMemory + a.Result.FinalMetrics.FreeMemory);
        }
    }

    [Fact]
    public void Run_UnsortedWorkload_IsProcessedBySequence()
    {
        var shuffled = Workload().OrderByDescending(o => o.Sequence).ToList();

        var result = new EvaluationService().Run(Config, shuffled);

        Assert.Equal(new[] { 1, 2, 3, 4 },
            result.Algorithms[0].Result.Trace.Select(t => t.Sequence));
    }

    [Fact]
    public void Run_CancelledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => new EvaluationService().Run(Config, Workload(), cts.Token));
    }
}

