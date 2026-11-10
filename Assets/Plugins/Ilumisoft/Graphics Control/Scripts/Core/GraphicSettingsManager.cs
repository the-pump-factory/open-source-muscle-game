using System.Collections.Generic;

namespace Ilumisoft.GraphicsControl
{
    public abstract class GraphicSettingsManager : SingletonBehaviour<GraphicSettingsManager>
    {
        protected List<GraphicSettingsApplier> GraphicSettingsAppliers { get; set; } = new();

        public abstract List<GraphicSetting> GetGraphicSettings();

        public abstract T Get<T>() where T : GraphicSetting;

        public abstract bool TryGet<T>(out T graphicSetting) where T : GraphicSetting;

        public abstract void LoadSettings();

        public abstract void SaveSettings();

        public abstract void ApplySettings();

        public void Register(GraphicSettingsApplier graphicSettingsApplier)
        {
            if(graphicSettingsApplier != null && !GraphicSettingsAppliers.Contains(graphicSettingsApplier))
            {
                GraphicSettingsAppliers.Add(graphicSettingsApplier);
            }
        }

        public void Unregister(GraphicSettingsApplier graphicSettingsApplier)
        {
            GraphicSettingsAppliers.Remove(graphicSettingsApplier);
        }
    }
}