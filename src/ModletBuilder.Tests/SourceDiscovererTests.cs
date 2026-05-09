using ModletBuilder.Core.Models;
using ModletBuilder.Core.SourceDiscovery;

namespace ModletBuilder.Tests;

public class SourceDiscovererTests : IDisposable
{
    private readonly string _tempDir;

    public SourceDiscovererTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Explicit_frag_xml_file_is_discovered()
    {
        var file = CreateFragFile("a.frag.xml");

        var (files, diagnostics) = SourceDiscoverer.Discover([file], recursive: false);

        Assert.Empty(diagnostics);
        Assert.Single(files);
        Assert.Equal(Path.GetFullPath(file), files[0]);
    }

    [Fact]
    public void Non_frag_xml_file_produces_error()
    {
        var file = Path.Combine(_tempDir, "file.xml");
        File.WriteAllText(file, "<root/>");

        var (files, diagnostics) = SourceDiscoverer.Discover([file], recursive: false);

        Assert.Empty(files);
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
    }

    [Fact]
    public void Directory_is_scanned_for_frag_xml_files()
    {
        CreateFragFile("sub/a.frag.xml");
        CreateFragFile("sub/b.frag.xml");

        var dir = Path.Combine(_tempDir, "sub");
        var (files, diagnostics) = SourceDiscoverer.Discover([dir], recursive: false);

        Assert.Empty(diagnostics);
        Assert.Equal(2, files.Count);
    }

    [Fact]
    public void Non_existent_path_produces_error()
    {
        var missing = Path.Combine(_tempDir, "does_not_exist");

        var (files, diagnostics) = SourceDiscoverer.Discover([missing], recursive: false);

        Assert.Empty(files);
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
    }

    [Fact]
    public void Duplicate_paths_are_deduplicated()
    {
        var file = CreateFragFile("a.frag.xml");

        var (files, diagnostics) = SourceDiscoverer.Discover([file, file], recursive: false);

        Assert.Empty(diagnostics);
        Assert.Single(files);
    }

    [Fact]
    public void Files_are_sorted_alphabetically()
    {
        CreateFragFile("z.frag.xml");
        CreateFragFile("a.frag.xml");
        CreateFragFile("m.frag.xml");

        var (files, diagnostics) = SourceDiscoverer.Discover([_tempDir], recursive: false);

        Assert.Empty(diagnostics);
        Assert.Equal(3, files.Count);

        var names = files.Select(Path.GetFileName).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList(), names);
    }

    [Fact]
    public void Non_recursive_does_not_scan_subdirectories()
    {
        CreateFragFile("top.frag.xml");
        CreateFragFile("sub/nested.frag.xml");

        var (files, _) = SourceDiscoverer.Discover([_tempDir], recursive: false);

        Assert.Single(files);
        Assert.Contains("top.frag.xml", files[0]);
    }

    [Fact]
    public void Recursive_scans_subdirectories()
    {
        CreateFragFile("top.frag.xml");
        CreateFragFile("sub/nested.frag.xml");

        var (files, _) = SourceDiscoverer.Discover([_tempDir], recursive: true);

        Assert.Equal(2, files.Count);
    }

    [Fact]
    public void Mixed_files_and_directories_are_accepted()
    {
        var fileA = CreateFragFile("a.frag.xml");
        CreateFragFile("subdir/b.frag.xml");
        var dir = Path.Combine(_tempDir, "subdir");

        var (files, diagnostics) = SourceDiscoverer.Discover([fileA, dir], recursive: false);

        Assert.Empty(diagnostics);
        Assert.Equal(2, files.Count);
    }

    private string CreateFragFile(string relativePath)
    {
        var fullPath = Path.Combine(_tempDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "<modlet><fragment name=\"x\" target=\"items\"/></modlet>");
        return fullPath;
    }
}
