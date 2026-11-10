using UnityEditor;
using UnityEngine;

namespace Ilumisoft.GraphicsControl.Editor
{
    class GraphicsCotnrolSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateStartupProfileConfigurationProvider() => CreateProvider("Project/Graphics Control", Configuration.Find());

        static SettingsProvider CreateProvider(string settingsWindowPath, Object asset)
        {
            var provider = AssetSettingsProvider.CreateProviderFromObject(settingsWindowPath, asset);

            provider.keywords = SettingsProvider.GetSearchKeywordsFromSerializedObject(new SerializedObject(asset));
            return provider;
        }
    }
}