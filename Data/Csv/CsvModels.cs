using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Data.Csv;

/// <summary>
/// Đại diện cho một lỗi validation khi đọc file CSV,
/// bao gồm số dòng, tên cột và lý do lỗi chi tiết.
/// </summary>
public sealed record CsvValidationError(int LineNumber, string? ColumnName, string Message)
{
    public override string ToString()
    {
        if (LineNumber <= 0 && string.IsNullOrEmpty(ColumnName))
            return Message;

        if (LineNumber > 0 && string.IsNullOrEmpty(ColumnName))
            return $"Dòng {LineNumber}: {Message}";

        return $"Dòng {LineNumber}, Cột '{ColumnName}': {Message}";
    }
}

/// <summary>
/// Kết quả đọc và validate file CSV.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu kết quả khi thành công.</typeparam>
public sealed record CsvReadResult<T>(
    bool IsSuccess,
    T? Value,
    IReadOnlyList<CsvValidationError> Errors)
{
    public static CsvReadResult<T> Success(T value) =>
        new(true, value, Array.Empty<CsvValidationError>());

    public static CsvReadResult<T> Failure(IEnumerable<CsvValidationError> errors) =>
        new(false, default, errors.ToList());

    public static CsvReadResult<T> Failure(int lineNumber, string? columnName, string message) =>
        new(false, default, new[] { new CsvValidationError(lineNumber, columnName, message) });

    /// <summary>
    /// Tổng hợp tất cả các thông báo lỗi thành chuỗi có thể hiển thị cho người dùng.
    /// </summary>
    public string ErrorSummary => Errors.Count == 0
        ? string.Empty
        : string.Join(Environment.NewLine, Errors.Select(e => e.ToString()));
}

/// <summary>
/// Dữ liệu một hàng so sánh thuật toán trong Comparison CSV (20 cột chuẩn).
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

/// <summary>
/// Yêu cầu xuất Trace CSV.
/// </summary>
public sealed record TraceExportRequest(
    string OutputDirectory,
    string Algorithm,
    string WorkloadName,
    IReadOnlyList<TraceEntry> Trace,
    DateTime? Timestamp = null,
    string? CustomFileName = null);

/// <summary>
/// Yêu cầu xuất Comparison CSV.
/// </summary>
public sealed record ComparisonExportRequest(
    string OutputDirectory,
    string WorkloadName,
    IReadOnlyList<ComparisonRecord> Records,
    DateTime? Timestamp = null,
    string? CustomFileName = null);

/// <summary>
/// Kết quả xuất file CSV.
/// </summary>
public sealed record ExportResult(
    bool IsSuccess,
    string? FilePath,
    string? ErrorMessage)
{
    public static ExportResult Success(string filePath) => new(true, filePath, null);
    public static ExportResult Failure(string errorMessage) => new(false, null, errorMessage);
}

/// <summary>
/// Interface dịch vụ xuất Trace CSV.
/// </summary>
public interface ITraceCsvExporter
{
    ExportResult Export(TraceExportRequest request);
}

/// <summary>
/// Interface dịch vụ xuất Comparison CSV.
/// </summary>
public interface IComparisonCsvExporter
{
    ExportResult Export(ComparisonExportRequest request);
}
