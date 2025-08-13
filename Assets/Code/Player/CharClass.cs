using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharClass", menuName = "CharClass")]
public class CharClass : ScriptableObject
{
    [SerializeField]  float _MaxHp;
    [SerializeField] int _movementSpeed;
    [SerializeField] int _maxActionPoints;
    public float MaxHp { get; set; }
    public float Health { get; set; }
    public int movementSpeed { get; set; }
    public int maxActionPoints { get; set; }
    public int currentActionPoints { get; set; }
    public List<ActionData> availableActions = new List<ActionData>();
    public List<ItemData> inventory = new List<ItemData>();
    public void onStart()
    {
        MaxHp = _MaxHp;
        Health=_MaxHp;
        movementSpeed = _movementSpeed;
        maxActionPoints = _maxActionPoints;
        currentActionPoints = maxActionPoints;
    }
}
