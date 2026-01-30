using HoverTanks.UI;
using UnityEngine;
using System;

public class UpgradeBayEquipmentMenu : UpgradeBayTypedMenu
{
	[SerializeField] UpgradeBayItemSelectMenu _itemSelectMenu;
	[SerializeField] UIButton _btnSlot;
	[SerializeField] PowerableUpgradeUI[] _upgradeButtons;

	protected override string MenuName => $"{_itemType} Slot";

	protected override void OnActivate()
	{
		/*
		_btnSlot.interactable = _itemType != ItemType.Weapon;

		switch (_itemType)
		{
			case ItemType.Weapon:
			{
				EnumerateUpgrades(_upgradeBayUI.PlayerShopData.WeaponClass, _upgradeBayUI.PlayerShopData.IsModSlotActive,
					_modShopManager.GetWeaponModClassesForClass);
			} break;

			case ItemType.Module:
			{
				EnumerateUpgrades(_upgradeBayUI.PlayerShopData.ModuleClass, _upgradeBayUI.PlayerShopData.IsModSlotActive,
					_modShopManager.GetModuleModClassesForClass);
			} break;
		}
		*/
	}

	protected override void OnDeactivate()
	{
	}

	public void BtnSlot()
	{
		_itemSelectMenu.SetItemType(_itemType);
		_itemSelectMenu.SetModifyingSlotIndex(0);
		_itemSelectMenu.Activate(this);
	}

	public void BtnUpgrade(int slot)
	{
		/*
		_upgradeBayUI.PlayerShopData.ToggleModSlotActive(_itemType, (ModSlot)slot);
		_upgradeButtons[slot].SetPowered(_upgradeBayUI.PlayerShopData.IsModSlotActive(_itemType, (ModSlot)slot));
		*/
	}

	/*
	private void EnumerateUpgrades<T>(IClassName iClass, Func<ItemType, ModSlot, bool> isUpgradeActive, Func<string, T[]> getMods) where T : IClassName
	{
		bool isClassValid = iClass != null && !string.IsNullOrWhiteSpace(iClass.Name);
		_btnSlot.text = isClassValid ? iClass.Name : "Empty";

		if (!isClassValid)
		{
			foreach (var button in _upgradeButtons)
			{
				button.gameObject.SetActive(false);
			}

			return;
		}

		var mods = getMods(iClass.ClassName);

		if (mods == null)
		{
			return;
		}

		for (int i = 0; i < _upgradeButtons.Length; ++i)
		{
			var button = _upgradeButtons[i];
			bool hasMod = mods.Length > i;

			button.gameObject.SetActive(hasMod);

			if (!hasMod)
			{
				continue;
			}

			button.Init(mods[i], isUpgradeActive(_itemType, (ModSlot)i));
		}
	}
	*/
}
