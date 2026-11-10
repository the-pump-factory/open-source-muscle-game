using UnityEditor;
using UnityEngine;

namespace Ilumisoft.GraphicsControl.Editor
{
    public static class MenuItems
    {
        // Add a menu item to create custom GameObjects.
        // Priority 1 ensures it is grouped with the other menu items of the same kind
        // and propagated to the hierarchy dropdown and hierarchy context menus.
        [MenuItem("GameObject/UI/Graphic Settings Panel", false, 10)]
        static void CreateCustomGameObject(MenuCommand menuCommand)
        {
            Configuration configuration = Configuration.Find();

            if(configuration.GraphicSettingsPanel == null)
            {
                Debug.LogWarning("Could not create Graphic Settings Panel, because no prefab has been assigned in the project settings.");
                return;
            }

            // Create a custom game object
            GameObject go = PrefabUtility.InstantiatePrefab(configuration.GraphicSettingsPanel) as GameObject;// new GameObject("Graphic Settings Panel");

            go.name = "Graphic Settings Panel";

            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

            Selection.activeObject = go;
        }
    }
}