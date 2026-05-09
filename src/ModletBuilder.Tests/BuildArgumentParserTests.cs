using ModletBuilder.Cli;
using ModletBuilder.Core.Models;

namespace ModletBuilder.Tests;

public class BuildArgumentParserTests
{
    [Fact]
    public void Valid_minimal_args_parse_successfully()
    {
        var (options, errors) = BuildCommand.ParseArgs(["--src", "file.frag.xml", "--out", "C:\\Mods\\MyMod"]);

        Assert.Empty(errors);
        Assert.NotNull(options);
        Assert.Equal(["file.frag.xml"], options.Sources);
        Assert.Equal("C:\\Mods\\MyMod", options.OutputDir);
        Assert.False(options.Recursive);
        Assert.False(options.DryRun);
    }

    [Fact]
    public void Multiple_src_paths_are_collected()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "a.frag.xml", "src-dir", "b.frag.xml", "--out", "out"]);

        Assert.Empty(errors);
        Assert.NotNull(options);
        Assert.Equal(["a.frag.xml", "src-dir", "b.frag.xml"], options.Sources);
    }

    [Fact]
    public void Recursive_flag_is_parsed()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--recursive"]);

        Assert.Empty(errors);
        Assert.NotNull(options);
        Assert.True(options.Recursive);
    }

    [Fact]
    public void DryRun_flag_is_parsed()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--dry-run"]);

        Assert.Empty(errors);
        Assert.NotNull(options);
        Assert.True(options.DryRun);
    }

    [Fact]
    public void Missing_src_returns_error()
    {
        var (options, errors) = BuildCommand.ParseArgs(["--out", "out"]);

        Assert.Null(options);
        Assert.Contains(errors, e => e.Contains("--src"));
    }

    [Fact]
    public void Missing_out_returns_error()
    {
        var (options, errors) = BuildCommand.ParseArgs(["--src", "file.frag.xml"]);

        Assert.Null(options);
        Assert.Contains(errors, e => e.Contains("--out"));
    }

    [Fact]
    public void Missing_both_src_and_out_returns_two_errors()
    {
        var (options, errors) = BuildCommand.ParseArgs([]);

        Assert.Null(options);
        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void Unknown_option_returns_error()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "file.frag.xml", "--out", "out", "--unknown"]);

        Assert.Null(options);
        Assert.Contains(errors, e => e.Contains("--unknown"));
    }

    [Fact]
    public void Src_without_paths_returns_error()
    {
        var (options, errors) = BuildCommand.ParseArgs(["--src", "--out", "out"]);

        Assert.Null(options);
        Assert.Contains(errors, e => e.Contains("--src"));
    }
}
