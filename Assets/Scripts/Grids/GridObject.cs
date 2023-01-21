using UnityEngine;

namespace VipereSolide.Grids
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class GridObject : MonoBehaviour
    {
        public enum ExecuteMode
        {
            Update,
            LateUpdate
        }

        [SerializeField] protected ExecuteMode _executeMode = ExecuteMode.LateUpdate;

        private void LateUpdate()
        {
            if (_executeMode == ExecuteMode.LateUpdate)
            {
                GetGrid();
            }
        }

        private void Update()
        {
            if (_executeMode == ExecuteMode.Update)
            {
                GetGrid();
            }
        }

        private void GetGrid()
        {
            if (Grid.enabled == false)
            {
                return;
            }

            Vector3 __truePosition = transform.localPosition;
            __truePosition.x = Mathf.Floor(__truePosition.x / Grid.gridScale) * Grid.gridScale;
            __truePosition.y = Mathf.Floor(__truePosition.y / Grid.gridScale) * Grid.gridScale;
            __truePosition.z = Mathf.Floor(__truePosition.z / Grid.gridScale) * Grid.gridScale;

            transform.localPosition = __truePosition;
        }
    }
}