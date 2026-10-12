using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace ET.Client
{
    /// <summary>本地训练场的临时资源与恢复信息，由大厅持有，不是网络对局。</summary>
    [EnableClass]
    public sealed class BrawlTrainingSession
    {
        public SceneHandle SceneHandle;
        public UnityEngine.SceneManagement.Scene PreviousScene;
        public GameObject RuntimeRoot;
        public GameObject Overlay;
        public Image LoadingBackground;
        public TextMeshProUGUI StatusText;
        public Button ExitButton;
        public CanvasGroup LobbyGroup;
        public bool CreatedLobbyGroup;
        public float PreviousAlpha;
        public bool PreviousInteractable;
        public bool PreviousBlocksRaycasts;
        public bool Loading;
        public bool Closing;
        public bool CancelRequested;
        public readonly List<Camera> DisabledCameras = new();
        public readonly List<AudioListener> DisabledListeners = new();
    }
}
