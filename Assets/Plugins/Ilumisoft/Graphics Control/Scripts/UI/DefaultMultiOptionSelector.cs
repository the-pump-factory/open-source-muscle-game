using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Ilumisoft.GraphicsControl.UI
{
    /// <summary>
    /// Default implementation of the <see cref="MultiOptionSelector"/>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class DefaultMultiOptionSelector : MultiOptionSelector, IMoveHandler
    {
        [Header("Text")]
        [SerializeField]
        TMPro.TextMeshProUGUI labelText;

        [SerializeField]
        TMPro.TextMeshProUGUI valueText;

        [Header("Audio")]
        [SerializeField]
        AudioClip selectionChangedSFX;

        [SerializeField]
        AudioClip verticalMovementSFX;

        [Header("Settings")]
        [Tooltip("When enabled, the selector will loop through the options when reaching the limits.")]
        [SerializeField]
        bool loop = true;

        /// <summary>
        /// The index of the current selection
        /// </summary>
        int currentIndex = 0;

        /// <summary>
        /// A list of all possible values
        /// </summary>
        List<string> Values;

        /// <summary>
        /// Event being invoked when the index has been changed
        /// </summary>
        UnityAction<int> OnSelectionChanged { get; set; }

        /// <summary>
        /// Reference to the Audio Source Component
        /// </summary>
        AudioSource AudioSource { get; set; }

        private void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
        }

        public override void Initialize(string label, List<string> values, int index, UnityAction<int> onSelectionChanged)
        {
            OnSelectionChanged += onSelectionChanged;

            labelText.text = label;
            Values = values;
            currentIndex = index;

            UpdateValueText();
        }

        public void OnMove(AxisEventData eventData)
        {
            Move(eventData.moveVector);
        }

        /// <summary>
        /// Selects the next option
        /// </summary>
        public void SelectNextOption() => Move(Vector2.right);

        /// <summary>
        /// Selects the previous Option
        /// </summary>
        public void SelectPreviousOption() => Move(Vector2.left);

        private void Move(Vector2 direction)
        {
            int previousIndex = currentIndex;

            // Increase the index when moving to the right
            if (direction.x > 0)
            {
                currentIndex++;

                if (currentIndex >= Values.Count)
                {
                    currentIndex = loop ? 0 : Values.Count - 1;
                }
            }

            // Decrease the index when moving to the left
            if (direction.x < 0)
            {
                currentIndex--;

                if (currentIndex < 0)
                {
                    currentIndex = loop ? Values.Count - 1 : 0;
                }
            }

            if (direction.y != 0)
            {
                if (AudioSource != null && verticalMovementSFX != null)
                {
                    PlayAudioFeedback(verticalMovementSFX);
                }
            }

            // Check whether the index has changed
            if (previousIndex != currentIndex)
            {
                OnSelectionChanged?.Invoke(currentIndex);
                UpdateValueText();
                PlayAudioFeedback(selectionChangedSFX);
            }
        }

        void UpdateValueText()
        {
            valueText.text = Values[currentIndex];
        }

        void PlayAudioFeedback(AudioClip audioClip)
        {
            if (AudioSource != null && audioClip != null)
            {
                AudioSource.PlayOneShot(audioClip);
            }
        }
    }
}