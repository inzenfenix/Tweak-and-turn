using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyManager : MonoBehaviour
{
    [Header("Energy Numerator Objects")]
    [SerializeField] private GameObject[] energyBatteries;
    [SerializeField] TMPro.TextMeshPro extraEnergyText;

    private Material[] energyMats;

    [Header("\nEnergy Configs")]
    [SerializeField] private int startEnergy = 2;
    [SerializeField] private int maxEnergy = 6;
    private readonly int maxAcumulatedEnergy = 3;

    [HideInInspector] public int currentEnergy;
    [HideInInspector] public int currentEnergyAI;

    [HideInInspector] public int currentRechargeEnergy;
    [HideInInspector] public int currentRechargeEnergyAI;

    [HideInInspector] public int extraEnergy;
    [HideInInspector] public int extraEnergyAI;

    [Header("\nEnergy Audio")]
    [SerializeField] private AudioSource energySound;
    [SerializeField] private AudioClip[] energyAudios;

    [Header("\nEnergy phases's colors")]
    [SerializeField] private Color cyan = new Color(68, 155, 184, 255f) / 150f;
    [SerializeField] private Color gray = new Color(61, 64, 63, 255f) / 150f;
    [SerializeField] private Color red = new Color(176, 62, 77, 255f) / 150f;


    private void Awake()
    {
        energyMats = new Material[energyBatteries.Length];

        for(int i = 0; i < energyBatteries.Length; i++)
        {
            energyMats[i] = energyBatteries[i].GetComponent<MeshRenderer>().material;
        }

        currentEnergy = currentEnergyAI = currentRechargeEnergy = currentRechargeEnergyAI = startEnergy;

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
                //Red
                energyMats[i].color = red;
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
                    energyMats[i].color = red;
                }
            }

            
        }

        return true;
    }

    public bool UseEnergy(int amount)
    {
        if(amount > currentEnergy) return false;

        currentEnergy -= amount;

        if (currentEnergy <= energyMats.Length)
        {
            for (int i = 0; i < currentEnergy; i++)
            {
                //Cyan
                energyMats[i].color = cyan;
            }
        }

        else
        {
            for (int i = 0; i < energyMats.Length; i++)
            {
                energyMats[i].color = cyan;
            }

            extraEnergyText.text = "+" + $"{currentEnergy - energyMats.Length}";
        }

        for(int i = currentEnergy; i < maxEnergy; i++)
        {
            //Black
            energyMats[i].color = gray;
        }

        return true;
    }

    private IEnumerator RefreshEnergy()
    {

        for (int i = 0; i < energyBatteries.Length; i++)
        {
            energyMats[i].color = gray;
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
                energyMats[i].color = cyan;
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
                energyMats[i].color = cyan;
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
                energyMats[i].color = cyan;
            }
        }

        else
        {
            for (int i = 0; i < maxEnergy; i++)
            {
                energyMats[i].color = cyan;
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

