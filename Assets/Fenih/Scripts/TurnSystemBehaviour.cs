using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CurrentState
{
    DrawingCards,
    PuttingCardsOnBoard,
    PlayingCards,
    PassTurn
};

public class TurnSystemBehaviour : MonoBehaviour
{
    private List<GameObject> availableCards;

    private List<GameObject> discardCards;

    private List<GameObject> currentCards;

    private int amountOfDrawCards = 5;    

    private int drawPileInitialAmount = 15;

    private int currentCardAmount = 0;

    [SerializeField] private GameObject[] cardsPrefabs;
    [SerializeField] private Transform[] cardsPositions;

    private int maxAmountOfCards;

    [SerializeField] private Transform drawPile;
    private float currentDrawPileYOffset = 0;

    [SerializeField] private Transform discardPile;
    private float currentDiscardPileYOffset = 0;

    private int maxTurns = 5;
    private int currentTurn = 1;

    private CurrentState currentState;

    private float enemyTimeTest = 0;


    private void Awake()
    {
        currentCardAmount = 0;
        currentTurn = 1;

        availableCards = new List<GameObject>();
        discardCards = new List<GameObject>();

        maxAmountOfCards = cardsPositions.Length;

        currentState = CurrentState.DrawingCards;

        CreateCards();

        DrawCards();
    }

    private void OnEnable()
    {
        GameManager.OnEndTurn += GameManager_OnEndTurn;
    }
    private void OnDisable()
    {
        GameManager.OnEndTurn -= GameManager_OnEndTurn;
    }

    private void Update()
    {
        switch(currentState)
        {
            case (CurrentState.PlayingCards):
                break;

            case (CurrentState.PassTurn):
                if (enemyTimeTest > 0) enemyTimeTest -= Time.deltaTime;

                else
                {
                    currentState = CurrentState.DrawingCards;
                    DrawCards();
                }

                break;
            default: 
                break;
        }
    }

    private void GameManager_OnEndTurn(object sender, System.EventArgs e)
    {
        if (currentState != CurrentState.PuttingCardsOnBoard) return;

        ToDiscardPile();

        currentState = CurrentState.PlayingCards;
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
                if (discardCards.Count >= 5)
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

            currentCardAmount++;
        }

        currentState = CurrentState.PuttingCardsOnBoard;
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
