using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Units_Selection
{
    public class Unit : MonoBehaviour
    {
        [SerializeField] private LayerMask layerMask;

        public void Start(){
            UnitSelections.Instance.AddElement(UnitSelections.Instance.UnitList, transform);
        }

        private void OnDestroy(){
            UnitSelections.Instance.RemoveElement( UnitSelections.Instance.UnitList, transform);
        }

        private void OnDrawGizmos()
        {
            var pos = transform.position;
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawCube(pos, transform.localScale);
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawSphere(pos, 1);
        }

        [ContextMenu("StartCheck")]
        private void StartCheck()
        {
            StartCoroutine(CheckForCollision());
        }
        private IEnumerator CheckForCollision()
        {
            Collider[] hitColliders = new Collider[2];
            Physics.OverlapSphereNonAlloc(transform.position, 1, hitColliders, layerMask, QueryTriggerInteraction.UseGlobal);
            foreach (var col in hitColliders)
            {
                if (col != null && col.gameObject != gameObject)
                {
                    Debug.Log("Colliding");
                    var pos = transform.position;
                    MoveJobSystem.Instance.SetDestination(
                                new float3(pos.x + Random.Range(-3,4), pos.y, pos.z + Random.Range(-3,4)), gameObject.transform);
                }
            }
            yield return new WaitForSeconds(2f);
            StartCoroutine(CheckForCollision());
        }
    }
}
        