using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace ET.Client
{
    /// <summary>训练场/练习赛的客户端资源与恢复信息，由大厅持有；练习赛另关联服务器房间。</summary>
    [EnableClass]
    public sealed class BrawlTrainingSession
    {
        public SceneHandle SceneHandle;
        public UnityEngine.SceneManagement.Scene PreviousScene;
        public GameObject RuntimeRoot;
        public GameObject TerrainRoot;
        public GameObject Hero;
        public Camera BattleCamera;
        public EntityRef<BrawlCameraComponent> CameraController;
        public GameObject NPCRoot;
        public int CellSize;
        public BrawlAIPlayProbe AIProbe; // Optional, explicitly started verification only.
        public bool IsPractice;
        public long PracticeEntryId;
        public long PracticeRoomId;
        public long MyUnitId;
        public EntityRef<ClientSenderComponent> PracticeSender;
        public EntityRef<BrawlPracticeClientComponent> MovementClient;
        public long InputSequence;
        public long AttackSequence;
        public long LastProjectileSnapshotSequence;
        public long LastProjectileEventId;
        public IBrawlProjectilePresentation ProjectilePresentation;
        public GameObject ProjectileTemplateRoot;
        public readonly Dictionary<string, GameObject> ProjectileTemplates = new();
        public readonly Dictionary<int, BrawlProjectileConfig> ProjectileVisualConfigs = new();
        public readonly List<BrawlProjectileEffectView> ProjectileEffects = new();
        public readonly List<BrawlProjectileNotice> PendingProjectileEvents = new();
        public readonly HashSet<int> ClearedGrass = new();
        public readonly Dictionary<int, long> PendingClearedGrass = new();
        public readonly List<int> AppliedClearedGrass = new();
        public Vector2 LastMoveDirection = Vector2.up;
        public Vector2 FacingDirection = Vector2.up;
        public BrawlSkillJoystick NormalAttackJoystick;
        public BrawlSkillJoystick SuperSkillJoystick;
        public IBrawlSkillAimPreview SkillAimPreview;
        public readonly Dictionary<BrawlSkillSlot, BrawlSkillAimVisual> SkillAimVisuals = new();
        public long SkillAimUnitId;
        public readonly Dictionary<long, BrawlProjectileView> Projectiles = new();
        public readonly List<long> RemovedProjectiles = new();
        public Button NormalAttackButton;
        public TextMeshProUGUI NormalAttackLabel;
        public TextMeshProUGUI SuperSkillLabel;
        public bool SuperSkillImplemented;
        public Button SuperSkillButton;
        public long LastAcknowledgedInput;
        public long LastServerTick;
        public double RenderTick;
        public float NextInputTime;
        public float LastRenderTime;
        public Vector2 LastSentDirection;
        public Vector2 JoystickDirection;
        public int? JoystickPointer;
        public RectTransform JoystickBase;
        public RectTransform JoystickKnob;
        public readonly List<BrawlPracticeUnitView> PracticeUnits = new();
        public readonly Dictionary<GameObject, BrawlCharacterAnimationView> CharacterAnimations = new();
        public readonly List<BrawlLifeNotice> LastLifeEvents = new();
        public readonly Dictionary<string, AssetHandle> ModelHandles = new();
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
