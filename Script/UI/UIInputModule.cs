using UnityEngine;
using System;
using System.Collections.Generic;

namespace HoverTanks.UI
{
	public interface ISelectableOwner : ICurrentSelectable
	{
		PlayerId PlayerId { get; }
		void RegisterSelectable(UISelectable selectable);
		void UnregisterSelectable(UISelectable selectable);
	}

	public interface ICurrentSelectable
	{
		UISelectable Current { get; }
		void SetCurrent(UISelectable target, UISelectable source = null);
	}

	public class UIInputModule : ISelectableOwner
	{
		private enum Direction
		{
			None,
			Up,
			Down,
			Left,
			Right
		}

		public Action OnNavigateUp;
		public Action OnNavigateDown;
		public Action OnNavigateLeft;
		public Action OnNavigateRight;
		public Action OnConfirm;
		public Action OnCancel;

		public PlayerId PlayerId { get; }
		public UISelectable Current { get; set; }

		private readonly List<UISelectable> _selectables;
		private Direction _currentDirection;

		public UIInputModule(PlayerId playerId)
		{
			PlayerId = playerId;
			_selectables = new List<UISelectable>();
		}

		public void Update()
		{
			float h = Input.GetAxis("h");
			float v = Input.GetAxis("v");

			if (_currentDirection == Direction.None)
			{
				if (v > 0)
				{
					_currentDirection = Direction.Up;
					OnNavigateUp?.Invoke();
				}
				else if (v < 0)
				{
					_currentDirection = Direction.Down;
					OnNavigateDown?.Invoke();
				}
				else if (h < 0)
				{
					_currentDirection = Direction.Left;
					OnNavigateLeft?.Invoke();
				}
				else if (h > 0)
				{
					_currentDirection = Direction.Right;
					OnNavigateRight?.Invoke();
				}
			}
			else
			{
				if (h == 0 && v == 0)
				{
					_currentDirection = Direction.None;
				}
			}

			if (Input.GetButtonDown("Cancel"))
			{
				OnCancel?.Invoke();
			}
		}

		public void RegisterSelectable(UISelectable selectable)
		{
			if (_selectables.Contains(selectable))
			{
				return;
			}

			_selectables.Add(selectable);
		}

		public void UnregisterSelectable(UISelectable selectable)
		{
			if (Current == selectable)
			{
				Current.Deselect();
				Current = null;
			}

			_selectables.Remove(selectable);
		}

		/// <summary>
		/// Sets the current selectable.
		/// </summary>
		public void SetCurrent(UISelectable target, UISelectable source = null)
		{
			// abort if no change
			if (Current == target)
			{
				return;
			}

			if (target == null)
			{
				// prevent clear if source isn't current
				if (source != Current)
				{
					return;
				}
			}
			else
			{
				// abort if target not interactable
				if (!target.interactable)
				{
					return;
				}
			}

			// abort if not registered
			if (!_selectables.Contains(target))
			{
				return;
			}

			// deselect current
			if (Current != null)
			{
				Current.Deselect();
			}

			Current = target;

			// select new current
			if (Current != null)
			{
				Current.Select();
			}
		}
	}
}
