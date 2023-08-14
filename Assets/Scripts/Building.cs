using UnityEngine;
// ReSharper disable PossibleLossOfFraction

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class Building : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private LayerMask ignoreLayer;
    [SerializeField] private bool isAvailable = true;
    [SerializeField] private Vector2Int size = Vector2Int.one;
    [SerializeField] private BoxCollider objectCollider;
    public bool IsAvailable => isAvailable;
    public Vector2Int Size => size;

    private bool _isOnLand = true;
    private bool _isTriggered;
    private GameObject _cubeObj;
    private Renderer[] _renderers;
    private const string ID = "_ID";

    private const float RAYCAST_DISTANCE = 0.5f;


    private void Start()
    {
        SetID((byte)Random.Range(0, 255));
        _renderers = GetComponentsInChildren<Renderer>();
    }

    private void Reset()
    {
        objectCollider = GetComponent<BoxCollider>();
    }

    public void SetTransparent()
    {
        RecolorMaterial(isAvailable ? Color.green : Color.red);
        CreateCube();
    }

    public void SetNormal()
    {
        Destroy(_cubeObj);
        RecolorMaterial(Color.white);
        objectCollider.isTrigger = false;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void CreateCube()
    {
        for (var x = 0; x < Size.x; x++)
        {
            for (var y = 0; y < Size.y; y++)
            {
                if (_cubeObj == null)
                {
                    var transform1 = transform;
                    var cubePosition = transform1.position + new Vector3(x, 0, y);
                    _cubeObj = Instantiate(cubePrefab, cubePosition, Quaternion.identity, transform1);
                }

                _cubeObj.GetComponent<Renderer>().material.color = IsAvailable ? new Color(0, 255, 0, 0.6f) : new Color(255, 0, 0, 0.6f);
                _cubeObj.transform.localScale = new Vector3(Size.x, 0.05f, Size.y);
                _cubeObj.transform.position = transform.position + new Vector3(x / 2, 0, y / 2);
            }
        }

        CheckIfOnLand();

        isAvailable = _isOnLand && !_isTriggered;
    }

    private void CheckIfOnLand()
    {
        var cornerPosition = _cubeObj.transform.position - new Vector3(Size.x / 2f, 0, Size.y / 2f) + new Vector3(0, 0.2f, 0);

        Ray rayTopLeft = new(cornerPosition + new Vector3(0, 0, 0), Vector3.down * RAYCAST_DISTANCE);
        Ray rayTopRight = new(cornerPosition + new Vector3(Size.x, 0, 0), Vector3.down * RAYCAST_DISTANCE);
        Ray rayBottomLeft = new(cornerPosition + new Vector3(0, 0, Size.y), Vector3.down * RAYCAST_DISTANCE);
        Ray rayBottomRight = new(cornerPosition + new Vector3(Size.x, 0, Size.y), Vector3.down * RAYCAST_DISTANCE);

        _isOnLand = true;

        if (!Physics.Raycast(rayTopLeft, RAYCAST_DISTANCE, ~ignoreLayer) ||
            !Physics.Raycast(rayTopRight, RAYCAST_DISTANCE, ~ignoreLayer) ||
            !Physics.Raycast(rayBottomLeft, RAYCAST_DISTANCE, ~ignoreLayer) ||
            !Physics.Raycast(rayBottomRight, RAYCAST_DISTANCE, ~ignoreLayer))
        {
            _isOnLand = false;
        }
    }

    private void RecolorMaterial(Color color)
    {
        foreach (var item in _renderers)
        {
            item.material.color = color;
        }
    }

    //Done so pixel shader will set different id for buildings so their outlines will work properly
    private void SetID(byte id)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var item in renderers)
        {
            // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
            item.material.SetFloat(ID, id);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other != null && other != objectCollider)
        {
            _isTriggered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null && other != objectCollider)
        {
            _isTriggered = false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_cubeObj == null) return;
        var cornerPosition = _cubeObj.transform.position - new Vector3(size.x / 2f, 0, size.y / 2f) + new Vector3(0, 0.2f, 0);

        Gizmos.DrawRay(cornerPosition + new Vector3(0, 0, 0), Vector3.down * RAYCAST_DISTANCE);
        Gizmos.DrawRay(cornerPosition + new Vector3(size.x, 0, 0), Vector3.down * RAYCAST_DISTANCE);
        Gizmos.DrawRay(cornerPosition + new Vector3(0, 0, size.y), Vector3.down * RAYCAST_DISTANCE);
        Gizmos.DrawRay(cornerPosition + new Vector3(size.x, 0, size.y), Vector3.down * RAYCAST_DISTANCE);
    }
#endif
}
