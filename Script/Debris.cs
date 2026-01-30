using HoverTanks.Events;
using UnityEngine;

public class Debris : MonoBehaviour
{
    private IDebrisManager _manager;

    public void Init(IDebrisManager manager)
    {
        _manager = manager;

        LocalEvents.Subscribe<SceneChangeData>(OnSceneChange);
    }

    public void Destroy()
    {
        Destroy(this.gameObject);
    }

    private void OnDestroy()
    {
        if (_manager == null)
        {
            return;
        }

        _manager.UnregisterDebris(this);

        LocalEvents.Unsubscribe<SceneChangeData>(OnSceneChange);
    }

    private void OnSceneChange(SceneChangeData data)
    {
        Destroy();
    }
}
