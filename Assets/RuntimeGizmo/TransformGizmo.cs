using System.Collections.Generic;
using System.Collections;
using System;

using UnityEngine;

using CommandUndoRedo;
using VipereSolide.User.Inputs;

namespace RuntimeGizmos
{
    //To be safe, if you are changing any transforms hierarchy, such as parenting an object to something,
    //you should call ClearTargets before doing so just to be sure nothing unexpected happens... as well as call UndoRedoManager.Clear()
    //For example, if you select an object that has _children, move the _children elsewhere, deselect the original object, then try to add those old _children to the selection, I think it wont work.

    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class TransformGizmo : MonoBehaviour
    {
        #region Variables

        #region Inspector

        [Header("Tools State")]
        public TransformSpace space = TransformSpace.Global;
        public TransformType transformType = TransformType.Move;
        public TransformPivot pivot = TransformPivot.Pivot;
        public CenterType centerType = CenterType.All;
        public ScaleType scaleType = ScaleType.FromPoint;

        [Header("Snapping")]
        public float movementSnap = .25f;
        public float rotationSnap = 15f;
        public float scaleSnap = 1f;

        [Header("Multipliers")]
        public float allMoveHandleLengthMultiplier = 1f;
        public float allRotateHandleLengthMultiplier = 1.4f;
        public float allScaleHandleLengthMultiplier = 1.6f;
        public float minSelectedDistanceCheck = .01f;
        public float moveSpeedMultiplier = 1f;
        public float scaleSpeedMultiplier = 1f;
        public float rotateSpeedMultiplier = 1f;
        public float allRotateSpeedMultiplier = 20f;

        [Header("Tool Miscellaneous Settings")]
        public bool useFirstSelectedAsMain = true;

        [Tooltip("If circularRotationMethod is true, when rotating you will need to move your mouse around the object as if turning a wheel. If circularRotationMethod is false, when rotating you can just click and drag in a line to rotate.")]
        public bool circularRotationMethod = false;

        [Tooltip("Mainly for if you want the pivot point to update correctly if selected objects are moving outside the transformgizmo. Might be poor on performance if lots of objects are selected...")]
        public bool forceUpdatePivotPointOnChange = true;
        public bool manuallyHandleLineGizmo = false;
        [SerializeField] private Material _lineMaterial;
        public bool manuallyHandleOutlineGizmo = false;
        [SerializeField] private Material _outlineMaterial;

        [Header("Undo/Redo")]
        public int maxUndoStored = 100;

        [Header("Graphics")]
        public Color xColor = new Color(1, 0, 0, 0.8f);
        public Color yColor = new Color(0, 1, 0, 0.8f);
        public Color zColor = new Color(0, 0, 1, 0.8f);
        public Color allColor = new Color(.7f, .7f, .7f, 0.8f);
        public Color selectedColor = new Color(1, 1, 0, 0.8f);
        public Color hoverColor = new Color(1, .75f, 0, 0.8f);
        public float planesOpacity = .5f;

        [Space]
        public float handleLength = .25f;
        public float handleWidth = .003f;
        public float planeSize = .035f;
        public float triangleSize = .03f;
        public float boxSize = .03f;
        public int circleDetail = 40;

        [Header("Masks")]
        public LayerMask selectionMask = Physics.DefaultRaycastLayers;

        public Action onCheckForSelectedAxis;
        public Action onDrawCustomGizmo;

        #endregion
        #region Getters/Setters

        public Camera mainCamera
        {
            get;
            private set;
        }
        public bool isTransforming
        {
            get;
            private set;
        }
        public float totalScaleAmount
        {
            get;
            private set;
        }
        public Quaternion totalRotationAmount
        {
            get;
            private set;
        }
        public Axis translatingAxis
        {
            get
            {
                return _nearAxis;
            }
        }
        public Axis translatingAxisPlane
        {
            get
            {
                return _planeAxis;
            }
        }
        public bool hasTranslatingAxisPlane
        {
            get
            {
                return translatingAxisPlane != Axis.None && translatingAxisPlane != Axis.Any;
            }
        }
        public TransformType transformingType
        {
            get
            {
                return _translatingType;
            }
        }
        public Vector3 pivotPoint
        {
            get;
            private set;
        }
        public Transform mainTargetRoot
        {
            get
            {
                if (_targetRootsOrdered.Count == 0)
                {
                    return null;
                }

                return (useFirstSelectedAsMain) ? _targetRootsOrdered[0] : _targetRootsOrdered[_targetRootsOrdered.Count - 1];
            }
        }

        #endregion
        #region Private

        private Vector3 _totalCenterPivotPoint;
        private AxisInfo _axisInfo;
        private Axis _nearAxis = Axis.None;
        private Axis _planeAxis = Axis.None;
        private TransformType _translatingType;

        private AxisVectors _handleLines = new AxisVectors();
        private AxisVectors _handlePlanes = new AxisVectors();
        private AxisVectors _handleTriangles = new AxisVectors();
        private AxisVectors _handleSquares = new AxisVectors();
        private AxisVectors _circlesLines = new AxisVectors();

        //We use a HashSet and a List for _targetRoots so that we get fast lookup with the hashset while also keeping track of the order with the list.
        private List<Transform> _targetRootsOrdered = new List<Transform>();
        private Dictionary<Transform, TargetInfo> _targetRoots = new Dictionary<Transform, TargetInfo>();
        private HashSet<Renderer> _highlightedRenderers = new HashSet<Renderer>();
        private HashSet<Transform> _children = new HashSet<Transform>();

        private List<Transform> _childrenBuffer = new List<Transform>();
        private List<Renderer> _renderersBuffer = new List<Renderer>();
        private List<Material> _materialsBuffer = new List<Material>();

        private WaitForEndOfFrame _waitForEndOFFrame = new WaitForEndOfFrame();
        private Coroutine _forceUpdatePivotCoroutine;

        #endregion

        #endregion

        #region Methods

        #region Unity

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
            SetMaterial();
        }
        private void OnEnable()
        {
            _forceUpdatePivotCoroutine = StartCoroutine(ForceUpdatePivotPointAtEndOfFrame());
        }
        private void OnDisable()
        {
            ClearTargets(); //Just so things gets cleaned up, such as removing any materials we placed on objects.

            StopCoroutine(_forceUpdatePivotCoroutine);
        }
        private void OnDestroy()
        {
            ClearAllHighlightedRenderers();
        }
        private void Update()
        {
            HandleUndoRedo();
            SetSpaceAndType();

            if (manuallyHandleLineGizmo || manuallyHandleOutlineGizmo)
            {
                if (onCheckForSelectedAxis != null) onCheckForSelectedAxis();
            }
            else
            {
                SetNearAxis();
            }

            if (VipereSolide.GameManager.IsPointerOverUIElement() == true)
            {
                return;
            }

            GetTarget();

            if (mainTargetRoot == null) return;

            HandleTargetTransformActions();
        }
        private void LateUpdate()
        {
            if (mainTargetRoot == null) return;

            //We run this in lateupdate since coroutines run after update and we want our gizmos to have the updated target transform position after TransformSelected()
            SetAxisInfo();

            if (manuallyHandleLineGizmo || manuallyHandleOutlineGizmo)
            {
                if (onDrawCustomGizmo != null) onDrawCustomGizmo();
            }
            else
            {
                SetLines();
            }
        }
        private void OnPostRender()
        {
            if (mainTargetRoot == null || manuallyHandleLineGizmo || manuallyHandleOutlineGizmo) return;

            _lineMaterial.SetPass(0);

            Color xColor = (_nearAxis == Axis.X) ? (isTransforming) ? selectedColor : hoverColor : this.xColor;
            Color yColor = (_nearAxis == Axis.Y) ? (isTransforming) ? selectedColor : hoverColor : this.yColor;
            Color zColor = (_nearAxis == Axis.Z) ? (isTransforming) ? selectedColor : hoverColor : this.zColor;
            Color allColor = (_nearAxis == Axis.Any) ? (isTransforming) ? selectedColor : hoverColor : this.allColor;

            //Note: The order of drawing the __axis decides what gets drawn over what.

            TransformType moveOrScaleType = (transformType == TransformType.Scale || (isTransforming && _translatingType == TransformType.Scale)) ? TransformType.Scale : TransformType.Move;
            DrawQuads(_handleLines.z, GetColor(moveOrScaleType, this.zColor, zColor, hasTranslatingAxisPlane));
            DrawQuads(_handleLines.x, GetColor(moveOrScaleType, this.xColor, xColor, hasTranslatingAxisPlane));
            DrawQuads(_handleLines.y, GetColor(moveOrScaleType, this.yColor, yColor, hasTranslatingAxisPlane));

            DrawTriangles(_handleTriangles.x, GetColor(TransformType.Move, this.xColor, xColor, hasTranslatingAxisPlane));
            DrawTriangles(_handleTriangles.y, GetColor(TransformType.Move, this.yColor, yColor, hasTranslatingAxisPlane));
            DrawTriangles(_handleTriangles.z, GetColor(TransformType.Move, this.zColor, zColor, hasTranslatingAxisPlane));

            DrawQuads(_handlePlanes.z, GetColor(TransformType.Move, this.zColor, zColor, planesOpacity, !hasTranslatingAxisPlane));
            DrawQuads(_handlePlanes.x, GetColor(TransformType.Move, this.xColor, xColor, planesOpacity, !hasTranslatingAxisPlane));
            DrawQuads(_handlePlanes.y, GetColor(TransformType.Move, this.yColor, yColor, planesOpacity, !hasTranslatingAxisPlane));

            DrawQuads(_handleSquares.x, GetColor(TransformType.Scale, this.xColor, xColor));
            DrawQuads(_handleSquares.y, GetColor(TransformType.Scale, this.yColor, yColor));
            DrawQuads(_handleSquares.z, GetColor(TransformType.Scale, this.zColor, zColor));
            DrawQuads(_handleSquares.all, GetColor(TransformType.Scale, this.allColor, allColor));

            DrawQuads(_circlesLines.all, GetColor(TransformType.Rotate, this.allColor, allColor));
            DrawQuads(_circlesLines.x, GetColor(TransformType.Rotate, this.xColor, xColor));
            DrawQuads(_circlesLines.y, GetColor(TransformType.Rotate, this.yColor, yColor));
            DrawQuads(_circlesLines.z, GetColor(TransformType.Rotate, this.zColor, zColor));
        }

        #endregion
        #region Behaviour

        /// <summary>
        /// Calculates the current state's color for a given tool or transform type, taking in account this tool's color settings.
        /// </summary>
        /// <param name="_type">The tool or transform type you want to get the color of.</param>
        /// <param name="_normalColor">The default, unselected and unhovered color of that tool.</param>
        /// <param name="_nearColor">The hovered color of that tool.</param>
        /// <param name="_forceUseNormal">When passed true, the tool will never take the "_nearColor" (hovered color).</param>
        /// <returns>A UnityEngine.Color struct containing color information for the current state of the given tool.</returns>
        private Color GetColor(TransformType _type, Color _normalColor, Color _nearColor, bool _forceUseNormal = false)
        {
            return GetColor(_type, _normalColor, _nearColor, false, 1, _forceUseNormal);
        }
        /// <summary>
        /// Calculates the current state's color for a given tool or transform type, taking in account this tool's color settings.
        /// </summary>
        /// <param name="_type">The tool or transform type you want to get the color of.</param>
        /// <param name="_normalColor">The default, unselected and unhovered color of that tool.</param>
        /// <param name="_nearColor">The hovered color of that tool.</param>
        /// <param name="_alpha">The alpha of this tool, whatever if it's being hovered, selected or not.</param>
        /// <param name="_forceUseNormal">When passed true, the tool will never take the "_nearColor" (hovered color).</param>
        /// <returns>A UnityEngine.Color struct containing color information for the current state of the given tool.</returns>
		private Color GetColor(TransformType _type, Color _normalColor, Color _nearColor, float _alpha, bool _forceUseNormal = false)
        {
            return GetColor(_type, _normalColor, _nearColor, true, _alpha, _forceUseNormal);
        }
        /// <summary>
        /// Calculates the current state's color for a given tool or transform type, taking in account this tool's color settings.
        /// </summary>
        /// <param name="_type">The tool or transform type you want to get the color of.</param>
        /// <param name="_normalColor">The default, unselected and unhovered color of that tool.</param>
        /// <param name="_nearColor">The hovered color of that tool.</param>
        /// <param name="_setAlpha">Whether you want to override the alpha, or always have it be opaque.</param>
        /// <param name="_alpha">The alpha of this tool, whatever if it's being hovered, selected or not. Only applies in case you have "_setAlpha" enabled.</param>
        /// <param name="_forceUseNormal">When passed true, the tool will never take the "_nearColor" (hovered color).</param>
        /// <returns>A UnityEngine.Color struct containing color information for the current state of the given tool.</returns>
		private Color GetColor(TransformType _type, Color _normalColor, Color _nearColor, bool _setAlpha, float _alpha, bool _forceUseNormal = false)
        {
            Color __color;

            if (!_forceUseNormal && TranslatingTypeContains(_type, false))
            {
                __color = _nearColor;
            }
            else
            {
                __color = _normalColor;
            }

            if (_setAlpha)
            {
                __color.a = _alpha;
            }

            return __color;
        }

        /// <summary>
        /// Listens to undo and redo inputs and calls UndoRedoManager's corresponding methods if necessary.
        /// </summary>
        private void HandleUndoRedo()
        {
            if (maxUndoStored != UndoRedoManager.maxUndoStored)
            {
                UndoRedoManager.maxUndoStored = maxUndoStored;
            }

            if (Input.GetKey(GameInputs.actionKey))
            {
                if (Input.GetKeyDown(GameInputs.undoAction))
                {
                    UndoRedoManager.Undo();
                }
                else if (Input.GetKeyDown(GameInputs.redoAction))
                {
                    UndoRedoManager.Redo();
                }
            }
        }
        /// <summary>
        /// Listens to user's inputs and updates corresponding transform space and type if necessary. Does not run if the action key is held down.
        /// </summary>
		private void SetSpaceAndType()
        {
            if (Input.GetKey(GameInputs.actionKey))
            {
                return;
            }

            GetTransformType();
            GetTransformSpace();
        }
        /// <summary>
        /// Listens to user's inputs and updates corresponding transform type if necessary.
        /// </summary>
        private void GetTransformType()
        {
            if (Input.GetKeyDown(GameInputs.setMoveType))
            {
                transformType = TransformType.Move;
            }
            else if (Input.GetKeyDown(GameInputs.setRotateType))
            {
                transformType = TransformType.Rotate;
            }
            else if (Input.GetKeyDown(GameInputs.setScaleType))
            {
                transformType = TransformType.Scale;
            }
            else if (Input.GetKeyDown(GameInputs.setAllTransformType))
            {
                transformType = TransformType.All;
            }

            if (!isTransforming)
            {
                _translatingType = transformType;
            }
        }
        /// <summary>
        /// Listens to user's inputs and updates corresponding transform space if necessary.
        /// </summary>
		private void GetTransformSpace()
        {
            if (Input.GetKeyDown(GameInputs.setPivotModeToggle))
            {
                if (pivot == TransformPivot.Pivot) pivot = TransformPivot.Center;
                else if (pivot == TransformPivot.Center) pivot = TransformPivot.Pivot;

                SetPivotPoint();
            }

            if (Input.GetKeyDown(GameInputs.setCenterTypeToggle))
            {
                if (centerType == CenterType.All) centerType = CenterType.Solo;
                else if (centerType == CenterType.Solo) centerType = CenterType.All;

                SetPivotPoint();
            }

            if (Input.GetKeyDown(GameInputs.setSpaceToggle))
            {
                if (space == TransformSpace.Global) space = TransformSpace.Local;
                else if (space == TransformSpace.Local) space = TransformSpace.Global;
            }

            if (Input.GetKeyDown(GameInputs.setScaleTypeToggle))
            {
                if (scaleType == ScaleType.FromPoint) scaleType = ScaleType.FromPointOffset;
                else if (scaleType == ScaleType.FromPointOffset) scaleType = ScaleType.FromPoint;
            }

            if (transformType == TransformType.Scale)
            {
                if (pivot == TransformPivot.Pivot)
                {
                    //FromPointOffset can be inaccurate and should only really be used in Center mode if desired.
                    scaleType = ScaleType.FromPoint;
                }
            }
        }
        /// <summary>
        /// Responsible for running or not the transform actions method (such as translating or rotating) on the selected object if needed.
        /// </summary>
        private void HandleTargetTransformActions()
        {
            if (mainTargetRoot != null)
            {
                if (_nearAxis != Axis.None && Input.GetMouseButtonDown(0))
                {
                    StartCoroutine(TransformSelected(_translatingType));
                }
            }
        }
        /// <summary>
        /// Responsible for executing all transform actions on the selected object.
        /// </summary>
        /// <param name="_transformType">The used tool for transformation</param>
        /// <returns></returns>
		private IEnumerator TransformSelected(TransformType _transformType)
        {
            isTransforming = true;
            totalScaleAmount = 0;
            totalRotationAmount = Quaternion.identity;

            Vector3 __originalPivot = pivotPoint;
            Vector3 __otherAxis1;
            Vector3 __otherAxis2;
            Vector3 __axis = GetNearAxisDirection(out __otherAxis1, out __otherAxis2);
            Vector3 __planeNormal = hasTranslatingAxisPlane ? __axis : (transform.position - __originalPivot).normalized;
            Vector3 __projectedAxis = Vector3.ProjectOnPlane(__axis, __planeNormal).normalized;
            Vector3 __previousMousePosition = Vector3.zero;

            Vector3 __currentSnapMovementAmount = Vector3.zero;
            float __currentSnapRotationAmount = 0;
            float __currentSnapScaleAmount = 0;

            List<ICommand> __transformCommands = new List<ICommand>();
            for (int i = 0; i < _targetRootsOrdered.Count; i++)
            {
                ICommand __newCommand = new TransformCommand(this, _targetRootsOrdered[i]);
                __transformCommands.Add(__newCommand);
            }

            while (!Input.GetMouseButtonUp(0))
            {
                Ray __mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
                Vector3 __mousePosition = Geometry.LinePlaneIntersect(__mouseRay.origin, __mouseRay.direction, __originalPivot, __planeNormal);
                bool __isSnapping = Input.GetKey(GameInputs.translationSnapping);

                if (__previousMousePosition != Vector3.zero && __mousePosition != Vector3.zero)
                {
                    switch (_transformType)
                    {
                        case TransformType.Move:
                            TransformSelected_HandleMovement(__mousePosition, __previousMousePosition, __projectedAxis, __axis, __otherAxis1, __otherAxis2, __currentSnapMovementAmount, __isSnapping);
                            break;
                        case TransformType.Scale:
                            TransformSelected_HandleScale(__mousePosition, __previousMousePosition, __projectedAxis, __axis, __currentSnapScaleAmount, __originalPivot, __isSnapping);
                            break;
                        case TransformType.Rotate:
                            TransformSelected_HandleRotation(__mousePosition, __previousMousePosition, __projectedAxis, __axis, __planeNormal, __currentSnapRotationAmount, __originalPivot, __isSnapping);
                            break;
                    }
                }

                __previousMousePosition = __mousePosition;

                yield return null;
            }

            for (int i = 0; i < __transformCommands.Count; i++)
            {
                ((TransformCommand)__transformCommands[i]).StoreNewTransformValues();
            }

            CommandGroup __commandGroup = new CommandGroup();
            __commandGroup.Set(__transformCommands);
            UndoRedoManager.Insert(__commandGroup);

            totalRotationAmount = Quaternion.identity;
            totalScaleAmount = 0;
            isTransforming = false;

            SetTranslatingAxis(transformType, Axis.None);
            SetPivotPoint();
        }
        protected void TransformSelected_HandleMovement(Vector3 _mousePosition, Vector3 _previousMousePosition, Vector3 _projectedAxis, Vector3 _axis, Vector3 _otherAxis1, Vector3 _otherAxis2, Vector3 _currentSnapMovementAmount, bool _isSnapping)
        {
            Vector3 __movement = Vector3.zero;

            if (hasTranslatingAxisPlane)
            {
                __movement = _mousePosition - _previousMousePosition;
            }
            else
            {
                float __moveAmount = ExtVector3.MagnitudeInDirection(_mousePosition - _previousMousePosition, _projectedAxis) * moveSpeedMultiplier;
                __movement = _axis * __moveAmount;
            }

            if (_isSnapping && movementSnap > 0)
            {
                _currentSnapMovementAmount += __movement;
                __movement = Vector3.zero;

                if (hasTranslatingAxisPlane)
                {
                    float __amountInAxis1 = ExtVector3.MagnitudeInDirection(_currentSnapMovementAmount, _otherAxis1);
                    float __amountInAxis2 = ExtVector3.MagnitudeInDirection(_currentSnapMovementAmount, _otherAxis2);

                    float __remainder1;
                    float __snapAmount1 = CalculateSnapAmount(movementSnap, __amountInAxis1, out __remainder1);
                    float __remainder2;
                    float __snapAmount2 = CalculateSnapAmount(movementSnap, __amountInAxis2, out __remainder2);

                    if (__snapAmount1 != 0)
                    {
                        Vector3 __snapMove = (_otherAxis1 * __snapAmount1);
                        __movement += __snapMove;
                        _currentSnapMovementAmount -= __snapMove;
                    }

                    if (__snapAmount2 != 0)
                    {
                        Vector3 __snapMove = (_otherAxis2 * __snapAmount2);
                        __movement += __snapMove;
                        _currentSnapMovementAmount -= __snapMove;
                    }
                }
                else
                {
                    float __remainder;
                    float __snapAmount = CalculateSnapAmount(movementSnap, _currentSnapMovementAmount.magnitude, out __remainder);

                    if (__snapAmount != 0)
                    {
                        __movement = _currentSnapMovementAmount.normalized * __snapAmount;
                        _currentSnapMovementAmount = _currentSnapMovementAmount.normalized * __remainder;
                    }
                }
            }

            for (int i = 0; i < _targetRootsOrdered.Count; i++)
            {
                Transform target = _targetRootsOrdered[i];

                target.Translate(__movement, Space.World);
            }

            SetPivotPointOffset(__movement);
        }
        protected void TransformSelected_HandleScale(Vector3 _mousePosition, Vector3 _previousMousePosition, Vector3 _projectedAxis, Vector3 _axis, float _currentSnapScaleAmount, Vector3 _originalPivot, bool _isSnapping)
        {
            Vector3 __projected = (_nearAxis == Axis.Any) ? transform.right : _projectedAxis;
            float __scaleAmount = ExtVector3.MagnitudeInDirection(_mousePosition - _previousMousePosition, __projected) * scaleSpeedMultiplier;

            if (_isSnapping && scaleSnap > 0)
            {
                _currentSnapScaleAmount += __scaleAmount;
                __scaleAmount = 0;

                float remainder;
                float snapAmount = CalculateSnapAmount(scaleSnap, _currentSnapScaleAmount, out remainder);

                if (snapAmount != 0)
                {
                    __scaleAmount = snapAmount;
                    _currentSnapScaleAmount = remainder;
                }
            }

            Vector3 __localAxis = _axis;

            if (GetProperTransformSpace() == TransformSpace.Local && _nearAxis != Axis.Any)
            {
                __localAxis = mainTargetRoot.InverseTransformDirection(_axis);
            }

            Vector3 __targetScaleAmount = __localAxis * __scaleAmount;
            if (_nearAxis == Axis.Any)
            {
                __targetScaleAmount = (ExtVector3.Abs(mainTargetRoot.localScale.normalized) * __scaleAmount);
            }

            for (int i = 0; i < _targetRootsOrdered.Count; i++)
            {
                Transform __target = _targetRootsOrdered[i];
                Vector3 __targetScale = __target.localScale + __targetScaleAmount;

                if (pivot == TransformPivot.Pivot)
                {
                    __target.localScale = __targetScale;
                    continue;
                }

                if (pivot == TransformPivot.Center)
                {
                    if (scaleType == ScaleType.FromPoint)
                    {
                        __target.SetScaleFrom(_originalPivot, __targetScale);
                        continue;
                    }

                    if (scaleType == ScaleType.FromPointOffset)
                    {
                        __target.SetScaleFromOffset(_originalPivot, __targetScale);
                        continue;
                    }
                }
            }

            totalScaleAmount += __scaleAmount;

        }
        protected void TransformSelected_HandleRotation(Vector3 _mousePosition, Vector3 _previousMousePosition, Vector3 _projectedAxis, Vector3 _axis, Vector3 _planeNormal, float _currentSnapRotationAmount, Vector3 _originalPivot, bool _isSnapping)
        {
            float __rotateAmount = 0;
            Vector3 __rotationAxis = _axis;

            if (_nearAxis == Axis.Any)
            {
                Vector3 __rotation = transform.TransformDirection(new Vector3(Input.GetAxis("Mouse Y"), -Input.GetAxis("Mouse X"), 0));
                Quaternion.Euler(__rotation).ToAngleAxis(out __rotateAmount, out __rotationAxis);
                __rotateAmount *= allRotateSpeedMultiplier;
            }
            else
            {
                if (circularRotationMethod)
                {
                    float __angle = Vector3.SignedAngle(_previousMousePosition - _originalPivot, _mousePosition - _originalPivot, _axis);
                    __rotateAmount = __angle * rotateSpeedMultiplier;
                }
                else
                {
                    Vector3 __projected = (_nearAxis == Axis.Any || ExtVector3.IsParallel(_axis, _planeNormal)) ? _planeNormal : Vector3.Cross(_axis, _planeNormal);
                    __rotateAmount = (ExtVector3.MagnitudeInDirection(_mousePosition - _previousMousePosition, __projected) * (rotateSpeedMultiplier * 100f)) / GetDistanceMultiplier();
                }
            }

            if (_isSnapping && rotationSnap > 0)
            {
                _currentSnapRotationAmount += __rotateAmount;
                __rotateAmount = 0;

                float __remainder;
                float __snapAmount = CalculateSnapAmount(rotationSnap, _currentSnapRotationAmount, out __remainder);

                if (__snapAmount != 0)
                {
                    __rotateAmount = __snapAmount;
                    _currentSnapRotationAmount = __remainder;
                }
            }

            for (int i = 0; i < _targetRootsOrdered.Count; i++)
            {
                Transform __target = _targetRootsOrdered[i];

                if (pivot == TransformPivot.Pivot)
                {
                    __target.Rotate(__rotationAxis, __rotateAmount, Space.World);
                    continue;
                }

                if (pivot == TransformPivot.Center)
                {
                    __target.RotateAround(_originalPivot, __rotationAxis, __rotateAmount);
                    continue;
                }
            }

            totalRotationAmount *= Quaternion.Euler(__rotationAxis * __rotateAmount);
        }
        /// <summary>
        /// Takes an input float value and a snap input, and outputs a value floored to the closest snap value.
        /// </summary>
        /// <param name="_snapValue">The size of each "cells" on the snap "grid".</param>
        /// <param name="_currentAmount">The floating number you want to round.</param>
        /// <param name="_remainder">The output rounded value.</param>
        /// <returns>A float representing the given value, snapped to the closest "point" on a snap "grid".</returns>
		private float CalculateSnapAmount(float _snapValue, float _currentAmount, out float _remainder)
        {
            _remainder = 0;

            if (_snapValue <= 0)
            {
                return _currentAmount;
            }

            float __currentAmountAbs = Mathf.Abs(_currentAmount);
            if (__currentAmountAbs > _snapValue)
            {
                _remainder = __currentAmountAbs % _snapValue;
                return _snapValue * (Mathf.Sign(_currentAmount) * Mathf.Floor(__currentAmountAbs / _snapValue));
            }

            return 0;
        }

        /// <summary>
        /// Calculates the direction of the near axis, resulting in a vector3 and the two other directions.
        /// </summary>
        /// <param name="_otherAxis1">First other direction of the vector when calculating the near axis' direction.</param>
        /// <param name="_otherAxis2">Second other direction of the vector when calculating the near axis' direction.</param>
        /// <returns>A UnityEngine.Vector3 containing the direction of the near axis.</returns>
        private Vector3 GetNearAxisDirection(out Vector3 _otherAxis1, out Vector3 _otherAxis2)
        {
            _otherAxis2 = Vector3.zero;
            _otherAxis1 = Vector3.zero;

            if (_nearAxis != Axis.None)
            {
                switch (_nearAxis)
                {
                    case Axis.X:
                        _otherAxis1 = _axisInfo.yDirection;
                        _otherAxis2 = _axisInfo.zDirection;
                        return _axisInfo.xDirection;

                    case Axis.Y:
                        _otherAxis1 = _axisInfo.xDirection;
                        _otherAxis2 = _axisInfo.zDirection;
                        return _axisInfo.yDirection;

                    case Axis.Z:
                        _otherAxis1 = _axisInfo.xDirection;
                        _otherAxis2 = _axisInfo.yDirection;
                        return _axisInfo.zDirection;

                    case Axis.Any:
                        return Vector3.one;
                }
            }

            return Vector3.zero;
        }
        private void GetTarget()
        {
            if (_nearAxis == Axis.None && Input.GetMouseButtonDown(0))
            {
                bool isAdding = Input.GetKey(GameInputs.addSelection);
                bool isRemoving = Input.GetKey(GameInputs.removeSelection);

                RaycastHit hitInfo;
                if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, selectionMask))
                {
                    Transform target = hitInfo.transform;

                    if (isAdding)
                    {
                        AddTarget(target);
                    }
                    else if (isRemoving)
                    {
                        RemoveTarget(target);
                    }
                    else if (!isAdding && !isRemoving)
                    {
                        ClearAndAddTarget(target);
                    }
                }
                else
                {
                    if (!isAdding && !isRemoving)
                    {
                        ClearTargets();
                    }
                }
            }
        }
        private void ClearAndAddTarget(Transform target)
        {
            UndoRedoManager.Insert(new ClearAndAddTargetCommand(this, target, _targetRootsOrdered));

            ClearTargets(false);
            AddTarget(target, false);
        }
        private void AddTargetHighlightedRenderers(Transform target)
        {
            if (target != null)
            {
                GetTargetRenderers(target, _renderersBuffer);

                for (int i = 0; i < _renderersBuffer.Count; i++)
                {
                    Renderer render = _renderersBuffer[i];

                    if (!_highlightedRenderers.Contains(render))
                    {
                        _materialsBuffer.Clear();
                        _materialsBuffer.AddRange(render.sharedMaterials);

                        if (!_materialsBuffer.Contains(_outlineMaterial))
                        {
                            _materialsBuffer.Add(_outlineMaterial);
                            render.materials = _materialsBuffer.ToArray();
                        }

                        _highlightedRenderers.Add(render);
                    }
                }

                _materialsBuffer.Clear();
            }
        }
        private void GetTargetRenderers(Transform target, List<Renderer> renderers)
        {
            renderers.Clear();
            if (target != null)
            {
                target.GetComponentsInChildren<Renderer>(true, renderers);
            }
        }
        private void ClearAllHighlightedRenderers()
        {
            foreach (var target in _targetRoots)
            {
                RemoveTargetHighlightedRenderers(target.Key);
            }

            //In case any are still left, such as if they changed parents or what not when they were highlighted.
            _renderersBuffer.Clear();
            _renderersBuffer.AddRange(_highlightedRenderers);
            RemoveHighlightedRenderers(_renderersBuffer);
        }
        private void RemoveTargetHighlightedRenderers(Transform target)
        {
            GetTargetRenderers(target, _renderersBuffer);

            RemoveHighlightedRenderers(_renderersBuffer);
        }
        private void RemoveHighlightedRenderers(List<Renderer> renderers)
        {
            for (int i = 0; i < _renderersBuffer.Count; i++)
            {
                Renderer render = _renderersBuffer[i];
                if (render != null)
                {
                    _materialsBuffer.Clear();
                    _materialsBuffer.AddRange(render.sharedMaterials);

                    if (_materialsBuffer.Contains(_outlineMaterial))
                    {
                        _materialsBuffer.Remove(_outlineMaterial);
                        render.materials = _materialsBuffer.ToArray();
                    }
                }

                _highlightedRenderers.Remove(render);
            }

            _renderersBuffer.Clear();
        }
        private void AddTargetRoot(Transform targetRoot)
        {
            _targetRoots.Add(targetRoot, new TargetInfo());
            _targetRootsOrdered.Add(targetRoot);

            AddAllChildren(targetRoot);
        }
        private void RemoveTargetRoot(Transform targetRoot)
        {
            if (_targetRoots.Remove(targetRoot))
            {
                _targetRootsOrdered.Remove(targetRoot);

                RemoveAllChildren(targetRoot);
            }
        }
        private void AddAllChildren(Transform target)
        {
            _childrenBuffer.Clear();
            target.GetComponentsInChildren<Transform>(true, _childrenBuffer);
            _childrenBuffer.Remove(target);

            for (int i = 0; i < _childrenBuffer.Count; i++)
            {
                Transform child = _childrenBuffer[i];
                _children.Add(child);
                RemoveTargetRoot(child); //We do this in case we selected child first and then the parent.
            }

            _childrenBuffer.Clear();
        }
        private void RemoveAllChildren(Transform target)
        {
            _childrenBuffer.Clear();
            target.GetComponentsInChildren<Transform>(true, _childrenBuffer);
            _childrenBuffer.Remove(target);

            for (int i = 0; i < _childrenBuffer.Count; i++)
            {
                _children.Remove(_childrenBuffer[i]);
            }

            _childrenBuffer.Clear();
        }
        private void SetPivotPointOffset(Vector3 offset)
        {
            pivotPoint += offset;
            _totalCenterPivotPoint += offset;
        }
        private IEnumerator ForceUpdatePivotPointAtEndOfFrame()
        {
            while (this.enabled)
            {
                ForceUpdatePivotPointOnChange();
                yield return _waitForEndOFFrame;
            }
        }
        private void ForceUpdatePivotPointOnChange()
        {
            if (forceUpdatePivotPointOnChange)
            {
                if (mainTargetRoot != null && !isTransforming)
                {
                    bool hasSet = false;
                    Dictionary<Transform, TargetInfo>.Enumerator targets = _targetRoots.GetEnumerator();
                    while (targets.MoveNext())
                    {
                        if (!hasSet)
                        {
                            if (targets.Current.Value.previousPosition != Vector3.zero && targets.Current.Key.position != targets.Current.Value.previousPosition)
                            {
                                SetPivotPoint();
                                hasSet = true;
                            }
                        }

                        targets.Current.Value.previousPosition = targets.Current.Key.position;
                    }
                }
            }
        }
        private void SetNearAxis()
        {
            if (isTransforming) return;

            SetTranslatingAxis(transformType, Axis.None);

            if (mainTargetRoot == null) return;

            float distanceMultiplier = GetDistanceMultiplier();
            float handleMinSelectedDistanceCheck = (this.minSelectedDistanceCheck + handleWidth) * distanceMultiplier;

            if (_nearAxis == Axis.None && (TransformTypeContains(TransformType.Move) || TransformTypeContains(TransformType.Scale)))
            {
                //Important to check scale lines before move lines since in TransformType.All the move planes would block the scales center scale all gizmo.
                if (_nearAxis == Axis.None && TransformTypeContains(TransformType.Scale))
                {
                    float tipMinSelectedDistanceCheck = (this.minSelectedDistanceCheck + boxSize) * distanceMultiplier;
                    HandleNearestPlanes(TransformType.Scale, _handleSquares, tipMinSelectedDistanceCheck);
                }

                if (_nearAxis == Axis.None && TransformTypeContains(TransformType.Move))
                {
                    //Important to check the planes first before the handle tip since it makes selecting the planes easier.
                    float planeMinSelectedDistanceCheck = (this.minSelectedDistanceCheck + planeSize) * distanceMultiplier;
                    HandleNearestPlanes(TransformType.Move, _handlePlanes, planeMinSelectedDistanceCheck);

                    if (_nearAxis != Axis.None)
                    {
                        _planeAxis = _nearAxis;
                    }
                    else
                    {
                        float tipMinSelectedDistanceCheck = (this.minSelectedDistanceCheck + triangleSize) * distanceMultiplier;
                        HandleNearestLines(TransformType.Move, _handleTriangles, tipMinSelectedDistanceCheck);
                    }
                }

                if (_nearAxis == Axis.None)
                {
                    //Since Move and Scale share the same handle line, we give Move the priority.
                    TransformType _transformType = transformType == TransformType.All ? TransformType.Move : transformType;
                    HandleNearestLines(_transformType, _handleLines, handleMinSelectedDistanceCheck);
                }
            }

            if (_nearAxis == Axis.None && TransformTypeContains(TransformType.Rotate))
            {
                HandleNearestLines(TransformType.Rotate, _circlesLines, handleMinSelectedDistanceCheck);
            }
        }
        private void HandleNearestLines(TransformType type, AxisVectors axisVectors, float minSelectedDistanceCheck)
        {
            float xClosestDistance = ClosestDistanceFromMouseToLines(axisVectors.x);
            float yClosestDistance = ClosestDistanceFromMouseToLines(axisVectors.y);
            float zClosestDistance = ClosestDistanceFromMouseToLines(axisVectors.z);
            float allClosestDistance = ClosestDistanceFromMouseToLines(axisVectors.all);

            HandleNearest(type, xClosestDistance, yClosestDistance, zClosestDistance, allClosestDistance, minSelectedDistanceCheck);
        }
        private void HandleNearestPlanes(TransformType type, AxisVectors axisVectors, float minSelectedDistanceCheck)
        {
            float xClosestDistance = ClosestDistanceFromMouseToPlanes(axisVectors.x);
            float yClosestDistance = ClosestDistanceFromMouseToPlanes(axisVectors.y);
            float zClosestDistance = ClosestDistanceFromMouseToPlanes(axisVectors.z);
            float allClosestDistance = ClosestDistanceFromMouseToPlanes(axisVectors.all);

            HandleNearest(type, xClosestDistance, yClosestDistance, zClosestDistance, allClosestDistance, minSelectedDistanceCheck);
        }
        private void HandleNearest(TransformType type, float xClosestDistance, float yClosestDistance, float zClosestDistance, float allClosestDistance, float minSelectedDistanceCheck)
        {
            if (type == TransformType.Scale && allClosestDistance <= minSelectedDistanceCheck) SetTranslatingAxis(type, Axis.Any);
            else if (xClosestDistance <= minSelectedDistanceCheck && xClosestDistance <= yClosestDistance && xClosestDistance <= zClosestDistance) SetTranslatingAxis(type, Axis.X);
            else if (yClosestDistance <= minSelectedDistanceCheck && yClosestDistance <= xClosestDistance && yClosestDistance <= zClosestDistance) SetTranslatingAxis(type, Axis.Y);
            else if (zClosestDistance <= minSelectedDistanceCheck && zClosestDistance <= xClosestDistance && zClosestDistance <= yClosestDistance) SetTranslatingAxis(type, Axis.Z);
            else if (type == TransformType.Rotate && mainTargetRoot != null)
            {
                Ray __mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
                Vector3 mousePlaneHit = Geometry.LinePlaneIntersect(__mouseRay.origin, __mouseRay.direction, pivotPoint, (transform.position - pivotPoint).normalized);
                if ((pivotPoint - mousePlaneHit).sqrMagnitude <= (GetHandleLength(TransformType.Rotate)).Squared()) SetTranslatingAxis(type, Axis.Any);
            }
        }
        private float ClosestDistanceFromMouseToLines(List<Vector3> lines)
        {
            Ray __mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

            float closestDistance = float.MaxValue;
            for (int i = 0; i + 1 < lines.Count; i++)
            {
                IntersectPoints points = Geometry.ClosestPointsOnSegmentToLine(lines[i], lines[i + 1], __mouseRay.origin, __mouseRay.direction);
                float distance = Vector3.Distance(points.first, points.second);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }
            return closestDistance;
        }
        private float ClosestDistanceFromMouseToPlanes(List<Vector3> planePoints)
        {
            float closestDistance = float.MaxValue;

            if (planePoints.Count >= 4)
            {
                Ray __mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

                for (int i = 0; i < planePoints.Count; i += 4)
                {
                    Plane plane = new Plane(planePoints[i], planePoints[i + 1], planePoints[i + 2]);

                    float distanceToPlane;
                    if (plane.Raycast(__mouseRay, out distanceToPlane))
                    {
                        Vector3 pointOnPlane = __mouseRay.origin + (__mouseRay.direction * distanceToPlane);
                        Vector3 planeCenter = (planePoints[0] + planePoints[1] + planePoints[2] + planePoints[3]) / 4f;

                        float distance = Vector3.Distance(planeCenter, pointOnPlane);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                        }
                    }
                }
            }

            return closestDistance;
        }
        //float DistanceFromMouseToPlane(List<Vector3> planeLines)
        //{
        //	if(planeLines.Count >= 4)
        //	{
        //		Ray __mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        //		Plane plane = new Plane(planeLines[0], planeLines[1], planeLines[2]);

        //		float distanceToPlane;
        //		if(plane.Raycast(__mouseRay, out distanceToPlane))
        //		{
        //			Vector3 pointOnPlane = __mouseRay.origin + (__mouseRay.direction * distanceToPlane);
        //			Vector3 planeCenter = (planeLines[0] + planeLines[1] + planeLines[2] + planeLines[3]) / 4f;

        //			return Vector3.Distance(planeCenter, pointOnPlane);
        //		}
        //	}

        //	return float.MaxValue;
        //}
        private void SetAxisInfo()
        {
            if (mainTargetRoot != null)
            {
                _axisInfo.Set(mainTargetRoot, pivotPoint, GetProperTransformSpace());
            }
        }
        private void SetLines()
        {
            SetHandleLines();
            SetHandlePlanes();
            SetHandleTriangles();
            SetHandleSquares();
            SetCircles(GetAxisInfo(), _circlesLines);
        }
        private void SetHandleLines()
        {
            _handleLines.Clear();

            if (TranslatingTypeContains(TransformType.Move) || TranslatingTypeContains(TransformType.Scale))
            {
                float lineWidth = handleWidth * GetDistanceMultiplier();

                float xLineLength = 0;
                float yLineLength = 0;
                float zLineLength = 0;
                if (TranslatingTypeContains(TransformType.Move))
                {
                    xLineLength = yLineLength = zLineLength = GetHandleLength(TransformType.Move);
                }
                else if (TranslatingTypeContains(TransformType.Scale))
                {
                    xLineLength = GetHandleLength(TransformType.Scale, Axis.X);
                    yLineLength = GetHandleLength(TransformType.Scale, Axis.Y);
                    zLineLength = GetHandleLength(TransformType.Scale, Axis.Z);
                }

                AddQuads(pivotPoint, _axisInfo.xDirection, _axisInfo.yDirection, _axisInfo.zDirection, xLineLength, lineWidth, _handleLines.x);
                AddQuads(pivotPoint, _axisInfo.yDirection, _axisInfo.xDirection, _axisInfo.zDirection, yLineLength, lineWidth, _handleLines.y);
                AddQuads(pivotPoint, _axisInfo.zDirection, _axisInfo.xDirection, _axisInfo.yDirection, zLineLength, lineWidth, _handleLines.z);
            }
        }
        private int AxisDirectionMultiplier(Vector3 direction, Vector3 otherDirection)
        {
            return ExtVector3.IsInDirection(direction, otherDirection) ? 1 : -1;
        }
        private void SetHandlePlanes()
        {
            _handlePlanes.Clear();

            if (TranslatingTypeContains(TransformType.Move))
            {
                Vector3 pivotToCamera = mainCamera.transform.position - pivotPoint;
                float cameraXSign = Mathf.Sign(Vector3.Dot(_axisInfo.xDirection, pivotToCamera));
                float cameraYSign = Mathf.Sign(Vector3.Dot(_axisInfo.yDirection, pivotToCamera));
                float cameraZSign = Mathf.Sign(Vector3.Dot(_axisInfo.zDirection, pivotToCamera));

                float planeSize = this.planeSize;
                if (transformType == TransformType.All) { planeSize *= allMoveHandleLengthMultiplier; }
                planeSize *= GetDistanceMultiplier();

                Vector3 xDirection = (_axisInfo.xDirection * planeSize) * cameraXSign;
                Vector3 yDirection = (_axisInfo.yDirection * planeSize) * cameraYSign;
                Vector3 zDirection = (_axisInfo.zDirection * planeSize) * cameraZSign;

                Vector3 xPlaneCenter = pivotPoint + (yDirection + zDirection);
                Vector3 yPlaneCenter = pivotPoint + (xDirection + zDirection);
                Vector3 zPlaneCenter = pivotPoint + (xDirection + yDirection);

                AddQuad(xPlaneCenter, _axisInfo.yDirection, _axisInfo.zDirection, planeSize, _handlePlanes.x);
                AddQuad(yPlaneCenter, _axisInfo.xDirection, _axisInfo.zDirection, planeSize, _handlePlanes.y);
                AddQuad(zPlaneCenter, _axisInfo.xDirection, _axisInfo.yDirection, planeSize, _handlePlanes.z);
            }
        }
        private void SetHandleTriangles()
        {
            _handleTriangles.Clear();

            if (TranslatingTypeContains(TransformType.Move))
            {
                float triangleLength = triangleSize * GetDistanceMultiplier();
                AddTriangles(_axisInfo.GetXAxisEnd(GetHandleLength(TransformType.Move)), _axisInfo.xDirection, _axisInfo.yDirection, _axisInfo.zDirection, triangleLength, _handleTriangles.x);
                AddTriangles(_axisInfo.GetYAxisEnd(GetHandleLength(TransformType.Move)), _axisInfo.yDirection, _axisInfo.xDirection, _axisInfo.zDirection, triangleLength, _handleTriangles.y);
                AddTriangles(_axisInfo.GetZAxisEnd(GetHandleLength(TransformType.Move)), _axisInfo.zDirection, _axisInfo.yDirection, _axisInfo.xDirection, triangleLength, _handleTriangles.z);
            }
        }
        private void AddTriangles(Vector3 axisEnd, Vector3 axisDirection, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float size, List<Vector3> resultsBuffer)
        {
            Vector3 endPoint = axisEnd + (axisDirection * (size * 2f));
            Square baseSquare = GetBaseSquare(axisEnd, axisOtherDirection1, axisOtherDirection2, size / 2f);

            resultsBuffer.Add(baseSquare.bottomLeft);
            resultsBuffer.Add(baseSquare.topLeft);
            resultsBuffer.Add(baseSquare.topRight);
            resultsBuffer.Add(baseSquare.topLeft);
            resultsBuffer.Add(baseSquare.bottomRight);
            resultsBuffer.Add(baseSquare.topRight);

            for (int i = 0; i < 4; i++)
            {
                resultsBuffer.Add(baseSquare[i]);
                resultsBuffer.Add(baseSquare[i + 1]);
                resultsBuffer.Add(endPoint);
            }
        }
        private void SetHandleSquares()
        {
            _handleSquares.Clear();

            if (TranslatingTypeContains(TransformType.Scale))
            {
                float boxSize = this.boxSize * GetDistanceMultiplier();
                AddSquares(_axisInfo.GetXAxisEnd(GetHandleLength(TransformType.Scale, Axis.X)), _axisInfo.xDirection, _axisInfo.yDirection, _axisInfo.zDirection, boxSize, _handleSquares.x);
                AddSquares(_axisInfo.GetYAxisEnd(GetHandleLength(TransformType.Scale, Axis.Y)), _axisInfo.yDirection, _axisInfo.xDirection, _axisInfo.zDirection, boxSize, _handleSquares.y);
                AddSquares(_axisInfo.GetZAxisEnd(GetHandleLength(TransformType.Scale, Axis.Z)), _axisInfo.zDirection, _axisInfo.xDirection, _axisInfo.yDirection, boxSize, _handleSquares.z);
                AddSquares(pivotPoint - (_axisInfo.xDirection * (boxSize * .5f)), _axisInfo.xDirection, _axisInfo.yDirection, _axisInfo.zDirection, boxSize, _handleSquares.all);
            }
        }
        private void AddSquares(Vector3 axisStart, Vector3 axisDirection, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float size, List<Vector3> resultsBuffer)
        {
            AddQuads(axisStart, axisDirection, axisOtherDirection1, axisOtherDirection2, size, size * .5f, resultsBuffer);
        }
        private void AddQuads(Vector3 axisStart, Vector3 axisDirection, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float length, float width, List<Vector3> resultsBuffer)
        {
            Vector3 axisEnd = axisStart + (axisDirection * length);
            AddQuads(axisStart, axisEnd, axisOtherDirection1, axisOtherDirection2, width, resultsBuffer);
        }
        private void AddQuads(Vector3 axisStart, Vector3 axisEnd, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float width, List<Vector3> resultsBuffer)
        {
            Square baseRectangle = GetBaseSquare(axisStart, axisOtherDirection1, axisOtherDirection2, width);
            Square baseRectangleEnd = GetBaseSquare(axisEnd, axisOtherDirection1, axisOtherDirection2, width);

            resultsBuffer.Add(baseRectangle.bottomLeft);
            resultsBuffer.Add(baseRectangle.topLeft);
            resultsBuffer.Add(baseRectangle.topRight);
            resultsBuffer.Add(baseRectangle.bottomRight);

            resultsBuffer.Add(baseRectangleEnd.bottomLeft);
            resultsBuffer.Add(baseRectangleEnd.topLeft);
            resultsBuffer.Add(baseRectangleEnd.topRight);
            resultsBuffer.Add(baseRectangleEnd.bottomRight);

            for (int i = 0; i < 4; i++)
            {
                resultsBuffer.Add(baseRectangle[i]);
                resultsBuffer.Add(baseRectangleEnd[i]);
                resultsBuffer.Add(baseRectangleEnd[i + 1]);
                resultsBuffer.Add(baseRectangle[i + 1]);
            }
        }
        private void AddQuad(Vector3 axisStart, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float width, List<Vector3> resultsBuffer)
        {
            Square baseRectangle = GetBaseSquare(axisStart, axisOtherDirection1, axisOtherDirection2, width);

            resultsBuffer.Add(baseRectangle.bottomLeft);
            resultsBuffer.Add(baseRectangle.topLeft);
            resultsBuffer.Add(baseRectangle.topRight);
            resultsBuffer.Add(baseRectangle.bottomRight);
        }
        private Square GetBaseSquare(Vector3 axisEnd, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float size)
        {
            Square square;
            Vector3 offsetUp = ((axisOtherDirection1 * size) + (axisOtherDirection2 * size));
            Vector3 offsetDown = ((axisOtherDirection1 * size) - (axisOtherDirection2 * size));
            //These might not really be the proper directions, as in the bottomLeft might not really be at the bottom left...
            square.bottomLeft = axisEnd + offsetDown;
            square.topLeft = axisEnd + offsetUp;
            square.bottomRight = axisEnd - offsetUp;
            square.topRight = axisEnd - offsetDown;
            return square;
        }
        private void SetCircles(AxisInfo _axisInfo, AxisVectors axisVectors)
        {
            axisVectors.Clear();

            if (TranslatingTypeContains(TransformType.Rotate))
            {
                float circleLength = GetHandleLength(TransformType.Rotate);
                AddCircle(pivotPoint, _axisInfo.xDirection, circleLength, axisVectors.x);
                AddCircle(pivotPoint, _axisInfo.yDirection, circleLength, axisVectors.y);
                AddCircle(pivotPoint, _axisInfo.zDirection, circleLength, axisVectors.z);
                AddCircle(pivotPoint, (pivotPoint - transform.position).normalized, circleLength, axisVectors.all, false);
            }
        }
        private void AddCircle(Vector3 origin, Vector3 axisDirection, float size, List<Vector3> resultsBuffer, bool depthTest = true)
        {
            Vector3 up = axisDirection.normalized * size;
            Vector3 forward = Vector3.Slerp(up, -up, .5f);
            Vector3 right = Vector3.Cross(up, forward).normalized * size;

            Matrix4x4 matrix = new Matrix4x4();

            matrix[0] = right.x;
            matrix[1] = right.y;
            matrix[2] = right.z;

            matrix[4] = up.x;
            matrix[5] = up.y;
            matrix[6] = up.z;

            matrix[8] = forward.x;
            matrix[9] = forward.y;
            matrix[10] = forward.z;

            Vector3 lastPoint = origin + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
            Vector3 nextPoint = Vector3.zero;
            float multiplier = 360f / circleDetail;

            Plane plane = new Plane((transform.position - pivotPoint).normalized, pivotPoint);

            float circleHandleWidth = handleWidth * GetDistanceMultiplier();

            for (int i = 0; i < circleDetail + 1; i++)
            {
                nextPoint.x = Mathf.Cos((i * multiplier) * Mathf.Deg2Rad);
                nextPoint.z = Mathf.Sin((i * multiplier) * Mathf.Deg2Rad);
                nextPoint.y = 0;

                nextPoint = origin + matrix.MultiplyPoint3x4(nextPoint);

                if (!depthTest || plane.GetSide(lastPoint))
                {
                    Vector3 centerPoint = (lastPoint + nextPoint) * .5f;
                    Vector3 upDirection = (centerPoint - origin).normalized;
                    AddQuads(lastPoint, nextPoint, upDirection, axisDirection, circleHandleWidth, resultsBuffer);
                }

                lastPoint = nextPoint;
            }
        }
        private void DrawLines(List<Vector3> lines, Color color)
        {
            if (lines.Count == 0) return;

            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int i = 0; i < lines.Count; i += 2)
            {
                GL.Vertex(lines[i]);
                GL.Vertex(lines[i + 1]);
            }

            GL.End();
        }
        private void DrawTriangles(List<Vector3> lines, Color color)
        {
            if (lines.Count == 0) return;

            GL.Begin(GL.TRIANGLES);
            GL.Color(color);

            for (int i = 0; i < lines.Count; i += 3)
            {
                GL.Vertex(lines[i]);
                GL.Vertex(lines[i + 1]);
                GL.Vertex(lines[i + 2]);
            }

            GL.End();
        }
        private void DrawQuads(List<Vector3> lines, Color color)
        {
            if (lines.Count == 0) return;

            GL.Begin(GL.QUADS);
            GL.Color(color);

            for (int i = 0; i < lines.Count; i += 4)
            {
                GL.Vertex(lines[i]);
                GL.Vertex(lines[i + 1]);
                GL.Vertex(lines[i + 2]);
                GL.Vertex(lines[i + 3]);
            }

            GL.End();
        }
        private void DrawFilledCircle(List<Vector3> lines, Color color)
        {
            if (lines.Count == 0) return;

            Vector3 center = Vector3.zero;
            for (int i = 0; i < lines.Count; i++)
            {
                center += lines[i];
            }
            center /= lines.Count;

            GL.Begin(GL.TRIANGLES);
            GL.Color(color);

            for (int i = 0; i + 1 < lines.Count; i++)
            {
                GL.Vertex(lines[i]);
                GL.Vertex(lines[i + 1]);
                GL.Vertex(center);
            }

            GL.End();
        }
        /// <summary>
        /// Instantiates new line and outline materials if none are already existing.
        /// </summary>
		private void SetMaterial()
        {
            if (_lineMaterial == null && !manuallyHandleLineGizmo)
            {
                _lineMaterial = new Material(Shader.Find("Custom/Lines"));
            }

            if (_outlineMaterial == null && !manuallyHandleOutlineGizmo)
            {
                _outlineMaterial = new Material(Shader.Find("Custom/Outline"));
            }
        }

        #endregion
        #region Public

        //We only support scaling in local space.
        public TransformSpace GetProperTransformSpace()
        {
            return transformType == TransformType.Scale ? TransformSpace.Local : space;
        }
        public bool TransformTypeContains(TransformType type)
        {
            return TransformTypeContains(transformType, type);
        }
        public bool TranslatingTypeContains(TransformType type, bool checkIsTransforming = true)
        {
            TransformType _transformType = !checkIsTransforming || isTransforming ? _translatingType : transformType;
            return TransformTypeContains(_transformType, type);
        }
        public bool TransformTypeContains(TransformType mainType, TransformType type)
        {
            return ExtTransformType.TransformTypeContains(mainType, type, GetProperTransformSpace());
        }
        public float GetHandleLength(TransformType type, Axis __axis = Axis.None, bool multiplyDistanceMultiplier = true)
        {
            float length = handleLength;
            if (transformType == TransformType.All)
            {
                if (type == TransformType.Move) length *= allMoveHandleLengthMultiplier;
                if (type == TransformType.Rotate) length *= allRotateHandleLengthMultiplier;
                if (type == TransformType.Scale) length *= allScaleHandleLengthMultiplier;
            }

            if (multiplyDistanceMultiplier) length *= GetDistanceMultiplier();

            if (type == TransformType.Scale && isTransforming && (translatingAxis == __axis || translatingAxis == Axis.Any)) length += totalScaleAmount;

            return length;
        }
        /// <summary>
        /// Returns the a normalized size of the gizmos, no matter how far the camera is from the target object(s).
        /// </summary>
        /// <returns>A float representing the new normalized gizmos scale.</returns>
        public float GetDistanceMultiplier()
        {
            if (mainTargetRoot == null)
            {
                return 0f;
            }

            if (mainCamera.orthographic)
            {
                return Mathf.Max(0.01f, mainCamera.orthographicSize * 2f);
            }

            float __magnitudeInDirection = ExtVector3.MagnitudeInDirection(pivotPoint - transform.position, mainCamera.transform.forward);
            return Mathf.Max(0.01f, Mathf.Abs(__magnitudeInDirection));
        }
        public void SetTranslatingAxis(TransformType type, Axis __axis, Axis _planeAxis = Axis.None)
        {
            this._translatingType = type;
            this._nearAxis = __axis;
            this._planeAxis = _planeAxis;
        }
        public AxisInfo GetAxisInfo()
        {
            AxisInfo currentAxisInfo = _axisInfo;

            if (isTransforming && GetProperTransformSpace() == TransformSpace.Global && _translatingType == TransformType.Rotate)
            {
                currentAxisInfo.xDirection = totalRotationAmount * Vector3.right;
                currentAxisInfo.yDirection = totalRotationAmount * Vector3.up;
                currentAxisInfo.zDirection = totalRotationAmount * Vector3.forward;
            }

            return currentAxisInfo;
        }
        public void SetPivotPoint()
        {
            if (mainTargetRoot != null)
            {
                if (pivot == TransformPivot.Pivot)
                {
                    pivotPoint = mainTargetRoot.position;
                }
                else if (pivot == TransformPivot.Center)
                {
                    _totalCenterPivotPoint = Vector3.zero;

                    Dictionary<Transform, TargetInfo>.Enumerator targetsEnumerator = _targetRoots.GetEnumerator(); //We avoid foreach to avoid garbage.
                    while (targetsEnumerator.MoveNext())
                    {
                        Transform target = targetsEnumerator.Current.Key;
                        TargetInfo info = targetsEnumerator.Current.Value;
                        info.centerPivotPoint = target.GetCenter(centerType);

                        _totalCenterPivotPoint += info.centerPivotPoint;
                    }

                    _totalCenterPivotPoint /= _targetRoots.Count;

                    if (centerType == CenterType.Solo)
                    {
                        pivotPoint = _targetRoots[mainTargetRoot].centerPivotPoint;
                    }
                    else if (centerType == CenterType.All)
                    {
                        pivotPoint = _totalCenterPivotPoint;
                    }
                }
            }
        }
        public void AddTarget(Transform target, bool addCommand = true)
        {
            if (target != null)
            {
                if (_targetRoots.ContainsKey(target)) return;
                if (_children.Contains(target)) return;

                if (addCommand) UndoRedoManager.Insert(new AddTargetCommand(this, target, _targetRootsOrdered));

                AddTargetRoot(target);
                AddTargetHighlightedRenderers(target);

                SetPivotPoint();
            }
        }
        public void RemoveTarget(Transform target, bool addCommand = true)
        {
            if (target != null)
            {
                if (!_targetRoots.ContainsKey(target)) return;

                if (addCommand) UndoRedoManager.Insert(new RemoveTargetCommand(this, target));

                RemoveTargetHighlightedRenderers(target);
                RemoveTargetRoot(target);

                SetPivotPoint();
            }
        }
        public void ClearTargets(bool addCommand = true)
        {
            if (addCommand) UndoRedoManager.Insert(new ClearTargetsCommand(this, _targetRootsOrdered));

            ClearAllHighlightedRenderers();
            _targetRoots.Clear();
            _targetRootsOrdered.Clear();
            _children.Clear();
        }

        #endregion

        #endregion
    }
}