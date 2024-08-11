using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FinishedGameManager : MonoBehaviour
{
    [SerializeField] private GameObject finishedPanel;

    [SerializeField] private TextMeshProUGUI yourPoints;
    [SerializeField] private TextMeshProUGUI enemyPoints;

    [SerializeField] private TextMeshProUGUI winner;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        finishedPanel.SetActive(false);

        restartButton.onClick.AddListener(ResetGame);

        restartButton.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        restartButton.onClick.RemoveAllListeners();
    }

    public void FinishedGame(BoardTile[,] tiles)
    {
        StartCoroutine(FinishedGameScene(tiles));
    }

    private void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator FinishedGameScene(BoardTile[,] tiles)
    {
        finishedPanel.SetActive(true);

        int playersPoints = 0;
        int opponentsPoints = 0;

        for(int i = 0; i < tiles.GetLength(0); i++)
        {
            for(int j = 0; j < tiles.GetLength(1); j++)
            {
                if (tiles[i, j].isPlayersTile) playersPoints++; 
                else opponentsPoints++;
            }
        }

        yield return new WaitForSeconds(.01f);

        int showPlayerPoints = 0;
        int showOpponentPoints = 0;

        for(int i = 0; i <= playersPoints; i++)
        {
            yourPoints.text = showPlayerPoints.ToString();
            yield return new WaitForSeconds(.01f);
            showPlayerPoints++;
        }

        yield return new WaitForSeconds(.1f);

        for (int i = 0; i <= opponentsPoints; i++)
        {
            enemyPoints.text = showOpponentPoints.ToString();
            yield return new WaitForSeconds(.1f);
            showOpponentPoints++;
        }

        yield return new WaitForSeconds(.1f);

        if (playersPoints > opponentsPoints)
        {
            winner.text = "YOU WIN! CONGRATS!";
        }

        else if (playersPoints < opponentsPoints)
        {
            winner.text = "YOU LOST! TRY AGAIN!";
        }

        else
            winner.text = "You draw... somehow";

        yield return new WaitForSeconds(.1f);

        restartButton.gameObject.SetActive(true);
    }
}
