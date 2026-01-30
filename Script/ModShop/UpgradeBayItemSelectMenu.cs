using HoverTanks.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeBayItemSelectMenu : UpgradeBayTypedMenu
{
	[SerializeField] Text _lblSelectHeader;
	[SerializeField] Text _lblSelectDescription;
	[SerializeField] UIButton _btnPrevPage;
	[SerializeField] UIButton _btnNextPage;
	[SerializeField] UpgradeBayStorageItemSlot[] _slots;

	protected override string MenuName => $"{_itemType} Select";

	private List<string> _storage;
	private int _modifyingSlotIndex;
	private int _currentPage;
	private int _maxPages;

	public void SetModifyingSlotIndex(int index)
	{
		_modifyingSlotIndex = 0;
	}

	protected override void OnActivate()
	{
		/*
		switch (_itemType)
		{
			case ItemType.Weapon: _storage = _upgradeBayUI.PlayerShopData.WeaponStorage; break;
			case ItemType.Module: _storage = _upgradeBayUI.PlayerShopData.ModuleStorage; break;
			case ItemType.Augment: _storage = _upgradeBayUI.PlayerShopData.AugmentStorage; break;
		}

		_currentPage = 0;
		_maxPages = Mathf.CeilToInt((float)_storage.Count / _slots.Length);

		OnPageChanged();
		*/
	}

	protected override void OnDeactivate()
	{
		_btnPrevPage.interactable = false;
		_btnNextPage.interactable = false;
	}

	public void BtnItemClick(int slot)
	{
		/*
		var slotItem = _slots[slot];

		if (slotItem.ShopInfo == null)
		{
			Log.Error(LogChannel.ModShop, $"[UpgradeBayItemSelectMenu] BtnItemClick - shop info was null for slot {slot}");
			return;
		}

		ClassInfo info = new ClassInfo(slotItem.ShopInfo);

		switch (_itemType)
		{
			case ItemType.Weapon:
			case ItemType.Module:
			{
				_upgradeBayUI.PlayerShopData.ChangeEquipmentClass(_itemType, info);
			} break;

			case ItemType.Augment:
			{
				_upgradeBayUI.PlayerShopData.ChangeAugmentClass((ModSlot)_modifyingSlotIndex, info);
			} break;
		}

		// go to prev menu
		_upgradeBayUI.Backtrack();
		*/
	}

	public void BtnItemHover(int slot)
	{
		var shopInfo = _slots[slot].ShopInfo;

		if (shopInfo == null)
		{
			return;
		}

		UpdateText(shopInfo.DisplayName, shopInfo.Description);
	}

	public void BtnUnequip()
	{
		/*
		switch (_itemType)
		{
			case ItemType.Weapon:
			case ItemType.Module:
			{
				_upgradeBayUI.PlayerShopData.ChangeEquipmentClass(_itemType, null);
			} break;

			case ItemType.Augment:
			{
				_upgradeBayUI.PlayerShopData.ChangeAugmentClass((ModSlot)_modifyingSlotIndex, null);
			} break;
		}

		// go to prev menu
		_upgradeBayUI.Backtrack();
		*/
	}

	public void BtnPrevPage()
	{
		if (_currentPage <= 0)
		{
			return;
		}

		--_currentPage;

		OnPageChanged();
	}

	public void BtnNextPage()
	{
		if (_currentPage >= _maxPages)
		{
			return;
		}

		++_currentPage;

		OnPageChanged();
	}

	private void EnumerateSlots()
	{
		/*
		for (int i = 0; i < _slots.Length; ++i)
		{
			var slot = _slots[i];
			int storageIndex = (_currentPage * _slots.Length) + i;

			if (storageIndex >= _storage.Count)
			{
				slot.interactable = false;
				continue;
			}

			var shopInfo = _modShopManager.GetShopInfoForClass(_storage[storageIndex]);

			if (shopInfo == null)
			{
				slot.interactable = false;
				continue;
			}

			slot.SetShopInfo(shopInfo);
			slot.interactable = true;
		}
		*/
	}

	private void OnPageChanged()
	{
		_btnPrevPage.interactable = _currentPage > 0;
		_btnNextPage.interactable = _currentPage + 1 < _maxPages && _maxPages > 1;

		SetDefaultText();
		EnumerateSlots();
	}

	private void UpdateText(string header, string description)
	{
		_lblSelectHeader.text = header;
		_lblSelectDescription.text = description;
	}

	private void SetDefaultText()
	{
		UpdateText("---", "");
	}
}
