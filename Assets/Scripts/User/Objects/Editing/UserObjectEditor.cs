using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using System;

namespace VipereSolide.User.Objects.Editing
{
    using Extentions;

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
        protected bool _executeObjectCreationAreaActions;
        protected int _objectEditingActionState = 0;

        protected GameObject _firstCreationSelectionPoint;
        protected GameObject _secondCreationSelectionPoint;

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
            if (_objectEditingActionState <= 0)
            {
                if (_firstCreationSelectionPoint != null)
                {
                    Destroy(_firstCreationSelectionPoint);
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                _objectEditingActionState++;

                if (_objectEditingActionState == 1)
                {
                    _firstCreationSelectionPoint = CreateSelectionPoint();
                    _firstCreationSelectionPoint.transform.position = _selectionPoint.transform.position;
                }

                if (_objectEditingActionState == 2)
                {
                    _secondCreationSelectionPoint = CreateSelectionPoint();
                    _secondCreationSelectionPoint.transform.position = _selectionPoint.transform.position;
                }
            }

            if (_objectEditingActionState > 0)
            {
                if (_firstCreationSelectionPoint == null || _objectCreationArea == null)
                {
                    _executeObjectCreationAreaActions = false;

                    if (_objectCreationArea != null)
                    {
                        _objectCreationArea.gameObject.SetActive(false);
                    }

                    return;
                }

                _objectCreationArea.gameObject.SetActive(true);
                _executeObjectCreationAreaActions = true;
            }
            else
            {
                _executeObjectCreationAreaActions = false;

                if (_objectCreationArea == null)
                {
                    return;
                }

                _objectCreationArea.gameObject.SetActive(false);
            }
        }

        private void ExecuteObjectCreationAreaActions()
        {
            if (_executeObjectCreationAreaActions == false)
            {
                return;
            }

            Vector3 __middlePosition = (_firstCreationSelectionPoint.transform.position + _selectionPoint.transform.position) / 2;
            __middlePosition.y = _firstCreationSelectionPoint.transform.position.y;

            float __scaleX = Mathf.Abs(_firstCreationSelectionPoint.transform.position.x - _selectionPoint.transform.position.x);
            float __scaleZ = Mathf.Abs(_firstCreationSelectionPoint.transform.position.z - _selectionPoint.transform.position.z);
            float __scaleY = 0.01f;

            // https://docs.unity3d.com/ScriptReference/Plane.Raycast.html
            if (_objectEditingActionState == 2)
            {
                Vector3 __directionFromCamera = (_userCamera.transform.position - _secondCreationSelectionPoint.transform.position).normalized;
                Vector3 __directionEulerAngles = Quaternion.LookRotation(__directionFromCamera).eulerAngles.SetX(90);
                GameObject __plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                __plane.transform.position = _secondCreationSelectionPoint.transform.position;
                __plane.transform.eulerAngles = __directionEulerAngles;
                __plane.transform.localScale = Vector3.one * 100f;

                __plane.transform.name = "colliding bitch";

                RaycastHit __intersectEnter;
                Ray __cameraRay = _userCamera.ScreenPointToRay(Input.mousePosition);
                bool __intersect = Physics.Raycast(__cameraRay, out __intersectEnter);

                if (__intersect)
                {
                    Debug.Log(__intersectEnter.point + " : " + __intersectEnter.transform.name);
                    _selectionPoint.transform.position = _secondCreationSelectionPoint.transform.position;
                    _selectionPoint.transform.position.SetY(__intersectEnter.point.y);
                }

                Destroy(__plane);

                __scaleX = Mathf.Abs(_firstCreationSelectionPoint.transform.position.x - _secondCreationSelectionPoint.transform.position.x);
                __scaleZ = Mathf.Abs(_firstCreationSelectionPoint.transform.position.z - _secondCreationSelectionPoint.transform.position.z);
                __scaleY = Mathf.Abs(_firstCreationSelectionPoint.transform.position.y - _selectionPoint.transform.position.y);

                __middlePosition = (_firstCreationSelectionPoint.transform.position + _secondCreationSelectionPoint.transform.position) / 2;
                __middlePosition.y = _firstCreationSelectionPoint.transform.position.y;
            }

            _objectCreationArea.transform.position = __middlePosition;
            _objectCreationArea.transform.localScale = new Vector3(__scaleX, 0.01f, __scaleZ);
        }

        private void Start()
        {
            CreateReferences();
        }

        private void Update()
        {
            MoveSelectionPoint();
            HandleObjectCreation();
        }

        private void LateUpdate()
        {
            ExecuteObjectCreationAreaActions();
        }
    }
}