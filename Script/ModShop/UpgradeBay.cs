using System.Collections.Generic;
using UnityEngine;

public class UpgradeBay : MonoBehaviour
{
	private enum States
	{
		Invalid = -1,

		Idle,
		LockingInVehicle,
		ShowingUI,
		UnlockingVehicle,
		DelayBeforeIdle,
	}

	[SerializeField] float _snapDistance;
	[SerializeField] UpgradeBayUI _ui;
	[SerializeField] Transform _snapPoint;
	[SerializeField] Transform _depositPoint;

/*
	private List<HoverTank> _tanksInArea = new List<HoverTank>();
	private HoverTank _tank;
	private States _state = States.Invalid;
	private float _stateEnterTimestamp;

	public void Init(IModShopManager modShopManager)
	{
		_ui.Init(modShopManager);
		_ui.OnUIClosed += OnUIClosed;
	}

	public void Start()
	{
		SetState(States.Idle);
	}

	public void Update()
	{
		switch (_state)
		{
			case States.LockingInVehicle: LockingInVehicleOnUpdate(); break;
			case States.ShowingUI: ShowingUIOnUpdate(); break;
			case States.UnlockingVehicle: UnlockingVehicleOnUpdate(); break;
		}
	}

	public void FixedUpdate()
	{
		switch (_state)
		{
			case States.Idle: IdleOnUpdate(); break;
			case States.DelayBeforeIdle: DelayBeforeIdleOnUpdate(); break;
		}
	}

	private void IdleOnUpdate()
	{
		if (_tank != null)
		{
			return;
		}

		if (_tanksInArea.Count == 0)
		{
			return;
		}

		for (int i = 0; i < _tanksInArea.Count; ++i)
		{
			var tank = _tanksInArea[i];
			float distance = Vector3.Distance(tank.Position, _snapPoint.position);

			if (distance > _snapDistance)
			{
				continue;
			}

			LockInVehicle(tank);

			break;
		}

		if (_tank == null)
		{
			return;
		}

		foreach (var tank in _tanksInArea)
		{
			if (tank.UnitId == _tank.UnitId)
			{
				continue;
			}

			Vector2 randomPos = Random.insideUnitCircle * 1;
			tank.transform.position = _depositPoint.position + new Vector3(randomPos.x, 0, randomPos.y);
		}

		_tanksInArea.Clear();

		SetState(States.LockingInVehicle);
	}

	private void LockingInVehicleOnUpdate()
	{
		SetState(States.ShowingUI);
	}

	private void ShowingUIOnUpdate()
	{
	}

	private void UnlockingVehicleOnUpdate()
	{
		SetState(States.DelayBeforeIdle);
	}

	private void DelayBeforeIdleOnUpdate()
	{
		if (Time.time - _stateEnterTimestamp < 3)
		{
			return;
		}

		SetState(States.Idle);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_state != States.Idle)
		{
			return;
		}

		var tank = other.GetComponent<HoverTank>();

		if (tank == null)
		{
			return;
		}

		if (_tanksInArea.Contains(tank))
		{
			return;
		}

		_tanksInArea.Add(tank);
	}

	private void OnTriggerExit(Collider other)
	{
		switch (_state)
		{
			case States.Idle:
			case States.DelayBeforeIdle:
				break;

			default: return;
		}

		var tank = other.GetComponent<HoverTank>();

		if (tank == null)
		{
			return;
		}

		_tanksInArea.Remove(tank);

		if (_tank != null
			&& _tank.UnitId == tank.UnitId)
		{
			_tank = null;
		}
	}

	private void LockInVehicle(HoverTank tank)
	{
		if (_state != States.Idle)
		{
			return;
		}

		if (_tank != null)
		{
			return;
		}

		_tank = tank;
		_tank.Controller.SetControlDisabled(ControlRequestSources.ModShop);
		_tank.transform.SetParent(_snapPoint);
		_tank.transform.position = _snapPoint.position;
		_tank.SetKinematic(true);

		_ui.Activate(tank.PlayerId, tank.WeaponInfo, tank.ModuleInfo);
	}

	private void OnUIClosed()
	{
		if (_tank == null)
		{
			return;
		}

		_tank.Controller.SetControlEnabled(ControlRequestSources.ModShop);
		_tank.SetKinematic(false);

		_state = States.UnlockingVehicle;
	}

	private void SetState(States state)
	{
		if (_state == state)
		{
			return;
		}

		_state = state;
		_stateEnterTimestamp = Time.time;
	}
*/
}
