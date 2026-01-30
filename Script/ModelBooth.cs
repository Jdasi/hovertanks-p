using UnityEngine;
using System.Collections;

public class ModelBooth : MonoBehaviour
{
    public Texture Texture => _renderTex;

    [SerializeField] Camera _camera;
    [SerializeField] Transform _mount;

    private GameObject _model;
    private RenderTexture _renderTex;

    public void Init(GameObject prefab)
    {
        Cleanup(false);

        if (_renderTex == null)
        {
            _renderTex = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            _renderTex.Create();

            _camera.targetTexture = _renderTex;
            _camera.forceIntoRenderTexture = true;
        }

        _model = Instantiate(prefab, _mount);
        _model.transform.localScale = Vector3.one;
        _model.transform.localPosition = Vector3.zero;
        _model.transform.localRotation = Quaternion.identity;

        Invoke(nameof(DeactivateCamera), 1);
    }

    private void DeactivateCamera()
    {
        _camera.enabled = false;
    }

    private void OnDestroy()
    {
        Cleanup(true);
    }

    private void Cleanup(bool full)
    {
        if (full && _renderTex != null)
        {
            _renderTex.DiscardContents();
            _renderTex.Release();
        }

        if (_model != null)
        {
            Destroy(_model);
            _model = null;
        }
    }
}
