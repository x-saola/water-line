#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using static VFolders.Libs.VUtils;
using static VFolders.Libs.VGUI;
using static VFolders.VFolders;

namespace VFolders
{
    // A standalone companion window rather than an overlay drawn inside the Project window itself:
    // ProjectBrowser is a legacy pure-IMGUI window with no public extension point for a persistent toolbar,
    // so injecting one would require patching its private layout internals (fragile across Unity versions).
    // Dock this window above/beside the Project tab for the same "browser toolbar" feel.
    public class VFoldersBookmark : EditorWindow
    {
        [MenuItem("Tools/vFolders/Bookmarks Window", false, 50)]
        static void Open() => GetWindow<VFoldersBookmark>();

        List<string> history = new List<string>();
        int historyIndex = -1;

        bool showSearch;
        string searchQuery = "";
        Vector2 contentScroll;

        const int bookmarkColumns = 2;

        GUIStyle _currentTabStyle;
        GUIStyle currentTabStyle => _currentTabStyle ??= new GUIStyle(EditorStyles.toolbarButton) { fontStyle = FontStyle.Bold };

        void OnEnable()
        {
            titleContent = new GUIContent("Bookmarks Window", EditorGUIUtility.IconContent("Project").image);
            minSize = new Vector2(200, 40);

            Selection.selectionChanged += OnSelectionChanged;
            RecordCurrentIfFolder();
        }
        void OnDisable() => Selection.selectionChanged -= OnSelectionChanged;

        void OnSelectionChanged()
        {
            RecordCurrentIfFolder();
            Repaint();
        }

        static string GetSelectedFolderPath()
        {
            if (!Selection.activeObject) return null;

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !path.IsNullOrEmpty() && AssetDatabase.IsValidFolder(path) ? path : null;
        }

        void RecordCurrentIfFolder()
        {
            var path = GetSelectedFolderPath();
            if (path == null) return;
            if (historyIndex.IsInRangeOf(history) && history[historyIndex] == path) return;

            if (historyIndex < history.Count - 1)
                history.RemoveRange(historyIndex + 1, history.Count - historyIndex - 1);

            history.Add(path);
            historyIndex = history.Count - 1;
        }

        static void NavigateToFolder(string path)
        {
            if (path.IsNullOrEmpty()) return;
            if (!AssetDatabase.IsValidFolder(path)) return;

            var folder = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (!folder) return;

            PingObject(folder, select: true, focusProjectWindow: true);
        }

        void GoBack()
        {
            if (historyIndex <= 0) return;
            historyIndex--;
            NavigateToFolder(history[historyIndex]);
        }
        void GoForward()
        {
            if (historyIndex >= history.Count - 1) return;
            historyIndex++;
            NavigateToFolder(history[historyIndex]);
        }

        string currentFolderPath => historyIndex.IsInRangeOf(history) ? history[historyIndex] : null;

        void PruneMissingBookmarks()
        {
            if (!data) return;

            if (data.bookmarkedFolderGuids.RemoveAll(guid => guid.ToPath().IsNullOrEmpty()) == 0) return;

            data.Dirty();
            data.Save();
        }

        void ToggleBookmarkCurrentFolder()
        {
            if (!data) return;

            var path = currentFolderPath;
            if (path.IsNullOrEmpty()) return;

            var guid = path.ToGuid();

            if (!data.bookmarkedFolderGuids.Remove(guid))
                data.bookmarkedFolderGuids.Add(guid);

            data.Dirty();
            data.Save();
        }

        void RemoveBookmark(string guid)
        {
            if (!data) return;
            if (!data.bookmarkedFolderGuids.Remove(guid)) return;

            data.Dirty();
            data.Save();
        }

        void ToolbarGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            SetGUIEnabled(historyIndex > 0);
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("back").image, "Back"), EditorStyles.toolbarButton, GUILayout.Width(24)))
                GoBack();
            ResetGUIEnabled();

            SetGUIEnabled(historyIndex < history.Count - 1);
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("forward").image, "Forward"), EditorStyles.toolbarButton, GUILayout.Width(24)))
                GoForward();
            ResetGUIEnabled();

            GUILayout.FlexibleSpace();

            var currentIsBookmarked = data && !currentFolderPath.IsNullOrEmpty() && data.bookmarkedFolderGuids.Contains(currentFolderPath.ToGuid());

            SetGUIEnabled(data && !currentFolderPath.IsNullOrEmpty());
            var addBookmarkIcon = currentIsBookmarked ? "Toolbar Minus" : "Toolbar Plus";
            var addBookmarkTooltip = currentIsBookmarked ? "Remove bookmark for current folder" : "Bookmark current folder";
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(addBookmarkIcon).image, addBookmarkTooltip), EditorStyles.toolbarButton, GUILayout.Width(24)))
                ToggleBookmarkCurrentFolder();
            ResetGUIEnabled();

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("Search Icon").image, "Quick search"), EditorStyles.toolbarButton, GUILayout.Width(24)))
                showSearch = !showSearch;

            GUILayout.EndHorizontal();
        }

        // Wraps into additional rows (rather than scrolling sideways) once bookmarks exceed bookmarkColumns per row.
        void BookmarksGUI()
        {
            if (!data) return;
            if (data.bookmarkedFolderGuids.Count == 0) return;

            var guids = data.bookmarkedFolderGuids;
            string rightClickedGuid = null;

            for (var row = 0; row * bookmarkColumns < guids.Count; row++)
            {
                GUILayout.BeginHorizontal();

                for (var col = 0; col < bookmarkColumns; col++)
                {
                    var index = row * bookmarkColumns + col;
                    if (index >= guids.Count) break;

                    var guid = guids[index];
                    var path = guid.ToPath();
                    if (path.IsNullOrEmpty()) continue;

                    var isCurrent = path == currentFolderPath;
                    var content = new GUIContent(Path.GetFileName(path), path);

                    if (GUILayout.Button(content, isCurrent ? currentTabStyle : EditorStyles.toolbarButton, GUILayout.ExpandWidth(true)))
                        NavigateToFolder(path);

                    // Don't call GenericMenu.ShowAsContext() here: it can throw ExitGUIException to unwind the
                    // GUI call stack immediately, which would skip the EndHorizontal calls for the remaining rows
                    // and corrupt GUILayout's group stack for the next repaint. Defer it until after they've run.
                    if (curEvent.isContextClick && GUILayoutUtility.GetLastRect().Contains(curEvent.mousePosition))
                    {
                        rightClickedGuid = guid;
                        curEvent.Use();
                    }
                }

                GUILayout.EndHorizontal();
            }

            if (rightClickedGuid != null)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove Bookmark"), false, () => RemoveBookmark(rightClickedGuid));
                menu.ShowAsContext();
            }
        }

        void SearchGUI()
        {
            if (!showSearch) return;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUI.SetNextControlName("vFoldersNavigatorSearch");
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);

            if (!searchQuery.IsNullOrEmpty() && GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20)))
                searchQuery = "";

            GUILayout.EndHorizontal();

            if (searchQuery.IsNullOrEmpty()) return;

            var results = AssetDatabase.FindAssets(searchQuery)
                                        .Distinct()
                                        .Select(guid => guid.ToPath())
                                        .Where(path => !path.IsNullOrEmpty())
                                        .Take(30)
                                        .ToList();

            foreach (var path in results)
            {
                if (!GUILayout.Button(path, EditorStyles.label)) continue;

                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (!obj) continue;

                PingObject(obj, select: true, focusProjectWindow: true);

                if (AssetDatabase.IsValidFolder(path))
                    NavigateToFolder(path);
            }
        }

        void OnGUI()
        {
            PruneMissingBookmarks();

            ToolbarGUI();

            // Bookmarks now wrap into rows instead of scrolling sideways, so their total height grows with the
            // count. Without this scroll view, rows beyond the window's current height were silently drawn past
            // the bottom edge and invisible - not an actual cap on how many bookmarks could be added.
            contentScroll = EditorGUILayout.BeginScrollView(contentScroll);
            BookmarksGUI();
            SearchGUI();
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
