using System;

namespace HoverTanks.Loadouts
{
    [Serializable]
    public struct Loadout
    {
        public ModuleClass ModuleClass;
        public AugmentClass Augment1Class;
        public AugmentClass Augment2Class;
        public AugmentClass Augment3Class;

        public Slots EnabledWeaponMods;
        public Slots EnabledModuleMods;
    }

    [Flags]
    public enum Slots
    {
        None = 0,

        Slot1 = Bits.Bit0,
        Slot2 = Bits.Bit1,
        Slot3 = Bits.Bit2,
    }
}
