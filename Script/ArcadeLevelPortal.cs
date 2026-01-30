using HoverTanks.Entities;
using HoverTanks.Events;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ArcadeLevelPortal : MonoBehaviour
{
    public event Action<PlayerId> PlayerInteracted;

    private List<PlayerId> _playersToGiveInteractWhenFormed;
    private float _readyTimestamp;
    private bool _processedPawnsQueue;

    public bool IsFullyFormed()
    {
        return Time.time >= _readyTimestamp;
    }

    private void Awake()
    {
        _playersToGiveInteractWhenFormed = new List<PlayerId>();
        _readyTimestamp = Time.time + 3;
    }

    private void FixedUpdate()
    {
        if (_processedPawnsQueue)
        {
            return;
        }

        if (!IsFullyFormed())
        {
            return;
        }

        foreach (var player in _playersToGiveInteractWhenFormed)
        {
            AddInteractContext(player);
        }

        _playersToGiveInteractWhenFormed = null;
        _processedPawnsQueue = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetPawnFromCollider(other, out var pawn))
        {
            return;
        }

        if (!IsFullyFormed())
        {
            _playersToGiveInteractWhenFormed.Add(pawn.identity.playerId);
        }
        else
        {
            AddInteractContext(pawn.identity.playerId);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryGetPawnFromCollider(other, out var pawn))
        {
            return;
        }

        if (!IsFullyFormed())
        {
            _playersToGiveInteractWhenFormed.Remove(pawn.identity.playerId);
        }
        else
        {
            RemoveInteractContext(pawn.identity.playerId);
        }
    }

    private bool TryGetPawnFromCollider(Collider other, out Pawn pawn)
    {
        pawn = null;

        if (!other.CompareTag("Unit"))
        {
            return false;
        }

        pawn = other.GetComponent<Pawn>();
        return pawn != null;
    }

    private void AddInteractContext(PlayerId playerId)
    {
        LocalEvents.Invoke(new AddInteractContextData()
        {
            Uid = gameObject.GetInstanceID(),
            PlayerId = playerId,
            Callback = OnPlayerInteracted,
            Description = "Use Portal"
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

    private void OnPlayerInteracted(PlayerId playerid)
    {
        PlayerInteracted?.Invoke(playerid);
    }
}
