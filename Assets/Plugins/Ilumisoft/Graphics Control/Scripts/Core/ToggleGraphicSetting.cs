using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    /// <summary>
    /// Toggle Graphic Settings can either be on or off. 
    /// This for example used for Bloom, Motion Blur or Grain Post Processing effects.
    /// </summary>
    public abstract class ToggleGraphicSetting : MultiOptionGraphicSetting<bool>
    {
        [Tooltip("Whether the option should be enabled by default or not")]
        public bool IsEnabledByDefault = true;

        public override void Initialize()
        {
            // Toggle graphic settings can be eitehr on or off
            AddOption("Off", false);
            AddOption("On", true);

            SelectOption(IsEnabledByDefault);
        }

        /// <summary>
        /// Returns true if the setting is on, false otherwise
        /// </summary>
        /// <returns></returns>
        public bool IsEnabled() => GetSelectedOption();

        /// <summary>
        /// Loads the stored settings
        /// </summary>
        public override void LoadSetting()
        {
            bool value = GraphicSettingsStorage.GetBool(GetSettingName(), IsEnabled());

            SelectOption(value);
        }

        /// <summary>
        /// Saves the selected option
        /// </summary>
        public override void SaveSetting()
        {
            GraphicSettingsStorage.SetBool(GetSettingName(), IsEnabled());
        }
    }
}