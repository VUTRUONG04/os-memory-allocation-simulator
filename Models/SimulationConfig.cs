namespace OSMemoryAllocationSimulator.Models;

/// <summary>
/// Cấu hình cho một lượt mô phỏng.
/// MemorySize và AllocationUnit đều tính bằng KB, phải là số nguyên dương.
/// MemorySize >= AllocationUnit (quyết định nhóm).
/// MemorySize không cần chia hết cho AllocationUnit.
/// </summary>
public sealed record SimulationConfig
{
    public int MemorySize { get; init; }
    public int AllocationUnit { get; init; }

    public SimulationConfig(int MemorySize, int AllocationUnit)
    {
        if (MemorySize <= 0 || AllocationUnit <= 0)
        {
            throw new ArgumentException("MemorySize và AllocationUnit phải là số nguyên dương.");
        }

        if (MemorySize < AllocationUnit)
        {
            throw new ArgumentException("MemorySize phải lớn hơn hoặc bằng AllocationUnit.");
        }

        this.MemorySize = MemorySize;
        this.AllocationUnit = AllocationUnit;
    }
}
