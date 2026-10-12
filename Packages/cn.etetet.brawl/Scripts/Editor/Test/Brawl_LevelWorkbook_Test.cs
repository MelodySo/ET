using System;
using System.IO;
using System.Linq;

namespace ET.Test
{
    public class Brawl_LevelWorkbook_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            string directory = Path.GetFullPath(Path.Combine("Temp", "BrawlLevelTests", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "BrawlLevel.xlsx");
            File.Copy(BrawlLevelWorkbook.DefaultPath, path);
            try
            {
                // Test inputs live only in the workbook copy; use unused IDs rather than changing production rows.
                int id = 2000000001;
                BrawlLevelConfig first = new(id, 1, "001", 2, 3,
                    new[] { 0, 1, 2, 3, 4, 0 }, new[] { 0, 5 }, new[] { 3 });
                string stamp = BrawlLevelWorkbook.Save(path, first, null);
                BrawlLevelConfig loaded = BrawlLevelWorkbook.Read(path).Load(id, out string readStamp);
                if (stamp != readStamp || !loaded.MapGrid.SequenceEqual(first.MapGrid) || loaded.SceneAsset != first.SceneAsset)
                    return Fail(1, "Initial save/import did not round-trip.");

                BrawlLevelConfig updated = new(id, 1, "ChangedArena", 2, 3,
                    first.MapGrid, first.SpawnPoints, first.ItemSpawnPoints);
                stamp = BrawlLevelWorkbook.Save(path, updated, stamp);
                BrawlLevelConfig other = new(id + 1, 2, "OtherArena", 1, 1,
                    Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
                BrawlLevelWorkbook.Save(path, other, null);
                loaded = BrawlLevelWorkbook.Read(path).Load(id, out readStamp);
                if (readStamp != stamp || loaded.SceneAsset != "ChangedArena" || !loaded.SpawnPoints.SequenceEqual(first.SpawnPoints))
                    return Fail(2, "Repeated save or saving another row lost data.");
                if (!Rejects(() => BrawlLevelWorkbook.Save(path, first, null)))
                    return Fail(3, "Create overwrote an existing ID.");
                if (!Rejects(() => BrawlLevelWorkbook.Save(path, first, "stale")))
                    return Fail(4, "Stale import overwrote a modified row.");
                if (!Rejects(() => BrawlLevelWorkbook.Read(path).Load(id + 2, out _)))
                    return Fail(5, "Missing ID was accepted.");

                BrawlLevelConfig invalid = new(id, 1, "", 1, 1, new[] { 0, 0 }, Array.Empty<int>(), Array.Empty<int>());
                if (!Rejects(() => BrawlLevelWorkbook.Validate(invalid))) return Fail(6, "Grid length mismatch was accepted.");
                invalid = new(id, 1, "", 1, 1, new[] { 9 }, Array.Empty<int>(), Array.Empty<int>());
                if (!Rejects(() => BrawlLevelWorkbook.Validate(invalid))) return Fail(7, "Unknown tile was accepted.");
                invalid = new(id, 1, "", 1, 1, new[] { 0 }, new[] { 1 }, Array.Empty<int>());
                if (!Rejects(() => BrawlLevelWorkbook.Validate(invalid))) return Fail(8, "Out-of-bounds spawn was accepted.");
                invalid = new(id, 1, "", 1, 1, new[] { 0 }, new[] { 0, 0 }, Array.Empty<int>());
                if (!Rejects(() => BrawlLevelWorkbook.Validate(invalid))) return Fail(9, "Duplicate spawn was accepted.");
                invalid = new(id, 1, "", 129, 129, Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
                if (!Rejects(() => BrawlLevelWorkbook.Validate(invalid))) return Fail(10, "Excel cell size bound was not checked.");

                byte[] before = File.ReadAllBytes(path);
                using (FileStream locked = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    if (!Rejects(() => BrawlLevelWorkbook.Save(path, updated, stamp)))
                        return Fail(11, "Exclusive file lock was ignored.");
                }
                if (!before.SequenceEqual(File.ReadAllBytes(path))) return Fail(12, "Failed save changed original bytes.");
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                return ErrorCode.ERR_Success;
            }
            finally
            {
                // These are the exact files created above, never a user workbook or broad recursive delete.
                if (File.Exists(path)) File.Delete(path);
                Directory.Delete(directory);
            }
        }

        private static bool Rejects(Action action)
        {
            try { action(); return false; }
            catch (Exception e) when (e is InvalidDataException || e is IOException || e is ArgumentException)
            { return true; }
        }

        private static int Fail(int code, string message)
        {
            Log.Console($"Brawl level workbook test failed: {message}");
            return 55000 + code;
        }
    }
}
