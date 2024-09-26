using System;
using System.Collections;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board creation configs")]
    [SerializeField] private Transform startPoint;

    private float XCardSeparation = .5f;
    private float ZCardSeparation = .2f;

    [SerializeField] private int columnAmount = 4;
    [SerializeField] private int rowAmount = 10;

    [Header("\nBoard Tile Prefab")]
    [SerializeField] private GameObject cardHolderPrefab;

    private BoardTile[,] tiles;

    [Header("\nHover Tile Symbols")]
    [SerializeField] private Transform acceptSymbol;
    [SerializeField] private Transform rejectSymbol;

    private GameObject selectedCard;
    private GameObject selectedTile;

    private bool selectingBoardTile = false;

    [Header("\nHover Tile properties")]
    [SerializeField] private LayerMask tileMask;
    [SerializeField] private Color hoverCardColor;

    public static event EventHandler<GameObject> OnPutCardOnBoard;

    bool playableTile = false;
    private BoardTile hoveredTile = null;

    private EnergyManager energyManager;
    private TurnSystemBehaviour turnSystemBehaviour;

    private int curHPHealing = 1;

    private Color playerColor;
    private Color enemyColor;

    

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

        tiles = new BoardTile[rowAmount, columnAmount];

        int startTilesPerPlayer = (columnAmount * rowAmount) / 2;

        int currentTileNum = 0;
        for (int i = 0; i < rowAmount; i++)
        {
            for (int j = 0; j < columnAmount; j++)
            {

                GameObject currentTile = GameObject.Instantiate(cardHolderPrefab, startPoint);
                currentTile.name = "BoardSpace";

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
                tiles[i, j].isUsable = true;

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
            BoardTile tile = HoverTile();

            if (Input.GetMouseButtonDown(0) && tile != null)
            {
                PlayCard(tile);
            }
        }
    }

    private BoardTile HoverTile()
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(cameraRay, out RaycastHit hit, float.MaxValue, tileMask))
        {
            if (selectedTile != hit.collider.gameObject)
            {

                TileIndices tileIndices = hit.collider.gameObject.GetComponent<TileIndices>();
                selectedTile = hit.collider.gameObject;

                int i = tileIndices.row;
                int j = tileIndices.col;


                hoveredTile = tiles[i, j];
                CardBehaviour chosenCard = selectedCard.GetComponent<CardBehaviour>();

                if (!hoveredTile.isUsable)
                {
                    acceptSymbol.gameObject.SetActive(false);
                    rejectSymbol.gameObject.SetActive(true);
                    rejectSymbol.position = hoveredTile.tileHolder.transform.position + Vector3.up * .05f;
                    return null;
                }

                else if (hoveredTile.isPlayersTile &&
                    (chosenCard.category == Category.Normal || chosenCard.category == Category.Building)
                    && hoveredTile.currentCard == null)
                {
                    rejectSymbol.gameObject.SetActive(false);
                    acceptSymbol.gameObject.SetActive(true);
                    acceptSymbol.position = hoveredTile.tileHolder.transform.position + Vector3.up * .05f;
                    return hoveredTile;
                }

                else if (hoveredTile.currentCard != null && chosenCard.category == Category.Throwable)
                {
                    rejectSymbol.gameObject.SetActive(false);
                    acceptSymbol.gameObject.SetActive(true);
                    acceptSymbol.position = hoveredTile.currentCard.transform.position + Vector3.up * 1f;
                    return hoveredTile;
                }

                else
                {
                    acceptSymbol.gameObject.SetActive(false);
                    rejectSymbol.gameObject.SetActive(true);
                    rejectSymbol.position = hoveredTile.tileHolder.transform.position + Vector3.up * .05f;
                    return null;
                }
            }

            else
            {
                if (hoveredTile.isPlayersTile && hoveredTile.currentCard == null && hoveredTile.isUsable)
                    return hoveredTile;

                else
                    return null;
            }
        }

        else
        {
            selectedTile = null;
            hoveredTile = null;

            acceptSymbol.gameObject.SetActive(false);
            rejectSymbol.gameObject.SetActive(false);

            return null;
        }
    }

    private void PlayCard(BoardTile tile)
    {
        if (!energyManager.UseEnergy(selectedCard.GetComponent<CardBehaviour>().energyRequired))
        {
            return;
        }

        selectedCard.transform.position = tile.tileHolder.transform.position + Vector3.up * .05f;
        selectedCard.transform.rotation = Quaternion.identity;
        selectedCard.transform.parent = selectedTile.transform;

        selectedCard.transform.localScale += new Vector3(1, 0, 1) * 0.04f;

        selectedCard.GetComponent<CardBehaviour>().PutCardOnBoard();

        CardBehaviour chosenCardBehaviour = selectedCard.GetComponent<CardBehaviour>();

        chosenCardBehaviour.ResetRotationCardBoardVisual();

        if (chosenCardBehaviour.characterBoardVisual != null)
            chosenCardBehaviour.characterBoardVisual.SetActive(true);

        if (chosenCardBehaviour.category != Category.Throwable)
            tile.currentCard = selectedCard.GetComponent<CardBehaviour>();

        if (tile.currentCard.category == Category.Normal)
        {
            int row = tile.tileHolder.GetComponent<TileIndices>().row;
            int col = tile.tileHolder.GetComponent<TileIndices>().col;

            tile.currentCard.CardNextAction(tiles, tile, row, col, "+");
            ChangeStateTile(row, col, 1, 1, false);
        }

        else if (chosenCardBehaviour.category == Category.Throwable && tile.currentCard != null)
        {
            switch (chosenCardBehaviour.ability)
            {
                case (SpecialAbilities.HealCard):
                    tile.currentCard.GetComponent<CardBehaviour>().HealDamage(curHPHealing);
                    StartCoroutine(turnSystemBehaviour.DuplicateCardToDiscardPile(selectedCard));
                    Destroy(selectedCard);
                    break;
            }
        }

        if (chosenCardBehaviour.category == Category.Building)
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

    public void ChangeStateTile(int row, int col, int deltaRow, int deltaCol, bool isUsable)
    {
        BoardTile tile = tiles[row, col];
        tile.isUsable = isUsable;

        for(int i = row - deltaRow; i <= row + deltaRow; i++)
        {
            //All conditions that we don't want to change the state of the tile, like the last rows or out-of-bounds rows
            if (i >= tiles.GetLength(0) - 1 || i <= 0 || i == row) continue;

            tiles[i, col].isUsable = isUsable;
        }

        //Don't change the tiles at the end of each extreme side
        if (row != 0 && row != tiles.GetLength(0) - 1)
        {
            for (int j = col - deltaCol; j <= col + deltaCol; j++)
            {
                //All conditions that we don't want to change the state of the tile
                if (j >= tiles.GetLength(1) || j < 0 || j == col) continue;

                tiles[row, j].isUsable = isUsable;
            }
        }
    }

    public void CheckUsableTiles()
    {
        for(int i = 0; i < tiles.GetLength(0); i++)
        {
            for(int j = 0; j < tiles.GetLength(1); j++)
            {
                BoardTile tile = tiles[i, j];

                if (!tile.isUsable || tile.currentCard != null)
                {
                    tile.ChangeTileColor(hoverCardColor);
                }
            }
        }
    }

    public void StopCheckingUsableTiles()
    {
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                BoardTile tile = tiles[i, j];

                tile.ResetTileColor();
            }
        }
    }
}
