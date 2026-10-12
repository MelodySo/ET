using System;
using System.Collections.Generic;
using System.IO;
using MongoDB.Bson;

namespace ET
{
    /// <summary>保存时读取玩法源表，不依赖 Play 模式或已导出的运行时单例。</summary>
    internal static class BrawlModeWorkbook
    {
        public static int GetPlayerCount(int modeId)
        {
            string path = Path.GetFullPath("Packages/cn.etetet.brawl/Luban/Config/Datas/BrawlMode.xlsx");
            string info = BrawlEditorToolRunner.Excel("excel_sheet", new BsonDocument { { "operation", "list" }, { "path", path } });
            BsonArray sheets = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(info);
            if (sheets.Count == 0) throw new InvalidDataException("BrawlMode.xlsx 没有工作表。");
            int rows = sheets[0]["rowCount"].AsInt32;
            int columns = sheets[0]["columnCount"].AsInt32;
            if (rows < 5 || columns < 2) throw new InvalidDataException("BrawlMode.xlsx 尚无玩法数据。");
            string result = BrawlEditorToolRunner.Excel("excel_range", new BsonDocument
            {
                { "operation", "get" }, { "path", path }, { "range", $"A1:{ColumnName(columns)}{rows}" },
                { "includeFormulas", true }, { "calculateFormulas", false },
            });
            Dictionary<string, string> cells = new();
            foreach (BsonValue value in BsonDocument.Parse(result)["items"].AsBsonArray)
            {
                BsonDocument item = value.AsBsonDocument;
                if (item.TryGetValue("formula", out BsonValue formula) && !formula.IsBsonNull &&
                    !string.IsNullOrWhiteSpace(formula.ToString()))
                    throw new InvalidDataException($"玩法表单元格 {item["cell"].AsString} 含公式：{formula}。请改为直接填写的值。");
                cells[item["cell"].AsString] = item["value"].IsBsonNull ? "" : item["value"].AsString.Trim();
            }
            string Cell(string column, int row) => cells.TryGetValue(column + row, out string text) ? text : "";
            string FindColumn(string name)
            {
                string found = null;
                for (int column = 1; column <= columns; ++column)
                {
                    string address = ColumnName(column);
                    if (Cell(address, 1) != name) continue;
                    if (found != null) throw new InvalidDataException($"玩法表字段重复：{name}");
                    found = address;
                }
                return found ?? throw new InvalidDataException($"玩法表缺少字段：{name}");
            }
            string idColumn = FindColumn("Id");
            string sizeColumn = FindColumn("TeamSize");
            string countColumn = FindColumn("TeamCount");
            int required = 0;
            for (int row = 5; row <= rows; ++row)
            {
                if (Cell("A", row).StartsWith("#", StringComparison.Ordinal)) continue;
                if (!int.TryParse(Cell(idColumn, row), out int id) || id != modeId) continue;
                if (required != 0) throw new InvalidDataException($"玩法 Id 重复：{modeId}");
                if (!int.TryParse(Cell(sizeColumn, row), out int size) || size <= 0 ||
                    !int.TryParse(Cell(countColumn, row), out int count) || count <= 0 || (long)size * count > 10000)
                    throw new InvalidDataException($"玩法 {modeId} 的 TeamSize / TeamCount 非法。");
                required = size * count;
            }
            if (required == 0) throw new InvalidDataException($"BrawlMode.xlsx 中不存在玩法 {modeId}。");
            return required;
        }

        private static string ColumnName(int column)
        {
            string name = "";
            while (column > 0) { --column; name = (char)('A' + column % 26) + name; column /= 26; }
            return name;
        }
    }
}
