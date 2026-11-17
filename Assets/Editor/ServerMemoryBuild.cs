using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ServerMemoryBuild
{
    public static bool KeepClientUI { get; private set; }

    [MenuItem("Build/Optimized Linux Dedicated Server")]
    public static void BuildLinux()
    {
        string output = "Builds/MemoryServer/ServerBuild";
        string[] args = Environment.GetCommandLineArgs();
        KeepClientUI = args.Contains("-serverKeepClientUI");
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == "-serverBuildOutput") output = args[i + 1];

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        var options = new BuildPlayerOptions
        {
            // Preserve scene names/order: several legacy games still use build indices.
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = output,
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.None
        };
        var previousSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
        BuildReport report;
        try
        {
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
            report = BuildPipeline.BuildPlayer(options);
        }
        finally
        {
            EditorUserBuildSettings.standaloneBuildSubtarget = previousSubtarget;
            KeepClientUI = false;
        }
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Server build failed: {report.summary.result}, {report.summary.totalErrors} errors.");

        Debug.Log($"[ServerBuild] Succeeded: {report.summary.totalSize} bytes at {output}");
    }
}
