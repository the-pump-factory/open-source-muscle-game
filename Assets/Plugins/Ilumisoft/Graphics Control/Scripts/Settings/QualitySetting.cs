using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Graphics Control/Settings/Quality Setting")]
    public class QualitySetting : MultiOptionGraphicSetting<int>
    {
        public override void Initialize()
        {
            var names = QualitySettings.names;

            for (int i = 0; i < names.Length; i++)
            {
                AddOption(names[i], i);
            }

            SetIndex(QualitySettings.GetQualityLevel());
        }

        public override string GetSettingName() => "Quality";

        public override void SaveSetting()
        {
            GraphicSettingsStorage.SetInt(key: GetSettingName(), value: GetIndex());
        }

        public override void LoadSetting()
        {
            // The key of the setting
            string key = GetSettingName();

            // Default value used as a fallback option
            int defaultValue = QualitySettings.GetQualityLevel();

            // Get the stored value
            int storedValue = GraphicSettingsStorage.GetInt(key, defaultValue);

            // Apply the stored value
            SetIndex(storedValue);
        }
    }
}