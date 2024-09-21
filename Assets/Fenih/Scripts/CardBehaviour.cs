using MoreMountains.Feedbacks;
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
    public int movementRange;
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
    [SerializeField] private ActionVisualManager actionsVisuals;

    [Header("\nCard Design")]
    [SerializeField] private Color backgroundColor;
    [SerializeField] private Texture2D backgroundImage;
    [SerializeField] private bool changeHandVisualBGColor = true;



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

                    if (backgroundImage != null)
                    {
                        //renderer.material.SetTexture("_BaseMap", backgroundImage);
                    }
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
                    if (changeHandVisualBGColor)
                    {
                        renderer.material.color = backgroundColor;
                    }

                    if (backgroundImage != null)
                    {
                        renderer.material.SetTexture("_BaseMap", backgroundImage);
                    }
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

        if(actionsVisuals !=  null)
            actionsVisuals.DisableAll();
    }

    public void MakeDamage()
    {
        if(characterAnimator != null)
            characterAnimator.SetTrigger("OnMakeDamage");
    }

    public IEnumerator CardActions(BoardTile[,] tiles, BoardTile curTile, int row, int column, Color playerColor, Color enemyColor, 
                                     MMF_Player juiceDamageFeedbackPlayer, TurnSystemBehaviour turnSystem, string op, bool isEnemy = false)
    {
        //Move this card
        cardPlayed = true;
        int rowEnd = isEnemy ? 0 : tiles.GetLength(0) - 1;

        if (category == Category.Normal)
        {
            if (AddOrSubstract(row, op, 1) == rowEnd)
            {
                if (tiles[AddOrSubstract(row, op, 1), column].currentCard != null)
                {
                    yield return TryDoingDamage(tiles, AddOrSubstract(row, op, 1), column, this, juiceDamageFeedbackPlayer, turnSystem, isEnemy);
                }
            }

            bool isARowCond = isEnemy ? AddOrSubstract(row, op, 1) >= 0 : AddOrSubstract(row, op, 1) < tiles.GetLength(0);

            if (isARowCond && (AddOrSubstract(row, op, 1) != rowEnd))
            {
                bool madeDamage = false;

                if (tiles[AddOrSubstract(row, op, 1), column].currentCard != null)
                {
                    if (tiles[AddOrSubstract(row, op, 1), column].currentCard.category == Category.Building && 
                        tiles[AddOrSubstract(row, op, 1), column].secondaryCard == null && (
                        tiles[AddOrSubstract(row, op, 1), column].isPlayersTile && !isEnemy ||
                        !tiles[AddOrSubstract(row, op, 1), column].isPlayersTile && isEnemy))
                    {
                        yield return MoveSecondaryCard(tiles, AddOrSubstract(row, op, 1), column, curTile);
                        curTile = tiles[AddOrSubstract(row, op, 1), column];
                    }
                }

                for (int i = 1; i <= range; i++)
                {
                    if (AddOrSubstract(row, op, i) < 0) break;

                    else if (tiles[AddOrSubstract(row, op, i), column].currentCard != null && 
                            ((tiles[AddOrSubstract(row, op, i), column].isPlayersTile && isEnemy) || 
                            (!tiles[AddOrSubstract(row, op, i), column].isPlayersTile && !isEnemy)))
                    {
                        madeDamage = true;
                        yield return TryDoingDamage(tiles, AddOrSubstract(row, op, i), column, this, juiceDamageFeedbackPlayer, turnSystem, isEnemy);
                        break;
                    }
                }

                if (tiles[AddOrSubstract(row, op, 1), column].currentCard == null && !madeDamage)
                {
                    yield return MoveMainCard(tiles, AddOrSubstract(row, op, 1), column, curTile, playerColor, enemyColor, isEnemy);
                }
            }
        }

        else if (category == Category.Building)
        {
            //In case the tile has a secondary card
            if (curTile.secondaryCard != null)
            {
                if (tiles[AddOrSubstract(row, op, 1), column].currentCard == null && !curTile.secondaryCard.cardPlayed)
                {
                    yield return MoveSecondaryCardForward(tiles, AddOrSubstract(row, op, 1), column, curTile, playerColor, enemyColor, isEnemy);
                }

                else if (tiles[AddOrSubstract(row, op, 1), column].currentCard != null && !curTile.secondaryCard.cardPlayed)
                {
                    for (int i = 1; i <= curTile.secondaryCard.range; i++)
                    {
                        if (AddOrSubstract(row, op, i) < 0) break;

                        else if (tiles[AddOrSubstract(row, op, i), column].currentCard != null &&
                                ((tiles[AddOrSubstract(row, op, i), column].isPlayersTile && isEnemy) ||
                                (!tiles[AddOrSubstract(row, op, i), column].isPlayersTile && !isEnemy)))
                        {
                            yield return TryDoingDamage(tiles, AddOrSubstract(row, op, i), column, curTile.secondaryCard, juiceDamageFeedbackPlayer, turnSystem, isEnemy);
                            break;
                        }
                    }
                }
            }
        }
    }

    public void CardNextAction(BoardTile[,] tiles, BoardTile curTile, int row, int column, string op, bool isEnemy = false)
    {
        if (actionsVisuals == null) return;

        //Move this card
        int rowEnd = isEnemy ? 0 : tiles.GetLength(0) - 1;

        if (category == Category.Normal)
        {
            if (AddOrSubstract(row, op, 1) == rowEnd)
            {
                if (tiles[AddOrSubstract(row, op, 1), column].currentCard != null)
                {
                    actionsVisuals.EnableSword();
                    return;
                }
            }

            bool isARowCond = isEnemy ? AddOrSubstract(row, op, 1) >= 0 : AddOrSubstract(row, op, 1) < tiles.GetLength(0);

            if (isARowCond && (AddOrSubstract(row, op, 1) != rowEnd))
            {
                bool madeDamage = false;

                if (tiles[AddOrSubstract(row, op, 1), column].currentCard != null)
                {
                    if (tiles[AddOrSubstract(row, op, 1), column].currentCard.category == Category.Building &&
                        tiles[AddOrSubstract(row, op, 1), column].secondaryCard == null && (
                        tiles[AddOrSubstract(row, op, 1), column].isPlayersTile && !isEnemy ||
                        !tiles[AddOrSubstract(row, op, 1), column].isPlayersTile && isEnemy))
                    {
                        if(isEnemy)
                        {
                            actionsVisuals.EnableArrow("Backward");
                        }

                        else
                        {
                            actionsVisuals.EnableArrow("Forward");
                        }

                        curTile = tiles[AddOrSubstract(row, op, 1), column];
                        return;
                    }
                }

                for (int i = 1; i <= range; i++)
                {
                    if (AddOrSubstract(row, op, i) < 0) break;

                    else if (tiles[AddOrSubstract(row, op, i), column].currentCard != null &&
                            ((tiles[AddOrSubstract(row, op, i), column].isPlayersTile && isEnemy) ||
                            (!tiles[AddOrSubstract(row, op, i), column].isPlayersTile && !isEnemy)))
                    {
                        madeDamage = true;
                        
                        if(range <= 1)
                        {
                            actionsVisuals.EnableSword();
                        }
                        else
                        {
                            actionsVisuals.EnableBow();
                        }

                        return;
                    }
                }

                if (tiles[AddOrSubstract(row, op, 1), column].currentCard == null && !madeDamage)
                {
                    if (isEnemy)
                    {
                        actionsVisuals.EnableArrow("Backward");
                    }

                    else
                    {
                        actionsVisuals.EnableArrow("Forward");
                    }
                }
            }
        }

        else if (category == Category.Building)
        {
            //In case the tile has a secondary card
            if (curTile.secondaryCard != null)
            {
                if (tiles[AddOrSubstract(row, op, 1), column].currentCard == null && !curTile.secondaryCard.cardPlayed)
                {
                    if (isEnemy)
                    {
                        curTile.secondaryCard.actionsVisuals.EnableArrow("Backward");
                    }

                    else
                    {
                        curTile.secondaryCard.actionsVisuals.EnableArrow("Forward");
                    }
                }

                else if (tiles[AddOrSubstract(row, op, 1), column].currentCard != null && !curTile.secondaryCard.cardPlayed)
                {
                    for (int i = 1; i <= curTile.secondaryCard.range; i++)
                    {
                        if (AddOrSubstract(row, op, i) < 0) break;

                        else if (tiles[AddOrSubstract(row, op, i), column].currentCard != null &&
                                ((tiles[AddOrSubstract(row, op, i), column].isPlayersTile && isEnemy) ||
                                (!tiles[AddOrSubstract(row, op, i), column].isPlayersTile && !isEnemy)))
                        {
                            if (curTile.secondaryCard.range <= 1)
                            {
                                curTile.secondaryCard.actionsVisuals.EnableSword();
                            }
                            else
                            {
                                curTile.secondaryCard.actionsVisuals.EnableBow();
                            }
                            return;
                        }
                    }
                }
            }
        }
    }

    private int AddOrSubstract(int x, string op, int y)
    {
        if (op.Equals("+"))
            return x + y;
        else if (op.Equals("-"))
            return x - y;
        else
            return -1;
    }

    private IEnumerator MoveMainCard(BoardTile[,] thisTurnTiles, int row, int col, BoardTile curTile, Color playerColor, Color enemyColor, bool isEnemy = false)
    {
        thisTurnTiles[row, col].currentCard = curTile.currentCard;
        curTile.currentCard = null;

        float lerp = 0;
        float speed = 7.5f;

        Vector3 originalPos = thisTurnTiles[row, col].currentCard.transform.position;
        Vector3 goalPos = thisTurnTiles[row, col].tileHolder.transform.position + Vector3.up * .05f;

        while (lerp < 1)
        {
            lerp += Time.deltaTime * speed;
            thisTurnTiles[row, col].currentCard.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
            yield return new WaitForEndOfFrame();
        }

        if (thisTurnTiles[row, col].isPlayersTile && isEnemy)
        {
            thisTurnTiles[row, col].isPlayersTile = false;
            thisTurnTiles[row, col].ChangeTileColor(enemyColor);
        }

        else if (!thisTurnTiles[row, col].isPlayersTile && !isEnemy)
        {
            thisTurnTiles[row, col].isPlayersTile = true;
            thisTurnTiles[row, col].ChangeTileColor(playerColor);
        }

        thisTurnTiles[row, col].currentCard.transform.parent = thisTurnTiles[row, col].tileHolder.transform;
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator MoveSecondaryCard(BoardTile[,] thisTurnTiles, int row, int col, BoardTile curTile)
    {
        thisTurnTiles[row, col].secondaryCard = curTile.currentCard;
        curTile.currentCard = null;

        float lerp = 0;
        float speed = 7.5f;

        Vector3 originalPos = thisTurnTiles[row, col].secondaryCard.transform.position;
        Vector3 goalPos = thisTurnTiles[row, col].tileHolder.transform.position + Vector3.up * .15f;

        while (lerp < 1)
        {
            lerp += Time.deltaTime * speed;
            thisTurnTiles[row, col].secondaryCard.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
            yield return new WaitForEndOfFrame();
        }

        thisTurnTiles[row, col].secondaryCard.transform.parent = thisTurnTiles[row, col].tileHolder.transform;

        yield return new WaitForEndOfFrame();
    }

    private IEnumerator MoveSecondaryCardForward(BoardTile[,] thisTurnTiles, int row, int col, BoardTile curTile, Color playerColor, Color enemyColor, bool isOpponent = false)
    {
        thisTurnTiles[row, col].currentCard = curTile.secondaryCard;
        curTile.secondaryCard = null;

        float lerp = 0;
        float speed = 7.5f;

        Vector3 originalPos = thisTurnTiles[row, col].currentCard.transform.position;
        Vector3 goalPos = thisTurnTiles[row, col].tileHolder.transform.position + Vector3.up * .05f;

        thisTurnTiles[row, col].currentCard.cardPlayed = true;

        while (lerp < 1)
        {
            lerp += Time.deltaTime * speed;
            thisTurnTiles[row, col].currentCard.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
            yield return new WaitForEndOfFrame();
        }

        thisTurnTiles[row, col].currentCard.transform.parent = thisTurnTiles[row, col].tileHolder.transform;

        yield return new WaitForEndOfFrame();

        if (thisTurnTiles[row, col].isPlayersTile && isOpponent)
        {
            thisTurnTiles[row, col].isPlayersTile = false;
            thisTurnTiles[row, col].ChangeTileColor(enemyColor);
        }

        else if (!thisTurnTiles[row, col].isPlayersTile && !isOpponent)
        {
            thisTurnTiles[row, col].isPlayersTile = true;
            thisTurnTiles[row, col].ChangeTileColor(playerColor);
        }
    }

    private IEnumerator TryDoingDamage(BoardTile[,] thisTurnTiles, int row, int col, CardBehaviour curCard, MMF_Player juicePlayer, TurnSystemBehaviour turnSystem, bool isOpponnent = false)
    {
        if (curCard.damage > 0 && ((thisTurnTiles[row, col].isPlayersTile && isOpponnent) || (!thisTurnTiles[row, col].isPlayersTile && !isOpponnent)))
        {
            curCard.GetComponent<CardBehaviour>().MakeDamage();
            yield return new WaitForSeconds(.2f);
            juicePlayer.PlayFeedbacks();
            if (thisTurnTiles[row, col].currentCard != null)
            {
                int cardHP = thisTurnTiles[row, col].currentCard.TakeDamage(curCard.damage);

                if (cardHP <= 0)
                {
                    if (thisTurnTiles[row, col].currentCard.category == Category.Building)
                    {
                        DestroyedBuilding(thisTurnTiles[row, col].currentCard, turnSystem, isOpponnent);
                    }

                    Destroy(thisTurnTiles[row, col].currentCard.gameObject, .25f);
                    thisTurnTiles[row, col].currentCard = null;

                    if (thisTurnTiles[row, col].secondaryCard != null)
                    {
                        thisTurnTiles[row, col].currentCard = thisTurnTiles[row, col].secondaryCard;
                        thisTurnTiles[row, col].secondaryCard = null;

                        thisTurnTiles[row, col].currentCard.transform.position = thisTurnTiles[row, col].tileHolder.transform.position + Vector3.up * .05f;
                    }
                }
            }
        }
    }

    private void DestroyedBuilding(CardBehaviour destroyedCard, TurnSystemBehaviour turnSystem, bool isOpponent = false)
    {
        if (isOpponent)
        {
            switch (destroyedCard.ability)
            {
                case SpecialAbilities.DrawUP:
                    turnSystem.SubstractExtraCharge(1, true);
                    break;
                case SpecialAbilities.HealCard:
                    turnSystem.extraHPEnemy--;
                    break;
                case SpecialAbilities.EnergyUP:
                    turnSystem.energyManager.extraEnergyAI--;
                    break;
                case SpecialAbilities.AttackUp:
                    turnSystem.extraAttackEnemy--;
                    break;

                default:
                    break;
            }
        }
        else
        {
            switch (destroyedCard.ability)
            {
                case SpecialAbilities.DrawUP:
                    turnSystem.SubstractExtraCharge(1, false);
                    break;
                case SpecialAbilities.HealCard:
                    turnSystem.extraHPPlayer--;
                    break;
                case SpecialAbilities.EnergyUP:
                    turnSystem.energyManager.extraEnergy--;
                    break;
                case SpecialAbilities.AttackUp:
                    turnSystem.extraAttackPlayer--;
                    break;

                default:
                    break;
            }
        }
    }

    
}
