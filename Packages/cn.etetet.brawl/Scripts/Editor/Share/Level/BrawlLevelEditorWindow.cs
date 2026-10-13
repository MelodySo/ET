using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ET
{
    public sealed class BrawlLevelEditorWindow : EditorWindow
    {
        internal const string LastLevelIdKey = "ET.Brawl.LevelEditor.LastLevelId";
        [SerializeField] private int requestedId;
        [SerializeField] private int selectedId;
        [SerializeField] private int modeId;
        [SerializeField] private int styleId;
        [SerializeField] private string sceneAsset = "";
        [SerializeField] private int rowCount;
        [SerializeField] private int columnCount;
        [SerializeField] private int[] mapGrid = Array.Empty<int>();
        [SerializeField] private int[] spawnPoints = Array.Empty<int>();
        [SerializeField] private int[] npcIds = Array.Empty<int>();
        [SerializeField] private int[] npcPoints = Array.Empty<int>();
        [SerializeField] private int npcBrushId;
        [SerializeField] private bool npcTool;
        private BrawlNPCWorkbook npcWorkbook;
        private int[] npcOptions = Array.Empty<int>();
        private string[] npcLabels = Array.Empty<string>();
        private bool npcLoadAttempted;
        [SerializeField] private string savedStamp;
        [SerializeField] private bool isNew;
        [SerializeField] private bool dirty;
        [SerializeField] private string message;
        [SerializeField] private MessageType messageType;
        [SerializeField] private BrawlTileType brush = BrawlTileType.Ground;
        [SerializeField] private bool spawnTool;
        [SerializeField] private float cellPixels = 20;
        [SerializeField] private Vector2 pan = new(10, 10);
        private int strokeControl;
        private int lastCell = -1;
        private int strokeGroup = -1;
        private SceneAsset sceneObject;
        private GUIStyle spawnStyle;

        internal int SelectedLevelId => this.selectedId;


        [MenuItem("Brawl/关卡编辑器")]
        public static void Open()
        {
            BrawlLevelEditorWindow window = GetWindow<BrawlLevelEditorWindow>();
            window.titleContent = new GUIContent("Brawl 关卡编辑器");
            window.minSize = new Vector2(650, 650);
            window.Show();
        }

        private void OnEnable()
        {
            if (this.requestedId == 0 && this.selectedId == 0)
                this.requestedId = PlayerPrefs.GetInt(LastLevelIdKey, 0);
            if (!Enum.IsDefined(typeof(BrawlTileType), this.brush)) this.brush = BrawlTileType.Ground;
            this.minSize = new Vector2(650, 650);
            if (this.selectedId != 0 && this.mapGrid.Length == 0)
            {
                this.rowCount = Mathf.Clamp(this.rowCount, 1, 100);
                this.columnCount = Mathf.Clamp(this.columnCount, 1, 100);
                this.mapGrid = new int[this.rowCount * this.columnCount];
                this.dirty = true;
            }
            // No workbook is opened here: only memory state survives a Unity domain reload.
            this.hasUnsavedChanges = this.dirty;
            this.saveChangesMessage = "当前关卡有未保存修改。是否保存到 BrawlLevel.xlsx？";
            Undo.undoRedoPerformed += this.OnUndoRedo;
            this.ResolveScene();
        }

        private void OnDisable()
        {
            if (this.requestedId > 0) this.RememberLevelId(this.requestedId);
            this.EndStroke();
            Undo.undoRedoPerformed -= this.OnUndoRedo;
        }

        private void RememberLevelId(int id)
        {
            this.requestedId = id;
            PlayerPrefs.SetInt(LastLevelIdKey, id);
            PlayerPrefs.Save();
        }

        private void OnUndoRedo()
        {
            if (this.selectedId == 0) return;
            this.ResolveScene();
            this.SetDirty(true);
            this.Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("BrawlLevel.xlsx", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("仅在导入、保存时打开 Excel，用完立即释放。", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (this.selectedId == 0) this.DrawSelection();
                else this.DrawLevel();
            }
            if (!string.IsNullOrEmpty(this.message)) EditorGUILayout.HelpBox(this.message, this.messageType);
        }

        private void DrawSelection()
        {
            this.requestedId = EditorGUILayout.IntField("关卡 ID", this.requestedId);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("新建")) this.TryOperation("新建关卡", () => this.CreateLevel(this.requestedId, BrawlLevelWorkbook.DefaultPath));
            if (GUILayout.Button("导入")) this.TryOperation("导入关卡", () => this.ImportLevel(this.requestedId, BrawlLevelWorkbook.DefaultPath));
            if (GUILayout.Button("导出配置")) this.TryOperation("导出配置", this.ExportConfigs);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("新建与导入使用上方 ID。新建仅创建草稿，保存后才写入 Excel。\n导出配置会生成项目全部 Excel 的配置代码，不会创建地图。", MessageType.Info);
        }

        private void DrawLevel()
        {
            EditorGUILayout.LabelField("当前关卡 ID", this.selectedId.ToString());
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("保存")) this.TryOperation("保存关卡", this.SaveLevel);
            if (GUILayout.Button("退出"))
            {
                if (!this.dirty || EditorUtility.DisplayDialog("退出关卡", "放弃当前未保存修改？Excel 中已有数据不会删除。", "放弃修改并退出", "取消"))
                {
                    this.ExitLevel();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            int nextMode = EditorGUILayout.IntField("ModeId（所属玩法）", this.modeId);
            int nextStyle = EditorGUILayout.IntField("StyleId（地形风格）", this.styleId);
            SceneAsset nextScene = (SceneAsset)EditorGUILayout.ObjectField("SceneAsset（拖入场景）", this.sceneObject, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(this, "修改关卡信息");
                this.modeId = nextMode;
                this.styleId = nextStyle;
                if (nextScene != this.sceneObject)
                {
                    this.sceneObject = nextScene;
                    this.sceneAsset = nextScene == null ? "" : AssetDatabase.GetAssetPath(nextScene);
                }
                this.SetDirty(true);
            }
            EditorGUILayout.LabelField("保存的场景路径", this.sceneAsset);
            int rows = Mathf.Clamp(EditorGUILayout.DelayedIntField("RowCount（行数 1–100）", this.rowCount), 1, 100);
            int columns = Mathf.Clamp(EditorGUILayout.DelayedIntField("ColumnCount（列数 1–100）", this.columnCount), 1, 100);
            if (rows != this.rowCount || columns != this.columnCount)
            {
                if ((rows >= this.rowCount && columns >= this.columnCount) ||
                    EditorUtility.DisplayDialog("缩小地图", "保留左下角重叠区域，范围外格子和点位将被裁掉。可以撤销。", "确定", "取消"))
                    this.ResizeGrid(rows, columns);
            }
            EditorGUILayout.LabelField("格子尺寸", "固定 1 米，不写入配置");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("MapGrid", $"{this.mapGrid.Length} 个格子");
            EditorGUILayout.LabelField("SpawnPoints", $"{this.spawnPoints.Length} 个出生点");
            EditorGUILayout.LabelField("NPC", $"{this.npcPoints.Length} 个（独立于地形）");
            this.DrawGrid();
        }

        internal void CreateLevel(int id, string workbookPath)
        {
            if (id <= 0) throw new InvalidDataException("请输入大于 0 的关卡 ID。");
            if (BrawlLevelWorkbook.Read(workbookPath).Contains(id))
                throw new InvalidDataException("该关卡已存在，请使用导入。");
            Undo.ClearUndo(this);
            this.selectedId = id;
            this.modeId = id / 100;
            this.styleId = 0;
            this.sceneAsset = "";
            this.rowCount = 20;
            this.columnCount = 20;
            this.mapGrid = new int[this.rowCount * this.columnCount];
            this.ResetView();
            this.spawnPoints = Array.Empty<int>();
            this.npcIds = Array.Empty<int>();
            this.npcPoints = Array.Empty<int>();
            this.savedStamp = null;
            this.isNew = true;
            this.SetDirty(true);
            this.SetMessage("草稿已创建，请填写所属玩法、行数和列数后保存。ModeId 初始建议值为 ID / 100，可修改。");
            this.RememberLevelId(id);
        }

        internal void ImportLevel(int id, string workbookPath)
        {
            BrawlLevelConfig config = BrawlLevelWorkbook.Read(workbookPath).Load(id, out string stamp);
            Undo.ClearUndo(this);
            this.selectedId = config.Id;
            this.modeId = config.ModeId;
            this.styleId = config.StyleId;
            this.sceneAsset = config.SceneAsset;
            this.rowCount = config.RowCount;
            this.columnCount = config.ColumnCount;
            this.mapGrid = config.MapGrid.Length == 0 ? new int[config.RowCount * config.ColumnCount] : config.MapGrid;
            this.spawnPoints = config.SpawnPoints;
            this.npcIds = config.NPCIds;
            this.npcPoints = config.NPCPoints;
            this.savedStamp = stamp;
            this.isNew = false;
            this.ResetView();
            this.ResolveScene();
            this.SetDirty(config.MapGrid.Length == 0);
            this.SetMessage("关卡已导入。出生点按添加顺序编号。");
            this.RememberLevelId(id);
        }

        private void SaveLevel()
        {
            this.SaveLevel(BrawlLevelWorkbook.DefaultPath);
        }

        internal void SaveLevel(string workbookPath)
        {
            BrawlLevelWorkbook.Validate(new BrawlLevelConfig(this.selectedId, this.modeId, this.styleId, this.sceneAsset,
                this.rowCount, this.columnCount, this.mapGrid, this.spawnPoints, this.npcIds, this.npcPoints));
            int required = BrawlModeWorkbook.GetPlayerCount(this.modeId);
            if (this.spawnPoints.Length != required)
                throw new InvalidDataException($"玩法 {this.modeId} 需要 {required} 个出生点（TeamSize × TeamCount），当前为 {this.spawnPoints.Length} 个。");
            if (this.mapGrid.Length != this.rowCount * this.columnCount)
                throw new InvalidDataException("请先完成地图网格初始化。");
            foreach (int point in this.spawnPoints)
                if (!IsWalkable(this.mapGrid[point])) throw new InvalidDataException($"出生点 {point} 位于不可站立的格子，请移动出生点或修改地形。");
            if (this.npcIds.Length > 0)
            {
                this.ReloadNPCs();
                for (int i = 0; i < this.npcIds.Length; ++i)
                {
                    if (!this.npcWorkbook.Contains(this.npcIds[i]))
                        throw new InvalidDataException($"NPC ID {this.npcIds[i]} 已不存在，请删除该摆放或修复 NPC 表。");
                    if (!IsWalkable(this.mapGrid[this.npcPoints[i]]) || this.spawnPoints.Contains(this.npcPoints[i]))
                        throw new InvalidDataException($"NPC 格子 {this.npcPoints[i]} 必须是平地或草丛，且不能与玩家出生点重叠。");
                }
            }
            BrawlLevelConfig config = new(this.selectedId, this.modeId, this.styleId, this.sceneAsset, this.rowCount, this.columnCount,
                this.mapGrid, this.spawnPoints, this.npcIds, this.npcPoints);
            this.savedStamp = BrawlLevelWorkbook.Save(workbookPath, config, this.isNew ? null : this.savedStamp);
            this.isNew = false;
            // Window snapshots contain workbook conflict stamps: never restore an old stamp after saving.
            Undo.ClearUndo(this);
            this.SetDirty(false);
            this.SetMessage($"关卡 {this.selectedId} 已保存，仍可继续编辑。退出后可导出配置。");
        }

        internal void ExitLevel()
        {
            this.EndStroke();
            Undo.ClearUndo(this);
            this.selectedId = 0;
            this.savedStamp = null;
            this.isNew = false;
            this.mapGrid = Array.Empty<int>();
            this.spawnPoints = Array.Empty<int>();
            this.npcIds = Array.Empty<int>();
            this.npcPoints = Array.Empty<int>();
            this.SetDirty(false);
            this.SetMessage("已退出，未执行保存。");
        }

        private void ExportConfigs()
        {
            string output = BrawlEditorToolRunner.Export();
            Debug.Log(output);
            this.SetMessage("配置导出成功，Unity 将刷新生成代码。详细信息见 Console。");
            AssetDatabase.Refresh();
        }

        public override void SaveChanges()
        {
            if (this.TryOperation("保存关卡", this.SaveLevel)) base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            this.SetDirty(false);
            base.DiscardChanges();
        }

        private bool TryOperation(string title, Action operation)
        {
            try
            {
                EditorUtility.DisplayProgressBar(title, "正在处理，请稍候……", 0.5f);
                operation();
                return true;
            }
            catch (Exception exception)
            {
                this.message = exception.Message;
                this.messageType = MessageType.Error;
                Debug.LogWarning($"Brawl level editor: {exception}");
                return false;
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private void SetDirty(bool value) { this.dirty = value; this.hasUnsavedChanges = value; }
        private void SetMessage(string value) { this.message = value; this.messageType = MessageType.Info; }

        private void ResetView()
        {
            this.brush = BrawlTileType.Ground;
            this.spawnTool = false;
            this.npcTool = false;
            this.cellPixels = 20;
            this.pan = new Vector2(10, 10);
            this.sceneObject = null;
        }

        private void ResolveScene()
        {
            this.sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(this.sceneAsset);
            if (this.sceneObject != null || string.IsNullOrEmpty(this.sceneAsset)) return;
            // Also display legacy short-name addresses, but do not silently change them on import.
            string[] matches = AssetDatabase.FindAssets("t:Scene " + Path.GetFileNameWithoutExtension(this.sceneAsset))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path) == this.sceneAsset).ToArray();
            if (matches.Length == 1) this.sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(matches[0]);
        }

        private void ResizeGrid(int rows, int columns)
        {
            Undo.RegisterCompleteObjectUndo(this, "调整地图尺寸");
            int[] resized = new int[rows * columns];
            for (int row = 0; row < Math.Min(rows, this.rowCount); ++row)
                for (int column = 0; column < Math.Min(columns, this.columnCount); ++column)
                    resized[row * columns + column] = this.mapGrid[row * this.columnCount + column];
            this.spawnPoints = this.RemapPoints(this.spawnPoints, rows, columns);
            List<int> nextIds = new();
            List<int> nextPoints = new();
            for (int i = 0; i < this.npcPoints.Length; ++i)
            {
                int row = this.npcPoints[i] / this.columnCount;
                int column = this.npcPoints[i] % this.columnCount;
                if (row >= rows || column >= columns) continue;
                nextIds.Add(this.npcIds[i]);
                nextPoints.Add(row * columns + column);
            }
            this.npcIds = nextIds.ToArray();
            this.npcPoints = nextPoints.ToArray();
            this.rowCount = rows;
            this.columnCount = columns;
            this.mapGrid = resized;
            this.SetDirty(true);
        }

        private int[] RemapPoints(int[] points, int rows, int columns)
        {
            int oldColumns = this.columnCount;
            return points.Where(p => p / oldColumns < rows && p % oldColumns < columns)
                .Select(p => p / oldColumns * columns + p % oldColumns).ToArray();
        }

        private void DrawGrid()
        {
            EditorGUILayout.BeginHorizontal();
            int tool = GUILayout.Toolbar(this.npcTool ? 2 : this.spawnTool ? 1 : 0, new[] { "地形", "出生点", "NPC" });
            this.spawnTool = tool == 1;
            this.npcTool = tool == 2;
            using (new EditorGUI.DisabledScope(this.spawnTool || this.npcTool))
                this.brush = (BrawlTileType)EditorGUILayout.EnumPopup(this.brush);
            if (GUILayout.Button("撤销", GUILayout.Width(45))) Undo.PerformUndo();
            if (GUILayout.Button("重做", GUILayout.Width(45))) Undo.PerformRedo();
            bool fit = GUILayout.Button("全图", GUILayout.Width(45));
            EditorGUILayout.EndHorizontal();
            if (this.npcTool) this.DrawNPCBrush();
            EditorGUILayout.LabelField(this.npcTool
                ? "NPC：左键单击放置/替换，右键单击删除，不支持拖刷。滚轮缩放，中键平移。"
                : "左键拖画 / 添加出生点，右键删除出生点；滚轮缩放，中键平移。左下角为 (0,0)。", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("0 平地（绿）  1 墙（灰）  2 草丛（深绿）  3 障碍（蓝）  4 水面（青）；NPC 为橙色", EditorStyles.miniLabel);
            Rect viewport = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (fit)
            {
                this.cellPixels = Mathf.Clamp(Mathf.Min((viewport.width - 20) / this.columnCount,
                    (viewport.height - 20) / this.rowCount), 2, 60);
                this.pan = new Vector2(10, 10);
            }
            this.HandleGridInput(viewport);
            if (Event.current.type != EventType.Repaint) return;
            GUI.BeginClip(viewport);
            EditorGUI.DrawRect(new Rect(0, 0, viewport.width, viewport.height), new Color(0.12f, 0.12f, 0.12f));
            int firstColumn = Mathf.Max(0, Mathf.FloorToInt(-this.pan.x / this.cellPixels));
            int lastColumn = Mathf.Min(this.columnCount - 1, Mathf.CeilToInt((viewport.width - this.pan.x) / this.cellPixels));
            int firstY = Mathf.Max(0, Mathf.FloorToInt(-this.pan.y / this.cellPixels));
            int lastY = Mathf.Min(this.rowCount - 1, Mathf.CeilToInt((viewport.height - this.pan.y) / this.cellPixels));
            for (int y = firstY; y <= lastY; ++y)
                for (int x = firstColumn; x <= lastColumn; ++x)
                {
                    int index = (this.rowCount - 1 - y) * this.columnCount + x;
                    float gap = this.cellPixels >= 8 ? 1 : 0;
                    EditorGUI.DrawRect(new Rect(this.pan.x + x * this.cellPixels, this.pan.y + y * this.cellPixels,
                        this.cellPixels - gap, this.cellPixels - gap), TileColor((BrawlTileType)this.mapGrid[index]));
                }
            if (this.spawnStyle == null) this.spawnStyle = new GUIStyle(EditorStyles.boldLabel)
                { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            for (int i = 0; i < this.npcPoints.Length; ++i)
            {
                int index = this.npcPoints[i];
                Rect cell = new(this.pan.x + index % this.columnCount * this.cellPixels,
                    this.pan.y + (this.rowCount - 1 - index / this.columnCount) * this.cellPixels, this.cellPixels, this.cellPixels);
                EditorGUI.DrawRect(new Rect(cell.x + cell.width * 0.12f, cell.y + cell.height * 0.12f,
                    cell.width * 0.76f, cell.height * 0.76f), new Color32(210, 110, 20, 255));
                string name = this.npcWorkbook?.Get(this.npcIds[i], "Name") ?? "";
                GUI.Label(cell, new GUIContent(this.cellPixels >= 35 ? this.npcIds[i].ToString() : "N",
                    $"NPC {this.npcIds[i]} {name}\n格子索引：{index}"), this.spawnStyle);
            }
            for (int i = 0; i < this.spawnPoints.Length; ++i)
            {
                int index = this.spawnPoints[i];
                Rect cell = new(this.pan.x + index % this.columnCount * this.cellPixels,
                    this.pan.y + (this.rowCount - 1 - index / this.columnCount) * this.cellPixels, this.cellPixels, this.cellPixels);
                EditorGUI.DrawRect(new Rect(cell.x + cell.width * 0.15f, cell.y + cell.height * 0.15f,
                    cell.width * 0.7f, cell.height * 0.7f), new Color(0.65f, 0.1f, 0.8f));
                if (this.cellPixels >= 18) GUI.Label(cell, (i + 1).ToString(), this.spawnStyle);
            }
            GUI.EndClip();
        }

        private void HandleGridInput(Rect viewport)
        {
            if (!GUI.enabled) return;
            Event e = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            Vector2 local = e.mousePosition - viewport.position;
            if (e.type == EventType.ScrollWheel && viewport.Contains(e.mousePosition))
            {
                float old = this.cellPixels;
                this.cellPixels = Mathf.Clamp(old * Mathf.Pow(1.12f, -e.delta.y), 2, 60);
                this.pan = local - (local - this.pan) * (this.cellPixels / old);
                e.Use();
                this.Repaint();
            }
            if (e.type == EventType.MouseDown && viewport.Contains(e.mousePosition) && e.button <= 2)
            {
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = control;
                this.strokeControl = control;
                this.lastCell = -1;
                if (e.button == 0 || (e.button == 1 && (this.spawnTool || this.npcTool)))
                {
                    Undo.IncrementCurrentGroup();
                    this.strokeGroup = Undo.GetCurrentGroup();
                    Undo.RegisterCompleteObjectUndo(this, this.npcTool ? "编辑 NPC" : this.spawnTool ? "编辑出生点" : "绘制地形");
                    this.PaintAt(local, e.button == 1);
                }
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == this.strokeControl && this.strokeControl != 0)
            {
                if (e.button == 2) this.pan += e.delta;
                else if (!this.npcTool && viewport.Contains(e.mousePosition) && (e.button == 0 || (e.button == 1 && this.spawnTool)))
                    this.PaintAt(local, e.button == 1);
                else this.lastCell = -1;
                e.Use();
                this.Repaint();
            }
            else if (e.rawType == EventType.MouseUp && this.strokeControl != 0)
            {
                this.EndStroke();
                e.Use();
            }
        }

        private void EndStroke()
        {
            if (this.strokeGroup >= 0) Undo.CollapseUndoOperations(this.strokeGroup);
            if (this.strokeControl != 0 && GUIUtility.hotControl == this.strokeControl) GUIUtility.hotControl = 0;
            this.strokeControl = 0;
            this.strokeGroup = -1;
            this.lastCell = -1;
        }

        private void PaintAt(Vector2 local, bool remove)
        {
            int x = Mathf.FloorToInt((local.x - this.pan.x) / this.cellPixels);
            int y = this.rowCount - 1 - Mathf.FloorToInt((local.y - this.pan.y) / this.cellPixels);
            if (x < 0 || x >= this.columnCount || y < 0 || y >= this.rowCount) { this.lastCell = -1; return; }
            int index = y * this.columnCount + x;
            if (this.npcTool)
            {
                this.PaintNPC(index, remove);
            }
            else if (this.spawnTool)
            {
                List<int> points = new(this.spawnPoints);
                bool changed = remove ? points.Remove(index) : !points.Contains(index) && !this.npcPoints.Contains(index) && IsWalkable(this.mapGrid[index]);
                if (changed)
                {
                    if (!remove) points.Add(index);
                    this.spawnPoints = points.ToArray();
                    this.SetDirty(true);
                }
            }
            else
            {
                int fromX = this.lastCell < 0 ? x : this.lastCell % this.columnCount;
                int fromY = this.lastCell < 0 ? y : this.lastCell / this.columnCount;
                int steps = Math.Max(Math.Abs(x - fromX), Math.Abs(y - fromY));
                for (int i = 0; i <= steps; ++i)
                {
                    float t = steps == 0 ? 1 : (float)i / steps;
                    int cell = Mathf.RoundToInt(Mathf.Lerp(fromY, y, t)) * this.columnCount + Mathf.RoundToInt(Mathf.Lerp(fromX, x, t));
                    if (this.mapGrid[cell] == (int)this.brush) continue;
                    this.mapGrid[cell] = (int)this.brush;
                    this.SetDirty(true);
                }
            }
            this.lastCell = index;
            this.Repaint();
        }

        private static bool IsWalkable(int tile) => tile == (int)BrawlTileType.Ground || tile == (int)BrawlTileType.Grass;

        private void ReloadNPCs()
        {
            this.npcLoadAttempted = true;
            this.npcWorkbook = null;
            this.npcOptions = Array.Empty<int>();
            this.npcLabels = Array.Empty<string>();
            this.npcWorkbook = BrawlNPCWorkbook.Read();
            this.npcOptions = this.npcWorkbook.Ids;
            this.npcLabels = this.npcOptions.Select(id => $"{id}  {this.npcWorkbook.Get(id, "Name")}").ToArray();
        }

        private void DrawNPCBrush()
        {
            if (!this.npcLoadAttempted) this.TryOperation("读取 NPC 表", this.ReloadNPCs);
            EditorGUILayout.BeginHorizontal();
            this.npcBrushId = EditorGUILayout.IntField("NPC ID", this.npcBrushId);
            if (GUILayout.Button("刷新 NPC 表", GUILayout.Width(100))) this.TryOperation("读取 NPC 表", this.ReloadNPCs);
            EditorGUILayout.EndHorizontal();
            if (this.npcWorkbook == null)
            {
                EditorGUILayout.HelpBox("NPC 表读取失败，请修复后点击刷新；仍可右键删除已有 NPC。", MessageType.Warning);
                return;
            }
            int selected = Array.IndexOf(this.npcOptions, this.npcBrushId);
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup("选择 NPC", selected, this.npcLabels);
            if (EditorGUI.EndChangeCheck() && next >= 0 && next < this.npcOptions.Length) this.npcBrushId = this.npcOptions[next];
            if (!this.npcWorkbook.Contains(this.npcBrushId))
            {
                EditorGUILayout.HelpBox("请填写有效 NPC ID，或从下拉列表选择后再放置。", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("名称", this.npcWorkbook.Get(this.npcBrushId, "Name"));
            EditorGUILayout.LabelField("类型 / 基础生命值", $"{this.npcWorkbook.Get(this.npcBrushId, "NPCType")} / {this.npcWorkbook.Get(this.npcBrushId, "BaseHp")}");
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("模型地址（只读）", this.npcWorkbook.Get(this.npcBrushId, "ModelPath"));
            string description = this.npcWorkbook.Get(this.npcBrushId, "Description");
            if (!string.IsNullOrEmpty(description)) EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
        }

        private void PaintNPC(int index, bool remove)
        {
            int existing = Array.IndexOf(this.npcPoints, index);
            if (remove)
            {
                if (existing < 0) return;
                this.npcIds = this.npcIds.Where((_, i) => i != existing).ToArray();
                this.npcPoints = this.npcPoints.Where((_, i) => i != existing).ToArray();
            }
            else
            {
                if (this.npcWorkbook == null || !this.npcWorkbook.Contains(this.npcBrushId))
                {
                    this.SetMessage("请先选择有效 NPC。");
                    return;
                }
                if (!IsWalkable(this.mapGrid[index]) || this.spawnPoints.Contains(index))
                {
                    this.SetMessage("NPC 只能放在平地或草丛，且不能与玩家出生点重叠。");
                    return;
                }
                if (existing >= 0)
                {
                    if (this.npcIds[existing] == this.npcBrushId) return;
                    this.npcIds[existing] = this.npcBrushId;
                }
                else
                {
                    this.npcIds = this.npcIds.Append(this.npcBrushId).ToArray();
                    this.npcPoints = this.npcPoints.Append(index).ToArray();
                }
            }
            this.SetDirty(true);
        }

        private static Color TileColor(BrawlTileType tile) => tile switch
        {
            BrawlTileType.Ground => new Color32(0, 255, 0, 255),
            BrawlTileType.Wall => new Color32(90, 95, 105, 255),
            BrawlTileType.Grass => new Color32(20, 115, 45, 255),
            BrawlTileType.BulletPassableObstacle => new Color32(55, 145, 220, 255),
            BrawlTileType.Water => new Color32(35, 205, 220, 255),
            _ => Color.magenta,
        };
    }
}
