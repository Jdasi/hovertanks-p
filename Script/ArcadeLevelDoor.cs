using HoverTanks.Entities;
using System;
using UnityEngine;

public class ArcadeLevelDoor : MonoBehaviour
{
    public enum States
    {
        Closed,

        Opening,
        Open,

        Closing,
    }

    public bool IsOpen => _state == States.Open || _state == States.Closing;
    public Vector3 Position => _door.position;
    public Transform[] PlayerSpawns => _playerSpawns;

    public event Action<PlayerId> PlayerEntered;

    [SerializeField] Transform[] _playerSpawns;
    [SerializeField] Transform _door;
    [SerializeField] float _yOpen;
    [SerializeField] float _yClosed;

    private States _state;

    public void Open()
    {
        _state = States.Opening;
    }

    public void Close()
    {
        _state = States.Closing;
    }

    public void SetOpen(bool open)
    {
        _door.localPosition = new Vector3(_door.localPosition.x, open ? _yOpen : _yClosed, _door.localPosition.z);
        _state = open ? States.Open : States.Closed;
    }

    private void FixedUpdate()
    {
        switch (_state)
        {
            case States.Opening:
            case States.Closing:
            {
                bool open = _state == States.Opening;
                Vector3 step = Vector3.up * Time.fixedDeltaTime * 2 * Time.timeScale;

                _door.localPosition += open ? -step : step;

                // opening
                if (open && _door.localPosition.y <= _yOpen)
                {
                    SetOpen(true);
                }
                // closing
                else if (!open && _door.localPosition.y >= _yClosed)
                {
                    SetOpen(false);
                }
            } break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Unit"))
        {
            return;
        }

        var pawn = other.GetComponent<Pawn>();

        if (pawn == null)
        {
            return;
        }

        PlayerEntered?.Invoke(pawn.identity.playerId);
    }
}
