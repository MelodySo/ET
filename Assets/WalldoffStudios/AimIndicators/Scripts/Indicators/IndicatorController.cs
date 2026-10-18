using System;
using System.Collections.Generic;
using UnityEngine;

namespace WalldoffStudios.Indicators
{
    [Serializable]
    public struct IndicatorInfo
    {
        public IndicatorType type;
        public IndicatorBase indicator;
        public IndicatorSettings settings;
        [HideInInspector] public int textureIndex;
    }
    public class IndicatorController : MonoBehaviour
    {
        [SerializeField] private List<IndicatorInfo> indicatorInfos = null;
        [SerializeField] private IndicatorVisualsSO indicatorVisuals = null;


        private void OnEnable()
        {
            CollectChildIndicators();
        }

        public void CollectChildIndicators()
        {
            int childCount = transform.childCount;
            int indicatorCount = indicatorInfos.Count;
            
            if (indicatorCount < childCount)
            {
                for (int i = 0; i < childCount - indicatorInfos.Count; i++)
                {
                    indicatorInfos.Add(new IndicatorInfo());
                }
                for (int i = 0; i < childCount; i++)
                {
                    IndicatorInfo info = indicatorInfos[i];
                    Transform child = transform.GetChild(i);
                    if (child.TryGetComponent(out IndicatorBase indicator))
                    {
                        info.indicator = indicator;
                        info.settings = indicator.gameObject.GetComponent<IndicatorSettings>();
                        info.type = info.settings.IndicatorType;
                        info.textureIndex = 0;
                        indicatorInfos[i] = info;
                    }
                }
            }
        }
        
#if UNITY_EDITOR
        private Material[] indicatorMats;
        public void SetMaterialReferences(Material[] mats) => indicatorMats = mats;

        public void SetCorrectIndicatorType()
        {
            for (int i = 0; i < indicatorInfos.Count; i++)
            {
                IndicatorInfo info = indicatorInfos[i];
                if (info.indicator is null)
                {
                    Debug.LogWarning($"Indicator at index {i} was null, trying to collect it now");
                    info.indicator = transform.GetChild(i).GetComponent<IndicatorBase>();
                    info.settings = transform.GetChild(i).GetComponent<IndicatorSettings>();
                }

                if (info.indicator is null)
                {
                    Debug.LogError($"Couldn't collect indicator, won't update indicator class");
                    return;
                }

                GameObject currentIndicatorGO = info.indicator.gameObject;

                switch (info.type)
                {
                    case IndicatorType.CONE:
                        if (info.indicator is ConeIndicator == false)
                        {
                            if (info.indicator != null) DestroyImmediate(info.indicator);
                            currentIndicatorGO.name = "Cone Indicator";
                            info.indicator = currentIndicatorGO.AddComponent<ConeIndicator>();
                            info.indicator.GetComponent<MeshRenderer>().material = indicatorMats[0];
                            info.settings.IndicatorType = IndicatorType.CONE;
                            SetCorrectTextures(i);
                        }
                        break;
                    
                    case IndicatorType.LINE:
                        if (info.indicator is LineIndicator == false)
                        {
                            if (info.indicator != null) DestroyImmediate(info.indicator);
                            currentIndicatorGO.name = "Line Indicator";
                            info.indicator = currentIndicatorGO.AddComponent<LineIndicator>();
                            info.indicator.GetComponent<MeshRenderer>().material = indicatorMats[1];
                            info.settings.IndicatorType = IndicatorType.LINE;
                            SetCorrectTextures(i);
                        }
                        break;
                    
                    case IndicatorType.PARABOLIC:
                        if (info.indicator is ParabolicIndicator == false)
                        {
                            if (info.indicator != null) DestroyImmediate(info.indicator);
                            currentIndicatorGO.name = "Parabolic Indicator";
                            info.indicator = currentIndicatorGO.AddComponent<ParabolicIndicator>();
                            info.indicator.GetComponent<MeshRenderer>().material = indicatorMats[2];
                            info.settings.IndicatorType = IndicatorType.PARABOLIC;
                            SetCorrectTextures(i);
                        }
                        break;
                    
                    case IndicatorType.TARGET:
                        if (info.indicator is TargetIndicator == false)
                        {
                            if (info.indicator != null) DestroyImmediate(info.indicator);
                            currentIndicatorGO.name = "Target Indicator";
                            info.indicator = currentIndicatorGO.AddComponent<TargetIndicator>();
                            info.indicator.GetComponent<MeshRenderer>().material = indicatorMats[3];
                            info.settings.IndicatorType = IndicatorType.TARGET;
                            SetCorrectTextures(i);
                        }
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException(nameof(info.type), info.type, null);
                }
                
                indicatorInfos[i] = info;
                indicatorInfos[i].indicator.transform.SetSiblingIndex(i);
            }
        }

        private void SetCorrectTextures(int index)
        {
            IndicatorInfo info = indicatorInfos[index];

            Tuple<Texture2D, int> texAndIndex = indicatorVisuals.GetTextureAndIndex(info.type, 0, true);
            info.settings.MainTex = texAndIndex.Item1;  

            if (info.type == IndicatorType.CONE && info.settings.EdgeTex == null)
            {
                info.settings.EdgeTex = indicatorVisuals.GetEdgeTexture(0);
            }
            
            indicatorInfos[index] = info;
        }

        public IndicatorType GetCurrentType(int index)
        {
            if (indicatorInfos.Count - 1 < index) return IndicatorType.CONE;
            return indicatorInfos[index].type;
        }

        //When a child gameObject gets deleted manually
        public void ChildTransformDeleted()
        {
            for (int i = 0; i < indicatorInfos.Count; i++)
            {
                IndicatorInfo info = indicatorInfos[i];
                if (info.indicator == null)
                {
                    indicatorInfos.RemoveAt(i);
                    break;   
                }
            }
        }

        public void AddIndicator()
        {
            GameObject newIndicator = new GameObject("Cone Indicator");
            newIndicator.transform.SetParent(transform);
            ConeIndicator coneIndicator = newIndicator.AddComponent<ConeIndicator>();
            newIndicator.GetComponent<MeshRenderer>().material = indicatorMats[0];
            newIndicator.transform.SetSiblingIndex(indicatorInfos.Count -1);
            Vector3 indicatorPos = newIndicator.transform.localPosition;
            indicatorPos.y = 0.0f;
            newIndicator.transform.localPosition = indicatorPos;
            
            indicatorInfos.Add(new IndicatorInfo
            {
                type = IndicatorType.CONE,
                indicator = coneIndicator,
                settings = newIndicator.GetComponent<IndicatorSettings>(),
            });
            SetCorrectTextures(indicatorInfos.Count -1);
        }

        public void DuplicateIndicator(int index)
        {
            IndicatorInfo info = indicatorInfos[index];
            GameObject newIndicator = Instantiate(info.indicator.gameObject, transform);
            newIndicator.name = info.indicator.gameObject.name;
            info.indicator = newIndicator.GetComponent<IndicatorBase>();
            info.settings = newIndicator.GetComponent<IndicatorSettings>();
            indicatorInfos.Add(info);
        }

        public void RemoveIndicator(int index)
        {
            if (indicatorInfos[index].indicator == null) return;
            
            GameObject indicator = indicatorInfos[index].indicator.gameObject;
            DestroyImmediate(indicator);
        }
#endif

        public void SetWorldSpace(bool is2D)
        {
            foreach (IndicatorInfo info in indicatorInfos)
            {
                if(info.indicator == null) continue;
                info.indicator.Is2D = is2D;
            }
        }

        public void ShotIndicator()
        {
            for (int i = 0; i < indicatorInfos.Count; i++)
            {
                if(indicatorInfos[i].indicator == null) continue;
                indicatorInfos[i].indicator.AnimateFillAmount();
            }
        }

        public void UpdateIndicatorAim(Vector2 aimDir)
        {
            for (int i = 0; i < indicatorInfos.Count; i++)
            {
                if(indicatorInfos[i].indicator == null) continue;
                
                if (indicatorInfos[i].indicator.IndicatorType != IndicatorType.PARABOLIC) continue;
                
                indicatorInfos[i].indicator.UpdateAim(aimDir);
            }
        }
        
        public void ToggleAim(bool toggle)
        {
            for (int i = 0; i < indicatorInfos.Count; i++)
            {
                if(indicatorInfos[i].indicator == null) continue;
                indicatorInfos[i].indicator.ToggleAim(toggle);
            }
        }
        
        public void IndicatorResetFillAmount()
        {
            for (int i = 0; i < indicatorInfos.Count; i++)
            {
                if(indicatorInfos[i].indicator == null) continue;
                if (indicatorInfos[i].settings.UseFillEffect == false) continue;
                indicatorInfos[i].indicator.SetFillAmount(0.0f); 
            }
        }

        public void IndicatorToggleTexture(bool next)
        {
            for (int i = 0; i < indicatorInfos.Count; i++)
            {
                IndicatorInfo info = indicatorInfos[i];
                if (info.indicator == null) continue;
                
                info.textureIndex += next ? 1 : -1;
                
                Tuple<Texture2D, int> texAndIndex =
                    indicatorVisuals.GetTextureAndIndex(info.type, info.textureIndex, next);
                
                if (texAndIndex.Item1 == null)
                {
                    throw new SystemException(
                        $"The texture array for {info.type} has 0 elements serialized" +
                        $"Serialize at least 1 texture in the indicator textures scriptable object to get it working");
                }
                
                info.indicator.SetMaterial(texAndIndex.Item1);
                info.textureIndex = texAndIndex.Item2;
                indicatorInfos[i] = info;
            }
        }
    }
}