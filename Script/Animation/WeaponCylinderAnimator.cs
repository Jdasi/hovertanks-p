using HoverTanks.Entities;
using UnityEngine;

namespace HoverTanks.Animation
{
    public class WeaponCylinderAnimator : MonoBehaviour
    {
        [Header("Parameters")]
        [SerializeField] float _accel;
        [SerializeField] float _decel;
        [SerializeField] float _maxSpeed;
        [SerializeField] Vector3 _axis;

        [Header("References")]
        [SerializeField] Pawn _pawn;
        [SerializeField] Transform _cylinder;

        private float _currentSpeed;

        private void Update()
        {
            if (_pawn.WeaponInfo.IsActive
                || _pawn.WeaponInfo.IsSpoolingUp)
            {
                _currentSpeed = Mathf.Min(_currentSpeed + _accel, _maxSpeed);
            }
            else
            {
                _currentSpeed = Mathf.Max(_currentSpeed - _decel, 0);
            }

            if (_currentSpeed > 0)
            {
                _cylinder.Rotate(_axis, _currentSpeed * Time.deltaTime);
            }
        }
    }
}
