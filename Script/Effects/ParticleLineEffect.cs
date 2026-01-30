using UnityEngine;

namespace HoverTanks.Effects
{
    public class ParticleLineEffect : LineEffect
    {
        [Header("References")]
        [SerializeField] ParticleSystem _particleLine;

        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            _particleLine.transform.SetParent(null);
            _particleLine.Stop();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (ShootPoint == null)
            {
                return;
            }

            _particleLine.transform.position = ShootPoint.position;
            _particleLine.transform.forward = ShootPoint.forward;

            Scan(out var _, out var _);
        }
    }
}
