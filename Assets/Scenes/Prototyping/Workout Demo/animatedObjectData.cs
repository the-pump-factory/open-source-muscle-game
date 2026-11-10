using Swole.API.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "animatedObjectData", menuName = "WorkoutDemo/animatedObjectData", order = 2)]
public class animatedObjectData : ScriptableObject
{
    public GameStateController.AnimatedObject data;
}
