using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [RequireComponent(typeof(GraphicSettingsManager))]
    public abstract class GraphicSetting : MonoBehaviour, IGraphicSetting
    {
        protected GraphicSettingsStorage GraphicSettingsStorage { get; set; }

        protected virtual void Awake()
        {
            GraphicSettingsStorage = GetComponent<GraphicSettingsStorage>();
        }

        public abstract void Initialize();
        public abstract string GetSettingName();
        public abstract void LoadSetting();
        public abstract void SaveSetting();
    }
}