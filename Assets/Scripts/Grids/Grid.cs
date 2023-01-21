using UnityEngine;

namespace VipereSolide.Grids
{
    public static class Grid
    {
        public static float gridScale = 0.125f;
        public static bool enabled = true;

        public static Vector3 GetGrid(this Vector3 _source)
        {
            _source.x = Mathf.Ceil(_source.x / Grid.gridScale) * Grid.gridScale;
            _source.y = Mathf.Floor(_source.y / Grid.gridScale) * Grid.gridScale;
            _source.z = Mathf.Ceil(_source.z / Grid.gridScale) * Grid.gridScale;
            return _source;
        }

        public static Vector3 GetGrid(this Vector3 _source, GridObject.GridType _xGrid, GridObject.GridType _yGrid, GridObject.GridType _zGrid)
        {
            if (_xGrid != GridObject.GridType.None)
            {
                if (_xGrid == GridObject.GridType.Ceil) _source.x = Mathf.Ceil(_source.x / Grid.gridScale) * Grid.gridScale;
                else _source.x = Mathf.Floor(_source.x / Grid.gridScale) * Grid.gridScale;
            }

            if (_yGrid != GridObject.GridType.None)
            {
                if (_yGrid == GridObject.GridType.Ceil) _source.y = Mathf.Ceil(_source.y / Grid.gridScale) * Grid.gridScale;
                else _source.y = Mathf.Floor(_source.y / Grid.gridScale) * Grid.gridScale;
            }

            if (_zGrid != GridObject.GridType.None)
            {
                if (_zGrid == GridObject.GridType.Ceil) _source.z = Mathf.Ceil(_source.z / Grid.gridScale) * Grid.gridScale;
                else _source.z = Mathf.Floor(_source.z / Grid.gridScale) * Grid.gridScale;
            }

            return _source;
        }
    }
}