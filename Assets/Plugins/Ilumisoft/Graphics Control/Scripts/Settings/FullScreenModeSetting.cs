using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [System.Serializable, System.Flags]
    public enum FullScreenModeOptions
    {
        ExclusiveFullScreen = 1,
        FullScreenWindow = 2,
        MaximizedWindow = 4,
        Windowed = 8,
        Everything = 0b1111
    }

   
    [DisallowMultipleComponent]
    [AddComponentMenu("Graphics Control/Settings/Full Screen Mode Setting")]
    public class FullScreenModeSetting : MultiOptionGraphicSetting<FullScreenMode>
    {
        [SerializeField]
        FullScreenModeOptions EnabledOptions = FullScreenModeOptions.Everything;

        public FullScreenMode DefaultOption = FullScreenMode.ExclusiveFullScreen;

        public override void Initialize()
        {
            AddOptionIfEnabled(FullScreenMode.Windowed);
            AddOptionIfEnabled(FullScreenMode.FullScreenWindow);
            AddOptionIfEnabled(FullScreenMode.MaximizedWindow);
            AddOptionIfEnabled(FullScreenMode.ExclusiveFullScreen);

            // Always add the default option if not already added
            if(!IsEnabled(DefaultOption))
            {
                AddOption(GetDisplayName(DefaultOption), DefaultOption);
            }

            SelectOption(DefaultOption);
        }

        bool IsEnabled(FullScreenMode fullScreenMode)
        {
            return fullScreenMode switch
            {
                FullScreenMode.ExclusiveFullScreen => EnabledOptions.HasFlag(FullScreenModeOptions.ExclusiveFullScreen),
                FullScreenMode.FullScreenWindow => EnabledOptions.HasFlag(FullScreenModeOptions.FullScreenWindow),
                FullScreenMode.MaximizedWindow => EnabledOptions.HasFlag(FullScreenModeOptions.MaximizedWindow),
                FullScreenMode.Windowed => EnabledOptions.HasFlag(FullScreenModeOptions.Windowed),
                _ => false,
            };
        }

        /// <summary>
        /// Adds the given option to the option table if it has been set in the EnabledOptions
        /// </summary>
        /// <param name="value"></param>
        /// <param name="option"></param>
        void AddOptionIfEnabled(FullScreenMode option)
        {
            if (IsEnabled(option))
            {
                AddOption(GetDisplayName(option), option);
            }
        }

        /// <summary>
        /// Gets the display name the given <see cref="FullScreenMode"/>
        /// </summary>
        /// <param name="fullScreenMode"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        string GetDisplayName(FullScreenMode fullScreenMode) => fullScreenMode switch
        {
            FullScreenMode.ExclusiveFullScreen => "Exclusive Fullscreen",
            FullScreenMode.FullScreenWindow => "Full Screen Window",
            FullScreenMode.MaximizedWindow => "Maximized Window",
            FullScreenMode.Windowed => "Window",
            _ => throw new System.NotImplementedException(),
        };

        public override string GetSettingName() => "Full Screen Mode";

        public override void SaveSetting()
        {
            GraphicSettingsStorage.SetInt("Full Screen Mode", (int)GetSelectedOption());
        }

        public override void LoadSetting()
        {
            int value = GraphicSettingsStorage.GetInt("Full Screen Mode", (int)DefaultOption);

            SelectOption((FullScreenMode)value);
        }
    }
}