using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Algorithms;

public interface IFitAlgorithm
{
    /// <summary>
    /// Tên thuật toán dùng trong CSV và hiển thị (FirstFit / BestFit / WorstFit).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Tìm vùng trống phù hợp để cấp phát.
    /// </summary>
    /// <param name="freeRegions">Danh sách các vùng trống, đã sắp xếp theo StartAddress tăng dần.</param>
    /// <param name="requiredSize">Kích thước cần cấp phát (đã làm tròn theo AllocationUnit).</param>
    /// <returns>Vùng trống được chọn, hoặc null nếu không có vùng nào đủ lớn.</returns>
    MemoryRegion? FindFit(IReadOnlyList<MemoryRegion> freeRegions, int requiredSize);
}
