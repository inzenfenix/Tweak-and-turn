using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class OrtoCameraBehaviour : MonoBehaviour
{
    private void Update()
    {
        transform.SetPositionAndRotation(Camera.main.transform.position, Camera.main.transform.rotation);
    }
}
