using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class InputManager : MonoBehaviour
{
    public static InputManager instance { get; private set; }
    public bool IsFreeCamMode { get; private set; }
    public Vector2 MovementInput { get; private set; }
    public float ZoomInput { get; private set; }

    [Header("~~InputManager Hand~~")]
    public InputActionProperty Movement;
    public InputActionProperty Zoom;
    public InputActionProperty Pause;
    public InputActionProperty Interact;
    public InputActionProperty Select;
    public InputActionProperty Inventory;
    public InputActionProperty FreeCam;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        StartUp();
        KeyBindSetUP();
    }
    public void StartUp()
    {
        Zoom.action.Enable();
        Movement.action.Enable();
        Pause.action.Enable();
        Interact.action.Enable();
        Select.action.Enable();
        Inventory.action.Enable();
        Select.action.Enable();
        FreeCam.action.Enable();
    }
    public void KeyBindSetUP()
    {
        Movement.action.performed += ctx => MovementInput = ctx.ReadValue<Vector2>();
        Movement.action.canceled += ctx => MovementInput = Vector2.zero;
        Zoom.action.performed += ctx => ZoomInput = ctx.ReadValue<float>();
        Zoom.action.canceled += ctx => ZoomInput = 0f;
        FreeCam.action.performed += ctx => IsFreeCamMode = !IsFreeCamMode;
    }
    public void DeSetUp()
    {
        Zoom.action.Disable();
        Movement.action.Disable();
        Pause.action.Disable();
        Interact.action.Disable();
        Select.action.Disable();
        Inventory.action.Disable();
        Select.action.Disable();
        FreeCam.action.Disable();
    }
}
