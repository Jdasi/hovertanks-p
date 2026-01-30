using HoverTanks.Entities;
using HoverTanks.Networking;
using UnityEngine;

public class Void : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!Server.IsActive)
        {
            return;
        }

        var life = other.GetComponent<LifeForce>();

        if (life == null)
        {
            return;
        }

        if (!life.IsAlive)
        {
            return;
        }

        if (!life.LastExternalDamageInfo.HasExpired)
        {
            life.Kill(life.LastExternalDamageInfo.Inflictor, ElementType.Void);
        }
        else
        {
            life.Kill();
        }
    }
}
