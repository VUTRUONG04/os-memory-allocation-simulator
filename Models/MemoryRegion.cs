namespace OSMemoryAllocationSimulator.Models;

/// <summary>
/// Biểu diễn một vùng nhớ liên tục trong bộ nhớ.
/// - StartAddress: địa chỉ bắt đầu (từ 0).
/// - Size: kích thước vùng (luôn > 0).
/// - IsFree: true nếu là vùng trống, false nếu đã cấp phát.
/// - ProcessId: PID của tiến trình đang chiếm (null nếu IsFree).
/// - RequestedSize: kích thước yêu cầu ban đầu (null nếu IsFree).
/// </summary>
public sealed record MemoryRegion(
    int StartAddress,
    int Size,
    bool IsFree,
    string? ProcessId = null,
    int? RequestedSize = null);
