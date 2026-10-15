using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ET.Client
{
    /// <summary>手动触发的本地资源集成验证，不连接服务端，不写入关卡或账号数据。</summary>
    public static class BrawlTrainingVerification
    {
        public static async ETTask Run(Scene root)
        {
            EntityRef<Scene> rootRef = root;
            LobbyPanelComponent lobby = await root.YIUIRoot().OpenPanelAsync<LobbyPanelComponent>();
            EntityRef<LobbyPanelComponent> lobbyRef = lobby;
            lobby.SelectedHeroId = 101;
            try
            {
                int[] levels = { 10101, 10001, 10101 };
                for (int pass = 0; pass < levels.Length; ++pass)
                {
                    lobby = lobbyRef;
                    await BrawlTrainingHelper.Enter(lobby, levels[pass]);
                    lobby = lobbyRef;
                    root = rootRef;
                    BrawlTrainingSession session = lobby.TrainingSession;
                    if (session == null || session.Loading || session.TerrainRoot == null) throw new Exception("Training did not enter");
                    BrawlLevelConfig level = root.Fiber().GetSingleton<BrawlLevelConfigCategory>().Get(levels[pass]);
                    BrawlLevelStyleConfig style = root.Fiber().GetSingleton<BrawlLevelStyleConfigCategory>().Get(level.StyleId);
                    int expected = level.MapGrid.Count(tile => !string.IsNullOrWhiteSpace(BrawlTrainingHelper.TerrainAddress(style, tile)));
                    if (session.TerrainRoot.transform.childCount != expected) throw new Exception("Terrain count mismatch");
                    for (int i = 0; i < level.MapGrid.Length; ++i)
                    {
                        string address = BrawlTrainingHelper.TerrainAddress(style, level.MapGrid[i]);
                        if (string.IsNullOrWhiteSpace(address)) continue;
                        Transform tile = session.TerrainRoot.transform.Find($"Tile_{i}_{address}");
                        if (tile == null || tile.localPosition != BrawlTrainingHelper.CellPosition(i, level, session.CellSize)) throw new Exception("Terrain position mismatch");
                    }
                    if (session.RuntimeRoot.transform.childCount != 5) throw new Exception("Unexpected runtime objects");
                    if (session.Hero == null || session.Hero.transform.localPosition != BrawlTrainingHelper.CellPosition(level.SpawnPoints[0], level, session.CellSize))
                        throw new Exception("Hero spawn mismatch");
                    int npcCount = level.NPCIds?.Length ?? 0;
                    if (session.NPCRoot == null || session.NPCRoot.transform.childCount != npcCount) throw new Exception("NPC count mismatch");
                    for (int i = 0; i < npcCount; ++i)
                    {
                        Transform npc = session.NPCRoot.transform.Find($"NPC_{i}_{level.NPCIds[i]}");
                        if (npc == null || npc.localPosition != BrawlTrainingHelper.CellPosition(level.NPCPoints[i], level, session.CellSize))
                            throw new Exception("NPC position mismatch");
                    }
                    if (!session.ExitButton.interactable || session.LobbyGroup.interactable) throw new Exception("Training UI state mismatch");
                    string sceneName = session.SceneHandle.SceneObject.name;
                    if (pass < 2)
                    {
                        Camera camera = session.RuntimeRoot.GetComponentInChildren<Camera>();
                        RenderTexture target = new(1280, 720, 24);
                        RenderTexture previous = RenderTexture.active;
                        Texture2D image = new(1280, 720, TextureFormat.RGB24, false);
                        try
                        {
                            camera.targetTexture = target;
                            camera.Render();
                            RenderTexture.active = target;
                            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                            image.Apply();
                            File.WriteAllBytes($"Temp/BrawlTerrain_{level.Id}.png", image.EncodeToPNG());
                        }
                        finally
                        {
                            camera.targetTexture = null;
                            RenderTexture.active = previous;
                            UnityEngine.Object.Destroy(image);
                            UnityEngine.Object.Destroy(target);
                        }
                    }
                    await BrawlTrainingHelper.Exit(lobby);
                    lobby = lobbyRef;
                    if (lobby.TrainingSession != null || session.ModelHandles.Count != 0 || session.SceneHandle != null ||
                        UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName).isLoaded)
                        throw new Exception("Training cleanup failed");
                    Debug.Log($"BRAWL_TERRAIN_PASS round={pass + 1} scene={sceneName} tiles={expected} NPC={npcCount} handles=0");
                }
            }
            finally
            {
                lobby = lobbyRef;
                if (lobby != null && lobby.TrainingSession != null) await BrawlTrainingHelper.Exit(lobby);
            }
        }
    }
}
