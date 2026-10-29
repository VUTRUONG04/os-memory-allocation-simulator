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
        throw new NotImplementedException("Chưa triển khai — task ALGO-03.");
    }
}
