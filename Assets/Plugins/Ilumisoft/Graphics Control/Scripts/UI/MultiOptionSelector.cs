using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ilumisoft.GraphicsControl.UI
{
    public abstract class MultiOptionSelector : MonoBehaviour
    {
        public abstract void Initialize(string label, List<string> values, int index, UnityAction<int> onSelectionChanged);
    }
}