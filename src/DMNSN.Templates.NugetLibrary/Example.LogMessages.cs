using Microsoft.Extensions.Logging;

namespace DMNSN.Templates.NugetLibrary;

public partial class Example
{

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "GetMessage called with name: {Name}")]
    private partial void LogInfoExample(string name);
}
