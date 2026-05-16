using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Logging;

internal sealed class BuildLogger
{
    private readonly VerbosityLevel _verbosity;
    private readonly TextWriter _out;
    private readonly TextWriter _err;

    internal BuildLogger(VerbosityLevel verbosity, TextWriter @out, TextWriter err)
    {
        _verbosity = verbosity;
        _out = @out;
        _err = err;
    }

    internal void Debug(string message)       => Write(VerbosityLevel.Debug,       message);
    internal void Information(string message) => Write(VerbosityLevel.Information, message);
    internal void Warning(string message)     => Write(VerbosityLevel.Warning,     message);
    internal void Error(string message)       => Write(VerbosityLevel.Error,       message, isError: true);

    private void Write(VerbosityLevel level, string message, bool isError = false)
    {
        if (level < _verbosity)
            return;

        (isError ? _err : _out).WriteLine(message);
    }
}
