using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public class MaterialTiler : MonoBehaviour
{
    public enum ScaleDimensions
    {
        XY,
        YX,
        XZ,
        ZX,
        YZ,
        ZY
    }

    [SerializeField] protected Vector2 _tiling;
    [SerializeField] protected bool _useCopiedObject;
    [SerializeField] protected Transform _copiedObject;
    [SerializeField] protected ScaleDimensions _dimensions;

    protected MeshRenderer _meshRenderer;

    private void Start()
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    private void Update()
    {
        Vector3 __copiedScale = transform.localScale;

        if (_useCopiedObject)
        {
            __copiedScale = _copiedObject.localScale;
        }

        if (_dimensions == ScaleDimensions.ZX)
        {
        _meshRenderer.material.mainTextureScale = new Vector2(__copiedScale.z * _tiling.x, __copiedScale.x * _tiling.y);
        } else if (_dimensions == ScaleDimensions.XZ)
        {
        _meshRenderer.material.mainTextureScale = new Vector2(__copiedScale.x * _tiling.x, __copiedScale.z * _tiling.y);
        } else if (_dimensions == ScaleDimensions.YX)
        {
        _meshRenderer.material.mainTextureScale = new Vector2(__copiedScale.y * _tiling.x, __copiedScale.x * _tiling.y);
        } else if (_dimensions == ScaleDimensions.XY)
        {
        _meshRenderer.material.mainTextureScale = new Vector2(__copiedScale.x * _tiling.x, __copiedScale.y * _tiling.y);
        }else if (_dimensions == ScaleDimensions.YZ)
        {
        _meshRenderer.material.mainTextureScale = new Vector2(__copiedScale.y * _tiling.x, __copiedScale.z * _tiling.y);
        }else if (_dimensions == ScaleDimensions.ZY)
        {
        _meshRenderer.material.mainTextureScale = new Vector2(__copiedScale.z * _tiling.x, __copiedScale.y * _tiling.y);
        }
    }
}