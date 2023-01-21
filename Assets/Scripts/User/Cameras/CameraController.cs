using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VipereSolide.User.Cameras
{
    using Inputs;

    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        [Header("Moving")]
        [SerializeField] protected float _movingSpeed;
        [SerializeField] protected float _shiftMovingSpeed;

        [Header("View")]
        [SerializeField] protected float _viewSpeed;

        [Header("References")]
        [SerializeField] protected Transform _cameraViewHandler;

        protected float _currentSpeed;

        private void GetCurrentSpeed()
        {
            _currentSpeed = (Input.GetKey(GameInputs.sprint) ? _movingSpeed * _shiftMovingSpeed : _movingSpeed);
        }

        private void View()
        {
            if (!Input.GetKey(GameInputs.view))
            {
                return;
            }

            float __x = Input.GetAxisRaw("Mouse X");
            float __y = -Input.GetAxisRaw("Mouse Y");
            Vector2 __mouseView = new Vector2(__x, __y) * _viewSpeed;

            transform.localEulerAngles += new Vector3(0, __mouseView.x, 0);
            _cameraViewHandler.localEulerAngles += new Vector3(__mouseView.y, 0, 0);
        }

        private void Move()
        {
            Vector3 __direction = transform.forward * PlayerInputs.instance.move.z + transform.right * PlayerInputs.instance.move.x + Vector3.up * PlayerInputs.instance.move.y;
            __direction *= Time.deltaTime * _currentSpeed;
            transform.position += __direction;
        }

        private void Update()
        {
            GetCurrentSpeed();
            View();
            Move();
        }
    }
}