using UnityEngine;

public class BlobShadowController : MonoBehaviour
{
    [SerializeField] GameObject _blob;

    private void FixedUpdate()
    {
        if (GameManager.fixedFrameCount % 2 != 0)
        {
            return;
        }

        if (!Physics.Raycast(transform.position, Vector3.down, out var hitInfo, 20, GameManager.instance.BounceLayer))
        {
            _blob.SetActive(false);
        }
        else
        {
            _blob.transform.position = hitInfo.point;
            _blob.SetActive(true);
        }
    }

}
