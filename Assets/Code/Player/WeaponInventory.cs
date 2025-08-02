using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{

    [Header("Inventory Settings")]
    public int maxWeapons = 5;
    public List<WeponClass> weapons = new List<WeponClass>();
    public int currentWeaponIndex = 0;

    [Header("Input Settings")]
    public KeyCode nextWeaponKey = KeyCode.Q;
    public KeyCode previousWeaponKey = KeyCode.Tab;
    public KeyCode weapon1Key = KeyCode.Alpha1;
    public KeyCode weapon2Key = KeyCode.Alpha2;
    public KeyCode weapon3Key = KeyCode.Alpha3;
    public KeyCode weapon4Key = KeyCode.Alpha4;
    public KeyCode weapon5Key = KeyCode.Alpha5;

    [Header("Starting Weapon")]
    public WeponClass startingWeapon;

    // Events
    public event Action<WeponClass> OnWeaponChanged;
    public event Action<List<WeponClass>, int> OnInventoryChanged;

    // Properties
    public WeponClass CurrentWeapon => weapons.Count > 0 && currentWeaponIndex < weapons.Count ? weapons[currentWeaponIndex] : null;
    public bool IsInventoryFull => weapons.Count >= maxWeapons;
    public int WeaponCount => weapons.Count;

    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();

        // Add starting weapon if provided
        if (startingWeapon != null)
        {
            TryAddWeapon(startingWeapon);
        }

        // Set initial weapon
        if (weapons.Count > 0)
        {
            EquipWeapon(0);
        }
    }

    private void Update()
    {
        HandleWeaponSwitching();
    }

    private void HandleWeaponSwitching()
    {
        if (weapons.Count <= 1) return;

        // Scroll through weapons
        if (Input.GetKeyDown(nextWeaponKey))
        {
            SwitchToNextWeapon();
        }
        else if (Input.GetKeyDown(previousWeaponKey))
        {
            SwitchToPreviousWeapon();
        }

        // Direct weapon selection
        if (Input.GetKeyDown(weapon1Key)) TrySwitchToWeapon(0);
        else if (Input.GetKeyDown(weapon2Key)) TrySwitchToWeapon(1);
        else if (Input.GetKeyDown(weapon3Key)) TrySwitchToWeapon(2);
        else if (Input.GetKeyDown(weapon4Key)) TrySwitchToWeapon(3);
        else if (Input.GetKeyDown(weapon5Key)) TrySwitchToWeapon(4);

        // Mouse wheel support
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            SwitchToNextWeapon();
        }
        else if (scroll < 0f)
        {
            SwitchToPreviousWeapon();
        }
    }

    public bool TryAddWeapon(WeponClass weapon)
    {
        if (weapon == null) return false;

        // Check if weapon already exists
        if (weapons.Contains(weapon))
        {
            Debug.Log($"Weapon {weapon.name} already in inventory!");
            return false;
        }

        // Check if inventory is full
        if (IsInventoryFull)
        {
            Debug.Log("Weapon inventory is full!");
            return false;
        }

        // Add weapon
        weapons.Add(weapon);
        Debug.Log($"Added weapon: {weapon.name}. Total weapons: {weapons.Count}");

        // If this is the first weapon, equip it
        if (weapons.Count == 1)
        {
            EquipWeapon(0);
        }

        OnInventoryChanged?.Invoke(weapons, currentWeaponIndex);
        return true;
    }

    public bool TryRemoveWeapon(WeponClass weapon)
    {
        if (weapon == null || !weapons.Contains(weapon)) return false;

        int weaponIndex = weapons.IndexOf(weapon);
        weapons.RemoveAt(weaponIndex);

        // Adjust current weapon index if necessary
        if (currentWeaponIndex >= weapons.Count)
        {
            currentWeaponIndex = Mathf.Max(0, weapons.Count - 1);
        }

        // Equip new current weapon
        if (weapons.Count > 0)
        {
            EquipWeapon(currentWeaponIndex);
        }
        else
        {
            // No weapons left
            playerStats?.SetCurrentWeapon(null);
            OnWeaponChanged?.Invoke(null);
        }

        OnInventoryChanged?.Invoke(weapons, currentWeaponIndex);
        return true;
    }

    public void SwitchToNextWeapon()
    {
        if (weapons.Count <= 1) return;

        currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Count;
        EquipWeapon(currentWeaponIndex);
    }

    public void SwitchToPreviousWeapon()
    {
        if (weapons.Count <= 1) return;

        currentWeaponIndex = (currentWeaponIndex - 1 + weapons.Count) % weapons.Count;
        EquipWeapon(currentWeaponIndex);
    }

    public void TrySwitchToWeapon(int index)
    {
        if (index >= 0 && index < weapons.Count)
        {
            currentWeaponIndex = index;
            EquipWeapon(currentWeaponIndex);
        }
    }

    private void EquipWeapon(int index)
    {
        if (index < 0 || index >= weapons.Count) return;

        currentWeaponIndex = index;
        WeponClass newWeapon = weapons[currentWeaponIndex];

        // Update player stats
        if (playerStats != null)
        {
            playerStats.SetCurrentWeapon(newWeapon);
        }

        // Invoke events
        OnWeaponChanged?.Invoke(newWeapon);
        OnInventoryChanged?.Invoke(weapons, currentWeaponIndex);

        Debug.Log($"Equipped weapon: {newWeapon.name} ({currentWeaponIndex + 1}/{weapons.Count})");
    }

  


}
