
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    private NavMeshAgent agent;
    public bool IsSelected;
    void Start()
    {
        UnitSelections.Instance.unitlist.Add(this);
        agent = GetComponent<NavMeshAgent>();
    }

    void OnDestroy()
    {
        UnitSelections.Instance.unitlist.Remove(this);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) && IsSelected) // Left mouse button clicked
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                MoveToDestination(hit.point);
            }
        }
    }

    private void MoveToDestination(Vector3 destination)
    {
        agent.SetDestination(destination);
    }
}
