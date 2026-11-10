using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Disabler : MonoBehaviour
{
    public float delay = 1;
    private float t; 

    public void Awake()
    {
        t = delay;
    }
    public void Update()
    {
        if (t > 0)
        {
            t -= Time.deltaTime;
            if (t <= 0) gameObject.SetActive(false);
        }
    }
}
