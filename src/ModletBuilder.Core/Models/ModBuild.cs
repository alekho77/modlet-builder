namespace ModletBuilder.Core.Models;

internal sealed record ModBuild(
    string ModName,
    IReadOnlyList<Fragment> OrderedFragments);
