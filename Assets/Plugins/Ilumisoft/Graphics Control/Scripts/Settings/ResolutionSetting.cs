using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Graphics Control/Settings/Resolution Setting")]
    public class ResolutionSetting : MultiOptionGraphicSetting<Resolution>
    {
        public override void Initialize()
        {
            var supportedResolutions = Screen.resolutions;

            foreach (var resolution in supportedResolutions)
            {
                AddOption($"{resolution.width}x{resolution.height}", resolution);
            }

            SelectOption(r => AreEqual(r, Screen.currentResolution), defaultIndex: 0);
        }

        public override string GetSettingName() => "Resolution";

        public override void SaveSetting()
        {
            GraphicSettingsStorage.SetInt("Resolution_Width", GetSelectedOption().width);
            GraphicSettingsStorage.SetInt("Resolution_Height", GetSelectedOption().height);
        }

        public override void LoadSetting()
        {
            var currentResolution = Screen.currentResolution;

            Resolution resolution = new()
            {
                width = GraphicSettingsStorage.GetInt("Resolution_Width", currentResolution.width),
                height = GraphicSettingsStorage.GetInt("Resolution_Height", currentResolution.height)
            };

            SelectOption(r => AreEqual(r, resolution), defaultIndex: 0);
        }

        bool AreEqual(Resolution a, Resolution b)
        {
            return a.width == b.width && a.height == b.height;
        }
    }
}