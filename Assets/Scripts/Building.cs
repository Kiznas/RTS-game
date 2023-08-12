using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class Building : MonoBehaviour
{
    [SerializeField] private GameObject _cubePrefab;
    [SerializeField] private LayerMask _ignoreLayer;
    [SerializeField] private bool _isAvaliable = true;
    [SerializeField] private Vector2Int _size = Vector2Int.one;
    [SerializeField] private BoxCollider _objectCollider;
    public bool IsAvailable { get { return _isAvaliable; } }
    public Vector2Int Size { get { return _size; } }

    private bool _isOnLand = true;
    private bool _isTriggered = false;
    private GameObject _cubeObj;
    

    private void Reset()
    {
        _objectCollider = GetComponent<BoxCollider>();
    }

    public void SetTransparent()
    {
        RecolorMaterial(_isAvaliable ? Color.green : Color.red);
        CreateCube();
    }

    public void SetNormal()
    {
        Destroy(_cubeObj);
        RecolorMaterial(Color.white);
        _objectCollider.isTrigger = false;
        SetID((byte)Random.Range(0, 255));
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
                    _cubeObj = Instantiate(_cubePrefab, cubePosition, Quaternion.identity, transform);
                }

                _cubeObj.GetComponent<Renderer>().material.color = IsAvailable ? new Color(0, 255, 0, 0.6f) : new Color(255, 0, 0, 0.6f);
                _cubeObj.transform.localScale = new Vector3(Size.x, 0.05f, Size.y);
                _cubeObj.transform.position = transform.position + new Vector3(x / 2, 0, y / 2);
            }
        }

        CheckIfOnLand();

        _isAvaliable = _isOnLand && !_isTriggered;
    }

    private void CheckIfOnLand()
    {
        Vector3 cornerPosition = _cubeObj.transform.position - new Vector3(Size.x / 2f, 0, Size.y / 2f) + new Vector3(0, 0.2f, 0);
        float raycastDistance = 0.5f;

        Ray rayTopLeft = new(cornerPosition + new Vector3(0, 0, 0), Vector3.down * raycastDistance);
        Ray rayTopRight = new(cornerPosition + new Vector3(Size.x, 0, 0), Vector3.down * raycastDistance);
        Ray rayBottomLeft = new(cornerPosition + new Vector3(0, 0, Size.y), Vector3.down * raycastDistance);
        Ray rayBottomRight = new(cornerPosition + new Vector3(Size.x, 0, Size.y), Vector3.down * raycastDistance);

        _isOnLand = true;

        if (!Physics.Raycast(rayTopLeft, raycastDistance, ~_ignoreLayer) ||
            !Physics.Raycast(rayTopRight, raycastDistance, ~_ignoreLayer) ||
            !Physics.Raycast(rayBottomLeft, raycastDistance, ~_ignoreLayer) ||
            !Physics.Raycast(rayBottomRight, raycastDistance, ~_ignoreLayer))
        {
            _isOnLand = false;
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

    //Done so pixel shader will set different id for buildings so their highliting will work properly
    private void SetID(byte id)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var item in renderers)
        {
            item.material.SetFloat("_ID", id);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other != null && other.gameObject != this)
        {
            _isTriggered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject != this)
        {
            _isTriggered = false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(_cubeObj != null)
        {
            Vector3 cornerPosition = _cubeObj.transform.position - new Vector3(_size.x / 2f, 0, _size.y / 2f) + new Vector3(0, 0.2f, 0);
            float raycastDistance = 0.5f;

            Gizmos.DrawRay(cornerPosition + new Vector3(0, 0, 0), Vector3.down * raycastDistance);
            Gizmos.DrawRay(cornerPosition + new Vector3(_size.x, 0, 0), Vector3.down * raycastDistance);
            Gizmos.DrawRay(cornerPosition + new Vector3(0, 0, _size.y), Vector3.down * raycastDistance);
            Gizmos.DrawRay(cornerPosition + new Vector3(_size.x, 0, _size.y), Vector3.down * raycastDistance);
        }
    }
#endif
}
