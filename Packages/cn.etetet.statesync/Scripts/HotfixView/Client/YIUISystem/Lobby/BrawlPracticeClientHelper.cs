using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    public static class BrawlPracticeClientHelper
    {
        public static async ETTask<int> Enter(LobbyPanelComponent self, BrawlTrainingSession session, int heroId)
        {
            EntityRef<LobbyPanelComponent> owner = self;
            ClientSenderComponent sender = self.Root().GetComponent<ClientSenderComponent>();
            if (sender == null) throw new InvalidOperationException("练习赛需要先登录服务器。");
            session.PracticeSender = sender;
            session.PracticeEntryId = IdGenerater.Instance.GenerateId();
            C2G_BrawlPracticeEnter request = C2G_BrawlPracticeEnter.Create();
            request.HeroId = heroId;
            request.EntryId = session.PracticeEntryId;
            using G2C_BrawlPracticeEnter response = (G2C_BrawlPracticeEnter)await sender.Call(request, false);
            self = owner;
            if (response.Error != ErrorCode.ERR_Success)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message) ? $"练习赛请求失败：{response.Error}" : response.Message);
            session.PracticeRoomId = response.RoomId;
            session.MyUnitId = response.MyUnitId;
            session.CellSize = response.CellSize;
            if (self == null || session.CancelRequested) return response.LevelId;
            if (response.RoomId <= 0 || response.MyUnitId <= 0 || response.CellSize <= 0)
                throw new InvalidOperationException("服务器返回的房间数据无效。");
            BrawlHeroConfigCategory heroes = self.Fiber().GetSingleton<BrawlHeroConfigCategory>();
            BrawlNPCConfigCategory npcs = self.Fiber().GetSingleton<BrawlNPCConfigCategory>();
            HashSet<long> unitIds = new();
            bool foundPlayer = false;
            foreach (BrawlPracticeUnitInfo info in response.Units)
            {
                BrawlPracticeUnitKind kind = (BrawlPracticeUnitKind)info.Kind;
                if (info.UnitId <= 0 || !unitIds.Add(info.UnitId) || !Enum.IsDefined(typeof(BrawlPracticeUnitKind), kind))
                    throw new InvalidOperationException("服务器单位 ID 或种类无效。");
                string address = kind == BrawlPracticeUnitKind.NPC
                    ? npcs.GetOrDefault(info.ConfigId)?.ModelPath
                    : heroes.GetOrDefault(info.ConfigId)?.ModelPath;
                if (string.IsNullOrWhiteSpace(address)) throw new InvalidOperationException($"缺少单位模型配置：{kind} / {info.ConfigId}");
                if (info.UnitId == response.MyUnitId)
                {
                    if (kind != BrawlPracticeUnitKind.Player || info.ConfigId != heroId)
                        throw new InvalidOperationException("服务器返回的玩家英雄不一致。");
                    foundPlayer = true;
                    session.FacingDirection = new Vector2(info.FacingX, info.FacingZ);
                }
                session.PracticeUnits.Add(new BrawlPracticeUnitView
                {
                    UnitId = info.UnitId, ConfigId = info.ConfigId, Kind = kind, Faction = (BrawlFaction)info.Faction,
                    FacingDirection = new Vector2(info.FacingX, info.FacingZ),
                    Position = new Vector3(info.X, info.Y, info.Z), ModelPath = address,
                });
            }
            if (!foundPlayer) throw new InvalidOperationException("服务器单位清单中缺少当前玩家。");
            session.MovementClient = self.Root().AddComponent<BrawlPracticeClientComponent, BrawlTrainingSession>(session);
            BrawlJoystickHelper.Create(session);
            return response.LevelId;
        }

        public static async ETTask Exit(BrawlTrainingSession session)
        {
            if (session.PracticeEntryId == 0) return;
            ClientSenderComponent sender = session.PracticeSender;
            long entryId = session.PracticeEntryId;
            session.PracticeEntryId = 0;
            // 登录连接已经销毁时，服务器通过连接生命周期清理房间。
            if (sender == null) return;
            try
            {
                C2G_BrawlPracticeExit request = C2G_BrawlPracticeExit.Create();
                request.EntryId = entryId;
                using G2C_BrawlPracticeExit response = (G2C_BrawlPracticeExit)await sender.Call(request, false);
                if (response.Error != ErrorCode.ERR_Success) Log.Warning($"Brawl practice exit failed: {response.Error} {response.Message}");
            }
            catch (Exception exception)
            {
                // 网络故障不能阻止本地场景恢复。重入会替换旧房间，断线也会清理。
                Log.Warning($"Brawl practice exit request failed: {exception.Message}");
            }
        }
    }
}
