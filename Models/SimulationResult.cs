namespace OSMemoryAllocationSimulator.Models;

/// <summary>
/// Snapshot các chỉ số bộ nhớ tại một thời điểm.
/// UsedMemory + FreeMemory phải luôn bằng MemorySize.
/// </summary>
public sealed record MemoryMetrics(
    int UsedMemory,
    int FreeMemory,
    int FreeBlockCount,
    int LargestFreeBlock,
    int TotalInternalFragmentation);

/// <summary>
/// Kết quả hoàn chỉnh của một lượt mô phỏng.
/// Chứa cấu hình, toàn bộ Trace, trạng thái bộ nhớ cuối và chỉ số cuối.
/// ViewModel không cần tự tính lại nghiệp vụ từ kết quả này.
/// </summary>
public sealed record SimulationResult(
    SimulationConfig Config,
    IReadOnlyList<TraceEntry> Trace,
    IReadOnlyList<MemoryRegion> FinalRegions,
    MemoryMetrics FinalMetrics);
