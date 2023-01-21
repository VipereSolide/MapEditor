using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public class MaterialTiler : MonoBehaviour
{
    [SerializeField] protected Vector2 _tiling;

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
        _meshRenderer.material.mainTextureScale = new Vector2(transform.localScale.x * _tiling.x, transform.localScale.z * _tiling.y);
    }
}