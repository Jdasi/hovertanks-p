using System.Collections.Generic;
using UnityEngine;

namespace HoverTanks.StatusEffects
{
    public class StatusEffectFactory : MonoBehaviour
    {
        private static StatusEffectFactory _instance;

        private Dictionary<StatusClass, StatusEffect> _types;

        private void Awake()
        {
            if (_instance == null)
            {
                InitSingleton();
            }
            else
            {
                Destroy(this);
            }
        }

        public static bool TryCreate(StatusClass @class, out StatusEffect status)
        {
            if (_instance._types == null
                || !_instance._types.TryGetValue(@class, out var type))
            {
                Log.Error(LogChannel.StatusEffectFactory, $"Create - no type found for {@class}, ensure one is added to the factory prefab");

                status = null;
                return false;
            }

            status = Instantiate(type);
            return true;
        }

        private void InitSingleton()
        {
            _instance = this;
            EnumerateStatusEffects();
        }

        private void EnumerateStatusEffects()
        {
            var typesList = Resources.LoadAll<StatusEffect>("Statuses");
            _types = new Dictionary<StatusClass, StatusEffect>(typesList.Length);

            foreach (var status in typesList)
            {
                _types.Add(status.StatusClass, status);
            }
        }
    }
}
