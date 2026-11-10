using Swole.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class recyclingListTester : MonoBehaviour
{

    public UIRecyclingList list;
     
    public int count;

    // Start is called before the first frame update
    void Start()
    {
        if (list == null) list = gameObject.GetComponentInChildren<UIRecyclingList>();
        if (list != null)
        {
            for (int a = 0; a < count; a++) list.AddNewMember($"mem_{a}", null);
            list.Refresh();
        }
    }
}
