using System.Collections.Generic;
using UnityEngine;

namespace nugen.modding
{

    public static class ModUI
    {

        public static GameObject[] GetSelectedGameObjects()
        {

            GameObject[] GetRuntimeSelectedGameObjects()
            {

                return new GameObject[0];

            }

#if UNITY_EDITOR

            if (Application.isPlaying)
            {

                return GetRuntimeSelectedGameObjects();

            }
            else
            {

                return UnityEditor.Selection.gameObjects;

            }

#else

            return GetRuntimeSelectedGameObjects();

#endif

        }

        public enum ClickType
        {

            Down, Hold, Up

        }

        private static Material UIMaterial, UIMaterial_selected, UIMaterial_visualizer, UIMaterial_auxiliary;

        public static Material GetDefaultMaterial()
        {

            if (UIMaterial != null) return UIMaterial;

            if (Miscellaneous.IsHDRP())
            {

                Shader shader = Shader.Find("HDRP/Unlit");

                UIMaterial = new Material(shader);

                UIMaterial.SetColor("_UnlitColor", Color.blue);

            }
            else
            {

                Shader shader = Shader.Find("Unlit/Color");

                UIMaterial = new Material(shader);

                UIMaterial.SetColor("_Color", Color.blue);

            }

            return UIMaterial;

        }

        public static Material GetSelectionMaterial()
        {

            if (UIMaterial_selected != null) return UIMaterial_selected;

            if (Miscellaneous.IsHDRP())
            {

                Shader shader = Shader.Find("HDRP/Unlit");

                UIMaterial_selected = new Material(shader);

                UIMaterial_selected.SetColor("_UnlitColor", Color.green);

            }
            else
            {

                Shader shader = Shader.Find("Unlit/Color");

                UIMaterial_selected = new Material(shader);

                UIMaterial_selected.SetColor("_Color", Color.green);

            }

            return UIMaterial_selected;

        }

        public static Material GetVisualizerMaterial()
        {

            if (UIMaterial_visualizer != null) return UIMaterial_visualizer;

            if (Miscellaneous.IsHDRP())
            {

                Shader shader = Shader.Find("HDRP/Unlit");

                UIMaterial_visualizer = new Material(shader);

                UIMaterial_visualizer.SetColor("_UnlitColor", Color.cyan);

            }
            else
            {

                Shader shader = Shader.Find("Unlit/Color");

                UIMaterial_visualizer = new Material(shader);

                UIMaterial_visualizer.SetColor("_Color", Color.cyan);

            }

            return UIMaterial_visualizer;

        }

        public static Material GetAuxiliaryMaterial()
        {

            if (UIMaterial_auxiliary != null) return UIMaterial_auxiliary;

            if (Miscellaneous.IsHDRP())
            {

                Shader shader = Shader.Find("HDRP/Unlit");

                UIMaterial_auxiliary = new Material(shader);

                UIMaterial_auxiliary.SetColor("_UnlitColor", Color.magenta);

            }
            else
            {

                Shader shader = Shader.Find("Unlit/Color");

                UIMaterial_auxiliary = new Material(shader);

                UIMaterial_auxiliary.SetColor("_Color", Color.magenta);

            }

            return UIMaterial_auxiliary;

        }

        [ExecuteInEditMode]
        public abstract class Renderer : MonoBehaviour
        {

            public abstract void OnRender();

            void LateUpdate()
            {

                if (!Application.isPlaying) return;

                OnRender();

            }

#if UNITY_EDITOR

            void OnDrawGizmos()
            {

                OnRender();

            }

#endif

        }

        public interface Renderable {

            public void Render();

            public void RenderGizmo();

        }

        public static ModdingInterfaceBehaviour behaviour = null;

        public static ModdingInterfaceBehaviour GetBehaviour()
        {

            CheckBehaviour();

            return behaviour;

        }

        public static void CheckBehaviour()
        {

            if (behaviour == null)
            {

                ModdingInterfaceBehaviour[] toRemove = GameObject.FindObjectsOfType<ModdingInterfaceBehaviour>();

                for (int a = 0; a < toRemove.Length; a++) GameObject.DestroyImmediate(toRemove[a].gameObject);

                behaviour = new GameObject("ModUI").AddComponent<ModdingInterfaceBehaviour>();

            }

        }

        private static List<Renderable> renderLoad = new List<Renderable>();

        public static void Queue(Renderable renderable)
        {

            CheckBehaviour();

            renderLoad.Add(renderable);

        }

        public static Color color = Color.green;

        public static void Render()
        {

            if (renderLoad == null) renderLoad = new List<Renderable>();

            if (!Application.isEditor || Application.isPlaying)
            {

                for (int a = 0; a < renderLoad.Count; a++) renderLoad[a].Render();

            }
            else
            {

                for (int a = 0; a < renderLoad.Count; a++) renderLoad[a].RenderGizmo();

            }

            renderLoad.Clear();

        }

        public static void SetColor(Color color)
        {

            Queue(new RenderColor(color));

        }

        public struct RenderColor : Renderable
        {

            public Color color;

            public RenderColor(Color color)
            {

                this.color = color;

            }

            public void Render()
            {

                ModUI.color = color;

            }

            public void RenderGizmo()
            {

                Gizmos.color = color;

            }

        }

        public static void DrawLine(Vector3 p1, Vector3 p2)
        {

            Queue(new RenderLine(p1, p2));

        }

        public static void DrawRay(Vector3 p1, Vector3 direction)
        {

            Queue(new RenderLine(p1, p1 + direction));

        }

        public struct RenderLine : Renderable
        {

            public Vector3 p1, p2;

            public RenderLine(Vector3 p1, Vector3 p2)
            {
                this.p1 = p1;
                this.p2 = p2;
            }

            public void Render()
            {

            }

            public void RenderGizmo()
            {

                Gizmos.DrawLine(p1, p2);

            }

        }

        public static void DrawSphere(Vector3 position, float radius)
        {

            Queue(new RenderSphere(position, radius));

        }

        public static void DrawWireSphere(Vector3 position, float radius)
        {

            Queue(new RenderWireSphere(position, radius));

        }

        public struct RenderSphere: Renderable
        {

            public Vector3 position;

            public float radius;

            public RenderSphere(Vector3 position, float radius)
            {
                this.position = position;
                this.radius = radius;
            }

            public void Render()
            {

            }

            public void RenderGizmo()
            {

                Gizmos.DrawSphere(position, radius);

            }

        }

        public struct RenderWireSphere : Renderable
        {

            public Vector3 position;

            public float radius;

            public RenderWireSphere(Vector3 position, float radius)
            {
                this.position = position;
                this.radius = radius;
            }

            public void Render()
            {

            }

            public void RenderGizmo()
            {

                Gizmos.DrawWireSphere(position, radius);

            }

        }

    }

}
