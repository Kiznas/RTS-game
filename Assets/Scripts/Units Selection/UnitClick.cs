using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class UnitClick : MonoBehaviour
{
    [FormerlySerializedAs("_camera")] [SerializeField] private Camera camera;

    [FormerlySerializedAs("_clickable")] [SerializeField] private LayerMask clickable;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickable))
            {

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    UnitSelections.Instance.ShiftClickSelect(hit.collider.GetComponent<Unit>());
                }
                else
                {
                    UnitSelections.Instance.ClickSelect(hit.collider.GetComponent<Unit>());
                }

            }
            else
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    UnitSelections.Instance.DeselectAll();
                }
            }
        }
    }
}
