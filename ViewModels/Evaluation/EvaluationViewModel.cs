using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSMemoryAllocationSimulator.Models;
using OSMemoryAllocationSimulator.Services;

namespace OSMemoryAllocationSimulator.ViewModels.Evaluation
{
    
    public sealed record ComparisonRow(
        string AlgorithmName,
        int TotalAllocateRequests,
        int SuccessCount,
        int FailureCount,
        double? SuccessRate, // phần trăm 0..100; null nếu không có ALLOCATE
        double RuntimeRun1Ms,
        double RuntimeRun2Ms,
        double RuntimeRun3Ms,
        double RuntimeRun4Ms,
        double RuntimeRun5Ms,
        double AverageRuntimeMs,
        int TotalInternalFragmentation,
        int LargestFreeBlock);

    /// <summary>Một điểm dữ liệu cho biểu đồ. Value2 dùng cho biểu đồ 2 cột (thất bại).</summary>
    public sealed record ChartPoint(string Label, double Value, double Value2 = 0);

    public partial class EvaluationViewModel : BaseViewModel
    {
        private readonly IEvaluationService _service;
        private IReadOnlyList<WorkloadOperation> _workload = SampleWorkload();

        [ObservableProperty]
        private int memorySize = 4096;

        [ObservableProperty]
        private int allocationUnit = 64;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunEvaluationCommand))]
        private bool isRunning;

        [ObservableProperty]
        private string statusMessage = "Chưa chạy đánh giá.";

        [ObservableProperty]
        private string evaluationComment = string.Empty;

        public ObservableCollection<ComparisonRow> ComparisonResults { get; } = new();
        public ObservableCollection<ChartPoint> AllocationChart { get; } = new();
        public ObservableCollection<ChartPoint> FragmentationChart { get; } = new();
        public ObservableCollection<ChartPoint> LargestFreeBlockChart { get; } = new();
        public ObservableCollection<ChartPoint> RuntimeChart { get; } = new();

        public EvaluationViewModel() : this(new EvaluationService())
        {
        }

        public EvaluationViewModel(IEvaluationService service)
        {
            _service = service;
        }

        /// <summary>Nạp workload hợp lệ. Kết quả cũ bị xóa.</summary>
        public void SetWorkload(IReadOnlyList<WorkloadOperation> workload)
        {
            _workload = workload;
            ClearResults();
        }

        partial void OnMemorySizeChanged(int value) => ClearResults();
        partial void OnAllocationUnitChanged(int value) => ClearResults();

        private void ClearResults()
        {
            if (ComparisonResults.Count == 0) return;

            ComparisonResults.Clear();
            AllocationChart.Clear();
            FragmentationChart.Clear();
            LargestFreeBlockChart.Clear();
            RuntimeChart.Clear();
            EvaluationComment = string.Empty;
            StatusMessage = "Cấu hình hoặc workload đã thay đổi, hãy chạy lại đánh giá.";
        }

        private bool CanRun() => !IsRunning;

        [RelayCommand(CanExecute = nameof(CanRun))]
        private async Task RunEvaluationAsync()
        {
            if (_workload.Count == 0)
            {
                StatusMessage = "Chưa có workload.";
                return;
            }

            IsRunning = true;
            StatusMessage = "Đang chạy...";
            ComparisonResults.Clear();

            try
            {
                var config = new SimulationConfig(MemorySize, AllocationUnit);
                var workload = _workload;
                var result = await Task.Run(() => _service.Run(config, workload));

                Fill(result);
                StatusMessage = "Hoàn thành.";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Đã hủy.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }

        [RelayCommand]
        private void ExportComparison()
        {
            // Tạm thời: chưa có CsvExporter (theo task Jira).
            Debug.WriteLine("Exporting...");
            StatusMessage = "Xuất CSV so sánh: chưa có CsvExporter.";
        }

        private void Fill(EvaluationResult result)
        {
            ComparisonResults.Clear();
            AllocationChart.Clear();
            FragmentationChart.Clear();
            LargestFreeBlockChart.Clear();
            RuntimeChart.Clear();

            foreach (var a in result.Algorithms)
            {
                var m = a.Result.FinalMetrics;
                var t = a.RuntimesMs;

                ComparisonResults.Add(new ComparisonRow(
                    a.Algorithm,
                    a.TotalAllocationRequests,
                    a.SuccessfulAllocations,
                    a.FailedAllocations,
                    a.SuccessRate * 100,
                    t[0], t[1], t[2], t[3], t[4],
                    a.AverageRuntimeMs,
                    m.TotalInternalFragmentation,
                    m.LargestFreeBlock));

                AllocationChart.Add(new ChartPoint(a.Algorithm, a.SuccessfulAllocations, a.FailedAllocations));
                FragmentationChart.Add(new ChartPoint(a.Algorithm, m.TotalInternalFragmentation));
                LargestFreeBlockChart.Add(new ChartPoint(a.Algorithm, m.LargestFreeBlock));
                RuntimeChart.Add(new ChartPoint(a.Algorithm, a.AverageRuntimeMs));
            }

            EvaluationComment = BuildComment(ComparisonResults);
        }

        // Nhận xét khách quan, chỉ dựa trên số liệu, không thiên vị thuật toán nào.
        private static string BuildComment(IReadOnlyList<ComparisonRow> rows)
        {
            if (rows.Count == 0) return string.Empty;

            static string Names(IEnumerable<ComparisonRow> r) => string.Join(", ", r.Select(x => x.AlgorithmName));

            var parts = new List<string>();

            var withRate = rows.Where(r => r.SuccessRate.HasValue).ToList();
            if (withRate.Count > 0)
            {
                double best = withRate.Max(r => r.SuccessRate!.Value);
                parts.Add($"Tỷ lệ cấp phát thành công cao nhất ({best:F2}%): {Names(withRate.Where(r => r.SuccessRate == best))}.");
            }
            else
            {
                parts.Add("Workload không có yêu cầu ALLOCATE nên không có tỷ lệ thành công.");
            }

            int minFrag = rows.Min(r => r.TotalInternalFragmentation);
            parts.Add($"Phân mảnh nội thấp nhất ({minFrag}): {Names(rows.Where(r => r.TotalInternalFragmentation == minFrag))}.");

            int maxFree = rows.Max(r => r.LargestFreeBlock);
            parts.Add($"Vùng trống lớn nhất ({maxFree}): {Names(rows.Where(r => r.LargestFreeBlock == maxFree))}.");

            double minTime = rows.Min(r => r.AverageRuntimeMs);
            parts.Add($"Thời gian chạy trung bình thấp nhất ({minTime:F4} ms): {Names(rows.Where(r => r.AverageRuntimeMs == minTime))}.");

            return string.Join(" ", parts);
        }

        // Workload mẫu để chạy thử giao diện khi chưa có bộ đọc CSV.
        private static IReadOnlyList<WorkloadOperation> SampleWorkload() => new List<WorkloadOperation>
        {
            new(1, MemoryOperationType.Allocate, "P1", 500),
            new(2, MemoryOperationType.Allocate, "P2", 1200),
            new(3, MemoryOperationType.Allocate, "P3", 300),
            new(4, MemoryOperationType.Free, "P2", null),
            new(5, MemoryOperationType.Allocate, "P4", 700),
            new(6, MemoryOperationType.Allocate, "P5", 2500),
            new(7, MemoryOperationType.Free, "P1", null),
            new(8, MemoryOperationType.Allocate, "P6", 400)
        };
    }
}


