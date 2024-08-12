using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private Transform startPoint;

    private float XCardSeparation = .5f;
    private float ZCardSeparation = .2f;

    [SerializeField] private int columnAmount = 4;
    [SerializeField] private int rowAmount = 10;

    [SerializeField] private GameObject cardHolderPrefab;

    private BoardTile[,] tiles;

    [SerializeField] private Transform acceptSymbol;
    [SerializeField] private Transform rejectSymbol;

    private GameObject selectedCard;
    private GameObject selectedTile;

    private bool selectingBoardTile = false;

    [SerializeField] private LayerMask tileMask;

    public static event EventHandler<GameObject> OnPutCardOnBoard;

    bool playableTile = false;
    private BoardTile hoveredTile = null;

    private EnergyManager energyManager;

    private TurnSystemBehaviour turnSystemBehaviour;

    private int curHPHealing = 1;

    private UnityEngine.Color playerColor;
    private UnityEngine.Color enemyColor;

    private bool finishGame = false;

    public BoardTile[,] Tiles
    {
        get { return tiles; }
        private set { tiles = value; }
    }

    private void Awake()
    {
        curHPHealing = 1;

        XCardSeparation = 0;
        ZCardSeparation = 0;

        energyManager = GetComponent<EnergyManager>();

        turnSystemBehaviour = GetComponent<TurnSystemBehaviour>();

        playerColor = turnSystemBehaviour.playerColor;
        enemyColor = turnSystemBehaviour.enemyColor;

        tiles = new BoardTile[rowAmount,columnAmount];

        int startTilesPerPlayer = (columnAmount * rowAmount) / 2;

        int currentTileNum = 0;
        for(int i = 0; i < rowAmount; i++)
        {
            for(int j = 0; j < columnAmount; j++)
            {

                GameObject currentTile = GameObject.Instantiate(cardHolderPrefab, startPoint);
                currentTile.transform.localPosition = Vector3.zero;

                currentTile.transform.localPosition += Vector3.right * XCardSeparation + Vector3.forward * ZCardSeparation;

                XCardSeparation += .48f;

                UnityEngine.Color curColor = playerColor;
                bool isPlayersTile = true;

                if (currentTileNum >= startTilesPerPlayer)
                {
                    curColor = enemyColor;
                    isPlayersTile = false;
                }

                tiles[i, j] = new BoardTile(curColor, currentTile, isPlayersTile);

                currentTile.GetComponent<TileIndices>().row = i;
                currentTile.GetComponent<TileIndices>().col = j;

                currentTileNum++;
            }

            XCardSeparation = 0f;
            ZCardSeparation += .53f;
        }
    }

    private void OnEnable()
    {
        TurnSystemBehaviour.OnCardChose += TurnSystemBehaviour_OnCardChose;
        TurnSystemBehaviour.OnCardChoseExited += TurnSystemBehaviour_OnCardChoseExited;
    }

    private void OnDisable()
    {
        TurnSystemBehaviour.OnCardChose -= TurnSystemBehaviour_OnCardChose;
        TurnSystemBehaviour.OnCardChoseExited -= TurnSystemBehaviour_OnCardChoseExited;
    }

    private void Update()
    {
        if (finishGame) return;

        if (selectingBoardTile)
        {
            BoardTile tile = null;

            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, tileMask))
            {
                if (selectedTile != hit.collider.gameObject)
                {

                    TileIndices tileIndices = hit.collider.gameObject.GetComponent<TileIndices>();
                    selectedTile = hit.collider.gameObject;

                    int i = tileIndices.row;
                    int j = tileIndices.col;


                    hoveredTile = tile = tiles[i, j];
                    CardBehaviour chosenCard = selectedCard.GetComponent<CardBehaviour>();

                    if (tile.isPlayersTile && (i == 0 || turnSystemBehaviour.currentTurn <= 3) && chosenCard.category == Category.Normal && tile.currentCard == null)
                    {
                        playableTile = true;
                        rejectSymbol.gameObject.SetActive(false);
                        acceptSymbol.gameObject.SetActive(true);
                        acceptSymbol.position = tile.tileHolder.transform.position + Vector3.up * .05f;
                    }

                    else if (tile.isPlayersTile && i == 1 && chosenCard.category == Category.Building && tile.currentCard == null)
                    {
                        playableTile = true;
                        rejectSymbol.gameObject.SetActive(false);
                        acceptSymbol.gameObject.SetActive(true);
                        acceptSymbol.position = tile.tileHolder.transform.position + Vector3.up * .05f;
                    }

                    else if (tile.currentCard != null && chosenCard.category == Category.Throwable)
                    {
                        playableTile = true;
                        rejectSymbol.gameObject.SetActive(false);
                        acceptSymbol.gameObject.SetActive(true);
                        acceptSymbol.position = tile.currentCard.transform.position + Vector3.up * 1f;
                    }

                    else
                    {
                        acceptSymbol.gameObject.SetActive(false);
                        rejectSymbol.gameObject.SetActive(true);
                        rejectSymbol.position = tile.tileHolder.transform.position + Vector3.up * .05f;
                        playableTile = false;
                    }
                }
            }

            else
            {
                selectedTile = null;
                hoveredTile = null;

                acceptSymbol.gameObject.SetActive(false);
                rejectSymbol.gameObject.SetActive(false);

                playableTile = false;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (playableTile)
                {
                    if(!energyManager.UseEnergy(selectedCard.GetComponent<CardBehaviour>().energyRequired))
                    {
                        return;
                    }

                    selectedCard.transform.position = hoveredTile.tileHolder.transform.position + Vector3.up * .05f;
                    selectedCard.transform.rotation = Quaternion.identity;
                    selectedCard.transform.parent = selectedTile.transform;

                    selectedCard.transform.localScale += new Vector3(1, 0, 1) * 0.04f;

                    selectedCard.GetComponent<CardBehaviour>().PutCardOnBoard();

                    CardBehaviour chosenCardBehaviour = selectedCard.GetComponent<CardBehaviour>();

                    if(chosenCardBehaviour.category != Category.Throwable)
                        hoveredTile.currentCard = selectedCard.GetComponent<CardBehaviour>();

                    else if(chosenCardBehaviour.category == Category.Throwable && hoveredTile.currentCard != null)
                    {
                        switch(chosenCardBehaviour.ability)
                        {
                            case (SpecialAbilities.HealCard):
                                hoveredTile.currentCard.GetComponent<CardBehaviour>().HealDamage(curHPHealing);
                                Destroy(selectedCard);
                                break;
                        }
                    }

                    if(chosenCardBehaviour.category == Category.Building)
                    {
                        switch (chosenCardBehaviour.ability)
                        {
                            case SpecialAbilities.DrawUP:
                                turnSystemBehaviour.AddExtraDraw(1, false);
                                break;
                            case SpecialAbilities.HealCard:
                                turnSystemBehaviour.extraHPPlayer++;
                                break;
                            case SpecialAbilities.EnergyUP:
                                energyManager.extraEnergy++;
                                break;
                            case SpecialAbilities.AttackUp:
                                turnSystemBehaviour.extraAttackPlayer++;
                                break;

                            default:
                                break;
                        }
                    }

                    selectedTile = null;
                    hoveredTile = null;

                    acceptSymbol.gameObject.SetActive(false);
                    rejectSymbol.gameObject.SetActive(false);


                    OnPutCardOnBoard?.Invoke(this, selectedCard);
                    selectedCard = null;

                    return;
                }
            }
        }
    }

    private void TurnSystemBehaviour_OnCardChose(object sender, GameObject e)
    {
        if (e == null) return;

        StartCoroutine(WaitForCard(e));
    }

    private IEnumerator WaitForCard(GameObject card)
    {
        yield return new WaitForEndOfFrame();

        selectingBoardTile = true;
        selectedCard = card;
    }

    private void TurnSystemBehaviour_OnCardChoseExited(object sender, System.EventArgs e)
    {
        selectedCard = null;
        selectedTile = null;
        selectingBoardTile = false;

        acceptSymbol.gameObject.SetActive(false);
        rejectSymbol.gameObject.SetActive(false);
    }

    public void FinishGame()
    {
        finishGame = true;
    }
}
