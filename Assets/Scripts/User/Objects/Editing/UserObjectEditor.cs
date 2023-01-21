using System.Collections.Generic;
using System.Collections;

using UnityEngine;

namespace VipereSolide.User.Objects.Editing
{
    [DisallowMultipleComponent]
    public class UserObjectEditor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected Camera _userCamera;

        [Space]
        [SerializeField] protected GameObject _selectionPointPrefab;
        [SerializeField] protected Transform _selectionPointPrefabContainer;

        [Space]
        [SerializeField] protected GameObject _objectCreationAreaPrefab;
        [SerializeField] protected Transform _objectCreationAreaPrefabContainer;

        protected GameObject _selectionPoint;
        protected GameObject _objectCreationArea;
        protected bool _holdingLMB;

        protected GameObject _firstCreationSelectionPoint;

        private GameObject CreateSelectionPoint()
        {
            GameObject __selectionPoint = Instantiate(_selectionPointPrefab, _selectionPointPrefabContainer);
            __selectionPoint.name = _selectionPointPrefab.transform.name;

            return __selectionPoint;
        }

        private GameObject CreateObjectCreationArea()
        {
            GameObject __objectCreationArea = Instantiate(_objectCreationAreaPrefab, _objectCreationAreaPrefabContainer);
            __objectCreationArea.name = _objectCreationAreaPrefab.transform.name;

            return __objectCreationArea;
        }

        private void CreateReferences()
        {
            _selectionPoint = CreateSelectionPoint();
            _objectCreationArea = CreateObjectCreationArea();
        }

        private void MoveSelectionPoint()
        {
            Ray __fromCamera = _userCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit __cameraHit;
            bool __ray = Physics.Raycast(__fromCamera, out __cameraHit);

            if (__ray)
            {
                _selectionPoint.transform.position = __cameraHit.point;
            }
        }

        private void HandleObjectCreation()
        {
            if (Input.GetMouseButtonUp(0))
            {
                if (_firstCreationSelectionPoint != null)
                {
                    Destroy(_firstCreationSelectionPoint);
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                _firstCreationSelectionPoint = CreateSelectionPoint();
                _firstCreationSelectionPoint.transform.position = _selectionPoint.transform.position;
            }

            if (_holdingLMB)
            {
                if (_firstCreationSelectionPoint == null || _objectCreationArea == null)
                {
                    if (_objectCreationArea != null)
                    {
                        _objectCreationArea.gameObject.SetActive(false);
                    }
                    
                    return;
                }

                _objectCreationArea.gameObject.SetActive(true);

                Vector3 __middlePosition = (_firstCreationSelectionPoint.transform.position + _selectionPoint.transform.position) / 2;
                _objectCreationArea.transform.position = __middlePosition;

                float __scaleX = Mathf.Abs(_firstCreationSelectionPoint.transform.position.x - _selectionPoint.transform.position.x);
                float __scaleZ = Mathf.Abs(_firstCreationSelectionPoint.transform.position.z - _selectionPoint.transform.position.z);
                _objectCreationArea.transform.localScale = new Vector3(__scaleX, 0.01f, __scaleZ);
            }
            else
            {
                if (_objectCreationArea == null)
                {
                    return;
                }

                _objectCreationArea.gameObject.SetActive(false);
            }
        }

        private void GetInputs()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _holdingLMB = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                _holdingLMB = false;
            }
        }

        private void Start()
        {
            CreateReferences();
        }

        private void Update()
        {
            GetInputs();

            MoveSelectionPoint();
            HandleObjectCreation();
        }
    }
}