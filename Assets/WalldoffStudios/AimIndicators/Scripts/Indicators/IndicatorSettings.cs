using System;
using UnityEngine;

namespace WalldoffStudios.Indicators
{
    public class IndicatorSettings : MonoBehaviour
    {
        public IndicatorType IndicatorType;
        
        [SerializeField] private bool alwaysDisplayIndicator = false;
        [SerializeField] private Transform endPointTarget;
        [SerializeField] private Texture2D mainTex;
        [SerializeField] private bool renderEdges = true;
        [SerializeField] private Texture2D edgeTex;
        [SerializeField] private float range = 40.0f;
        [SerializeField] private float edgePadding = 0.5f;
        [SerializeField] private float radialSize = 20.0f;
        [SerializeField] private float fov = 60.0f;
        [SerializeField] private int raycasts = 45;
        [SerializeField] private float timeBetweenRaycasts;
        [SerializeField] private float minDistanceForUpdate = 0.001f;
        [SerializeField] private float lerpTime = 0.1f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private bool useHitDetection = true;
        [SerializeField] private bool drawDebug = true;
        [SerializeField] private float height = 30.0f;
        [SerializeField] private float meshWidth = 4.0f;
        [SerializeField] private float resolution = 0.02f;
        [SerializeField] private float distortion = 0.017f;
        [SerializeField] private float offset = 1.0f;
        [SerializeField] private float brightness = 1.0f;
        [SerializeField] private Color mainColor = Color.white;
        [SerializeField] private bool useFillEffect = true;
        [SerializeField] private Color fillColor = Color.cyan;
        [SerializeField] private float fillSpeed = 1.2f;


        public bool AlwaysDisplayIndicator => alwaysDisplayIndicator;
        public Transform EndPointTarget => endPointTarget;
        public Texture2D MainTex
        {
            get => mainTex;
            set => mainTex = value;
        }

        public bool RenderEdges => renderEdges;

        public Texture2D EdgeTex
        {
            get => edgeTex;
            set => edgeTex = value;
        }

        public float Range => range;
        public float EdgePadding => edgePadding;
        public float RadialSize => radialSize;
        public float FOV => fov;
        public int Raycasts => raycasts;
        public float TimeBetweenRaycasts => timeBetweenRaycasts;
        public float MinDistanceForUpdate => minDistanceForUpdate;
        public float LerpTime => lerpTime;
        public LayerMask ObstacleMask => obstacleMask;
        public bool UseHitDetection => useHitDetection;
        public bool DrawDebug => drawDebug;
        public float Height => height;
        public float MeshWidth => meshWidth;
        public float Resolution => resolution;
        public float Distortion => distortion;
        public float Offset => offset;
        public float Brightness => brightness;
        public Color MainColor => mainColor;
        public bool UseFillEffect => useFillEffect;
        public Color FillColor => fillColor;
        public float FillSpeed => fillSpeed;

        private void Awake()
        {
            if (endPointTarget == null && IndicatorType == IndicatorType.PARABOLIC)
            {
                throw new SystemException($"endPointTarget is null on gameObject {gameObject.name}");
            }
        } 
    }
}