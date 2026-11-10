using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MagicaCloth;

using Swole;
using Swole.API.Unity.Animation;

[ExecuteAlways]
public class CharacterPhysicsControl : MonoBehaviour
{

#if UNITY_EDITOR
    public bool scan;
#endif

    public bool enableAllLocatedPhysics = true;
    public bool disableAllUnlocatedPhysics = true;

    public List<PhysicsTeam> physicsTeams;  

    protected virtual void Awake()
    {
        if (Application.isPlaying)
        {
            ReinitializePhysics(swole.Settings.disableAllCharacterPhysics);
        }
    }
    protected virtual void OnEnable()
    {
        if (Application.isPlaying)
        {
            ReinitializePhysics(swole.Settings.disableAllCharacterPhysics);
            swole.ListenForSettingsChanged(ListenForSettingsChanged);  
        }
    }
    protected virtual void OnDisable()
    {
        if (Application.isPlaying)
        {
            swole.StopListeningForSettingsChanged(ListenForSettingsChanged);
        }
    }
    protected virtual void ListenForSettingsChanged(SwoleSettings prevSettings, SwoleSettings newSettings)
    {
        if (prevSettings.disableAllCharacterPhysics == newSettings.disableAllCharacterPhysics) return;
        ReinitializePhysics(newSettings.disableAllCharacterPhysics); 
    }
    public virtual void ReinitializePhysics(bool disableAllCharacterPhysics)
    {
        if (Application.isPlaying)
        {
            if (disableAllUnlocatedPhysics || disableAllCharacterPhysics)
            {
                var physics = gameObject.GetComponentsInChildren<PhysicsTeam>(true);
                if (physics != null)
                {
                    foreach (var team in physics) if (team != null && (disableAllCharacterPhysics || physicsTeams == null || !physicsTeams.Contains(team))) team.enabled = false;
                }

                //AnimationEditor.DisableAllProceduralAnimationComponents(gameObject);
            }

            if (enableAllLocatedPhysics && !disableAllCharacterPhysics)
            {
                if (physicsTeams != null)
                {
                    foreach (var team in physicsTeams) if (team != null) team.enabled = true;
                } 
            }
        }
    }

#if UNITY_EDITOR
    public virtual void Update()
    {
        if (scan)
        {
            scan = false;

            physicsTeams = new List<PhysicsTeam>(gameObject.GetComponentsInChildren<PhysicsTeam>(true));
        }
    }
#endif

}
