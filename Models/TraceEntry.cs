namespace OSMemoryAllocationSimulator.Models;

/// <summary>
/// Trạng thái thao tác: thành công hoặc thất bại.
/// Xuất ra CSV dưới dạng SUCCESS / FAILED.
/// </summary>
public enum OperationStatus
{
    Success,
    Failed
}

/// <summary>
/// Một mục trong Trace — ghi lại kết quả xử lý một thao tác workload.
/// Mỗi thao tác (kể cả thất bại) tạo đúng một TraceEntry.
/// Các chỉ số (TotalFreeMemory, LargestFreeBlock, InternalFragmentation)
/// được đo SAU thao tác.
/// </summary>
public sealed record TraceEntry(
    int Sequence,
    MemoryOperationType Operation,
    string ProcessId,
    int? RequestedSize,
    int? AllocatedSize,
    OperationStatus Status,
    string Reason,
    int TotalFreeMemory,
    int LargestFreeBlock,
    int InternalFragmentation);
