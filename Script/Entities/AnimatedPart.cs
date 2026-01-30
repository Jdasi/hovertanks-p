using System;
using UnityEngine;

namespace HoverTanks.Entities
{
    public abstract class AnimatedPart
    {
        public Transform Target;
        public Vector3 StartRotation;

        private float _prevForwardDot;
        private float _prevRightDot;

        public void HandleAnimation(float forwardDot, float rightDot, float currentTurnSpeed, bool isTurboOn)
        {
            if (!Target.gameObject.activeInHierarchy)
            {
                return;
            }

            if (isTurboOn)
            {
                forwardDot *= 2.25f;
                rightDot *= 2.25f;
            }

            forwardDot = Mathf.Lerp(_prevForwardDot, forwardDot, 5 * Time.fixedDeltaTime);
            rightDot = Mathf.Lerp(_prevRightDot, rightDot, 5 * Time.fixedDeltaTime);

            OnHandleAnimation(forwardDot, rightDot, currentTurnSpeed);

            _prevForwardDot = forwardDot;
            _prevRightDot = rightDot;
        }

        public virtual void Init(Transform rotateTransform) { }

        protected abstract void OnHandleAnimation(float forwardDot, float rightDot, float currentTurnSpeed);
    }

    [Serializable]
    public class AnimatedPartBody : AnimatedPart
    {
        public float ForwardTilt;
        public float BackwardTilt;
        public float StrafeTilt;

        protected override void OnHandleAnimation(float forwardDot, float rightDot, float currentTurnSpeed)
        {
            float xTiltFactor;

            if (forwardDot >= 0)
            {
                xTiltFactor = ForwardTilt;
            }
            else
            {
                xTiltFactor = BackwardTilt;
            }

            float x = StartRotation.x + forwardDot * xTiltFactor;
            float y = StartRotation.y + -rightDot * StrafeTilt;

            Target.localEulerAngles = Quaternion.Euler(x, y, StartRotation.z).eulerAngles;
        }
    }

    [Serializable]
    public class AnimatedPartPack : AnimatedPart
    {
        public float Tilt;

        protected override void OnHandleAnimation(float forwardDot, float rightDot, float currentTurnSpeed)
        {
            float y = StartRotation.y + -rightDot * Tilt;
            Target.localEulerAngles = Quaternion.Euler(StartRotation.x, y, StartRotation.z).eulerAngles;
        }
    }

    [Serializable]
    public class AnimatedPartWing : AnimatedPart
    {
        public enum TiltAxis
        {
            X,
            Y,
            Z
        }

        public float TurnTilt;
        public float ForwardTilt;
        public float BackwardTilt;

        public TiltAxis TiltAx;

        protected override void OnHandleAnimation(float forwardDot, float rightDot, float currentTurnSpeed)
        {
            float axisStart = 0;
            switch (TiltAx)
            {
                case TiltAxis.X: axisStart = StartRotation.x; break;
                case TiltAxis.Y: axisStart = StartRotation.y; break;
                case TiltAxis.Z: axisStart = StartRotation.z; break;
            }

            float turn = axisStart + currentTurnSpeed * TurnTilt;
            float tiltFactor;

            if (forwardDot >= 0)
            {
                tiltFactor = ForwardTilt;
            }
            else
            {
                tiltFactor = BackwardTilt;
            }

            tiltFactor *= forwardDot;
            float tilt = turn + tiltFactor;

            switch (TiltAx)
            {
                case TiltAxis.X: ApplyTilt(tilt, StartRotation.y, StartRotation.z); break;
                case TiltAxis.Y: ApplyTilt(StartRotation.x, tilt, StartRotation.z); break;
                case TiltAxis.Z: ApplyTilt(StartRotation.x, StartRotation.y, tilt); break;
            }
        }

        private void ApplyTilt(float x, float y, float z)
        {
            Target.localEulerAngles = Quaternion.Euler(x, y, z).eulerAngles;
        }
    }

    [Serializable]
    public class AnimatedPartTurret : AnimatedPart
    {
        public enum ParentAxis
        {
            Forward,
            Up
        }

        public ParentAxis Axis;
        public bool CorrectX;

        private Transform _rotationTransform;

        public override void Init(Transform rotationTransform)
        {
            _rotationTransform = rotationTransform;
        }

        protected override void OnHandleAnimation(float forwardDot, float rightDot, float currentTurnSpeed)
        {
            Vector3 up = Axis == ParentAxis.Forward ? -Target.parent.forward : Target.parent.up;
            Target.LookAt(Target.position + _rotationTransform.forward, up);

            if (CorrectX)
            {
                Target.Rotate(90, 0, 0);
            }
        }
    }
}
