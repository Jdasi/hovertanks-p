using HoverTanks.Entities;
using HoverTanks.Networking;
using UnityEngine;

public partial class ArcadeLevel_Base
{
    private class TeleportProcess
    {
        private enum States
        {
            CreateEffect,
            MovePawn,
            Done
        }

        public PlayerId PlayerId => _playerId;
        public bool IsDone => _state == States.Done;

        private readonly PlayerId _playerId;
        private readonly Pawn _pawn;
        private readonly GameObject _effectPrefab;
        private readonly float _createEffectTime;
        private readonly float _movePawnTime;
        private readonly Vector3 _moveTarget;

        private States _state;

        public TeleportProcess(Pawn pawn, GameObject effectPrefab, float createEffectTime)
        {
            _playerId = pawn.identity.playerId;
            _pawn = pawn;
            _effectPrefab = effectPrefab;
            _createEffectTime = createEffectTime;
            _movePawnTime = createEffectTime + TELEPORT_DELAY_BEFORE_MOVE;

            _moveTarget = pawn.Position;
            _moveTarget.y = 0.35f;
        }

        public void Run()
        {
            switch (_state)
            {
                case States.CreateEffect:
                {
                    if (Time.time < _createEffectTime)
                    {
                        return;
                    }

                    Instantiate(_effectPrefab, _moveTarget, Quaternion.identity);

                    _state = States.MovePawn;
                } break;

                case States.MovePawn:
                {
                    if (Time.time < _movePawnTime)
                    {
                        return;
                    }

                    if (Server.IsActive)
                    {
                        _pawn.transform.position = _moveTarget;
                    }

                    _state = States.Done;
                } break;
            }
        }
    }
}
