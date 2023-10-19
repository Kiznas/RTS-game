using UnityEngine;
using System.Collections;
using Unity.Mathematics;

namespace Units_Selection
{
    public class Unit : MonoBehaviour
    {
        [SerializeField] private LayerMask layerMask;

        public void Start(){
            UnitSelections.AddElement(UnitSelections.Instance.UnitList, transform);
        }

        private void OnDestroy(){
            UnitSelections.RemoveElement(UnitSelections.Instance.UnitList, transform);
        }

        private void LateUpdate()
        {
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
            }
        }

        private void OnDrawGizmos()
        {
            var pos = transform.position;
            var scale = transform.localScale;
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawCube(pos, scale);
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawSphere(pos, scale.x /2.5f);
        }

        // [ContextMenu("StartCheck")]
        // private void StartCheck()
        // {
        //     StartCoroutine(CheckForCollision());
        // }
        // private IEnumerator CheckForCollision()
        // {
        //     Collider[] hitColliders = new Collider[2];
        //     Physics.OverlapSphereNonAlloc(transform.position, transform.localScale.x / 2.5f, hitColliders, layerMask, QueryTriggerInteraction.UseGlobal);
        //     foreach (var col in hitColliders)
        //     {
        //         if (col != null && col.gameObject != gameObject && !col.gameObject.transform.hasChanged)
        //         {
        //             var pos = transform.position;
        //             Vector3 collisionCenter = col.transform.position;
        //             Vector3 moveDirection = collisionCenter - pos;
        //             MoveJobSystem.Instance.SetDestination(
        //                         new float3(pos.x + moveDirection.x * -1.5f, pos.y, pos.z + moveDirection.z * -1.5f), gameObject.transform);
        //         }
        //     }
        //     yield return new WaitForSeconds(2f);
        //     StartCoroutine(CheckForCollision());
        // }
    }
}
        