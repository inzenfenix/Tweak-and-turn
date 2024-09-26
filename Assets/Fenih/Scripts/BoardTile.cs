using UnityEngine;

[System.Serializable]
public class BoardTile
{
    public Color color;

    public CardBehaviour currentCard;
    public CardBehaviour secondaryCard;

    public GameObject tileHolder;
    public bool isPlayersTile;

    public bool isUsable;

    private readonly Material tileMat;

    private Color defColor;


    public BoardTile(Color color, GameObject tileHolder, bool isPlayersTile)
    {
        this.color = color;
        this.tileHolder = tileHolder;
        this.isPlayersTile = isPlayersTile;
        this.currentCard = null;

        tileMat = tileHolder.GetComponent<MeshRenderer>().material;

        tileMat.color = defColor = color;
    }

    public void ChangeTileColor(Color color, bool isDefColor = false)
    {
        tileMat.color = color;

        if(isDefColor)
            defColor = color;
    }

    public void ResetTileColor()
    {
        tileMat.color = defColor;
    }

}
