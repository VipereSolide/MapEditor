using UnityEngine;

namespace VipereSolide.Extentions
{
    public static class Vector3Extentions
    {
        public static Vector3 SetX(this Vector3 _base, float _value)
        {
            return new Vector3(_value, _base.y, _base.z);
        }

        public static Vector3 SetY(this Vector3 _base, float _value)
        {
            return new Vector3(_base.x, _value, _base.z);
        }

        public static Vector3 SetZ(this Vector3 _base, float _value)
        {
            return new Vector3(_base.x, _base.y, _value);
        }
    }
}