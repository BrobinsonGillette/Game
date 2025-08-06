using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class InputManager : MonoBehaviour
{

    [Header("~~InputManager Hand~~")]
    public InputActionProperty Movement;
    public InputActionProperty Pause;
    public InputActionProperty Interact;
    public InputActionProperty Select;
    public InputActionProperty Inventory;






    public void StartUp()
    {
        Movement.action.Enable();
        Pause.action.Enable();
        Interact.action.Enable();
        Select.action.Enable();
        Inventory.action.Enable();
        Select.action.Enable();
    }

    public void DeSetUp()
    {
        Movement.action.Disable();
        Pause.action.Disable();
        Interact.action.Disable();
        Select.action.Disable();
        Inventory.action.Disable();
        Select.action.Disable();

    }
}
