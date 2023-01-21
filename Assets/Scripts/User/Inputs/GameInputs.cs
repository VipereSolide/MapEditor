using UnityEngine;

namespace VipereSolide.User.Inputs
{
    public static class GameInputs
    {
        // View
        public static KeyCode view = KeyCode.Tab;

        // Movement
        public static KeyCode sprint = KeyCode.LeftShift;
        public static KeyCode forward = KeyCode.Z;
        public static KeyCode leftward = KeyCode.Q;
        public static KeyCode backward = KeyCode.S;
        public static KeyCode rightward = KeyCode.D;
        public static KeyCode upward = KeyCode.Space;
        public static KeyCode downward = KeyCode.X;

        // Tools
        public static KeyCode setMoveType = KeyCode.W;
		public static KeyCode setRotateType = KeyCode.E;
		public static KeyCode setScaleType = KeyCode.R;
		public static KeyCode setAllTransformType = KeyCode.Y;
		public static KeyCode setSpaceToggle = KeyCode.X;
		public static KeyCode setPivotModeToggle = KeyCode.Z;
		public static KeyCode setCenterTypeToggle = KeyCode.C;
		public static KeyCode setScaleTypeToggle = KeyCode.S;
		public static KeyCode translationSnapping = KeyCode.LeftControl;
		public static KeyCode addSelection = KeyCode.LeftShift;
		public static KeyCode removeSelection = KeyCode.LeftControl;
		public static KeyCode actionKey = KeyCode.LeftShift; //Its set to shift instead of control so that while in the editor we dont accidentally undo editor changes =/
		public static KeyCode undoAction = KeyCode.Z;
		public static KeyCode redoAction = KeyCode.Y;
    }
}