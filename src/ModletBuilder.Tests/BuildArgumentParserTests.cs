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
        Assert.False(options.Clean);
        Assert.Equal(VerbosityLevel.Information, options.Verbosity);
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
    public void Targets_option_is_unknown_and_returns_error()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--targets", "ModA"]);

        Assert.Null(options);
        Assert.Contains(errors, e => e.Contains("--targets") || e.Contains("targets"));
    }

    [Fact]
    public void Clean_flag_is_parsed()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--clean"]);

        Assert.Empty(errors);
        Assert.NotNull(options);
        Assert.True(options.Clean);
    }

    [Fact]
    public void Verbosity_debug_is_parsed()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--verbosity", "debug"]);

        Assert.Empty(errors);
        Assert.NotNull(options);
        Assert.Equal(VerbosityLevel.Debug, options.Verbosity);
    }

    [Fact]
    public void Verbosity_information_is_parsed()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--verbosity", "information"]);

        Assert.Empty(errors);
        Assert.Equal(VerbosityLevel.Information, options!.Verbosity);
    }

    [Fact]
    public void Verbosity_warning_is_parsed()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--verbosity", "warning"]);

        Assert.Empty(errors);
        Assert.Equal(VerbosityLevel.Warning, options!.Verbosity);
    }

    [Fact]
    public void Verbosity_error_is_parsed()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--verbosity", "error"]);

        Assert.Empty(errors);
        Assert.Equal(VerbosityLevel.Error, options!.Verbosity);
    }

    [Fact]
    public void Verbosity_none_is_parsed()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--verbosity", "none"]);

        Assert.Empty(errors);
        Assert.Equal(VerbosityLevel.None, options!.Verbosity);
    }

    [Fact]
    public void Verbosity_unknown_value_returns_error()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--verbosity", "verbose"]);

        Assert.Null(options);
        Assert.Contains(errors, e => e.Contains("verbose"));
    }

    [Fact]
    public void Verbosity_without_value_returns_error()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "src/", "--out", "out", "--verbosity"]);

        Assert.Null(options);
        Assert.Contains(errors, e => e.Contains("--verbosity"));
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

    [Fact]
    public void Out_without_value_at_end_returns_error()
    {
        var (options, errors) = BuildCommand.ParseArgs(["--src", "file.frag.xml", "--out"]);

        Assert.Null(options);
        Assert.Contains(errors, e => e.Contains("--out"));
    }

    [Fact]
    public void Out_specified_twice_uses_last_value()
    {
        var (options, errors) = BuildCommand.ParseArgs(
            ["--src", "file.frag.xml", "--out", "first_out", "--out", "second_out"]);

        Assert.Empty(errors);
        Assert.NotNull(options);
        Assert.Equal("second_out", options.OutputDir);
    }
}
