using HoverTanks.UI;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public interface IUpgradeBayUI
{
	/*
	void SetMenuName(string name);
	void Backtrack();
	PlayerShopData PlayerShopData { get; }
	*/
}

public class UpgradeBayUI : MonoBehaviour, IUpgradeBayUI
{
	/*
	public Action OnUIClosed { get; set; }
	public bool IsActive { get; private set; }
	public PlayerShopData PlayerShopData { get; private set; }

	[Header("Menus")]
	[SerializeField] UpgradeBayMenu _mainMenu;
	[SerializeField] UpgradeBayMenu[] _menus;

	[Header("References")]
	[SerializeField] GameObject _uiRoot;
	[SerializeField] Text _lblMenuName;
	[SerializeField] Text _lblCredits;
	[SerializeField] ReactorSlotUI[] _reactorSlots;
	[SerializeField] Image _imgReactorCapLine;

	private IModShopManager _modShopManager;
	private UIInputModule _inputModule;
	private Stack<UpgradeBayMenu> _menuStack;
	private UISelectable[] _selectables;
	private UpgradeBayMenu _currentMenu { get { return _menuStack.Count > 0 ? _menuStack.Peek() : null; } }

	public void SetMenuName(string name)
	{
		_lblMenuName.text = name;
	}

	public void Init(IModShopManager modShopManager)
	{
		_modShopManager = modShopManager;
		_menuStack = new Stack<UpgradeBayMenu>();
		_selectables = GetComponentsInChildren<UISelectable>(true);

		if (_menus != null)
		{
			foreach (var menu in _menus)
			{
				menu.Init(modShopManager, this);
				menu.OnMenuActivated += OnMenuActivated;
			}
		}

		_uiRoot.SetActive(false);
	}

	public void Activate(PlayerId playerId, IClassName weaponClass, IClassName moduleClass)
	{
		if (IsActive)
		{
			return;
		}

		IsActive = true;

		_inputModule = new UIInputModule(playerId);
		_inputModule.OnCancel += Backtrack;

		// update selectable ownership
		foreach (var selectable in _selectables)
		{
			selectable.SetOwner(_inputModule);
		}

		PlayerShopData = _modShopManager.GetModDataForPlayer(playerId);
		PlayerShopData.OnReactorChanged += OnReactorChanged;

		// needs to happen at least a frame later because UI
		//GameManager.ScheduleAction(ForceSyncReactorUI, 0.01f);

		_lblCredits.text = $"{PlayerShopData.Credits}";
		_mainMenu.Activate();
		_uiRoot.SetActive(true);
	}

	public void Deactivate()
	{
		if (!IsActive)
		{
			return;
		}

		IsActive = false;
		OnUIClosed?.Invoke();

		_inputModule = null;
		_uiRoot.SetActive(false);

		if (PlayerShopData != null)
		{
			PlayerShopData.OnReactorChanged -= OnReactorChanged;
		}

		PlayerShopData = null;

		foreach (var menu in _menus)
		{
			menu.Deactivate();
		}
	}

	public void Backtrack()
	{
		// deactivate current
		if (_currentMenu != null)
		{
			_currentMenu.Deactivate();
			_menuStack.Pop();
		}

		if (_currentMenu != null)
		{
			// re-activate previous
			_currentMenu.Activate();
			_menuStack.Pop();
		}
		else
		{
			// terminate ui
			Deactivate();
		}
	}

	public void BtnUpgradeReactor()
	{
		PlayerShopData.UpgradeReactor();
	}

	private void OnMenuActivated(UpgradeBayMenu menu)
	{
		_menuStack.Push(menu);

		if (_inputModule == null)
		{
			return;
		}

		// controller should start with a button selected
		if (GameManager.IsUsingController)
		{
			foreach (var selectable in _selectables)
			{
				if (!selectable.interactable)
				{
					continue;
				}

				_inputModule.SetCurrent(selectable);
				break;
			}
		}
	}

	private void Update()
	{
		_inputModule?.Update();
	}

	private void ForceSyncReactorUI()
	{
		if (PlayerShopData == null)
		{
			return;
		}

		OnReactorChanged(PlayerShopData.UnlockedReactorSlots, PlayerShopData.UsedReactorSlots);
	}

	private void OnReactorChanged(int numUnlocked, int numUsed)
	{
		_imgReactorCapLine.enabled = numUnlocked < PlayerShopData.MAX_REACTOR_SLOTS;

		for (int i = 0; i < _reactorSlots.Length; ++i)
		{
			var slot = _reactorSlots[i];
			bool moveCapLine = false;

			if (i < numUsed)
			{
				slot.SetState(ReactorSlotUI.States.Used);
				moveCapLine = true;
			}
			else if (i < numUnlocked)
			{
				slot.SetState(ReactorSlotUI.States.Unlocked);
				moveCapLine = true;
			}
			else
			{
				slot.SetState(ReactorSlotUI.States.Locked);
			}

			if (moveCapLine)
			{
				_imgReactorCapLine.rectTransform.position = slot.rectTransform.position + slot.rectTransform.up *
					((slot.rectTransform.rect.height / 2) + (_imgReactorCapLine.rectTransform.rect.height / 2));
			}
		}
	}
	*/
}
