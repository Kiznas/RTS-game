using UnityEngine;

public class BuildingsGrid : MonoBehaviour
{
    [SerializeField] private Vector2Int gridSize;
    [SerializeField] private LayerMask raycastIgnoreLayer;
    [SerializeField] private Camera mainCamera;

    private Building[,] _grid;
    private Building _flyingBuilding;

    private void Awake()
    {
        _grid = new Building[gridSize.x, gridSize.y];
    }

    public void StartPlacingBuilding(Building buildingPrefab)
    {
        if (_flyingBuilding != null)
        {
            Destroy(_flyingBuilding.gameObject);
        }

        _flyingBuilding = Instantiate(buildingPrefab);
    }

    private void Update()
    {
        if (_flyingBuilding == null) return;
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, ~raycastIgnoreLayer)) return;
        var worldPosition = hit.point;

        var roundedX = Mathf.Round(worldPosition.x / 0.1f) * 0.1f;
        var roundedY = Mathf.Round(worldPosition.z / 0.1f) * 0.1f;

        var x = Mathf.RoundToInt(roundedX);
        var y = Mathf.RoundToInt(roundedY);

        if (hit.collider.CompareTag("Land"))
        {
            _flyingBuilding.transform.position = new Vector3(roundedX, hit.point.y, roundedY);
        }

        _flyingBuilding.SetTransparent();

        //Rewrite on input system
        if (_flyingBuilding.IsAvailable && Input.GetMouseButtonDown(0))
        {
            PlaceFlyingBuilding(x, y);
        }
    }
    private void PlaceFlyingBuilding(int placeX, int placeY)
    {
        for (var x = 0; x < _flyingBuilding.Size.x; x++)
        {
            for (var y = 0; y < _flyingBuilding.Size.y; y++)
            {
                _grid[placeX + x, placeY + y] = _flyingBuilding;
            }
        }

        _flyingBuilding.SetNormal();
        _flyingBuilding = null;
    }
}
