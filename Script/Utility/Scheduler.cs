using UnityEngine;
using System;
using System.Collections.Generic;

public class Scheduler
{
	private class ScheduledAction
	{
		public readonly uint Id;

		private readonly Action _action;
		private readonly float _invokeTimestamp;

		private bool _hasInvoked;

		public ScheduledAction(uint id, Action action, float delay)
		{
			Id = id;

			_action = action;
			_invokeTimestamp = Time.time + delay;
		}

		public bool Invoke()
		{
			if (_hasInvoked)
			{
				return true;
			}

			if (Time.time < _invokeTimestamp)
			{
				return false;
			}

			_action.Invoke();
			_hasInvoked = true;

			return true;
		}
	}

	private List<ScheduledAction> _actions;
	private uint _numScheduledActions;

	public Scheduler()
	{
		_actions = new List<ScheduledAction>();
	}

	/// <summary>
	/// Schedule an action to occur after a delay.
	/// </summary>
	/// <param name="action">The action to perform.</param>
	/// <param name="delay">The delay from now in scaled time the action will be invoked.</param>
	/// <returns>The id of the scheduled action. Use this to cancel redundant actions.</returns>
	public uint Schedule(Action action, float delay)
	{
		var scheduledAction = new ScheduledAction(_numScheduledActions++, action, delay);
		_actions.Add(scheduledAction);

		return scheduledAction.Id;
	}

	/// <summary>
	/// Check if an action id is valid.
	/// </summary>
	public bool IsIdValid(uint id)
	{
		int index = GetActionIndexById(id);
		return index >= 0;
	}

	/// <summary>
	/// Cancel an action by its id.
	/// </summary>
	public bool Cancel(uint id)
	{
		int index = GetActionIndexById(id);

		if (index < 0)
		{
			return false;
		}

		_actions.RemoveAt(index);

		return true;
	}

	/// <summary>
	/// Process the scheduler's actions.
	/// </summary>
	public void Process()
	{
		if (_actions.Count == 0)
		{
			return;
		}

		for (int i = _actions.Count - 1; i >= 0; --i)
		{
			if (!_actions[i].Invoke())
			{
				continue;
			}

			_actions.RemoveAt(i);
		}
	}

	private int GetActionIndexById(uint id)
	{
		for (int i = 0; i < _actions.Count; ++i)
		{
			if (_actions[i].Id != id)
			{
				continue;
			}

			return i;
		}

		return -1;
	}
}
