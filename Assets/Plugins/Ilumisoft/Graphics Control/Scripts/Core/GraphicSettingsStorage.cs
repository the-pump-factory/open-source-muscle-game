using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    public abstract class GraphicSettingsStorage : MonoBehaviour
    {
        public abstract bool GetBool(string key, bool defaultValue);
        public abstract float GetFloat(string key, float defaultValue);
        public abstract int GetInt(string key, int defaultValue);
        public abstract string GetString(string key, string defaultValue);
        public abstract void SetBool(string key, bool value);
        public abstract void SetFloat(string key, float value);
        public abstract void SetInt(string key, int value);
        public abstract void SetString(string key, string value);
    }
}