namespace Core.Tests;

using Core.Reports;
using Xunit;

public sealed class ReportServiceTests
{
    [Theory]
    [InlineData(1000, 2)]
    [InlineData(100, 0)]
    public void LowTotal_CountsOnlyBelowThreshold(decimal threshold, int expected)
    {
        var result = ReportService.LowTotal(TestData.Orders(), threshold);
        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public void TopOrders_IsDeterministic()
    {
        var result = ReportService.TopOrders(TestData.Orders().Reverse(), 2);
        Assert.Equal(new[] { "TEST-ORD-1", "TEST-ORD-2" }, result.Select(r => r.OrderId));
    }

    [Fact]
    public void Totals_WhenEmptySource_ReturnsZeroWithoutThrowing()
    {
        var totals = ReportService.Totals([]);
        Assert.Equal(0, totals.Items);
        Assert.Equal(0m, totals.Total);
        Assert.Equal(0m, totals.Average);
        Assert.Equal(0m, totals.Max);
    }
}
