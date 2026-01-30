using UnityEngine;
using UnityEngine.UI;

namespace HoverTanks.UI
{
	public class AccoladeReadoutItem : MonoBehaviour
	{
		private enum States
		{
			Idle,

			Displaying,
			Fading,
		}

		public bool IsActive => _field.gameObject.activeSelf;

		private const float DISPLAY_DURATION = 3;
		private const float FADE_DURATION = 1;
		private const float NEXT_CHARACTER_INTERVAL = 0.07f;

		[SerializeField] Text _field;
		[SerializeField] FadableGraphic _fade;

		private float _timeBeforeFade;
		private float _nextCharacterTimer;
		private int _nextCharacterIndex;
		private string _targetString;

		private States _state;

		public void Refresh(string target)
		{
			_field.text = "";
			_field.color = Color.white;
			_fade.StopFade();
			_targetString = target;
			_timeBeforeFade = Time.time + DISPLAY_DURATION;
			_nextCharacterTimer = 0;
			_nextCharacterIndex = 0;

			SetActive(true);
		}

		public void Update()
		{
			switch (_state)
			{
				case States.Idle: return;

				case States.Displaying:
				{
					if (_nextCharacterIndex < _targetString.Length)
					{
						_nextCharacterTimer -= Time.deltaTime;

						if (_nextCharacterTimer <= 0)
						{
							_field.text += _targetString[_nextCharacterIndex++];
							_nextCharacterTimer = NEXT_CHARACTER_INTERVAL;
						}
					}

					if (Time.time >= _timeBeforeFade)
					{
						_fade.FadeOut(FADE_DURATION);
						_state = States.Fading;
					}
				} break;

				case States.Fading:
				{
					if (Time.time >= _timeBeforeFade + FADE_DURATION)
					{
						SetActive(false);
					}
				} break;
			}
		}

		public void SetActive(bool active)
		{
			_field.gameObject.SetActive(active);

			if (active)
			{
				_state = States.Displaying;
			}
			else
			{
				_state = States.Idle;
			}
		}
	}
}
