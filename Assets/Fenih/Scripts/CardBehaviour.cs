using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpecialAbilities
{
    AttackInAllDirections,
    AttackHorizontally,
    EnergyUP,
    DrawUP,

}

[Serializable]
public class CardBehaviour : MonoBehaviour
{
    public int damage;
    public int hp;
    public int range;
    public int energyRequired;

    public SpecialAbilities ability;
    public string cardName;
}
