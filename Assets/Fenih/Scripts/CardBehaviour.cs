using MoreMountains.Feedbacks;
using System;
using System.Collections;
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

    //DefaultProperties
    [HideInInspector] public int defaultDamage;
    [HideInInspector] public int defaultHp;
    [HideInInspector] public int defaultRange;
    [HideInInspector] public int defaultMovementRange;
    [HideInInspector] public int defaultEnergyRequired;
    [HideInInspector] public SpecialAbilities defaultAbility;
    [HideInInspector] public Category defaultCategory;
    [HideInInspector] public string defaultCardName;

    //Has the card been played this turn?
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
    [SerializeField] private Color boardVisualBackgroundColor;
    [SerializeField] private Color handVisualBackgroundColor;
    [SerializeField] private Texture2D backgroundImage;
    [SerializeField] private bool changeHandVisualBGColor = true;

    private void Awake()
    {
        GetVisualObjects();
        ChangeCardsColor();

        damageText.text = "";
        damageText.gameObject.SetActive(false);
        characterBoardVisual.SetActive(false);

        defaultDamage = damage;
        defaultHp = hp;
        defaultRange = range;
        defaultMovementRange = movementRange;
        defaultEnergyRequired = energyRequired;
        defaultAbility = ability;
        defaultCategory = category;
        defaultCardName = cardName;

        transform.name = defaultCardName;
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

    public void ResetProperties(int damage, int hp, int range, int movementRange, 
                               int energyRequired, SpecialAbilities ability, Category category, string name)
    {
        this.damage = damage;
        this.hp = hp;
        this.range = range;
        this.movementRange = movementRange;
        this.energyRequired = energyRequired;
        this.ability = ability;
        this.category = category;
        this.cardName = name;

        transform.name = cardName;
    }

    public void ResetRotationCardBoardVisual()
    {
        foreach (Transform child in boardVisual.transform)
        {
            if (child.name == "Card")
            {
                child.rotation = Quaternion.Euler(0, 0, 0);
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
                child.rotation = Quaternion.Euler(0, 0, 180);

                if(child.TryGetComponent(out MeshRenderer renderer))
                {

                    renderer.material.color = boardVisualBackgroundColor;

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
                        renderer.material.color = handVisualBackgroundColor;
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
                                   MMF_Player juiceDamageFeedbackPlayer, TurnSystemBehaviour turnSystem, 
                                   BoardManager boardManager, string op, bool isEnemy)
    {
        //Move this card
        cardPlayed = true;
        int rowEnd = isEnemy ? 0 : tiles.GetLength(0) - 1;

        if (curTile.secondaryCard == null)
        {
            boardManager.ChangeStateTile(row, column, 1, 1, true);
        }

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
                bool makeDamage = false;

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
                    if (AddOrSubstract(row, op, i) < 0 || AddOrSubstract(row, op, i) >= tiles.GetLength(0) - 1) break;

                    else if (tiles[AddOrSubstract(row, op, i), column].currentCard != null && 
                            ((tiles[AddOrSubstract(row, op, i), column].isPlayersTile && isEnemy) || 
                            (!tiles[AddOrSubstract(row, op, i), column].isPlayersTile && !isEnemy)))
                    {
                        makeDamage = true;
                        yield return TryDoingDamage(tiles, AddOrSubstract(row, op, i), column, this, juiceDamageFeedbackPlayer, turnSystem, isEnemy);
                        break;
                    }
                }

                if ((tiles[AddOrSubstract(row, op, 1), column].currentCard == null && !makeDamage) || 
                    (tiles[AddOrSubstract(row, op, 1), column].currentCard == null && makeDamage && range <= 1))
                {
                    yield return new WaitForSeconds(.2f);
                    yield return MoveMainCard(tiles, AddOrSubstract(row, op, 1), column, curTile, playerColor, enemyColor, isEnemy);
                }
            }

            boardManager.ChangeStateTile(row, column, 1, 1, false);
        }

        else if (category == Category.Building)
        {
            //In case the tile has a secondary card
            if (curTile.secondaryCard != null)
            {
                if (tiles[AddOrSubstract(row, op, 1), column].currentCard == null && !curTile.secondaryCard.cardPlayed)
                {
                    boardManager.ChangeStateTile(AddOrSubstract(row, op, 1), column, 1, 1, false);
                    yield return new WaitForSeconds(.15f);
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

                    if (tiles[AddOrSubstract(row, op, 1), column].currentCard == null)
                    {
                        boardManager.ChangeStateTile(AddOrSubstract(row, op, 1), column, 1, 1, false);
                        yield return new WaitForSeconds(.15f);
                        yield return MoveSecondaryCardForward(tiles, AddOrSubstract(row, op, 1), column, curTile, playerColor, enemyColor, isEnemy);
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

                if (tiles[AddOrSubstract(row, op, 1), column].currentCard == null)
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
            thisTurnTiles[row, col].ChangeTileColor(enemyColor, true);
        }

        else if (!thisTurnTiles[row, col].isPlayersTile && !isEnemy)
        {
            thisTurnTiles[row, col].isPlayersTile = true;
            thisTurnTiles[row, col].ChangeTileColor(playerColor, true);
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
            thisTurnTiles[row, col].ChangeTileColor(enemyColor, true);
        }

        else if (!thisTurnTiles[row, col].isPlayersTile && !isOpponent)
        {
            thisTurnTiles[row, col].isPlayersTile = true;
            thisTurnTiles[row, col].ChangeTileColor(playerColor, true);
        }
    }

    private IEnumerator TryDoingDamage(BoardTile[,] thisTurnTiles, int row, int col, CardBehaviour curCard, MMF_Player juicePlayer, TurnSystemBehaviour turnSystem, bool isEnemy)
    {
        if (curCard.damage > 0 && ((thisTurnTiles[row, col].isPlayersTile && isEnemy) || (!thisTurnTiles[row, col].isPlayersTile && !isEnemy)))
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
                        DestroyedBuilding(thisTurnTiles[row, col].currentCard, turnSystem, !isEnemy);
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

    private void DestroyedBuilding(CardBehaviour destroyedCard, TurnSystemBehaviour turnSystem, bool isPlayer = false)
    {
        if (isPlayer)
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
