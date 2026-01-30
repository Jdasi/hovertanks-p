using System.Collections.Generic;
using UnityEngine;

public interface IDebrisManager
{
    void UnregisterDebris(Debris debris);
}

public class DebrisManager : MonoBehaviour, IDebrisManager
{
    private static DebrisManager _instance;

    private List<Debris> _debris;

    public static void Register(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (obj.GetComponent<Debris>())
        {
            return;
        }

        var debris = obj.AddComponent<Debris>();
        debris.Init(_instance);

        _instance._debris.Add(debris);
    }

    public static void Flush()
    {
        for (int i = 0; i < _instance._debris.Count; ++i)
        {
            var debris =_instance. _debris[i];

            if (debris == null)
            {
                continue;
            }

            debris.Destroy();
        }

        _instance._debris.Clear();
    }

    private void Awake()
    {
        if (_instance == null)
        {
            InitSingleton();
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void InitSingleton()
    {
        _instance = this;
        _debris = new List<Debris>(10);
    }

    public void UnregisterDebris(Debris debris)
    {
        _instance._debris.Remove(debris);
    }
}
