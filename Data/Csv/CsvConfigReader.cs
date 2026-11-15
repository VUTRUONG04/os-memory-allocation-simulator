using System.Globalization;
using System.IO;
using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Data.Csv;

/// <summary>
/// Lớp đọc và kiểm tra tính hợp lệ của file cấu hình CSV (Config.csv).
/// Schema yêu cầu:
/// Header: MemorySize,AllocationUnit
/// - MemorySize > 0, AllocationUnit > 0
/// - MemorySize >= AllocationUnit
/// - File phải chứa đúng một dòng dữ liệu.
/// </summary>
public class CsvConfigReader
{
    public const string ExpectedHeader = "MemorySize,AllocationUnit";
    public const string ColumnMemorySize = "MemorySize";
    public const string ColumnAllocationUnit = "AllocationUnit";

    /// <summary>
    /// Đọc và xác thực file cấu hình từ đường dẫn tệp tin.
    /// </summary>
    public CsvReadResult<SimulationConfig> Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return CsvReadResult<SimulationConfig>.Failure(0, null, "Đường dẫn file cấu hình không được để trống.");
        }

        if (!File.Exists(filePath))
        {
            return CsvReadResult<SimulationConfig>.Failure(0, null, $"Không tìm thấy file cấu hình tại: {filePath}");
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);
            return Read(reader);
        }
        catch (Exception ex)
        {
            return CsvReadResult<SimulationConfig>.Failure(0, null, $"Lỗi I/O khi đọc file cấu hình: {ex.Message}");
        }
    }

    /// <summary>
    /// Đọc và xác thực cấu hình từ một chuỗi văn bản CSV.
    /// </summary>
    public CsvReadResult<SimulationConfig> ReadFromString(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return CsvReadResult<SimulationConfig>.Failure(1, null, "File cấu hình rỗng.");
        }

        using var reader = new StringReader(csvContent);
        return Read(reader);
    }

    /// <summary>
    /// Đọc và xác thực cấu hình từ TextReader.
    /// </summary>
    public CsvReadResult<SimulationConfig> Read(TextReader reader)
    {
        List<CsvRow> rows;
        try
        {
            rows = CsvParserHelper.ParseRows(reader);
        }
        catch (FormatException fe)
        {
            return CsvReadResult<SimulationConfig>.Failure(0, null, $"Lỗi cú pháp CSV: {fe.Message}");
        }

        if (rows.Count == 0)
        {
            return CsvReadResult<SimulationConfig>.Failure(1, null, "File cấu hình rỗng, không có dữ liệu.");
        }

        // 1. Kiểm tra Header
        var headerRow = rows[0];
        if (headerRow.Fields.Count != 2 ||
            !string.Equals(headerRow.Fields[0], ColumnMemorySize, StringComparison.Ordinal) ||
            !string.Equals(headerRow.Fields[1], ColumnAllocationUnit, StringComparison.Ordinal))
        {
            string foundHeader = string.Join(",", headerRow.Fields);
            return CsvReadResult<SimulationConfig>.Failure(
                headerRow.LineNumber,
                "Header",
                $"Header không hợp lệ. Kỳ vọng '{ExpectedHeader}' nhưng nhận được '{foundHeader}'.");
        }

        // 2. Kiểm tra số lượng dòng dữ liệu
        var dataRows = rows.Skip(1).ToList();
        if (dataRows.Count == 0)
        {
            return CsvReadResult<SimulationConfig>.Failure(
                headerRow.LineNumber + 1,
                null,
                "File cấu hình chỉ có header mà không có dòng dữ liệu nào.");
        }

        if (dataRows.Count > 1)
        {
            return CsvReadResult<SimulationConfig>.Failure(
                dataRows[1].LineNumber,
                null,
                $"File cấu hình chỉ được chứa đúng 1 dòng dữ liệu, nhưng phát hiện {dataRows.Count} dòng.");
        }

        // 3. Validate dòng dữ liệu duy nhất
        var dataRow = dataRows[0];
        var errors = new List<CsvValidationError>();

        if (dataRow.Fields.Count != 2)
        {
            errors.Add(new CsvValidationError(
                dataRow.LineNumber,
                null,
                $"Số lượng cột không hợp lệ. Kỳ vọng 2 cột, nhận được {dataRow.Fields.Count} cột."));
            return CsvReadResult<SimulationConfig>.Failure(errors);
        }

        bool isMemorySizeParsed = false;
        int memorySize = 0;
        string memStr = dataRow.Fields[0];

        if (string.IsNullOrWhiteSpace(memStr))
        {
            errors.Add(new CsvValidationError(
                dataRow.LineNumber,
                ColumnMemorySize,
                "Giá trị MemorySize không được để trống."));
        }
        else if (!int.TryParse(memStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out memorySize))
        {
            errors.Add(new CsvValidationError(
                dataRow.LineNumber,
                ColumnMemorySize,
                $"Giá trị MemorySize '{memStr}' không phải là số nguyên hợp lệ."));
        }
        else if (memorySize <= 0)
        {
            errors.Add(new CsvValidationError(
                dataRow.LineNumber,
                ColumnMemorySize,
                $"MemorySize phải là số nguyên dương (> 0), nhận được: {memorySize}."));
        }
        else
        {
            isMemorySizeParsed = true;
        }

        bool isAllocationUnitParsed = false;
        int allocationUnit = 0;
        string allocStr = dataRow.Fields[1];

        if (string.IsNullOrWhiteSpace(allocStr))
        {
            errors.Add(new CsvValidationError(
                dataRow.LineNumber,
                ColumnAllocationUnit,
                "Giá trị AllocationUnit không được để trống."));
        }
        else if (!int.TryParse(allocStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out allocationUnit))
        {
            errors.Add(new CsvValidationError(
                dataRow.LineNumber,
                ColumnAllocationUnit,
                $"Giá trị AllocationUnit '{allocStr}' không phải là số nguyên hợp lệ."));
        }
        else if (allocationUnit <= 0)
        {
            errors.Add(new CsvValidationError(
                dataRow.LineNumber,
                ColumnAllocationUnit,
                $"AllocationUnit phải là số nguyên dương (> 0), nhận được: {allocationUnit}."));
        }
        else
        {
            isAllocationUnitParsed = true;
        }

        // Kiểm tra quy tắc quan hệ giữa MemorySize và AllocationUnit
        if (isMemorySizeParsed && isAllocationUnitParsed)
        {
            if (memorySize < allocationUnit)
            {
                errors.Add(new CsvValidationError(
                    dataRow.LineNumber,
                    ColumnMemorySize,
                    $"MemorySize ({memorySize}) phải lớn hơn hoặc bằng AllocationUnit ({allocationUnit})."));
            }
        }

        if (errors.Count > 0)
        {
            return CsvReadResult<SimulationConfig>.Failure(errors);
        }

        return CsvReadResult<SimulationConfig>.Success(new SimulationConfig(memorySize, allocationUnit));
    }
}
