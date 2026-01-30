using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageFlasher : MonoBehaviour
{
    private class MatPack
    {
        public List<Texture> OriginalTextures;
        public List<Color> OriginalColors;

        public MatPack()
        {
            OriginalTextures = new List<Texture>();
            OriginalColors = new List<Color>();
        }
    }

    public bool IsFlashing => _flashCountdown > 0;

    private readonly Color _color = Color.white;
    private readonly float _duration = 0.05f;

    private List<MeshRenderer> _renderers = new List<MeshRenderer>();
    private List<MatPack> _originals = new List<MatPack>();

    private float _flashCountdown;

    public void Flash()
    {
        bool alreadyFlashing = IsFlashing;

        // reset countdown
        _flashCountdown = _duration;

        if (alreadyFlashing)
        {
            return;
        }

        for (int i = 0; i < _renderers.Count; ++i)
        {
            var r = _renderers[i];

            for (int j = 0; j < r.materials.Length; ++j)
            {
                r.materials[j].SetTexture("_MainTex", GameManager.instance.DefaultTexture);
                r.materials[j].color = _color;
            }
        }
    }

    public void Cancel()
    {
        if (!IsFlashing)
        {
            return;
        }

        ResetMaterials();

        _flashCountdown = 0;
    }

    private void Start()
    {
        CacheRenderers(this.transform);

        for (int i = 0; i < _renderers.Count; ++i)
        {
            var r = _renderers[i];

            MatPack pack = new MatPack();

            for (int j = 0; j < r.materials.Length; ++j)
            {
                var mat = r.materials[j];
                pack.OriginalTextures.Add(mat.mainTexture);
                pack.OriginalColors.Add(mat.color);
            }

            _originals.Add(pack);
        }
    }

    private void Update()
    {
        if (_flashCountdown <= 0)
        {
            return;
        }

        _flashCountdown -= Time.deltaTime;

        if (_flashCountdown <= 0)
        {
            ResetMaterials();
        }
    }

    private void OnDisable()
    {
        Cancel();
    }

    private void CacheRenderers(Transform transform)
    {
        foreach (Transform child in transform)
        {
            var renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                _renderers.Add(renderer);
            }

            if (child.childCount > 0)
            {
                CacheRenderers(child);
            }
        }
    }

    private void ResetMaterials()
    {
        for (int i = 0; i < _renderers.Count; ++i)
        {
            var r = _renderers[i];
            var pack = _originals[i];

            for (int j = 0; j < r.materials.Length; ++j)
            {
                r.materials[j].SetTexture("_MainTex", pack.OriginalTextures[j]);
                r.materials[j].color = pack.OriginalColors[j];
            }
        }
    }

    private void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
    }

}