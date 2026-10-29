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
        throw new NotImplementedException("Chưa triển khai — task ALGO-04.");
    }
}
