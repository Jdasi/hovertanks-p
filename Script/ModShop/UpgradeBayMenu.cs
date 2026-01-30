using System;
using UnityEngine;

public abstract class UpgradeBayMenu : MonoBehaviour
{
	public enum Ids
	{
		Invalid,

		Main,
		Weapon,
		Module,
		Augments,
		ItemSelect
	}

	public Action<UpgradeBayMenu> OnMenuActivated { get; set; }

	/*
	protected IModShopManager _modShopManager;
	protected IUpgradeBayUI _upgradeBayUI;

	public void Init(IModShopManager modShopManager, IUpgradeBayUI upgradeBayUI)
	{
		_modShopManager = modShopManager;
		_upgradeBayUI = upgradeBayUI;

		Deactivate();
	}
	*/

	/// <summary>
	/// Activate this menu, and cache the menu we came from.
	/// </summary>
	public void Activate(UpgradeBayMenu prevMenu = null)
	{
		/*
		if (gameObject.activeSelf)
		{
			return;
		}

		if (prevMenu != null)
		{
			prevMenu.Deactivate();
		}

		_upgradeBayUI.SetMenuName(MenuName);

		gameObject.SetActive(true);
		OnActivate();

		OnMenuActivated?.Invoke(this);
		*/
	}

	/// <summary>
	/// Turn off the menu.
	/// </summary>
	public void Deactivate()
	{
		if (!gameObject.activeSelf)
		{
			return;
		}

		gameObject.SetActive(false);
		OnDeactivate();
	}

	protected abstract string MenuName { get; }
	protected virtual void OnActivate() { }
	protected virtual void OnDeactivate() { }
}

public abstract class UpgradeBayTypedMenu : UpgradeBayMenu
{
	protected EquipmentType _itemType;

	public void SetItemType(EquipmentType type)
	{
		_itemType = type;
	}
}
