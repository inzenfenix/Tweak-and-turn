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
    HealCard,
    RowAttack,
};

public enum Category
{
    Normal,
    Building,
    Throwable,
    Special
};

[Serializable]
public class CardBehaviour : MonoBehaviour
{
    public int damage;
    public int hp;
    public int range;
    public int energyRequired;

    public SpecialAbilities ability;
    public Category category;
    public string cardName;

    [HideInInspector] public bool cardPlayed = false;

    public int TakeDamage(int amount)
    {
        hp -= amount;
        return hp;
    }

    public int HealDamage(int amount)
    {
        hp += amount;

        return hp;
    }
}
