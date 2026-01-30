using HoverTanks.Loadouts;
using HoverTanks.Networking;
using UnityEngine;

namespace HoverTanks.Entities
{
    public interface IPawn
    {
        NetworkIdentity identity { get; }
        Vector3 Position { get; }
        Vector3 TargetMoveDir { get; }
        Transform SightPoint { get; }
        bool IsOverheating { get; }
        IWeaponInfo WeaponInfo { get; }
        IModuleInfo ModuleInfo { get; }
    }

    public interface IPawnControl : IPawn
    {
        void Move(float horizontal, float vertical);
        void Move(Vector3 dir);
        void ClearMove();
        void AddImpulse(Vector3 force);
        void StartAiming(Vector3 pos);
        void StopAiming();
        void StartTurbo();
        void StopTurbo();
        void Shoot(Vector3 aimPos = default);
        void StopShoot();
        void Reload();
        void StopReload();
        void StartModule(Vector3 aimPos = default);
        void StopModule();
        void ClearAllInput();
    }
}
