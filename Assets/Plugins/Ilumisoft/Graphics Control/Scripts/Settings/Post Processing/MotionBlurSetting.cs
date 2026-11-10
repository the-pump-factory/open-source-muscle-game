using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [AddComponentMenu("Graphics Control/Settings/Motion Blur Setting")]
    [DisallowMultipleComponent()]
    public class MotionBlurSetting : ToggleGraphicSetting
    {
        public override string GetSettingName() => "Motion Blur";
    }
}