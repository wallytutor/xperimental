using System.Diagnostics;

namespace Xl.Elmer.Sif;

public enum ElmerGridFormat
{
    Abaqus = 1,
    Elmer = 2,
    Ansys = 4,
    Universal = 8,
    Gmsh = 14,
    Vtu = 22,
    Su2 = 27
}

public sealed class ElmerGridProject
{
    public ElmerGridProject(string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new ArgumentException("Working directory cannot be null or empty.", nameof(workingDirectory));
        }

        WorkingDirectory = workingDirectory;
    }

    public string WorkingDirectory { get; }

    public string ExecutablePath { get; set; } = "ElmerGrid";

    public ProcessStartInfo CreateConversionStartInfo(
        ElmerGridFormat from,
        ElmerGridFormat to,
        string inputPath,
        string? outputPath = null,
        IEnumerable<string>? additionalArguments = null)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("Input path cannot be null or empty.", nameof(inputPath));
        }

        var startInfo = CreateBaseStartInfo();
        startInfo.ArgumentList.Add(((int)from).ToString());
        startInfo.ArgumentList.Add(((int)to).ToString());
        startInfo.ArgumentList.Add(inputPath);

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            startInfo.ArgumentList.Add("-out");
            startInfo.ArgumentList.Add(outputPath);
        }

        AddArguments(startInfo, additionalArguments);
        return startInfo;
    }

    public ProcessStartInfo CreatePartitionStartInfo(
        ElmerGridFormat meshFormat,
        string meshPath,
        int partitions,
        int? haloLayers = null,
        IEnumerable<string>? additionalArguments = null)
    {
        if (string.IsNullOrWhiteSpace(meshPath))
        {
            throw new ArgumentException("Mesh path cannot be null or empty.", nameof(meshPath));
        }

        if (partitions < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(partitions), "Partition count must be at least one.");
        }

        var startInfo = CreateBaseStartInfo();
        startInfo.ArgumentList.Add(((int)meshFormat).ToString());
        startInfo.ArgumentList.Add(((int)meshFormat).ToString());
        startInfo.ArgumentList.Add(meshPath);
        startInfo.ArgumentList.Add("-metis");
        startInfo.ArgumentList.Add(partitions.ToString());

        if (haloLayers is not null)
        {
            startInfo.ArgumentList.Add(haloLayers.Value.ToString());
        }

        AddArguments(startInfo, additionalArguments);
        return startInfo;
    }

    public Task<int> ConvertAsync(
        ElmerGridFormat from,
        ElmerGridFormat to,
        string inputPath,
        string? outputPath = null,
        IEnumerable<string>? additionalArguments = null,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(CreateConversionStartInfo(from, to, inputPath, outputPath, additionalArguments), cancellationToken);
    }

    public Task<int> PartitionAsync(
        ElmerGridFormat meshFormat,
        string meshPath,
        int partitions,
        int? haloLayers = null,
        IEnumerable<string>? additionalArguments = null,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(CreatePartitionStartInfo(meshFormat, meshPath, partitions, haloLayers, additionalArguments), cancellationToken);
    }

    private ProcessStartInfo CreateBaseStartInfo()
    {
        return new ProcessStartInfo
        {
            FileName = ExecutablePath,
            WorkingDirectory = WorkingDirectory,
            UseShellExecute = false
        };
    }

    private static void AddArguments(ProcessStartInfo startInfo, IEnumerable<string>? additionalArguments)
    {
        if (additionalArguments is null)
        {
            return;
        }

        foreach (var argument in additionalArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
    }

    private static async Task<int> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start process {startInfo.FileName}.");

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}