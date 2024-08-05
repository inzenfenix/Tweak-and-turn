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

    private void Awake()
    {
        XCardSeparation = 0;
        ZCardSeparation = 0;

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

                XCardSeparation += .6f;

                UnityEngine.Color curColor = UnityEngine.Color.blue;
                bool isPlayersTile = true;

                if (currentTileNum >= startTilesPerPlayer)
                {
                    curColor = UnityEngine.Color.red;
                    isPlayersTile = false;
                }

                tiles[i, j] = new BoardTile(curColor, currentTile, isPlayersTile);

                currentTileNum++;
            }

            XCardSeparation = 0f;
            ZCardSeparation += .5f;
        }
    }
}
