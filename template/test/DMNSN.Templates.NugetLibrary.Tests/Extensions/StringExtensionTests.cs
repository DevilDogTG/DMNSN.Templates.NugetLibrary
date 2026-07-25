using DMNSN.Templates.NugetLibrary.Extensions;
using Xunit;

namespace DMNSN.Templates.NugetLibrary.Tests.Extensions;

public class StringExtensionTests
{
    [Theory]
    [InlineData("hello world", "Hello World")]
    [InlineData("HELLO WORLD", "Hello World")]
    [InlineData("hELLo", "Hello")]
    [InlineData("multiple   spaces", "Multiple   Spaces")]
    public void ToTitleCase_CapitalisesEachWord(string input, string expected)
        => Assert.Equal(expected, input.ToTitleCase());

    [Fact]
    public void ToTitleCase_ReturnsEmptyStringUnchanged()
        => Assert.Equal(string.Empty, string.Empty.ToTitleCase());

    [Fact]
    public void ToTitleCase_ReturnsNullUnchanged()
    {
        // The early return covers null even though the signature is non-nullable - callers from
        // nullable-disabled code can still pass one.
        string? input = null;

        Assert.Null(input!.ToTitleCase());
    }
}
