using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SlotMachineSetup : MonoBehaviour
{
    [Header("Setup Configuration")]
    public SlotMachine slotMachine;
    public bool autoSetupOnStart = true;

    [Header("Weapon Configuration")]
    public List<WeponClass> trashWeapons = new List<WeponClass>();
    public List<WeponClass> commonWeapons = new List<WeponClass>();
    public List<WeponClass> uncommonWeapons = new List<WeponClass>();
    public List<WeponClass> rareWeapons = new List<WeponClass>();
    public List<WeponClass> epicWeapons = new List<WeponClass>();
    public List<WeponClass> legendaryWeapons = new List<WeponClass>();
    public List<WeponClass> mythicWeapons = new List<WeponClass>();
    public List<WeponClass> exoticWeapons = new List<WeponClass>();
    public List<WeponClass> ultimateWeapons = new List<WeponClass>();
    public List<WeponClass> superUltimateWeapons = new List<WeponClass>();
    public List<WeponClass> godlikeWeapons = new List<WeponClass>();

    [Header("Default Prize Icons (Optional)")]
    public Sprite weaponIcon;
    public Sprite expIcon;
    public Sprite healthIcon;


    private void Start()
    {
        if (autoSetupOnStart && slotMachine != null)
        {
            SetupSlotMachine();
        }
    }

    [ContextMenu("Setup Slot Machine")]
    public void SetupSlotMachine()
    {
        if (slotMachine == null)
        {
            Debug.LogError("SlotMachine reference not assigned!");
            return;
        }

        Debug.Log("Setting up Slot Machine...");

        // Clear existing prizes
        slotMachine.weaponPrizes.Clear();
        slotMachine.expPrizes.Clear();
        slotMachine.healthPrizes.Clear();

        // Setup weapon prizes
        SetupWeaponPrizes();

        // Setup EXP prizes
        SetupExpPrizes();

        // Setup health prizes
        SetupHealthPrizes();

        Debug.Log($"Slot Machine setup complete! Total prizes: {slotMachine.weaponPrizes.Count + slotMachine.expPrizes.Count + slotMachine.healthPrizes.Count}");
    }

    private void SetupWeaponPrizes()
    {
        AddWeaponPrizesForType(PrizeType.Trash, trashWeapons);
        AddWeaponPrizesForType(PrizeType.Common, commonWeapons);
        AddWeaponPrizesForType(PrizeType.Uncommon, uncommonWeapons);
        AddWeaponPrizesForType(PrizeType.Rare, rareWeapons);
        AddWeaponPrizesForType(PrizeType.Epic, epicWeapons);
        AddWeaponPrizesForType(PrizeType.Legendary, legendaryWeapons);
        AddWeaponPrizesForType(PrizeType.Mythic, mythicWeapons);
        AddWeaponPrizesForType(PrizeType.Exotic, exoticWeapons);
        AddWeaponPrizesForType(PrizeType.Ultimate, ultimateWeapons);
        AddWeaponPrizesForType(PrizeType.SuperUltimate, superUltimateWeapons);
        AddWeaponPrizesForType(PrizeType.Godlike, godlikeWeapons);
    }

    private void AddWeaponPrizesForType(PrizeType prizeType, List<WeponClass> weapons)
    {
        foreach (WeponClass weapon in weapons)
        {
            if (weapon != null)
            {
                SlotPrize weaponPrize = new SlotPrize
                {
                    prizeType = prizeType,
                    weapon = weapon,
                    prizeName = weapon.name,
                    prizeIcon = weaponIcon, // You can assign specific icons per weapon if available
                    prizeColor = GetPrizeTypeColor(prizeType)
                };
                slotMachine.weaponPrizes.Add(weaponPrize);
            }
        }
    }

    private void SetupExpPrizes()
    {
        // Create EXP prizes for each rarity tier
        CreateExpPrize(PrizeType.Trash, 5, "Small EXP Boost");
        CreateExpPrize(PrizeType.Common, 15, "EXP Boost");
        CreateExpPrize(PrizeType.Uncommon, 30, "Good EXP Boost");
        CreateExpPrize(PrizeType.Rare, 60, "Great EXP Boost");
        CreateExpPrize(PrizeType.Epic, 120, "Epic EXP Boost");
        CreateExpPrize(PrizeType.Legendary, 250, "Legendary EXP Boost");
        CreateExpPrize(PrizeType.Mythic, 500, "Mythic EXP Boost");
        CreateExpPrize(PrizeType.Exotic, 1000, "Exotic EXP Boost");
        CreateExpPrize(PrizeType.Ultimate, 2000, "Ultimate EXP Boost");
        CreateExpPrize(PrizeType.SuperUltimate, 5000, "Super Ultimate EXP Boost");
        CreateExpPrize(PrizeType.Godlike, 10000, "Godlike EXP Boost");
    }

    private void CreateExpPrize(PrizeType prizeType, int expAmount, string prizeName)
    {
        SlotPrize expPrize = new SlotPrize
        {
            prizeType = prizeType,
            expAmount = expAmount,
            prizeName = prizeName,
            prizeIcon = expIcon,
            prizeColor = GetPrizeTypeColor(prizeType)
        };
        slotMachine.expPrizes.Add(expPrize);
    }

    private void SetupHealthPrizes()
    {
        // Create health prizes for each rarity tier
        CreateHealthPrize(PrizeType.Trash, 10, "Small Health Potion");
        CreateHealthPrize(PrizeType.Common, 25, "Health Potion");
        CreateHealthPrize(PrizeType.Uncommon, 50, "Good Health Potion");
        CreateHealthPrize(PrizeType.Rare, 100, "Great Health Potion");
        CreateHealthPrize(PrizeType.Epic, 200, "Epic Health Potion");
        CreateHealthPrize(PrizeType.Legendary, 400, "Legendary Health Potion");
        CreateHealthPrize(PrizeType.Mythic, 750, "Mythic Health Potion");
        CreateHealthPrize(PrizeType.Exotic, 1500, "Exotic Health Potion");
        CreateHealthPrize(PrizeType.Ultimate, 3000, "Ultimate Health Potion");
        CreateHealthPrize(PrizeType.SuperUltimate, 6000, "Super Ultimate Health Potion");
        CreateHealthPrize(PrizeType.Godlike, 10000, "Godlike Health Potion");
    }

    private void CreateHealthPrize(PrizeType prizeType, int healthAmount, string prizeName)
    {
        SlotPrize healthPrize = new SlotPrize
        {
            prizeType = prizeType,
            healthAmount = healthAmount,
            prizeName = prizeName,
            prizeIcon = healthIcon,
            prizeColor = GetPrizeTypeColor(prizeType)
        };
        slotMachine.healthPrizes.Add(healthPrize);
    }

    private Color GetPrizeTypeColor(PrizeType prizeType)
    {
        switch (prizeType)
        {
            case PrizeType.Trash: return Color.gray;
            case PrizeType.Common: return Color.white;
            case PrizeType.Uncommon: return Color.green;
            case PrizeType.Rare: return Color.blue;
            case PrizeType.Epic: return Color.magenta;
            case PrizeType.Legendary: return Color.yellow;
            case PrizeType.Mythic: return new Color(1f, 0.5f, 0f); // Orange
            case PrizeType.Exotic: return Color.cyan;
            case PrizeType.Ultimate: return new Color(1f, 0f, 1f); // Pink
            case PrizeType.SuperUltimate: return new Color(0.5f, 0f, 1f); // Purple
            case PrizeType.Godlike: return new Color(1f, 0.8f, 0f); // Gold
            default: return Color.white;
        }
    }


}
