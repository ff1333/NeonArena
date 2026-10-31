using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

public sealed class MobileJoystickTouchArea : OnScreenControl
{
    [InputControl(layout = "Vector2")]
    [SerializeField] private string inputControlPath;

    private RectTransform canvasRect;
    private RectTransform background;
    private RectTransform handle;
    private Image backgroundImage;
    private Image handleImage;
    private float movementRange;
    private Vector2 pointerOrigin;
    private TouchControl activeTouch;
#if UNITY_EDITOR
    private bool mouseActive;
#endif
    private readonly List<RaycastResult> raycastResults =
        new List<RaycastResult>();

    protected override string controlPathInternal
    {
        get => inputControlPath;
        set => inputControlPath = value;
    }

    public void Initialize(
        RectTransform rootCanvas,
        RectTransform joystickBackground,
        RectTransform joystickHandle,
        Image joystickBackgroundImage,
        Image joystickHandleImage,
        float joystickMovementRange)
    {
        canvasRect = rootCanvas;
        background = joystickBackground;
        handle = joystickHandle;
        backgroundImage = joystickBackgroundImage;
        handleImage = joystickHandleImage;
        movementRange = Mathf.Max(1f, joystickMovementRange);
        controlPath = "<Gamepad>/leftStick";
        SetVisualsVisible(false);
    }

    private void Update()
    {
        if (Touchscreen.current != null)
        {
            UpdateTouchscreen();
            return;
        }

#if UNITY_EDITOR
        UpdateMouse();
#endif
    }

    private void UpdateTouchscreen()
    {
        if (activeTouch != null)
        {
            if (activeTouch.press.isPressed)
            {
                MoveInput(activeTouch.position.ReadValue());
            }
            else
            {
                EndInput();
                activeTouch = null;
            }

            return;
        }

        foreach (TouchControl touch in Touchscreen.current.touches)
        {
            if (!touch.press.wasPressedThisFrame)
            {
                continue;
            }

            Vector2 position = touch.position.ReadValue();
            if (BeginInput(position))
            {
                activeTouch = touch;
            }

            return;
        }
    }

#if UNITY_EDITOR
    private void UpdateMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (!mouseActive && mouse.leftButton.wasPressedThisFrame)
        {
            mouseActive = BeginInput(mouse.position.ReadValue());
        }

        if (!mouseActive)
        {
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            MoveInput(mouse.position.ReadValue());
        }
        else
        {
            EndInput();
            mouseActive = false;
        }
    }
#endif

    public void CancelInput()
    {
        EndInput();
        activeTouch = null;
#if UNITY_EDITOR
        mouseActive = false;
#endif
    }

    protected override void OnDisable()
    {
        CancelInput();
        base.OnDisable();
    }

    private bool BeginInput(Vector2 screenPosition)
    {
        if (IsOverInteractiveUi(screenPosition))
        {
            return false;
        }

        pointerOrigin = ToCanvasPosition(screenPosition);
        background.anchoredPosition = pointerOrigin;
        handle.anchoredPosition = pointerOrigin;
        SetVisualsVisible(true);
        SendValueToControl(Vector2.zero);
        return true;
    }

    private void MoveInput(Vector2 screenPosition)
    {
        Vector2 delta = Vector2.ClampMagnitude(
            ToCanvasPosition(screenPosition) - pointerOrigin,
            movementRange);
        handle.anchoredPosition = pointerOrigin + delta;
        SendValueToControl(delta / movementRange);
    }

    private void EndInput()
    {
        SendValueToControl(Vector2.zero);
        if (handle != null)
        {
            handle.anchoredPosition = pointerOrigin;
        }

        SetVisualsVisible(false);
    }

    private Vector2 ToCanvasPosition(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            null,
            out Vector2 localPosition);
        return localPosition;
    }

    private bool IsOverInteractiveUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            Selectable selectable = raycastResults[i].gameObject
                .GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsActive() &&
                selectable.IsInteractable())
            {
                return true;
            }
        }

        return false;
    }

    private void SetVisualsVisible(bool visible)
    {
        if (backgroundImage != null)
        {
            backgroundImage.enabled = visible;
        }

        if (handleImage != null)
        {
            handleImage.enabled = visible;
        }
    }
}
