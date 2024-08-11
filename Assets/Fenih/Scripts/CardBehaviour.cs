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
    None
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

    [SerializeField] private GameObject drawnCardShow;
    [SerializeField] private GameObject boardCardShow;
    [SerializeField] private GameObject characterGO;

    [SerializeField] private Animator characterAnimator;

    [SerializeField] private ParticleSystem hitParticleEffect;


    public int TakeDamage(int amount)
    {
        hp -= amount;
        hitParticleEffect.Play();

        return hp;
    }

    public int HealDamage(int amount)
    {
        hp += amount;

        return hp;
    }

    public void ShowCard()
    {
        drawnCardShow.SetActive(true);
        boardCardShow.SetActive(false);
        characterGO.SetActive(false);
    }

    public void PutCardOnBoard()
    {
        drawnCardShow.SetActive(false);
        boardCardShow.SetActive(true);
        characterGO.SetActive(true);
    }

    public void PutOnPile()
    {
        drawnCardShow.SetActive(false);
        boardCardShow.SetActive(true);
        characterGO.SetActive(false);
    }

    public void MakeDamage()
    {
        characterAnimator.SetTrigger("OnMakeDamage");
    }
}
