using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [AddComponentMenu("Graphics Control/Settings/Chromatic Aberration Setting")]
    [DisallowMultipleComponent()]
    public class ChromaticAberrationSetting : ToggleGraphicSetting
    {
        public override string GetSettingName() => "Chromatic Aberration";
    }
}