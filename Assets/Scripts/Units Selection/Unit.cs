using UnityEngine;

namespace Units_Selection
{
    public class Unit : MonoBehaviour{
        private Transform _childObj;

        public void Start(){
            UnitSelections.Instance.unitList.Add(transform);
        }

        private void OnDestroy(){
            UnitSelections.Instance.unitList.Remove(transform);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawCube(transform.position, transform.localScale);
        }
    }
}
        