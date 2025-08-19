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
    public int movementSpeed { get; set; }
    public int maxActionPoints { get; set; }
    public List<ActionData> availableActions = new List<ActionData>();
    public void onStart()
    {
        MaxHp = _MaxHp;
        movementSpeed = _movementSpeed;
        maxActionPoints = _maxActionPoints;
    }
    public float getHealth()
    {
        return MaxHp;
    }
    public int getActionPoints()
    {
        return maxActionPoints;
    }
}
