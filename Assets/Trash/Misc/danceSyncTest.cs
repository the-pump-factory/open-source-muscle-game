using Swole.API.Unity.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class danceSyncTest : MonoBehaviour
{

    public string triggerName = "play";

    public Animator[] animators;
    public CustomAnimator[] customAnimators;

    public bool play;
    public bool stopAndReset;

    private Dictionary<Animator, Tuple<Vector3, Quaternion>> startStates = new Dictionary<Animator, Tuple<Vector3, Quaternion>>();

    private void Awake()
    {
        if (animators != null)
        {

            foreach (var animator in animators) if (animator != null)
                {
                    startStates[animator] = new Tuple<Vector3, Quaternion>(animator.transform.position, animator.transform.rotation);
                }

        }
    }

    void Update()
    {
        if (play)
        {
            play = false;
            Play();
        }

        if (stopAndReset)
        {
            stopAndReset = false;
            StopAndReset(); 
        }
    }

    public void Play()
    { 
        if (animators != null)
        {
            foreach (var animator in animators) if (animator != null)
                {
                    animator.Rebind();
                    animator.Update(0f);
                    animator.transform.SetPositionAndRotation(startStates[animator].Item1, startStates[animator].Item2);
                    animator.SetTrigger(triggerName);
                }
        }
        if (customAnimators != null)
        {
            foreach (var customAnimator in customAnimators) if (customAnimator != null)
                {
                    for (int a = 0; a < customAnimator.LayerCount; a++)
                    {
                        var layer = customAnimator.GetLayer(a);
                        var state = layer.ActiveState;
                        if (state == null) continue;
                        state.RestartAnims();
                        var mc = layer.GetMotionControllerUnsafe(state.MotionControllerIndex);
                        if (mc == null) continue;
                        mc.BaseSpeed = 1;
                    }
                }
        }
    }

    public void StopAndReset()
    { 
        if (animators != null)
        {
            foreach (var animator in animators) if (animator != null)
                {
                    animator.Rebind();
                    animator.Update(0f);
                    animator.transform.SetPositionAndRotation(startStates[animator].Item1, startStates[animator].Item2);
                }
        }
        if (customAnimators != null)
        {
            foreach (var customAnimator in customAnimators) if (customAnimator != null)
                {
                    for (int a = 0; a < customAnimator.LayerCount; a++)
                    {
                        var layer = customAnimator.GetLayer(a);
                        var state = layer.ActiveState;
                        if (state == null) continue;
                        state.RestartAnims();
                        var mc = layer.GetMotionControllerUnsafe(state.MotionControllerIndex);
                        if (mc == null) continue;
                        mc.BaseSpeed = 0;
                    }
                }
        }
    }

}
