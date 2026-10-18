using System;
using System.Collections;
using UnityEngine;
using WalldoffStudios.Extensions;

namespace WalldoffStudios.Indicators
{
    public class LineIndicator : IndicatorBase
    {
        private float hitDistance;

        private Coroutine drawingRoutine;
        private bool isActive;

        private readonly RaycastHit2D[] rayhit2D = new RaycastHit2D[4];
        readonly RaycastHit[] rayHit = new RaycastHit[4];

        private Vector4[] vertPoints;
        private static readonly int VertPoints = Shader.PropertyToID("_VertPoints");

        protected override void SetIndicatorType() => IndicatorType = IndicatorType.LINE;

        protected override void Start()
        {
            base.Start();
            CreateMesh();

            ToggleAimRenderer(settings.AlwaysDisplayIndicator);
        }

        public override void OnValuesUpdated()
        {
            base.OnValuesUpdated();
            
            UpdateWidth();
        }

        private void UpdateWidth()
        {
            for (int i = 0; i < vertPoints.Length; i++)
            {
                bool isEven = i % 2 == 0;
                vertPoints[i].x = isEven ? settings.MeshWidth * -0.5f : settings.MeshWidth * 0.5f;
            }
            SetVertPoints();
        }

        private void CreateMesh()
        {
            MeshGenerator.CreateLineMesh(settings.MeshWidth, settings.Range, Is2D, meshFilter);
            vertPoints = meshFilter.mesh.vertices.ToVector4Array();
            SetVertPoints();
        }
        
        public override void ToggleAim(bool toggle)
        {
            ToggleAimRenderer(toggle);
            if( settings.UseHitDetection == false) return;
            
            isActive = toggle;
            if (toggle == true)
            {
                if(drawingRoutine != null) StopCoroutine(drawingRoutine);
                drawingRoutine = StartCoroutine(UpdateMeshRoutine());
            }
            else
            {
                if (drawingRoutine != null) StopCoroutine(drawingRoutine);
            }
        }
        
        private IEnumerator UpdateMeshRoutine()
        {
            float timer = 0.0f;
            while (isActive)
            {
                timer += Time.deltaTime;
                if (timer > settings.TimeBetweenRaycasts)
                {
                    float newDistance = GetRayHitDistance() - settings.Offset;
                    if (Math.Abs(hitDistance - newDistance) > settings.MinDistanceForUpdate)
                    {
                        hitDistance = Mathf.Lerp(hitDistance, newDistance, settings.LerpTime);
                        UpdateHitPoints();
                    }
                    timer -= settings.TimeBetweenRaycasts;
                }

                yield return null;
            }
        }

        private void UpdateHitPoints()
        {
            float dist = hitDistance - settings.MeshWidth;
            if (dist < settings.MeshWidth) dist *= 0.75f;
            if (Is2D == true)
            {
                vertPoints[2].y = dist;
                vertPoints[3].y = dist;
                vertPoints[4].y = hitDistance;
                vertPoints[5].y = hitDistance;
            }
            else
            {
                vertPoints[2].z = dist;
                vertPoints[3].z = dist;
                vertPoints[4].z = hitDistance;
                vertPoints[5].z = hitDistance;   
            }

            SetVertPoints();
        }

        private float GetRayHitDistance()
        {
            float totalWidth = settings.MeshWidth - settings.EdgePadding;
            float increment = totalWidth / settings.Raycasts;
            
            Transform localTransform = transform;
            Vector3 playerPos = localTransform.position;
            Vector3 right = localTransform.right;
            
            Vector3 startX = right * (totalWidth * -0.5f);
            startX += right * (increment * 0.5f);

            float distance = settings.Range;

            for (int i = 0; i < settings.Raycasts; i++)
            {
                Vector3 origin = startX;
                origin += right * (increment * i);

                Vector3 target;
                if (Is2D == true)
                {
                    target = origin + localTransform.up;
                }
                else
                {
                    target = origin + localTransform.forward;    
                }

                Vector3 direction = (target - origin).normalized;
                
                if(settings.DrawDebug) Debug.DrawRay(playerPos + origin, direction * settings.Range, Color.red, settings.TimeBetweenRaycasts);

                if (Is2D == true)
                {
                    if (Physics2D.RaycastNonAlloc(playerPos + origin, direction, rayhit2D, settings.Range, settings.ObstacleMask) > 0)
                    {
                        for (int j = 0; j < rayhit2D.Length; j++)
                        {
                            if(rayhit2D[j].collider == null) continue;
                            
                            float dist = rayhit2D[j].distance;
                            if (dist < distance)
                            {
                                distance = dist;
                            }
                        }
                    }
                }
                else
                {
                    if (Physics.RaycastNonAlloc(playerPos + origin, direction, rayHit, settings.Range, settings.ObstacleMask) > 0)
                    {
                        for (int j = 0; j < rayHit.Length; j++)
                        {
                            if(rayHit[j].collider == null) continue;
                            
                            if (rayHit[j].distance < distance)
                            {
                                distance = rayHit[j].distance;
                            }   
                        }
                    }   
                }
            }

            return distance;
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
