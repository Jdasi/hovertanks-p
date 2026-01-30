using UnityEngine;

public class LightningZap : MonoBehaviour
{
    [SerializeField] LineRenderer _line;

    private float _widthFactor;

    public void SetEnabled(bool enabled)
    {
        if (_line.enabled == enabled)
        {
            return;
        }

        _line.enabled = enabled;
    }

    public void SetWidthFactor(float widthFactor)
    {
        _widthFactor = widthFactor;
    }

    public void Jitter(Vector3 from, Vector3 to, int maxPoints = 2)
    {
        SetEnabled(true);

        float width = Random.Range(0.5f, 1f) * _widthFactor;
        _line.startWidth = width;
        _line.endWidth = width;

        // create some points for the lightning arc
        int numPoints = (int)Vector3.Distance(from, to) + 1;
        numPoints = Mathf.Max(numPoints, maxPoints);
        _line.positionCount = numPoints;

        _line.SetPosition(0, from);
        _line.SetPosition(numPoints - 1, to);

        // fake a lightning arc by scattering the middle points
        for (int i = 1; i < numPoints - 1; ++i)
        {
            float step = (float)(i + 1) / (numPoints + 1);

            Vector3 pos = Vector3.Lerp(from, to, step);
            pos += new Vector3(Random.Range(-0.75f, 0.75f), Random.Range(-0.5f, 0.5f), Random.Range(-0.75f, 0.75f));

            _line.SetPosition(i, pos);
        }
    }

    private void OnEnable()
    {
        SetEnabled(true);
    }

    private void OnDisable()
    {
        SetEnabled(false);
    }
}
