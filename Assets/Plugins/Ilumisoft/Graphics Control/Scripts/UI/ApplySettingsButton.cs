using UnityEngine;
using UnityEngine.UI;

namespace Ilumisoft.GraphicsControl.UI
{
    [RequireComponent(typeof(Button))]
    public class ApplySettingsButton : MonoBehaviour
    {
        Button Button { get; set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
            Button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            GraphicSettingsManager.Instance.ApplySettings();
            GraphicSettingsManager.Instance.SaveSettings();
        }
    }
}