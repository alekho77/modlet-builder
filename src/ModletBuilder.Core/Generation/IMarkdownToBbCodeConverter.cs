using ModletBuilder.Core.Logging;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Core.Generation;

internal interface IMarkdownToBbCodeConverter
{
    IReadOnlyList<Diagnostic> Convert(
        string markdownPath,
        string outputPath,
        BuildLogger logger);
}
