using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private bool useCornerMovement;
    [SerializeField] private new Camera camera;

    [SerializeField]
    private float maxSpeed = 5f;
    private float _speed;
    
    [SerializeField]
    private float acceleration = 10f;
    [SerializeField]
    private float damping = 15f;

    [SerializeField]
    private float stepSize = 2f;
    [SerializeField]
    private float zoomDampening = 7.5f;
    [SerializeField]
    private float minFOV = 5f;
    [SerializeField]
    private float maxFOV = 50f;
    [SerializeField]
    private float zoomSpeed = 2f;

    [SerializeField]
    [Range(0f, 0.1f)]
    private float edgeTolerance = 0.05f;

    private InputSettings _cameraActions;
    private Transform _cameraTransform;
    
    private Vector3 _targetPosition;
    private float _zoomHeight;

    //used to track and maintain velocity w/o a rigidbody
    private Vector3 _horizontalVelocity;
    private Vector3 _lastPosition;

    //tracks where the dragging action started
    private Vector3 _startDrag;

    private void Awake()
    {
        _cameraActions = new InputSettings();
        _cameraTransform = camera.transform;
    }

    private void OnEnable()
    {
        _zoomHeight = _cameraTransform.localPosition.y;
        _cameraTransform.LookAt(transform);

        _lastPosition = transform.position;

        _cameraActions.CameraMovement.Zoom.performed += ZoomCamera;
        _cameraActions.CameraMovement.Enable();
    }

    private void OnDisable()
    {
        _cameraActions.CameraMovement.Zoom.performed -= ZoomCamera;
        _cameraActions.CameraMovement.Disable();
    }

    private void Update()
    {
        if (useCornerMovement) { CheckMouseAtScreenEdge(); }
        DragCamera();
        //move base and camera objects
        UpdateVelocity();
        UpdateBasePosition();
        UpdateCameraPosition();
    }

    private void UpdateVelocity()
    {
        var position = transform.position;
        _horizontalVelocity = (position - _lastPosition) / Time.deltaTime;
        _horizontalVelocity.y = 0f;
        _lastPosition = position;
    }

    private void DragCamera()
    {
        if (!Mouse.current.rightButton.isPressed)
            return;

        //create plane to raycast to
        var plane = new Plane(Vector3.up, Vector3.zero);
        var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (plane.Raycast(ray, out var distance))
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
                _startDrag = ray.GetPoint(distance);
            else
                _targetPosition += _startDrag - ray.GetPoint(distance);
        }
    }

    private void CheckMouseAtScreenEdge()
    {
        //mouse position is in pixels
        var mousePosition = Mouse.current.position.ReadValue();
        var moveDirection = Vector3.zero;

        //horizontal scrolling
        if (mousePosition.x < edgeTolerance * Screen.width)
            moveDirection += -GetCameraRight();
        else if (mousePosition.x > (1f - edgeTolerance) * Screen.width)
            moveDirection += GetCameraRight();

        //vertical scrolling
        if (mousePosition.y < edgeTolerance * Screen.height)
            moveDirection += -GetCameraForward();
        else if (mousePosition.y > (1f - edgeTolerance) * Screen.height)
            moveDirection += GetCameraForward();

        _targetPosition += moveDirection;
    }

    private void UpdateBasePosition()
    {
        if (_targetPosition.sqrMagnitude > 0.1f)
        {
            //create a ramp up or acceleration
            _speed = Mathf.Lerp(_speed, maxSpeed, Time.deltaTime * acceleration);
            transform.position += _speed * Time.deltaTime * _targetPosition;
        }
        else
        {
            //create smooth slow down
            _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, Vector3.zero, Time.deltaTime * damping);
            transform.position += _horizontalVelocity * Time.deltaTime;
        }

        //reset for next frame
        _targetPosition = Vector3.zero;
    }

    private void ZoomCamera(InputAction.CallbackContext obj)
    {
        var inputValue = -obj.ReadValue<Vector2>().y / 100f;

        if (!(Mathf.Abs(inputValue) > 0.1f)) return;
        var newFOV = camera.orthographicSize + inputValue * stepSize;

        if (newFOV < minFOV)
            newFOV = minFOV;
        else if (newFOV > maxFOV)
            newFOV = maxFOV;

        camera.orthographicSize = newFOV;
    }

    private void UpdateCameraPosition()
    {
        //set zoom target
        var localPosition = _cameraTransform.localPosition;
        var zoomTarget = new Vector3(localPosition.x, _zoomHeight, localPosition.z);
        //add vector for forward/backward zoom
        zoomTarget -= zoomSpeed * (_zoomHeight - localPosition.y) * Vector3.forward;

        localPosition = Vector3.Lerp(localPosition, zoomTarget, Time.deltaTime * zoomDampening);
        _cameraTransform.localPosition = localPosition;
        _cameraTransform.LookAt(transform);
    }

    private Vector3 GetCameraForward()
    {
        var forward = _cameraTransform.forward;
        forward.y = 0f;
        return forward;
    }

    //gets the horizontal right vector of the camera
    private Vector3 GetCameraRight()
    {
        var right = _cameraTransform.right;
        right.y = 0f;
        return right;
    }
}