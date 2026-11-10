#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MagicaCloth;
using Swole.API.Unity;

public class BreastSizePhysicsSync : MonoBehaviour
{

    public MuscularRenderedCharacter character;
    private IMuscularBasic muscleController;
    public MagicaBoneSpring[] springs;
    public BreastPhysicsBasic[] physics;

    protected void Awake()
    {
        if (character != null) muscleController = character;
        if (muscleController == null) muscleController = gameObject.GetComponent<IMuscularBasic>();
    }

    void Update()
    {
        if (muscleController != null && (springs != null || physics != null))
        {
            float factor = Mathf.Clamp01((muscleController.BreastPresence - 0.05f) / 0.35f); 
            if (springs != null)
            {
                foreach (var spring in springs)
                {
                    if (spring == null || !spring.enabled) continue;
                    spring.BlendWeight = factor;
                }
            }
            if (physics != null)
            {
                foreach (var comp in physics)
                {
                    if (comp == null || !comp.enabled) continue;
                    comp.mix = factor; 
                }
            }
        }
    }
}

#endif