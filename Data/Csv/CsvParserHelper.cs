using System.IO;
using System.Text;

namespace OSMemoryAllocationSimulator.Data.Csv;

/// <summary>
/// Đại diện cho một hàng trong file CSV kèm số dòng thực tế trong file gốc.
/// </summary>
public sealed class CsvRow
{
    public int LineNumber { get; }
    public IReadOnlyList<string> Fields { get; }

    public CsvRow(int lineNumber, IReadOnlyList<string> fields)
    {
        LineNumber = lineNumber;
        Fields = fields;
    }
}

/// <summary>
/// Tiện ích hỗ trợ phân tích và định dạng dữ liệu CSV theo chuẩn RFC 4180.
/// </summary>
public static class CsvParserHelper
{
    /// <summary>
    /// Phân tích nội dung CSV từ một TextReader thành danh sách các dòng dữ liệu.
    /// Tự động bỏ qua các dòng hoàn toàn trống và trim khoảng trắng ở các ô.
    /// </summary>
    public static List<CsvRow> ParseRows(TextReader reader)
    {
        var rows = new List<CsvRow>();
        var currentFields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;
        bool hadQuotesInLine = false;
        int currentLineNumber = 1;
        int rowStartLineNumber = 1;

        int nextChar;
        while ((nextChar = reader.Read()) != -1)
        {
            char c = (char)nextChar;

            if (c == '"')
            {
                hadQuotesInLine = true;
                if (inQuotes)
                {
                    int peek = reader.Peek();
                    if (peek == '"')
                    {
                        reader.Read(); // Bỏ qua ký tự " thoát
                        currentField.Append('"');
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    inQuotes = true;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                currentFields.Add(currentField.ToString().Trim());
                currentField.Clear();
            }
            else if ((c == '\r' || c == '\n') && !inQuotes)
            {
                if (c == '\r' && reader.Peek() == '\n')
                {
                    reader.Read();
                }

                currentFields.Add(currentField.ToString().Trim());
                currentField.Clear();

                // Dòng được xem là hoàn toàn trống nếu không có dấu phẩy, nội dung rỗng và không có ngoặc kép
                bool isBlankLine = currentFields.Count <= 1 &&
                    string.IsNullOrWhiteSpace(currentFields.FirstOrDefault()) &&
                    !hadQuotesInLine;

                if (!isBlankLine)
                {
                    rows.Add(new CsvRow(rowStartLineNumber, currentFields.ToList()));
                }

                currentFields.Clear();
                hadQuotesInLine = false;
                currentLineNumber++;
                rowStartLineNumber = currentLineNumber;
            }
            else
            {
                currentField.Append(c);

                if (c == '\n')
                {
                    currentLineNumber++;
                }
            }
        }

        if (inQuotes)
        {
            throw new FormatException($"Dấu ngoặc kép chưa được đóng tại dòng {rowStartLineNumber}.");
        }

        // Xử lý dòng cuối cùng nếu không có ký tự xuống dòng ở cuối file
        if (currentField.Length > 0 || currentFields.Count > 0 || hadQuotesInLine)
        {
            currentFields.Add(currentField.ToString().Trim());
            bool isBlankLine = currentFields.Count <= 1 &&
                string.IsNullOrWhiteSpace(currentFields.FirstOrDefault()) &&
                !hadQuotesInLine;

            if (!isBlankLine)
            {
                rows.Add(new CsvRow(rowStartLineNumber, currentFields.ToList()));
            }
        }

        return rows;
    }

    /// <summary>
    /// Escape một trường trong CSV theo chuẩn RFC 4180.
    /// Bao bọc trong dấu nháy kép nếu chứa dấu phẩy, dấu nháy kép hoặc xuống dòng.
    /// </summary>
    public static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        bool needsQuotes = value.Contains(',') ||
                           value.Contains('"') ||
                           value.Contains('\r') ||
                           value.Contains('\n');

        if (needsQuotes)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Làm sạch tên file hoặc tên workload bằng cách loại bỏ các ký tự không hợp lệ.
    /// </summary>
    public static string SanitizeFileName(string name, string fallback = "Workload")
    {
        if (string.IsNullOrWhiteSpace(name))
            return fallback;

        string sanitized = name.Trim();

        // Nếu người dùng truyền vào cả đuôi file .csv, lược bỏ đi
        if (sanitized.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = Path.GetFileNameWithoutExtension(sanitized);
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(sanitized.Length);
        foreach (char c in sanitized)
        {
            if (invalidChars.Contains(c) || c == ' ')
                sb.Append('_');
            else
                sb.Append(c);
        }

        string result = sb.ToString().Trim('_');
        return string.IsNullOrEmpty(result) ? fallback : result;
    }
}
