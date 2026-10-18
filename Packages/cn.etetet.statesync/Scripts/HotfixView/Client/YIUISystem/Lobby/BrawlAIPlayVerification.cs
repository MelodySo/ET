using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace ET.Client
{
    /// <summary>Manual UnityBridge menu verification. No attack messages or server position overrides.</summary>
    public static class BrawlAIPlayVerification
    {
        public static async ETTask Run(Scene root)
        {
            if (root.GetComponent<BrawlAIPlayGuard>() != null) { Debug.LogWarning("Brawl AI verification is already running."); return; }
            BrawlAIPlayGuard guard = root.AddComponent<BrawlAIPlayGuard>();
            EntityRef<BrawlAIPlayGuard> guardRef = guard;
            try { await RunOnce(root); }
            catch (Exception e) { Debug.LogError($"BRAWL_AI_PLAY_FAIL {e.Message}"); }
            finally { guard = guardRef; guard?.Dispose(); }
        }
        private static async ETTask RunOnce(Scene root)
        {
            EntityRef<Scene> rootRef = root;
            if (root.GetComponent<PlayerComponent>().MyId == 0)
            {
                string account = PlayerPrefs.GetString("Brawl.Login.Account", "");
                string password = PlayerPrefs.GetString("Brawl.Login.Password", "");
                if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
                    throw new InvalidOperationException("No saved local login. Log in normally, then run Brawl/Verification/RunAI.");
                await LoginHelper.Login(root, root.GetComponent<GlobalComponent>().GlobalConfig.Address, account, password);
            }
            root = rootRef;
            LobbyPanelComponent lobby = await root.YIUIRoot().OpenPanelAsync<LobbyPanelComponent>();
            EntityRef<LobbyPanelComponent> lobbyRef = lobby;
            if (lobby.TrainingSession != null) throw new InvalidOperationException("Exit the current practice session before running verification.");
            lobby.SelectedHeroId = 101;
            long oldRoom = 0;
            try
            {
                for (int pass = 0; pass < 2; ++pass)
                {
                    lobby = lobbyRef;
                    await BrawlTrainingHelper.Enter(lobby, 10201, "练习赛");
                    lobby = lobbyRef;
                    BrawlTrainingSession session = lobby.TrainingSession;
                    if (session == null || session.Loading || session.PracticeRoomId == oldRoom) throw new Exception("Practice entry failed or reused old room.");
                    oldRoom = session.PracticeRoomId;
                    session.AIProbe = new BrawlAIPlayProbe();
                    BrawlPracticeUnitView bot = session.PracticeUnits.Single(v => v.Kind == BrawlPracticeUnitKind.Bot);
                    BrawlPracticeUnitView player = session.PracticeUnits.Single(v => v.UnitId == session.MyUnitId);
                    Vector3 initial = bot.Position;
                    float initialDistance = Vector3.Distance(bot.Position, player.Position);
                    float botHealth = bot.Health, playerHealth = player.Health;
                    long start = session.LastServerTick;
                    // Consume real ordered snapshots until the bounded verification window ends.
                    while (session.LastServerTick - start < 100)
                    {
                        await Next(session);
                        if (session.Projectiles.Count != 0 || bot.Health != botHealth || player.Health != playerHealth)
                            throw new Exception("Movement-only verification observed damage or projectiles.");
                    }
                    float travelled = Vector3.Distance(initial, bot.Position);
                    float finalDistance = Vector3.Distance(bot.Position, player.Position);
                    if (initialDistance <= 5 && (travelled < .5f || finalDistance >= initialDistance - .5f))
                        throw new Exception($"Bull did not approach: travel={travelled}, distance={initialDistance}->{finalDistance}");
                    if (initialDistance > 5) throw new Exception("Verification spawn starts beyond 5m; no Chase assertion possible.");
                    Vector3 beforePlayerMove = bot.Position;
                    session.AIProbe.Direction = Vector2.down;
                    start = session.LastServerTick;
                    while (session.LastServerTick - start < 30) await Next(session);
                    session.AIProbe.Direction = Vector2.zero;
                    start = session.LastServerTick;
                    while (session.LastServerTick - start < 15) await Next(session);
                    float followTravel = Vector3.Distance(beforePlayerMove, bot.Position);
                    if (followTravel < .3f) throw new Exception("Bull did not follow moving player.");
                    if (session.Projectiles.Count != 0 || bot.Health != botHealth || player.Health != playerHealth)
                        throw new Exception("Unexpected attack during follow test.");
                    Capture(session, $"Temp/BrawlAI-play-{pass + 1}.png");
                    Debug.Log($"BRAWL_AI_PLAY_PASS round={pass + 1} room={oldRoom} tick={session.LastServerTick} bullTravel={travelled:F3} distance={initialDistance:F3}->{finalDistance:F3} movingTargetTravel={followTravel:F3} projectiles=0");
                    session.AIProbe = null;
                    lobby = lobbyRef;
                    await BrawlTrainingHelper.Exit(lobby);
                    lobby = lobbyRef;
                    if (lobby.TrainingSession != null || session.ModelHandles.Count != 0 || session.SceneHandle != null)
                        throw new Exception("Practice exit leaked scene or model handles.");
                }
                Debug.Log("BRAWL_AI_PLAY_COMPLETE: two fresh rooms, real server snapshots, stationary/moving target, no skills, clean exit.");
            }
            catch (Exception e) { Debug.LogError($"BRAWL_AI_PLAY_FAIL {e}"); }
            finally
            {
                lobby = lobbyRef;
                if (lobby != null && lobby.TrainingSession != null)
                {
                    lobby.TrainingSession.AIProbe = null;
                    await BrawlTrainingHelper.Exit(lobby);
                }
            }
        }
        private static async ETTask Next(BrawlTrainingSession session)
        {
            if (session.AIProbe == null || session.Closing || session.CancelRequested) throw new Exception("Practice closed.");
            ETTask<bool> pending = ETTask<bool>.Create();
            session.AIProbe.Pending = pending;
            ETCancellationToken timeoutCancellation = new();
            Timeout(session, pending).Coroutine(timeoutCancellation);
            try
            {
                if (!await pending) throw new Exception("Practice closed or no valid server snapshot for 5 seconds.");
            }
            finally { timeoutCancellation.Cancel(); }
        }
        private static async ETTask Timeout(BrawlTrainingSession session, ETTask<bool> pending)
        {
            BrawlPracticeClientComponent client = session.MovementClient;
            if (client == null) { Abort(session); return; }
            await client.Root().TimerComponent.WaitAsync(5000);
            ETCancellationToken cancellation = await ETTask.GetContextAsync<ETCancellationToken>();
            if (cancellation.IsCancel()) return;
            if (session.AIProbe?.Pending == pending) Abort(session);
        }
        public static void Abort(BrawlTrainingSession session)
        {
            BrawlAIPlayProbe probe = session.AIProbe;
            if (probe == null) return;
            probe.Direction = Vector2.zero;
            ETTask<bool> pending = probe.Pending;
            probe.Pending = null;
            pending?.SetResult(false);
        }
        public static void Notify(BrawlTrainingSession session)
        {
            ETTask<bool> pending = session.AIProbe?.Pending;
            if (pending == null) return;
            session.AIProbe.Pending = null;
            pending.SetResult(true);
        }        private static void Capture(BrawlTrainingSession session, string path)
        {
            Camera camera = session.BattleCamera;
            RenderTexture texture = new(1280, 720, 24);
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                UnityEngine.Object.Destroy(image); UnityEngine.Object.Destroy(texture);
            }
        }
    }
}