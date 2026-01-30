using UnityEngine;

public class LightningZapFactory : MonoBehaviour
{
    [SerializeField] LightningZap _prefab;

    public void CreateZap(Vector3 from, Vector3 to, float widthFactor = 1)
    {
        var zap = CreateZap(widthFactor);

        zap.Jitter(from, to);

        Destroy(zap.gameObject, 0.25f);
    }

    public LightningZap CreateZap(float widthFactor = 1, bool startEnabled = true)
    {
        var zap = Instantiate(_prefab);

        zap.SetWidthFactor(widthFactor);
        zap.SetEnabled(startEnabled);

        return zap;
    }
}
