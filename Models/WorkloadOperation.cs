namespace OSMemoryAllocationSimulator.Models;

/// <summary>
/// Loại thao tác bộ nhớ trong workload.
/// </summary>
public enum MemoryOperationType
{
    Allocate,
    Free
}

/// <summary>
/// Một thao tác trong workload.
/// - Sequence: số nguyên dương, duy nhất trong workload.
/// - ProcessId: trim hai đầu, phân biệt hoa/thường, không rỗng.
/// - RequestedSize: số nguyên dương với ALLOCATE; null/trống với FREE.
/// </summary>
public sealed record WorkloadOperation(
    int Sequence,
    MemoryOperationType Operation,
    string ProcessId,
    int? RequestedSize);
