using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] GameObject lookingCardsOnHandCamera;
    [SerializeField] GameObject puttingCardOnBoardCamera;
    [SerializeField] GameObject lookingAtBatteryCamera;

    [SerializeField] GameObject ortoCamera;

    private void OnEnable()
    {
        TurnSystemBehaviour.OnCardChose += TurnSystemBehaviour_OnCardChose;
        TurnSystemBehaviour.OnCardChoseExited += TurnSystemBehaviour_OnCardChoseExited;

        TurnSystemBehaviour.OnChangeCamera += TurnSystemBehaviour_OnChangeCamera;
    }

    

    private void OnDisable()
    {
        TurnSystemBehaviour.OnCardChose -= TurnSystemBehaviour_OnCardChose;
        TurnSystemBehaviour.OnCardChoseExited -= TurnSystemBehaviour_OnCardChoseExited;

        TurnSystemBehaviour.OnChangeCamera -= TurnSystemBehaviour_OnChangeCamera;
    }

    private void TurnSystemBehaviour_OnChangeCamera(object sender, CurrentCamera e)
    {
        lookingAtBatteryCamera.SetActive(false);
        puttingCardOnBoardCamera.SetActive(false);
        lookingCardsOnHandCamera.SetActive(false);

        switch(e)
        {
            case(CurrentCamera.PlayingCards):
                puttingCardOnBoardCamera.SetActive(true);
                break;
            case (CurrentCamera.ChoosingCards):
                lookingCardsOnHandCamera.SetActive(true);
                break;
            case (CurrentCamera.BatteryCharging):
                lookingAtBatteryCamera.SetActive(true);
                break;
            default:
                break;
        }
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