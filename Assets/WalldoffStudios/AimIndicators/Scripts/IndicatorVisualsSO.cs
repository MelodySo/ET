using System;
using UnityEngine;

namespace WalldoffStudios.Indicators
{
    [CreateAssetMenu(fileName = "Indicator Visuals", menuName = "WalldoffStudios/Indicators/Indicator Visuals")]
    public class IndicatorVisualsSO : ScriptableObject
    {
        [SerializeField] private Texture2D[] coneTextures = null;
        [SerializeField] private Texture2D[] lineTextures = null;
        [SerializeField] private Texture2D[] parabolicTextures = null;
        [SerializeField] private Texture2D[] targetTextures = null;

        [SerializeField] private Texture2D[] edgeTextures = null;

        public Tuple<Texture2D, int> GetTextureAndIndex(IndicatorType type, int index, bool next)
        {
            Texture2D tex = null;
            int texIndex = 0;
            
            switch (type)
            {
                case IndicatorType.CONE:
                    if (coneTextures.Length == 0) return null;

                    texIndex = next ? index % coneTextures.Length : (index + coneTextures.Length) % coneTextures.Length;
                    tex = coneTextures[texIndex];
                    break;

                case IndicatorType.LINE:
                    if (lineTextures.Length == 0) return null;
                    
                    texIndex = next ? index % lineTextures.Length : (index + lineTextures.Length) % lineTextures.Length;
                    tex = lineTextures[texIndex];
                    break;

                case IndicatorType.PARABOLIC:
                    if (parabolicTextures.Length == 0) return null;
                    
                    texIndex = next ? index % parabolicTextures.Length : (index + parabolicTextures.Length) % parabolicTextures.Length;
                    tex = parabolicTextures[texIndex];
                    break;

                case IndicatorType.TARGET:
                    if (targetTextures.Length == 0) return null;
                    
                    texIndex = next ? index % targetTextures.Length : (index + targetTextures.Length) % targetTextures.Length;
                    tex = targetTextures[texIndex];
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            
            return new Tuple<Texture2D, int>(tex, texIndex);
        }

        public Texture2D GetEdgeTexture(int index)
        {
            int texIndex = index % edgeTextures.Length;
            return edgeTextures[texIndex];
        }
    }
}