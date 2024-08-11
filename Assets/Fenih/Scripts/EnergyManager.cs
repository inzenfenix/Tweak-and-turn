using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyManager : MonoBehaviour
{
    [SerializeField] private GameObject[] energyBatteries;

    private Material[] energyMats;

    [SerializeField] private int startEnergy = 2;
    [SerializeField] private int maxEnergy = 6;
    private int maxAcumulatedEnergy = 3;
    private int acumulatedEnergy;
    [HideInInspector] public int currentEnergy;
    [HideInInspector] public int currentEnergyAI;

    [HideInInspector] public int currentRechargeEnergy;
    [HideInInspector] public int currentRechargeEnergyAI;


    [SerializeField] TMPro.TextMeshPro extraEnergyText;

    [HideInInspector] public int extraEnergy;
    [HideInInspector] public int extraEnergyAI;

    [SerializeField] private AudioSource energySound;
    [SerializeField] private AudioClip[] energyAudios;


    private void Awake()
    {
        energyMats = new Material[energyBatteries.Length];

        for(int i = 0; i < energyBatteries.Length; i++)
        {
            energyMats[i] = energyBatteries[i].GetComponent<MeshRenderer>().material;
            //energyMats[i].color = Color.black;
        }

        currentEnergy = currentEnergyAI = currentRechargeEnergy = currentRechargeEnergyAI = startEnergy;
        acumulatedEnergy = Mathf.Clamp(currentEnergy, 0, maxAcumulatedEnergy);

        extraEnergy = extraEnergyAI = 0;

        StartCoroutine(RefreshEnergy());
    }


    public bool HoverEnergy(int amount)
    {
        if (amount > currentEnergy) return false;

        if (currentEnergy <= maxEnergy)
        {
            for (int i = amount - 1; i >= 0; i--)
            {
                energyMats[i].color = Color.red;
            }
        }

        else
        {
            int extraEnergy = (-amount) + (currentEnergy - maxEnergy);

            if(extraEnergy > 0)
            {
                extraEnergyText.text = "+" + (extraEnergy);
            }

            else
            {
                extraEnergyText.text = "+0";
                extraEnergy *= -1;

                if (extraEnergy > 6) return false;

                for (int i = energyMats.Length - 1; i > energyMats.Length - 1 - extraEnergy; i--)
                {
                    energyMats[i].color = Color.red;
                }
            }

            
        }

        return true;
    }

    public bool UseEnergy(int amount)
    {
        if(amount > currentEnergy) return false;

        currentEnergy -= amount;
        acumulatedEnergy = currentEnergy;

        if (currentEnergy <= energyMats.Length)
        {
            for (int i = 0; i < currentEnergy; i++)
            {
                energyMats[i].color = Color.cyan;
            }
        }

        else
        {
            for (int i = 0; i < energyMats.Length; i++)
            {
                energyMats[i].color = Color.cyan;
            }

            extraEnergyText.text = "+" + $"{currentEnergy - energyMats.Length}";
        }

        for(int i = currentEnergy; i < maxEnergy; i++)
        {
            energyMats[i].color = Color.black;
        }

        return true;
    }

    private IEnumerator RefreshEnergy()
    {

        for (int i = 0; i < energyBatteries.Length; i++)
        {
            energyMats[i].color = Color.black;
        }


        yield return new WaitForSeconds(.1f);

        if (currentEnergy <= energyMats.Length)
        {
            for (int i = 0; i < currentEnergy; i++)
            {
                energySound.clip = energyAudios[i];
                energySound.Play();
                yield return new WaitForEndOfFrame();
                yield return new WaitForSeconds(.15f);
                energyMats[i].color = Color.cyan;
            }
        }

        else
        {
            for (int i = 0; i < energyMats.Length; i++)
            {
                energySound.clip = energyAudios[i];
                energySound.Play();

                yield return new WaitForEndOfFrame();
                yield return new WaitForSeconds(.15f);
                energyMats[i].color = Color.cyan;
            }

            extraEnergyText.text = "+" + $"{currentEnergy - energyMats.Length}";
        }

        yield return new WaitForSeconds(.25f);
    }

    public void AddEnergyCharge()
    {
        currentRechargeEnergy++;

        currentRechargeEnergy = currentRechargeEnergyAI = Mathf.Min(maxEnergy, currentRechargeEnergy);
    }

    public IEnumerator RestartEnergy()
    {
        
        currentEnergy = currentRechargeEnergy;
        currentEnergyAI = currentRechargeEnergyAI;

        currentEnergy += extraEnergy;
        currentEnergyAI += extraEnergyAI;

        yield return StartCoroutine(RefreshEnergy());
    }

    public void StopHoveringEnergy()
    {
        if (currentEnergy <= maxEnergy)
        {
            for (int i = 0; i < currentEnergy; i++)
            {
                energyMats[i].color = Color.cyan;
            }
        }

        else
        {
            for (int i = 0; i < maxEnergy; i++)
            {
                energyMats[i].color = Color.cyan;
            }

            extraEnergyText.text = "+" + (currentEnergy - maxEnergy);
        }
    }

    public void AddExtraEnergy(int amount, bool toOpponent = false)
    {
        if (!toOpponent)
            extraEnergy += amount;

        else
            extraEnergyAI += amount;
    }

    public void SubstractExtraEnergy(int amount, bool toOpponent = false)
    {
        if (!toOpponent)
            extraEnergy -= amount;

        else
            extraEnergyAI -= amount;
    }
}

