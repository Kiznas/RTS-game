using UnityEngine;

namespace Units_Selection
{
    public class UnitClick : MonoBehaviour
    {
        [SerializeField] private new Camera camera;
        [SerializeField] private LayerMask clickable;

        public void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = camera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out var ratHit, Mathf.Infinity, clickable))
                {

                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        UnitSelections.Instance.ShiftClickSelect(ratHit.collider.GetComponent<Transform>());
                    }
                    else
                    {
                        UnitSelections.Instance.ClickSelect(ratHit.collider.GetComponent<Transform>());
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
}

