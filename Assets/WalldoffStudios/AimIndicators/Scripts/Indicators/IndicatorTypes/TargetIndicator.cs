using System.Collections;
using UnityEngine;

namespace WalldoffStudios.Indicators
{
    public class TargetIndicator : IndicatorBase
    {
        private Vector4[] vertPoints;
        private static readonly int VertPoints = Shader.PropertyToID("_VertPoints");

        private float cachedRadialSize;
        private Coroutine meshUpdatingRoutine;

        protected override void SetIndicatorType() => IndicatorType = IndicatorType.TARGET;

        protected override void Start()
        {
            base.Start();
            ToggleAimRenderer(settings.AlwaysDisplayIndicator);
            if (meshUpdatingRoutine == null)
            {
                meshUpdatingRoutine = StartCoroutine(UpdateMeshRoutine());
            }
            CreateMesh();
            UpdateVertices();
        }

        private void CreateMesh() => meshFilter.mesh = MeshGenerator.CreateTargetMesh(settings.RadialSize, Is2D);

        public override void OnValuesUpdated()
        {
            base.OnValuesUpdated();
            if (meshUpdatingRoutine == null)
            {
                meshUpdatingRoutine = StartCoroutine(UpdateMeshRoutine());
            }
        }

        private IEnumerator UpdateMeshRoutine()
        {
            while (Mathf.Abs(cachedRadialSize - settings.RadialSize) > 0.01f)
            {
                cachedRadialSize = Mathf.Lerp(cachedRadialSize, settings.RadialSize, settings.LerpTime);
                UpdateRadialSize();
                
                yield return null;
            }
            
            cachedRadialSize = settings.RadialSize;
            UpdateRadialSize();
            meshUpdatingRoutine = null;
        }

        private void UpdateRadialSize()
        {
            if (Is2D == true)
            {
                vertPoints = new Vector4[4]
                {
                    new Vector4(-0.5f,-0.5f,0.0f,0.0f) * cachedRadialSize,
                    new Vector4(0.5f,-0.5f,0.0f,0.0f) * cachedRadialSize,
                    new Vector4(-0.5f,0.5f,0.0f,0.0f) * cachedRadialSize,
                    new Vector4(0.5f,0.5f,0.0f,0.0f) * cachedRadialSize
                };   
            }
            else
            {
                vertPoints = new Vector4[4]
                {
                    new Vector4(-0.5f,0,-0.5f,0) * cachedRadialSize,
                    new Vector4(0.5f,0,-0.5f,0) * cachedRadialSize,
                    new Vector4(-0.5f,0,0.5f,0) * cachedRadialSize,
                    new Vector4(0.5f,0,0.5f,0) * cachedRadialSize
                };
            }
            UpdateVertices();
        }

        private void UpdateVertices()
        {
            meshRenderer.GetPropertyBlock(matPropertyBlock);
            matPropertyBlock.SetVectorArray(VertPoints, vertPoints);
            meshRenderer.SetPropertyBlock(matPropertyBlock);
        }
        
        public override void ToggleAim(bool toggle) => ToggleAimRenderer(toggle);
    }
}