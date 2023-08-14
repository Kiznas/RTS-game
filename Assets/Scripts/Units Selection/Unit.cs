using UnityEngine;
using UnityEngine.AI;

namespace Units_Selection
{
    public class Unit : MonoBehaviour
    {
        private NavMeshAgent _agent;
        public bool isSelected;
        private Camera _camera;

        public void Start()
        {
            _camera = Camera.main;
            UnitSelections.Instance.unitList.Add(this);
            _agent = GetComponent<NavMeshAgent>();
        }

        void OnDestroy()
        {
            UnitSelections.Instance.unitList.Remove(this);
        }

        private void Update()
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Input.GetMouseButtonDown(1) && isSelected) // Left mouse button clicked
            {
                if (Physics.Raycast(ray, out var hit))
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
}
        