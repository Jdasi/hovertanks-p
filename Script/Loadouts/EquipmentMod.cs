using UnityEngine;

namespace HoverTanks.Loadouts
{
    public abstract class EquipmentMod : ScriptableObject
    {
        public static class Aspects
        {
            public interface ICleanup
            {
                void Cleanup();
            }
        }

        protected Equipment Equipment { get; private set; }

        /// <summary>
        /// Creates an instance of the mod and applies it to the equipment.
        /// </summary>
        public void Apply(Equipment equipment)
        {
            var instance = Instantiate(this);
            instance.Init(equipment);

            if (instance is Aspects.ICleanup aspect)
            {
                instance.Equipment.Destroyed += aspect.Cleanup;
            }
        }

        private void Init(Equipment equipment)
        {
            Equipment = equipment;
            OnInit();
        }

        protected abstract void OnInit();
    }
}
