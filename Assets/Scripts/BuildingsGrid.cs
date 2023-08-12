using UnityEngine;

public class BuildingsGrid : MonoBehaviour
{
    [SerializeField] private Vector2Int _gridSize;
    [SerializeField] private LayerMask _raycastIgnoreLayer;
    [SerializeField] private Camera _mainCamera;

    private Building[,] _grid;
    private Building _flyingBuilding;

    private void Awake()
    {
        _grid = new Building[_gridSize.x, _gridSize.y];
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
        if (_flyingBuilding != null)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~_raycastIgnoreLayer))
            {
                Vector3 worldPosition = hit.point;

                float roundedX = Mathf.Round(worldPosition.x / 0.1f) * 0.1f;
                float roundedY = Mathf.Round(worldPosition.z / 0.1f) * 0.1f;

                int x = Mathf.RoundToInt(roundedX);
                int y = Mathf.RoundToInt(roundedY);

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
        }
    }
    private void PlaceFlyingBuilding(int placeX, int placeY)
    {
        for (int x = 0; x < _flyingBuilding.Size.x; x++)
        {
            for (int y = 0; y < _flyingBuilding.Size.y; y++)
            {
                _grid[placeX + x, placeY + y] = _flyingBuilding;
            }
        }

        _flyingBuilding.SetNormal();
        _flyingBuilding = null;
    }
}
