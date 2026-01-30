using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReactorSlotUI : MonoBehaviour
{
	public enum States
	{
		Locked,
		Unlocked,
		Used
	}

	public States State { get; private set; }
	public RectTransform rectTransform => _imgFill.rectTransform;

	[SerializeField] Image _imgFill;

	[Space]
	[SerializeField] Color _lockedColor;
	[SerializeField] Color _unlockedColor;
	[SerializeField] Color _usedColor;

	public void SetState(States state)
	{
		State = state;

		switch (state)
		{
			case States.Locked:
			{
				_imgFill.color = _lockedColor;
			} break;

			case States.Unlocked:
			{
				_imgFill.color = _unlockedColor;
			} break;

			case States.Used:
			{
				_imgFill.color = _usedColor;
			} break;
		}
	}
}
