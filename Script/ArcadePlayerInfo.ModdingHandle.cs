using HoverTanks.Loadouts;

public partial class ArcadePlayerInfo
{
    public class ModdingHandle
    {
        public delegate void ReactorChangedEvent(int numUnlocked, int numUsed);
	    public event ReactorChangedEvent OnReactorChanged;

        private ArcadePlayerInfo _player;
        private ArcadePlayerShopData _shopData;

        public ModdingHandle(ArcadePlayerInfo player)
        {
            _player = player;
            _shopData = player._localShopData;

            // TODO - load reactor cost data associated with all the player's addons
                // weapon/module mod costs will need to be loaded from the prefab somehow
                // module and augment costs can be loaded from the shop json file
        }

        public int GetCurrentCredits()
        {
            return _player.Credits;
        }

        public int GetWeaponModReactorCost()
        {
            return -1;
        }

        public int GetModuleReactorCost()
        {
            return -1;
        }

        public int GetAugmentReactorCost()
        {
            return -1;
        }

        public bool CanUpgradeReactor()
        {
            return _shopData.UnlockedReactorSlots < ArcadePlayerShopData.MAX_REACTOR_SLOT_UNLOCKS;
        }

        public bool UpgradeReactor()
        {
            if (!CanUpgradeReactor())
            {
                return false;
            }

            ++_shopData.UnlockedReactorSlots;
            OnReactorChanged?.Invoke(_shopData.UnlockedReactorSlots, _shopData.UsedReactorSlots);

            return true;
        }

        public bool IsModSlotActive(EquipmentType type, Slots slot)
        {
            switch (type)
            {
                case EquipmentType.Weapon: return _player.Loadout.EnabledWeaponMods.HasFlag(slot);
                case EquipmentType.Module: return _player.Loadout.EnabledModuleMods.HasFlag(slot);
                case EquipmentType.Augment:
                {
                    switch (slot)
                    {
                        case Slots.Slot1: return _player.Loadout.Augment1Class != AugmentClass.Invalid;
                        case Slots.Slot2: return _player.Loadout.Augment2Class != AugmentClass.Invalid;
                        case Slots.Slot3: return _player.Loadout.Augment3Class != AugmentClass.Invalid;
                    }
                } break;
            }

            return false;
        }

        private bool ModifyReactorSlotUsage(int amount)
        {
            if (amount > 0
                && _shopData.RemainingReactorSlots <= 0)
            {
                // no remaining slots
                return false;
            }
            else if (amount < 0
                && _shopData.UsedReactorSlots <= 0)
            {
                // no used slots
                return false;
            }
            else if (amount == 0)
            {
                // no change
                return false;
            }

            _shopData.UsedReactorSlots += amount;
            OnReactorChanged?.Invoke(_shopData.UnlockedReactorSlots, _shopData.UsedReactorSlots);

            return true;
        }
    }
}
