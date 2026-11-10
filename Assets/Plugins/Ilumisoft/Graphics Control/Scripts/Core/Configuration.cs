using UnityEngine;

namespace Ilumisoft.GraphicsControl
{
    [CreateAssetMenu(menuName = "Gaphics Control/Configuration", fileName = "Configuration")]
    public class Configuration : ScriptableObject
    {
        public const string ConfigurationPath = "Ilumisoft/Graphics Control/Configuration";

        [Header("Prefabs")]
        public GameObject GraphicSettingsManager;

        [Tooltip("Automatically creates a persistent instance of the Graphic Settings Manager at startup when enabled")]
        public bool AutoCreate = true;

        public GameObject GraphicSettingsPanel;

        public static Configuration Find()
        {
            var result = Resources.Load<Configuration>(ConfigurationPath);

            return result;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            var configuration = Find();

            // Automatically create the Graphic Settings Manager at startup
            if(configuration.AutoCreate && configuration.GraphicSettingsManager !=null)
            {
                var instance = Instantiate(configuration.GraphicSettingsManager);
                instance.name = configuration.GraphicSettingsManager.name;
                DontDestroyOnLoad(instance);
            }
        }
    }
}