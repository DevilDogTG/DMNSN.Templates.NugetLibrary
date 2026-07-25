using Microsoft.Extensions.Logging;

namespace DMNSN.Templates.NugetLibrary;

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
