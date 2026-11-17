using System.IO;
using Microsoft.VisualBasic.FileIO;
using OSMemoryAllocationSimulator.Models;
using System.Globalization;


namespace OSMemoryAllocationSimulator.Services;

public sealed class WorkloadCsvService
{
    public IReadOnlyList<WorkloadOperation> Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Đường dẫn CSV không hợp lệ.");

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Không tìm thấy file CSV.", filePath);

        using var parser = new TextFieldParser(filePath);

        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = true;

        // Đọc header
        if (parser.EndOfData)
            throw new FormatException("File CSV đang trống.");

        var headers = parser.ReadFields();

        if (headers == null)
            throw new FormatException("Không đọc được header CSV.");

        var requiredHeaders = new[]
        {
            "Sequence",
            "Operation",
            "ProcessId",
            "RequestedSize"
        };

        var indexes = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headers.Length; i++)
        {
            indexes[headers[i]] = i;
        }

        foreach (var header in requiredHeaders)
        {
            if (!indexes.ContainsKey(header))
                throw new FormatException(
                    $"CSV thiếu cột bắt buộc: {header}");
        }

        var operations = new List<WorkloadOperation>();
        var sequences = new HashSet<int>();

        int lineNumber = 1;

        while (!parser.EndOfData)
        {
            lineNumber++;

            var fields = parser.ReadFields();

            if (fields == null || fields.All(string.IsNullOrWhiteSpace))
                continue;

            try
            {
                if (fields.Length != headers.Length)
                    throw new FormatException(
                        "Số lượng cột không khớp với header.");

                int sequence = int.Parse(
                    fields[indexes["Sequence"]],
                    CultureInfo.InvariantCulture);

                if (sequence <= 0)
                    throw new FormatException(
                        "Sequence phải lớn hơn 0.");

                if (!sequences.Add(sequence))
                    throw new FormatException(
                        $"Sequence {sequence} bị trùng.");

                string operationText =
                    fields[indexes["Operation"]].Trim();

                string processId =
                    fields[indexes["ProcessId"]].Trim();

                string requestedText =
                    fields[indexes["RequestedSize"]].Trim();

                if (string.IsNullOrWhiteSpace(processId))
                    throw new FormatException(
                        "ProcessId không được để trống.");

                MemoryOperationType operationType;

                if (operationText.Equals(
                    "ALLOCATE",
                    StringComparison.OrdinalIgnoreCase))
                {
                    operationType = MemoryOperationType.Allocate;
                }
                else if (operationText.Equals(
                    "FREE",
                    StringComparison.OrdinalIgnoreCase))
                {
                    operationType = MemoryOperationType.Free;
                }
                else
                {
                    throw new FormatException(
                        "Operation chỉ chấp nhận ALLOCATE hoặc FREE.");
                }

                int? requestedSize = null;

                if (operationType == MemoryOperationType.Allocate)
                {
                    if (!int.TryParse(
                        requestedText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int size) || size <= 0)
                    {
                        throw new FormatException(
                            "RequestedSize của ALLOCATE phải là số nguyên dương.");
                    }

                    requestedSize = size;
                }
                else if (!string.IsNullOrWhiteSpace(requestedText))
                {
                    throw new FormatException(
                        "RequestedSize của FREE phải để trống.");
                }

                operations.Add(new WorkloadOperation(
                    sequence,
                    operationType,
                    processId,
                    requestedSize));
            }
            catch (Exception ex) when (
                ex is FormatException ||
                ex is OverflowException)
            {
                throw new FormatException(
                    $"Lỗi tại dòng {lineNumber}: {ex.Message}", ex);
            }
        }

        if (operations.Count == 0)
            throw new FormatException(
                "Workload phải có ít nhất một thao tác.");

        return operations
            .OrderBy(operation => operation.Sequence)
            .ToList();
    }
}
