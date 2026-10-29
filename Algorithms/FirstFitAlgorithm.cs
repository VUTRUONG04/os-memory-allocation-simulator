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
        throw new NotImplementedException("Chưa triển khai — task ALGO-02.");
    }
}
