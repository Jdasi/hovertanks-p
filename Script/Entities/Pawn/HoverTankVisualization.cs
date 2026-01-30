using System.Collections.Generic;
using UnityEngine;

namespace HoverTanks.Entities
{
    public class HoverTankVisualization : PawnVisualization
    {
        [Header("Animated Parts")]
        [SerializeField] AnimatedPartBody[] _bodyParts;
        [SerializeField] AnimatedPartPack[] _packParts;
        [SerializeField] AnimatedPartWing[] _wingParts;
        [SerializeField] AnimatedPartTurret[] _turretParts;

        [Header("Effects")]
        [SerializeField] ParticleSystem _ramParticle;
        [SerializeField] ThrusterPoint[] _thrusterPoints;

        private List<AnimatedPart> _animatedParts;

        protected override void OnInit(Transform rotationTransform)
        {
            // life listeners
            _pawn.Life.OnDeathBasic += OnDeathBasic;

            InitThrusterPoints();
            EnumerateAnimatedParts(rotationTransform);
        }

        private void OnDestroy()
        {
            if (_thrusterPoints != null)
            {
                for (int i = 0; i < _thrusterPoints.Length; ++i)
                {
                    _thrusterPoints[i].Cleanup();
                }
            }
        }

        private void EnumerateAnimatedParts(Transform rotationTransform)
        {
            _animatedParts = new List<AnimatedPart>();

            EnumerateAnimatedParts(ref _bodyParts, rotationTransform);
            EnumerateAnimatedParts(ref _packParts, rotationTransform);
            EnumerateAnimatedParts(ref _wingParts, rotationTransform);
            EnumerateAnimatedParts(ref _turretParts, rotationTransform);
        }

        private void EnumerateAnimatedParts<T>(ref T[] parts, Transform rotationTransform) where T : AnimatedPart
        {
            if (parts == null)
            {
                return;
            }

            foreach (var part in parts)
            {
                part.Init(rotationTransform);
                _animatedParts.Add(part);
            }

#if !DEBUG
            parts = null;
#endif
        }

        protected override void OnUpdate()
        {
            HandleRamEffect();
        }

        private void HandleRamEffect()
        {
            if (_ramParticle == null)
            {
                return;
            }

            if (_pawn.IsInRamState)
            {
                if (!_ramParticle.isPlaying)
                {
                    _ramParticle.Play();
                }
            }
            else
            {
                if (_ramParticle.isPlaying)
                {
                    _ramParticle.Stop();
                }
            }
        }

        protected override void OnFixedUpdate()
        {
            // update animated parts
            if (_animatedParts != null)
            {
                for (int i = 0; i < _animatedParts.Count; ++i)
                {
                    _animatedParts[i].HandleAnimation(_animData.ForwardDot, _animData.RightDot, _animData.TurnSpeed, _animData.IsTurboOn);
                }
            }

            // update thrusters
            if (_thrusterPoints != null)
            {
                float intensity = Mathf.Max(0, _animData.ForwardDot);
                for (int i = 0; i < _thrusterPoints.Length; ++i)
                {
                    _thrusterPoints[i].Update(intensity, _animData.IsTurboOn);
                }
            }
        }

        private void InitThrusterPoints()
        {
            if (_thrusterPoints == null)
            {
                return;
            }

            for (int i = 0; i < _thrusterPoints.Length; ++i)
            {
                _thrusterPoints[i].Init();
            }
        }

        private void OnDeathBasic()
        {
            DynamicDecals.PaintExplosion(transform.position);
            JHelper.BlowIntoPieces(gameObject);
        }
    }
}
