using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSMemoryAllocationSimulator.Models;
using OSMemoryAllocationSimulator.Services;
using System.IO;
using Microsoft.Win32;
using OSMemoryAllocationSimulator.Data.Csv;

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
        int LargestFreeBlock,
        int UsedMemory = 0,
    int FreeMemory = 0,
    int FreeBlockCount = 0);

    /// <summary>Một điểm dữ liệu cho biểu đồ. Value2 dùng cho biểu đồ 2 cột (thất bại).</summary>
    public sealed record ChartPoint(string Label, double Value, double Value2 = 0);

    public partial class EvaluationViewModel : BaseViewModel
    {
        private readonly IEvaluationService _service;
        private readonly IComparisonCsvExporter _comparisonCsvExporter;
        private string _workloadName = "Workload";
        private readonly CsvWorkloadReader _workloadReader;
        private IReadOnlyList<WorkloadOperation> _workload = Array.Empty<WorkloadOperation>();

        [ObservableProperty]
        private int memorySize = 4096;

        [ObservableProperty]
        private int allocationUnit = 64;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunEvaluationCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExportComparisonCommand))]
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
        [ObservableProperty] private double allocationMax = 1;
        [ObservableProperty] private double fragmentationMax = 1;
        [ObservableProperty] private double largestFreeBlockMax = 1;
        [ObservableProperty] private double runtimeMax = 1;
        [ObservableProperty]
        private string workloadInfo = "Chưa chọn workload. Cột bắt buộc: Sequence, Operation, ProcessId, RequestedSize";
        public EvaluationViewModel()
            : this(
                new EvaluationService(),
                new CsvWorkloadReader(),
                new CsvExporter())
        {
        }

        public EvaluationViewModel(
            IEvaluationService service,
            CsvWorkloadReader workloadReader,
            IComparisonCsvExporter csvExporter)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _workloadReader = workloadReader ?? throw new ArgumentNullException(nameof(workloadReader));

            ArgumentNullException.ThrowIfNull(csvExporter);

            _comparisonCsvExporter = csvExporter;

        }

        /// <summary>Nạp workload hợp lệ. Kết quả cũ bị xóa.</summary>
        public void SetWorkload(IReadOnlyList<WorkloadOperation> workload)
        {
            _workload = workload;
            ClearResults();
            RunEvaluationCommand.NotifyCanExecuteChanged();
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
            ExportComparisonCommand.NotifyCanExecuteChanged();
            StatusMessage = "Cấu hình hoặc workload đã thay đổi, hãy chạy lại đánh giá.";
        }

        private bool CanRun() => !IsRunning && _workload.Count > 0;
        private bool CanExport() => !IsRunning && ComparisonResults.Count > 0;

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

        [RelayCommand(CanExecute = nameof(CanExport))]
        private void ExportComparison()
        {
            if (ComparisonResults.Count == 0)
            {
                StatusMessage = "Chưa có kết quả để xuất CSV. Hãy chạy đánh giá trước.";
                return;
            }

            try
            {
                var records = ComparisonResults.Select(r => new ComparisonRecord(
                    workloadName: _workloadName,
                    algorithm: r.AlgorithmName,
                    operationCount: _workload.Count,
                    memorySize: MemorySize,
                    allocationUnit: AllocationUnit,
                    totalAllocationRequests: r.TotalAllocateRequests,
                    successfulAllocations: r.SuccessCount,
                    failedAllocations: r.FailureCount,
                    successRate: r.SuccessRate,
                    usedMemory: r.UsedMemory,
                    freeMemory: r.FreeMemory,
                    freeBlockCount: r.FreeBlockCount,
                    largestFreeBlock: r.LargestFreeBlock,
                    totalInternalFragmentation: r.TotalInternalFragmentation,
                    runtimeRun1Ms: r.RuntimeRun1Ms,
                    runtimeRun2Ms: r.RuntimeRun2Ms,
                    runtimeRun3Ms: r.RuntimeRun3Ms,
                    runtimeRun4Ms: r.RuntimeRun4Ms,
                    runtimeRun5Ms: r.RuntimeRun5Ms,
                    averageRuntimeMs: r.AverageRuntimeMs
                )).ToList();

                var dialog = new SaveFileDialog
                {
                    Title = "Lưu file so sánh thuật toán (CSV)",
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = CsvExporter.GenerateComparisonFileName(_workloadName)
                };

                if (dialog.ShowDialog() != true)
                    return;

                CsvExporter.ExportComparisonToFile(dialog.FileName, records);

                StatusMessage = $"Đã xuất file so sánh thành công: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khi xuất file CSV: {ex.Message}";
            }
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
                    m.LargestFreeBlock,
                    m.UsedMemory,
                    m.FreeMemory,
                    m.FreeBlockCount));

                AllocationChart.Add(new ChartPoint(a.Algorithm, a.SuccessfulAllocations, a.FailedAllocations));
                FragmentationChart.Add(new ChartPoint(a.Algorithm, m.TotalInternalFragmentation));
                LargestFreeBlockChart.Add(new ChartPoint(a.Algorithm, m.LargestFreeBlock));
                RuntimeChart.Add(new ChartPoint(a.Algorithm, a.AverageRuntimeMs));
            }
            AllocationMax = Math.Max(0.0001, AllocationChart.Select(p => Math.Max(p.Value, p.Value2)).DefaultIfEmpty(0).Max());
            FragmentationMax = Math.Max(0.0001, FragmentationChart.Select(p => p.Value).DefaultIfEmpty(0).Max());
            LargestFreeBlockMax = Math.Max(0.0001, LargestFreeBlockChart.Select(p => p.Value).DefaultIfEmpty(0).Max());
            RuntimeMax = Math.Max(0.0001, RuntimeChart.Select(p => p.Value).DefaultIfEmpty(0).Max());

            EvaluationComment = BuildComment(ComparisonResults);
        }


        [RelayCommand]
        private void ChooseWorkloadFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Chọn file workload",
                Filter = "CSV (*.csv)|*.csv"
            };
            if (dialog.ShowDialog() != true) return;

            _workloadName = Path.GetFileNameWithoutExtension(dialog.FileName);
            var result = _workloadReader.Read(dialog.FileName);

            if (!result.IsSuccess || result.Value is null)
            {
                var lines = result.Errors.Take(5).Select(e => e.ToString()).ToList();
                if (result.Errors.Count > 5)
                    lines.Add($"... và {result.Errors.Count - 5} lỗi khác.");

                WorkloadInfo = "Lỗi đọc file workload:" + Environment.NewLine + string.Join(Environment.NewLine, lines);
                return;
            }

            SetWorkload(result.Value);
            WorkloadInfo = $"Đã chọn: {Path.GetFileName(dialog.FileName)} ({result.Value.Count} thao tác)";
            StatusMessage = "Đã nạp workload. Bấm Chạy đánh giá.";
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


