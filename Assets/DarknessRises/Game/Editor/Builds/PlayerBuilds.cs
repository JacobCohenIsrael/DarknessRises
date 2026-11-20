using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace DarknessRises.Game.Editor.Builds
{
    /// <summary>
    /// Headless player builds for testing outside the Editor: an Android APK for side-loading and a WebGL build
    /// zipped for itch.io. Addressables content is built with the player (the settings' "build with player" option).
    /// Every build is versioned: the version is <see cref="PlayerSettings.bundleVersion"/> (major.minor.patch,
    /// source-controlled in ProjectSettings) and it goes into every output name. Pass <c>-buildVersion 0.2.0</c> to
    /// set a new version first. Run one target per Editor start, so Unity opens straight on that platform:
    /// <c>unity build . --target Android --execute-method DarknessRises.Game.Editor.Builds.PlayerBuilds.BuildAndroid</c>
    /// <c>unity build . --target WebGL --execute-method DarknessRises.Game.Editor.Builds.PlayerBuilds.BuildWebGL</c>
    /// Outputs: <c>Builds/Android/DarknessRises-{version}.apk</c> and <c>Builds/WebGL/DarknessRises-{version}-webgl.zip</c>.
    /// The Web build renders with WebGPU where the browser supports it and falls back to WebGL 2 where it doesn't.
    /// <para>
    /// Dev builds for playtesting: <c>BuildAndroidDev</c> and <c>BuildWebGLDev</c> make a development build of the same
    /// version, named with a <c>-dev</c> suffix (<c>DarknessRises-{version}-dev.apk</c>,
    /// <c>DarknessRises-{version}-dev-webgl.zip</c>). Unity defines <c>DEBUG</c> in their player scripts, which compiles in
    /// the debug commands and the touch debug menu (the DEV button at the top right). No script debugging or profiler
    /// autoconnect, so a dev build plays like the release. Release builds have none of the debug code.
    /// <c>unity build . --target Android --execute-method DarknessRises.Game.Editor.Builds.PlayerBuilds.BuildAndroidDev</c>
    /// <c>unity build . --target WebGL --execute-method DarknessRises.Game.Editor.Builds.PlayerBuilds.BuildWebGLDev</c>
    /// To rebuild the current version without bumping it, pass
    /// <c>--versioning-strategy custom --build-version {the current bundleVersion}</c>.
    /// </para>
    /// </summary>
    public static class PlayerBuilds
    {
        private const string ProductFileName = "DarknessRises";
        private const string ApplicationIdentifier = "com.TheGamefather.DarknessRises";
        private const string VersionArgument = "-buildVersion";
        private const string AndroidFolder = "Builds/Android";
        private const string WebGLFolder = "Builds/WebGL";
        private const string DevSuffix = "-dev";

        // The Web player's graphics APIs, in order of preference: Unity uses the first the browser supports.
        private static readonly GraphicsDeviceType[] WebGraphicsAPIs = { GraphicsDeviceType.WebGPU, GraphicsDeviceType.OpenGLES3 };

        public static void BuildAndroid() => BuildAndroid(false);

        /// <summary>A development build of the same version, with the debug menu: <c>DarknessRises-{version}-dev.apk</c>.</summary>
        public static void BuildAndroidDev() => BuildAndroid(true);

        public static void BuildWebGL() => BuildWebGL(false);

        /// <summary>A development build of the same version, with the debug menu: <c>DarknessRises-{version}-dev-webgl.zip</c>.</summary>
        public static void BuildWebGLDev() => BuildWebGL(true);

        private static void BuildAndroid(bool development)
        {
            var version = ApplyVersion();
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, ApplicationIdentifier);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = version.ToAndroidVersionCode();
            PlayerSettings.Android.useCustomKeystore = false; // Debug-signed: for side-loading, not the store.
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, $"{AndroidFolder}/{ProductFileName}-{version}{Suffix(development)}.apk", development);
        }

        private static void BuildWebGL(bool development)
        {
            var version = ApplyVersion();

            // itch.io doesn't send Content-Encoding headers for Unity's files, so the loader decompresses them itself.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            // WebGPU where the browser has it (the user, 2026-11-20: it plays very well), WebGL 2 where it doesn't.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, WebGraphicsAPIs);
            var folder = $"{WebGLFolder}/{ProductFileName}-{version}{Suffix(development)}";
            Build(BuildTarget.WebGL, folder, development);

            var zip = $"{folder}-webgl.zip";
            ZipForItch(folder, zip);
            Debug.Log($"[PlayerBuilds] WebGL zip for itch.io -> {zip}");
        }

        /// <summary>
        /// Zips the build with index.html at the root, as itch.io wants, and with forward-slash entry names: itch.io's
        /// servers can't read the backslash names some Windows zip tools write.
        /// </summary>
        private static void ZipForItch(string folder, string zip)
        {
            File.Delete(zip);
            var root = Path.GetFullPath(folder);
            using var archive = ZipFile.Open(zip, ZipArchiveMode.Create);
            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                var entry = Path.GetRelativePath(root, file).Replace('\\', '/');
                archive.CreateEntryFromFile(file, entry, System.IO.Compression.CompressionLevel.Optimal);
            }
        }

        /// <summary>Takes <c>-buildVersion</c> from the command line if given, and returns the version to build.</summary>
        private static BuildVersion ApplyVersion()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, VersionArgument);
            if (index >= 0 && index + 1 < args.Length)
            {
                PlayerSettings.bundleVersion = BuildVersion.Parse(args[index + 1]).ToString();
            }

            return BuildVersion.Parse(PlayerSettings.bundleVersion);
        }

        private static string Suffix(bool development) => development ? DevSuffix : string.Empty;

        private static void Build(BuildTarget target, string output, bool development)
        {
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                throw new BuildFailedException(
                    $"The Editor is on {EditorUserBuildSettings.activeBuildTarget}; start it with --target {target}.");
            }

            // The shadow settings are code, not whatever the URP assets were left at.
            Lightning.LightningShadowSetup.Apply();
            AssetDatabase.SaveAssets();
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = output,
                target = target,
                // Development alone: no script debugging or profiler autoconnect, so it plays like the release.
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[PlayerBuilds] {target} {PlayerSettings.bundleVersion}{Suffix(development)}: {summary.result}, " +
                      $"{summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime.TotalSeconds:F0} s, " +
                      $"{summary.totalErrors} errors -> {output}");
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"{target} build {summary.result}.");
            }
        }

        /// <summary>A major.minor.patch build version.</summary>
        private readonly struct BuildVersion
        {
            private const int MaxPart = 99;

            private readonly int major;
            private readonly int minor;
            private readonly int patch;

            private BuildVersion(int major, int minor, int patch)
            {
                this.major = major;
                this.minor = minor;
                this.patch = patch;
            }

            public static BuildVersion Parse(string text)
            {
                var parts = text.Trim().Split('.');
                if (parts.Length != 3 || !int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor)
                    || !int.TryParse(parts[2], out var patch) || major < 0 || minor is < 0 or > MaxPart
                    || patch is < 0 or > MaxPart)
                {
                    throw new BuildFailedException(
                        $"Build version '{text}' must be major.minor.patch, with minor and patch 0–{MaxPart}.");
                }

                return new BuildVersion(major, minor, patch);
            }

            /// <summary>Android needs a growing integer: 0.1.0 → 100, 1.2.3 → 10203.</summary>
            public int ToAndroidVersionCode() => major * 10000 + minor * 100 + patch;

            public override string ToString() => $"{major}.{minor}.{patch}";
        }
    }
}
