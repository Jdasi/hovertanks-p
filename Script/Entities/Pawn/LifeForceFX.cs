using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoverTanks.Entities
{
#if UNITY_EDITOR
    [CustomEditor(typeof(LifeForceFX))]
    public class LifeForceFX_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var script = (LifeForceFX)target;
            if (GUILayout.Button("Populate"))
            {
                script.Editor_Populate();
                EditorUtility.SetDirty(script);
            }
        }
    }
#endif

    public class LifeForceFX : MonoBehaviour
    {
        [SerializeField] LifeForce _life;
        [SerializeField] ParticleSystem _overheatDamageEffect;

        [Space]
        [SerializeField] ParticleSystem[] _damageParticlesLow;
        [SerializeField] ParticleSystem[] _damageParticlesMedium;
        [SerializeField] ParticleSystem[] _damageParticlesHeavy;

        [Space]
        [SerializeField] ParticleSystem[] _heatParticlesMedium;
        [SerializeField] ParticleSystem[] _heatParticlesHigh;
        [SerializeField] ParticleSystem[] _heatParticlesCritical;

        private EffectsSource _damageSource;
        private EffectsSource _glancingBlowSource;
        private EffectsSource _heatSource;
        private EffectsSource _heatCriticalSource;

        private void Awake()
        {
            _damageSource = AudioManager.CreateEffectsSource(this.transform);
            _damageSource.name = "DamageSource";

            _glancingBlowSource = AudioManager.CreateEffectsSource(this.transform);
            _glancingBlowSource.name = "GlancingBlowSource";

            _heatSource = AudioManager.CreateEffectsSource(this.transform);
            _heatSource.Init(GameManager.instance.SteamAudio);
            _heatSource.loop = true;
            _heatSource.volume = 0;
            _heatSource.name = "HeatSource";

            _heatCriticalSource = AudioManager.CreateEffectsSource(this.transform);
            _heatCriticalSource.Init(GameManager.instance.HeatCriticalAudio);
            _heatCriticalSource.name = "HeatCriticalSource";

            _life.OnDamage += OnDamage;
            _life.OnHealBasic += OnHealBasic;
            _life.OnHeatLevelChanged += OnHeatLevelChanged;
            _life.OnHealthChanged += OnHealthChanged;
            _life = null;
        }

        private void OnDamage(ElementData data)
        {
            _damageSource.Play(GameManager.instance.DamageAudio);

            if (Bit.IsSet((int)data.Flags, (int)ElementFlags.WasGlancingBlow))
            {
                _glancingBlowSource.Play(GameManager.instance.GlancingBlowAudio);
            }

            if (_overheatDamageEffect != null
                && data.Element == ElementType.Overheat)
            {
                _overheatDamageEffect.Play();
            }
        }

        private void OnHealBasic()
        {
            _damageSource.Play(GameManager.instance.HealAudio);
        }

        private void OnHeatLevelChanged(HeatLevelChangedData data)
        {
            if (!_heatSource.isPlaying)
            {
                _heatSource.Play();
            }

            if (data.JustIncreasedToCritical)
            {
                _heatCriticalSource.Play();
            }

            _heatSource.volume = data.Level >= HeatLevel.Medium ? (float)data.Level / 100 : 0;

            SetParticlesPlaying(_heatParticlesMedium, data.Level >= HeatLevel.Medium);
            SetParticlesPlaying(_heatParticlesHigh, data.Level >= HeatLevel.High);
            SetParticlesPlaying(_heatParticlesCritical, data.Level >= HeatLevel.Critical);
        }

        private void OnHealthChanged(HealthChangedData data)
        {
            switch (data.Level)
            {
                case DamageLevel.None:
                case DamageLevel.Low:
                {
                    SetParticlesPlaying(_damageParticlesLow, false);
                    SetParticlesPlaying(_damageParticlesMedium, false);
                    SetParticlesPlaying(_damageParticlesHeavy, false);
                } break;

                case DamageLevel.Medium:
                {
                    SetParticlesPlaying(_damageParticlesLow, true);
                    SetParticlesPlaying(_damageParticlesMedium, false);
                    SetParticlesPlaying(_damageParticlesHeavy, false);
                } break;

                case DamageLevel.Heavy:
                {
                    SetParticlesPlaying(_damageParticlesLow, true);
                    SetParticlesPlaying(_damageParticlesMedium, true);
                    SetParticlesPlaying(_damageParticlesHeavy, false);
                } break;

                case DamageLevel.Critical:
                {
                    SetParticlesPlaying(_damageParticlesLow, true);
                    SetParticlesPlaying(_damageParticlesMedium, true);
                    SetParticlesPlaying(_damageParticlesHeavy, true);
                } break;
            }
        }

        private void SetParticlesPlaying(ParticleSystem[] particles, bool playing)
        {
            if (particles == null)
            {
                return;
            }

            foreach (var particle in particles)
            {
                if (playing)
                {
                    particle.Play();
                }
                else
                {
                    particle.Stop();
                }
            }
        }

#if UNITY_EDITOR
        public void Editor_Populate()
        {
            _life = GetComponentInParent<LifeForce>();

            var overheatTickRoot = _life.transform.FindDescendant("VehicleOverheatTick");
            if (overheatTickRoot != null)
            {
                _overheatDamageEffect = overheatTickRoot.GetComponent<ParticleSystem>();
            }

            var damageParticlesLow = new List<ParticleSystem>();
            var damageParticlesMedium = new List<ParticleSystem>();
            var damageParticlesHeavy = new List<ParticleSystem>();

            var heatParticlesMedium = new List<ParticleSystem>();
            var heatParticlesHigh = new List<ParticleSystem>();
            var heatParticlesCritical = new List<ParticleSystem>();

            var particles = GetComponentsInChildren<ParticleSystem>();

            // enumerate damage particles
            foreach (var particle in particles)
            {
                if (particle.name.Contains("VehicleDamage"))
                {
                    if (particle.name.EndsWith("_L"))
                    {
                        damageParticlesLow.Add(particle);
                    }
                    else if (particle.name.EndsWith("_M"))
                    {
                        damageParticlesMedium.Add(particle);
                    }
                    else if (particle.name.EndsWith("_H"))
                    {
                        damageParticlesHeavy.Add(particle);
                    }
                }
                else if (particle.name.Contains("EngineHeat"))
                {
                    if (particle.name.EndsWith("_L"))
                    {
                        heatParticlesMedium.Add(particle);
                    }
                    else if (particle.name.EndsWith("_M"))
                    {
                        heatParticlesHigh.Add(particle);
                    }
                    else if (particle.name.EndsWith("_H"))
                    {
                        heatParticlesCritical.Add(particle);
                    }
                }
            }

            _damageParticlesLow = damageParticlesLow.ToArray();
            _damageParticlesMedium = damageParticlesMedium.ToArray();
            _damageParticlesHeavy = damageParticlesHeavy.ToArray();

            _heatParticlesMedium = heatParticlesMedium.ToArray();
            _heatParticlesHigh = heatParticlesHigh.ToArray();
            _heatParticlesCritical = heatParticlesCritical.ToArray();
        }
#endif
    }
}
