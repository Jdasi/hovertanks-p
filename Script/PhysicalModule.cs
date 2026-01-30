using UnityEngine;

public class PhysicalModule : MonoBehaviour
{
    public Transform ActivatePoint => _activatePoint;

    [SerializeField] Transform _activatePoint;

    [Header("Coloring")]
    [SerializeField] MeshRenderer _bodyMesh;
    [SerializeField] MeshRenderer _accentMesh;
    [SerializeField] MeshRenderer _darkMesh;

    private Material _bodyMat;
    private Material _accentMat;
    private Material _darkMat;

    public void Recolor(Color[] colors)
    {
        if (colors == null
            || colors.Length != 3)
        {
            return;
        }

        ColorMesh(_bodyMesh, ref _bodyMat, colors[0]);
        ColorMesh(_accentMesh, ref _accentMat, colors[1]);
        ColorMesh(_darkMesh, ref _darkMat, colors[2]);
    }

    private void OnDestroy()
    {
        DestroyMat(_bodyMat);
        DestroyMat(_accentMat);
        DestroyMat(_darkMat);
    }

    private void ColorMesh(MeshRenderer mesh, ref Material mat, Color color)
    {
        mat = mesh.material;
        mat.color = color;
    }

    private void DestroyMat(Material mat)
    {
        if (mat == null)
        {
            return;
        }

        Destroy(mat);
    }
}
