using UnityEngine;
using UnityEngine.InputSystem;

public class BuilderCameraController : MonoBehaviour
{
    private Vector3 initialRigPosition;
    private Quaternion initialPivotRotation;
    private float initialOrthographicSize;    
    
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera controlledCamera;

    [Header("Pan")]
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private bool useEdgePan = false;
    [SerializeField] private float edgePanSize = 15f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 0.02f;
    [SerializeField] private float minZoomDistance = 4f;
    [SerializeField] private float maxZoomDistance = 20f;

    [Header("Keyboard Rotation")]
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Right Click Orbit")]
    [SerializeField] private float mouseOrbitSpeed = 0.2f;
    [SerializeField] private LayerMask orbitAnchorMask = ~0;

    [Header("Pitch")]
    [SerializeField] private float fixedPitch = 45f;
    

    private bool isOrbiting;
    private Vector3 orbitAnchorPoint;
    
    [Header("Reset Defaults")]
    [SerializeField] private Vector3 defaultRigPosition = new Vector3(-8.22f, 12.82f, -8.48f);
    [SerializeField] private Vector3 defaultPivotRotation = new Vector3(35f, 45f, 0f);
    [SerializeField] private Vector3 defaultCameraLocalPosition = new Vector3(0f, 0f, -10f);

    private void Awake()
    {
        transform.position = defaultRigPosition;

        if (cameraPivot != null)
            cameraPivot.rotation = Quaternion.Euler(defaultPivotRotation);

        if (controlledCamera != null)
            controlledCamera.transform.localPosition = defaultCameraLocalPosition;
        
        initialRigPosition = transform.position;

        if (cameraPivot != null)
            initialPivotRotation = cameraPivot.rotation;

        if (controlledCamera != null && controlledCamera.orthographic)
            initialOrthographicSize = controlledCamera.orthographicSize;
    }

    private void Update()
    {
        HandlePan();
        HandleZoom();
        HandleKeyboardRotation();
        HandleRightClickOrbit();
    }

    private void HandlePan()
    {
        if (Keyboard.current == null || isOrbiting)
            return;

        Vector3 move = Vector3.zero;

        if (Keyboard.current.wKey.isPressed)
            move += GetFlatForward();

        if (Keyboard.current.sKey.isPressed)
            move -= GetFlatForward();

        if (Keyboard.current.dKey.isPressed)
            move += GetFlatRight();

        if (Keyboard.current.aKey.isPressed)
            move -= GetFlatRight();

        if (useEdgePan && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (mousePos.x <= edgePanSize)
                move -= GetFlatRight();

            if (mousePos.x >= Screen.width - edgePanSize)
                move += GetFlatRight();

            if (mousePos.y <= edgePanSize)
                move -= GetFlatForward();

            if (mousePos.y >= Screen.height - edgePanSize)
                move += GetFlatForward();
        }

        if (move.sqrMagnitude > 0.001f)
        {
            move.Normalize();
            transform.position += move * (panSpeed * Time.deltaTime);
        }
    }

    private void HandleZoom()
    {
        if (controlledCamera == null || Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        if (controlledCamera.orthographic)
        {
            float newSize = controlledCamera.orthographicSize - (scroll * 0.01f * zoomSpeed);
            controlledCamera.orthographicSize = Mathf.Clamp(newSize, minZoomDistance, maxZoomDistance);

            Debug.Log($"Ortho Zoom Size: {controlledCamera.orthographicSize}");
        }
        else
        {
            Vector3 localPos = controlledCamera.transform.localPosition;

            float currentDistance = Mathf.Abs(localPos.z);
            currentDistance -= scroll * 0.01f * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);

            localPos.z = -currentDistance;
            controlledCamera.transform.localPosition = localPos;

            Debug.Log($"Perspective Zoom Distance: {currentDistance}");
        }
    }

    private void HandleKeyboardRotation()
    {
        if (cameraPivot == null || Keyboard.current == null || isOrbiting)
            return;

        float yawInput = 0f;

        if (Keyboard.current.qKey.isPressed)
            yawInput -= 1f;

        if (Keyboard.current.eKey.isPressed)
            yawInput += 1f;

        if (Mathf.Abs(yawInput) > 0.01f)
        {
            Vector3 euler = cameraPivot.rotation.eulerAngles;
            euler.y += yawInput * rotationSpeed * Time.deltaTime;
            euler.x = fixedPitch;
            euler.z = 0f;

            cameraPivot.rotation = Quaternion.Euler(euler);
        }
    }

    private void HandleRightClickOrbit()
    {
        if (Mouse.current == null || controlledCamera == null || cameraPivot == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (TryGetOrbitAnchor(out Vector3 hitPoint))
            {
                orbitAnchorPoint = hitPoint;
                isOrbiting = true;
            }
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            isOrbiting = false;
        }

        if (!isOrbiting || !Mouse.current.rightButton.isPressed)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        if (delta.sqrMagnitude < 0.001f)
            return;

        float yawAmount = delta.x * mouseOrbitSpeed;

        transform.RotateAround(orbitAnchorPoint, Vector3.up, yawAmount);

        Vector3 euler = cameraPivot.rotation.eulerAngles;
        euler.x = fixedPitch;
        euler.z = 0f;
        cameraPivot.rotation = Quaternion.Euler(euler);
    }

    private bool TryGetOrbitAnchor(out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        if (controlledCamera == null || Mouse.current == null)
            return false;

        Ray ray = controlledCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, orbitAnchorMask))
        {
            hitPoint = hit.point;
            return true;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            hitPoint = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    private Vector3 GetFlatForward()
    {
        if (cameraPivot == null)
            return Vector3.forward;

        Vector3 forward = cameraPivot.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    private Vector3 GetFlatRight()
    {
        if (cameraPivot == null)
            return Vector3.right;

        Vector3 right = cameraPivot.right;
        right.y = 0f;
        return right.normalized;
    }

    public void ResetCameraToStart()
    {
        transform.position = initialRigPosition;

        if (cameraPivot != null)
            cameraPivot.rotation = initialPivotRotation;

        if (controlledCamera != null && controlledCamera.orthographic)
            controlledCamera.orthographicSize = initialOrthographicSize;

        isOrbiting = false;
    }
}