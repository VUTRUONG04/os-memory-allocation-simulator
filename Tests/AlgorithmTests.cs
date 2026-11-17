using OSMemoryAllocationSimulator.Algorithms;
using OSMemoryAllocationSimulator.Models;
using Xunit;

namespace OSMemoryAllocationSimulator.Tests;

public class AlgorithmTests
{
    private static MemoryRegion Free(int start, int size) => new(start, size, true);

    private static List<MemoryRegion> Regions(params (int Start, int Size)[] items) =>
        items.Select(i => Free(i.Start, i.Size)).ToList();

    public static IEnumerable<object[]> AllAlgorithms()
    {
        yield return new object[] { new FirstFitAlgorithm() };
        yield return new object[] { new BestFitAlgorithm() };
        yield return new object[] { new WorstFitAlgorithm() };
    }

    // ---------- Name ----------

    [Fact]
    public void Name_ShouldMatchSpecification()
    {
        Assert.Equal("FirstFit", new FirstFitAlgorithm().Name);
        Assert.Equal("BestFit", new BestFitAlgorithm().Name);
        Assert.Equal("WorstFit", new WorstFitAlgorithm().Name);
    }

    // ---------- Dùng chung cho cả 3 thuật toán ----------

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void FindFit_ExactFit_ReturnsThatRegion(IFitAlgorithm algo)
    {
        var regions = Regions((0, 100));

        var result = algo.FindFit(regions, 100);

        Assert.NotNull(result);
        Assert.Equal(0, result!.StartAddress);
        Assert.Equal(100, result.Size);
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void FindFit_RegionLargerThanRequired_ReturnsThatRegion(IFitAlgorithm algo)
    {
        var regions = Regions((50, 300));

        var result = algo.FindFit(regions, 128);

        Assert.NotNull(result);
        Assert.Equal(50, result!.StartAddress);
        Assert.Equal(300, result.Size);
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void FindFit_NoRegionBigEnough_ReturnsNull(IFitAlgorithm algo)
    {
        var regions = Regions((0, 64), (200, 128), (500, 100));

        Assert.Null(algo.FindFit(regions, 129));
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void FindFit_EmptyList_ReturnsNull(IFitAlgorithm algo)
    {
        Assert.Null(algo.FindFit(new List<MemoryRegion>(), 64));
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void FindFit_NullList_ThrowsArgumentNullException(IFitAlgorithm algo)
    {
        Assert.Throws<ArgumentNullException>(() => algo.FindFit(null!, 64));
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void FindFit_SkipsRegionsTooSmall(IFitAlgorithm algo)
    {
        // Chỉ có vùng ở địa chỉ 300 là đủ lớn.
        var regions = Regions((0, 32), (100, 64), (300, 256));

        var result = algo.FindFit(regions, 200);

        Assert.NotNull(result);
        Assert.Equal(300, result!.StartAddress);
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void FindFit_DoesNotMutateInputList(IFitAlgorithm algo)
    {
        var regions = Regions((0, 128), (200, 64), (400, 256));
        var snapshot = regions.ToList();

        algo.FindFit(regions, 64);

        Assert.Equal(snapshot, regions); // record => so sánh theo giá trị, thứ tự giữ nguyên
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void FindFit_ReturnsReferenceFromInputList(IFitAlgorithm algo)
    {
        var regions = Regions((0, 128), (200, 256));

        var result = algo.FindFit(regions, 100);

        Assert.Contains(result, regions);
    }

    // ---------- First-Fit ----------

    [Fact]
    public void FirstFit_ChoosesFirstBigEnoughRegion_NotSmallestNorLargest()
    {
        var regions = Regions((0, 50), (100, 200), (400, 100), (600, 500));

        var result = new FirstFitAlgorithm().FindFit(regions, 90);

        Assert.NotNull(result);
        Assert.Equal(100, result!.StartAddress); // đầu tiên đủ lớn, dù 400 nhỏ hơn và 600 lớn hơn
    }

    [Fact]
    public void FirstFit_MultipleEqualRegions_ChoosesLowestAddress()
    {
        var regions = Regions((0, 100), (200, 100), (400, 100));

        var result = new FirstFitAlgorithm().FindFit(regions, 100);

        Assert.Equal(0, result!.StartAddress);
    }

    // ---------- Best-Fit ----------

    [Fact]
    public void BestFit_ChoosesSmallestRegionThatFits()
    {
        var regions = Regions((0, 500), (600, 120), (800, 300), (1200, 90));

        var result = new BestFitAlgorithm().FindFit(regions, 100);

        Assert.NotNull(result);
        Assert.Equal(600, result!.StartAddress);
        Assert.Equal(120, result.Size);
    }

    [Fact]
    public void BestFit_ExactFitBeatsLargerRegionsEvenIfLater()
    {
        var regions = Regions((0, 500), (600, 200), (900, 100));

        var result = new BestFitAlgorithm().FindFit(regions, 100);

        Assert.Equal(900, result!.StartAddress);
        Assert.Equal(100, result.Size);
    }

    [Fact]
    public void BestFit_TieBreaking_ChoosesLowestStartAddress()
    {
        var regions = Regions((0, 500), (300, 128), (700, 128), (1000, 128));

        var result = new BestFitAlgorithm().FindFit(regions, 100);

        Assert.Equal(300, result!.StartAddress);
        Assert.Equal(128, result.Size);
    }

    [Fact]
    public void BestFit_TieBreaking_WorksEvenWhenInputNotSortedByAddress()
    {
        var regions = Regions((1000, 128), (300, 128), (700, 128));

        var result = new BestFitAlgorithm().FindFit(regions, 100);

        Assert.Equal(300, result!.StartAddress);
    }

    [Fact]
    public void BestFit_TieBreaking_LaterSmallerRegionStillWinsOverEarlierLarger()
    {
        var regions = Regions((0, 256), (300, 128), (700, 256));

        var result = new BestFitAlgorithm().FindFit(regions, 100);

        Assert.Equal(300, result!.StartAddress);
    }

    // ---------- Worst-Fit ----------

    [Fact]
    public void WorstFit_ChoosesLargestRegion()
    {
        var regions = Regions((0, 100), (200, 700), (1000, 300));

        var result = new WorstFitAlgorithm().FindFit(regions, 50);

        Assert.NotNull(result);
        Assert.Equal(200, result!.StartAddress);
        Assert.Equal(700, result.Size);
    }

    [Fact]
    public void WorstFit_LargestIsLastRegion()
    {
        var regions = Regions((0, 100), (200, 200), (500, 900));

        var result = new WorstFitAlgorithm().FindFit(regions, 100);

        Assert.Equal(500, result!.StartAddress);
    }

    [Fact]
    public void WorstFit_TieBreaking_ChoosesLowestStartAddress()
    {
        var regions = Regions((0, 100), (300, 512), (700, 512), (1500, 512));

        var result = new WorstFitAlgorithm().FindFit(regions, 100);

        Assert.Equal(300, result!.StartAddress);
        Assert.Equal(512, result.Size);
    }

    [Fact]
    public void WorstFit_TieBreaking_WorksEvenWhenInputNotSortedByAddress()
    {
        var regions = Regions((1500, 512), (300, 512), (700, 512));

        var result = new WorstFitAlgorithm().FindFit(regions, 100);

        Assert.Equal(300, result!.StartAddress);
    }

    [Fact]
    public void WorstFit_LargestRegionTooSmall_ReturnsNull()
    {
        var regions = Regions((0, 100), (200, 100));

        Assert.Null(new WorstFitAlgorithm().FindFit(regions, 101));
    }

    // ---------- So sánh 3 thuật toán trên cùng dữ liệu ----------

    [Fact]
    public void ThreeAlgorithms_OnSameInput_PickDifferentRegions()
    {
        // Địa chỉ: 0(300), 400(120), 700(600), 1500(150)
        var regions = Regions((0, 300), (400, 120), (700, 600), (1500, 150));

        Assert.Equal(0, new FirstFitAlgorithm().FindFit(regions, 100)!.StartAddress);
        Assert.Equal(400, new BestFitAlgorithm().FindFit(regions, 100)!.StartAddress);
        Assert.Equal(700, new WorstFitAlgorithm().FindFit(regions, 100)!.StartAddress);
    }
}
