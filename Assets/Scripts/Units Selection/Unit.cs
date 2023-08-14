
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Unit : MonoBehaviour
{
    private NavMeshAgent _agent;
    [FormerlySerializedAs("IsSelected")] public bool isSelected;
    void Start()
    {
        UnitSelections.Instance.unitlist.Add(this);
        _agent = GetComponent<NavMeshAgent>();
    }

    void OnDestroy()
    {
        UnitSelections.Instance.unitlist.Remove(this);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) && isSelected) // Left mouse button clicked
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
        _agent.SetDestination(destination);
    }
}
