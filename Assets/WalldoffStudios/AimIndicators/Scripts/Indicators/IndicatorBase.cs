using System.Collections;
using UnityEngine;

namespace WalldoffStudios.Indicators
{
    public enum IndicatorType
    {
        CONE,
        LINE,
        PARABOLIC,
        TARGET,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(IndicatorSettings))]
    public abstract class IndicatorBase : MonoBehaviour
    {
        private Coroutine fillRoutine;

        protected IndicatorSettings settings;
        public IndicatorType IndicatorType { get; protected set; }
        
        protected MaterialPropertyBlock matPropertyBlock;
        protected MeshRenderer meshRenderer;
        protected MeshFilter meshFilter;

        public bool Is2D { protected get; set; }

        protected static readonly int MainTex = Shader.PropertyToID("_MainTex");
        protected static readonly int MainColor = Shader.PropertyToID("_MainColor");
        protected static readonly int FillColor = Shader.PropertyToID("_FillColor");
        protected static readonly int Fill = Shader.PropertyToID("_Fill");
        protected static readonly int Brightness = Shader.PropertyToID("_Brightness");

        protected virtual void Awake()
        {
            settings = GetComponent<IndicatorSettings>();
            
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            matPropertyBlock = new MaterialPropertyBlock();
            
            SetKeyWords();
            SetIndicatorType();
        }

        protected virtual void SetKeyWords(){}

        protected virtual void Start()
        {
            SetMaterial(settings.MainTex);
            SetColor(settings.MainColor, settings.FillColor);
            SetBrightness();
        }

        protected abstract void SetIndicatorType();

        public virtual void UpdateAim(Vector2 aimDir){}
        
        public abstract void ToggleAim(bool toggle);
        protected void ToggleAimRenderer(bool toggle) => meshRenderer.enabled = toggle;
        public virtual void OnValuesUpdated()
        {
            SetColor(settings.MainColor, settings.FillColor);
            SetBrightness();
        }

        public void AnimateFillAmount()
        {
            if (settings.UseFillEffect == false) return;
            if (fillRoutine != null) StopCoroutine(fillRoutine);
            fillRoutine = StartCoroutine(UpdateAimFill());
        }
        
        private IEnumerator UpdateAimFill()
        {
            float fillAmount = 0.0f;
            while (fillAmount < 1.0f)
            {
                fillAmount += Time.deltaTime * settings.FillSpeed;
                SetFillAmount(fillAmount);
                yield return null;
            }
            SetFillAmount(1.0f);
        }

        private void SetBrightness()
        {
            meshRenderer.GetPropertyBlock(matPropertyBlock);
            matPropertyBlock.SetFloat(Brightness, settings.Brightness);
            meshRenderer.SetPropertyBlock(matPropertyBlock);
        }

        public void SetMaterial(Texture2D tex2D)
        {
            if(tex2D == null){ return;}
            meshRenderer.GetPropertyBlock(matPropertyBlock);
            matPropertyBlock.SetTexture(MainTex, tex2D);
            meshRenderer.SetPropertyBlock(matPropertyBlock);
        }

        protected void SetColor(Color mainColor, Color fillColor)
        {
            meshRenderer.GetPropertyBlock(matPropertyBlock);
            matPropertyBlock.SetColor(MainColor, mainColor);
            matPropertyBlock.SetColor(FillColor, fillColor);
            meshRenderer.SetPropertyBlock(matPropertyBlock);
        }

        public void SetFillAmount(float fillAmount)
        {
            float fill = Mathf.Clamp(fillAmount, 0.0f, 1.0f);
            meshRenderer.GetPropertyBlock(matPropertyBlock);
            matPropertyBlock.SetFloat(Fill, fill);
            meshRenderer.SetPropertyBlock(matPropertyBlock);
        }
    }
}