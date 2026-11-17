using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Algorithms;

/// <summary>
/// Worst-Fit: chọn vùng trống LỚN NHẤT đủ lớn.
/// Tie-breaking: nếu nhiều vùng cùng kích thước, chọn vùng có StartAddress nhỏ nhất.
/// </summary>
public sealed class WorstFitAlgorithm : IFitAlgorithm
{
    public string Name => "WorstFit";

    public MemoryRegion? FindFit(IReadOnlyList<MemoryRegion> freeRegions, int requiredSize)
    {
        ArgumentNullException.ThrowIfNull(freeRegions);

        MemoryRegion? worst = null;

        for (int i = 0; i < freeRegions.Count; i++)
        {
            MemoryRegion candidate = freeRegions[i];
            if (candidate.Size < requiredSize)
            {
                continue;
            }

            if (worst is null
                || candidate.Size > worst.Size
                || (candidate.Size == worst.Size && candidate.StartAddress < worst.StartAddress))
            {
                worst = candidate;
            }
        }

        return worst;
    }
}
