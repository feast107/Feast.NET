using System.Runtime.CompilerServices;

namespace Feast.Aspire.AppHost.Mounts;

public static class Mounts
{
    static Mounts()
    {
        Here = Location();
        return;
        string Location([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
    }
    
    private static string Here { get; }

    public static IResourceBuilder<ContainerResource> WithDefaultMount(this IResourceBuilder<ContainerResource> builder)
    {
        var directory = new DirectoryInfo(Path.Combine(Here, builder.Resource.Name));
        if (!directory.Exists)
            return builder;
        foreach (var (physics, target) in EachNotEmpty(directory, directory))
        {
            builder.WithBindMount(physics, $"/{target.Replace('\\', '/')}");
        }
        return builder;
    }

    private static IEnumerable<(string physics, string target)> EachNotEmpty(DirectoryInfo root, DirectoryInfo directory)
    {
        if (directory.EnumerateFiles().Any())
        {
            yield return (directory.FullName, Path.GetRelativePath(root.FullName, directory.FullName));
            yield break;
        }
        foreach (var subDirectory in directory.EnumerateDirectories())
        foreach (var tuple in EachNotEmpty(root, subDirectory))
            yield return tuple;
    }
    
}