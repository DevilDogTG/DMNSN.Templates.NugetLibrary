using Microsoft.Extensions.Logging;

namespace DMNSN.Templates.NugetLibrary;

/// <summary>
/// Example service showing the two patterns this template sets up: constructor-injected
/// logging, and high-performance source-generated log messages (see Example.LogMessages.cs).
/// Replace it with your own types.
/// </summary>
/// <param name="logger">Logger used to record calls.</param>
public partial class Example(
    ILogger<Example> logger)
{
    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <param name="name">The name to include in the message.</param>
    /// <returns>The message string.</returns>
    public string GetMessage(string name)
    {
        LogInfoExample(name);
        return $"Hello {name}, this is message from DMNSN.Templates.NugetLibrary!";
    }
}
