using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DMNSN.Templates.NugetLibrary.Tests;

public class ExampleTests
{
    // NullLogger avoids needing a logging provider in tests. Swap it for a capturing fake if you
    // want to assert on what was logged.
    private readonly Example _sut = new(NullLogger<Example>.Instance);

    [Fact]
    public void GetMessage_IncludesTheProvidedName()
    {
        var result = _sut.GetMessage("World");

        Assert.Equal("Hello World, this is message from DMNSN.Templates.NugetLibrary!", result);
    }

    [Theory]
    [InlineData("Alice")]
    [InlineData("")]
    public void GetMessage_AlwaysMentionsTheLibrary(string name)
    {
        var result = _sut.GetMessage(name);

        Assert.Contains("DMNSN.Templates.NugetLibrary", result);
    }
}
