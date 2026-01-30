using System;
using UnityEngine;

public class CollisionEventForwarder : MonoBehaviour
{
    public event Action<GameObject, Collider> TriggerEntered;
    public event Action<GameObject, Collider> TriggerExited;
    public event Action<GameObject, Collider> TriggerStayed;
    public event Action<GameObject, Collision> CollisionEntered;
    public event Action<GameObject, Collision> CollisionExited;
    public event Action<GameObject, Collision> CollisionStayed;

    private void OnTriggerEnter(Collider other)
    {
        TriggerEntered?.Invoke(gameObject, other);
    }

    private void OnTriggerExit(Collider other)
    {
        TriggerExited?.Invoke(gameObject, other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerStayed?.Invoke(gameObject, other);
    }

    private void OnCollisionEnter(Collision other)
    {
        CollisionEntered?.Invoke(gameObject, other);
    }

    private void OnCollisionExit(Collision other)
    {
        CollisionExited?.Invoke(gameObject, other);
    }

    private void OnCollisionStay(Collision other)
    {
        CollisionStayed?.Invoke(gameObject, other);
    }
}
