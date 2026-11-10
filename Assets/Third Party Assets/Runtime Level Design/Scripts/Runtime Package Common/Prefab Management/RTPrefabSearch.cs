using UnityEngine;
using UnityEngine.UI;

namespace RLD
{
    public class RTPrefabSearch : MonoBehaviour
    {
        private InputField _searchField;

        //public InputField SearchField { get { return _searchField; } }
        public InputField SearchField { get { 
                if (_searchField == null) _searchField = GetComponentInChildren<InputField>();
                return _searchField; } }

        private void Awake()
        {
            _searchField = GetComponentInChildren<InputField>();
        }
    }
}
