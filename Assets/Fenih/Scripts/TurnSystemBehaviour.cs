using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
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

    private int drawPileInitialAmount = 50;

    private int currentCardAmount = 0;

    [SerializeField] private GameObject[] cardsPrefabs;
    [SerializeField] private Transform[] cardsPositions;

    private int maxAmountOfCards;

    [SerializeField] private Transform drawPile;
    private float currentDrawPileYOffset = 0;

    [SerializeField] private Transform discardPile;
    private float currentDiscardPileYOffset = 0;

    private int maxTurns = 15;
    [HideInInspector] public int currentTurn = 1;

    private CurrentState currentState;
    private CurrentState currentStateAI;

    private float enemyTimeTest = 0;

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

    private void Awake()
    {
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

    private void DrawCardsAI()
    {
        currentCardsAI = new List<GameObject>();

        for (int i = 0; i < amountOfDrawCards; i++)
        {
            int randomCardIndex = UnityEngine.Random.Range(0, cardsPrefabs.Length);

            GameObject newCard = GameObject.Instantiate(cardsPrefabs[randomCardIndex], Vector3.up * 30f + Vector3.forward * 20f, Quaternion.identity);

            currentCardsAI.Add(newCard);
        }

        currentStateAI = CurrentState.PuttingCardsOnBoard;
    }

    private IEnumerator PuttingCardsOnBoardAI()
    {
        thisTurnTiles = boardManager.Tiles;
        currentlyWorking = true;

        int energy = energyManager.currentRechargeEnergy;

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

        //Add normal cards
        for (int i = 0; i < currentCardsAI.Count; i++)
        {
            CardBehaviour card = currentCardsAI[i].GetComponent<CardBehaviour>();

            if ((card.category == Category.Normal || card.category == Category.Special) && card.energyRequired < energy)
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

            if (card.category == Category.Building && card.energyRequired < energy)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (!winning[j] && thisTurnTiles[buildingRow, j].currentCard == null)
                    {
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

                if (card.energyRequired < energy && (card.category == Category.Normal || card.category == Category.Special))
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

        while(lerp < 1)
        {
            lerp += Time.deltaTime * speed;

            card.transform.localPosition = Vector3.Lerp(originalPos, Vector3.zero + Vector3.up * .5f, lerp);
            card.transform.localRotation = Quaternion.Lerp(originalRot, Quaternion.identity, lerp);

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
                        if (thisTurnTiles[i - 1, j].currentCard != null)
                        {
                            if (thisTurnTiles[i - 1, j].currentCard.category == Category.Building && thisTurnTiles[i - 2, j].currentCard == null)
                            {
                                thisTurnTiles[i - 2, j].currentCard = curTile.currentCard;
                                curTile.currentCard = null;

                                float lerp = 0;
                                float speed = 7.5f;

                                Vector3 originalPos = thisTurnTiles[i - 2, j].currentCard.transform.position;
                                Vector3 goalPos = thisTurnTiles[i - 2, j].tileHolder.transform.position + Vector3.up * .05f;

                                while (lerp < 1)
                                {
                                    lerp += Time.deltaTime * speed;
                                    thisTurnTiles[i - 2, j].currentCard.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
                                    yield return new WaitForEndOfFrame();
                                }

                                if (!thisTurnTiles[i - 2, j].isPlayersTile)
                                {
                                    thisTurnTiles[i - 2, j].isPlayersTile = false;
                                    thisTurnTiles[i - 2, j].ChangeTileColor(enemyColor);
                                }

                                thisTurnTiles[i - 2, j].currentCard.transform.parent = thisTurnTiles[i - 2, j].tileHolder.transform;

                                yield return new WaitForEndOfFrame();
                            }
                        }

                        else
                        {
                            thisTurnTiles[i - 1, j].currentCard = curTile.currentCard;
                            curTile.currentCard = null;

                            float lerp = 0;
                            float speed = 7.5f;

                            Vector3 originalPos = thisTurnTiles[i - 1, j].currentCard.transform.position;
                            Vector3 goalPos = thisTurnTiles[i - 1, j].tileHolder.transform.position + Vector3.up * .05f;

                            while (lerp < 1)
                            {
                                lerp += Time.deltaTime * speed;
                                thisTurnTiles[i - 1, j].currentCard.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
                                yield return new WaitForEndOfFrame();
                            }

                            if (!thisTurnTiles[i - 1, j].isPlayersTile)
                            {
                                thisTurnTiles[i - 1, j].isPlayersTile = false;
                                thisTurnTiles[i - 1, j].ChangeTileColor(enemyColor);
                            }

                            thisTurnTiles[i - 1, j].currentCard.transform.parent = thisTurnTiles[i - 1, j].tileHolder.transform;
                            yield return new WaitForEndOfFrame();
                        }
                    }

                    if (i - 1 == 0)
                    {
                        if (thisTurnTiles[i - 1, j].currentCard != null)
                        {
                            if (curTile.currentCard.damage > 0 && !thisTurnTiles[i - 1, j].isPlayersTile)
                            {
                                juicePlayer.PlayFeedbacks();
                                int enemyHP = thisTurnTiles[i - 1, j].currentCard.TakeDamage(curTile.currentCard.damage);

                                if (enemyHP <= 0)
                                {
                                    Destroy(thisTurnTiles[i - 1, j].currentCard.gameObject);
                                    thisTurnTiles[i - 1, j].currentCard = null;
                                }
                            }
                        }
                    }

                    if ((i - 1 >= 0) && (i != thisTurnTiles.GetLength(0) - 1) && (i - 1 != 0))
                    {
                        if (thisTurnTiles[i - 1, j].currentCard != null)
                        {
                            if (curTile.currentCard.damage > 0 && thisTurnTiles[i - 1, j].isPlayersTile)
                            {
                                juicePlayer.PlayFeedbacks();
                                int allyHP = thisTurnTiles[i - 1, j].currentCard.TakeDamage(curTile.currentCard.damage);

                                if (allyHP <= 0)
                                {
                                    Destroy(thisTurnTiles[i - 1, j].currentCard.gameObject);
                                    thisTurnTiles[i - 1, j].currentCard = null;
                                }
                            }
                        }

                        else
                        {
                            thisTurnTiles[i - 1, j].currentCard = curTile.currentCard;
                            curTile.currentCard = null;

                            float lerp = 0;
                            float speed = 7.5f;

                            Vector3 originalPos = thisTurnTiles[i - 1, j].currentCard.transform.position;
                            Vector3 goalPos = thisTurnTiles[i - 1, j].tileHolder.transform.position + Vector3.up * .05f;

                            while (lerp < 1)
                            {
                                lerp += Time.deltaTime * speed;
                                thisTurnTiles[i - 1, j].currentCard.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
                                yield return new WaitForEndOfFrame();
                            }

                            if (thisTurnTiles[i - 1, j].isPlayersTile)
                            {
                                thisTurnTiles[i - 1, j].isPlayersTile = false;
                                thisTurnTiles[i - 1, j].ChangeTileColor(enemyColor);
                            }

                            thisTurnTiles[i - 1, j].currentCard.transform.parent = thisTurnTiles[i - 1, j].tileHolder.transform;
                            yield return new WaitForEndOfFrame();
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < thisTurnTiles.GetLength(0); i++)
        {
            for (int j = 0; j < thisTurnTiles.GetLength(1); j++)
            {
                BoardTile curTile = thisTurnTiles[i, j];

                if (curTile.currentCard == null) continue;

                if (curTile.currentCard.cardPlayed) curTile.currentCard.cardPlayed = false;
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
                        if (thisTurnTiles[i + 1, j].currentCard != null)
                        {
                            if (thisTurnTiles[i + 1, j].currentCard.category == Category.Building && thisTurnTiles[i + 2, j].currentCard == null)
                            {
                                thisTurnTiles[i + 2, j].currentCard = curTile.currentCard;
                                curTile.currentCard = null;

                                float lerp = 0;
                                float speed = 7.5f;

                                Vector3 originalPos = thisTurnTiles[i + 2, j].currentCard.transform.position;
                                Vector3 goalPos = thisTurnTiles[i + 2, j].tileHolder.transform.position + Vector3.up * .05f;

                                while (lerp < 1)
                                {
                                    lerp += Time.deltaTime * speed;
                                    thisTurnTiles[i + 2, j].currentCard.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
                                    yield return new WaitForEndOfFrame();
                                }

                                if (!thisTurnTiles[i + 2, j].isPlayersTile)
                                {
                                    thisTurnTiles[i + 2, j].isPlayersTile = true;
                                    thisTurnTiles[i + 2, j].ChangeTileColor(playerColor);
                                }

                                thisTurnTiles[i + 2, j].currentCard.transform.parent = thisTurnTiles[i + 2, j].tileHolder.transform;

                                yield return new WaitForEndOfFrame();
                            }
                        }

                        else
                        {
                            thisTurnTiles[i + 1, j].currentCard = curTile.currentCard;
                            curTile.currentCard = null;

                            float lerp = 0;
                            float speed = 7.5f;

                            Vector3 originalPos = thisTurnTiles[i + 1, j].currentCard.transform.position;
                            Vector3 goalPos = thisTurnTiles[i + 1, j].tileHolder.transform.position + Vector3.up * .05f;

                            while (lerp < 1)
                            {
                                lerp += Time.deltaTime * speed;
                                thisTurnTiles[i + 1, j].currentCard.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
                                yield return new WaitForEndOfFrame();
                            }

                            if (!thisTurnTiles[i + 1, j].isPlayersTile)
                            {
                                thisTurnTiles[i + 1, j].isPlayersTile = true;
                                thisTurnTiles[i + 1, j].ChangeTileColor(playerColor);
                            }

                            thisTurnTiles[i + 1, j].currentCard.transform.parent = thisTurnTiles[i + 1, j].tileHolder.transform;
                            yield return new WaitForEndOfFrame();
                        }
                    }

                    if (i + 1 == thisTurnTiles.GetLength(0) - 1)
                    {
                        if (thisTurnTiles[i + 1, j].currentCard != null)
                        {
                            if (curTile.currentCard.damage > 0 && !thisTurnTiles[i + 1, j].isPlayersTile)
                            {
                                int enemyHP = thisTurnTiles[i + 1, j].currentCard.TakeDamage(curTile.currentCard.damage);

                                juicePlayer.PlayFeedbacks();

                                if (enemyHP <= 0)
                                {
                                    Destroy(thisTurnTiles[i + 1, j].currentCard.gameObject);

                                    thisTurnTiles[i + 1, j].currentCard = null;
                                    
                                    
                                }
                            }
                        }
                    }


                    if ((i + 1 < thisTurnTiles.GetLength(0)) && (i != 0) && (i + 1 != thisTurnTiles.GetLength(0) - 1))
                    {
                        if (thisTurnTiles[i + 1, j].currentCard != null)
                        {
                            if (curTile.currentCard.damage > 0 && !thisTurnTiles[i + 1, j].isPlayersTile)
                            {
                                int enemyHP = thisTurnTiles[i + 1, j].currentCard.TakeDamage(curTile.currentCard.damage);

                                juicePlayer.PlayFeedbacks();

                                if (enemyHP <= 0)
                                {
                                    Destroy(thisTurnTiles[i + 1, j].currentCard.gameObject);
                                    thisTurnTiles[i + 1, j].currentCard = null;
                                }
                            }
                        }

                        else if (thisTurnTiles[i + 1, j].currentCard == null)
                        {
                            thisTurnTiles[i + 1, j].currentCard = curTile.currentCard;
                            curTile.currentCard = null;

                            float lerp = 0;
                            float speed = 7.5f;

                            Vector3 originalPos = thisTurnTiles[i + 1, j].currentCard.transform.position;
                            Vector3 goalPos = thisTurnTiles[i + 1, j].tileHolder.transform.position + Vector3.up * .05f;

                            while (lerp < 1)
                            {
                                lerp += Time.deltaTime * speed;
                                thisTurnTiles[i + 1, j].currentCard.transform.position = Vector3.Lerp(originalPos, goalPos, lerp);
                                yield return new WaitForEndOfFrame();
                            }


                            if (!thisTurnTiles[i + 1, j].isPlayersTile)
                            {
                                thisTurnTiles[i + 1, j].isPlayersTile = true;
                                thisTurnTiles[i + 1, j].ChangeTileColor(playerColor);
                            }

                            thisTurnTiles[i + 1, j].currentCard.transform.parent = thisTurnTiles[i + 1, j].tileHolder.transform;

                            yield return new WaitForEndOfFrame();
                        }
                    }
                }
            }

            yield return new WaitForSeconds(.1f);
        }

        for (int i = 0; i < thisTurnTiles.GetLength(0); i++)
        {
            for (int j = 0; j < thisTurnTiles.GetLength(1); j++)
            {
                BoardTile curTile = thisTurnTiles[i, j];

                if (curTile.currentCard == null) continue;

                if (curTile.currentCard.cardPlayed) curTile.currentCard.cardPlayed = false;
            }
        }


        currentTurn++;
        currentState = CurrentState.PassTurn;
        currentlyWorking = false;
        OnChangeCamera?.Invoke(this, CurrentCamera.ChoosingCards);

        yield return new WaitForEndOfFrame();
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

                    StartCoroutine(CardScale(hoveredCard.transform.localScale, hoveredCard.transform.localScale + new Vector3(1, 0, 1) * .2f, hoveredCard));
                }

                if (hoveredCard == null)
                {
                    hoveredCard = hit.collider.gameObject;
                    hoveredCardBehaviour = hoveredCard.GetComponent<CardBehaviour>();
                    energyManager.HoverEnergy(hoveredCardBehaviour.energyRequired);

                    StartCoroutine(CardScale(hoveredCard.transform.localScale, hoveredCard.transform.localScale + new Vector3(1, 0, 1) * .2f, hoveredCard));
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

        if (Input.GetKeyDown(KeyCode.W))
        {
            OnCardChose?.Invoke(this, null);
            return;
        }

        if (Input.GetKeyDown(KeyCode.S))
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

            while(lerp < 1)
            {
                lerp += Time.deltaTime * lerpSpeed;

                curCardTransform.localPosition = Vector3.Lerp(originalPos, Vector3.zero, lerp);
                curCardTransform.localRotation = Quaternion.Slerp(originalRot, Quaternion.identity, lerp);

                yield return new WaitForEndOfFrame();
            }

            curCard.layer = LayerMask.NameToLayer("DrawnCards");

            currentCardAmount++;

            yield return new WaitForSeconds(0.1f);
        }

        currentState = CurrentState.PuttingCardsOnBoard;

        for (int i = 0; i < currentCards.Count; i++)
        {
            defaultCardScale[i] = currentCards[i].transform.localScale;
        }

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
}
