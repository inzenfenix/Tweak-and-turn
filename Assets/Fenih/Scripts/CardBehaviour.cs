using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum SpecialAbilities
{
    AttackInAllDirections,
    AttackHorizontally,
    EnergyUP,
    DrawUP,
    HealCard,
    RowAttack,
    AttackUp,
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
    [Header("\nCard properties")]
    public int damage;
    public int hp;
    public int range;
    public int energyRequired;
    public SpecialAbilities ability;
    public Category category;
    public string cardName;

    [HideInInspector] public bool cardPlayed = false;

    [Header("Child objects")]
    private GameObject handVisual;
    private GameObject boardVisual;
    [HideInInspector] public GameObject characterBoardVisual;

    [Header("\nComponents and others")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private ParticleSystem hitParticleEffect;
    [SerializeField] private TextMeshPro damageText;

    [Header("\nCard Design")]
    [SerializeField] private Color backgroundColor;

    private void Awake()
    {
        GetVisualObjects();
        ChangeCardsColor();

        damageText.text = "";
        damageText.gameObject.SetActive(false);
        characterBoardVisual.SetActive(false);
    }

    private void GetVisualObjects()
    {
        foreach (Transform child in transform)
        {
            if (child.CompareTag("HandVisual"))
            {
                handVisual = child.gameObject;
            }

            else if (child.CompareTag("BoardVisual"))
            {
                boardVisual = child.gameObject;
            }
        }

        if (boardVisual != null)
        {
            foreach (Transform child in boardVisual.transform)
            {
                if (child.CompareTag("CharacterVisual"))
                {
                    characterBoardVisual = child.gameObject;
                }
            }
        }
    }

    private void ChangeCardsColor()
    {
        //Get card from board visual
        foreach(Transform child in boardVisual.transform)
        {
            if(child.name == "Card")
            {
                if(child.TryGetComponent(out MeshRenderer renderer))
                {
                    renderer.material.color = backgroundColor;
                }
            }
        }

        //Get card from hand visual
        foreach (Transform child in handVisual.transform)
        {
            if (child.name == "Card")
            {
                if (child.TryGetComponent(out MeshRenderer renderer))
                {
                    renderer.material.color = backgroundColor;
                }
            }
        }
    }

    public int TakeDamage(int amount)
    {
        hp -= amount;
        hitParticleEffect.Play();

        StartCoroutine(ShowDamage(amount));

        return hp;
    }

    private IEnumerator ShowDamage(int amount)
    {
        yield return new WaitForEndOfFrame();
        damageText.gameObject.SetActive(true);
        damageText.text = "-" + amount;
        yield return new WaitForSeconds(1f);

        damageText.text = "";
        damageText.gameObject.SetActive(false);
    }

    public int HealDamage(int amount)
    {
        hp += amount;

        return hp;
    }

    public void ShowCard()
    {
        handVisual.SetActive(true);
        boardVisual.SetActive(false);
        characterBoardVisual.SetActive(false);
    }

    public void PutCardOnBoard()
    {
        handVisual.SetActive(false);
        boardVisual.SetActive(true);
        characterBoardVisual.SetActive(true);
    }

    public void PutOnPile()
    {
        handVisual.SetActive(false);
        boardVisual.SetActive(true);
        characterBoardVisual.SetActive(false);
    }

    public void MakeDamage()
    {
        if(characterAnimator != null)
            characterAnimator.SetTrigger("OnMakeDamage");
    }
}
