using System.Globalization;
using System.IO;
using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Data.Csv;

/// <summary>
/// Lớp đọc và kiểm tra tính hợp lệ của file kịch bản bộ nhớ CSV (Workload.csv).
/// Schema yêu cầu:
/// Header: Sequence,Operation,ProcessId,RequestedSize
/// - Sequence: Số nguyên dương, duy nhất.
/// - Operation: Chỉ chấp nhận ALLOCATE hoặc FREE.
/// - ProcessId: Không rỗng hoặc khoảng trắng sau khi trim, giữ nguyên chữ hoa/thường.
/// - RequestedSize: Số nguyên dương với ALLOCATE; để trống/null với FREE.
/// - File phải có ít nhất một dòng dữ liệu (không chấp nhận file chỉ có header).
/// - Danh sách thao tác trả về được sắp xếp theo Sequence tăng dần.
/// </summary>
public class CsvWorkloadReader
{
    public const string ExpectedHeader = "Sequence,Operation,ProcessId,RequestedSize";
    public const string ColumnSequence = "Sequence";
    public const string ColumnOperation = "Operation";
    public const string ColumnProcessId = "ProcessId";
    public const string ColumnRequestedSize = "RequestedSize";

    /// <summary>
    /// Đọc và xác thực toàn bộ file workload từ đường dẫn tệp tin.
    /// </summary>
    public CsvReadResult<IReadOnlyList<WorkloadOperation>> Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return CsvReadResult<IReadOnlyList<WorkloadOperation>>.Failure(0, null, "Đường dẫn file workload không được để trống.");
        }

        if (!File.Exists(filePath))
        {
            return CsvReadResult<IReadOnlyList<WorkloadOperation>>.Failure(0, null, $"Không tìm thấy file workload tại: {filePath}");
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);
            return Read(reader);
        }
        catch (Exception ex)
        {
            return CsvReadResult<IReadOnlyList<WorkloadOperation>>.Failure(0, null, $"Lỗi I/O khi đọc file workload: {ex.Message}");
        }
    }

    /// <summary>
    /// Đọc và xác thực workload từ chuỗi văn bản CSV.
    /// </summary>
    public CsvReadResult<IReadOnlyList<WorkloadOperation>> ReadFromString(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return CsvReadResult<IReadOnlyList<WorkloadOperation>>.Failure(1, null, "File workload rỗng.");
        }

        using var reader = new StringReader(csvContent);
        return Read(reader);
    }

    /// <summary>
    /// Đọc và xác thực workload từ TextReader.
    /// </summary>
    public CsvReadResult<IReadOnlyList<WorkloadOperation>> Read(TextReader reader)
    {
        List<CsvRow> rows;
        try
        {
            rows = CsvParserHelper.ParseRows(reader);
        }
        catch (FormatException fe)
        {
            return CsvReadResult<IReadOnlyList<WorkloadOperation>>.Failure(0, null, $"Lỗi cú pháp CSV: {fe.Message}");
        }

        if (rows.Count == 0)
        {
            return CsvReadResult<IReadOnlyList<WorkloadOperation>>.Failure(1, null, "File workload rỗng, không có dữ liệu.");
        }

        // 1. Kiểm tra Header
        var headerRow = rows[0];
        if (headerRow.Fields.Count != 4 ||
            !string.Equals(headerRow.Fields[0], ColumnSequence, StringComparison.Ordinal) ||
            !string.Equals(headerRow.Fields[1], ColumnOperation, StringComparison.Ordinal) ||
            !string.Equals(headerRow.Fields[2], ColumnProcessId, StringComparison.Ordinal) ||
            !string.Equals(headerRow.Fields[3], ColumnRequestedSize, StringComparison.Ordinal))
        {
            string foundHeader = string.Join(",", headerRow.Fields);
            return CsvReadResult<IReadOnlyList<WorkloadOperation>>.Failure(
                headerRow.LineNumber,
                "Header",
                $"Header không hợp lệ. Kỳ vọng '{ExpectedHeader}' nhưng nhận được '{foundHeader}'.");
        }

        // 2. Kiểm tra workload rỗng (chỉ có header)
        var dataRows = rows.Skip(1).ToList();
        if (dataRows.Count == 0)
        {
            return CsvReadResult<IReadOnlyList<WorkloadOperation>>.Failure(
                headerRow.LineNumber + 1,
                null,
                "File workload chỉ chứa header mà không có dòng dữ liệu nào. Workload không được rỗng.");
        }

        // 3. Phân tích và validate toàn bộ các dòng dữ liệu
        var errors = new List<CsvValidationError>();
        var seenSequences = new HashSet<int>();
        var operations = new List<WorkloadOperation>();

        foreach (var row in dataRows)
        {
            if (row.Fields.Count != 4)
            {
                errors.Add(new CsvValidationError(
                    row.LineNumber,
                    null,
                    $"Số lượng cột không hợp lệ. Kỳ vọng 4 cột, nhận được {row.Fields.Count} cột."));
                continue;
            }

            bool isRowValid = true;

            // 3.1. Validate Sequence
            string seqStr = row.Fields[0];
            int sequence = 0;
            if (string.IsNullOrWhiteSpace(seqStr))
            {
                errors.Add(new CsvValidationError(row.LineNumber, ColumnSequence, "Sequence không được để trống."));
                isRowValid = false;
            }
            else if (!int.TryParse(seqStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out sequence))
            {
                errors.Add(new CsvValidationError(row.LineNumber, ColumnSequence, $"Sequence '{seqStr}' không phải là số nguyên hợp lệ."));
                isRowValid = false;
            }
            else if (sequence <= 0)
            {
                errors.Add(new CsvValidationError(row.LineNumber, ColumnSequence, $"Sequence phải là số nguyên dương (> 0), nhận được: {sequence}."));
                isRowValid = false;
            }
            else if (!seenSequences.Add(sequence))
            {
                errors.Add(new CsvValidationError(row.LineNumber, ColumnSequence, $"Sequence '{sequence}' bị trùng lặp. Mỗi Sequence trong file phải là duy nhất."));
                isRowValid = false;
            }

            // 3.2. Validate Operation
            string opStr = row.Fields[1];
            MemoryOperationType operationType = default;
            bool isOpTypeValid = false;

            if (string.Equals(opStr, "ALLOCATE", StringComparison.Ordinal))
            {
                operationType = MemoryOperationType.Allocate;
                isOpTypeValid = true;
            }
            else if (string.Equals(opStr, "FREE", StringComparison.Ordinal))
            {
                operationType = MemoryOperationType.Free;
                isOpTypeValid = true;
            }
            else
            {
                errors.Add(new CsvValidationError(row.LineNumber, ColumnOperation, $"Operation '{opStr}' không hợp lệ. Chỉ chấp nhận 'ALLOCATE' hoặc 'FREE'."));
                isRowValid = false;
            }

            // 3.3. Validate ProcessId (phân biệt hoa thường, không rỗng sau trim)
            string processId = row.Fields[2];
            if (string.IsNullOrEmpty(processId))
            {
                errors.Add(new CsvValidationError(row.LineNumber, ColumnProcessId, "ProcessId không được rỗng hoặc chỉ chứa khoảng trắng."));
                isRowValid = false;
            }

            // 3.4. Validate RequestedSize
            string sizeStr = row.Fields[3];
            int? requestedSize = null;

            if (isOpTypeValid)
            {
                if (operationType == MemoryOperationType.Allocate)
                {
                    if (string.IsNullOrWhiteSpace(sizeStr))
                    {
                        errors.Add(new CsvValidationError(row.LineNumber, ColumnRequestedSize, "RequestedSize là bắt buộc đối với thao tác ALLOCATE."));
                        isRowValid = false;
                    }
                    else if (!int.TryParse(sizeStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int size))
                    {
                        errors.Add(new CsvValidationError(row.LineNumber, ColumnRequestedSize, $"RequestedSize '{sizeStr}' không phải là số nguyên hợp lệ đối với ALLOCATE."));
                        isRowValid = false;
                    }
                    else if (size <= 0)
                    {
                        errors.Add(new CsvValidationError(row.LineNumber, ColumnRequestedSize, $"RequestedSize phải là số nguyên dương (> 0) đối với ALLOCATE, nhận được: {size}."));
                        isRowValid = false;
                    }
                    else
                    {
                        requestedSize = size;
                    }
                }
                else // MemoryOperationType.Free
                {
                    if (string.IsNullOrWhiteSpace(sizeStr))
                    {
                        requestedSize = null;
                    }
                    else if (!int.TryParse(sizeStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    {
                        // Cột số không được chứa chữ cái theo schema
                        errors.Add(new CsvValidationError(row.LineNumber, ColumnRequestedSize, $"RequestedSize cho thao tác FREE phải để trống hoặc là số nguyên hợp lệ, nhận được '{sizeStr}'."));
                        isRowValid = false;
                    }
                    else
                    {
                        // Theo hợp đồng schema 3.2: Với FREE, RequestedSize không được dùng, lưu trữ null
                        requestedSize = null;
                    }
                }
            }

            if (isRowValid)
            {
                operations.Add(new WorkloadOperation(sequence, operationType, processId, requestedSize));
            }
        }

        // Nếu có bất kỳ lỗi nào, trả về danh sách lỗi đầy đủ, không trả về dữ liệu partial
        if (errors.Count > 0)
        {
            return CsvReadResult<IReadOnlyList<WorkloadOperation>>.Failure(errors);
        }

        // Tự động sắp xếp danh sách thao tác theo Sequence tăng dần trước khi trả về
        var sortedOperations = operations.OrderBy(op => op.Sequence).ToList();

        return CsvReadResult<IReadOnlyList<WorkloadOperation>>.Success(sortedOperations);
    }
}
