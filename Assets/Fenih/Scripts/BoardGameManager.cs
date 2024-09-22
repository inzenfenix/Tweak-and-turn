using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGameManager : MonoBehaviour
{
    public static event EventHandler OnEndTurn;

    [SerializeField] private LayerMask endTurnLayer;

    [SerializeField] private AudioSource bgMusic1;
    [SerializeField] private AudioSource bgMusic2;

    [SerializeField] private AudioSource bellSound;

    [SerializeField] private MeshRenderer turnItemMeshRenderer;
    private Material turnItemMaterial;
    private bool hoveringTurnItem = false;

    [SerializeField] private GameObject endTurnText;

    private void OnEnable()
    {
        bgMusic1.PlayDelayed(1);
        bgMusic2.PlayDelayed(1);

        hoveringTurnItem = false;
        turnItemMaterial = turnItemMeshRenderer.material;
        endTurnText.SetActive(false);
    }

    private void Update()
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(cameraRay, float.MaxValue, endTurnLayer) && !hoveringTurnItem)
        {
            turnItemMaterial.SetFloat("_OutlineWidth", .8f);
            hoveringTurnItem = true;
            endTurnText.SetActive(true);
        }

        else if(!Physics.Raycast(cameraRay, float.MaxValue, endTurnLayer) && hoveringTurnItem)
        {
            turnItemMaterial.SetFloat("_OutlineWidth", .0f);
            endTurnText.SetActive(false);
            hoveringTurnItem = false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(cameraRay, float.MaxValue, endTurnLayer))
            {
                bellSound.Play();
                OnEndTurn?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
