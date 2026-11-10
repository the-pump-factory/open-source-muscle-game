using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [AddComponentMenu("Graphics Control/Settings/Bloom Setting")]
    [DisallowMultipleComponent()]
    public class BloomSetting : ToggleGraphicSetting
    {
        public override string GetSettingName() => "Bloom";
    }
}