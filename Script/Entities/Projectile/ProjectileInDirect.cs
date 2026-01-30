using HoverTanks.Networking;
using System.Collections;
using UnityEngine;

namespace HoverTanks.Entities
{
    public class ProjectileInDirect : Projectile
    {
        [SerializeField] float _delayBeforeExplosion;
        [SerializeField] GameObject _explosionPrefab;
        [SerializeField] LayerMask _scanLayer;
        [SerializeField] GameObject[] _telegraphObjs;
        [SerializeField] EffectAudioSettings _leadAudio;

        private bool _hitFloor = true;

        protected override void Start()
        {
            base.Start();

            Physics.Raycast(transform.position + Vector3.up * 10, -Vector3.up, out var hitInfo, 100, _scanLayer);

            if (hitInfo.collider.CompareTag("Void"))
            {
                foreach (var obj in _telegraphObjs)
                {
                    obj.SetActive(false);
                }

                _hitFloor = false;
            }

            if (_hitFloor)
            {
                transform.position = hitInfo.point + Vector3.up * 0.05f;

                StartCoroutine(ExplodeRoutine());
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        private IEnumerator ExplodeRoutine()
        {
            if (_leadAudio != null)
            {
                yield return new WaitForSeconds(_delayBeforeExplosion - _leadAudio.Clip.length);

                AudioManager.PlayClipAtPoint(_leadAudio, transform.position);

                yield return new WaitForSeconds(_leadAudio.Clip.length);
            }
            else
            {
                yield return new WaitForSeconds(_delayBeforeExplosion);
            }

            if (_explosionPrefab != null
                && _hitFloor)
            {
                var deathEffect = Instantiate(_explosionPrefab, transform.position, transform.rotation);
                DebrisManager.Register(deathEffect);

                if (Server.IsActive)
                {
                    var explosion = deathEffect.GetComponent<Explosion>();
                    explosion?.Init(Owner);
                }
                Destroy(this.gameObject);

                DynamicDecals.PaintExplosion(transform.position, 1.15f);
            }
        }
    }
}
