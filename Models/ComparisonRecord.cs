namespace OSMemoryAllocationSimulator.Models;

/// <summary>
/// Dữ liệu tổng hợp kết quả đánh giá một thuật toán trên một workload (20 cột chuẩn).
/// Được sử dụng chung bởi EvaluationService, Evaluation UI (Bảng so sánh) và CsvExporter.
/// </summary>
public sealed record ComparisonRecord
{
    public string WorkloadName { get; init; }
    public string Algorithm { get; init; }
    public int OperationCount { get; init; }
    public int MemorySize { get; init; }
    public int AllocationUnit { get; init; }
    public int TotalAllocationRequests { get; init; }
    public int SuccessfulAllocations { get; init; }
    public int FailedAllocations { get; init; }
    public double? SuccessRate { get; init; }
    public int UsedMemory { get; init; }
    public int FreeMemory { get; init; }
    public int FreeBlockCount { get; init; }
    public int LargestFreeBlock { get; init; }
    public int TotalInternalFragmentation { get; init; }
    public double RuntimeRun1Ms { get; init; }
    public double RuntimeRun2Ms { get; init; }
    public double RuntimeRun3Ms { get; init; }
    public double RuntimeRun4Ms { get; init; }
    public double RuntimeRun5Ms { get; init; }
    public double AverageRuntimeMs { get; init; }

    public ComparisonRecord(
        string workloadName,
        string algorithm,
        int operationCount,
        int memorySize,
        int allocationUnit,
        int totalAllocationRequests,
        int successfulAllocations,
        int failedAllocations,
        double? successRate,
        int usedMemory,
        int freeMemory,
        int freeBlockCount,
        int largestFreeBlock,
        int totalInternalFragmentation,
        double runtimeRun1Ms,
        double runtimeRun2Ms,
        double runtimeRun3Ms,
        double runtimeRun4Ms,
        double runtimeRun5Ms,
        double? averageRuntimeMs = null)
    {
        WorkloadName = workloadName;
        Algorithm = algorithm;
        OperationCount = operationCount;
        MemorySize = memorySize;
        AllocationUnit = allocationUnit;
        TotalAllocationRequests = totalAllocationRequests;
        SuccessfulAllocations = successfulAllocations;
        FailedAllocations = failedAllocations;
        SuccessRate = successRate;
        UsedMemory = usedMemory;
        FreeMemory = freeMemory;
        FreeBlockCount = freeBlockCount;
        LargestFreeBlock = largestFreeBlock;
        TotalInternalFragmentation = totalInternalFragmentation;
        RuntimeRun1Ms = runtimeRun1Ms;
        RuntimeRun2Ms = runtimeRun2Ms;
        RuntimeRun3Ms = runtimeRun3Ms;
        RuntimeRun4Ms = runtimeRun4Ms;
        RuntimeRun5Ms = runtimeRun5Ms;
        AverageRuntimeMs = averageRuntimeMs ?? ((runtimeRun1Ms + runtimeRun2Ms + runtimeRun3Ms + runtimeRun4Ms + runtimeRun5Ms) / 5.0);
    }
}
