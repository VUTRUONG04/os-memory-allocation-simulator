using System.IO;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OSMemoryAllocationSimulator.Algorithms;
using OSMemoryAllocationSimulator.Models;
using OSMemoryAllocationSimulator.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace OSMemoryAllocationSimulator.ViewModels;

public partial class SimulationConfigViewModel : BaseViewModel
{
    // ==========================================
    // 1. OBSERVABLE PROPERTIES
    // ==========================================
    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private int memorySize = 4096;

    [ObservableProperty]
    private int allocationUnit = 64;

    [ObservableProperty]
    private string selectedAlgorithm = "FirstFit (Mock)";

    [ObservableProperty]
    private string statusMessage = "Chưa bắt đầu mô phỏng.";

    [ObservableProperty]
    private string csvFilePath = string.Empty;
    [ObservableProperty]
    private int usedMemory;

    [ObservableProperty]
    private int freeMemory;

    [ObservableProperty]
    private int freeBlockCount;

    [ObservableProperty]
    private int largestFreeBlock;

    [ObservableProperty]
    private int totalInternalFragmentation;

    [ObservableProperty]
    private double memoryUsagePercentage;

    // ==========================================
    // 2. OBSERVABLE COLLECTIONS
    // ==========================================

    public ObservableCollection<string> AlgorithmOptions { get; } = new()
    {
        "FirstFit (Mock)",
        "BestFit (Chưa triển khai)",
        "WorstFit (Chưa triển khai)"
    };

    public ObservableCollection<MemoryRegion> MemoryMap { get; } = new();

    public ObservableCollection<TraceEntry> TraceEntries { get; } = new();

    // ==========================================
    // 3. SERVICE VÀ WORKLOAD
    // ==========================================
    private readonly DispatcherTimer _simulationTimer;
    private SimulationService? _simulationService;

    private readonly IReadOnlyList<WorkloadOperation> _presetWorkload =
        new List<WorkloadOperation>
        {
            new(1, MemoryOperationType.Allocate, "P1", 100),
            new(2, MemoryOperationType.Allocate, "P2", 180),
            new(3, MemoryOperationType.Free, "P1", null),
            new(4, MemoryOperationType.Free, "P2", null)
        };

    private IReadOnlyList<WorkloadOperation> _workload;

    private readonly WorkloadCsvService _csvService = new();

    public SimulationConfigViewModel()
    {
        _workload = _presetWorkload;

        _simulationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        _simulationTimer.Tick += (_, _) => Step();
    }

    // ==========================================
    // 4. RESULT INVALIDATION
    // ==========================================

    partial void OnMemorySizeChanged(int value)
    {
        InvalidateSimulation();
    }

    partial void OnAllocationUnitChanged(int value)
    {
        InvalidateSimulation();
    }

    partial void OnSelectedAlgorithmChanged(string value)
    {
        InvalidateSimulation();
    }

    private void InvalidateSimulation()
    {
        _simulationService = null;
        _simulationTimer.Stop();
        IsRunning = false;
        MemoryMap.Clear();
        TraceEntries.Clear();

        UsedMemory = 0;
        FreeMemory = 0;
        FreeBlockCount = 0;
        LargestFreeBlock = 0;
        TotalInternalFragmentation = 0;
        MemoryUsagePercentage = 0;

        StatusMessage =
            "Cấu hình đã thay đổi. Hãy bắt đầu mô phỏng lại.";
    }
    // ==========================================
    // 5. ResetStatistics()
    // ==========================================

    private void ResetStatistics()
    {
        UsedMemory = 0;
        FreeMemory = 0;
        FreeBlockCount = 0;
        LargestFreeBlock = 0;
        TotalInternalFragmentation = 0;
        MemoryUsagePercentage = 0;
    }
    // ==========================================
    // 5. LOAD PRESET WORKLOAD COMMAND
    // ==========================================

    [RelayCommand]
    private void LoadPreset()
    {
        _workload = _presetWorkload;

        CsvFilePath = string.Empty;

        InvalidateSimulation();

        StatusMessage =
            $"Đã nạp workload mẫu gồm {_workload.Count} thao tác.";
    }

    // ==========================================
    // 6. BROWSE AND LOAD CSV COMMAND
    // ==========================================

    [RelayCommand]
    private void BrowseAndLoadCsv()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn file Workload CSV",
            Filter = "CSV files (*.csv)|*.csv",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            // Đọc và kiểm tra dữ liệu CSV
            var operations = _csvService.Load(dialog.FileName);

            // Chỉ cập nhật workload khi đọc CSV thành công
            _workload = operations;

            CsvFilePath = dialog.FileName;

            // Xóa kết quả mô phỏng cũ
            InvalidateSimulation();

            StatusMessage =
                $"Nạp CSV thành công: {operations.Count} thao tác.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Không thể nạp CSV: {ex.Message}";
        }
    }

    // ==========================================
    // 7. START SIMULATION COMMAND
    // ==========================================

    [RelayCommand]
    private void StartSimulation()
    {
        // Kiểm tra cấu hình bộ nhớ
        _simulationTimer.Stop();
        IsRunning = false;
        if (MemorySize <= 0 || AllocationUnit <= 0)
        {
            StatusMessage =
                "MemorySize và AllocationUnit phải lớn hơn 0.";
            return;
        }

        if (MemorySize < AllocationUnit)
        {
            StatusMessage =
                "MemorySize phải lớn hơn hoặc bằng AllocationUnit.";
            return;
        }

        // Kiểm tra workload
        if (_workload == null || _workload.Count == 0)
        {
            StatusMessage = "Workload không có thao tác nào.";
            return;
        }

        // Hiện tại chỉ hỗ trợ FirstFit Mock
        if (SelectedAlgorithm != "FirstFit (Mock)")
        {
            StatusMessage =
                "Thuật toán này chưa được triển khai. Hãy chọn FirstFit (Mock).";
            return;
        }

        try
        {
            // Tạo cấu hình mô phỏng
            var config = new SimulationConfig(
                MemorySize,
                AllocationUnit);

            // Khởi tạo SimulationService
            _simulationService = new SimulationService(
                config,
                new SimpleFirstFitMock(),
                _workload);

            // Xóa dữ liệu cũ trên giao diện
            MemoryMap.Clear();
            TraceEntries.Clear();

            // Hiển thị bộ nhớ ban đầu
            RefreshMemoryMap();

            StatusMessage =
                $"Mô phỏng đã sẵn sàng. Có {_workload.Count} thao tác cần thực hiện.";
        }
        catch (Exception ex)
        {
            _simulationTimer.Stop();
            IsRunning = false;

            _simulationService = null;

            MemoryMap.Clear();
            TraceEntries.Clear();

            ResetStatistics();

            StatusMessage =
                $"Không thể bắt đầu mô phỏng: {ex.Message}";
        }
    }

    // ==========================================
    // 8. STEP COMMAND
    // ==========================================

    [RelayCommand]
    private void Step()
    {
        if (_simulationService is null)
        {
            StatusMessage =
                "Vui lòng bắt đầu mô phỏng trước.";
            return;
        }

        var entry = _simulationService.Step();

        if (entry is null)
        {
            StatusMessage = "Mô phỏng đã hoàn thành.";
            return;
        }

        // Cập nhật Trace
        TraceEntries.Add(entry);

        // Cập nhật Memory Map
        RefreshMemoryMap();

        StatusMessage =
            $"Đã thực hiện thao tác {entry.Sequence}: {entry.Operation} - {entry.ProcessId}";

        if (_simulationService.IsCompleted)
        {
            _simulationTimer.Stop();
            IsRunning = false;

            StatusMessage = "Mô phỏng đã hoàn thành!";
        }
    }

    // ==========================================
    // 9. RESET COMMAND
    // ==========================================

    [RelayCommand]
    private void Reset()
    {
        // Dừng Timer trước khi Reset
        _simulationTimer.Stop();
        IsRunning = false;

        if (_simulationService is null)
        {
            MemoryMap.Clear();
            TraceEntries.Clear();

            ResetStatistics();

            StatusMessage = "Chưa có mô phỏng để đặt lại.";
            return;
        }

        // Reset bộ nhớ về trạng thái ban đầu
        _simulationService.Reset();

        // Xóa lịch sử thao tác
        TraceEntries.Clear();

        // Cập nhật lại giao diện
        RefreshMemoryMap();

        StatusMessage =
            "Đã đặt lại mô phỏng về trạng thái ban đầu.";
    }
    // ==========================================
    // 10. REFRESH MEMORY MAP
    // ==========================================

    private void RefreshMemoryMap()
    {
        if (_simulationService is null)
            return;

        // Cập nhật Memory Map
        MemoryMap.Clear();

        foreach (var region in _simulationService.MemoryMap)
        {
            MemoryMap.Add(region);
        }

        // Cập nhật Statistics
        var metrics = _simulationService.CurrentMetrics;

        UsedMemory = metrics.UsedMemory;
        FreeMemory = metrics.FreeMemory;
        FreeBlockCount = metrics.FreeBlockCount;
        LargestFreeBlock = metrics.LargestFreeBlock;
        TotalInternalFragmentation = metrics.TotalInternalFragmentation;

        // Tính phần trăm sử dụng bộ nhớ
        MemoryUsagePercentage = MemorySize > 0
            ? Math.Round((double)UsedMemory / MemorySize * 100, 2)
            : 0;
    }

    // ==========================================
    // 11. SIMPLE FIRST FIT MOCK
    // ==========================================

    private sealed class SimpleFirstFitMock : IFitAlgorithm
    {
        public string Name => "FirstFitMock";

        public MemoryRegion? FindFit(
            IReadOnlyList<MemoryRegion> freeRegions,
            int requiredSize)
        {
            return freeRegions.FirstOrDefault(
                region => region.Size >= requiredSize);
        }
    }
    [RelayCommand]
    private void Run()
    {
        if (_simulationService is null)
        {
            StatusMessage = "Vui lòng bắt đầu mô phỏng trước.";
            return;
        }

        if (_simulationService.IsCompleted)
        {
            StatusMessage = "Mô phỏng đã hoàn thành.";
            return;
        }

        _simulationTimer.Start();
        IsRunning = true;

        StatusMessage = "Đang chạy mô phỏng tự động...";
    }

    [RelayCommand]
    private void Pause()
    {
        _simulationTimer.Stop();
        IsRunning = false;

        StatusMessage = "Mô phỏng đã tạm dừng.";
    }
}
