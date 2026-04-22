using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BattleCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera controlledCamera;
    [SerializeField] private UnitActionMenuController actionMenu;

    [Header("WASD Pan")]
    [SerializeField] private float keyboardPanSpeed = 8f;
    [SerializeField] private bool allowGamepadLeftStickPan = false;

    [Header("Right Stick Pan")]
    [SerializeField] private float rightStickPanSpeed = 8f;
    [SerializeField] private float rightStickDeadzone = 0.15f;
    [SerializeField] private bool blockRightStickPanWhenActionMenuIsOpen = true;

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

        if (actionMenu == null)
            actionMenu = FindFirstObjectByType<UnitActionMenuController>();

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
        if (BattlePauseMenuController.IsPauseMenuOpen)
            return;

        HandleKeyboardPan();
        HandleRightDragPan();
    }

    private void OnPointerPositionChanged(InputAction.CallbackContext context)
    {
        ControllerInputModeTracker.NotifyMouseKeyboardInput();
        pointerPosition = context.ReadValue<Vector2>();
    }

    private void OnRightClickStarted(InputAction.CallbackContext context)
    {
        ControllerInputModeTracker.NotifyMouseKeyboardInput();

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
        Vector2 moveInput = GetKeyboardPanInput();

        if (allowGamepadLeftStickPan)
            moveInput += inputActions.PlayerInputActions.Move.ReadValue<Vector2>();

        Vector2 rightStickInput = GetRightStickPanInput();
        if (rightStickInput.sqrMagnitude >= rightStickDeadzone * rightStickDeadzone)
            moveInput += rightStickInput * (rightStickPanSpeed / Mathf.Max(0.01f, keyboardPanSpeed));

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

    private Vector2 GetKeyboardPanInput()
    {
        if (Keyboard.current == null)
            return Vector2.zero;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            ControllerInputModeTracker.NotifyMouseKeyboardInput();
            input.y += 1f;
        }

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            ControllerInputModeTracker.NotifyMouseKeyboardInput();
            input.y -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            ControllerInputModeTracker.NotifyMouseKeyboardInput();
            input.x += 1f;
        }

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            ControllerInputModeTracker.NotifyMouseKeyboardInput();
            input.x -= 1f;
        }

        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    private Vector2 GetRightStickPanInput()
    {
        if (blockRightStickPanWhenActionMenuIsOpen && actionMenu != null && actionMenu.IsVisible)
            return Vector2.zero;

        if (Gamepad.current != null)
        {
            Vector2 input = Gamepad.current.rightStick.ReadValue();
            if (input.sqrMagnitude >= rightStickDeadzone * rightStickDeadzone)
                ControllerInputModeTracker.NotifyControllerInput();

            return input;
        }

        return Vector2.zero;
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
