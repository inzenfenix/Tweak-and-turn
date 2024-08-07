using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoardTile
{
    public Color color;
    public CardBehaviour currentCard;
    public GameObject tileHolder;
    public bool isPlayersTile;

    private Material tileMat;

    public BoardTile(UnityEngine.Color color, GameObject tileHolder, bool isPlayersTile)
    {
        this.color = color;
        this.tileHolder = tileHolder;
        this.isPlayersTile = isPlayersTile;
        this.currentCard = null;

        tileMat = tileHolder.GetComponent<MeshRenderer>().material;

        tileMat.color = color;
    }

    public void ChangeTileColor(Color color)
    {
        tileMat.color = color;
    }


}
