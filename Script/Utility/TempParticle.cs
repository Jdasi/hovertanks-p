using System.Collections;
using UnityEngine;

public class TempParticle : MonoBehaviour
{
    void Start()
    {
        ParticleSystem system = GetComponent<ParticleSystem>();

        if (system == null)
        {
            system = GetComponentInChildren<ParticleSystem>();
        }

        StartCoroutine(CleanUp(system));
    }

    IEnumerator CleanUp(ParticleSystem system)
    {
        yield return new WaitUntil(() => !system.isEmitting);
        system.Stop();

        yield return new WaitUntil(() => !system.IsAlive());
        Destroy(this.gameObject);
    }

}
