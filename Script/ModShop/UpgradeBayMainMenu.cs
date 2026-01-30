using UnityEngine;

public class UpgradeBayMainMenu : UpgradeBayMenu
{
	[SerializeField] UpgradeBayEquipmentMenu _equipmentMenu;
	[SerializeField] UpgradeBayAugmentSlotsMenu _augmentSlotsMenu;

	protected override string MenuName => "Main Menu";

	public void BtnWeaponMenu()
	{
		_equipmentMenu.SetItemType(EquipmentType.Weapon);
		_equipmentMenu.Activate(this);
	}

	public void BtnModuleMenu()
	{
		_equipmentMenu.SetItemType(EquipmentType.Module);
		_equipmentMenu.Activate(this);
	}

	public void BtnAugmentSlotsMenu()
	{
		_augmentSlotsMenu.Activate(this);
	}
}
