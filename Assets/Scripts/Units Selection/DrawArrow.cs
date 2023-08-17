using UnityEngine;

namespace Units_Selection
{
    public class DrawArrow : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GameObject directionArrow;

        private const int ROTATION_FIXER = 90;

        // ReSharper disable once InconsistentNaming
        private Vector3 _startPos{
            get => directionArrow.transform.position;
            set => directionArrow.transform.position = value;
        }

        private Vector3 _endPoint;
        private float _angle;
        private bool _isPlacingFormation;

        private void Update()
        {
            HandleMouseInput();
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(1))
            {
                StartPlacingArrow();
            }

            if (_isPlacingFormation)
            {
                UpdateArrowRotation();
            }

            if (Input.GetMouseButtonUp(1))
            {
                FinishPlacingArrow();
            }
        }

        private void StartPlacingArrow()
        {
            _startPos = GetMouseWorldPosition();
            directionArrow.SetActive(true);
            _isPlacingFormation = true;
        }

        private void UpdateArrowRotation()
        {
            _endPoint = GetMouseWorldPosition();
            var angle = CalculateAngle(_startPos, _endPoint);
            directionArrow.transform.eulerAngles = new Vector3(ROTATION_FIXER, -angle + ROTATION_FIXER * 2, 0);
        }

        private void FinishPlacingArrow()
        {
            _isPlacingFormation = false;
            directionArrow.SetActive(false);
            _angle = CalculateAngle(_startPos, _endPoint);
            EventAggregator.Post(this, new SendAngle { Angle = (int)_angle, StartPos = _startPos });
        }

        private Vector3 GetMouseWorldPosition()
        {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out var hit) ? hit.point : Vector3.zero;
        }

        private float CalculateAngle(Vector3 startPoint, Vector3 endPoint)
        {
            var direction = endPoint - startPoint;
            var angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
            return (angle + 360) % 360;
        }
    }
}
