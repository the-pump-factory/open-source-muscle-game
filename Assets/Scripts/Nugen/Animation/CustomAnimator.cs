using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen.animation
{

    public abstract class CustomAnimator : MonoBehaviour
    {

        public delegate void AnimationListener();

        public event AnimationListener preAnimate;

        public event AnimationListener postAnimate;

        public virtual void PreAnimateUpdate()
        {

            preAnimate?.Invoke();

        }

        public virtual void PostAnimateUpdate()
        {

            postAnimate?.Invoke();

        }

    }

}
