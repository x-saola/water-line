using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Delta.Tools.Editor
{
    public class ProjectCloner : EditorWindow
    {
        // Kept as its own toggle: flip to false to stop auto-committing without touching the copy/rename steps.
        private const bool AUTO_CREATE_INITIAL_COMMIT = true;

        private string _newProjectName = "";
        private string _gitlabUrl = "";

        // Single-flight: only one background push is tracked at a time, which is enough for a dev tool
        // that's used one clone at a time. Static so OnGUI can check it even after the window is closed/reopened.
        private static volatile bool _pushRunning;
        private static Exception _pushError;

        [MenuItem("Tools/Delta/Clone Project")]
        public static void Open()
        {
            var window = GetWindow<ProjectCloner>("Clone Project");
            window.minSize = new Vector2(520, 240);
            window.Show();
        }

        private void OnGUI()
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 180f;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Clone this project into a new one", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _newProjectName = EditorGUILayout.TextField("New Project Name", _newProjectName);

            string sourceRoot = GetSourceRoot();
            string slug = ToKebabCase(_newProjectName);
            string destRoot = string.IsNullOrEmpty(slug) ? "" : Path.Combine(Directory.GetParent(sourceRoot).FullName, slug);

            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Destination", destRoot);

            EditorGUILayout.Space(8);
            _gitlabUrl = EditorGUILayout.TextField("GitLab Remote URL (optional)", _gitlabUrl).Trim();

            EditorGUILayout.Space(4);
            string error = Validate(_newProjectName, slug, destRoot);
            if (error != null)
                EditorGUILayout.HelpBox(error, MessageType.Error);
            else
                EditorGUILayout.HelpBox(
                    "Bundle ID, Firebase config and ad unit IDs are NOT changed by this tool.\n" +
                    "Open Tools/Delta/Technical Setup in the new project to configure them.",
                    MessageType.Info);

            if (_pushRunning)
                EditorGUILayout.HelpBox("A previous push to GitLab is still running in the background...", MessageType.Info);

            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(error != null || _pushRunning))
            {
                if (GUILayout.Button("Clone Project", GUILayout.Height(30)) && ConfirmClone(destRoot, _gitlabUrl))
                    RunClone(sourceRoot, destRoot, _newProjectName, _gitlabUrl);
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        private static bool ConfirmClone(string destRoot, string gitlabUrl)
        {
            string message = string.IsNullOrEmpty(gitlabUrl)
                ? $"This will copy the project into:\n{destRoot}\n\nThis may take a moment. Continue?"
                : $"This will copy the project into:\n{destRoot}\n\nand push the initial commit to:\n{gitlabUrl}\n\n" +
                  "The push happens in the background, so the Editor stays responsive — you'll get a " +
                  "separate notification once it finishes (or fails). Continue?";

            return EditorUtility.DisplayDialog("Clone Project", message, "Clone", "Cancel");
        }

        private static string GetSourceRoot() => Directory.GetParent(Application.dataPath).FullName;

        private static string ToKebabCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            string lowered = input.Trim().ToLowerInvariant();
            string dashed = Regex.Replace(lowered, @"[\s_]+", "-");
            string stripped = Regex.Replace(dashed, @"[^a-z0-9-]", "");
            return Regex.Replace(stripped, @"-+", "-").Trim('-');
        }

        private static string Validate(string newProjectName, string slug, string destRoot)
        {
            if (string.IsNullOrEmpty(newProjectName))
                return "Enter a project name.";
            if (string.IsNullOrEmpty(slug))
                return "Project name must contain at least one letter, digit or hyphen.";
            if (Directory.Exists(destRoot) || File.Exists(destRoot))
                return $"A folder already exists at: {destRoot}";
            return null;
        }

        private static void RunClone(string sourceRoot, string destRoot, string productName, string gitlabUrl)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Clone Project", "Collecting file list...", 0f);
                List<string> files = GetFilesToCopy(sourceRoot);

                CopyFiles(sourceRoot, destRoot, files);

                EditorUtility.DisplayProgressBar("Clone Project", "Initializing git repository...", 0.95f);
                GitInit(destRoot);

                EditorUtility.DisplayProgressBar("Clone Project", "Renaming product...", 0.97f);
                RenameProductName(Path.Combine(destRoot, "ProjectSettings", "ProjectSettings.asset"), productName);

                bool willPush = false;
                if (AUTO_CREATE_INITIAL_COMMIT)
                {
                    EditorUtility.DisplayProgressBar("Clone Project", "Creating initial commit...", 0.99f);
                    CreateInitialCommit(destRoot);

                    willPush = !string.IsNullOrEmpty(gitlabUrl);
                    if (willPush)
                        StartPushInBackground(destRoot, gitlabUrl);
                }

                EditorUtility.ClearProgressBar();
                ShowLocalCloneDialog(destRoot, files.Count, gitlabUrl, willPush);
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Clone Project Failed",
                    $"{e.Message}\n\nDestination (possibly partial): {destRoot}", "OK");
                Debug.LogException(e);
            }
        }

        private static void ShowLocalCloneDialog(string destRoot, int fileCount, string gitlabUrl, bool willPush)
        {
            string message = $"Cloned {fileCount} files to:\n{destRoot}\n\n" +
                "Bundle ID, Firebase config and ad unit IDs were NOT changed — " +
                "open the new project and use Tools/Delta/Technical Setup to configure them.";

            if (willPush)
                message += $"\n\nPushing initial commit to {gitlabUrl} in the background — you'll get a separate notification when it finishes.";

            bool openInFinder = EditorUtility.DisplayDialog("Clone Project", message, "Reveal in Finder", "Close");

            if (openInFinder)
                EditorUtility.RevealInFinder(destRoot);
        }

        // Runs the push on a thread-pool thread so a slow/dead remote can't freeze the Editor's main thread —
        // EditorApplication.update polls _pushRunning (safe: it only reads/writes on the main thread and the
        // background thread each touch a disjoint set of fields) and shows the result once the task finishes.
        private static void StartPushInBackground(string destRoot, string gitlabUrl)
        {
            _pushRunning = true;
            _pushError = null;

            Task.Run(() =>
            {
                try { PushToGitLab(destRoot, gitlabUrl); }
                catch (Exception e) { _pushError = e; }
                finally { _pushRunning = false; }
            });

            EditorApplication.update += PollPushCompletion;

            void PollPushCompletion()
            {
                if (_pushRunning)
                    return;

                EditorApplication.update -= PollPushCompletion;

                if (_pushError == null)
                    EditorUtility.DisplayDialog("Clone Project", $"Pushed initial commit to:\n{gitlabUrl}", "OK");
                else
                    EditorUtility.DisplayDialog("Clone Project — Push Failed",
                        $"Push to {gitlabUrl} failed:\n{_pushError.Message}\n\nThe local repo is committed — push manually once resolved.",
                        "OK");
            }
        }

        // -z NUL-terminates entries so filenames with spaces/special chars (e.g. "TextMesh Pro") round-trip
        // without the quoting Git normally applies, and the result already respects .gitignore (incl. nested rules).
        private static List<string> GetFilesToCopy(string sourceRoot)
        {
            string output = RunGit(sourceRoot, "ls-files", "--cached", "--others", "--exclude-standard", "-z");
            return output.Split('\0').Where(s => s.Length > 0).ToList();
        }

        private static void CopyFiles(string sourceRoot, string destRoot, List<string> relativePaths)
        {
            Directory.CreateDirectory(destRoot);

            for (int i = 0; i < relativePaths.Count; i++)
            {
                string relativePath = relativePaths[i].Replace('/', Path.DirectorySeparatorChar);
                string sourcePath = Path.Combine(sourceRoot, relativePath);
                string destPath = Path.Combine(destRoot, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(sourcePath, destPath, true);

                if (i % 25 == 0 || i == relativePaths.Count - 1)
                    EditorUtility.DisplayProgressBar("Clone Project", $"Copying files... ({i + 1}/{relativePaths.Count})", (float)i / relativePaths.Count);
            }
        }

        private static void GitInit(string destRoot)
        {
            RunGit(destRoot, "init");
            RunGit(destRoot, "lfs", "install", "--local");
        }

        private static void RenameProductName(string projectSettingsPath, string newName)
        {
            string[] lines = File.ReadAllLines(projectSettingsPath);
            var pattern = new Regex(@"^(\s*productName:\s*).*$");

            for (int i = 0; i < lines.Length; i++)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    lines[i] = pattern.Replace(lines[i], $"$1{newName}");
                    break;
                }
            }

            File.WriteAllLines(projectSettingsPath, lines);
        }

        private static void CreateInitialCommit(string destRoot)
        {
            RunGit(destRoot, "add", "-A");
            RunGit(destRoot, "commit", "-m", "Initial commit from Atom template");
        }

        // Runs on a background thread (see StartPushInBackground), so this can afford to be generous —
        // it only guards against a truly stuck connection, not the Editor's responsiveness. A large
        // project on a slow link can legitimately take several minutes to push.
        private const int PUSH_TIMEOUT_MS = 10 * 60 * 1000;

        private static void PushToGitLab(string destRoot, string remoteUrl)
        {
            RunGit(destRoot, "remote", "add", "origin", remoteUrl);

            ProcessStartInfo startInfo = CreateGitProcessStartInfo(destRoot, "push", "-u", "origin", "HEAD");
            // Fail fast instead of hanging the Editor forever if GitLab needs credentials we can't supply.
            startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";

            using (var process = Process.Start(startInfo))
            {
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(PUSH_TIMEOUT_MS))
                {
                    process.Kill();
                    throw new TimeoutException($"git push did not finish within {PUSH_TIMEOUT_MS / 1000}s.");
                }

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"git push failed: {errorTask.Result}");
            }
        }

        // The Unity Editor process (esp. on macOS, launched from Finder/Dock) often runs with a
        // minimal PATH that resolves the system `git` but not `git-lfs` installed via Homebrew/MacPorts.
        private static readonly string[] ExtraGitSearchPaths = { "/opt/homebrew/bin", "/usr/local/bin", "/opt/local/bin" };

        private static ProcessStartInfo CreateGitProcessStartInfo(string workingDirectory, params string[] args)
        {
            var startInfo = new ProcessStartInfo("git")
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
            };

            foreach (string arg in args)
                startInfo.ArgumentList.Add(arg);

            string path = startInfo.EnvironmentVariables["PATH"] ?? "";
            string[] existingPaths = path.Split(Path.PathSeparator);
            foreach (string extra in ExtraGitSearchPaths)
            {
                if (Directory.Exists(extra) && !existingPaths.Contains(extra))
                    path += Path.PathSeparator + extra;
            }
            startInfo.EnvironmentVariables["PATH"] = path;

            return startInfo;
        }

        private static string RunGit(string workingDirectory, params string[] args)
        {
            ProcessStartInfo startInfo = CreateGitProcessStartInfo(workingDirectory, args);

            using (var process = Process.Start(startInfo))
            {
                // Read both streams concurrently before WaitForExit — reading them sequentially can deadlock
                // if one pipe's buffer fills while the process is blocked writing to the other.
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                process.WaitForExit();

                string output = outputTask.Result;
                string error = errorTask.Result;

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"git {string.Join(" ", args)} failed: {error}");

                return output;
            }
        }
    }
}
