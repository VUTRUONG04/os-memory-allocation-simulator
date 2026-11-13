using System.Diagnostics;
using OSMemoryAllocationSimulator.Algorithms;
using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Services;

public sealed record AlgorithmEvaluation(
    string Algorithm,
    SimulationResult Result,
    int TotalAllocationRequests,
    int SuccessfulAllocations,
    int FailedAllocations,
    double? SuccessRate, 
    IReadOnlyList<double> RuntimesMs, 
    double AverageRuntimeMs);

public sealed record EvaluationResult(
    SimulationConfig Config,
    int OperationCount,
    IReadOnlyList<AlgorithmEvaluation> Algorithms);

public interface IEvaluationService
{
    EvaluationResult Run(
        SimulationConfig config,
        IReadOnlyList<WorkloadOperation> workload,
        CancellationToken cancellationToken = default);
}

public sealed class EvaluationService : IEvaluationService
{
    public const int RuntimeRuns = 5;

    private readonly IReadOnlyList<IFitAlgorithm> _algorithms;

    public EvaluationService()
        : this(new IFitAlgorithm[]
        {
            new FirstFitAlgorithm(),
            new BestFitAlgorithm(),
            new WorstFitAlgorithm()
        })
    {
    }

    public EvaluationService(IReadOnlyList<IFitAlgorithm> algorithms)
    {
        _algorithms = algorithms ?? throw new ArgumentNullException(nameof(algorithms));
    }

    public EvaluationResult Run(
        SimulationConfig config,
        IReadOnlyList<WorkloadOperation> workload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(workload);

        var ordered = workload.OrderBy(o => o.Sequence).ToList();
        var evaluations = new List<AlgorithmEvaluation>();

        foreach (var algorithm in _algorithms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            evaluations.Add(EvaluateOne(config, ordered, algorithm, cancellationToken));
        }

        return new EvaluationResult(config, ordered.Count, evaluations);
    }

    private static AlgorithmEvaluation EvaluateOne(
        SimulationConfig config,
        IReadOnlyList<WorkloadOperation> ordered,
        IFitAlgorithm algorithm,
        CancellationToken ct)
    {
        SimulationResult? result = null;
        var runtimes = new List<double>(RuntimeRuns);

        for (int run = 0; run < RuntimeRuns; run++)
        {
            ct.ThrowIfCancellationRequested();

            var manager = new MemoryManager(config);
            var trace = new List<TraceEntry>(ordered.Count);

            var sw = Stopwatch.StartNew();
            foreach (var op in ordered)
            {
                trace.Add(manager.ExecuteOperation(op, algorithm));
            }
            sw.Stop();

            runtimes.Add(sw.Elapsed.TotalMilliseconds);

            if (run == 0)
            {
                result = new SimulationResult(
                    config,
                    trace,
                    manager.Regions.ToList(),
                    manager.GetMetrics());
            }
        }

        var allocTrace = result!.Trace.Where(t => t.Operation == MemoryOperationType.Allocate).ToList();
        int total = allocTrace.Count;
        int success = allocTrace.Count(t => t.Status == OperationStatus.Success);
        int failed = total - success;
        double? rate = total == 0 ? null : (double)success / total;

        return new AlgorithmEvaluation(
            algorithm.Name,
            result,
            total,
            success,
            failed,
            rate,
            runtimes,
            runtimes.Average());
    }
}
