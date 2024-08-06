using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyManager : MonoBehaviour
{
    [SerializeField] private GameObject[] energyBatteries;

    private Material[] energyMats;

    [SerializeField] private int startEnergy = 3;
    [SerializeField] private int maxEnergy = 6;
    private int maxAcumulatedEnergy = 3;
    private int acumulatedEnergy;
    [HideInInspector] public int currentEnergy;
    

    private void Awake()
    {
        energyMats = new Material[energyBatteries.Length];

        for(int i = 0; i < energyBatteries.Length; i++)
        {
            energyMats[i] = energyBatteries[i].GetComponent<MeshRenderer>().material;
            energyMats[i].color = Color.black;
        }

        for (int i = 0; i < startEnergy; i++)
        {
            energyMats[i] = energyBatteries[i].GetComponent<MeshRenderer>().material;
            energyMats[i].color = Color.cyan;
        }

        currentEnergy = startEnergy;
        acumulatedEnergy = Mathf.Clamp(currentEnergy, 0, maxAcumulatedEnergy);
    }


    public bool HoverEnergy(int amount)
    {
        if (amount > currentEnergy) return false;

        for(int i = amount - 1; i >= 0; i--)
        {
            energyMats[i].color = Color.red;
        }

        return true;
    }

    public bool UseEnergy(int amount)
    {
        if(amount > currentEnergy) return false;

        currentEnergy -= amount;
        acumulatedEnergy = currentEnergy;

        for (int i = 0; i < currentEnergy; i++)
        {
            energyMats[i].color = Color.cyan;
        }

        for(int i = currentEnergy; i < maxEnergy; i++)
        {
            energyMats[i].color = Color.black;
        }

        return true;
    }

    public void StopHoveringEnergy()
    {
        for (int i = 0; i < currentEnergy; i++)
        {
            energyMats[i].color = Color.cyan;
        }
    }
}