using UnityEngine;
using System.Collections.Generic;

namespace Units_Selection
{
    public class DragUnitSelect : MonoBehaviour
    {
        [SerializeField] private new Camera camera;
        [SerializeField] private RectTransform boxVisual;

        private Rect _selectionBox;
        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private List<Transform> _unitsList;

        private void Start()
        {
            _startPosition = Vector2.zero; 
            _endPosition = Vector2.zero;
            DrawVisual();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _startPosition = Input.mousePosition;
                _selectionBox = new Rect();
            }

            if (Input.GetMouseButton(0))
            {
                _endPosition = Input.mousePosition;
                DrawVisual();
                DrawSelection();
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (boxVisual.sizeDelta is { x: >= 30, y: >= 30 })
                {
                    SelectUnits();
                }

                _startPosition = Vector2.zero;
                _endPosition = Vector2.zero;
                DrawVisual();
            }
        }

        private void DrawVisual()
        {
            var boxStart = _startPosition;
            var boxEnd = _endPosition;

            var boxCenter = (boxStart + boxEnd) / 2;
            boxVisual.position = boxCenter;

            var boxSize = new Vector2(Mathf.Abs(boxStart.x - boxEnd.x), Mathf.Abs(boxStart.y - boxEnd.y));

            boxVisual.sizeDelta = boxSize;
        }

        private void DrawSelection()
        {
            _selectionBox.xMin = Mathf.Min(Input.mousePosition.x, _startPosition.x);
            _selectionBox.xMax = Mathf.Max(Input.mousePosition.x, _startPosition.x);

            _selectionBox.yMin = Mathf.Min(Input.mousePosition.y, _startPosition.y);
            _selectionBox.yMax = Mathf.Max(Input.mousePosition.y, _startPosition.y);  
        }
        private void SelectUnits()
        {
            _unitsList = new List<Transform>();
            foreach (var t in UnitSelections.Instance.UnitList)
            {
                if (_selectionBox.Contains(camera.WorldToScreenPoint(t.position)))
                    _unitsList.Add(t);
            }
            UnitSelections.Instance.DragSelect(_unitsList.ToArray());
        }
    }
}
    

