using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace WalldoffStudios.Indicators
{
	public class ConeIndicator : IndicatorBase
	{
		private bool isDrawing;
		private Coroutine drawingRoutine;

		private readonly RaycastHit2D[] rayhit2D = new RaycastHit2D[1];
		private readonly RaycastHit[] rayHit = new RaycastHit[1];
		private float[] lengths;

		private static readonly int VectorLengths = Shader.PropertyToID("_Lengths");
		private static readonly int Range = Shader.PropertyToID("_Range");
		private static readonly int EdgeTex = Shader.PropertyToID("_EdgeTex");
		
		private LocalKeyword edgeKeyWord;
		private static readonly int DistortionAmount = Shader.PropertyToID("_DistortionAmount");

		protected override void SetKeyWords()
		{
			edgeKeyWord = new LocalKeyword(meshRenderer.material.shader, "EDGE_ON");
		}

		protected override void Start()
		{
			base.Start();
			SetDistortion(settings.Distortion);
			SetRange(settings.Range);
			CreateMesh();
			SetEdgeTex();
			ToggleAimRenderer(settings.AlwaysDisplayIndicator);
			ToggleEdgeRendering();
		}

		protected override void SetIndicatorType() => IndicatorType = IndicatorType.CONE;
		
		public override void OnValuesUpdated()
		{
			base.OnValuesUpdated();
			SetDistortion(settings.Distortion);
			SetEdgeTex();
			SetRange(settings.Range);
			ToggleEdgeRendering();
		}

		private void ToggleEdgeRendering()
		{
			if (settings.RenderEdges == true)
			{
				meshRenderer.material.EnableKeyword(edgeKeyWord);
			}
			else
			{
				meshRenderer.material.DisableKeyword(edgeKeyWord);
			}
		}

		private void CreateMesh()
		{
			MeshGenerator.CreateConeMesh(settings.Raycasts, settings.FOV, transform, Is2D, meshFilter);
			
			lengths = new float[settings.Raycasts +1];
			for (int i = 0; i < lengths.Length; i++)
			{
				lengths[i] = 1.0f;
			}
			SetVertLengths();
		}

		public override void ToggleAim(bool toggle)
		{
			ToggleAimRenderer(toggle);
			if(settings.UseHitDetection == false) return;
			
			isDrawing = toggle;
			if (toggle == true)
			{
				if(drawingRoutine != null) StopCoroutine(drawingRoutine);
				drawingRoutine = StartCoroutine(UpdateMeshRoutine());
			}
			else
			{
				if(drawingRoutine != null) StopCoroutine(drawingRoutine);
			}
		}

		IEnumerator UpdateMeshRoutine()
		{
			float timer = 0.0f;
			while (isDrawing)
			{
				timer += Time.deltaTime;
				if (timer > settings.TimeBetweenRaycasts)
				{
					UpdateHitPoints();
					timer -= settings.TimeBetweenRaycasts;
				}

				yield return null;
			}
		}

		private void UpdateHitPoints()
		{
			float angleIncrements = settings.FOV / settings.Raycasts;
			var localTransform = transform;
			bool targetUpdated = false; 
				
			for (int i = 0; i <= settings.Raycasts; i++)
			{
				float angle = 0.0f;
				Vector3 pos;
				if (Is2D == true)
				{
					angle = -(localTransform.localEulerAngles.z - settings.FOV * 0.5f + angleIncrements * i);
					pos = (Vector2)localTransform.position - Get2DHitPoint(angle);
				}
				else
				{
					angle = localTransform.eulerAngles.y - settings.FOV * 0.5f + angleIncrements * i;
					pos = localTransform.position - Get3DHitPoint(angle);
				}

				float magnitude = pos.magnitude + settings.Offset;
				lengths[i] = Mathf.Clamp(magnitude / settings.Range, 0.0f, 1.0f);
				// float saturated = Mathf.Clamp(magnitude / settings.Range, 0.0f, 1.0f);
				
				// if (Math.Abs(saturated - lengths[i]) > settings.MinDistanceForUpdate)
				// {
				// 	
				// }
			}

			SetVertLengths();
		}

		private Vector2 Get2DHitPoint(float angle)
		{
			Vector2 dir = MeshGenerator.DirectionFromAngle2D(angle).normalized;
			
			if(settings.DrawDebug) Debug.DrawRay(transform.position, dir * settings.Range, Color.red, settings.TimeBetweenRaycasts);
			
			if (Physics2D.RaycastNonAlloc(transform.position, dir, rayhit2D, settings.Range, settings.ObstacleMask) > 0)
			{
				return rayhit2D[0].point;
			}

			return (Vector2)transform.position + dir * settings.Range;
		}

		private Vector3 Get3DHitPoint(float angle)
		{
			Vector3 dir = MeshGenerator.DirectionFromAngle3D(angle).normalized;

			if(settings.DrawDebug) Debug.DrawRay(transform.position, dir * settings.Range, Color.red, settings.TimeBetweenRaycasts);
			
			if (Physics.RaycastNonAlloc(transform.position, dir, rayHit, settings.Range, settings.ObstacleMask) > 0)
			{
				return rayHit[0].point;
			}

			return transform.position + dir * settings.Range;
		}

		private void SetDistortion(float distortion)
		{
			meshRenderer.GetPropertyBlock(matPropertyBlock);
			matPropertyBlock.SetFloat(DistortionAmount, distortion);
			meshRenderer.SetPropertyBlock(matPropertyBlock);
		}

		private void SetRange(float range)
		{
			meshRenderer.GetPropertyBlock(matPropertyBlock);
			matPropertyBlock.SetFloat(Range, range);
			meshRenderer.SetPropertyBlock(matPropertyBlock);
		}

		private void SetVertLengths()
		{
			meshRenderer.GetPropertyBlock(matPropertyBlock);
			matPropertyBlock.SetFloatArray(VectorLengths, lengths);
			meshRenderer.SetPropertyBlock(matPropertyBlock);
		}

		private void SetEdgeTex()
		{
			if(settings.EdgeTex == null) return;
			meshRenderer.GetPropertyBlock(matPropertyBlock);
			matPropertyBlock.SetTexture(EdgeTex, settings.EdgeTex);
			meshRenderer.SetPropertyBlock(matPropertyBlock);
		}
	}
}