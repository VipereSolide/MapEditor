using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VipereSolide.User.Inputs
{
    [DisallowMultipleComponent]
    public class PlayerInputs : MonoBehaviour
    {
        public static PlayerInputs instance;

        public Vector3 move
        {
            get { return new Vector3(_inputX, _inputY, _inputZ); }
        }

        protected float _inputX;
        protected float _inputY;
        protected float _inputZ;

        private void GetMoveInputs()
        {
            if (Input.GetKey(GameInputs.forward)) _inputZ = 1;
            if (Input.GetKey(GameInputs.backward)) _inputZ = -1;
            if ((Input.GetKey(GameInputs.backward) && Input.GetKey(GameInputs.forward)) || (!Input.GetKey(GameInputs.backward) && !Input.GetKey(GameInputs.forward))) _inputZ = 0;

            if (Input.GetKey(GameInputs.rightward)) _inputX = 1;
            if (Input.GetKey(GameInputs.leftward)) _inputX = -1;
            if ((Input.GetKey(GameInputs.leftward) && Input.GetKey(GameInputs.rightward)) || (!Input.GetKey(GameInputs.leftward) && !Input.GetKey(GameInputs.rightward))) _inputX = 0;

            if (Input.GetKey(GameInputs.upward)) _inputY = 1;
            if (Input.GetKey(GameInputs.downward)) _inputY = -1;
            if ((Input.GetKey(GameInputs.downward) && Input.GetKey(GameInputs.upward)) || (!Input.GetKey(GameInputs.downward) && !Input.GetKey(GameInputs.upward))) _inputY = 0;        
        }

        private void Awake()
        {
            instance = this;
        }

        private void Update()
        {
            GetMoveInputs();
        }
    }
}