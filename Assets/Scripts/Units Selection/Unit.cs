using UnityEngine;

namespace Units_Selection
{
    public class Unit : MonoBehaviour{

        public void Start(){
            UnitSelections.Instance.AddElement(UnitSelections.Instance.UnitList, transform);
        }

        private void OnDestroy(){
            UnitSelections.Instance.RemoveElement( UnitSelections.Instance.UnitList, transform);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawCube(transform.position, transform.localScale);
        }
    }
}
        