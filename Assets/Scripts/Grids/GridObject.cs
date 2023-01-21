using UnityEngine;

namespace VipereSolide.Grids
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class GridObject : MonoBehaviour
    {
        private void LateUpdate()
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