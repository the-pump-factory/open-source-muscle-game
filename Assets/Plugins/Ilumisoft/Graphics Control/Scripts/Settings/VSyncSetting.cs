using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Graphics Control/Settings/VSync Setting")]
    public class VSyncSetting : ToggleGraphicSetting
    {
        public override string GetSettingName() => "VSync";
    }
}