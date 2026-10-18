using UnityEngine;
using WalldoffStudios.Extensions;

namespace WalldoffStudios.Indicators
{
    public class ParabolicIndicator : IndicatorBase
    {
        private Vector4[] vertPoints;
        private int vertIndex;
        private float gravity;

        private static readonly int VertPoints = Shader.PropertyToID("_Lengths");
        
        protected override void SetIndicatorType() => IndicatorType = IndicatorType.PARABOLIC;
        
        protected override void Start()
        {
            base.Start();
            CreateMesh();
            
            gravity = Physics.gravity.y;
            ToggleAimRenderer(settings.AlwaysDisplayIndicator);
        }

        public override void OnValuesUpdated()
        {
            base.OnValuesUpdated();
        }

        private void CreateMesh()
        {
            UpdateTargetPos(1.0f);

            MeshGenerator.CreateParabolicMesh(
                settings.Resolution,
                settings.MeshWidth,
                settings.Height,
                transform,
                settings.EndPointTarget, 
                meshFilter);

            vertPoints = meshFilter.mesh.vertices.ToVector4Array();
            SetVertPoints();
        }

        public override void ToggleAim(bool toggle) => ToggleAimRenderer(toggle);

        public override void UpdateAim(Vector2 aimDir)
        {
            float aimMagnitude = aimDir.magnitude;
            if(aimMagnitude < 0.1f) return;
            
            UpdateTargetPos(aimMagnitude);
            Vector3 targetPos = CalculateTargetPos();

            MeshGenerator.CalculatePathWithHeight(targetPos, settings.Height, out var velocity, out var angle, out var time);
            DrawPath(velocity, angle, time, settings.Resolution);
        }

        private void UpdateTargetPos(float aimMagnitude)
        {
            float power = Mathf.Clamp(aimMagnitude, 0.1f, 1.0f);
            
            Vector3 currentTargetPos = settings.EndPointTarget.localPosition;
            currentTargetPos.z = settings.Range * power;
            settings.EndPointTarget.localPosition = currentTargetPos;
        }

        private Vector3 CalculateTargetPos()
        {
            Vector3 shootDir = settings.EndPointTarget.position - transform.position;
            
            Vector3 groundDir;
            groundDir.x = shootDir.x;
            groundDir.y = 0;
            groundDir.z = shootDir.z;
        
            Vector3 targetPos;
            targetPos.x = groundDir.magnitude;
            targetPos.y = shootDir.y;
            targetPos.z = 0;
            return targetPos;
        }

        private void DrawPath(float velocity, float angle, float time, float step)
        {
            vertIndex = 0;

            for (float i = 0; i < time; i += step)
            {
                Vector2 pos = GetIndicatorPos(velocity, i, angle);
                
                if (vertIndex > 1) SetVertPoints(pos);
                vertIndex++;
            }

            CleanupLastVertPoints(GetIndicatorPos(velocity, time, angle));
            SetVertPoints();
        }

        private Vector2 GetIndicatorPos(float velocity, float time, float angle)
        {
            Vector2 pos;
            pos.x = velocity * time * Mathf.Cos(angle);
            pos.y = velocity * time * Mathf.Sin(angle) - 0.5f * -gravity * Mathf.Pow(time, 2);
            return pos;
        }

        private void SetVertPoints(Vector2 pos)
        {
            //bool to vary between left and right sides
            bool even = vertIndex % 2 == 0;
            float x = even ? -0.5f : 0.5f;

            Vector4 vertPos;
            vertPos.x = settings.MeshWidth * x;
            vertPos.y = pos.y;
            vertPos.z = pos.x;
            vertPos.w = 0;

            vertPoints[vertIndex] = vertPos;
        }

        private void CleanupLastVertPoints(Vector2 pos)
        {
            Vector4 vertPos;
            vertPos.x = settings.MeshWidth * 0.5f;
            vertPos.y = pos.y;
            vertPos.z = pos.x;
            vertPos.w = 0;
            
            vertPoints[vertIndex] = vertPos;
            vertPos.x = settings.MeshWidth * -0.5f;
            vertPoints[vertIndex + 1] = vertPos;
        }

        private void SetVertPoints()
        {
            if(vertPoints == null) return;
            meshRenderer.GetPropertyBlock(matPropertyBlock);
            matPropertyBlock.SetVectorArray(VertPoints, vertPoints);
            meshRenderer.SetPropertyBlock(matPropertyBlock);
        }
    }
}
