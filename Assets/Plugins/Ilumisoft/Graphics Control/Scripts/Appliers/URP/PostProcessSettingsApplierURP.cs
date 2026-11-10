using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ilumisoft.GraphicsControl.Rendering.Universal
{
    [RequireComponent(typeof(Volume))]
    public class PostProcessSettingsApplierURP : GraphicSettingsApplier
    {
        Volume PostProcessVolume { get; set; }

        GraphicSettingsManager GraphicSettingsManager { get; set; }

        private void Awake()
        {
            GraphicSettingsManager = FindObjectOfType<GraphicSettingsManager>();
            PostProcessVolume = GetComponent<Volume>();
        }

        void Start()
        {
            ApplySettings();
        }

        public override void ApplySettings()
        {
            ApplySetting<BloomSetting, Bloom>();
            ApplySetting<GrainSetting, FilmGrain>();
            ApplySetting<MotionBlurSetting, MotionBlur>();
            ApplySetting<ChromaticAberrationSetting, ChromaticAberration>();
        }

        void ApplySetting<TGraphicSetting, TPostProcessEffect>()
            where TGraphicSetting : ToggleGraphicSetting
            where TPostProcessEffect : VolumeComponent
        {
            // Settings cannot be applied when no profile has been set
            if (PostProcessVolume.profile == null)
            {
                return;
            }

            // Try to get the effect and the settings and enable/disable effect depending on the settings
            if (PostProcessVolume.profile.TryGet<TPostProcessEffect>(out var effect) &&
                GraphicSettingsManager.TryGet<TGraphicSetting>(out var setting))
            {
                effect.active = setting.IsEnabled();
            }
        }
    }
}