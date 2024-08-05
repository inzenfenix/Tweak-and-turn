using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static event EventHandler OnEndTurn;

    [SerializeField] private LayerMask endTurnLayer;

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            if(Physics.Raycast(cameraRay, float.MaxValue, endTurnLayer))
            {
                OnEndTurn?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
