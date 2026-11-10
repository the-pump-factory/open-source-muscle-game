using System.Collections;
using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    /// <summary>
    /// Applies global <see cref="GraphicSetting"/>'s like the resolution, vsync and general quality settings (if they have been added to the <see cref="GraphicSettingsManager"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Graphics Control/Global Settings Applier")]
    public class GlobalSettingsApplier : GraphicSettingsApplier
    {
        public override void ApplySettings()
        {
            ApplyScreenSettings();
            ApplyQuality();
            ApplyVSync();
        }

        void ApplyScreenSettings()
        {
            // Get the current resolution and fullscreen mode
            var resolution = Screen.currentResolution;
            var fullScreenMode = Screen.fullScreenMode;

            // Get the selected resolution setting
            if (TryGetSetting<ResolutionSetting>(out var resolutionSetting))
            {
                resolution = resolutionSetting.GetSelectedOption();
            }

            // Get the selected fullscreen mode setting
            if (TryGetSetting<FullScreenModeSetting>(out var fullScreenModeSetting))
            {
                fullScreenMode = fullScreenModeSetting.GetSelectedOption();
            }

            // If any of the settings is available apply resolution/fullScreenMode (if neither ResolutionSetting or FullScreenModeSetting is available, no update is required)
            if (resolutionSetting != null || fullScreenModeSetting != null)
            {
                Screen.SetResolution(resolution.width, resolution.height, fullScreenMode);
            }
        }

        void ApplyVSync()
        {
            if (TryGetSetting<VSyncSetting>(out var setting))
            {
                bool isEnabled = setting.GetSelectedOption();

                QualitySettings.vSyncCount = isEnabled ? 1 : 0;
            }
        }

        void ApplyQuality()
        {
            if (TryGetSetting<QualitySetting>(out var setting))
            {
                int qualityLevel = setting.GetSelectedOption();

                QualitySettings.SetQualityLevel(qualityLevel);
            }
        }

        bool TryGetSetting<T>(out T setting) where T : GraphicSetting
        {
            return GraphicSettingsManager.Instance.TryGet(out setting);
        }
    }
}