using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Delta.ProjectName;

namespace Delta.LevelEditor
{
    // Which tap-a-cell action is currently active in the tool palette.
    internal enum LevelEditorTool
    {
        PlaceA,
        PlaceB,
        PlaceC,
        Rock,
        Boulder,
        Valve,
        PlusOnePipe,
    }

    // Designer tool for authoring Waterline levels. Mirrors bubble-pond-association's
    // LevelEditorWindow (UI Toolkit + a .uss stylesheet, per-level level_N.json files, prev/next/
    // jump navigation, dirty-tracking Save) with the Categories/Bubbles/sprite-picker columns
    // replaced by a dimension row, a budget row (pipe lengths + timer + third anchor), a tool
    // palette, and a grid board - the parts specific to a pipe-connection puzzle. There is no
    // LevelDatabase here (unlike bubble-pond's): Waterline has no player-facing level sequence, so
    // saved levels are just JSON files browsed from this window, not indexed for runtime progression.
    public class LevelEditorWindow : EditorWindow
    {
        const string LevelsFolder = "Assets/_Project/Levels";
        const string USS_PATH = "Assets/_LevelEditor/Editor/LevelEditorWindow.uss";
        const string LastPathPrefKey = "WaterlineLevelEditor.LastPath";

        const int DefaultMoveBudget = 5;
        const int MaxMoveBudget = 30;
        const int MaxTimeLimitSeconds = 600;
        const int TimeLimitStepSeconds = 5;

        // ─── State ────────────────────────────────────────────────────

        LevelConfig _config;
        string _loadedPath;
        bool _dirty;
        readonly Dictionary<string, string> _levelPathMap = new();

        LevelEditorTool _selectedTool = LevelEditorTool.PlaceA;
        readonly Dictionary<LevelEditorTool, Button> _toolButtons = new();

        // ─── UI refs ──────────────────────────────────────────────────

        TextField _levelIdField;
        TextField _levelNameField;
        Label _pathLabel;
        Label _dirtyDot;
        Button _saveBtn;
        IntegerField _levelJumpField;
        DropdownField _loadDropdown;
        VisualElement _dimensionContainer;
        VisualElement _budgetContainer;
        VisualElement _toolPaletteContainer;
        VisualElement _boardContainer;
        VisualElement _validationContainer;
        VisualElement _importShield;
        TextField _importTextField;

        // ─── Entry point ──────────────────────────────────────────────

        // Opened from MenuItems.OpenLevelEditor (keeps the project's menu ordering/priority).
        public static void Open()
        {
            var win = GetWindow<LevelEditorWindow>("Level Editor");
            win.minSize = new Vector2(760, 540);
            win.Show();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (uss != null) root.styleSheets.Add(uss);

            root.Add(BuildToolbar());
            root.Add(BuildLevelBar());

            var body = new VisualElement();
            body.AddToClassList("editor-body");

            var leftCol = new VisualElement();
            leftCol.AddToClassList("left-col");
            leftCol.Add(BuildPanelCard("BOARD SIZE", BuildDimensionRow()));
            leftCol.Add(BuildPanelCard("PIPES & TIMER", BuildBudgetRow()));
            leftCol.Add(BuildPanelCard("TOOLS", BuildToolPalette()));

            _validationContainer = new VisualElement();
            leftCol.Add(BuildPanelCard("VALIDATION", _validationContainer));

            body.Add(leftCol);
            body.Add(BuildBoard());
            root.Add(body);

            BuildImportOverlay();

            RestoreLastLevel();
            if (_config == null) NewLevel();
        }

        // ─── Toolbar ──────────────────────────────────────────────────

        VisualElement BuildToolbar()
        {
            var bar = new VisualElement();
            bar.AddToClassList("toolbar");

            var newBtn = new Button(NewLevel) { text = "+ New" };
            newBtn.AddToClassList("btn-accent");
            bar.Add(newBtn);

            var navGroup = new VisualElement();
            navGroup.AddToClassList("toolbar-group");

            var prevBtn = new Button(LoadPrev) { text = "◀", tooltip = "Load previous level" };
            prevBtn.style.width = 26;
            navGroup.Add(prevBtn);

            _levelJumpField = new IntegerField { tooltip = "Level number — press Enter to load", isDelayed = true };
            _levelJumpField.style.width = 54;
            _levelJumpField.RegisterValueChangedCallback(evt => LoadLevelByNumber(evt.newValue));
            navGroup.Add(_levelJumpField);

            var nextBtn = new Button(LoadNext) { text = "▶", tooltip = "Load next level" };
            nextBtn.style.width = 26;
            navGroup.Add(nextBtn);

            bar.Add(navGroup);

            _saveBtn = new Button(SaveLevel) { text = "Save", tooltip = "Save level to JSON" };
            _saveBtn.AddToClassList("btn-save");
            bar.Add(_saveBtn);

            var extraGroup = new VisualElement();
            extraGroup.AddToClassList("toolbar-group");
            extraGroup.Add(new Button(ExportJson) { text = "Export", tooltip = "Copy all saved levels to the clipboard as JSON" });
            extraGroup.Add(new Button(ShowImportOverlay) { text = "Import", tooltip = "Paste a JSON array of levels to merge into the saved levels" });
            bar.Add(extraGroup);

            _pathLabel = new Label("(unsaved)");
            _pathLabel.AddToClassList("path-label");
            bar.Add(_pathLabel);

            _dirtyDot = new Label("●") { tooltip = "Unsaved changes" };
            _dirtyDot.AddToClassList("dirty-dot");
            _dirtyDot.style.display = DisplayStyle.None;
            bar.Add(_dirtyDot);

            _loadDropdown = new DropdownField();
            _loadDropdown.style.minWidth = 150;
            _loadDropdown.style.marginLeft = 4;
            _loadDropdown.RegisterValueChangedCallback(evt =>
            {
                if (_levelPathMap.TryGetValue(evt.newValue, out string path))
                    LoadLevel(path);
                _loadDropdown.SetValueWithoutNotify("— Load Level —");
            });
            bar.Add(_loadDropdown);

            RefreshLevelMap();
            return bar;
        }

        // ─── Level bar (id / name) ─────────────────────────────────────

        VisualElement BuildLevelBar()
        {
            var bar = new VisualElement();
            bar.AddToClassList("level-bar");

            _levelIdField = new TextField("ID") { isDelayed = true };
            _levelIdField.AddToClassList("level-id-field");
            _levelIdField.tooltip = "String id, e.g. level_1. Saved as <ID>.json";
            _levelIdField.RegisterValueChangedCallback(e =>
            {
                if (_config == null) return;
                _config.LevelId = e.newValue;
                MarkDirty();
            });
            bar.Add(_levelIdField);

            _levelNameField = new TextField("Name");
            _levelNameField.AddToClassList("level-name-field");
            _levelNameField.RegisterValueChangedCallback(e =>
            {
                if (_config == null) return;
                _config.LevelName = e.newValue;
                MarkDirty();
            });
            bar.Add(_levelNameField);

            return bar;
        }

        // ─── Panel card helper ──────────────────────────────────────────

        // Every left-column section (dimensions, budget, tools, validation) renders as a titled
        // card so the panel reads as distinct groups instead of one long stack of controls.
        static VisualElement BuildPanelCard(string title, VisualElement content)
        {
            var card = new VisualElement();
            card.AddToClassList("panel-card");

            var lbl = new Label(title);
            lbl.AddToClassList("panel-title");
            card.Add(lbl);

            card.Add(content);
            return card;
        }

        // ─── Dimension row (Columns / Rows) ────────────────────────────

        VisualElement BuildDimensionRow()
        {
            _dimensionContainer = new VisualElement();
            _dimensionContainer.AddToClassList("dimension-row");
            return _dimensionContainer;
        }

        void RebuildDimensionRow()
        {
            if (_dimensionContainer == null || _config == null) return;
            _dimensionContainer.Clear();

            _dimensionContainer.Add(BuildStepper("Columns", _config.Columns,
                LevelValidator.MinColumns, LevelValidator.MaxColumns, 1, null,
                v => TrySetDimensions(v, _config.Rows)));

            _dimensionContainer.Add(BuildStepper("Rows", _config.Rows,
                LevelValidator.MinRows, LevelValidator.MaxRows, 1, null,
                v => TrySetDimensions(_config.Columns, v)));
        }

        // Pipes are never silently relocated by a resize - shrinking the grid out from under one
        // is blocked outright. Obstacles are cheap to repaint, so those just ask for confirmation.
        void TrySetDimensions(int newColumns, int newRows)
        {
            if (newColumns == _config.Columns && newRows == _config.Rows) return;

            foreach (var pipe in _config.Pipes)
            {
                if (pipe.StartCell.Column >= newColumns || pipe.StartCell.Row >= newRows)
                {
                    EditorUtility.DisplayDialog("Can't resize",
                        $"Pipe {pipe.Id}'s start cell {pipe.StartCell} would be outside a {newColumns}x{newRows} grid. Move it first.",
                        "OK");
                    return;
                }
            }

            int strandedObstacles = _config.Obstacles.Count(o => o.Cell.Column >= newColumns || o.Cell.Row >= newRows);
            if (strandedObstacles > 0 &&
                !EditorUtility.DisplayDialog("Resize board",
                    $"{strandedObstacles} obstacle(s) are outside the new {newColumns}x{newRows} size and will be removed. Continue?",
                    "Remove & Resize", "Cancel"))
                return;

            _config.Obstacles.RemoveAll(o => o.Cell.Column >= newColumns || o.Cell.Row >= newRows);
            _config.Columns = newColumns;
            _config.Rows = newRows;
            MarkDirty();
            RebuildDimensionRow();
            RebuildBoard();
            RebuildValidationContainer();
        }

        // ─── Budget row (pipe lengths, timer, third anchor) ────────────

        VisualElement BuildBudgetRow()
        {
            _budgetContainer = new VisualElement();
            _budgetContainer.AddToClassList("budget-row");
            return _budgetContainer;
        }

        void RebuildBudgetRow()
        {
            if (_budgetContainer == null || _config == null) return;
            _budgetContainer.Clear();

            var pipeA = GetPipe(PipeId.A);
            var pipeB = GetPipe(PipeId.B);
            var pipeC = GetPipe(PipeId.C);

            if (pipeA != null)
                _budgetContainer.Add(BuildStepper("Pipe A length", pipeA.MoveBudget, 1, MaxMoveBudget, 1, null,
                    v => SetPipeBudget(PipeId.A, v)));
            if (pipeB != null)
                _budgetContainer.Add(BuildStepper("Pipe B length", pipeB.MoveBudget, 1, MaxMoveBudget, 1, null,
                    v => SetPipeBudget(PipeId.B, v)));

            if (pipeC != null)
            {
                _budgetContainer.Add(BuildStepper("Pipe C length", pipeC.MoveBudget, 1, MaxMoveBudget, 1, null,
                    v => SetPipeBudget(PipeId.C, v)));
                var removeBtn = new Button(RemoveThirdAnchor) { text = "× Remove third anchor" };
                removeBtn.AddToClassList("remove-anchor-btn");
                _budgetContainer.Add(removeBtn);
            }
            else
            {
                var addBtn = new Button(AddThirdAnchor) { text = "+ Add third anchor" };
                addBtn.AddToClassList("add-anchor-btn");
                _budgetContainer.Add(addBtn);
            }

            _budgetContainer.Add(BuildStepper("Time limit", _config.TimeLimitSeconds,
                0, MaxTimeLimitSeconds, TimeLimitStepSeconds, FormatTimeLimit, SetTimeLimit));
        }

        static string FormatTimeLimit(int seconds)
        {
            if (seconds <= 0) return "Off";
            return seconds < 60 ? $"{seconds}s" : $"{seconds / 60}:{seconds % 60:00}";
        }

        void SetPipeBudget(PipeId id, int value)
        {
            var pipe = GetPipe(id);
            if (pipe == null || pipe.MoveBudget == value) return;
            pipe.MoveBudget = value;
            MarkDirty();
            RebuildBudgetRow();
            RebuildBoard();
            RebuildValidationContainer();
        }

        void SetTimeLimit(int value)
        {
            if (_config.TimeLimitSeconds == value) return;
            _config.TimeLimitSeconds = value;
            MarkDirty();
            RebuildBudgetRow();
            RebuildValidationContainer();
        }

        void AddThirdAnchor()
        {
            if (GetPipe(PipeId.C) != null) return;
            _config.Pipes.Add(new PipeConfig { Id = PipeId.C, StartCell = FindFirstFreeCell(), MoveBudget = DefaultMoveBudget });
            MarkDirty();
            RebuildBudgetRow();
            RebuildToolPalette();
            RebuildBoard();
            RebuildValidationContainer();
        }

        void RemoveThirdAnchor()
        {
            _config.Pipes.RemoveAll(p => p.Id == PipeId.C);
            MarkDirty();
            RebuildBudgetRow();
            RebuildToolPalette();
            RebuildBoard();
            RebuildValidationContainer();
        }

        PipeConfig GetPipe(PipeId id) => _config?.Pipes?.FirstOrDefault(p => p.Id == id);

        GridCell FindFirstFreeCell()
        {
            for (int r = 0; r < _config.Rows; r++)
                for (int c = 0; c < _config.Columns; c++)
                {
                    var cell = new GridCell(r, c);
                    if (!IsOccupied(cell)) return cell;
                }
            return new GridCell(0, 0);
        }

        bool IsOccupied(GridCell cell)
        {
            if (_config.Pipes.Any(p => p.StartCell.Row == cell.Row && p.StartCell.Column == cell.Column)) return true;
            if (_config.Obstacles.Any(o => o.Cell.Row == cell.Row && o.Cell.Column == cell.Column)) return true;
            return false;
        }

        // ─── Reusable stepper ───────────────────────────────────────────

        // No stepper control exists anywhere in the framework (checked) - this generalizes the
        // inline −/label/+ pattern bubble-pond's editor uses ad hoc for category item count.
        // Always rebuilds via the caller's onChanged + a container Rebuild*, rather than mutating
        // its own label, so the displayed value can never drift from the accepted _config state
        // (e.g. a resize the user cancels never has to be visually un-done).
        static VisualElement BuildStepper(string label, int value, int min, int max, int step,
            Func<int, string> format, Action<int> onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("stepper-row");

            var lbl = new Label(label);
            lbl.AddToClassList("stepper-label");
            row.Add(lbl);

            var stepperBox = new VisualElement();
            stepperBox.AddToClassList("stepper");

            var minusBtn = new Button(() => onChanged(Mathf.Clamp(value - step, min, max))) { text = "−" };
            minusBtn.AddToClassList("stepper-btn");
            minusBtn.SetEnabled(value > min);
            stepperBox.Add(minusBtn);

            if (format != null)
            {
                // Time limit's formatted string ("1:30", "Off") isn't something a designer can
                // usefully type back in as raw seconds, so it stays a plain display label.
                var valueLabel = new Label(format(value));
                valueLabel.AddToClassList("stepper-value");
                stepperBox.Add(valueLabel);
            }
            else
            {
                var valueField = new IntegerField { isDelayed = true, value = value };
                valueField.AddToClassList("stepper-value");
                valueField.AddToClassList("stepper-value-field");
                valueField.RegisterValueChangedCallback(evt =>
                {
                    int clamped = Mathf.Clamp(evt.newValue, min, max);
                    if (clamped != value) onChanged(clamped);
                    else valueField.SetValueWithoutNotify(clamped); // clamped back to the current value - nothing to rebuild, just undo the stray keystrokes
                });
                stepperBox.Add(valueField);
            }

            var plusBtn = new Button(() => onChanged(Mathf.Clamp(value + step, min, max))) { text = "+" };
            plusBtn.AddToClassList("stepper-btn");
            plusBtn.SetEnabled(value < max);
            stepperBox.Add(plusBtn);

            row.Add(stepperBox);
            return row;
        }

        // ─── Tool palette ───────────────────────────────────────────────

        VisualElement BuildToolPalette()
        {
            _toolPaletteContainer = new VisualElement();
            _toolPaletteContainer.AddToClassList("tool-palette");
            return _toolPaletteContainer;
        }

        void RebuildToolPalette()
        {
            if (_toolPaletteContainer == null || _config == null) return;

            // A removed Pipe C can't leave the palette pointed at a tool it no longer offers.
            if (_selectedTool == LevelEditorTool.PlaceC && GetPipe(PipeId.C) == null)
                _selectedTool = LevelEditorTool.PlaceA;

            _toolPaletteContainer.Clear();
            _toolButtons.Clear();

            AddToolButton(LevelEditorTool.PlaceA, "Place A");
            AddToolButton(LevelEditorTool.PlaceB, "Place B");
            if (GetPipe(PipeId.C) != null)
                AddToolButton(LevelEditorTool.PlaceC, "Place C");

            AddToolButton(LevelEditorTool.Rock, "Rock");
            AddToolButton(LevelEditorTool.Boulder, "Boulder");
            AddToolButton(LevelEditorTool.Valve, "Valve");
            AddToolButton(LevelEditorTool.PlusOnePipe, "+1 Pipe");

            ApplyToolHighlight();
        }

        void AddToolButton(LevelEditorTool tool, string label)
        {
            var btn = new Button(() => SelectTool(tool)) { text = label };
            btn.AddToClassList("tool-btn");
            // Ties each tool's active color to its board-cell color (board-cell--anchor-a etc.)
            // so the palette doubles as a legend for what's painted on the grid.
            btn.AddToClassList($"tool-btn--{tool.ToString().ToLowerInvariant()}");
            _toolButtons[tool] = btn;
            _toolPaletteContainer.Add(btn);
        }

        void SelectTool(LevelEditorTool tool)
        {
            _selectedTool = tool;
            ApplyToolHighlight();
        }

        void ApplyToolHighlight()
        {
            foreach (var kv in _toolButtons)
                kv.Value.EnableInClassList("tool-btn--active", kv.Key == _selectedTool);
        }

        // ─── Board ──────────────────────────────────────────────────────

        VisualElement BuildBoard()
        {
            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.AddToClassList("board-scroll-container");
            _boardContainer = new VisualElement();
            _boardContainer.AddToClassList("board-container");
            scroll.Add(_boardContainer);
            return scroll;
        }

        void RebuildBoard()
        {
            if (_boardContainer == null || _config == null) return;
            _boardContainer.Clear();

            for (int r = 0; r < _config.Rows; r++)
            {
                var rowVE = new VisualElement();
                rowVE.AddToClassList("board-row");
                for (int c = 0; c < _config.Columns; c++)
                    rowVE.Add(BuildCell(new GridCell(r, c)));
                _boardContainer.Add(rowVE);
            }
        }

        VisualElement BuildCell(GridCell cell)
        {
            var cellVE = new VisualElement();
            cellVE.AddToClassList("board-cell");
            cellVE.RegisterCallback<PointerDownEvent>(_ => OnCellClicked(cell));

            var pipe = _config.Pipes.FirstOrDefault(p => p.StartCell.Row == cell.Row && p.StartCell.Column == cell.Column);
            if (pipe != null)
            {
                cellVE.AddToClassList($"board-cell--anchor-{pipe.Id.ToString().ToLowerInvariant()}");
                var digit = new Label(pipe.MoveBudget.ToString()) { pickingMode = PickingMode.Ignore };
                digit.AddToClassList("anchor-digit");
                cellVE.Add(digit);
                return cellVE;
            }

            var obstacle = _config.Obstacles.FirstOrDefault(o => o.Cell.Row == cell.Row && o.Cell.Column == cell.Column);
            if (obstacle != null)
            {
                cellVE.AddToClassList($"board-cell--{obstacle.Type.ToString().ToLowerInvariant()}");
                var glyph = new Label(ObstacleGlyph(obstacle.Type)) { pickingMode = PickingMode.Ignore };
                glyph.AddToClassList("obstacle-glyph");
                cellVE.Add(glyph);
                return cellVE;
            }

            cellVE.AddToClassList("board-cell--empty");
            var dot = new Label("·") { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("empty-dot");
            cellVE.Add(dot);
            return cellVE;
        }

        static string ObstacleGlyph(ObstacleType type) => type switch
        {
            ObstacleType.Rock => "▲",
            ObstacleType.Boulder => "●",
            ObstacleType.Valve => "◎",
            ObstacleType.PlusOnePipe => "+1",
            _ => "?",
        };

        void OnCellClicked(GridCell cell)
        {
            switch (_selectedTool)
            {
                case LevelEditorTool.PlaceA: PlaceAnchor(PipeId.A, cell); break;
                case LevelEditorTool.PlaceB: PlaceAnchor(PipeId.B, cell); break;
                case LevelEditorTool.PlaceC: PlaceAnchor(PipeId.C, cell); break;
                default: ToggleObstacle(cell, ToolToObstacleType(_selectedTool)); break;
            }
        }

        static ObstacleType ToolToObstacleType(LevelEditorTool tool) => tool switch
        {
            LevelEditorTool.Rock => ObstacleType.Rock,
            LevelEditorTool.Boulder => ObstacleType.Boulder,
            LevelEditorTool.Valve => ObstacleType.Valve,
            LevelEditorTool.PlusOnePipe => ObstacleType.PlusOnePipe,
            _ => ObstacleType.Rock,
        };

        void PlaceAnchor(PipeId id, GridCell cell)
        {
            var pipe = GetPipe(id);
            if (pipe == null) return;

            var otherPipe = _config.Pipes.FirstOrDefault(p =>
                p.Id != id && p.StartCell.Row == cell.Row && p.StartCell.Column == cell.Column);
            if (otherPipe != null)
            {
                Debug.LogWarning($"[LevelEditor] Cell already holds Pipe {otherPipe.Id}.");
                return;
            }

            // Anchors win over obstacles - placing one here clears whatever obstacle was in the way.
            _config.Obstacles.RemoveAll(o => o.Cell.Row == cell.Row && o.Cell.Column == cell.Column);
            pipe.StartCell = cell;
            MarkDirty();
            RebuildBoard();
            RebuildValidationContainer();
        }

        void ToggleObstacle(GridCell cell, ObstacleType type)
        {
            if (_config.Pipes.Any(p => p.StartCell.Row == cell.Row && p.StartCell.Column == cell.Column))
            {
                Debug.LogWarning("[LevelEditor] Move the anchor first — obstacles can't be placed under a pipe.");
                return;
            }

            var existing = _config.Obstacles.FirstOrDefault(o => o.Cell.Row == cell.Row && o.Cell.Column == cell.Column);
            if (existing != null) _config.Obstacles.Remove(existing);
            if (existing == null || existing.Type != type)
                _config.Obstacles.Add(new ObstacleConfig { Cell = cell, Type = type });

            MarkDirty();
            RebuildBoard();
            RebuildValidationContainer();
        }

        // ─── Validation rendering ───────────────────────────────────────

        void RebuildValidationContainer()
        {
            if (_validationContainer == null) return;
            _validationContainer.Clear();
            if (_config == null) return;

            var result = LevelValidator.Validate(_config);

            if (result.IsValid && result.Warnings.Count == 0)
            {
                var box = new VisualElement();
                box.AddToClassList("validation-box");
                box.AddToClassList("validation-box--ok");
                var ok = new Label("✓ Level is valid");
                ok.AddToClassList("validation-ok-label");
                box.Add(ok);
                _validationContainer.Add(box);
                return;
            }

            if (result.Errors.Count > 0)
            {
                var box = new VisualElement();
                box.AddToClassList("validation-box");
                box.AddToClassList("validation-box--error");
                var summary = new Label($"{result.Errors.Count} error(s)");
                summary.AddToClassList("validation-summary");
                box.Add(summary);
                foreach (var error in result.Errors)
                {
                    var msg = new Label("• " + error);
                    msg.AddToClassList("validation-msg");
                    msg.AddToClassList("validation-msg--error");
                    box.Add(msg);
                }
                _validationContainer.Add(box);
            }

            if (result.Warnings.Count > 0)
            {
                var box = new VisualElement();
                box.AddToClassList("validation-box");
                box.AddToClassList("validation-box--warn");
                var summary = new Label($"{result.Warnings.Count} warning(s)");
                summary.AddToClassList("validation-summary");
                box.Add(summary);
                foreach (var warning in result.Warnings)
                {
                    var msg = new Label("• " + warning);
                    msg.AddToClassList("validation-msg");
                    msg.AddToClassList("validation-msg--warn");
                    box.Add(msg);
                }
                _validationContainer.Add(box);
            }
        }

        // ─── New / Load / Save ──────────────────────────────────────────

        void NewLevel()
        {
            int n = NextLevelNumber();
            _config = new LevelConfig
            {
                LevelId = $"level_{n}",
                LevelName = $"Level {n}",
                Columns = 6,
                Rows = 9,
                TimeLimitSeconds = 0,
                Pipes = new List<PipeConfig>
                {
                    new() { Id = PipeId.A, StartCell = new GridCell(2, 2), MoveBudget = DefaultMoveBudget },
                    new() { Id = PipeId.B, StartCell = new GridCell(2, 3), MoveBudget = DefaultMoveBudget },
                },
                Obstacles = new List<ObstacleConfig>(),
            };
            _loadedPath = null;
            RefreshAll();
            ClearDirty();
        }

        void LoadLevel(string assetPath)
        {
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (ta == null) return;
            try
            {
                var cfg = JsonConvert.DeserializeObject<LevelConfig>(ta.text, JsonSettings);
                if (cfg == null) return;
                _config = cfg;
                _loadedPath = assetPath;
                EditorPrefs.SetString(LastPathPrefKey, assetPath);
                RefreshAll();
                ClearDirty();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelEditor] Load error ({assetPath}): {e.Message}");
            }
        }

        void SaveLevel()
        {
            if (_config == null) return;

            var result = LevelValidator.Validate(_config);
            if (!result.IsValid)
                Debug.LogWarning($"[LevelEditor] Saved with {result.Errors.Count} validation error(s) — see the Validation panel.");

            if (string.IsNullOrEmpty(_config.LevelId)) _config.LevelId = $"level_{NextLevelNumber()}";
            EnsureAssetFolder(LevelsFolder);

            string path = _loadedPath;
            if (string.IsNullOrEmpty(path))
                path = $"{LevelsFolder}/{_config.LevelId}.json";

            File.WriteAllText(
                Path.Combine(Directory.GetCurrentDirectory(), path),
                JsonConvert.SerializeObject(_config, JsonSettings));

            _loadedPath = path;
            AssetDatabase.ImportAsset(path);
            AssetDatabase.SaveAssets();

            EditorPrefs.SetString(LastPathPrefKey, path);
            RefreshLevelMap();
            UpdatePathLabel();
            ClearDirty();
            FlashSaved(path);
            Debug.Log($"[LevelEditor] Saved → {path}");
        }

        int NextLevelNumber()
        {
            int max = 0;
            if (AssetDatabase.IsValidFolder(LevelsFolder))
                foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LevelsFolder }))
                {
                    var name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                    if (name.StartsWith("level_") && int.TryParse(name.Substring(6), out int n))
                        max = Mathf.Max(max, n);
                }
            return max + 1;
        }

        // ─── Navigation ─────────────────────────────────────────────────

        void RefreshLevelMap()
        {
            _levelPathMap.Clear();
            if (AssetDatabase.IsValidFolder(LevelsFolder))
                foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LevelsFolder }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    if (!p.EndsWith(".json")) continue;
                    _levelPathMap[Path.GetFileNameWithoutExtension(p)] = p;
                }

            if (_loadDropdown != null)
            {
                var choices = new List<string> { "— Load Level —" };
                choices.AddRange(SortedLevelNumbers().Select(n => $"level_{n}"));
                _loadDropdown.choices = choices;
                _loadDropdown.SetValueWithoutNotify("— Load Level —");
            }
        }

        List<int> SortedLevelNumbers()
        {
            var nums = new List<int>();
            foreach (var key in _levelPathMap.Keys)
                if (key.StartsWith("level_") && int.TryParse(key.Substring(6), out int n))
                    nums.Add(n);
            nums.Sort();
            return nums;
        }

        int CurrentLevelNumber()
        {
            var id = _config?.LevelId ?? "";
            return id.StartsWith("level_") && int.TryParse(id.Substring(6), out int n) ? n : -1;
        }

        void LoadLevelByNumber(int n)
        {
            if (_levelPathMap.TryGetValue($"level_{n}", out string path))
                LoadLevel(path);
        }

        void LoadPrev()
        {
            var nums = SortedLevelNumbers();
            if (nums.Count == 0) return;
            var idx = nums.IndexOf(CurrentLevelNumber());
            if (idx < 0) LoadLevelByNumber(nums[^1]);
            else if (idx > 0) LoadLevelByNumber(nums[idx - 1]);
        }

        void LoadNext()
        {
            var nums = SortedLevelNumbers();
            if (nums.Count == 0) return;
            var idx = nums.IndexOf(CurrentLevelNumber());
            if (idx < 0) LoadLevelByNumber(nums[0]);
            else if (idx < nums.Count - 1) LoadLevelByNumber(nums[idx + 1]);
        }

        void UpdateLevelPicker()
        {
            var n = CurrentLevelNumber();
            if (_levelJumpField != null && n >= 0)
                _levelJumpField.SetValueWithoutNotify(n);
        }

        // ─── Refresh ────────────────────────────────────────────────────

        void RefreshAll()
        {
            if (_config == null) return;
            _levelIdField?.SetValueWithoutNotify(_config.LevelId);
            _levelNameField?.SetValueWithoutNotify(_config.LevelName ?? "");
            RebuildDimensionRow();
            RebuildBudgetRow();
            RebuildToolPalette();
            RebuildBoard();
            RebuildValidationContainer();
            UpdatePathLabel();
        }

        void UpdatePathLabel()
        {
            if (_pathLabel != null)
                _pathLabel.text = string.IsNullOrEmpty(_loadedPath) ? "(unsaved)" : _loadedPath;
            UpdateLevelPicker();
        }

        // ─── Dirty state & save feedback ────────────────────────────────

        void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            if (_dirtyDot != null) _dirtyDot.style.display = DisplayStyle.Flex;
            _saveBtn?.AddToClassList("btn-save--dirty");
        }

        void ClearDirty()
        {
            _dirty = false;
            if (_dirtyDot != null) _dirtyDot.style.display = DisplayStyle.None;
            _saveBtn?.RemoveFromClassList("btn-save--dirty");
        }

        void FlashSaved(string path)
        {
            if (_pathLabel == null) return;
            _pathLabel.text = $"✓ Saved → {path}";
            _pathLabel.AddToClassList("path-label--saved");
            _pathLabel.schedule.Execute(() =>
            {
                _pathLabel.RemoveFromClassList("path-label--saved");
                UpdatePathLabel();
            }).ExecuteLater(1500);
        }

        // ─── Export / Import ────────────────────────────────────────────

        // GDD: "Export dumps all saved levels as copyable JSON" - the whole saved-levels list,
        // to the clipboard, unlike bubble-pond's single-level SaveFilePanel disk export.
        void ExportJson()
        {
            RefreshLevelMap();
            var levels = new List<LevelConfig>();
            foreach (var path in _levelPathMap.Values)
            {
                var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (ta == null) continue;
                try
                {
                    var cfg = JsonConvert.DeserializeObject<LevelConfig>(ta.text, JsonSettings);
                    if (cfg != null) levels.Add(cfg);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[LevelEditor] Skipped {path} while exporting: {e.Message}");
                }
            }

            GUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(levels, JsonSettings);
            Debug.Log($"[LevelEditor] Copied {levels.Count} level(s) to the clipboard.");
            EditorUtility.DisplayDialog("Export", $"Copied {levels.Count} level(s) to the clipboard as JSON.", "OK");
        }

        // In-window overlay (shield + panel), the same technique bubble-pond's editor uses for its
        // sprite picker, instead of a second EditorWindow - keeps import state in one place.
        void BuildImportOverlay()
        {
            _importShield = new VisualElement();
            _importShield.AddToClassList("import-shield");
            _importShield.style.display = DisplayStyle.None;
            _importShield.RegisterCallback<PointerDownEvent>(_ => HideImportOverlay());

            var panel = new VisualElement();
            panel.AddToClassList("import-panel");
            panel.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());

            var title = new Label("Import levels (paste JSON array)");
            title.AddToClassList("panel-title");
            panel.Add(title);

            _importTextField = new TextField { multiline = true };
            _importTextField.AddToClassList("import-textfield");
            panel.Add(_importTextField);

            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("import-button-row");

            var cancelBtn = new Button(HideImportOverlay) { text = "Cancel" };
            cancelBtn.AddToClassList("btn-secondary");
            buttonRow.Add(cancelBtn);

            var importBtn = new Button(ConfirmImport) { text = "Import" };
            importBtn.AddToClassList("btn-accent");
            buttonRow.Add(importBtn);

            panel.Add(buttonRow);

            _importShield.Add(panel);
            rootVisualElement.Add(_importShield);
        }

        void ShowImportOverlay()
        {
            _importTextField.SetValueWithoutNotify(GUIUtility.systemCopyBuffer ?? "");
            _importShield.style.display = DisplayStyle.Flex;
        }

        void HideImportOverlay()
        {
            _importShield.style.display = DisplayStyle.None;
        }

        // GDD: "Import merges pasted JSON into the saved-levels list (reassigning IDs on collision)."
        void ConfirmImport()
        {
            List<LevelConfig> imported;
            try
            {
                imported = JsonConvert.DeserializeObject<List<LevelConfig>>(_importTextField.value, JsonSettings);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Import failed", $"Couldn't parse JSON: {e.Message}", "OK");
                return;
            }

            if (imported == null || imported.Count == 0)
            {
                EditorUtility.DisplayDialog("Import", "No levels found in the pasted JSON.", "OK");
                return;
            }

            RefreshLevelMap();
            var knownIds = new HashSet<string>(_levelPathMap.Keys);
            EnsureAssetFolder(LevelsFolder);

            int importedCount = 0;
            foreach (var level in imported)
            {
                if (string.IsNullOrEmpty(level.LevelId) || knownIds.Contains(level.LevelId))
                    level.LevelId = $"level_{NextLevelNumber()}";
                knownIds.Add(level.LevelId);

                string path = $"{LevelsFolder}/{level.LevelId}.json";
                File.WriteAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), path),
                    JsonConvert.SerializeObject(level, JsonSettings));
                AssetDatabase.ImportAsset(path);
                importedCount++;
            }

            AssetDatabase.SaveAssets();
            RefreshLevelMap();
            HideImportOverlay();
            Debug.Log($"[LevelEditor] Imported {importedCount} level(s).");
            EditorUtility.DisplayDialog("Import", $"Imported {importedCount} level(s).", "OK");
        }

        // ─── Restore ────────────────────────────────────────────────────

        void RestoreLastLevel()
        {
            var last = EditorPrefs.GetString(LastPathPrefKey, "");
            if (!string.IsNullOrEmpty(last) && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), last)))
                LoadLevel(last);
        }

        // ─── Helpers ────────────────────────────────────────────────────

        static JsonSerializerSettings JsonSettings => new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter() },
        };

        static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            var parts = assetFolder.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
