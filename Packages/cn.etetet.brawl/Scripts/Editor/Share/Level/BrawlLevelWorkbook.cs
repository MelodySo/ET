using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;

namespace ET
{
    /// <summary>短生命周期的工作簿快照，不持有 Excel 文件句柄。</summary>
    internal sealed class BrawlLevelWorkbook
    {
        public const string DefaultPath = "Packages/cn.etetet.brawl/Luban/Config/Datas/BrawlLevel.xlsx";
        private const int HeaderRows = 4;
        private const int ExcelCellTextLimit = 32767;
        private readonly Dictionary<string, string> cells = new();
        private readonly Dictionary<int, int> rowsById = new();
        private int rowCount;
        private string fileHash;

        public static BrawlLevelWorkbook Read(string path)
        {
            string hash = HashFile(path);
            string info = BrawlEditorToolRunner.Excel("excel_sheet", new BsonDocument { { "operation", "list" }, { "path", Path.GetFullPath(path) } });
            BsonArray sheets = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(info);
            if (sheets.Count == 0 || sheets[0]["name"].AsString != "BrawlLevel")
                throw new InvalidDataException("第一个工作表必须名为 BrawlLevel。");
            int count = Math.Max(HeaderRows, sheets[0]["rowCount"].AsInt32);
            string result = BrawlEditorToolRunner.Excel("excel_range", new BsonDocument
            {
                { "operation", "get" }, { "path", Path.GetFullPath(path) }, { "range", $"A1:I{count}" },
                { "includeFormulas", true }, { "calculateFormulas", false },
            });
            BrawlLevelWorkbook workbook = new() { rowCount = count, fileHash = hash };
            foreach (BsonValue value in BsonDocument.Parse(result)["items"].AsBsonArray)
            {
                BsonDocument item = value.AsBsonDocument;
                if (item.TryGetValue("formula", out BsonValue formula) && !formula.IsBsonNull &&
                    !string.IsNullOrWhiteSpace(formula.ToString()))
                    throw new InvalidDataException($"关卡表单元格 {item["cell"].AsString} 含公式：{formula}。请改为直接填写的值。");
                workbook.cells.Add(item["cell"].AsString, item["value"].IsBsonNull ? "" : item["value"].AsString);
            }
            workbook.CheckHeaders();
            for (int row = HeaderRows + 1; row <= count; ++row)
            {
                if (workbook.Cell(row, 0).StartsWith("#", StringComparison.Ordinal)) continue;
                string rawId = workbook.Cell(row, 1);
                if (string.IsNullOrWhiteSpace(rawId))
                {
                    if (Enumerable.Range(2, 7).Any(column => !string.IsNullOrWhiteSpace(workbook.Cell(row, column))))
                        throw new InvalidDataException($"第 {row} 行有数据但缺少 Id。");
                    continue;
                }
                int id = ParseInt(rawId, $"第 {row} 行 Id");
                if (id <= 0 || workbook.rowsById.ContainsKey(id)) throw new InvalidDataException($"第 {row} 行 Id 非法或重复：{id}");
                workbook.rowsById.Add(id, row);
            }
            if (hash != HashFile(path)) throw new IOException("读取期间 Excel 已被外部修改，请重试。");
            return workbook;
        }

        public bool Contains(int id) => this.rowsById.ContainsKey(id);

        public BrawlLevelConfig Load(int id, out string stamp)
        {
            if (!this.rowsById.TryGetValue(id, out int row)) throw new InvalidDataException($"关卡 {id} 不存在。");
            BrawlLevelConfig config = new(id, ParseInt(this.Cell(row, 2), "ModeId"), this.Cell(row, 3),
                ParseInt(this.Cell(row, 4), "RowCount"), ParseInt(this.Cell(row, 5), "ColumnCount"),
                ParseArray(this.Cell(row, 6), "MapGrid"), ParseArray(this.Cell(row, 7), "SpawnPoints"),
                ParseArray(this.Cell(row, 8), "ItemSpawnPoints"));
            Validate(config);
            stamp = this.Stamp(row);
            return config;
        }

        /// <param name="expectedStamp">null 表示新增；否则必须与上次导入/保存的目标行一致。</param>
        public static string Save(string path, BrawlLevelConfig config, string expectedStamp)
        {
            Validate(config);
            path = Path.GetFullPath(path);
            BrawlLevelWorkbook workbook = Read(path);
            bool exists = workbook.rowsById.TryGetValue(config.Id, out int row);
            if (expectedStamp == null && exists) throw new InvalidDataException($"关卡 {config.Id} 已存在，请使用导入。");
            if (expectedStamp != null && (!exists || expectedStamp != workbook.Stamp(row)))
                throw new IOException("该关卡在外部已被修改或删除，请退出并重新导入后再保存。");
            if (!exists) row = workbook.rowCount + 1;

            string folder = Path.GetFullPath(Path.Combine("Temp", "BrawlLevelEditor", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(folder);
            string pending = Path.Combine(folder, "pending.xlsx");
            string backup = Path.Combine(folder, "before-save.xlsx");
            File.Copy(path, pending);
            try
            {
                BsonArray values = new()
                {
                    "", config.Id, config.ModeId, "'" + (config.SceneAsset ?? ""), config.RowCount, config.ColumnCount,
                    "'" + FormatArray(config.MapGrid), "'" + FormatArray(config.SpawnPoints), "'" + FormatArray(config.ItemSpawnPoints),
                };
                BrawlEditorToolRunner.Excel("excel_range", new BsonDocument
                {
                    { "operation", "write" }, { "path", pending }, { "range", $"A{row}:I{row}" },
                    { "data", new BsonArray { values } },
                });
                BrawlLevelWorkbook saved = Read(pending);
                BrawlLevelConfig roundTrip = saved.Load(config.Id, out string stamp);
                if (roundTrip.SceneAsset != (config.SceneAsset ?? "") || roundTrip.ModeId != config.ModeId ||
                    roundTrip.RowCount != config.RowCount || roundTrip.ColumnCount != config.ColumnCount ||
                    !roundTrip.MapGrid.SequenceEqual(config.MapGrid) || !roundTrip.SpawnPoints.SequenceEqual(config.SpawnPoints) ||
                    !roundTrip.ItemSpawnPoints.SequenceEqual(config.ItemSpawnPoints))
                    throw new InvalidDataException("临时工作簿回读不一致，原表未修改。");
                // Deny concurrent writes while allowing our atomic replacement; an Excel lock fails safely here.
                using (FileStream guard = new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                {
                    if (Hash(guard) != workbook.fileHash) throw new IOException("保存期间 Excel 已被外部修改，原表未覆盖，请重试。");
                    File.Replace(pending, path, backup);
                }
                return stamp;
            }
            finally
            {
                if (File.Exists(pending)) File.Delete(pending);
                // A successful save retains the recoverable backup in Temp; failed writes leave no scratch directory.
                if (!File.Exists(backup)) Directory.Delete(folder);
            }
        }

        public static void Validate(BrawlLevelConfig config)
        {
            if (config.Id <= 0 || config.ModeId <= 0) throw new InvalidDataException("关卡 Id 和 ModeId 必须大于 0。");
            long count = (long)config.RowCount * config.ColumnCount;
            if (config.RowCount <= 0 || config.ColumnCount <= 0 || config.RowCount > 100 || config.ColumnCount > 100)
                throw new InvalidDataException("行数和列数必须在 1–100 之间。");
            if (config.MapGrid == null || config.SpawnPoints == null || config.ItemSpawnPoints == null)
                throw new InvalidDataException("数组不能为 null；未配置时使用空数组。");
            if (config.MapGrid.Length != 0 && config.MapGrid.Length != count)
                throw new InvalidDataException("已有 MapGrid 长度与行列数不匹配。本阶段不自动裁剪或重排地图。");
            foreach (int tile in config.MapGrid)
                if (!Enum.IsDefined(typeof(BrawlTileType), tile)) throw new InvalidDataException($"未知格子类型：{tile}");
            ValidatePoints(config.SpawnPoints, count, "SpawnPoints");
            ValidatePoints(config.ItemSpawnPoints, count, "ItemSpawnPoints");
            if ((config.SceneAsset ?? "").Length > ExcelCellTextLimit) throw new InvalidDataException("SceneAsset 超过 Excel 单元格字符上限。");
            FormatArray(config.MapGrid);
            FormatArray(config.SpawnPoints);
            FormatArray(config.ItemSpawnPoints);
        }

        private static void ValidatePoints(int[] values, long count, string name)
        {
            HashSet<int> unique = new();
            foreach (int value in values)
                if (value < 0 || value >= count || !unique.Add(value)) throw new InvalidDataException($"{name} 包含越界或重复索引：{value}");
        }

        private void CheckHeaders()
        {
            string[] names = { "##var", "Id", "ModeId", "SceneAsset", "RowCount", "ColumnCount", "MapGrid", "SpawnPoints", "ItemSpawnPoints" };
            string[] types = { "##type", "int", "int", "string", "int", "int", "(array#sep=,),int", "(array#sep=,),int", "(array#sep=,),int" };
            for (int column = 0; column < names.Length; ++column)
            {
                if (this.Cell(1, column) != names[column] || this.Cell(2, column) != types[column])
                    throw new InvalidDataException($"关卡表第 {column + 1} 列表头/类型不匹配，请保留标准表头。");
                string group = column == 0 ? "##group" : column == 3 ? "c" : "";
                if (this.Cell(3, column) != group) throw new InvalidDataException("关卡表导出分组已改变，请保留 SceneAsset=c，其余分组留空。");
            }
            if (this.Cell(4, 0) != "##") throw new InvalidDataException("关卡表第 4 行必须是注释行。");
        }

        private string Cell(int row, int column) => this.cells[$"{(char)('A' + column)}{row}"];
        private string Stamp(int row)
        {
            // Length-prefix the fields so no cell text can collide with a separator.
            string text = string.Concat(Enumerable.Range(0, 9).Select(column => $"{this.Cell(row, column).Length}:{this.Cell(row, column)}"));
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(text));
            return Hash(stream);
        }
        private static int ParseInt(string value, string name)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                throw new InvalidDataException($"{name} 不是合法整数：{value}");
            return result;
        }
        private static int[] ParseArray(string value, string name) => string.IsNullOrWhiteSpace(value)
            ? Array.Empty<int>() : value.Split(',').Select(item => ParseInt(item.Trim(), name)).ToArray();
        private static string FormatArray(int[] values)
        {
            string text = string.Join(",", values.Select(value => value.ToString(CultureInfo.InvariantCulture)));
            if (text.Length > ExcelCellTextLimit) throw new InvalidDataException("数组超过 Excel 单元格 32767 字符上限。");
            return text;
        }
        private static string HashFile(string path) { using FileStream stream = File.OpenRead(path); return Hash(stream); }
        private static string Hash(Stream stream) { using SHA256 hash = SHA256.Create(); return Convert.ToBase64String(hash.ComputeHash(stream)); }
    }
}
