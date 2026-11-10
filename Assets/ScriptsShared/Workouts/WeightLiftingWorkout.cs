using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(fileName = "WeightLiftingWorkout", menuName = "Flex Up/Workouts/Weight Lifting")]
public class WeightLiftingWorkout : WorkoutBase
{

    [Header("Weight Lifting")]
    public float loadChangeDelay = 0f;
    public float loadChangeDelaySpotter = 0f;

    public WorkoutBaseAnimations machineAnimations;

    public bool allowExternalWeightChange;

    public LinearAnimationSequence[] addWeightExternalAnimations;
    public LinearAnimationSequence[] removeWeightExternalAnimaions;

    public LinearAnimationSequence[] addWeightExternalMachineAnimations;
    public LinearAnimationSequence[] removeWeightExternalMachineAnimaions;

    [Header("Weight Lifting Performer")]
    public bool allowWeightChangeFromIdle;
    public LinearAnimationSequence[] addWeightIdleAnimations;
    public LinearAnimationSequence[] removeWeightIdleAnimaions;

    public LinearAnimationSequence[] addWeightIdleMachineAnimations;
    public LinearAnimationSequence[] removeWeightIdleMachineAnimaions;

    public bool allowWeightChangeFromRest;
    public LinearAnimationSequence[] addWeightRestAnimations;
    public LinearAnimationSequence[] removeWeightRestAnimaions;

    public LinearAnimationSequence[] addWeightRestMachineAnimations;
    public LinearAnimationSequence[] removeWeightRestMachineAnimaions;

    [Header("Weight Lifting Spotter")]
    public bool allowWeightChangeFromIdleSpotter;
    public LinearAnimationSequence[] addWeightIdleSpotterAnimations;
    public LinearAnimationSequence[] removeWeightIdleSpotterAnimaions;

    public LinearAnimationSequence[] addWeightIdleMachineSpotterAnimations;
    public LinearAnimationSequence[] removeWeightIdleMachineSpotterAnimaions;

    public bool allowWeightChangeFromRestSpotter;
    public LinearAnimationSequence[] addWeightRestSpotterAnimations;
    public LinearAnimationSequence[] removeWeightRestSpotterAnimaions;

    public LinearAnimationSequence[] addWeightRestMachineSpotterAnimations;
    public LinearAnimationSequence[] removeWeightRestMachineSpotterAnimaions;

}
