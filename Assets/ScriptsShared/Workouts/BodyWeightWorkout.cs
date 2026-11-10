using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(fileName = "BodyWeightWorkout", menuName = "Flex Up/Workouts/Body Weight")]
public class BodyWeightWorkout : WorkoutBase
{

    [Serializable]
    public enum BodyWeightSource
    {
        Muscle, Fat
    }

    [Serializable]
    public struct BodyWeightContributor
    {
        public BodyWeightSource source;
        public string name;

        [Tooltip("Ratio of additive load that can be contributed by this contributor.")]
        public float ratio;

        [Tooltip("The range of source values that map to 0 to 1 contribution. Defaults to 0 to 1 if min and max are zero.")]
        public Vector2 sourceValueRange;

        public bool clampSourceValue;
    }

    [Header("Body Weight")]
    public BodyWeightContributor[] bodyWeightContributors; 

}
