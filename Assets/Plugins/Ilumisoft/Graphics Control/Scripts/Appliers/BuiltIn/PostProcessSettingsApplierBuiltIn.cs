using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Ilumisoft.GraphicsControl.Rendering
{
    [RequireComponent(typeof(PostProcessVolume))]
    public class PostProcessSettingsApplierBuiltIn : GraphicSettingsApplier
    {
        PostProcessVolume PostProcessVolume { get; set; }

        GraphicSettingsManager GraphicSettingsManager { get; set; }

        private void Awake()
        {
            GraphicSettingsManager = FindObjectOfType<GraphicSettingsManager>();
            PostProcessVolume = GetComponent<PostProcessVolume>();
        }

        void Start()
        {
            ApplySettings();
        }

        public override void ApplySettings()
        {
            ApplySetting<BloomSetting, Bloom>();
            ApplySetting<GrainSetting, Grain>();
            ApplySetting<MotionBlurSetting, MotionBlur>();
            ApplySetting<AmbientOcclusionSetting, AmbientOcclusion>();
            ApplySetting<ChromaticAberrationSetting, ChromaticAberration>();
        }

        void ApplySetting<TGraphicSetting, TPostProcessEffect>()
            where TGraphicSetting : ToggleGraphicSetting
            where TPostProcessEffect : PostProcessEffectSettings
        {
            // Settings cannot be applied when no profile has been set
            if (PostProcessVolume.profile == null)
            {
                return;
            }

            // Try to get the effect and the settings and enable/disable effect depending on the settings
            if (PostProcessVolume.profile.TryGetSettings<TPostProcessEffect>(out var effect) &&
                GraphicSettingsManager.TryGet<TGraphicSetting>(out var setting))
            {
                effect.enabled.value = setting.IsEnabled();
            }
        }
    }
}