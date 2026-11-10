using Swole.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class listtest : MonoBehaviour
{
    public UIRecyclingList list;

    public bool test;
    public int count = 25;

    public void Update()
    {
        if (test)
        {
            test = false;
            for(int a = 0; a < count; a++) list.AddNewMember($"MEMBER {a}", null);
            list.Refresh();

        }
    }

}
