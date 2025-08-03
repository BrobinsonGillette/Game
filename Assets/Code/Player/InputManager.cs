using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class InputManager 
{

    [Header("~~InputManager Hand~~")]
    public InputActionProperty Movement;
    public InputActionProperty Attack;
    public InputActionProperty Pause;
    public InputActionProperty Interact;
    public InputActionProperty Select;
    public InputActionProperty Inventory;
    public InputActionProperty Sprint;
    public InputActionProperty loop;





    public void StartUp()
    {
        Movement.action.Enable();
        Attack.action.Enable();
        Pause.action.Enable();
        Interact.action.Enable();
        Sprint.action.Enable();
        Select.action.Enable();
        Inventory.action.Enable();
        Select.action.Enable();
        loop.action.Enable();
    }

    public void DeSetUp()
    {
        Movement.action.Disable();
        Attack.action.Disable();
        Pause.action.Disable();
        Interact.action.Disable();
        Sprint.action.Disable();
        Select.action.Disable();
        Inventory.action.Disable();
        Select.action.Disable();
        loop.action.Disable();
    }
}
