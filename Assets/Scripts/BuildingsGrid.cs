using UnityEngine;

public class BuildingsGrid : MonoBehaviour
{
    [SerializeField] private Vector2Int GridSize;
    [SerializeField] private LayerMask _raycastIgnoreLayer;

    private Building[,] grid;
    private Building flyingBuilding;
    private Camera mainCamera;

    private void Awake()
    {
        grid = new Building[GridSize.x, GridSize.y];
        mainCamera = Camera.main;
    }

    public void StartPlacingBuilding(Building buildingPrefab)
    {
        if (flyingBuilding != null)
        {
            Destroy(flyingBuilding.gameObject);
        }

        flyingBuilding = Instantiate(buildingPrefab);
    }

    private void Update()
    {
        if (flyingBuilding != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~_raycastIgnoreLayer))
            {
                Vector3 worldPosition = hit.point;

                float roundedX = Mathf.Round(worldPosition.x / 0.1f) * 0.1f;
                float roundedY = Mathf.Round(worldPosition.z / 0.1f) * 0.1f;

                int x = Mathf.RoundToInt(roundedX);
                int y = Mathf.RoundToInt(roundedY);

                if (hit.collider.CompareTag("Land"))
                {
                    flyingBuilding.transform.position = new Vector3(roundedX, hit.point.y, roundedY);
                }

                flyingBuilding.SetTransparent();

                if (flyingBuilding.IsAvailable && Input.GetMouseButtonDown(0))
                {
                    PlaceFlyingBuilding(x, y);
                }
            }
        }
    }
    private void PlaceFlyingBuilding(int placeX, int placeY)
    {
        for (int x = 0; x < flyingBuilding.Size.x; x++)
        {
            for (int y = 0; y < flyingBuilding.Size.y; y++)
            {
                grid[placeX + x, placeY + y] = flyingBuilding;
            }
        }

        flyingBuilding.SetNormal();
        flyingBuilding.objectCollider.isTrigger = false;
        flyingBuilding = null;
    }
}
