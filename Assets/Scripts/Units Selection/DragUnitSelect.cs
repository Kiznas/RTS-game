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

        private void Start()
        {
            _startPosition = Vector2.zero; 
            _endPosition = Vector2.zero;
            DrawVisual();
        }

        private void Update()
        {
            if(Input.GetMouseButtonDown(0))
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

            if(Input.GetMouseButtonUp(0))
            {
                SelectUnits();
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
            if(Input.mousePosition.x < _startPosition.x)
            {
                _selectionBox.xMin = Input.mousePosition.x;
                _selectionBox.xMax = _startPosition.x;
            }
            else
            {
                _selectionBox.xMin = _startPosition.x;
                _selectionBox.xMax = Input.mousePosition.x;
            }

            if (Input.mousePosition.y < _startPosition.y)
            {
                _selectionBox.yMin = Input.mousePosition.y;
                _selectionBox.yMax = _startPosition.y;
            }
            else
            {
                _selectionBox.yMin = _startPosition.y;
                _selectionBox.yMax = Input.mousePosition.y;
            }
        }
        private void SelectUnits()
        {
            //Refactor this...
            var unitList = new List<Unit>();
            foreach (var item in UnitSelections.Instance.unitList)
            {
                if (_selectionBox.Contains(camera.WorldToScreenPoint(item.transform.position)))
                {
                    unitList.Add(item);
                }
            }
            UnitSelections.Instance.DragSelect(unitList);
        }
    }
}

