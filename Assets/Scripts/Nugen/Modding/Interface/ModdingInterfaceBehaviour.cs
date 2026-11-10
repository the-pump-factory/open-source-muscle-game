using System.Collections.Generic;
using UnityEngine;

namespace nugen.modding
{

    [ExecuteInEditMode]
    public class ModdingInterfaceBehaviour : MonoBehaviour
    {


        public delegate void RaycastClick(int mouseButton, ModUI.ClickType clickType, bool clickedObject, RaycastHit hit);

        public event RaycastClick OnRaycastClick;

        void Update()
        {

            if (!Application.isPlaying) return;

            if (ModUI.behaviour != this)
            {

                GameObject.Destroy(this.gameObject);

                return;

            }

            ModUI.Render();

            if (OnRaycastClick != null)
            {

                for (int a = 0; a < 3; a++)
                {

                    bool down = Input.GetMouseButtonDown(a);

                    if (down || Input.GetMouseButton(a))
                    {

                        OnRaycastClick(a, down ? ModUI.ClickType.Down : ModUI.ClickType.Hold, Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Camera.main.farClipPlane), hit);

                        break;

                    }

                }

            }

        }

#if UNITY_EDITOR

        void OnDrawGizmos()
        {

            ModUI.Render();

        }

#endif

    }

}
