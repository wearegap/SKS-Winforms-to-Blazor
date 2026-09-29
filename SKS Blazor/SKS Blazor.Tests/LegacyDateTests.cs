using SKS_Blazor.Infrastructure.Helpers;
using Xunit;

namespace SKS_Blazor.Tests;

public class LegacyDateTests
{
    [Theory]
    [InlineData("1361232000000", 2013, 2, 19)] // epoch ms, seed rows (UTC midnight)
    [InlineData("1368748800000", 2013, 5, 17)]
    [InlineData("02/14/2020", 2020, 2, 14)]     // MM/dd/yyyy, frmOrderRequest
    [InlineData("2/13/2020", 2020, 2, 13)]      // M/d/yyyy, DateTimeHelper.ToString(DateTime.Today)
    [InlineData("2020-02-13", 2020, 2, 13)]     // yyyy-MM-dd, written by the first Blazor port
    [InlineData(" 4/23/2020 ", 2020, 4, 23)]
    public void Parse_AcceptsEveryLegacyShape(string raw, int y, int m, int d)
    {
        Assert.Equal(new DateTime(y, m, d), LegacyDate.Parse(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a date")]
    public void Parse_ReturnsNullForEmptyOrUnknown(string? raw)
    {
        Assert.Null(LegacyDate.Parse(raw));
    }

    [Fact]
    public void Format_WritesInvariantStorageFormat()
    {
        Assert.Equal("02/03/2020", LegacyDate.Format(new DateTime(2020, 2, 3, 15, 30, 0)));
    }

    [Fact]
    public void Display_NormalisesParseableAndKeepsOthers()
    {
        Assert.Equal("02/19/2013", LegacyDate.Display("1361232000000"));
        Assert.Equal("garbage", LegacyDate.Display("garbage"));
        Assert.Equal(string.Empty, LegacyDate.Display(null));
    }

    [Fact]
    public void InRange_IsInclusiveAndRejectsUnparseable()
    {
        var from = new DateTime(2013, 2, 19);
        var to = new DateTime(2013, 2, 20);
        Assert.True(LegacyDate.InRange("1361232000000", from, to));   // 2013-02-19
        Assert.True(LegacyDate.InRange("1361318400000", from, to));   // 2013-02-20
        Assert.False(LegacyDate.InRange("1361491200000", from, to));  // 2013-02-22
        Assert.True(LegacyDate.InRange("02/14/2020", null, null));
        Assert.False(LegacyDate.InRange("", null, null));
    }
}
