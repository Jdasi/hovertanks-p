using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.GameState
{
    public class GameStateAdventure : GameState
    {
        [SerializeField] Transform _cameraPivot;
        [SerializeField] float _lerpSpeed;
        [SerializeField] float _leadVelocityFactor;
        [SerializeField] float _leadAimFactor;
        [SerializeField] float _lerpAheadMax;

        private Pawn _playerPawn;
        private Vector3 _playerAimPos;

        protected override void OnStateEnter()
        {
        }

        private void Update()
        {
        }

        private void FixedUpdate()
        {
            if (_playerPawn == null)
            {
                return;
            }

            Vector3 playerPos = _playerPawn.Position;
            Vector3 playerVelocity = _playerPawn.Velocity;
            Vector3 avgPosition = playerPos;

            uint numPositionsToAverage = 1;

            if (_leadAimFactor > 0 && _playerAimPos != default)
            {
                avgPosition += _playerAimPos * _leadAimFactor;
                ++numPositionsToAverage;
            }
            else if (_leadVelocityFactor > 0 && playerVelocity != default)
            {
                avgPosition += playerPos + playerVelocity * _leadVelocityFactor;
                ++numPositionsToAverage;
            }

            // lerp towards the average positon
            avgPosition /= numPositionsToAverage;
            avgPosition.y = 0;
            _cameraPivot.position = Vector3.Lerp(_cameraPivot.position, avgPosition, _lerpSpeed * Time.deltaTime);

            // clamp leading
            Vector3 dir = _cameraPivot.position - playerPos;
            if (Vector3.Magnitude(dir) > _lerpAheadMax)
            {
                _cameraPivot.position = playerPos + dir.normalized * _lerpAheadMax;
            }

            //GameManager.ControllerAimBoundsCenter = _cameraPivot.position;
        }
    }
}
