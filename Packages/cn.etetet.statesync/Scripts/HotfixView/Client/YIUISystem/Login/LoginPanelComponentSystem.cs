using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    public static partial class LoginPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this LoginPanelComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this LoginPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this LoginPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始

        [YIUIInvoke(LoginPanelComponent.OnEventLoginInvoke)]
        private static async ETTask OnEventLoginInvoke(this LoginPanelComponent self)
        {
            if (self.LoginInProgress) return;
            self.LoginInProgress = true;
            EntityRef<LoginPanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = self.Root();
            try
            {
                GlobalComponent globalComponent = self.Root().GetComponent<GlobalComponent>();
                await LoginHelper.Login(self.Root(), globalComponent.GlobalConfig.Address, self.u_ComAccount.text, self.u_ComPassword.text);
            }
            catch (RpcException e)
            {
                Scene root = rootRef;
                if (root != null) TipsHelper.OpenSync<TipsMessageViewComponent>(root, LoginAccountInputHelper.ErrorMessage(e.Error));
            }
            catch (Exception)
            {
                Scene root = rootRef;
                if (root != null) TipsHelper.OpenSync<TipsMessageViewComponent>(root, "无法连接登录服务，请确认服务器和数据库已启动。");
                Log.Warning("Login failed because the service could not be reached.");
            }
            finally
            {
                self = selfRef;
                if (self != null) self.LoginInProgress = false;
            }
        }

        #endregion YIUIEvent结束
    }
}
