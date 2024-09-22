using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomBehaviour : MonoBehaviour
{
    [SerializeField] private MMF_Player juicePlayer;

    private void OnEnable()
    {
        BoardGameManager.OnEndTurn += GameManager_OnEndTurn;
    }

    private void OnDisable()
    {
        BoardGameManager.OnEndTurn -= GameManager_OnEndTurn;
    }

    private void GameManager_OnEndTurn(object sender, System.EventArgs e)
    {
        juicePlayer.PlayFeedbacks();
    }
}
