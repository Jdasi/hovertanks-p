using UnityEngine;
using UnityEngine.UI;

public class ItemKiosk : MonoBehaviour
{
	[SerializeField] GameObject _root;
	[SerializeField] Text _lblName;
	[SerializeField] Text _lblDescription;
	[SerializeField] Text _lblCost;

	private const float DELAY_BEFORE_AUTO_HIDE = 3f;
	private const float NO_AUTO_HIDE_TIME = 0f;

	private int _activeStandCount;
	private float _autoHideTime;

	public void Hide()
	{
		_root.SetActive(false);
		UpdateDisplay("", "", 0);
	}

	public void StandActivated(ShopInfo info)
	{
		UpdateDisplay(info.DisplayName, info.Description, info.CreditCost);
		_root.SetActive(true);

		++_activeStandCount;
		_autoHideTime = NO_AUTO_HIDE_TIME;
	}

	public void StandDeactivated()
	{
		--_activeStandCount;

		if (_activeStandCount == 0)
		{
			_autoHideTime = Time.time + DELAY_BEFORE_AUTO_HIDE;
		}
	}

    private void Awake()
    {
        Hide();
    }

    private void FixedUpdate()
    {
        if (_autoHideTime == NO_AUTO_HIDE_TIME
			|| Time.time < _autoHideTime)
		{
			return;
		}

		_root.SetActive(false);
		_autoHideTime = NO_AUTO_HIDE_TIME;
    }

    private void UpdateDisplay(string title, string body, uint cost)
	{
		if (_lblName.text.Equals(title))
		{
			return;
		}

		_lblName.text = title;
		_lblDescription.text = body;
		_lblCost.text = $"Cost: {cost}";
	}
}
