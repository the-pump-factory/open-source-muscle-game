using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen.animation
{

    public abstract class AnimationBasedComponent : MonoBehaviour
    {

        protected CustomAnimator animator;

        public CustomAnimator Animator => animator;

        public bool HasAnimator => animator != null;

        protected bool subscribed = false;

        public bool IsSubscribed => subscribed;

        protected abstract void Subscribe();

        protected abstract void Unsubscribe();

        protected virtual void OnDestroy()
        {

            Unsubscribe();

        }

        public void SetAnimator(CustomAnimator animator)
        {

            Unsubscribe();

            this.animator = animator;

            Subscribe();

        }

    }

}
