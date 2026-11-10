using Swole.API.Unity.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class customanimtest : MonoBehaviour
{

    public bool start;

    public bool reset;

    new public CustomAnimation animation;

    public GameObject rootObject;

    private CustomAnimation.Player player;

    void Start()
    {

    }

    private void Update()
    {
        
        /*if (start)
        {

            start = false;

            if (rootObject == null) rootObject = gameObject;

            CustomAnimator animator = rootObject.AddOrGetComponent<CustomAnimator>();

            player = animator.GetAnimationPlayer(animation);
            player.paused = false;

        }*/

        if (reset)
        {

            reset = false;

            if (player != null)
            {

                player.paused = false;
                player.Time = 0;

            }

        }

    }

}
