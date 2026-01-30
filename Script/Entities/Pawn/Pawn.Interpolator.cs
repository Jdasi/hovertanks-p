using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Entities
{
    public partial class Pawn
    {
        private class Interpolator
        {
            private const float BASE_MAX_DIST_TO_STATE = 5f;
            private const float BASE_MAX_TIMED_DIST_TO_STATE = 0.5f;
            private const float MARGIN_OF_ERROR_TIMEOUT_SHORT = 3f;
            private const float MARGIN_OF_ERROR_TIMEOUT_MEDIUM = 4f;
            private const float MARGIN_OF_ERROR_TIMEOUT_LONG = 5f;
            private const float VELOCITY_MAG_FACTOR = 2f;
            private const float POS_LERP_SPEED = 3f;
            private const float ROT_LERP_SPEED = 2f;
            private const float GOOD_ENOUGH_DIST = 0.025f;

            private readonly Pawn _pawn;
            private float _interpTimer;

            public Interpolator(Pawn pawn)
            {
                _pawn = pawn;
            }

            public void Run()
            {
                int latency;

                if (Server.IsActive && Server.Clients.TryGetValue(_pawn.identity.clientId, out var client))
                {
                    latency = client.Latency;
                }
                else
                {
                    latency = GameClient.Latency;
                }

                Vector3 targetPos = _pawn._observerSyncState.Position;
                Quaternion targetRot = _pawn._observerSyncState.Rotation;

                // interpolate position
                if (HandlePositionInterpolation(targetPos, latency))
                {
                    _interpTimer = 0;
                }

                // interpolate rotation
                _pawn._rotationTransform.rotation = Quaternion.Lerp(_pawn._rotationTransform.rotation, targetRot, ROT_LERP_SPEED * Time.fixedDeltaTime);
            }

            private bool HandlePositionInterpolation(in Vector3 targetPos, int latency)
            {
                float distToState = Vector3.Distance(_pawn.Position, targetPos);

                // good enough
                if (distToState <= GOOD_ENOUGH_DIST)
                {
                    return true;
                }

                float latencyDistTolerance = (latency / 1000f) * _pawn._rb.linearVelocity.magnitude * VELOCITY_MAG_FACTOR;

                // unreasonable margin of error, warp
                if (distToState > BASE_MAX_DIST_TO_STATE + latencyDistTolerance
                    ||
                    (distToState > BASE_MAX_TIMED_DIST_TO_STATE + latencyDistTolerance && _interpTimer >= MARGIN_OF_ERROR_TIMEOUT_SHORT)
                    ||
                    (distToState > Mathf.Max(BASE_MAX_DIST_TO_STATE, latencyDistTolerance) && _interpTimer >= MARGIN_OF_ERROR_TIMEOUT_MEDIUM)
                    ||
                    _interpTimer >= MARGIN_OF_ERROR_TIMEOUT_LONG)
                {
                    _pawn.transform.position = targetPos;
                    return true;
                }

                // not moving
                if (_pawn._rb.linearVelocity.sqrMagnitude == 0)
                {
                    PosInterpStep(targetPos);
                    return false;
                }

                Vector3 dirToState = (targetPos - _pawn.Position).normalized;
                float dotToUp = Vector3.Dot(dirToState, Vector3.up);

                // target above and outside stricter tolerance
                if (dotToUp >= 0.5f
                    && distToState > latencyDistTolerance / 2)
                {
                    PosInterpStep(targetPos);
                    return false;
                }

                Vector3 dirOfTravel = _pawn._rb.linearVelocity.normalized;
                float dotToTravel = Vector3.Dot(dirToState, dirOfTravel);

                // target ahead
                if (dotToTravel >= 0.5f)
                {
                    PosInterpStep(targetPos);
                    return false;
                }

                // target too far behind
                if (distToState > latencyDistTolerance)
                {
                    PosInterpStep(targetPos);
                    return false;
                }

                // moving within tolerance
                return true;
            }

            private void PosInterpStep(Vector3 targetPos)
            {
                _pawn.transform.position = Vector3.Lerp(_pawn.transform.position, targetPos, POS_LERP_SPEED * Time.fixedDeltaTime);
                _interpTimer += Time.fixedDeltaTime;
            }
        }
    }
}
