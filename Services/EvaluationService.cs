using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OSMemoryAllocationSimulator.Services
{
    public enum WorkloadOperationType { Allocate, Free }

    public sealed class WorkloadOperation
    {
        public WorkloadOperationType Type { get; init; }
        public int Size { get; init; }
        public string? BlockId { get; init; }
    }

    public sealed class EvaluationConfig
    {
        public int[] InitialFreeBlocks { get; init; } = Array.Empty<int>();
        public int RepeatCount { get; init; } = 5;
    }

    public sealed class EvaluationWorkload
    {
        public IReadOnlyList<WorkloadOperation> Operations { get; init; } = Array.Empty<WorkloadOperation>();
    }

    public sealed class AllocationOutcome
    {
        public bool Success { get; init; }
        public int RequestedSize { get; init; }
        public int AllocatedBlockSize { get; init; }
        public int InternalFragmentation => Success ? Math.Max(0, AllocatedBlockSize - RequestedSize) : 0;
    }

    public interface IFitAlgorithm
    {
        string Name { get; }
        int? FindBlock(IReadOnlyList<int> freeBlocks, int requestedSize);
    }

    public sealed class EvaluationResult
    {
        public string AlgorithmName { get; init; } = "";
        public int TotalAllocateRequests { get; init; }
        public int SuccessCount { get; init; }
        public int FailureCount { get; init; }
        public double? SuccessRate { get; init; }
        public double RuntimeRun1Ms { get; init; }
        public double RuntimeRun2Ms { get; init; }
        public double RuntimeRun3Ms { get; init; }
        public double RuntimeRun4Ms { get; init; }
        public double RuntimeRun5Ms { get; init; }
        public double AverageRuntimeMs { get; init; }
        public long TotalInternalFragmentation { get; init; }
        public int LargestFreeBlock { get; init; }
        public IReadOnlyList<double> RuntimeRunsMs { get; init; } = Array.Empty<double>();
    }

    public sealed class EvaluationService
    {
        private readonly IReadOnlyList<IFitAlgorithm> _algorithms;

        public EvaluationService(IEnumerable<IFitAlgorithm>? algorithms = null)
        {
            _algorithms = (algorithms ?? new IFitAlgorithm[]
            {
                new MockFitAlgorithm("FirstFit"),
                new MockFitAlgorithm("BestFit"),
                new MockFitAlgorithm("WorstFit")
            }).ToArray();

            if (_algorithms.Count != 3)
                throw new ArgumentException("Evaluation requires exactly three algorithms.", nameof(algorithms));
        }

        public Task<IReadOnlyList<EvaluationResult>> EvaluateAsync(
            EvaluationConfig config,
            EvaluationWorkload workload,
            CancellationToken cancellationToken = default)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (workload == null) throw new ArgumentNullException(nameof(workload));
            if (config.InitialFreeBlocks.Any(x => x < 0))
                throw new ArgumentException("Block sizes cannot be negative.", nameof(config));
            if (workload.Operations.Any(x => x.Type == WorkloadOperationType.Allocate && x.Size <= 0))
                throw new ArgumentException("ALLOCATE sizes must be positive.", nameof(workload));
            if (config.RepeatCount != 5)
                throw new ArgumentException("The acceptance criteria require exactly five runtime measurements.", nameof(config));
            IReadOnlyList<EvaluationResult> results = _algorithms
                .Select(algorithm => EvaluateOne(algorithm, config, workload, cancellationToken))
                .ToArray();
            return Task.FromResult(results);
        }

        private static EvaluationResult EvaluateOne(
            IFitAlgorithm algorithm, EvaluationConfig config, EvaluationWorkload workload,
            CancellationToken cancellationToken)
        {
            var runtimes = new List<double>(5);
            RunSummary? finalSummary = null;

            for (int run = 0; run < 5; run++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var freeBlocks = config.InitialFreeBlocks.ToList();
                var stopwatch = Stopwatch.StartNew();
                RunSummary summary = ExecuteWorkload(algorithm, freeBlocks, workload, cancellationToken);
                stopwatch.Stop();

                runtimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                finalSummary = summary;
            }

            var s = finalSummary!;
            return new EvaluationResult
            {
                AlgorithmName = algorithm.Name,
                TotalAllocateRequests = s.TotalRequests,
                SuccessCount = s.Success,
                FailureCount = s.Failure,
                SuccessRate = s.TotalRequests == 0 ? null : (double)s.Success / s.TotalRequests * 100.0,
                RuntimeRun1Ms = runtimes[0],
                RuntimeRun2Ms = runtimes[1],
                RuntimeRun3Ms = runtimes[2],
                RuntimeRun4Ms = runtimes[3],
                RuntimeRun5Ms = runtimes[4],
                AverageRuntimeMs = runtimes.Average(),
                TotalInternalFragmentation = s.InternalFragmentation,
                LargestFreeBlock = s.FreeBlocks.Count == 0 ? 0 : s.FreeBlocks.Max(),
                RuntimeRunsMs = runtimes.ToArray()
            };
        }

        private static RunSummary ExecuteWorkload(
            IFitAlgorithm algorithm, List<int> freeBlocks, EvaluationWorkload workload,
            CancellationToken cancellationToken)
        {
            int total = 0, success = 0, failure = 0;
            long fragmentation = 0;
            var allocated = new Dictionary<string, int>();
            int allocationIndex = 0;

            foreach (var operation in workload.Operations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (operation.Type == WorkloadOperationType.Allocate)
                {
                    total++;
                    int? index = algorithm.FindBlock(freeBlocks, operation.Size);
                    if (index.HasValue && index.Value >= 0 && index.Value < freeBlocks.Count &&
                        freeBlocks[index.Value] >= operation.Size)
                    {
                        int blockSize = freeBlocks[index.Value];
                        fragmentation += blockSize - operation.Size;
                        freeBlocks.RemoveAt(index.Value);
                        int remainder = blockSize - operation.Size;
                        if (remainder > 0) freeBlocks.Add(remainder);
                        string key = string.IsNullOrWhiteSpace(operation.BlockId)
                            ? $"allocation-{allocationIndex++}" : operation.BlockId!;
                        allocated[key] = operation.Size;
                        success++;
                    }
                    else failure++;
                }
                else if (operation.Type == WorkloadOperationType.Free &&
                         !string.IsNullOrWhiteSpace(operation.BlockId) &&
                         allocated.TryGetValue(operation.BlockId!, out int size))
                {
                    freeBlocks.Add(size);
                    allocated.Remove(operation.BlockId!);
                }
            }

            return new RunSummary(total, success, failure, fragmentation, freeBlocks);
        }

        private sealed class RunSummary
        {
            public int TotalRequests { get; }
            public int Success { get; }
            public int Failure { get; }
            public long InternalFragmentation { get; }
            public List<int> FreeBlocks { get; }
            public RunSummary(int total, int success, int failure, long fragmentation, List<int> freeBlocks)
                => (TotalRequests, Success, Failure, InternalFragmentation, FreeBlocks) =
                   (total, success, failure, fragmentation, freeBlocks);
        }
    }
    public sealed class MockFitAlgorithm : IFitAlgorithm
    {
        public string Name { get; }
        public MockFitAlgorithm(string name) => Name = name;
        public int? FindBlock(IReadOnlyList<int> freeBlocks, int requestedSize)
        {
            for (int i = 0; i < freeBlocks.Count; i++)
                if (freeBlocks[i] >= requestedSize) return i;
            return null;
        }
    }
}
