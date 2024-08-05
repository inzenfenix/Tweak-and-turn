using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] GameObject puttingCardOnBoardCamera;

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

    private void TurnSystemBehaviour_OnCardChoseExited(object sender, System.EventArgs e)
    {
        puttingCardOnBoardCamera.SetActive(false);
    }

    private void TurnSystemBehaviour_OnCardChose(object sender, GameObject e)
    {
        puttingCardOnBoardCamera.SetActive(true);
    }
}