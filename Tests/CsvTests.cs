using System.Globalization;
using OSMemoryAllocationSimulator.Data.Csv;
using OSMemoryAllocationSimulator.Models;
using Xunit;

namespace OSMemoryAllocationSimulator.Tests;

public class CsvTests
{
    private readonly CsvConfigReader _configReader = new();
    private readonly CsvWorkloadReader _workloadReader = new();

    #region Config CSV Reader Tests

    [Fact]
    public void ConfigReader_ValidCsv_ReturnsSuccessAndCorrectConfig()
    {
        string csv = @"MemorySize,AllocationUnit
1024,64";

        var result = _configReader.ReadFromString(csv);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1024, result.Value.MemorySize);
        Assert.Equal(64, result.Value.AllocationUnit);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ConfigReader_ValidCsv_WithQuotesAndWhitespace_ReturnsSuccess()
    {
        string csv = "MemorySize,AllocationUnit\r\n \"2048\" , 128 \r\n";

        var result = _configReader.ReadFromString(csv);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2048, result.Value.MemorySize);
        Assert.Equal(128, result.Value.AllocationUnit);
    }

    [Fact]
    public void ConfigReader_InvalidHeader_ReturnsFailureWithLineAndColumn()
    {
        string csv = @"WrongMemory,WrongUnit
1024,64";

        var result = _configReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Single(result.Errors);
        Assert.Equal(1, result.Errors[0].LineNumber);
        Assert.Equal("Header", result.Errors[0].ColumnName);
        Assert.Contains("Header không hợp lệ", result.Errors[0].Message);
    }

    [Fact]
    public void ConfigReader_HeaderWrongOrder_ReturnsFailure()
    {
        string csv = @"AllocationUnit,MemorySize
64,1024";

        var result = _configReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Equal("Header", result.Errors[0].ColumnName);
    }

    [Fact]
    public void ConfigReader_HeaderWrongColumnCount_ReturnsFailure()
    {
        string csv = @"MemorySize,AllocationUnit,ExtraColumn
1024,64,10";

        var result = _configReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Equal("Header", result.Errors[0].ColumnName);
    }

    [Fact]
    public void ConfigReader_EmptyFile_ReturnsFailure()
    {
        var result = _configReader.ReadFromString(string.Empty);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ConfigReader_HeaderOnly_NoDataRows_ReturnsFailure()
    {
        string csv = "MemorySize,AllocationUnit\r\n";

        var result = _configReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains("không có dòng dữ liệu nào", result.ErrorSummary);
    }

    [Fact]
    public void ConfigReader_MultipleDataRows_ReturnsFailure()
    {
        string csv = @"MemorySize,AllocationUnit
1024,64
2048,128";

        var result = _configReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains("chỉ được chứa đúng 1 dòng dữ liệu", result.ErrorSummary);
    }

    [Fact]
    public void ConfigReader_NonNumericMemorySize_ReturnsFailureWithColumnName()
    {
        string csv = @"MemorySize,AllocationUnit
abc,64";

        var result = _configReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == "MemorySize" && e.Message.Contains("không phải là số nguyên hợp lệ"));
    }

    [Fact]
    public void ConfigReader_NonNumericAllocationUnit_ReturnsFailureWithColumnName()
    {
        string csv = @"MemorySize,AllocationUnit
1024,xyz";

        var result = _configReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == "AllocationUnit" && e.Message.Contains("không phải là số nguyên hợp lệ"));
    }

    [Theory]
    [InlineData("0,64", "MemorySize")]
    [InlineData("-1024,64", "MemorySize")]
    [InlineData("1024,0", "AllocationUnit")]
    [InlineData("1024,-16", "AllocationUnit")]
    public void ConfigReader_NegativeOrZeroValues_ReturnsFailure(string dataLine, string expectedErrorColumn)
    {
        string csv = $"MemorySize,AllocationUnit\r\n{dataLine}";

        var result = _configReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == expectedErrorColumn && e.Message.Contains("phải là số nguyên dương"));
    }

    [Fact]
    public void ConfigReader_MemorySizeLessThanAllocationUnit_ReturnsFailure()
    {
        string csv = @"MemorySize,AllocationUnit
50,100";

        var result = _configReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == "MemorySize" && e.Message.Contains("lớn hơn hoặc bằng"));
    }

    [Fact]
    public void ConfigReader_MemorySizeEqualsAllocationUnit_ReturnsSuccess()
    {
        string csv = @"MemorySize,AllocationUnit
64,64";

        var result = _configReader.ReadFromString(csv);

        Assert.True(result.IsSuccess);
        Assert.Equal(64, result.Value!.MemorySize);
        Assert.Equal(64, result.Value!.AllocationUnit);
    }

    [Fact]
    public void ConfigReader_MemorySizeNotDivisibleByAllocationUnit_ReturnsSuccess()
    {
        string csv = @"MemorySize,AllocationUnit
103,10";

        var result = _configReader.ReadFromString(csv);

        Assert.True(result.IsSuccess);
        Assert.Equal(103, result.Value!.MemorySize);
        Assert.Equal(10, result.Value!.AllocationUnit);
    }

    [Fact]
    public void ConfigReader_FileNotFound_ReturnsFailure()
    {
        var result = _configReader.Read("NonExistentConfig_12345.csv");

        Assert.False(result.IsSuccess);
        Assert.Contains("Không tìm thấy file", result.ErrorSummary);
    }

    #endregion

    #region Workload CSV Reader Tests

    [Fact]
    public void WorkloadReader_ValidCsv_ReturnsSuccessAndSortedOperations()
    {
        string csv = @"Sequence,Operation,ProcessId,RequestedSize
1,ALLOCATE,P1,100
2,ALLOCATE,P2,200
3,FREE,P1,
4,ALLOCATE,P3,65
5,FREE,P2,";

        var result = _workloadReader.ReadFromString(csv);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value.Count);

        Assert.Equal(1, result.Value[0].Sequence);
        Assert.Equal(MemoryOperationType.Allocate, result.Value[0].Operation);
        Assert.Equal("P1", result.Value[0].ProcessId);
        Assert.Equal(100, result.Value[0].RequestedSize);

        Assert.Equal(3, result.Value[2].Sequence);
        Assert.Equal(MemoryOperationType.Free, result.Value[2].Operation);
        Assert.Equal("P1", result.Value[2].ProcessId);
        Assert.Null(result.Value[2].RequestedSize);
    }

    [Fact]
    public void WorkloadReader_UnsortedSequences_AutomaticallySortsAscending()
    {
        string csv = @"Sequence,Operation,ProcessId,RequestedSize
10,ALLOCATE,P3,50
2,ALLOCATE,P1,100
5,FREE,P1,";

        var result = _workloadReader.ReadFromString(csv);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal(2, result.Value[0].Sequence);
        Assert.Equal(5, result.Value[1].Sequence);
        Assert.Equal(10, result.Value[2].Sequence);
    }

    [Fact]
    public void WorkloadReader_InvalidHeader_ReturnsFailure()
    {
        string csv = @"Seq,Op,Process,Size
1,ALLOCATE,P1,100";

        var result = _workloadReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Equal("Header", result.Errors[0].ColumnName);
        Assert.Contains("Header không hợp lệ", result.Errors[0].Message);
    }

    [Fact]
    public void WorkloadReader_HeaderWrongColumnCount_ReturnsFailure()
    {
        string csv = @"Sequence,Operation,ProcessId
1,ALLOCATE,P1";

        var result = _workloadReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Equal("Header", result.Errors[0].ColumnName);
    }

    [Fact]
    public void WorkloadReader_EmptyFile_ReturnsFailure()
    {
        var result = _workloadReader.ReadFromString(string.Empty);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void WorkloadReader_HeaderOnly_EmptyWorkload_ReturnsFailure()
    {
        string csv = "Sequence,Operation,ProcessId,RequestedSize\r\n";

        var result = _workloadReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains("Workload không được rỗng", result.ErrorSummary);
    }

    [Fact]
    public void WorkloadReader_DuplicateSequence_ReturnsFailureWithColumnName()
    {
        string csv = @"Sequence,Operation,ProcessId,RequestedSize
1,ALLOCATE,P1,100
1,ALLOCATE,P2,200";

        var result = _workloadReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == "Sequence" && e.Message.Contains("bị trùng lặp"));
    }

    [Fact]
    public void WorkloadReader_InvalidOperation_ReturnsFailure()
    {
        string csv = @"Sequence,Operation,ProcessId,RequestedSize
1,DEALLOCATE,P1,100";

        var result = _workloadReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == "Operation" && e.Message.Contains("không hợp lệ"));
    }

    [Theory]
    [InlineData("1,ALLOCATE,,100")]
    [InlineData("1,ALLOCATE,   ,100")]
    public void WorkloadReader_EmptyOrWhitespaceProcessId_ReturnsFailure(string line)
    {
        string csv = $"Sequence,Operation,ProcessId,RequestedSize\r\n{line}";

        var result = _workloadReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == "ProcessId" && e.Message.Contains("không được rỗng"));
    }

    [Fact]
    public void WorkloadReader_PreservesProcessIdCasing()
    {
        string csv = @"Sequence,Operation,ProcessId,RequestedSize
1,ALLOCATE,Process_XyZ_123,100";

        var result = _workloadReader.ReadFromString(csv);

        Assert.True(result.IsSuccess);
        Assert.Equal("Process_XyZ_123", result.Value![0].ProcessId);
    }

    [Theory]
    [InlineData("abc,ALLOCATE,P1,100", "không phải là số nguyên hợp lệ")]
    [InlineData("0,ALLOCATE,P1,100", "phải là số nguyên dương")]
    [InlineData("-5,ALLOCATE,P1,100", "phải là số nguyên dương")]
    public void WorkloadReader_InvalidSequence_ReturnsFailure(string line, string expectedMsg)
    {
        string csv = $"Sequence,Operation,ProcessId,RequestedSize\r\n{line}";

        var result = _workloadReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == "Sequence" && e.Message.Contains(expectedMsg));
    }

    [Fact]
    public void WorkloadReader_AllocateMissingRequestedSize_ReturnsFailure()
    {
        string csv = @"Sequence,Operation,ProcessId,RequestedSize
1,ALLOCATE,P1,";

        var result = _workloadReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == "RequestedSize" && e.Message.Contains("bắt buộc"));
    }

    [Theory]
    [InlineData("1,ALLOCATE,P1,abc", "không phải là số nguyên hợp lệ")]
    [InlineData("1,ALLOCATE,P1,0", "phải là số nguyên dương")]
    [InlineData("1,ALLOCATE,P1,-20", "phải là số nguyên dương")]
    public void WorkloadReader_AllocateInvalidRequestedSize_ReturnsFailure(string line, string expectedMsg)
    {
        string csv = $"Sequence,Operation,ProcessId,RequestedSize\r\n{line}";

        var result = _workloadReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == "RequestedSize" && e.Message.Contains(expectedMsg));
    }

    [Fact]
    public void WorkloadReader_FreeWithEmptyRequestedSize_ReturnsSuccessWithNullSize()
    {
        string csv = @"Sequence,Operation,ProcessId,RequestedSize
1,FREE,P1,";

        var result = _workloadReader.ReadFromString(csv);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value![0].RequestedSize);
    }

    [Fact]
    public void WorkloadReader_FreeWithIntegerRequestedSize_ReturnsSuccessWithNullSize()
    {
        string csv = @"Sequence,Operation,ProcessId,RequestedSize
1,FREE,P1,100";

        var result = _workloadReader.ReadFromString(csv);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value![0].RequestedSize);
    }

    [Fact]
    public void WorkloadReader_FreeWithInvalidRequestedSize_ReturnsFailure()
    {
        string csv = @"Sequence,Operation,ProcessId,RequestedSize
1,FREE,P1,invalid_string";

        var result = _workloadReader.ReadFromString(csv);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.ColumnName == "RequestedSize" && e.Message.Contains("phải để trống hoặc là số nguyên hợp lệ"));
    }

    [Fact]
    public void WorkloadReader_SkipsBlankLinesSuccessfully()
    {
        string csv = "Sequence,Operation,ProcessId,RequestedSize\r\n\r\n1,ALLOCATE,P1,100\r\n   \r\n2,FREE,P1,\r\n";

        var result = _workloadReader.ReadFromString(csv);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public void WorkloadReader_FileNotFound_ReturnsFailure()
    {
        var result = _workloadReader.Read("NonExistentWorkload_9999.csv");

        Assert.False(result.IsSuccess);
        Assert.Contains("Không tìm thấy file", result.ErrorSummary);
    }

    #endregion

    #region CSV Exporter Tests

    [Fact]
    public void CsvExporter_TraceHeader_MatchesExactSpecification()
    {
        const string expected = "Sequence,Operation,ProcessId,RequestedSize,AllocatedSize,Status,Reason,TotalFreeMemory,LargestFreeBlock,InternalFragmentation";

        Assert.Equal(expected, CsvExporter.TraceHeader);

        string[] cols = CsvExporter.TraceHeader.Split(',');
        Assert.Equal(10, cols.Length);
    }

    [Fact]
    public void CsvExporter_ComparisonHeader_MatchesExactSpecification()
    {
        string expected = "WorkloadName,Algorithm,OperationCount,MemorySize,AllocationUnit,TotalAllocationRequests,SuccessfulAllocations,FailedAllocations,SuccessRate,UsedMemory,FreeMemory,FreeBlockCount,LargestFreeBlock,TotalInternalFragmentation,RuntimeRun1Ms,RuntimeRun2Ms,RuntimeRun3Ms,RuntimeRun4Ms,RuntimeRun5Ms,AverageRuntimeMs";

        Assert.Equal(CsvExporter.ComparisonHeader, expected);

        string[] cols = CsvExporter.ComparisonHeader.Split(',');
        Assert.Equal(20, cols.Length);

        // Kiểm tra đúng 5 cột runtime và 1 cột trung bình
        Assert.Equal("RuntimeRun1Ms", cols[14]);
        Assert.Equal("RuntimeRun2Ms", cols[15]);
        Assert.Equal("RuntimeRun3Ms", cols[16]);
        Assert.Equal("RuntimeRun4Ms", cols[17]);
        Assert.Equal("RuntimeRun5Ms", cols[18]);
        Assert.Equal("AverageRuntimeMs", cols[19]);
    }

    [Fact]
    public void CsvExporter_ExportTraceToCsvString_FormatsCorrectly()
    {
        var entries = new List<TraceEntry>
        {
            new(1, MemoryOperationType.Allocate, "P1", 100, 128, OperationStatus.Success, "SUCCESS", 896, 896, 28),
            new(2, MemoryOperationType.Allocate, "P2", 2000, null, OperationStatus.Failed, "INSUFFICIENT_MEMORY", 896, 896, 28),
            new(3, MemoryOperationType.Free, "P1", null, 128, OperationStatus.Success, "SUCCESS", 1024, 1024, 0)
        };

        string csv = CsvExporter.ExportTraceToCsvString(entries);
        string[] lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(4, lines.Length);
        Assert.Equal(CsvExporter.TraceHeader, lines[0]);

        // Dòng 1: Allocate thành công
        Assert.Equal("1,ALLOCATE,P1,100,128,SUCCESS,SUCCESS,896,896,28", lines[1]);

        // Dòng 2: Allocate thất bại -> AllocatedSize rỗng
        Assert.Equal("2,ALLOCATE,P2,2000,,FAILED,INSUFFICIENT_MEMORY,896,896,28", lines[2]);

        // Dòng 3: Free thành công -> RequestedSize rỗng
        Assert.Equal("3,FREE,P1,,128,SUCCESS,SUCCESS,1024,1024,0", lines[3]);
    }

    [Fact]
    public void CsvExporter_ExportComparisonToCsvString_FormatsCorrectly_WithSuccessRate()
    {
        var records = new List<ComparisonRecord>
        {
            new("WorkloadA", "FirstFit", 10, 1024, 64, 8, 7, 1, 87.5, 512, 512, 2, 256, 32, 1.2, 1.1, 1.3, 1.0, 1.4, 1.2),
            new("WorkloadA", "BestFit", 10, 1024, 64, 8, 8, 0, 100.0, 512, 512, 1, 512, 16, 1.5, 1.4, 1.6, 1.5, 1.5, 1.5),
            new("WorkloadA", "WorstFit", 10, 1024, 64, 8, 6, 2, 75.0, 400, 624, 3, 200, 48, 1.1, 1.0, 1.2, 1.1, 1.1, 1.1)
        };

        string csv = CsvExporter.ExportComparisonToCsvString(records);
        string[] lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(4, lines.Length);
        Assert.Equal(CsvExporter.ComparisonHeader, lines[0]);

        // FirstFit row
        string[] firstCols = lines[1].Split(',');
        Assert.Equal(20, firstCols.Length);
        Assert.Equal("WorkloadA", firstCols[0]);
        Assert.Equal("FirstFit", firstCols[1]);
        Assert.Equal("87.5", firstCols[8]); // SuccessRate không kèm %
        Assert.Equal("1.2", firstCols[14]); // RuntimeRun1Ms
        Assert.Equal("1.2", firstCols[19]); // AverageRuntimeMs
    }

    [Fact]
    public void CsvExporter_ExportComparisonToCsvString_EmptySuccessRate_WhenNoAllocations()
    {
        var records = new List<ComparisonRecord>
        {
            new("EmptyAllocWorkload", "FirstFit", 2, 1024, 64, 0, 0, 0, null, 0, 1024, 1, 1024, 0, 0.5, 0.5, 0.5, 0.5, 0.5)
        };

        string csv = CsvExporter.ExportComparisonToCsvString(records);
        string[] lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        string[] cols = lines[1].Split(',');
        Assert.Equal(20, cols.Length);
        Assert.Equal(string.Empty, cols[8]); // Cột SuccessRate rỗng
    }

    [Fact]
    public void CsvExporter_GenerateTraceFileName_FollowsNamingConvention()
    {
        var time = new DateTime(2026, 11, 6, 21, 35, 12);
        string fileName = CsvExporter.GenerateTraceFileName("FirstFit", "Workload1", time);

        Assert.Equal("Trace_FirstFit_Workload1_20261106_213512.csv", fileName);
    }

    [Fact]
    public void CsvExporter_GenerateComparisonFileName_FollowsNamingConvention()
    {
        var time = new DateTime(2026, 11, 6, 21, 35, 12);
        string fileName = CsvExporter.GenerateComparisonFileName("Workload1", time);

        Assert.Equal("Comparison_Workload1_20261106_213512.csv", fileName);
    }

    [Fact]
    public void CsvExporter_FileNameSanitization_RemovesInvalidCharactersAndCsvExtension()
    {
        var time = new DateTime(2026, 11, 6, 21, 0, 0);

        string fileName = CsvExporter.GenerateTraceFileName("Best Fit", "test:workload?.csv", time);
        Assert.Equal("Trace_Best_Fit_test_workload_20261106_210000.csv", fileName);

        string compFileName = CsvExporter.GenerateComparisonFileName("", time);
        Assert.Equal("Comparison_Workload_20261106_210000.csv", compFileName);
    }

    [Fact]
    public void CsvExporter_ExportToFile_SuccessfullyWritesAndReadsBack()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "CsvExporterTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            var exporter = new CsvExporter();
            var entries = new List<TraceEntry>
            {
                new(1, MemoryOperationType.Allocate, "P1", 100, 128, OperationStatus.Success, "SUCCESS", 896, 896, 28)
            };

            var request = new TraceExportRequest(
                OutputDirectory: tempDir,
                Algorithm: "FirstFit",
                WorkloadName: "TestFile",
                Trace: entries,
                Timestamp: new DateTime(2026, 11, 6, 12, 0, 0));

            var result = exporter.Export(request);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.FilePath);
            Assert.True(File.Exists(result.FilePath));

            string content = File.ReadAllText(result.FilePath);
            Assert.Contains(CsvExporter.TraceHeader, content);
            Assert.Contains("1,ALLOCATE,P1,100,128,SUCCESS,SUCCESS,896,896,28", content);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion
}
