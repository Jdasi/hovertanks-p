using HoverTanks.UI;
using UnityEngine;

public class UpgradeBayAugmentSlotsMenu : UpgradeBayMenu
{
	[SerializeField] UpgradeBayItemSelectMenu _itemSelectMenu;
	[SerializeField] UIButton[] _slots;

	protected override string MenuName => $"Augment Slots";

	protected override void OnActivate()
	{
		/*
		for (int i = 0; i < _slots.Length; ++i)
		{
			ClassInfo activeClass = _upgradeBayUI.PlayerShopData.GetAugmentClass((ModSlot)i);
			string name = activeClass == null ? "Empty" : activeClass.Name;
			_slots[i].text = name;
		}
		*/
	}

	protected override void OnDeactivate()
	{
	}

	public void BtnSlot(int slot)
	{
		/*
		_itemSelectMenu.SetItemType(ItemType.Augment);
		_itemSelectMenu.SetModifyingSlotIndex(slot);
		_itemSelectMenu.Activate(this);
		*/
	}
}
