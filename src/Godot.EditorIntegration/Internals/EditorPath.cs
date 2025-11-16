using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace Godot.EditorIntegration.Internals;

/// <summary>
/// Contains cached directory/file names and paths used for the .NET projects.
/// </summary>
internal static class EditorPath
{
    private static string? _projectAssemblyName;
    private static string? _slnPath;
    private static string? _csprojPath;
    private static string? _editorAssembliesPath;
    private static string? _baseBuildLogsPath;

    public static string ProjectAssemblyName => _projectAssemblyName ??= EditorInternal.GetProjectAssemblyName();

    public static string ProjectSlnPath => _slnPath ??= GetProjectSlnPath();

    public static string ProjectCSProjPath => _csprojPath ??= EditorInternal.GetProjectCSProjPath();

    public static string EditorAssembliesPath => _editorAssembliesPath ??= EditorInternal.GetEditorAssembliesPath();

    public static string GetLogsDirPathFor(string project, string configuration)
    {
        _baseBuildLogsPath ??= ProjectSettings.Singleton.GlobalizePath("user://msbuild_logs/");
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(project)));
        return Path.Join(_baseBuildLogsPath, hash, configuration);
    }

    public static string GetLogsDirPathFor(string configuration)
    {
        return GetLogsDirPathFor(ProjectCSProjPath, configuration);
    }

    public static void InvalidateCachedDirectories()
    {
        // Clear all the cached values that may change based on the project settings.
        _projectAssemblyName = null;
        _slnPath = null;
        _csprojPath = null;
    }

    private static string GetProjectSlnPath()
    {
        string slnDir = EditorInternal.GetProjectSlnPath();

        List<string> slnPaths = new();
        slnPaths.AddRange(Directory.GetFiles(slnDir, "*.sln"));
        slnPaths.AddRange(Directory.GetFiles(slnDir, "*.slnx"));

        for (int i = slnPaths.Count - 1; i > 0; --i)
        {
            ISolutionSerializer? serializer = SolutionSerializers.GetSerializerByMoniker(slnPaths[i]);

            if (serializer is null)
            {
                goto SolutionIsInvalid;
            }

            SolutionModel solution = serializer.OpenAsync(slnPaths[i], CancellationToken.None).Result;

            foreach (SolutionProjectModel project in solution.SolutionProjects)
            {
                string csProjPath = Path.GetFullPath(project.FilePath, Path.GetDirectoryName(slnPaths[i])!)
                                        .Replace('\\', '/');

                if (string.Equals(csProjPath, ProjectCSProjPath, StringComparison.Ordinal))
                {
                    goto SolutionIsValid;
                }
            }

        SolutionIsInvalid:
            slnPaths.RemoveAt(i);
        SolutionIsValid:;
        }

        // TODO: Better error handling.
        switch (slnPaths.Count)
        {
            case 1:
                return slnPaths[0];
            case 0:
                GD.PushError("NO SOLUTION");
                return Path.Combine(slnDir, $"{ProjectAssemblyName}.sln");
            default:
                GD.PushError("MULTIPLE SOLUTIONS");
                return string.Empty;
        }
    }
}
