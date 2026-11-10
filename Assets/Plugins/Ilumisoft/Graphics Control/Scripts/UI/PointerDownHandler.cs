using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Ilumisoft.GraphicsControl.UI
{
    public class PointerDownHandler : MonoBehaviour, IPointerDownHandler
    {
        public UnityEvent OnClick = new();

        public void OnPointerDown(PointerEventData eventData)
        {
            OnClick.Invoke();
        }
    }
}