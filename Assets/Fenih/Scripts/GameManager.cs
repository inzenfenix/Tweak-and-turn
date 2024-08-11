using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static event EventHandler OnEndTurn;

    [SerializeField] private LayerMask endTurnLayer;

    [SerializeField] private AudioSource bgMusic1;
    [SerializeField] private AudioSource bgMusic2;

    [SerializeField] private AudioSource bellSound;

    private void OnEnable()
    {
        bgMusic1.PlayDelayed(1);
        bgMusic2.PlayDelayed(1);
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            if(Physics.Raycast(cameraRay, float.MaxValue, endTurnLayer))
            {
                bellSound.Play();
                OnEndTurn?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
