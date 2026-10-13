using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MongoDB.Bson;

namespace ET
{
    /// <summary>编辑器持有的 NPC 源表快照，不依赖运行时配置单例或长期文件句柄。</summary>
    internal sealed class BrawlNPCWorkbook
    {
        private readonly Dictionary<int, Dictionary<string, string>> entries = new();

        public int[] Ids => this.entries.Keys.OrderBy(id => id).ToArray();
        public bool Contains(int id) => this.entries.ContainsKey(id);
        public string Get(int id, string field) => this.entries.TryGetValue(id, out var entry) && entry.TryGetValue(field, out string value) ? value : "";

        public static BrawlNPCWorkbook Read()
        {
            string path = Path.GetFullPath("Packages/cn.etetet.brawl/Luban/Config/Datas/BrawlNPC.xlsx");
            string info = BrawlEditorToolRunner.Excel("excel_sheet", new BsonDocument { { "operation", "list" }, { "path", path } });
            BsonArray sheets = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(info);
            if (sheets.Count == 0 || sheets[0]["name"].AsString != "BrawlNPC")
                throw new InvalidDataException("NPC 表的第一个工作表必须名为 BrawlNPC。");
            int rows = Math.Max(4, sheets[0]["rowCount"].AsInt32);
            int columns = sheets[0]["columnCount"].AsInt32;
            string Column(int column)
            {
                string name = "";
                while (column > 0) { --column; name = (char)('A' + column % 26) + name; column /= 26; }
                return name;
            }
            if (columns < 2) throw new InvalidDataException("NPC 表缺少字段。");
            string result = BrawlEditorToolRunner.Excel("excel_range", new BsonDocument
            {
                { "operation", "get" }, { "path", path }, { "range", $"A1:{Column(columns)}{rows}" },
                { "includeFormulas", true }, { "calculateFormulas", false },
            });
            Dictionary<string, string> cells = new();
            foreach (BsonValue value in BsonDocument.Parse(result)["items"].AsBsonArray)
            {
                BsonDocument item = value.AsBsonDocument;
                if (item.TryGetValue("formula", out BsonValue formula) && !formula.IsBsonNull && !string.IsNullOrWhiteSpace(formula.ToString()))
                    throw new InvalidDataException($"NPC 表单元格 {item["cell"]} 含公式，请填写直接值。");
                cells[item["cell"].AsString] = item["value"].IsBsonNull ? "" : item["value"].AsString.Trim();
            }
            string Cell(int row, int column) => cells.TryGetValue(Column(column) + row, out string value) ? value : "";
            Dictionary<string, int> headers = new();
            for (int column = 2; column <= columns; ++column)
            {
                string name = Cell(1, column);
                if (name == "") continue;
                if (headers.ContainsKey(name)) throw new InvalidDataException($"NPC 表字段重复：{name}");
                headers.Add(name, column);
            }
            foreach (string required in new[] { "Id", "Name", "ModelPath", "NPCType", "BaseHp" })
                if (!headers.ContainsKey(required)) throw new InvalidDataException($"NPC 表缺少字段：{required}");
            BrawlNPCWorkbook workbook = new();
            for (int row = 5; row <= rows; ++row)
            {
                if (Cell(row, 1).StartsWith("#", StringComparison.Ordinal)) continue;
                if (headers.Values.All(column => Cell(row, column) == "")) continue;
                if (!int.TryParse(Cell(row, headers["Id"]), out int id) || id <= 0 || workbook.entries.ContainsKey(id))
                    throw new InvalidDataException($"NPC 表第 {row} 行 Id 非法或重复。");
                workbook.entries.Add(id, headers.ToDictionary(pair => pair.Key, pair => Cell(row, pair.Value)));
            }
            return workbook;
        }
    }
}
