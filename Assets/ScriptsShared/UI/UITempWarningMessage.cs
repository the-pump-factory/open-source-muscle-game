using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class UITempWarningMessage : MonoBehaviour
{

    public static List<string> shownMessages = new List<string>();

    public string id;

    public GameObject rootObject;

    protected void Awake()
    {
        if (rootObject == null) rootObject = gameObject;
        if (shownMessages.Contains(id))
        {
            rootObject.SetActive(false); 
            return;
        }

        shownMessages.Add(id); 
    }

}
