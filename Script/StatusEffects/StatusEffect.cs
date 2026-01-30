using HoverTanks.Networking;
using System.Linq;
using UnityEngine;

namespace HoverTanks.StatusEffects
{
    public class StatusEffect : ScriptableObject
    {
        public StatusClass StatusClass => _statusClass;
        public bool HasExpired => _expireTime != PERMANENT_DURATION && Time.time >= _expireTime;

        public const float PERMANENT_DURATION = -1;

        [SerializeField] StatusClass _statusClass;
        [SerializeField] EntityType[] _allowedTypes;
        [SerializeField] GameObject _visualizationPrefab;


        protected IStatusEffectTarget Target { get; private set; }

        private float _expireTime;
        private GameObject _activeVisualization;

        public bool TryInitialize(IStatusEffectTarget target, float duration)
        {
            if (target == null)
            {
                return false;
            }

            if (_allowedTypes == null
                || !_allowedTypes.Contains(target.EntityType))
            {
                return false;
            }

            Target = target;
            Refresh(duration);

            return true;
        }

        public void Refresh(float duration)
        {
            // permanent duration
            if (duration <= 0
                || _expireTime == PERMANENT_DURATION)
            {
                _expireTime = PERMANENT_DURATION;
                return;
            }

            // highest temporary duration
            _expireTime = Mathf.Max(_expireTime, Time.time + duration);
        }

        public void Start()
        {
            if (_visualizationPrefab != null)
            {
                _activeVisualization = CreateVisualization(Target as NetworkEntity, _visualizationPrefab);
            }

            OnStart();
        }

        public void Stop()
        {
            if (_activeVisualization != null)
            {
                Destroy(_activeVisualization);
            }

            OnStop();
        }

        public void Update()
        {
            OnUpdate();
        }

        public void FixedUpdate()
        {
            OnFixedUpdate();
        }

        protected bool GetTargetAs<T>(out T type) where T : NetworkEntity
        {
            if (Target is T target)
            {
                type = target;
                return true;
            }

            type = null;
            return false;
        }

        private GameObject CreateVisualization(NetworkEntity onEntity, GameObject prefab)
        {
            if (onEntity == null
                || prefab == null)
            {
                return null;
            }

            return Instantiate(prefab, onEntity.transform);
        }

        protected virtual void OnStart() { }
        protected virtual void OnStop() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnFixedUpdate() { }
    }
}
