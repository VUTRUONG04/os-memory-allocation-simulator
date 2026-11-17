using OSMemoryAllocationSimulator.Models;

namespace OSMemoryAllocationSimulator.Algorithms;

/// <summary>
/// Best-Fit: chọn vùng trống NHỎ NHẤT đủ lớn.
/// Tie-breaking: nếu nhiều vùng cùng kích thước, chọn vùng có StartAddress nhỏ nhất.
/// </summary>
public sealed class BestFitAlgorithm : IFitAlgorithm
{
    public string Name => "BestFit";

    public MemoryRegion? FindFit(IReadOnlyList<MemoryRegion> freeRegions, int requiredSize)
    {
        ArgumentNullException.ThrowIfNull(freeRegions);

        MemoryRegion? best = null;

        // Không giả định thứ tự đầu vào: tự so sánh cả Size lẫn StartAddress để tie-break đúng.
        for (int i = 0; i < freeRegions.Count; i++)
        {
            MemoryRegion candidate = freeRegions[i];
            if (candidate.Size < requiredSize)
            {
                continue;
            }

            if (best is null
                || candidate.Size < best.Size
                || (candidate.Size == best.Size && candidate.StartAddress < best.StartAddress))
            {
                best = candidate;
            }
        }

        return best;
    }
}
