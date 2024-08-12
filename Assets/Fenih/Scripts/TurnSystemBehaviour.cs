using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public enum CurrentState
{
    DrawingCards,
    PuttingCardsOnBoard,
    EnemiesTurn,
    PlayingCards,
    PassTurn
};

public enum CurrentCamera
{
    ChoosingCards,
    PlayingCards,
    BatteryCharging
}

public class TurnSystemBehaviour : MonoBehaviour
{
    public static event EventHandler<GameObject> OnCardChose;
    public static event EventHandler OnCardChoseExited;

    public static event EventHandler<CurrentCamera> OnChangeCamera;

    private List<GameObject> availableCards;

    private List<GameObject> discardCards;

    private List<GameObject> currentCards;

    private int amountOfDrawCards = 5;
    private int amountOfDrawCardsAI = 6;

    private int drawPileInitialAmount = 50;

    private int currentCardAmount = 0;

    [SerializeField] private GameObject[] cardsPrefabs;
    [SerializeField] private GameObject[] cardsPrefabsEnemy;

    [SerializeField] private Transform[] cardsPositions;

    private int maxAmountOfCards;

    [SerializeField] private Transform drawPile;
    private float currentDrawPileYOffset = 0;

    [SerializeField] private Transform discardPile;
    private float currentDiscardPileYOffset = 0;

    private int maxTurns = 20;
    [HideInInspector] public int currentTurn = 1;

    private CurrentState currentState;
    private CurrentState currentStateAI;

    private GameObject hoveredCard;
    private CardBehaviour hoveredCardBehaviour;

    private GameObject selectedCard;
    private Vector3[] defaultCardScale;

    [SerializeField] private LayerMask cardMask;

    [SerializeField] private Transform chosenCardPos;

    private EnergyManager energyManager;
    private BoardManager boardManager;

    private BoardTile[,] thisTurnTiles;

    private bool isCardSelected;

    private List<GameObject> currentCardsAI;

    private bool currentlyWorking = false;

    private MMF_Player juicePlayer;

    public Color playerColor;
    public Color enemyColor;

    [SerializeField] private AudioSource cardWhooshSounds;
    [SerializeField] private AudioClip[] cardWhooshClips;

    [SerializeField] private TextMeshPro turnText;

    private bool finishGame = false;

    [SerializeField] private GameObject[] obeliskGemsPlayer;
    [SerializeField] private GameObject[] obeliskGemsEnemy;

    [SerializeField] private Material obelliskNormalMat;
    [SerializeField] private Material obelliskEmissiveMat;

    int currentTurnsObeliskPlayer = 0;
    int currentTurnsObeliskEnemy = 0;

    bool[] levelsObeliskPlayer;
    bool[] levelsObeliskEnemy;

    [HideInInspector] public int extraHPPlayer = 0;
    [HideInInspector] public int extraHPEnemy = 0;

    [HideInInspector] public int extraAttackPlayer = 0;
    [HideInInspector] public int extraAttackEnemy = 0;

    private void Awake()
    {
        amountOfDrawCards = 5;
        amountOfDrawCardsAI = 6;

        currentDrawPileYOffset = 0;
        currentDiscardPileYOffset = 0;

        currentCardAmount = 0;
        currentTurn = 1;

        availableCards = new List<GameObject>();
        discardCards = new List<GameObject>();

        maxAmountOfCards = cardsPositions.Length;

        currentState = CurrentState.DrawingCards;

        defaultCardScale = new Vector3[maxAmountOfCards];

        CreateCards();

        hoveredCard = null;

        isCardSelected = false;

        energyManager = GetComponent<EnergyManager>();
        boardManager = GetComponent<BoardManager>();
        juicePlayer = GetComponent<MMF_Player>();

        if (cardWhooshClips.Length > 0)
            cardWhooshSounds.clip = cardWhooshClips[0];

        turnText.text = "Current Turn\n" + currentTurn + "/" + maxTurns;

        finishGame = false;

        levelsObeliskPlayer = new bool[obeliskGemsPlayer.Length];
        levelsObeliskEnemy = new bool[obeliskGemsEnemy.Length];

        for(int i = 0; i < obeliskGemsPlayer.Length; i++)
        {
            levelsObeliskEnemy[i] = levelsObeliskPlayer[i] = false;
        }

    }

    private void OnEnable()
    {
        GameManager.OnEndTurn += GameManager_OnEndTurn;
        BoardManager.OnPutCardOnBoard += BoardManager_OnPutCardOnBoard;
    }



    private void OnDisable()
    {
        GameManager.OnEndTurn -= GameManager_OnEndTurn;
        BoardManager.OnPutCardOnBoard -= BoardManager_OnPutCardOnBoard;
    }

    private void BoardManager_OnPutCardOnBoard(object sender, GameObject e)
    {
        if (e == selectedCard)
        {
            int index = currentCards.IndexOf(e);

            OnCardChoseExited?.Invoke(this, EventArgs.Empty);

            currentCards.Remove(selectedCard);
            selectedCard = null;

            isCardSelected = false;

            RefreshDrawnCardsPositions();

            currentCardAmount--;
        }
    }

    private void Update()
    {
        if (finishGame) return;

        if (currentlyWorking) return;

        switch (currentState)
        {
            case (CurrentState.DrawingCards):
                StartCoroutine(DrawCards());
                break;

            case (CurrentState.PuttingCardsOnBoard):
                PuttingCardsOnBoard();
                break;

            case (CurrentState.PlayingCards):
                StartCoroutine(PlayingCards());
                break;

            case (CurrentState.PassTurn):
                currentState = CurrentState.EnemiesTurn;
                currentStateAI = CurrentState.DrawingCards;
                break;

            case (CurrentState.EnemiesTurn):
                EnemyTurn();
                break;

            default:
                break;
        }
    }

    private void ChangeWhooshSound()
    {
        int index = Array.IndexOf(cardWhooshClips, cardWhooshSounds.clip);

        int newIndex = index + 1;

        if (newIndex >= cardWhooshClips.Length)
            newIndex = 0;

        cardWhooshSounds.clip = cardWhooshClips[newIndex];
    }

    private void EnemyTurn()
    {
        switch (currentStateAI)
        {
            case (CurrentState.DrawingCards):
                DrawCardsAI();
                break;

            case (CurrentState.PuttingCardsOnBoard):
                StartCoroutine(PuttingCardsOnBoardAI());
                break;

            case (CurrentState.PlayingCards):
                StartCoroutine(PlayingCardsAI());
                break;

            case (CurrentState.PassTurn):
                CheckObelisks();
                for (int i = 0; i < currentCardsAI.Count; i++)
                {
                    Destroy(currentCardsAI[i]);
                }

                currentState = CurrentState.DrawingCards;
                break;

            default:
                break;
        }
    }

    private void CheckObelisks()
    {
        BoardTile[,] tiles = boardManager.Tiles;

        int playerWinningCols = 0;
        int enemyWinningCols = 0;

        for (int i = 0; i < tiles.GetLength(1); i++)
        {
            int tilesPerColPlayer = 0;
            int tilesPerColEnemy = 0;

            for (int j = 0; j < tiles.GetLength(0); j++)
            {
                if (tiles[j, i].isPlayersTile) tilesPerColPlayer++;
                else tilesPerColEnemy++;
            }

            if (tilesPerColPlayer > tilesPerColEnemy) playerWinningCols++;
            else enemyWinningCols++;
        }

        if(playerWinningCols != tiles.GetLength(1))
        {
            ResetObelisk();
        }

        if(enemyWinningCols != tiles.GetLength(1))
        {
            ResetObelisk(true);
        }

        if(playerWinningCols == tiles.GetLength(1))
        {
            currentTurnsObeliskPlayer++;

            currentTurnsObeliskPlayer = Mathf.Min(currentTurnsObeliskPlayer, obeliskGemsPlayer.Length);

            for(int i = 0; i < currentTurnsObeliskPlayer; i++)
            {
                obeliskGemsPlayer[i].GetComponent<MeshRenderer>().material = obelliskEmissiveMat;
            }

            switch(currentTurnsObeliskPlayer)
            {
                case (1):
                    if (!levelsObeliskPlayer[currentTurnsObeliskPlayer - 1])
                    {
                        levelsObeliskPlayer[currentTurnsObeliskPlayer - 1] = true;
                        energyManager.extraEnergy++;
                    }
                    break;
                case (2):
                    if (!levelsObeliskPlayer[currentTurnsObeliskPlayer - 1])
                    {
                        levelsObeliskPlayer[currentTurnsObeliskPlayer - 1] = true;
                        energyManager.extraEnergy++;
                    }
                    break;
                case (3):
                    if (!levelsObeliskPlayer[currentTurnsObeliskPlayer - 1])
                    {
                        levelsObeliskPlayer[currentTurnsObeliskPlayer - 1] = true;
                        extraHPPlayer++;
                    }
                    break;
                case (4):
                    if (!levelsObeliskPlayer[currentTurnsObeliskPlayer - 1])
                    {
                        levelsObeliskPlayer[currentTurnsObeliskPlayer - 1] = true;
                        extraAttackPlayer++;
                    }
                    break;
                case (5):
                    if (!levelsObeliskPlayer[currentTurnsObeliskPlayer - 1])
                    {
                        levelsObeliskPlayer[currentTurnsObeliskPlayer - 1] = true;
                        extraAttackPlayer++;
                    }
                    break;


                default:
                    break;
            }
        }

        if(enemyWinningCols == tiles.GetLength(1))
        {
            currentTurnsObeliskEnemy++;

            currentTurnsObeliskEnemy = Mathf.Min(currentTurnsObeliskEnemy, obeliskGemsEnemy.Length);

            for (int i = 0; i < currentTurnsObeliskEnemy; i++)
            {
                obeliskGemsEnemy[i].GetComponent<MeshRenderer>().material = obelliskEmissiveMat;
            }

            switch (currentTurnsObeliskEnemy)
            {
                case (1):
                    if (!levelsObeliskEnemy[currentTurnsObeliskEnemy - 1])
                    {
                        levelsObeliskEnemy[currentTurnsObeliskEnemy - 1] = true;
                        energyManager.extraEnergyAI++;
                    }
                    break;
                case (2):
                    if (!levelsObeliskEnemy[currentTurnsObeliskEnemy - 1])
                    {
                        levelsObeliskEnemy[currentTurnsObeliskEnemy - 1] = true;
                        energyManager.extraEnergyAI++;
                    }
                    break;
                case (3):
                    if (!levelsObeliskEnemy[currentTurnsObeliskEnemy - 1])
                    {
                        levelsObeliskEnemy[currentTurnsObeliskEnemy - 1] = true;
                        extraHPEnemy++;
                    }
                    break;
                case (4):
                    if (!levelsObeliskEnemy[currentTurnsObeliskEnemy - 1])
                    {
                        levelsObeliskEnemy[currentTurnsObeliskEnemy - 1] = true;
                        extraAttackEnemy++;
                    }
                    break;
                case (5):
                    if (!levelsObeliskEnemy[currentTurnsObeliskEnemy - 1])
                    {
                        levelsObeliskEnemy[currentTurnsObeliskEnemy - 1] = true;
                        extraAttackEnemy++;
                    }
                    break;


                default:
                    break;
            }
        }


    }

    private void ResetObelisk(bool isOpponent = false)
    {
        GameObject[] obelisk;

        if (isOpponent)
        {

            currentTurnsObeliskEnemy = 0;
            obelisk = obeliskGemsEnemy;

            switch (currentTurnsObeliskEnemy)
            {
                case (1):
                    energyManager.extraEnergyAI -= 1;
                    break;
                case (2):
                    energyManager.extraEnergyAI -= 2;
                    break;
                case (3):
                    extraHPEnemy -= 1;
                    energyManager.extraEnergyAI -= 2;
                    break;
                case (4):
                    extraAttackEnemy -= 1;
                    energyManager.extraEnergyAI -= 2;
                    extraHPEnemy -= 1;
                    break;
                case (5):
                    extraAttackEnemy -= 2;
                    energyManager.extraEnergyAI -= 2;
                    extraHPEnemy -= 1;
                    break;
                default:
                    break;
            }

            for (int i = 0; i < levelsObeliskEnemy.Length; i++)
            {
                levelsObeliskEnemy[i] = false;
            }
        }

        else
        {
            switch (currentTurnsObeliskPlayer)
            {
                case (1):
                    energyManager.extraEnergy -= 1;
                    break;
                case (2):
                        energyManager.extraEnergy -= 2;
                    break;
                case (3):
                    extraHPPlayer -= 1;
                    energyManager.extraEnergy -= 2;
                    break;
                case (4):
                    extraAttackPlayer -= 1;
                    energyManager.extraEnergy -= 2;
                    extraHPPlayer -= 1;
                    break;
                case (5):
                    extraAttackPlayer -= 2;
                    energyManager.extraEnergy -= 2;
                    extraHPPlayer -= 1;
                    break;
                default:
                    break;
            }

            for(int i = 0; i < levelsObeliskPlayer.Length; i++)
            {
                levelsObeliskPlayer[i] = false;
            }

            currentTurnsObeliskPlayer = 0;
            obelisk = obeliskGemsPlayer;
        }

        for(int i = 0; i < obelisk.Length;i++)
        {
            obelisk[i].GetComponent<MeshRenderer>().material = obelliskNormalMat;
        }

        
    }


    private void DrawCardsAI()
    {
        currentCardsAI = new List<GameObject>();

        for (int i = 0; i < amountOfDrawCardsAI; i++)
        {
            int randomCardIndex = UnityEngine.Random.Range(0, cardsPrefabs.Length);

            GameObject newCard = GameObject.Instantiate(cardsPrefabs[randomCardIndex], Vector3.up * 30f + Vector3.forward * 20f, Quaternion.identity);

            currentCardsAI.Add(newCard);
            newCard.GetComponent<CardBehaviour>().hp += extraHPEnemy;
            newCard.GetComponent<CardBehaviour>().damage += extraAttackEnemy;
        }

        currentStateAI = CurrentState.PuttingCardsOnBoard;
    }

    private IEnumerator PuttingCardsOnBoardAI()
    {
        thisTurnTiles = boardManager.Tiles;
        currentlyWorking = true;

        int energy = energyManager.currentEnergyAI;

        bool playedCard;

        int columns = boardManager.Tiles.GetLength(1);
        int rows = boardManager.Tiles.GetLength(0);

        bool[] winning = new bool[columns];

        int buildingRow = boardManager.Tiles.GetLength(0) - 2;

        //Decide if the ai is winning or losing a column
        for (int i = 0; i < columns; i++)
        {
            int aiTiles = 0;
            int playerTiles = 0;

            for (int j = 0; j < rows; j++)
            {
                BoardTile currentTile = boardManager.Tiles[j, i];
                if (currentTile.isPlayersTile) playerTiles++;
                else aiTiles++;
            }

            if (aiTiles > playerTiles) winning[i] = true;
            else winning[i] = false;
        }

        int losingColumnIndex = 0;
        bool losingRow = false;
        for(int i = 0; i < winning.Length; i++)
        {
            if (!winning[i])
            {
                losingColumnIndex = i;
                losingRow = true;
                break;
            }
        }

        if(losingRow)
        {
            int farthestRow = -1;

            if(thisTurnTiles[boardManager.Tiles.GetLength(0) - 1, losingColumnIndex].currentCard == null)
            {
                farthestRow = boardManager.Tiles.GetLength(0) - 1;
            }

            if (currentTurn < 3)
            {
                for (int i = rows - 1; i >= 0; i--)
                {
                    if (!thisTurnTiles[i, losingColumnIndex].isPlayersTile && thisTurnTiles[i, losingColumnIndex].currentCard == null)
                    {
                        farthestRow = i;
                    }
                }
            }

            if (farthestRow != -1)
            {
                for (int i = 0; i < currentCardsAI.Count; i++)
                {
                    CardBehaviour card = currentCardsAI[i].GetComponent<CardBehaviour>();
                    if (card.category == Category.Normal && card.energyRequired <= energy)
                    {
                        currentCardsAI.Remove(card.gameObject);
                        energy -= card.energyRequired;

                        yield return StartCoroutine(PlaceCardOnTileAI(card, thisTurnTiles[farthestRow, losingColumnIndex]));
                        break;
                    }
                }
            }
        }

        //Add normal cards
        for (int i = 0; i < currentCardsAI.Count; i++)
        {
            CardBehaviour card = currentCardsAI[i].GetComponent<CardBehaviour>();

            if ((card.category == Category.Normal || card.category == Category.Special) && card.energyRequired <= energy)
            {
                if (currentTurn > 3)
                {

                    for (int j = 0; j < columns; j++)
                    {
                        if (!winning[j] && thisTurnTiles[boardManager.Tiles.GetLength(0) - 1, j].currentCard == null && !thisTurnTiles[boardManager.Tiles.GetLength(0) - 1, j].isPlayersTile)
                        {
                            currentCardsAI.Remove(card.gameObject);
                            energy -= card.energyRequired;

                            yield return StartCoroutine(PlaceCardOnTileAI(card, thisTurnTiles[boardManager.Tiles.GetLength(0) - 1, j]));
                            break;
                        }
                    }
                }

                else
                {
                    int farthestRow = boardManager.Tiles.GetLength(0) - 1;

                    for (int j = 0; j < rows; j++)
                    {
                        for (int k = 0; k < columns; k++)
                        {
                            if (!thisTurnTiles[j, k].isPlayersTile)
                            {
                                if (j < farthestRow) farthestRow = j;
                            }
                        }

                    }

                    for (int j = 0; j < columns; j++)
                    {
                        if (!winning[j] && thisTurnTiles[farthestRow, j].currentCard == null && !thisTurnTiles[farthestRow, j].isPlayersTile)
                        {
                            currentCardsAI.Remove(card.gameObject);
                            energy -= card.energyRequired;

                            yield return StartCoroutine(PlaceCardOnTileAI(card, thisTurnTiles[farthestRow, j]));
                            break;
                        }
                    }
                }
            }
        }

        for (int i = 0; i < currentCardsAI.Count; i++)
        {
            CardBehaviour card = currentCardsAI[i].GetComponent<CardBehaviour>();

            if (card.category == Category.Building && card.energyRequired <= energy)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (!winning[j] && thisTurnTiles[buildingRow, j].currentCard == null)
                    {
                        switch (card.ability)
                        {
                            case SpecialAbilities.DrawUP:
                                AddExtraDraw(1, true);
                                break;
                            case SpecialAbilities.HealCard:
                                extraHPEnemy++;
                                break;
                            case SpecialAbilities.EnergyUP:
                                energyManager.extraEnergyAI++;
                                break;
                            case SpecialAbilities.AttackUp:
                                extraAttackEnemy++;
                                break;

                            default:
                                break;
                        }

                        currentCardsAI.Remove(card.gameObject);
                        energy -= card.energyRequired;

                        yield return StartCoroutine(PlaceCardOnTileAI(card, thisTurnTiles[buildingRow, j]));
                        break;

                    }
                }
            }
        }

        int plays = 0;

        do
        {
            playedCard = false;

            for (int i = 0; i < currentCardsAI.Count; i++)
            {
                CardBehaviour card = currentCardsAI[i].GetComponent<CardBehaviour>();

                if (card.energyRequired <= energy && (card.category == Category.Normal || card.category == Category.Special))
                {
                    playedCard = true;

                    for (int j = 0; j < columns; j++)
                    {
                        if (thisTurnTiles[boardManager.Tiles.GetLength(0) - 1, j].currentCard == null)
                        {
                            currentCardsAI.Remove(card.gameObject);
                            energy -= card.energyRequired;

                            yield return StartCoroutine(PlaceCardOnTileAI(card, thisTurnTiles[boardManager.Tiles.GetLength(0) - 1, j]));
                            break;
                        }
                    }

                    break;
                }
            }
            plays++;

        } while (playedCard && plays < 10);

        yield return new WaitForSeconds(.5f);
        currentStateAI = CurrentState.PlayingCards;
        currentlyWorking = false;
    }

    private IEnumerator PlaceCardOnTileAI(CardBehaviour card, BoardTile tile)
    {
        tile.currentCard = card;

        card.transform.parent = tile.tileHolder.transform;

        float lerp = 0;
        float speed = 7f;

        Vector3 originalPos = card.transform.localPosition;
        Quaternion originalRot = card.transform.localRotation;

        card.PutCardOnBoard();

        while(lerp < 1)
        {
            lerp += Time.deltaTime * speed;

            card.transform.localPosition = Vector3.Lerp(originalPos, Vector3.zero + Vector3.up * .5f, lerp);
            card.transform.localRotation = Quaternion.Lerp(originalRot, Quaternion.identity * Quaternion.Euler(0,180,0), lerp);

            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();
    }

    private IEnumerator PlayingCardsAI()
    {
        OnChangeCamera?.Invoke(this, CurrentCamera.PlayingCards);
        thisTurnTiles = boardManager.Tiles;
        currentlyWorking = true;
        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < thisTurnTiles.GetLength(0); i++)
        {
            for (int j = 0; j < thisTurnTiles.GetLength(1); j++)
            {
                BoardTile curTile = thisTurnTiles[i, j];

                if (curTile.currentCard == null) continue;

                if (curTile.currentCard.cardPlayed) continue;

                curTile.currentCard.cardPlayed = true;


                if (!curTile.isPlayersTile && curTile.currentCard.category == Category.Normal)
                {
                    if (i == thisTurnTiles.GetLength(0) - 1)
                    {
                        bool madeDamage = false;

                        if (thisTurnTiles[i - 1, j].currentCard != null)
                        {
                            if (thisTurnTiles[i - 1, j].currentCard.category == Category.Building && thisTurnTiles[i - 1, j].secondaryCard == null)
                            {
                                yield return MoveSecondaryCard(i - 1, j, curTile);
                                continue;
                            }
                        }

                        for (int k = 1; k <= curTile.currentCard.range; k++)
                        {
                            if (i - k < 0) break;

                            if (thisTurnTiles[i - k, j].isPlayersTile)
                            {
                                madeDamage = true;
                                yield return TryDoingDamage(i - k, j, curTile, true);
                                break;
                            }
                        }

                        if(!madeDamage && thisTurnTiles[i - 1, j].currentCard == null)
                        {
                            yield return MoveMainCard(i - 1, j, curTile, true);
                        }
                    }

                    if (i - 1 == 0)
                    {
                        if (thisTurnTiles[i - 1, j].currentCard != null)
                        {
                            yield return TryDoingDamage(i - 1, j, curTile, true);
                        }
                    }

                    if ((i - 1 >= 0) && (i != thisTurnTiles.GetLength(0) - 1) && (i - 1 != 0))
                    {
                        bool madeDamage = false;

                        for (int k = 1; k <= curTile.currentCard.range; k++)
                        {
                            if (i - k < 0) break;

                            else if(thisTurnTiles[i - k, j].currentCard != null && thisTurnTiles[i - k, j].isPlayersTile)
                            {
                                madeDamage = true;
                                yield return TryDoingDamage(i - k, j, curTile, true);
                                break;
                            }
                        }

                        if (thisTurnTiles[i - 1, j].currentCard == null && !madeDamage)
                        {
                            yield return MoveMainCard(i - 1, j, curTile, true);
                        }
                    }
                }

                else if(!curTile.isPlayersTile && curTile.currentCard.category == Category.Building)
                {
                    if (curTile.secondaryCard != null)
                    {
                        if (thisTurnTiles[i - 1, j].currentCard == null && !curTile.secondaryCard.cardPlayed)
                        {
                            yield return MoveSecondaryCardForward(i - 1, j, curTile, true);
                        }
                    }
                }
            }

            yield return new WaitForSeconds(.2f);
        }

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < thisTurnTiles.GetLength(0); i++)
        {
            for (int j = 0; j < thisTurnTiles.GetLength(1); j++)
            {
                BoardTile curTile = thisTurnTiles[i, j];

                if (curTile.currentCard == null) continue;

                if (curTile.currentCard.cardPlayed) curTile.currentCard.cardPlayed = false;

                if (curTile.secondaryCard != null)
                {
                    if (curTile.secondaryCard.cardPlayed) curTile.secondaryCard.cardPlayed = false;
                }
            }
        }

        currentStateAI = CurrentState.PassTurn;
        
        energyManager.AddEnergyCharge();
        OnChangeCamera?.Invoke(this, CurrentCamera.BatteryCharging);

        yield return StartCoroutine(energyManager.RestartEnergy());
        yield return new WaitForEndOfFrame();
        OnChangeCamera?.Invoke(this, CurrentCamera.ChoosingCards);

        currentlyWorking = false;
    }


    private IEnumerator PlayingCards()
    {
        OnChangeCamera?.Invoke(this, CurrentCamera.PlayingCards);

        thisTurnTiles = boardManager.Tiles;
        currentlyWorking = true;

        for (int i = 0; i < thisTurnTiles.GetLength(0); i++)
        {
            for (int j = 0; j < thisTurnTiles.GetLength(1); j++)
            {
                BoardTile curTile = thisTurnTiles[i, j];

                if (curTile.currentCard == null) continue;

                if (curTile.currentCard.cardPlayed) continue;

                curTile.currentCard.cardPlayed = true;

                if (curTile.isPlayersTile && curTile.currentCard.category == Category.Normal)
                {
                    if (i == 0)
                    {
                        bool madeDamage = false;

                        if (thisTurnTiles[i + 1, j].currentCard != null)
                        {
                            if (thisTurnTiles[i + 1, j].currentCard.category == Category.Building && thisTurnTiles[i + 1, j].secondaryCard == null)
                            {
                                yield return MoveSecondaryCard(i + 1, j, curTile);
                                continue;
                            }
                        }

                        for (int k = 1; k <= curTile.currentCard.range; k++)
                        {
                            if (i + k >= thisTurnTiles.GetLength(0)) break;

                            if (!thisTurnTiles[i + k, j].isPlayersTile)
                            {
                                madeDamage = true;
                                yield return TryDoingDamage(i + k, j, curTile);
                                break;
                            }
                        }

                        if(!madeDamage && thisTurnTiles[i + 1, j].currentCard == null)
                        {
                            yield return MoveMainCard(i + 1, j, curTile);
                        }
                    }

                    if (i + 1 == thisTurnTiles.GetLength(0) - 1)
                    {

                        if (thisTurnTiles[i + 1, j].currentCard != null)
                        {
                            yield return TryDoingDamage(i + 1, j, curTile);
                        }
                    }


                    if ((i + 1 < thisTurnTiles.GetLength(0)) && (i != 0) && (i + 1 != thisTurnTiles.GetLength(0) - 1))
                    {

                        bool madeDamage = false;

                        for (int k = 1; k <= curTile.currentCard.range; k++)
                        {
                            if (i + k >= thisTurnTiles.GetLength(0)) break;

                            if (thisTurnTiles[i + k, j].currentCard != null && !thisTurnTiles[i + k, j].isPlayersTile)
                            {
                                madeDamage = true;
                                yield return TryDoingDamage(i + k, j, curTile);
                                break;
                            }
                        }

                        if (thisTurnTiles[i + 1, j].currentCard == null && !madeDamage)
                        {
                            yield return MoveMainCard(i + 1, j, curTile);
                        }
                    }
                }

                else if (curTile.isPlayersTile && curTile.currentCard.category == Category.Building)
                {
                    if (curTile.secondaryCard != null)
                    {
                        if (thisTurnTiles[i + 1, j].currentCard == null && !curTile.secondaryCard.cardPlayed)
                        {
                            yield return MoveSecondaryCardForward(i + 1, j, curTile);
                        }
                    }
                }
            }

            yield return new WaitForSeconds(.2f);
        }

        for (int i = 0; i < thisTurnTiles.GetLength(0); i++)
        {
            for (int j = 0; j < thisTurnTiles.GetLength(1); j++)
            {
                BoardTile curTile = thisTurnTiles[i, j];

                if (curTile.currentCard == null) continue;

                if (curTile.currentCard.cardPlayed) curTile.currentCard.cardPlayed = false;

                if(curTile.secondaryCard != null)
                {
                    if (curTile.secondaryCard.cardPlayed) curTile.secondaryCard.cardPlayed = false;
                }
            }
        }


        currentTurn++;
        turnText.text = "Current Turn\n" + currentTurn + "/" + maxTurns;
        if (currentTurn > maxTurns)
        {
            FinishGame();
        }

        else
        {
            currentState = CurrentState.PassTurn;
            currentlyWorking = false;
            OnChangeCamera?.Invoke(this, CurrentCamera.ChoosingCards);
        }

        yield return new WaitForEndOfFrame();
    }

    private void DestroyedBuilding(CardBehaviour destroyedCard, bool isOpponent = false)
    {
        if (isOpponent)
        {
            switch (destroyedCard.ability)
            {
                case SpecialAbilities.DrawUP:
                    SubstractExtraCharge(1, true);
                    break;
                case SpecialAbilities.HealCard:
                    extraHPEnemy--;
                    break;
                case SpecialAbilities.EnergyUP:
                    energyManager.extraEnergyAI--;
                    break;
                case SpecialAbilities.AttackUp:
                    extraAttackEnemy--;
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
                    SubstractExtraCharge(1, false);
                    break;
                case SpecialAbilities.HealCard:
                    extraHPPlayer--;
                    break;
                case SpecialAbilities.EnergyUP:
                    energyManager.extraEnergy--;
                    break;
                case SpecialAbilities.AttackUp:
                    extraAttackPlayer--;
                    break;

                default:
                    break;
            }
        }
    }

    private void FinishGame()
    {
        finishGame = true;
        boardManager.FinishGame();

        GetComponent<FinishedGameManager>().FinishedGame(thisTurnTiles);
    }

    private IEnumerator TryDoingDamage(int row, int col, BoardTile curTile, bool isOpponnent = false)
    {
        if (curTile.currentCard.damage > 0 && ((thisTurnTiles[row, col].isPlayersTile && isOpponnent) ||(!thisTurnTiles[row, col].isPlayersTile && !isOpponnent)))
        {
            curTile.currentCard.GetComponent<CardBehaviour>().MakeDamage();
            yield return new WaitForSeconds(.2f);
            juicePlayer.PlayFeedbacks();
            if (thisTurnTiles[row, col].currentCard != null)
            {
                int cardHP = thisTurnTiles[row, col].currentCard.TakeDamage(curTile.currentCard.damage);

                if (cardHP <= 0)
                {
                    if(thisTurnTiles[row, col].currentCard.category == Category.Building)
                    {
                        DestroyedBuilding(thisTurnTiles[row, col].currentCard, isOpponnent);
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

    private IEnumerator MoveMainCard(int row, int col, BoardTile curTile, bool isEnemy = false)
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

    private IEnumerator MoveSecondaryCard(int row, int col, BoardTile curTile)
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

    private IEnumerator MoveSecondaryCardForward(int row, int col, BoardTile curTile, bool isOpponent = false)
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

    private void PuttingCardsOnBoard() 
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, cardMask))
        {
            if (hoveredCard != hit.collider.gameObject)
            {

                if (hoveredCard != null)
                {
                    StartCoroutine(ReturnToDefaultScale(hoveredCard));

                    hoveredCard = hit.collider.gameObject;
                    hoveredCardBehaviour = hoveredCard.GetComponent<CardBehaviour>();

                    energyManager.HoverEnergy(hoveredCardBehaviour.energyRequired);

                    StartCoroutine(CardScale(hoveredCard.transform.localScale, hoveredCard.transform.localScale + new Vector3(1, 0, 1) * .05f, hoveredCard));
                }

                if (hoveredCard == null)
                {
                    hoveredCard = hit.collider.gameObject;
                    hoveredCardBehaviour = hoveredCard.GetComponent<CardBehaviour>();
                    energyManager.HoverEnergy(hoveredCardBehaviour.energyRequired);

                    StartCoroutine(CardScale(hoveredCard.transform.localScale, hoveredCard.transform.localScale + new Vector3(1, 0, 1) * .05f, hoveredCard));
                }
            }
        }
        else
        {
            if (hoveredCard != null)
            {
                StartCoroutine(ReturnToDefaultScale(hoveredCard));

                if (!isCardSelected)
                    energyManager.StopHoveringEnergy();

                hoveredCard = null;
                hoveredCardBehaviour = null;
            }
        }

        if (Input.GetMouseButtonDown(0) && hoveredCard != null)
        {
            if (!energyManager.HoverEnergy(hoveredCardBehaviour.energyRequired))
            {
                StartCoroutine(CardHorizontalMovementNonUsable(hoveredCard));
                return;
            }

            OnCardChose?.Invoke(this, hoveredCard);

            hoveredCard.transform.position = chosenCardPos.position;
            hoveredCard.transform.rotation = chosenCardPos.rotation;
            hoveredCard.transform.parent = chosenCardPos;

            hoveredCard.layer = LayerMask.NameToLayer("Default");

            selectedCard = hoveredCard;

            isCardSelected = true;

            return;
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.mouseScrollDelta.y > 0)
        {
            OnCardChose?.Invoke(this, null);
            return;
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.mouseScrollDelta.y < 0)
        {
            OnCardChoseExited?.Invoke(this, EventArgs.Empty);

            if (selectedCard != null)
            {
                int index = currentCards.IndexOf(selectedCard);
                selectedCard.transform.position = cardsPositions[index].position;
                selectedCard.transform.rotation = cardsPositions[index].rotation;
                selectedCard.transform.parent = cardsPositions[index];
                selectedCard.layer = LayerMask.NameToLayer("DrawnCards");
                selectedCard = null;

                isCardSelected = false;
            }

            return;
        }
    }

    private void GameManager_OnEndTurn(object sender, System.EventArgs e)
    {
        if (currentState != CurrentState.PuttingCardsOnBoard) return;

        StartCoroutine(ToDiscardPile());

        currentState = CurrentState.PlayingCards;
    }

    private IEnumerator CardScale(Vector3 currentScale, Vector3 goalScale, GameObject card)
    {
        float lerp = 0;
        float speed = 4f;

        while (lerp < 1f)
        {
            lerp += Time.deltaTime * speed;
            card.transform.localScale = Vector3.Lerp(currentScale, goalScale, lerp);
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator CardHorizontalMovementNonUsable(GameObject card)
    {
        float lerp = 0;
        float speed = 8f;

        int index = currentCards.IndexOf(card);

        Vector3 originalPos = cardsPositions[index].position;
        Vector3 goalPos = originalPos + Vector3.right * .02f;

        while (lerp < 1f)
        {
            lerp += Time.deltaTime * speed;
            card.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
            yield return new WaitForEndOfFrame();
        }

        lerp = 0;

        originalPos = card.transform.position;
        goalPos = cardsPositions[index].position - Vector3.right * .02f;

        while (lerp < 1f)
        {
            lerp += Time.deltaTime * speed;
            card.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
            yield return new WaitForEndOfFrame();
        }

        lerp = 0;

        originalPos = card.transform.position;
        goalPos = cardsPositions[index].position;

        while (lerp < 1f)
        {
            lerp += Time.deltaTime * speed;
            card.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator ReturnToDefaultScale(GameObject card)
    {
        int index = currentCards.IndexOf(card);
        Vector3 currentScale = card.transform.localScale;

        if (index != -1)
        {

            float lerp = 0;
            float speed = 4f;

            while (lerp < 1f)
            {
                lerp += Time.deltaTime * speed;
                card.transform.localScale = Vector3.Lerp(currentScale, defaultCardScale[index], lerp);
                yield return new WaitForEndOfFrame();
            }
        }
    }

    private void RefreshDrawnCardsPositions()
    {
        for (int i = 0; i < currentCards.Count; i++)
        {
            if (currentCards.Count <= 0)
            {
                break;
            }

            GameObject curCard = currentCards[i];
            Transform curCardTransform = curCard.transform;

            curCardTransform.parent = cardsPositions[i];
            curCardTransform.localPosition = Vector3.zero;
            curCardTransform.localRotation = Quaternion.identity;
        }

        for (int i = 0; i < currentCards.Count; i++)
        {
            defaultCardScale[i] = currentCards[i].transform.localScale;
        }
    }


    private void CreateCards()
    {
        int curCard = 0;

        cardsPrefabs.MMShuffle();

        for (int i = 0; i < drawPileInitialAmount; i++)
        {
            GameObject card = GameObject.Instantiate(cardsPrefabs[curCard], drawPile);

            curCard++;

            if (curCard >= cardsPrefabs.Length)
            {
                cardsPrefabs.MMShuffle();
                curCard = 0;
            }

            card.transform.position = drawPile.position + new Vector3(0, currentDrawPileYOffset, 0);
            currentDrawPileYOffset += .01f;

            availableCards.Add(card);
        }
    }

    private IEnumerator DrawCards()
    {
        currentlyWorking = true;

        currentCards = new List<GameObject>();

        for (int i = 0; i < amountOfDrawCards; i++)
        {
            if (availableCards.Count <= 0)
            {
                Debug.Log("Out of available cards");
                if (discardCards.Count > 0)
                {
                    yield return StartCoroutine(FromDiscardPileToDrawPile());
                }
                else
                    break;
            }

            if (currentCards.Count == maxAmountOfCards) break;

            GameObject curCard = availableCards[availableCards.Count - 1];
            Transform curCardTransform = curCard.transform;

            currentCards.Add(curCard);
            availableCards.RemoveAt(availableCards.Count - 1);

            currentDrawPileYOffset -= .01f;

            curCardTransform.parent = cardsPositions[currentCardAmount];

            float lerp = 0;
            float lerpSpeed = 6.5f;
            Vector3 originalPos = curCardTransform.localPosition;
            Quaternion originalRot = curCardTransform.localRotation;

            if (!cardWhooshSounds.isPlaying)
            {
                ChangeWhooshSound();
                cardWhooshSounds.Play();
            }

            curCard.GetComponent<CardBehaviour>().ShowCard();

            while(lerp < 1)
            {
                lerp += Time.deltaTime * lerpSpeed;

                curCardTransform.localPosition = Vector3.Lerp(originalPos, Vector3.zero, lerp);
                curCardTransform.localRotation = Quaternion.Slerp(originalRot, Quaternion.identity, lerp);

                yield return new WaitForEndOfFrame();
            }

            curCard.layer = LayerMask.NameToLayer("DrawnCards");

            curCard.GetComponent<CardBehaviour>().hp += extraHPPlayer;
            curCard.GetComponent<CardBehaviour>().damage += extraAttackPlayer;

            currentCardAmount++;

            yield return new WaitForSeconds(0.1f);
        }

        currentState = CurrentState.PuttingCardsOnBoard;

        RefreshDrawnCardsPositions();

        currentlyWorking = false;
    }

    private IEnumerator ToDiscardPile()
    {
        currentlyWorking = true;

        for (int i = 0; i < currentCards.Count; i++)
        {
            GameObject curCard = currentCards[i];
            Transform curCardTransform = curCard.transform;

            discardCards.Add(curCard);

            curCardTransform.parent = discardPile;

            float lerp = 0;
            float lerpSpeed = 6.5f;
            Vector3 originalPos = curCardTransform.localPosition;
            Quaternion originalRot = curCardTransform.localRotation;

            curCard.GetComponent<CardBehaviour>().PutOnPile();

            while (lerp < 1)
            {
                lerp += Time.deltaTime * lerpSpeed;

                curCardTransform.localPosition = Vector3.Lerp(originalPos, Vector3.zero + Vector3.up * currentDiscardPileYOffset, lerp);
                curCardTransform.localRotation = Quaternion.Slerp(originalRot, Quaternion.identity, lerp);

                yield return new WaitForEndOfFrame();
            }

            currentDiscardPileYOffset += .01f;

            curCard.layer = LayerMask.NameToLayer("Default");

            currentCardAmount--;

            yield return new WaitForEndOfFrame();
        }

        currentCards = null;

        currentlyWorking = false;
    }

    private IEnumerator FromDiscardPileToDrawPile()
    {
        discardCards.MMShuffle();

        for (int i = 0; i < discardCards.Count; i++)
        {
            if (discardCards.Count <= 0)
            {
                break;
            }


            GameObject curCard = discardCards[i];
            Transform curCardTransform = curCard.transform;

            availableCards.Add(curCard);

            curCardTransform.parent = drawPile;
            float lerp = 0;
            float lerpSpeed = 9f;
            Vector3 originalPos = curCardTransform.localPosition;
            Quaternion originalRot = curCardTransform.localRotation;

            while(lerp < 1)
            {
                lerp += Time.deltaTime * lerpSpeed;

                curCardTransform.localPosition = Vector3.Lerp(originalPos, Vector3.zero + Vector3.up * currentDrawPileYOffset, lerp);
                curCardTransform.localRotation = Quaternion.Slerp(originalRot, Quaternion.identity, lerp);

                yield return new WaitForEndOfFrame();
            }

            currentDiscardPileYOffset -= .01f;
            currentDrawPileYOffset += .01f;

            yield return new WaitForEndOfFrame();
        }

        discardCards = new List<GameObject>();
    }

    public void AddExtraDraw(int amount, bool isOpponent)
    {
        if (!isOpponent)
        {
            amountOfDrawCards += amount;

            amountOfDrawCards = Mathf.Min(amountOfDrawCards, cardsPositions.Length);
        }
        else
        {
            amountOfDrawCardsAI += amount;

            amountOfDrawCardsAI = Mathf.Min(amountOfDrawCardsAI, cardsPositions.Length);
        }
    }

    public void SubstractExtraCharge(int amount, bool isOpponent)
    {
        if (!isOpponent)
        {
            amountOfDrawCards -= amount;

            amountOfDrawCards = Mathf.Max(amountOfDrawCards, 5);
        }

        else
        {
            amountOfDrawCardsAI -= amount;

            amountOfDrawCardsAI = Mathf.Max(amountOfDrawCards, 5);
        }
    }


}
