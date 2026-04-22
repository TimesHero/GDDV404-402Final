using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BattleCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera controlledCamera;

    [Header("WASD Pan")]
    [SerializeField] private float keyboardPanSpeed = 8f;

    [Header("Right Click Drag Pan")]
    [SerializeField] private float dragPanSensitivity = 1f;
    [SerializeField] private float dragPlaneY = 0f;
    [SerializeField] private bool blockDragWhenPointerIsOverUi = false;

    private InputSystem_Actions inputActions;
    private Vector2 pointerPosition;
    private bool isRightDragPanning;
    private Vector3 dragAnchorWorldPoint;

    private void Awake()
    {
        if (controlledCamera == null)
            controlledCamera = GetComponent<Camera>();

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();
        inputActions.PlayerInputActions.Enable();

        inputActions.Gameplay.PointerPosition.performed += OnPointerPositionChanged;
        inputActions.Gameplay.PaintClick.started += OnRightClickStarted;
        inputActions.Gameplay.PaintClick.canceled += OnRightClickCanceled;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.PointerPosition.performed -= OnPointerPositionChanged;
        inputActions.Gameplay.PaintClick.started -= OnRightClickStarted;
        inputActions.Gameplay.PaintClick.canceled -= OnRightClickCanceled;

        inputActions.PlayerInputActions.Disable();
        inputActions.Gameplay.Disable();

        isRightDragPanning = false;
    }

    private void Update()
    {
        HandleKeyboardPan();
        HandleRightDragPan();
    }

    private void OnPointerPositionChanged(InputAction.CallbackContext context)
    {
        pointerPosition = context.ReadValue<Vector2>();
    }

    private void OnRightClickStarted(InputAction.CallbackContext context)
    {
        if (blockDragWhenPointerIsOverUi && IsPointerOverUi())
            return;

        if (!TryGetPointerWorldPoint(out dragAnchorWorldPoint))
            return;

        isRightDragPanning = true;
    }

    private void OnRightClickCanceled(InputAction.CallbackContext context)
    {
        isRightDragPanning = false;
    }

    private void HandleKeyboardPan()
    {
        Vector2 moveInput = inputActions.PlayerInputActions.Move.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude < 0.001f)
            return;

        Vector3 moveDirection =
            GetFlatCameraForward() * moveInput.y +
            GetFlatCameraRight() * moveInput.x;

        if (moveDirection.sqrMagnitude < 0.001f)
            return;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        transform.position += moveDirection * (keyboardPanSpeed * Time.deltaTime);
    }

    private void HandleRightDragPan()
    {
        if (!isRightDragPanning)
            return;

        if (!inputActions.Gameplay.PaintClick.IsPressed())
        {
            isRightDragPanning = false;
            return;
        }

        if (!TryGetPointerWorldPoint(out Vector3 currentWorldPoint))
            return;

        Vector3 cameraDelta = (dragAnchorWorldPoint - currentWorldPoint) * dragPanSensitivity;
        transform.position += cameraDelta;
    }

    private bool TryGetPointerWorldPoint(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;

        if (controlledCamera == null)
            return false;

        Ray ray = controlledCamera.ScreenPointToRay(pointerPosition);
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, dragPlaneY, 0f));

        if (!dragPlane.Raycast(ray, out float enter))
            return false;

        worldPoint = ray.GetPoint(enter);
        return true;
    }

    private Vector3 GetFlatCameraForward()
    {
        if (controlledCamera == null)
            return Vector3.forward;

        Vector3 forward = controlledCamera.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return Vector3.forward;

        return forward.normalized;
    }

    private Vector3 GetFlatCameraRight()
    {
        if (controlledCamera == null)
            return Vector3.right;

        Vector3 right = controlledCamera.transform.right;
        right.y = 0f;

        if (right.sqrMagnitude < 0.001f)
            return Vector3.right;

        return right.normalized;
    }

    private bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }
}
