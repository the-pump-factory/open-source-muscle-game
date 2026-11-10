using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [AddComponentMenu("Graphics Control/Settings/Ambient Occlusion Setting")]
    [DisallowMultipleComponent()]
    public class AmbientOcclusionSetting : ToggleGraphicSetting
    {
        public override string GetSettingName() => "Ambient Occlusion";
    }
}