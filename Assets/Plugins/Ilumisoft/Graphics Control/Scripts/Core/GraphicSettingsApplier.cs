using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    /// <summary>
    /// Abstract base class for all <see cref="GraphicSettingsApplier"/>'s.
    /// </summary>
    public abstract class GraphicSettingsApplier : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            if (GraphicSettingsManager.Instance != null)
            {
                GraphicSettingsManager.Instance.Register(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (GraphicSettingsManager.Instance != null)
            {
                GraphicSettingsManager.Instance.Unregister(this);
            }
        }

        public abstract void ApplySettings();
    }
}