using System.Linq;
using System.Threading.Tasks;
using OSMemoryAllocationSimulator.Services;
using Xunit;

namespace OSMemoryAllocationSimulator.Tests
{
    public sealed class EvaluationServiceTests
    {
        [Fact]
        public async Task EvaluateAsync_ReturnsThreeAlgorithmsAndFiveRuntimeMeasurements()
        {
            var service = new EvaluationService();
            var config = new EvaluationConfig { InitialFreeBlocks = new[] { 100, 200 }, RepeatCount = 5 };
            var workload = new EvaluationWorkload
            {
                Operations = new[]
                {
                    new WorkloadOperation { Type = WorkloadOperationType.Allocate, Size = 50, BlockId = "A" },
                    new WorkloadOperation { Type = WorkloadOperationType.Allocate, Size = 500, BlockId = "B" }
                }
            };

            var results = await service.EvaluateAsync(config, workload);
            Assert.Equal(3, results.Count);
            Assert.All(results, r =>
            {
                Assert.Equal(2, r.TotalAllocateRequests);
                Assert.Equal(1, r.SuccessCount);
                Assert.Equal(1, r.FailureCount);
                Assert.Equal(50.0, r.SuccessRate);
                Assert.Equal(5, r.RuntimeRunsMs.Count);
                Assert.Equal(r.RuntimeRunsMs.Average(), r.AverageRuntimeMs, 6);
            });
        }

        [Fact]
        public async Task EvaluateAsync_SuccessRateIsNullWhenNoAllocateRequests()
        {
            var service = new EvaluationService();
            var config = new EvaluationConfig { InitialFreeBlocks = new[] { 100 }, RepeatCount = 5 };
            var workload = new EvaluationWorkload
            {
                Operations = new[] { new WorkloadOperation { Type = WorkloadOperationType.Free, BlockId = "missing" } }
            };

            var results = await service.EvaluateAsync(config, workload);
            Assert.All(results, r => Assert.Null(r.SuccessRate));
        }
    }
}
