namespace OSMemoryAllocationSimulator.Models;

/// <summary>
/// Cấu hình cho một lượt mô phỏng.
/// MemorySize và AllocationUnit đều tính bằng KB, phải là số nguyên dương.
/// MemorySize >= AllocationUnit (quyết định nhóm).
/// MemorySize không cần chia hết cho AllocationUnit.
/// </summary>
public sealed record SimulationConfig(
    int MemorySize,
    int AllocationUnit);
