using UnityEngine;
using System.Collections.Generic;

namespace Units_Selection {
    public class UnitsMaster : MonoBehaviour
    {
        private Camera _camera;
        private List<Unit> _selectedUnits = new();
        public void Start(){
            _camera = Camera.main;
        }

        private void Update()
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Input.GetMouseButtonDown(1)) 
            {
                if (Physics.Raycast(ray, out var hit))
                {
                    _selectedUnits = UnitSelections.Instance.unitSelectedList;
                    MoveToDestination(hit.point);
                }
                else
                {
                    UnitSelections.Instance.DeselectAll();
                }
            }
        }
        
        private void MoveToDestination(Vector3 destination){
            foreach (var unit in _selectedUnits){
                unit.agent.SetDestination(destination);
            }
        }
    } 
}

