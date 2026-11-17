using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Algorithms;

/// <summary>
/// First-Fit: chọn vùng trống ĐẦU TIÊN đủ lớn khi duyệt theo địa chỉ tăng dần.
/// </summary>
public sealed class FirstFitAlgorithm : IFitAlgorithm
{
    public string Name => "FirstFit";

    public MemoryRegion? FindFit(IReadOnlyList<MemoryRegion> freeRegions, int requiredSize)
    {
        ArgumentNullException.ThrowIfNull(freeRegions);

        // freeRegions đã sắp xếp theo StartAddress tăng dần -> vùng đủ lớn đầu tiên là kết quả.
        for (int i = 0; i < freeRegions.Count; i++)
        {
            if (freeRegions[i].Size >= requiredSize)
            {
                return freeRegions[i];
            }
        }

        return null;
    }
}
