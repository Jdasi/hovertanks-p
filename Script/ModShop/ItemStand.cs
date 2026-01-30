using HoverTanks.Entities;
using HoverTanks.Events;
using UnityEngine;

public class ItemStand : MonoBehaviour
{
	public EquipmentType Type;

	[SerializeField] GameObject _spinBox;
	[SerializeField] ItemKiosk _kiosk;

	private ShopInfo _info;

	public void Init(ShopInfo info)
	{
		_info = info;
		_spinBox.SetActive(true);
	}

	public void Start()
	{
		_spinBox.SetActive(_info != null);
	}

	public void OnTriggerEnter(Collider other)
	{
		_kiosk.StandActivated(_info);

		var pawn = other.GetComponent<Pawn>();

		if (pawn != null)
		{
			AddInteractContext(pawn.identity.playerId);
		}
	}

    public void OnTriggerExit(Collider other)
    {
        _kiosk.StandDeactivated();

        var pawn = other.GetComponent<Pawn>();

        if (pawn != null)
        {
            RemoveInteractContext(pawn.identity.playerId);
        }
    }

    private void AddInteractContext(PlayerId playerId)
    {
        LocalEvents.Invoke(new AddInteractContextData()
        {
            Uid = gameObject.GetInstanceID(),
            PlayerId = playerId,
            Callback = null,
            Description = "Purchase"
        });
    }

    private void RemoveInteractContext(PlayerId playerId)
    {
        LocalEvents.Invoke(new RemoveInteractContextData()
        {
            Uid = gameObject.GetInstanceID(),
            PlayerId = playerId,
        });
    }
}
