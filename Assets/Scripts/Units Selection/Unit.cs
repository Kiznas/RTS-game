using UnityEngine;
using UnityEngine.AI;

namespace Units_Selection
{
    public class Unit : MonoBehaviour{
        
        public NavMeshAgent agent;
        private Transform _childObj;
        public bool isSelected;
        
        public void Start(){
            UnitSelections.Instance.unitList.Add(this);
            agent = GetComponent<NavMeshAgent>();
            _childObj = gameObject.transform.GetChild(0);
        }

        private void OnDestroy(){
            UnitSelections.Instance.unitList.Remove(this);
        }

        public void UnitSelected(bool selected){
            _childObj.gameObject.SetActive(selected);
            isSelected = selected;
        }
    }
}
        