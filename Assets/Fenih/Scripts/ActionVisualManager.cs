using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionVisualManager : MonoBehaviour
{
    [SerializeField] private GameObject bow;
    [SerializeField] private GameObject sword;
    [SerializeField] private GameObject arrow;

    public void EnableBow()
    {
        bow.SetActive(true);
        sword.SetActive(false);
        arrow.SetActive(false);
    }

    public void EnableArrow(string dir)
    {
        bow.SetActive(false);
        sword.SetActive(false);
        arrow.SetActive(true);

        switch (dir)
        {
            case "Forward":
                arrow.transform.eulerAngles = new Vector3(0, -180, 0);
                break;
            case "Backward":
                arrow.transform.eulerAngles = new Vector3(0, 0, 0);
                break;
            case "Right":
                arrow.transform.eulerAngles = new Vector3(0, -90, 0);
                break;
            case "Left":
                arrow.transform.eulerAngles = new Vector3(0, 90, 0);
                break;
            default:
                break;
        }
    }

    public void EnableSword()
    {
        bow.SetActive(false);
        sword.SetActive(true);
        arrow.SetActive(false);
    }

    public void DisableAll()
    {
        bow.SetActive(false);
        sword.SetActive(false);
        arrow.SetActive(false);
    }
}
