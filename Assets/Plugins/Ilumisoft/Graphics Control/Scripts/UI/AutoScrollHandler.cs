using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ilumisoft.GraphicsControl.UI
{
    /// <summary>
    /// <see cref="UnityEngine.UI.ScrollRect"/> does not automatically scroll up or down when using the keyboard or gamepad.
    /// This class automatically handles scrolling, by scrolling towards the selected element of a  <see cref="UnityEngine.UI.ScrollRect"/> when selection changes.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    [DefaultExecutionOrder(1)]
    public class AutoScrollHandler : MonoBehaviour
    {
        /// <summary>
        ///  The currently selected game object
        /// </summary>
        GameObject selected = null;

        /// <summary>
        /// The previously selected game object
        /// </summary>
        GameObject previousSelected = null;

        [SerializeField]
        [Tooltip("Scroll speed when changing selection")]
        float scrollSpeed = 5.0f;

        /// <summary>
        /// Reference to the controlled <see cref="UnityEngine.UI.ScrollRect"/> component
        /// </summary>
        ScrollRect ScrollRect { get; set; }

        private void Awake()
        {
            ScrollRect = GetComponent<ScrollRect>();
        }

        private void Start()
        {
            selected = EventSystem.current.currentSelectedGameObject;
            previousSelected = selected;

            // Auto focus the first element
            ScrollRect.verticalScrollbar.value = 1;
        }

        void Update()
        {
            selected = EventSystem.current.currentSelectedGameObject;

            // Check whether the selection has changed
            if (selected != previousSelected)
            {
                previousSelected = selected;

                if (selected != null)
                {
                    OnSelectionChanged();
                }
            }
        }
     
        /// <summary>
        /// Scroll towards the selected element when the selection has changed
        /// </summary>
        void OnSelectionChanged()
        {
            if (TryGetIndex(selected, out var index))
            {
                StopAllCoroutines();
                StartCoroutine(ScrollTowards(index));
            }
        }

        /// <summary>
        /// Tries to get the index of the given element in the content
        /// </summary>
        /// <param name="element"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        bool TryGetIndex(GameObject element, out int index)
        {
            index = 0;

            foreach (Transform child in ScrollRect.content)
            {
                if (element == child.gameObject)
                {
                    return true;
                }

                index++;
            }

            return false;
        }

        /// <summary>
        /// Scrolls towards the content element with the given index
        /// </summary>
        /// <param name="targetIndex"></param>
        /// <returns></returns>
        IEnumerator ScrollTowards(int targetIndex)
        {
            var scrollbar = ScrollRect.verticalScrollbar;

            float target = 1 - targetIndex / (float)(ScrollRect.content.childCount - 1);

            while (Mathf.Abs(scrollbar.value - target) > 0.01f)
            {
                scrollbar.value = Mathf.MoveTowards(scrollbar.value, target, scrollSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }
}