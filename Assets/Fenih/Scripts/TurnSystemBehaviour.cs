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

public class TurnSystemBehaviour : MonoBehaviour
{
    public static event EventHandler<GameObject> OnCardChose;
    public static event EventHandler OnCardChoseExited;

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
        if(e == selectedCard)
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
        switch(currentState)
        {
            case (CurrentState.DrawingCards):
                DrawCards();
                break;

            case (CurrentState.PuttingCardsOnBoard):
                PuttingCardsOnBoard();
                break;

            case (CurrentState.EnemiesTurn):
                EnemiesTurn();
                break;

            case (CurrentState.PlayingCards):
                PlayingCards();
                break;

            case (CurrentState.PassTurn):
                if (enemyTimeTest > 0) enemyTimeTest -= Time.deltaTime;

                else
                {
                    energyManager.AddEnergyCharge();
                    energyManager.RestartEnergy();
                    currentState = CurrentState.DrawingCards;
                    
                }
                break;
            
            default: 
                break;
        }
    }

    private void EnemiesTurn()
    {

    }

    private void PlayingCards()
    {
        thisTurnTiles = boardManager.Tiles;

        for (int i = 0; i < thisTurnTiles.GetLength(0); i++)
        {
            for(int j = 0; j < thisTurnTiles.GetLength(1); j++)
            {
                BoardTile curTile = thisTurnTiles[i, j];

                if (curTile.currentCard == null) continue;

                if (curTile.currentCard.cardPlayed) continue;

                curTile.currentCard.cardPlayed = true;

                if (curTile.isPlayersTile && curTile.currentCard.category == Category.Normal)
                {
                    if(i + 1 < thisTurnTiles.GetLength(0))
                    {
                        if (thisTurnTiles[i + 1, j].currentCard != null)
                        {
                            if(curTile.currentCard.damage > 0 && !thisTurnTiles[i + 1, j].isPlayersTile)
                            {
                                int enemyHP = thisTurnTiles[i + 1, j].currentCard.TakeDamage(curTile.currentCard.damage);

                                if(enemyHP <= 0)
                                {
                                    Destroy(thisTurnTiles[i + 1, j].currentCard.gameObject);
                                    thisTurnTiles[i + 1, j].currentCard = null;
                                }
                            }
                        }

                        else if(thisTurnTiles[i + 1, j].currentCard == null)
                        {
                            thisTurnTiles[i + 1, j].currentCard = curTile.currentCard;
                            curTile.currentCard = null;

                            if(!thisTurnTiles[i + 1, j].isPlayersTile)
                            {
                                thisTurnTiles[i + 1, j].isPlayersTile = true;
                                thisTurnTiles[i + 1, j].ChangeTileColor(Color.blue);
                            }

                            thisTurnTiles[i + 1, j].currentCard.transform.position = thisTurnTiles[i + 1, j].tileHolder.transform.position + Vector3.up * .05f;
                        }
                    }
                }

                else if(!curTile.isPlayersTile && curTile.currentCard.category == Category.Normal)
                {
                    if (i - 1 >= 0)
                    {
                        if (thisTurnTiles[i - 1, j].currentCard != null)
                        {
                            if (curTile.currentCard.damage > 0 && thisTurnTiles[i + 1, j].isPlayersTile)
                            {
                                int allyHP = thisTurnTiles[i + 1, j].currentCard.TakeDamage(curTile.currentCard.damage);

                                if (allyHP <= 0)
                                {
                                    Destroy(thisTurnTiles[i + 1, j].currentCard.gameObject);
                                    thisTurnTiles[i + 1, j].currentCard = null;
                                }
                            }
                        }

                        else
                        {
                            thisTurnTiles[i - 1, j].currentCard = curTile.currentCard;
                            curTile.currentCard = null;

                            if (thisTurnTiles[i - 1, j].isPlayersTile)
                            {
                                thisTurnTiles[i - 1, j].isPlayersTile = false;
                                thisTurnTiles[i - 1, j].ChangeTileColor(Color.red);
                            }

                            thisTurnTiles[i - 1, j].currentCard.transform.position = thisTurnTiles[i - 1, j].tileHolder.transform.position + Vector3.up * .05f;
                        }
                    }
                }
            }
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
        enemyTimeTest = 4;
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

                if(!isCardSelected)
                    energyManager.StopHoveringEnergy();

                hoveredCard = null;
                hoveredCardBehaviour = null;
            }
        }

        if (Input.GetMouseButtonDown(0) && hoveredCard != null)
        {
            if(!energyManager.HoverEnergy(hoveredCardBehaviour.energyRequired))
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

        ToDiscardPile();

        currentState = CurrentState.EnemiesTurn;
    }

    private IEnumerator CardScale(Vector3 currentScale, Vector3 goalScale, GameObject card)
    {
        float lerp = 0;
        float speed = 4f;

        while(lerp < 1f)
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

    private void DrawCards()
    {

        currentCards = new List<GameObject>();

        for(int i = 0; i < amountOfDrawCards; i++)
        {
            if (availableCards.Count <= 0)
            {
                Debug.Log("Out of available cards");
                if (discardCards.Count > 0)
                    FromDiscardPileToDrawPile();
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
            curCardTransform.localPosition = Vector3.zero;
            curCardTransform.localRotation = Quaternion.identity;

            curCard.layer = LayerMask.NameToLayer("DrawnCards");

            currentCardAmount++;
        }

        currentState = CurrentState.PuttingCardsOnBoard;

        for(int i = 0; i < currentCards.Count; i++)
        {
            defaultCardScale[i] = currentCards[i].transform.localScale;
        }

        RefreshDrawnCardsPositions();
    }

    private void ToDiscardPile()
    {
        for (int i = 0; i < currentCards.Count; i++)
        {
            GameObject curCard = currentCards[i];
            Transform curCardTransform = curCard.transform;

            discardCards.Add(curCard);

            curCardTransform.parent = discardPile;
            curCardTransform.localPosition = Vector3.zero;
            curCardTransform.localRotation = Quaternion.identity;

            curCardTransform.localPosition += Vector3.up * currentDiscardPileYOffset;

            currentDiscardPileYOffset += .01f;

            curCard.layer = LayerMask.NameToLayer("Default");

            currentCardAmount--;
        }

        currentCards = null;
    }

    private void FromDiscardPileToDrawPile()
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
            curCardTransform.localPosition = Vector3.zero;
            curCardTransform.localRotation = Quaternion.identity;

            curCardTransform.localPosition += Vector3.up * currentDrawPileYOffset;

            currentDiscardPileYOffset -= .01f;
            currentDrawPileYOffset += .01f;

        }

        discardCards = new List<GameObject>();
    }
}
