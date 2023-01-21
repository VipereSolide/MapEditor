using System.Collections.Generic;
using System.Collections;

using UnityEngine.UI;
using UnityEngine;

using TMPro;

namespace VipereSolide.UI.PrefabBrowser
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PrefabItem : MonoBehaviour
    {
        [System.Serializable]
        public class Padding
        {
            public float top;
            public float bottom;
            public float left;
            public float right;

            public Vector2 GetPadding()
            {
                return new Vector2(right + left, top + bottom);
            }
        }

        [Header("Selected State")]
        [SerializeField] protected Padding _selectedStatePadding;

        [Header("References")]
        [SerializeField] protected RectTransform _selectedStateObject;

        [Space]
        [SerializeField] protected TMP_Text _itemNameText;
        [SerializeField] protected RectTransform _textSelectedState;

        [Space]
        [SerializeField] protected Image _itemIconImage;

        protected bool _selected;

        public bool selected
        {
            get { return _selected; }
            set { _selected = value; }
        }

        public Padding selectedStatePadding
        {
            get { return _selectedStatePadding; }
            set { _selectedStatePadding = value; }
        }

        private void Update()
        {
            if (_selectedStateObject == null)
            {
                this.enabled = false;
                return;
            }

            if (_selected)
            {
                _selectedStateObject.gameObject.SetActive(true);

                if (_textSelectedState != null && _itemNameText != null)
                {
                    UpdateSelectedState();
                }
                else
                {
                    this.enabled = false;
                }
            }
            else
            {
                _selectedStateObject.gameObject.SetActive(false);
            }
        }

        private void UpdateSelectedState()
        {
            _textSelectedState.sizeDelta = _itemNameText.rectTransform.sizeDelta + _selectedStatePadding.GetPadding();
        }
    }
}