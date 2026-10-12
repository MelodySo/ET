using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    public static partial class LobbyPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this LobbyPanelComponent self)
        {
            BrawlHeroMenuHelper.Create(self);
        }

        [EntitySystem]
        private static void Destroy(this LobbyPanelComponent self)
        {
            BrawlHeroMenuHelper.Destroy(self);
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this LobbyPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始

        [YIUIInvoke(LobbyPanelComponent.OnEventEnterMapInvoke)]
        private static async ETTask OnEventEnterMapInvoke(this LobbyPanelComponent self)
        {
            if (self.TrainingSession != null) return;
            EntityRef<LobbyPanelComponent> selfRef = self;
            await EnterMapHelper.EnterMapAsync(self.Root());
            self = selfRef;
            await self.UIPanel.CloseAsync();
        }

        #endregion YIUIEvent结束
    }
}
