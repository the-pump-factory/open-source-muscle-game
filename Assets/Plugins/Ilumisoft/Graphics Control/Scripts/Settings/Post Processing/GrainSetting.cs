using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [AddComponentMenu("Graphics Control/Settings/Grain Setting")]
    [DisallowMultipleComponent()]
    public class GrainSetting : ToggleGraphicSetting
    {
        public override string GetSettingName() => "Grain";
    }
}