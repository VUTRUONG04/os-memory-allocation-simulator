using System.Globalization;
using System.IO;
using System.Text;
using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Data.Csv;

/// <summary>
/// Lớp chịu trách nhiệm xuất dữ liệu mô phỏng và so sánh thuật toán ra file CSV.
/// Tuân thủ nghiêm ngặt Schema tại Mục 5.3 và 5.4 trong tài liệu quy chuẩn.
/// </summary>
public class CsvExporter : ITraceCsvExporter, IComparisonCsvExporter
{
    /// <summary>
    /// Header chuẩn chính xác 10 cột của Trace CSV theo Mục 5.3.
    /// </summary>
    public const string TraceHeader =
        "Sequence,Operation,ProcessId,RequestedSize,AllocatedSize,Status,Reason,TotalFreeMemory,LargestFreeBlock,InternalFragmentation";

    /// <summary>
    /// Header chuẩn chính xác 20 cột của Comparison CSV theo Mục 5.4.
    /// </summary>
    public const string ComparisonHeader =
        "WorkloadName,Algorithm,OperationCount,MemorySize,AllocationUnit,TotalAllocationRequests,SuccessfulAllocations,FailedAllocations,SuccessRate,UsedMemory,FreeMemory,FreeBlockCount,LargestFreeBlock,TotalInternalFragmentation,RuntimeRun1Ms,RuntimeRun2Ms,RuntimeRun3Ms,RuntimeRun4Ms,RuntimeRun5Ms,AverageRuntimeMs";

    #region Trace CSV

    /// <summary>
    /// Sinh tên file Trace CSV theo chuẩn: Trace_{Algorithm}_{WorkloadName}_{yyyyMMdd_HHmmss}.csv
    /// </summary>
    public static string GenerateTraceFileName(string algorithm, string workloadName, DateTime? timestamp = null)
    {
        string cleanAlgo = CsvParserHelper.SanitizeFileName(algorithm, "Algorithm");
        string cleanWorkload = CsvParserHelper.SanitizeFileName(workloadName, "Workload");
        string timeStr = (timestamp ?? DateTime.Now).ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        return $"Trace_{cleanAlgo}_{cleanWorkload}_{timeStr}.csv";
    }

    /// <summary>
    /// Chuyển đổi danh sách TraceEntry thành chuỗi nội dung CSV hoàn chỉnh với Header 10 cột.
    /// </summary>
    public static string ExportTraceToCsvString(IEnumerable<TraceEntry> trace)
    {
        var sb = new StringBuilder();
        sb.AppendLine(TraceHeader);

        if (trace == null)
            return sb.ToString();

        foreach (var entry in trace)
        {
            string opStr = entry.Operation == MemoryOperationType.Allocate ? "ALLOCATE" : "FREE";
            string reqSizeStr = entry.RequestedSize?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            string allocSizeStr = entry.AllocatedSize?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            string statusStr = entry.Status == OperationStatus.Success ? "SUCCESS" : "FAILED";

            sb.Append(entry.Sequence.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(opStr).Append(',')
              .Append(CsvParserHelper.EscapeCsvField(entry.ProcessId)).Append(',')
              .Append(reqSizeStr).Append(',')
              .Append(allocSizeStr).Append(',')
              .Append(statusStr).Append(',')
              .Append(CsvParserHelper.EscapeCsvField(entry.Reason)).Append(',')
              .Append(entry.TotalFreeMemory.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(entry.LargestFreeBlock.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(entry.InternalFragmentation.ToString(CultureInfo.InvariantCulture))
              .AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Xuất trực tiếp Trace ra đường dẫn file chỉ định.
    /// </summary>
    public static void ExportTraceToFile(string filePath, IEnumerable<TraceEntry> trace)
    {
        string content = ExportTraceToCsvString(trace);
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    /// <summary>
    /// Triển khai ITraceCsvExporter.
    /// </summary>
    public ExportResult Export(TraceExportRequest request)
    {
        if (request == null)
            return ExportResult.Failure("Yêu cầu xuất Trace không được null.");

        try
        {
            string fileName = !string.IsNullOrWhiteSpace(request.CustomFileName)
                ? request.CustomFileName
                : GenerateTraceFileName(request.Algorithm, request.WorkloadName, request.Timestamp);

            string targetDir = string.IsNullOrWhiteSpace(request.OutputDirectory)
                ? Directory.GetCurrentDirectory()
                : request.OutputDirectory;

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string fullPath = Path.Combine(targetDir, fileName);
            ExportTraceToFile(fullPath, request.Trace);

            return ExportResult.Success(fullPath);
        }
        catch (Exception ex)
        {
            return ExportResult.Failure($"Lỗi khi xuất Trace CSV: {ex.Message}");
        }
    }

    #endregion

    #region Comparison CSV

    /// <summary>
    /// Sinh tên file Comparison CSV theo chuẩn: Comparison_{WorkloadName}_{yyyyMMdd_HHmmss}.csv
    /// </summary>
    public static string GenerateComparisonFileName(string workloadName, DateTime? timestamp = null)
    {
        string cleanWorkload = CsvParserHelper.SanitizeFileName(workloadName, "Workload");
        string timeStr = (timestamp ?? DateTime.Now).ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        return $"Comparison_{cleanWorkload}_{timeStr}.csv";
    }

    /// <summary>
    /// Chuyển đổi danh sách ComparisonRecord thành chuỗi nội dung CSV hoàn chỉnh với Header 20 cột.
    /// </summary>
    public static string ExportComparisonToCsvString(IEnumerable<ComparisonRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ComparisonHeader);

        if (records == null)
            return sb.ToString();

        foreach (var r in records)
        {
            // SuccessRate để trống nếu workload không có ALLOCATE (SuccessRate = null)
            string successRateStr = r.SuccessRate.HasValue
                ? r.SuccessRate.Value.ToString("0.##", CultureInfo.InvariantCulture)
                : string.Empty;

            sb.Append(CsvParserHelper.EscapeCsvField(r.WorkloadName)).Append(',')
              .Append(CsvParserHelper.EscapeCsvField(r.Algorithm)).Append(',')
              .Append(r.OperationCount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.MemorySize.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.AllocationUnit.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.TotalAllocationRequests.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.SuccessfulAllocations.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.FailedAllocations.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(successRateStr).Append(',')
              .Append(r.UsedMemory.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.FreeMemory.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.FreeBlockCount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.LargestFreeBlock.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.TotalInternalFragmentation.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.RuntimeRun1Ms.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.RuntimeRun2Ms.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.RuntimeRun3Ms.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.RuntimeRun4Ms.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.RuntimeRun5Ms.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.AverageRuntimeMs.ToString(CultureInfo.InvariantCulture))
              .AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Xuất trực tiếp dữ liệu Comparison ra đường dẫn file chỉ định.
    /// </summary>
    public static void ExportComparisonToFile(string filePath, IEnumerable<ComparisonRecord> records)
    {
        string content = ExportComparisonToCsvString(records);
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    /// <summary>
    /// Triển khai IComparisonCsvExporter.
    /// </summary>
    public ExportResult Export(ComparisonExportRequest request)
    {
        if (request == null)
            return ExportResult.Failure("Yêu cầu xuất Comparison không được null.");

        try
        {
            string fileName = !string.IsNullOrWhiteSpace(request.CustomFileName)
                ? request.CustomFileName
                : GenerateComparisonFileName(request.WorkloadName, request.Timestamp);

            string targetDir = string.IsNullOrWhiteSpace(request.OutputDirectory)
                ? Directory.GetCurrentDirectory()
                : request.OutputDirectory;

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string fullPath = Path.Combine(targetDir, fileName);
            ExportComparisonToFile(fullPath, request.Records);

            return ExportResult.Success(fullPath);
        }
        catch (Exception ex)
        {
            return ExportResult.Failure($"Lỗi khi xuất Comparison CSV: {ex.Message}");
        }
    }

    #endregion
}
