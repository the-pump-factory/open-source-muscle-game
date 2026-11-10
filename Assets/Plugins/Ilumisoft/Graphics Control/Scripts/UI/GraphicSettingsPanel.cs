using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ilumisoft.GraphicsControl.UI
{
    public class GraphicSettingsPanel : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The parent object all UI selectors will be added to. This would be for example the content GameObject of a ScrollView")]
        Transform container;

        [SerializeField]
        MultiOptionSelector MultiOptionSelectorPrefab;

        /// <summary>
        /// Reference to the Graphic Settings Manager
        /// </summary>
        GraphicSettingsManager GraphicSettingsManager { get; set; }

        private void Awake()
        {
            GraphicSettingsManager = FindObjectOfType<GraphicSettingsManager>();
        }

        private void Start()
        {
            // Get all graphic settings
            var settings = GraphicSettingsManager.GetGraphicSettings();

            // List holding all selectors
            List<GameObject> selectors = new();

            // Create the appropriate selector UI elements for each setting
            foreach (var setting in settings)
            {
                if (setting is IMultiOptionGraphicSetting multiOptionSettings)
                {
                    var multiOptionSelector = Instantiate(MultiOptionSelectorPrefab, container);

                    multiOptionSelector.Initialize(setting.GetSettingName(), multiOptionSettings.GetOptionNames(), multiOptionSettings.GetIndex(), multiOptionSettings.SetIndex);

                    selectors.Add(multiOptionSelector.gameObject);
                }
            }

            // Automatically select the first element
            if (selectors.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(selectors[0]);
            }
        }

    }
}