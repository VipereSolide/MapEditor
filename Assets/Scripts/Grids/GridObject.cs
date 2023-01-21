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

        public enum GridType
        {
            Floor,
            Ceil,
            None
        }

        [SerializeField] protected ExecuteMode _executeMode = ExecuteMode.LateUpdate;
        [SerializeField] protected GridType _xGrid = GridType.Floor;
        [SerializeField] protected GridType _yGrid = GridType.Ceil;
        [SerializeField] protected GridType _zGrid = GridType.Floor;

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

            Vector3 __truePosition = transform.localPosition.GetGrid(_xGrid, _yGrid, _zGrid);
            transform.localPosition = __truePosition;
        }
    }
}