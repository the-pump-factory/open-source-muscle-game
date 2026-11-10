using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnitySpring.SemiImplicitEuler;

public class springtest : MonoBehaviour
{

    private Spring spring;

    public bool test;
    public float val;

    void Start()
    {
        // interpolate from -10f to 10f
        spring = new Spring()
        {
            startValue = 0,
            endValue = 2f,
            stiffness = 15,
            damping = 2,
            mass = 6

        };
    }
     
    void Update()
    {
        var x = spring.Evaluate(Time.deltaTime); 
        transform.position = new Vector3(x, 0f, 0f);

        if (test) 
        { 
            
            //spring.UpdateEndValue(val);

            test = false;
        
        }
    }

}
