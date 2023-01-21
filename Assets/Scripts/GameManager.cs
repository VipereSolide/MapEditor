#region Namespaces
using System.Collections.Generic;
using System.Collections;

using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

using TMPro;
#endregion

namespace VipereSolide
{
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        public static GameManager instance;

        protected void GetSingleton()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        #endregion

        #region Variables

        #region Protected

        protected List<TMP_InputField> _selectedItems = new List<TMP_InputField>();
        protected int _UILayer;

        #endregion
        #region Public

        public bool IsFocused
        {
            get { return _selectedItems.Count > 0; }
        }

        [HideInInspector] public UnityEvent<Vector2> onMouseDown;

        #endregion

        #endregion

        #region Methods

        #region Unity

        protected void Awake()
        {
            GetSingleton();
        }

        protected void Start()
        {
            GetUILayer();
        }

        protected void Update()
        {
            GetEvents();
        }

        #endregion
        #region Public

        public void AddSelectable(TMP_InputField _selectable)
        {
            _selectedItems.Add(_selectable);
        }

        public void ReleaseSelectable(TMP_InputField _selectable)
        {
            _selectedItems.Remove(_selectable);
        }

        /// <summary>
        /// Returns whether the mouse is hovering over UI elements or not by shooting a raycast.
        /// </summary>
        /// <returns></returns>
        public static bool IsPointerOverUIElement()
        {
            return instance._InternalIsPointerOverUIElement(instance.GetEventSystemRaycastResults());
        }

        #endregion
        #region Behaviour

        protected void GetEvents()
        {
            GetOnClickEvent();
        }

        protected void GetOnClickEvent()
        {
            if (Input.GetMouseButtonDown(0))
            {
                onMouseDown?.Invoke(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            }
        }

        protected void GetUILayer()
        {
            _UILayer = LayerMask.NameToLayer("UI");
        }

        protected bool _InternalIsPointerOverUIElement(List<RaycastResult> _eventSystemRaysastResults)
        {
            for (int _i = 0; _i < _eventSystemRaysastResults.Count; _i++)
            {
                RaycastResult _currentRaysastResult = _eventSystemRaysastResults[_i];

                if (_currentRaysastResult.gameObject.layer == _UILayer)
                {
                    return true;
                }
            }

            return false;
        }

        protected List<RaycastResult> GetEventSystemRaycastResults()
        {
            PointerEventData _eventData = new PointerEventData(EventSystem.current);
            _eventData.position = Input.mousePosition;

            List<RaycastResult> _raysastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(_eventData, _raysastResults);

            return _raysastResults;
        }

        #endregion

        #endregion
    }
}