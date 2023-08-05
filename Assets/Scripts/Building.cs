using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class Building : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private LayerMask _ignoreLayer;

    public Vector2Int Size = Vector2Int.one;
    public BoxCollider objectCollider;

    private GameObject _cubeObj;

    public bool IsAvailable = true;

    public bool isOnLand = true;
    public bool isTriggered = false;

    private void Reset()
    {
        objectCollider = GetComponent<BoxCollider>();
    }

    public void SetTransparent()
    {
        if (IsAvailable)
        {
            RecolorMaterial(Color.green);
        }
        else
        {
            RecolorMaterial(Color.red);
        }
        CreateCube();
    }

    public void SetNormal()
    {
        Destroy(_cubeObj);
        RecolorMaterial(Color.white);
    }

    private void CreateCube()
    {
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                if (_cubeObj == null)
                {
                    Vector3 cubePosition = transform.position + new Vector3(x, 0, y);
                    _cubeObj = Instantiate(cubePrefab, cubePosition, Quaternion.identity, transform);
                }
                if (IsAvailable)
                {
                    _cubeObj.GetComponent<Renderer>().material.color = new Color(0, 255, 0, 0.6f);
                }
                else
                {
                    _cubeObj.GetComponent<Renderer>().material.color = new Color(255, 0, 0, 0.6f);
                }

                _cubeObj.transform.localScale = new Vector3(Size.x, 0.05f, Size.y);
                _cubeObj.transform.position = transform.position + new Vector3(x / 2, 0, y / 2);

                Vector3 cornerPosition = _cubeObj.transform.position - new Vector3(Size.x / 2f, 0, Size.y / 2f) + new Vector3(0, 0.2f, 0);
                float raycastDistance = 0.5f;

                Ray rayTopLeft = new(cornerPosition + new Vector3(0, 0, 0), Vector3.down * raycastDistance);
                Ray rayTopRight = new(cornerPosition + new Vector3(Size.x, 0, 0), Vector3.down * raycastDistance);
                Ray rayBottomLeft = new(cornerPosition + new Vector3(0, 0, Size.y), Vector3.down * raycastDistance);
                Ray rayBottomRight = new(cornerPosition + new Vector3(Size.x, 0, Size.y), Vector3.down * raycastDistance);

                isOnLand = true;

                if (!Physics.Raycast(rayTopLeft, raycastDistance, ~_ignoreLayer) ||
                    !Physics.Raycast(rayTopRight, raycastDistance, ~_ignoreLayer) ||
                    !Physics.Raycast(rayBottomLeft, raycastDistance, ~_ignoreLayer) ||
                    !Physics.Raycast(rayBottomRight, raycastDistance, ~_ignoreLayer))
                {
                    isOnLand = false;
                }

                if (isOnLand && !isTriggered)
                {
                    IsAvailable = true;
                }
                else
                {
                    IsAvailable = false;
                }
            }
        }
    }


    private void RecolorMaterial(Color color)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var item in renderers)
        {
            item.material.color = color;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other != null && other.gameObject != this)
        {
            isTriggered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject != this)
        {
            isTriggered = false;
        }
    }

    private void OnDrawGizmos()
    {
        if(_cubeObj != null)
        {
            Vector3 cornerPosition = _cubeObj.transform.position - new Vector3(Size.x / 2f, 0, Size.y / 2f) + new Vector3(0, 0.2f, 0);
            float raycastDistance = 0.5f;

            Gizmos.DrawRay(cornerPosition + new Vector3(0, 0, 0), Vector3.down * raycastDistance);
            Gizmos.DrawRay(cornerPosition + new Vector3(Size.x, 0, 0), Vector3.down * raycastDistance);
            Gizmos.DrawRay(cornerPosition + new Vector3(0, 0, Size.y), Vector3.down * raycastDistance);
            Gizmos.DrawRay(cornerPosition + new Vector3(Size.x, 0, Size.y), Vector3.down * raycastDistance);
        }
    }

}
