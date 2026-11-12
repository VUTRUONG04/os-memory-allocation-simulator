using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using OSMemoryAllocationSimulator.Services;
namespace OSMemoryAllocationSimulator.ViewModels
{
    public sealed class EvaluationViewModel : BaseViewModel
    {
        private readonly EvaluationService _service;
        private bool _isRunning;
        private string _evaluationComment = "Chưa có kết quả. Hãy chạy đánh giá.";
        private EvaluationConfig _config = new() { InitialFreeBlocks = new[] { 100, 250, 400, 600 } };
        private EvaluationWorkload _workload = new()
        {
            Operations = new[]
            {
                new WorkloadOperation { Type = WorkloadOperationType.Allocate, Size = 80, BlockId = "A" },
                new WorkloadOperation { Type = WorkloadOperationType.Allocate, Size = 200, BlockId = "B" },
                new WorkloadOperation { Type = WorkloadOperationType.Allocate, Size = 350, BlockId = "C" },
                new WorkloadOperation { Type = WorkloadOperationType.Allocate, Size = 700, BlockId = "D" },
                new WorkloadOperation { Type = WorkloadOperationType.Free, BlockId = "B" }
            }
        };

        public ObservableCollection<EvaluationResult> ComparisonResults { get; } = new();
        public ObservableCollection<ChartPoint> AllocationChart { get; } = new();
        public ObservableCollection<ChartPoint> FragmentationChart { get; } = new();
        public ObservableCollection<ChartPoint> LargestFreeBlockChart { get; } = new();
        public ObservableCollection<ChartPoint> RuntimeChart { get; } = new();

        public bool IsRunning
        {
            get => _isRunning;
            private set { _isRunning = value; OnPropertyChanged(nameof(IsRunning)); }
        }
        public string EvaluationComment
        {
            get => _evaluationComment;
            private set { _evaluationComment = value; OnPropertyChanged(nameof(EvaluationComment)); }
        }

        public ICommand RunEvaluationCommand { get; }
        public ICommand ExportComparisonCommand { get; }

        public EvaluationViewModel() : this(new EvaluationService()) { }
        public EvaluationViewModel(EvaluationService service)
        {
            _service = service;
            RunEvaluationCommand = new AsyncCommand(RunEvaluationAsync, () => !IsRunning);
            ExportComparisonCommand = new RelayCommand(ExportComparison);
        }

        public void UpdateInput(EvaluationConfig config, EvaluationWorkload workload)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _workload = workload ?? throw new ArgumentNullException(nameof(workload));
            ClearResults();
        }

        public async Task RunEvaluationAsync()
        {
            if (IsRunning) return;
            IsRunning = true;
            ClearResults();
            try
            {
                var results = await Task.Run(async () =>
                    await _service.EvaluateAsync(_config, _workload).ConfigureAwait(false));
                foreach (var result in results) ComparisonResults.Add(result);
                RefreshCharts();
                EvaluationComment = BuildObjectiveComment(results.ToArray());
            }
            catch (Exception ex)
            {
                EvaluationComment = "Không thể đánh giá: " + ex.Message;
            }
            finally { IsRunning = false; }
        }

        private void RefreshCharts()
        {
            AllocationChart.Clear(); FragmentationChart.Clear();
            LargestFreeBlockChart.Clear(); RuntimeChart.Clear();
            foreach (var r in ComparisonResults)
            {
                AllocationChart.Add(new ChartPoint(r.AlgorithmName, r.SuccessCount, r.FailureCount));
                FragmentationChart.Add(new ChartPoint(r.AlgorithmName, r.TotalInternalFragmentation));
                LargestFreeBlockChart.Add(new ChartPoint(r.AlgorithmName, r.LargestFreeBlock));
                RuntimeChart.Add(new ChartPoint(r.AlgorithmName, r.AverageRuntimeMs));
            }
            OnPropertyChanged(nameof(AllocationChart));
            OnPropertyChanged(nameof(FragmentationChart));
            OnPropertyChanged(nameof(LargestFreeBlockChart));
            OnPropertyChanged(nameof(RuntimeChart));
        }

        private static string BuildObjectiveComment(EvaluationResult[] results)
        {
            if (results.Length == 0) return "Không có dữ liệu để nhận xét.";
            var fastest = results.OrderBy(x => x.AverageRuntimeMs).First();
            var mostSuccessful = results.OrderByDescending(x => x.SuccessRate ?? -1).First();
            var leastFragmented = results.OrderBy(x => x.TotalInternalFragmentation).First();
            return $"Theo workload hiện tại: thời gian trung bình thấp nhất là {fastest.AlgorithmName} " +
                   $"({fastest.AverageRuntimeMs:F4} ms); tỷ lệ thành công cao nhất là {mostSuccessful.AlgorithmName} " +
                   $"({(mostSuccessful.SuccessRate.HasValue ? mostSuccessful.SuccessRate.Value.ToString("F2") + "%" : "N/A")}); " +
                   $"phân mảnh nội bộ thấp nhất là {leastFragmented.AlgorithmName} " +
                   $"({leastFragmented.TotalInternalFragmentation}). Kết luận chỉ áp dụng cho cấu hình/workload này.";
        }

        private void ClearResults()
        {
            ComparisonResults.Clear(); AllocationChart.Clear(); FragmentationChart.Clear();
            LargestFreeBlockChart.Clear(); RuntimeChart.Clear();
            EvaluationComment = "Đã xóa kết quả cũ. Hãy chạy đánh giá lại.";
        }

        private void ExportComparison()
        {
            Debug.WriteLine("Exporting comparison CSV...");
            EvaluationComment = "Chức năng xuất CSV đang ở chế độ mock (ghi log Debug).";
        }
    }

    public sealed class ChartPoint
    {
        public string Label { get; }
        public double Value { get; }
        public double SecondaryValue { get; }
        public ChartPoint(string label, double value, double secondaryValue = 0)
            => (Label, Value, SecondaryValue) = (label, value, secondaryValue);
    }

    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            => (_execute, _canExecute) = (execute, canExecute);
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    public sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
            => (_execute, _canExecute) = (execute, canExecute);
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public async void Execute(object? parameter) => await _execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
