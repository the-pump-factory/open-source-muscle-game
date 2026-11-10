using System;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole;
using Swole.API.Unity;

namespace BulkOut
{

    [Serializable]
    public struct PhysiqueGoal
    {

        public MuscleGoal[] muscleGoals;

        public float CalculateScore(IMuscularBasic muscleController)
        {
            if (muscleGoals == null) return 0f;

            float score = 0f;

            foreach (var goal in muscleGoals)
            {
                score += goal.CalculateScore(muscleController);
            }

            return score;
        }

    }

    [Serializable]
    public enum MuscleGoalType
    {
        MassLevel, PumpLevel, MassRange, PumpRange, MassSymmetry, PumpSymmetry, MassSymmetryRange, PumpSymmetryRange
    }

    [Serializable]
    public struct MuscleGoal
    {

        public MuscleGroupIdentifier muscleGroup;

        public MuscleGoalType goalType;

        public bool clamp;
        public bool invert;

        public float minThreshold;
        public float maxThreshold;

        public AnimationCurve thresholdCurve;

        public float minScore;
        public float maxScore;
        
        public float CalculateScore(IMuscularBasic muscleController)
        {
            int muscleGroupIndex = muscleController.FindMuscleGroup(muscleGroup);
            if (muscleGroupIndex < 0) return 0f;

            float t = 0f;
            switch (goalType)
            {
                case MuscleGoalType.MassLevel:
                    if (minThreshold == maxThreshold)
                    {
                        t = muscleController.GetMuscleGroupMass(muscleGroupIndex) >= minThreshold ? 1f : 0f;
                    }
                    else
                    {
                        t = (muscleController.GetMuscleGroupMass(muscleGroupIndex) - minThreshold) / (maxThreshold - minThreshold);
                    }
                    break;
            }

            if (clamp) Mathf.Clamp01(t);
            if (invert) t = 1f - t;

            if (thresholdCurve != null && thresholdCurve.length > 0)
            {
                t = thresholdCurve.Evaluate(t);
            }

            return Mathf.LerpUnclamped(minScore, maxScore, t);
        }

    }

}