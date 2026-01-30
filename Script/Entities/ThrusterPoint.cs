using System;
using UnityEngine;

namespace HoverTanks.Entities
{
    [Serializable]
    public class ThrusterPoint
    {
        public Transform Point;
        public ParticleSystem ThrusterPrefab;
        public ParticleSystem SmokePrefab;

        [Space]
        public float RayCastDistance;
        public float RateIntensityMod;
        public float StartSizeMin;
        public float StartSizeMaxA;
        public float StartSizeMaxBMod;

        private HoverTankThruster _thruster;
        private ParticleSystem _smoke;

        private static int _floorLayer = -1;

        public void Init()
        {
            if (_floorLayer < 0)
            {
                _floorLayer = LayerMask.NameToLayer("Floor");
            }

            _thruster = GameObject.Instantiate(ThrusterPrefab, Point).GetComponent<HoverTankThruster>();
            _thruster.transform.localPosition = Vector3.zero;

            _smoke = GameObject.Instantiate(SmokePrefab);
        }

        public void Update(float intensity, bool turboOn)
        {
            if (turboOn)
            {
                intensity = 1.33f;
            }

            var hit = Physics.Raycast(_thruster.transform.position, -_thruster.transform.up, out var hitInfo, RayCastDistance, ~_floorLayer);

            var emissionModule = _smoke.emission;
            float hitIntensity = 0;

            // smoke intensity from distance to floor
            if (hit)
            {
                hitIntensity = RayCastDistance - hitInfo.distance;
                _smoke.transform.position = hitInfo.point;
            }

            emissionModule.rateOverTime = RateIntensityMod * hitIntensity;

            // thruster intensity from movement
            ParticleSystem.MainModule mainModule = _thruster.Main;
            float max = Mathf.Max(StartSizeMaxA, StartSizeMaxBMod * intensity);
            mainModule.startSize = new ParticleSystem.MinMaxCurve(StartSizeMin, max);

            // turbo
            _thruster.SetTurboEnabled(turboOn);
        }

        public void Cleanup()
        {
            if (_thruster != null)
            {
                GameObject.Destroy(_thruster.gameObject);
            }

            if (_smoke != null)
            {
                GameObject.Destroy(_smoke.gameObject);
            }
        }
    }
}
