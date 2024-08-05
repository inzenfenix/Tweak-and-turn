using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrtoCameraBehaviour : MonoBehaviour
{
    private void LateUpdate()
    {
        this.transform.SetPositionAndRotation(Vector3.zero, Camera.main.transform.rotation);
    }
}
