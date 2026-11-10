using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [DisallowMultipleComponent()]
    [DefaultExecutionOrder(-1)]
    public class DefaultGraphicSettingsManager : GraphicSettingsManager
    {
        List<GraphicSetting> settings = new();

        public override List<GraphicSetting> GetGraphicSettings() => settings;

        public override T Get<T>()
        {
            foreach (var setting in settings)
            {
                if (setting is T targetSetting)
                {
                    return targetSetting;
                }
            }

            return default;
        }

        public override bool TryGet<T>(out T graphicSetting)
        {
            foreach (var setting in settings)
            {
                if (setting is T targetSetting)
                {
                    graphicSetting = targetSetting;
                    return true;
                }
            }

            graphicSetting = default;

            return false;
        }

        protected override void Awake()
        {
            base.Awake();

            settings = GetComponents<GraphicSetting>().ToList();
        }

        private void Start()
        {
            foreach (var setting in settings)
            {
                setting.Initialize();
            }

            LoadSettings();
            ApplySettings();
        }

        public override void LoadSettings()
        {
            foreach (var setting in settings)
            {
                if (setting is GraphicSetting storeable)
                {
                    storeable.LoadSetting();
                }
            }
        }

        public override void SaveSettings()
        {
            foreach (var setting in settings)
            {
                if (setting is GraphicSetting storeable)
                {
                    storeable.SaveSetting();
                }
            }
        }

        public override void ApplySettings()
        {
            foreach (var settingApplier in GraphicSettingsAppliers)
            {
                settingApplier.ApplySettings();
            }
        }

        private void Reset()
        {
            if(!TryGetComponent<GraphicSettingsStorage>(out _))
            {
                gameObject.AddComponent<DefaultGraphicSettingsStorage>();
            }
        }
    }
}