using System;
using System.Collections;
using System.Collections.Generic;

using Swole;
using Swole.API.Unity;

using UnityEngine;
using UnityEngine.Events;

public class WorkoutSystem : MonoBehaviour
{
    public void OnDrawGizmos()
    {
        if (!Application.isPlaying || workoutProvider == null || workoutProvider.RepCurve == null || canvas == null) return;

        Gizmos.color = Color.cyan;
        for(int a = 0; a < workoutProvider.RepCurve.Length - 1; a++)
        {
            var posOnCurve1 = workoutProvider.RepCurve[a];
            var posOnCurve2 = workoutProvider.RepCurve[a + 1];
            var p1 = canvas.transform.TransformPoint(repKnobStartPos + new Vector3(Mathf.LerpUnclamped(-repKnobMaxMoveX, repKnobMaxMoveX, posOnCurve1.x + 1f), Mathf.LerpUnclamped(-repKnobMaxMoveY, repKnobMaxMoveY, posOnCurve1.y + 1f), 5f));
            var p2 = canvas.transform.TransformPoint(repKnobStartPos + new Vector3(Mathf.LerpUnclamped(-repKnobMaxMoveX, repKnobMaxMoveX, posOnCurve2.x + 1f), Mathf.LerpUnclamped(-repKnobMaxMoveY, repKnobMaxMoveY, posOnCurve2.y + 1f), 5f));

            Gizmos.DrawLine(p1, p2); 
        }      
    }

    public InteractableObjectBasic interactableObject;

    public string providerAddress;
    public int providerPort;

    public WorkoutBase workout;
    public BaseWorkoutProvider workoutProvider;

    public bool IgnoreInput
    {
        get => workoutProvider == null ? false : workoutProvider.ignoreInput;
        set
        {
            if (workoutProvider != null) workoutProvider.ignoreInput = value;
        }
    }

    public void SetWorkout(WorkoutBase workout)
    {
        if (workoutProvider != null) workoutProvider.Dispose();
        workoutProvider = null;

        this.workout = workout;
        if (workout == null) return;

        workoutProvider = new BaseWorkoutProvider(workout); 
        workoutProvider.Initialize();
    }

    public void SetWorkoutProvider(BaseWorkoutProvider provider)
    {
        if (workoutProvider != null) workoutProvider.Dispose();

        this.workoutProvider = provider;
        if (provider != null) 
        {
            this.workout = provider.Workout;
            provider.Initialize(); 
        }
    }

    public void ClearWorkout()
    {
        if (workoutProvider != null) workoutProvider.Dispose();
        workoutProvider = null;
        workout = null;
    }

    private Canvas canvas;

    public RectTransform repKnobTransform;
    public RectTransform targetDirectionPivot;
    public RectTransform inputDirectionPivot;

    /// <summary>
    /// The visual x movement range of the rep knob
    /// </summary>
    public float repKnobMaxMoveX = 500f;
    /// <summary>
    /// The visual y movement range of the rep knob
    /// </summary>
    public float repKnobMaxMoveY = 500f;
    private Vector3 repKnobStartPos;

    public void Awake()
    {
        if (workout != null)
        {
            workoutProvider = new BaseWorkoutProvider(workout);
            workoutProvider.Initialize();
        }

        if (repKnobTransform != null)
        {
            canvas = repKnobTransform.GetComponentInParent<Canvas>(true);
            repKnobStartPos = canvas.transform.InverseTransformPoint(repKnobTransform.position);
        }
    }

    public void Update()
    {
        if (workoutProvider == null) return;
        
        workoutProvider.Update(Time.deltaTime);

        if (repKnobTransform != null) 
        {
            var currentPerformanceState = workoutProvider.GetCurrentPerformanceState();
            var posOnCurve = workoutProvider.RepCurve.GetPosition(workoutProvider.RepPosition);
            repKnobTransform.position = canvas.transform.TransformPoint(repKnobStartPos + new Vector3(Mathf.LerpUnclamped(-repKnobMaxMoveX, repKnobMaxMoveX, posOnCurve.x + 1f), Mathf.LerpUnclamped(-repKnobMaxMoveY, repKnobMaxMoveY, posOnCurve.y + 1f), 0f));

            var targetAlignmentDir = workoutProvider.RepCurve.GetDirection(currentPerformanceState["repPosition"]) * (currentPerformanceState["reachedApex"] > 0.5f ? -1f : 1f);
            targetDirectionPivot.localRotation = Quaternion.Euler(0f, 0f, Maths.AngleOffsetAroundAxis(new Vector3(targetAlignmentDir.x, targetAlignmentDir.y, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f))); 

            float inputX = workoutProvider.InputManager[DefaultWorkoutInputManager._inputX];
            float inputY = workoutProvider.InputManager[DefaultWorkoutInputManager._inputY];
            if (inputX != 0f || inputY != 0f)
            {
                inputDirectionPivot.localRotation = Quaternion.Euler(0f, 0f, Maths.AngleOffsetAroundAxis(new Vector3(inputX, inputY, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f)));
            }
        }
    }

    public bool TryToEnter() => workoutProvider.TryToEnter();
    public bool TryToIdle() => workoutProvider.TryToIdle();
    public bool TryToRep() => workoutProvider.TryToRep();
    public bool TryToRest() => workoutProvider.TryToRest();
    public bool TryToExit() => workoutProvider.TryToExit();

    public float RepPosition => workoutProvider.RepPosition;

}

public interface IWorkoutInputManager
{
    public float this[string inputKey] { get; }
    public void Update(float deltaTime);
}
public class DefaultWorkoutInputManager : IWorkoutInputManager
{
    public const string _inputX = "x";
    public const string _inputY = "y";

    private readonly Dictionary<string, float> inputValues = new Dictionary<string, float>();
    public float this[string inputKey] => inputValues.ContainsKey(inputKey) ? inputValues[inputKey] : 0f;
    public void Update(float deltaTime)
    {
        float joy_hor = InputProxy.MainLeftJoystickHorizontal;
        float joy_ver = InputProxy.MainLeftJoystickVertical;
        float cursor_hor = InputProxy.CursorAxisX;
        float cursor_ver = InputProxy.CursorAxisY;

        inputValues[_inputX] = Math.Abs(joy_hor) > Math.Abs(cursor_hor) ? joy_hor : cursor_hor;
        inputValues[_inputY] = Math.Abs(joy_ver) > Math.Abs(cursor_ver) ? joy_ver : cursor_ver; 
    }
}

[Serializable]
public enum WorkoutPerformerState
{
    Null, EnteringWorkout, ExitingWorkout, IdlingWorkout, EnteringRep, ExitingRep, Repping, EnteringRest, ExitingRest, Resting
}
[Serializable, Flags]
public enum WorkoutLoadChangeRequirements
{
    Static = 0, PerformerIdling = 1, PerformerAtRest = 2, PerformerCanLoad = 4, SpotterRequired = 8, SpotterCanLoad = 16, WaitForLastChangeToComplete = 32
}
[Serializable]
public struct WorkoutMuscleState
{
    public float mass;
    public float fatigue;
}

public interface IWorkoutState
{
    public float this[string attribute] { get; set; }
    public float GetAttribute(string attribute);
    public void SetAttribute(string attribute, float value);

    public IWorkoutStateDiff GetDiff(IWorkoutState originalState);
    public void ApplyDiff(IWorkoutStateDiff diff);
}
public interface IWorkoutStateDiff
{
    public float this[string attribute] { get; set; }
    public float GetAttribute(string attribute);
    public void SetAttribute(string attribute, float value);
}

public delegate void PerformerStateChangeDelegate(WorkoutPerformerState prevState, WorkoutPerformerState newState);
public delegate void WorkoutStateChangeDelegate(IWorkoutState prevState, IWorkoutState newState);
public delegate void PerformanceStateChangeDelegate(int tick, IWorkoutState prevState, IWorkoutState newState);

public interface IWorkoutProvider : IDisposable
{
    public void Initialize();
    public IWorkoutInputManager InputManager { get; set; }
    public void SetInputManager(IWorkoutInputManager manager);
    public int TickRate { get; set; }
    public long Tick { get; }
    public void ResetTick();

    public IWorkoutState this[int tick] { get; set; }
    public IWorkoutState GetCurrentPerformanceState();
    public IWorkoutState GetPerformanceState(int tick);
    public void SetPerformanceState(int tick, IWorkoutState state);
    public void ListenForPerformanceStateChange(PerformanceStateChangeDelegate listener);
    public void StopListeningForPerformanceStateChange(PerformanceStateChangeDelegate listener);

    public IWorkoutState GetState();
    public void SetState(IWorkoutState state);
    public void ListenForWorkoutStateChange(WorkoutStateChangeDelegate listener);
    public void StopListeningForWorkoutStateChange(WorkoutStateChangeDelegate listener);

    public WorkoutPerformerState PerformerState { get; }
    public void SetPerformerState(WorkoutPerformerState state);
    public void TransitionPerformerState(WorkoutPerformerState targetState, float transitionDuration);
    public void ListenForPerformerStateChange(PerformerStateChangeDelegate listener);
    public void StopListeningForPerformerStateChange(PerformerStateChangeDelegate listener);

    public void Advance();
    public void Update(float deltaTime);

    public float Load { get; set; }
    public void SetLoad(float load);
    public float GetLoad();
    public WorkoutLoadChangeRequirements LoadChangeRequirements { get; }
    public bool CanChangeLoad { get; }
    public float LoadChangeDelay { get; }
    public float LoadChangeDelaySpotter { get; }
    public float MaxLiftableLoad { get; }

    public bool AllowSpotter { get; }
    public bool HasSpotter { get; }

    public bool CanStartWorkout { get; }
    public bool CanStartRep { get; }
    public bool CanStartResting { get; }

    public bool CanExitWorkout { get; }
    public bool CanExitResting { get; }

    public float EnterWorkoutTime { get; }
    public float ExitWorkoutTime { get; }
    public float EnterRestTime { get; }
    public float ExitRestTime { get; }
    public float EnterRepTime { get; }
    public float ExitRepTime { get; }

    public bool TryToEnter();
    public bool TryToIdle();
    public bool TryToRep();
    public bool TryToRest();
    public bool TryToExit();

    public WorkoutPerformerState NextPerformerState { get; }
    public bool IsTransitioningPerformerState { get; }
    public float PerformerTransitionProgress { get; }

    public Dictionary<string, ValueCurve> MuscleLoadContributions { get; set; }
    public void SetMuscleLoadContributions(Dictionary<string, ValueCurve> loadContributions);
    public Dictionary<string, ValueCurve> GetMuscleLoadContributions();
    public float GetCurrentLoadContribution(string muscleName);

    public Dictionary<string, ValueCurve> MuscleFatigueLoadMultipliers { get; set; }
    public void SetMuscleFatigueLoadMultipliers(Dictionary<string, ValueCurve> fatigueLoadMultipliers);
    public Dictionary<string, ValueCurve> GetMuscleFatigueLoadMultipliers();
    public float GetCurrentFatigueLoadMultiplier(string muscleName);

    public Dictionary<string, ValueCurve> MuscleRepPosGrowths { get; set; }
    public void SetMuscleRepPosGrowths(Dictionary<string, ValueCurve> repPosGrowths);
    public Dictionary<string, ValueCurve> GetMuscleRepPosGrowths();
    public float GetCurrentRepPosGrowth(string muscleName);

    public Dictionary<string, ValueCurve> MuscleLoadGrowths { get; set; }
    public void SetMuscleLoadGrowths(Dictionary<string, ValueCurve> loadGrowths);
    public Dictionary<string, ValueCurve> GetMuscleLoadGrowths();
    public float GetCurrentLoadGrowth(string muscleName);

    public Dictionary<string, WorkoutMuscleState> MuscleStates { get; set; }
    public void SetMuscleStates(Dictionary<string, WorkoutMuscleState> muscleStates);
    public Dictionary<string, WorkoutMuscleState> GetMuscleStates();
    public void SetMuscleState(string muscleName, WorkoutMuscleState muscleState);
    public WorkoutMuscleState GetMuscleState(string muscleName);
}
public interface INetworkedWorkoutProvider : IWorkoutProvider
{
}

public interface IWorkoutAttributes
{
    public float this[string attribute] { get; set; }

    public IWorkoutAttributes SetAttribute(int attributeIndex, float value);
    public IWorkoutAttributes SetAttribute(string attribute, float value);

    public float GetAttribute(int attributeIndex);
    public float GetAttribute(string attribute);

    public int AttributeCount { get; }
    public string GetAttributeName(int attributeIndex);
}

[Serializable]
public struct BaseWorkoutState : IWorkoutState
{
    [Serializable]
    public struct Attributes : IWorkoutAttributes
    {
        public int repCount;
        public int combo;
        public int overheat;
        public float stamina;

        public int AttributeCount => 4;
        public string GetAttributeName(int attributeIndex)
        {
            switch(attributeIndex)
            {
                case 0:
                    return nameof(repCount);
                case 1:
                    return nameof(combo);
                case 2:
                    return nameof(overheat);
                case 3:
                    return nameof(stamina);
            }

            return null;
        }

        public IWorkoutAttributes SetAttribute(int attributeIndex, float value) => SetAttribute(GetAttributeName(attributeIndex), value);
        public IWorkoutAttributes SetAttribute(string attribute, float value)
        {
            if (string.IsNullOrWhiteSpace(attribute)) return this;

            switch (attribute)
            {
                case nameof(repCount):
                    repCount = (int)value;
                    break;

                case nameof(combo):
                    combo = (int)value;
                    break;

                case nameof(overheat):
                    overheat = (int)value;
                    break;

                case nameof(stamina):
                    stamina = value;
                    break;
            }

            return this;
        }

        public float GetAttribute(int attributeIndex) => GetAttribute(GetAttributeName(attributeIndex));
        public float GetAttribute(string attribute)
        {
            if (string.IsNullOrWhiteSpace(attribute)) return 0f;

            switch (attribute)
            {
                case nameof(repCount):
                    return repCount;

                case nameof(combo):
                    return combo;

                case nameof(overheat):
                    return overheat;

                case nameof(stamina):
                    return stamina;
            }

            return 0f;
        }

        public float this[string attribute]
        {
            get => GetAttribute(attribute);
            set => SetAttribute(attribute, value);
        }

        public static Attributes operator +(Attributes a, Attributes b)
        {
            var val = a;

            for(int i = 0; i < val.AttributeCount; i++)
            {
                val = (Attributes)val.SetAttribute(i, a.GetAttribute(i) + b.GetAttribute(i));
            }

            return val;
        }
        public static Attributes operator -(Attributes a, Attributes b)
        {
            var val = a;

            for (int i = 0; i < val.AttributeCount; i++)
            {
                val = (Attributes)val.SetAttribute(i, a.GetAttribute(i) - b.GetAttribute(i));
            }

            return val;
        }
        public static Attributes operator *(Attributes a, Attributes b)
        {
            var val = a;

            for (int i = 0; i < val.AttributeCount; i++)
            {
                val = (Attributes)val.SetAttribute(i, a.GetAttribute(i) * b.GetAttribute(i));
            }

            return val;
        }
        public static Attributes operator /(Attributes a, Attributes b)
        {
            var val = a;

            for (int i = 0; i < val.AttributeCount; i++)
            {
                val = (Attributes)val.SetAttribute(i, a.GetAttribute(i) / b.GetAttribute(i));
            }

            return val;
        }
    }
    [Serializable]
    public struct Diff : IWorkoutStateDiff
    {

        public Attributes attributes;

        public float this[string attribute]
        {
            get => GetAttribute(attribute);
            set => SetAttribute(attribute, value);
        }

        public void SetAttribute(string attribute, float value) => attributes = (Attributes)attributes.SetAttribute(attribute, value);
        public float GetAttribute(string attribute) => attributes.GetAttribute(attribute);

    }

    public static BaseWorkoutState Default
    {
        get
        {
            return new BaseWorkoutState()
            {
                attributes = new Attributes()
                {
                    stamina = 1f
                }
            };
        }
    }

    public void ApplyDiff(IWorkoutStateDiff diff)
    {
        if (diff is Diff _diff)
        {
            attributes = attributes + _diff.attributes;
        }
    }

    public IWorkoutStateDiff GetDiff(IWorkoutState originalState)
    {
        if (originalState is BaseWorkoutState state)
        {
            return new Diff()
            {
                attributes = state.attributes - attributes
            };
        }

        return new Diff();
    }


    public Attributes attributes;

    public float this[string attribute]
    {
        get => GetAttribute(attribute);
        set => SetAttribute(attribute, value);
    }

    public void SetAttribute(string attribute, float value) => attributes = (Attributes)attributes.SetAttribute(attribute, value);
    public float GetAttribute(string attribute) => attributes.GetAttribute(attribute);
}
[Serializable]
public struct WorkoutPerformanceState : IWorkoutState
{

    [Serializable]
    public struct Attributes : IWorkoutAttributes
    {
        public float reachedApex;
        public float repPosition;
        public float alignment;
        public float stamina;
        public float repSpeed;

        public int AttributeCount => 3;
        public string GetAttributeName(int attributeIndex)
        {
            switch (attributeIndex)
            {
                case 0:
                    return nameof(reachedApex);
                case 1:
                    return nameof(repPosition);
                case 2:
                    return nameof(alignment);
                case 3:
                    return nameof(stamina);
                case 4:
                    return nameof(repSpeed);
            }

            return null;
        }

        public IWorkoutAttributes SetAttribute(int attributeIndex, float value) => SetAttribute(GetAttributeName(attributeIndex), value);
        public IWorkoutAttributes SetAttribute(string attribute, float value)
        {
            switch (attribute)
            {
                case nameof(reachedApex):
                    reachedApex = value;
                    break;

                case nameof(repPosition):
                    repPosition = value;
                    break;

                case nameof(alignment):
                    alignment = value;
                    break;

                case nameof(stamina):
                    stamina = value;
                    break;

                case nameof(repSpeed):
                    repSpeed = value;
                    break;
            }

            return this;
        }

        public float GetAttribute(int attributeIndex) => GetAttribute(GetAttributeName(attributeIndex));
        public float GetAttribute(string attribute)
        {
            switch (attribute)
            {
                case nameof(reachedApex):
                    return reachedApex;

                case nameof(repPosition):
                    return repPosition;

                case nameof(alignment):
                    return alignment;

                case nameof(stamina):
                    return stamina;

                case nameof(repSpeed):
                    return repSpeed;
            }

            return 0f;
        }

        public float this[string attribute]
        {
            get => GetAttribute(attribute);
            set => SetAttribute(attribute, value);
        }

        public static Attributes operator +(Attributes a, Attributes b)
        {
            var val = a;

            for (int i = 0; i < val.AttributeCount; i++)
            {
                val = (Attributes)val.SetAttribute(i, a.GetAttribute(i) + b.GetAttribute(i));
            }

            return val;
        }
        public static Attributes operator -(Attributes a, Attributes b)
        {
            var val = a;

            for (int i = 0; i < val.AttributeCount; i++)
            {
                val = (Attributes)val.SetAttribute(i, a.GetAttribute(i) - b.GetAttribute(i));
            }

            return val;
        }
        public static Attributes operator *(Attributes a, Attributes b)
        {
            var val = a;

            for (int i = 0; i < val.AttributeCount; i++)
            {
                val = (Attributes)val.SetAttribute(i, a.GetAttribute(i) * b.GetAttribute(i));
            }

            return val;
        }
        public static Attributes operator /(Attributes a, Attributes b)
        {
            var val = a;

            for (int i = 0; i < val.AttributeCount; i++)
            {
                val = (Attributes)val.SetAttribute(i, a.GetAttribute(i) / b.GetAttribute(i));
            }

            return val;
        }
    }
    [Serializable]
    public struct Diff : IWorkoutStateDiff
    {

        public Attributes attributes;

        public float this[string attribute]
        {
            get => GetAttribute(attribute);
            set => SetAttribute(attribute, value);
        }

        public void SetAttribute(string attribute, float value) => attributes = (Attributes)attributes.SetAttribute(attribute, value);
        public float GetAttribute(string attribute) => attributes.GetAttribute(attribute);

    }

    public static WorkoutPerformanceState Default
    {
        get
        {
            return new WorkoutPerformanceState()
            {
                attributes = new Attributes()
                {
                }
            };
        }
    }

    public void ApplyTypedDiff(Diff diff)
    {
        attributes = attributes + diff.attributes;
    }
    public void ApplyDiff(IWorkoutStateDiff diff)
    {
        if (diff is Diff _diff) ApplyTypedDiff(_diff);
    }

    public Diff GetTypedDiff(WorkoutPerformanceState originalState)
    {
        return new Diff()
        {
            attributes = originalState.attributes - attributes
        };
    }
    public IWorkoutStateDiff GetDiff(IWorkoutState originalState)
    {
        if (originalState is WorkoutPerformanceState state)
        {
            return GetTypedDiff(state);
        }

        return new Diff();
    }


    public Attributes attributes;
    public bool HasReachedApex => attributes.reachedApex > 0.5f;

    public float this[string attribute]
    {
        get => GetAttribute(attribute);
        set => SetAttribute(attribute, value);
    }

    public void SetAttribute(string attribute, float value) => attributes = (Attributes)attributes.SetAttribute(attribute, value);
    public float GetAttribute(string attribute) => attributes.GetAttribute(attribute);

    /// <summary>
    /// Convenience field to store a diff in this state
    /// </summary>
    public Diff storedDiff;

}

[Serializable]
public class ValueCurve
{
    private float[] values;
    public int Length => values == null ? 0 : values.Length;
    public float GetPoint(int index) => values[index];
    public float this[int index] => GetPoint(index);

    public ValueCurve(float[] values)
    {
        this.values = values;
    }

    public float GetValue(float t)
    {
        if (values == null || values.Length <= 1) return values.Length > 0 ? values[0] : 0f;

        t = Math.Clamp(t, 0f, 1f);
        int startIndex = Math.Max(1, (int)Math.Floor((values.Length - 1f) * t));

        float t0 = ((startIndex - 1) / (values.Length - 1f));
        float t1 = (startIndex / (values.Length - 1f));

        float t2 = (t - t0) / (t1 - t0);

        return (values[startIndex] * t2) + (values[startIndex - 1] * (1f - t2));
    }

    public static ValueCurve FromAnimationCurve(AnimationCurve animCurve, int samples = 100) 
    {
        float[] values;
        if (animCurve == null)
        {
            values = new float[1];
        }
        else
        {
            values = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = Math.Min(0.99999f, i / (samples - 1f));
                values[i] = animCurve.Evaluate(t);
            }
        }

        return new ValueCurve(values);
    }
}

[Serializable]
public struct Vec2
{

    public static float Dot(Vec2 v1, Vec2 v2) => (v1.x * v2.x) + (v1.y * v2.y);

    public static Vec2 zero => default;

    public float x;
    public float y;
    public Vec2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public override string ToString()
    {
        return $"{nameof(Vec2)}({x}, {y})";  
    }

    public Vec2 normalized
    {
        get
        {
            float length = (float)Math.Sqrt(x * x + y * y);
            if (length <= 0f) return zero;

            var n = this;

            n.x = x / length;
            n.y = y / length;

            return n;
        }
    }

    public static Vec2 operator +(Vec2 a, Vec2 b)
    {
        var val = a;

        val.x = a.x + b.x;
        val.y = a.y + b.y;

        return val;
    }
    public static Vec2 operator -(Vec2 a, Vec2 b)
    {
        var val = a;

        val.x = a.x - b.x;
        val.y = a.y - b.y;

        return val;
    }
    public static Vec2 operator *(Vec2 a, float b)
    {
        var val = a;

        val.x = a.x * b;
        val.y = a.y * b;

        return val;
    }
}
[Serializable]
public class PointCurve
{

    [SerializeField]
    private Vec2[] points;
    public int Length => points == null ? 0 : points.Length;
    public Vec2 GetPoint(int index) => points[index];
    public Vec2 this[int index] => GetPoint(index);

    public PointCurve(Vec2[] points)
    {
        this.points = points;
    }

    public Vec2 GetPosition(float t)
    {
        if (points == null || points.Length <= 1) return points.Length > 0 ? points[0] : Vec2.zero;

        t = Math.Clamp(t, 0f, 1f);
        int startIndex = Math.Clamp((int)Math.Floor((points.Length - 1) * t), 0, points.Length - 2);

        float t0 = (startIndex / (points.Length - 1f));
        float t1 = ((startIndex + 1) / (points.Length - 1f)); 

        float t2 = (t - t0) / (t1 - t0);
        return (points[startIndex + 1] * t2) + (points[startIndex] * (1f - t2)); 
    }
    public Vec2 GetDirection(float t)
    {
        if (points == null || points.Length <= 1) return Vec2.zero;

        t = Math.Clamp(t, 0f, 1f);
        int startIndex = Math.Clamp((int)Math.Floor((points.Length - 1) * t), 0, points.Length - 2);

        var p1 = points[startIndex];
        var p2 = points[startIndex + 1];
        
        return (points[startIndex + 1] - points[startIndex]).normalized;  
    }
}
[Serializable]
public struct KeyValueCurve
{
    public string name;
    public ValueCurve curve;
}
[Serializable]
public struct KeyValueFloat
{
    public string name;
    public float value; 
}
public class BaseWorkoutProvider : IWorkoutProvider 
{

    protected WorkoutBase workout;
    public WorkoutBase Workout => workout;

    protected WorkoutPerformerState performerState = WorkoutPerformerState.Null;
    public WorkoutPerformerState PerformerState => performerState;

    protected WorkoutPerformerState nextPerformerState = WorkoutPerformerState.Null;
    public WorkoutPerformerState NextPerformerState => nextPerformerState;

    protected float performerStateTransitionTimeLeft;
    protected float performerStateTransitionTime;
    protected bool transitionPerformerState;
    public bool IsTransitioningPerformerState => transitionPerformerState;
    public float PerformerTransitionProgress => performerStateTransitionTime <= 0 ? 1f : Mathf.Clamp01(1f - (performerStateTransitionTimeLeft / performerStateTransitionTime));

    public void SetPerformerState(WorkoutPerformerState state)
    {
        OnPerformerStateChange?.Invoke(performerState, state);
        performerState = state; 
    }
    public void TransitionPerformerState(WorkoutPerformerState targetState, float transitionDuration)
    {
        nextPerformerState = targetState;
        performerStateTransitionTime = performerStateTransitionTimeLeft = transitionDuration;
        transitionPerformerState = true;
    }

    public event PerformerStateChangeDelegate OnPerformerStateChange;
    public void ListenForPerformerStateChange(PerformerStateChangeDelegate listener)
    {
        OnPerformerStateChange += listener;
    }
    public void StopListeningForPerformerStateChange(PerformerStateChangeDelegate listener)
    {
        OnPerformerStateChange -= listener;
    }

    public float EnterWorkoutTime => workout.enterWorkoutTime;
    public float ExitWorkoutTime => workout.exitWorkoutTime;
    public float EnterRestTime => workout.enterRestTime;
    public float ExitRestTime => workout.exitRestTime;
    public float EnterRepTime => workout.enterRepTime;
    public float ExitRepTime => workout.exitRepTime;

    public bool CanStartWorkout => true;
    public bool CanStartRep => performerState == WorkoutPerformerState.IdlingWorkout;
    public bool CanStartResting => performerState == WorkoutPerformerState.IdlingWorkout;

    public bool CanExitWorkout => performerState == WorkoutPerformerState.IdlingWorkout;
    public bool CanExitResting => performerState == WorkoutPerformerState.Resting;

    public bool TryToEnter()
    {
        switch(performerState)
        {
            case WorkoutPerformerState.Null:
                SetPerformerState(WorkoutPerformerState.EnteringWorkout);
                TransitionPerformerState(WorkoutPerformerState.IdlingWorkout, EnterWorkoutTime);
                return true;

            case WorkoutPerformerState.EnteringWorkout:
                if (nextPerformerState != WorkoutPerformerState.IdlingWorkout || performerStateTransitionTimeLeft <= 0f)
                {
                    TransitionPerformerState(WorkoutPerformerState.IdlingWorkout, EnterWorkoutTime);
                }
                return true;
        }

        return false;
    }
    public bool TryToIdle()
    {
        switch (performerState)
        {
            case WorkoutPerformerState.Resting:
                if (nextPerformerState != WorkoutPerformerState.IdlingWorkout || performerStateTransitionTimeLeft <= 0f)
                {
                    SetPerformerState(WorkoutPerformerState.ExitingRest);
                    TransitionPerformerState(WorkoutPerformerState.IdlingWorkout, ExitRestTime);
                }
                return true;

            case WorkoutPerformerState.Repping:
                if (nextPerformerState != WorkoutPerformerState.IdlingWorkout || performerStateTransitionTimeLeft <= 0f)
                {
                    SetPerformerState(WorkoutPerformerState.ExitingRep);
                    TransitionPerformerState(WorkoutPerformerState.IdlingWorkout, ExitRepTime);
                }
                return true;
        }

        return false;
    }
    public bool TryToRep()
    {
        switch (performerState)
        {
            case WorkoutPerformerState.IdlingWorkout:
                if (nextPerformerState != WorkoutPerformerState.Repping || performerStateTransitionTimeLeft <= 0f)
                {
                    SetPerformerState(WorkoutPerformerState.EnteringRep);
                    TransitionPerformerState(WorkoutPerformerState.Repping, EnterRepTime);
                }
                return true;
        }

        return false;
    }
    public bool TryToRest()
    {
        switch (performerState)
        {
            case WorkoutPerformerState.IdlingWorkout:
                if (nextPerformerState != WorkoutPerformerState.Resting || performerStateTransitionTimeLeft <= 0f)
                {
                    SetPerformerState(WorkoutPerformerState.EnteringRest);
                    TransitionPerformerState(WorkoutPerformerState.Resting, EnterRestTime);  
                }
                return true;
        }

        return false;
    }
    public bool TryToExit()
    {
        switch (performerState)
        {
            case WorkoutPerformerState.IdlingWorkout:
                if (nextPerformerState != WorkoutPerformerState.Null || performerStateTransitionTimeLeft <= 0f)
                {
                    SetPerformerState(WorkoutPerformerState.ExitingWorkout);
                    TransitionPerformerState(WorkoutPerformerState.Null, ExitWorkoutTime);
                }
                return true;

            case WorkoutPerformerState.Resting:
                if (!workout.canExitWorkoutFromRest) return false; 
                if (nextPerformerState != WorkoutPerformerState.Null || performerStateTransitionTimeLeft <= 0f)
                {
                    SetPerformerState(WorkoutPerformerState.ExitingWorkout);
                    TransitionPerformerState(WorkoutPerformerState.Null, ExitWorkoutTime);
                }
                return true;
        }

        return false;
    }

    public virtual void Initialize()
    {
        if (inputManager == null) SetInputManager(new DefaultWorkoutInputManager());

        performerState = WorkoutPerformerState.Null;

        ResetTick();     
        SetState(BaseWorkoutState.Default);
        SetLoad(workout.loadRangeMin);
    }

    private IWorkoutInputManager inputManager;
    public IWorkoutInputManager InputManager
    {
        get => inputManager;
        set => SetInputManager(value);
    }
    public virtual void SetInputManager(IWorkoutInputManager inputManager)
    {
        this.inputManager = inputManager;
    }

    public bool ignoreInput;

    protected int tickRate = 16;
    public int TickRate 
    { 
        get => tickRate; 
        set => tickRate = value; 
    }
    public float TickRateDelta => 1f / tickRate;

    public long Tick => performanceStates.Count;

    public virtual void ResetTick()
    {
        if (performanceStates.Count > 0)
        {
            var finalPerformanceState = performanceStates[performanceStates.Count - 1];

            var state = GetState();
            state[nameof(BaseWorkoutState.Attributes.stamina)] = finalPerformanceState[nameof(WorkoutPerformanceState.Attributes.stamina)]; // set stamina to last performance state stamina
            SetState(state);
        }

        performanceStates.Clear();
    }

    protected BaseWorkoutState state;
    public IWorkoutState GetState() => state;
    public void SetState(IWorkoutState state)
    {
        if (state is BaseWorkoutState _state)
        {
            OnWorkoutStateChange?.Invoke(this.state, state);
            this.state = _state;
        }
    }
    public event WorkoutStateChangeDelegate OnWorkoutStateChange;
    public void ListenForWorkoutStateChange(WorkoutStateChangeDelegate listener)
    {
        OnWorkoutStateChange += listener;
    }
    public void StopListeningForWorkoutStateChange(WorkoutStateChangeDelegate listener)
    {
        OnWorkoutStateChange -= listener;
    }

    protected readonly List<WorkoutPerformanceState> performanceStates = new List<WorkoutPerformanceState>();
    public IWorkoutState GetCurrentPerformanceState() => performanceStates.Count <= 0 ? WorkoutPerformanceState.Default : performanceStates[performanceStates.Count - 1];
    public virtual IWorkoutState GetPerformanceState(int tick)
    {
        if (tick < 0 || tick >= performanceStates.Count) return WorkoutPerformanceState.Default;
        return performanceStates[tick];
    }
    public virtual void SetPerformanceState(int tick, IWorkoutState state)
    {
        if (tick < 0 || tick >= performanceStates.Count) return;

        if (state is WorkoutPerformanceState _state)
        {
            bool recalculateFuture = tick < performanceStates.Count - 1;

            if (recalculateFuture)
            {
                for(int a = performanceStates.Count - 1; a > tick; a--)
                {
                    var laterState = performanceStates[a];
                    var earlierState = performanceStates[a - 1];

                    laterState.storedDiff = laterState.GetTypedDiff(earlierState);
                    performanceStates[a] = laterState;
                }
            }

            performanceStates[tick] = _state;

            if (recalculateFuture)
            {
                for (int a = tick + 1; a < performanceStates.Count; a++)
                {
                    var laterState = performanceStates[a];
                    var earlierState = performanceStates[a - 1];
                    earlierState.ApplyTypedDiff(laterState.storedDiff);
                    
                    performanceStates[a] = earlierState;
                }
            } 
            else
            {
                OnPerformanceStateChange.Invoke(tick, tick > 0 ? performanceStates[tick - 1] : WorkoutPerformanceState.Default, _state); 
            }
        }   
    }
    public event PerformanceStateChangeDelegate OnPerformanceStateChange;
    public void ListenForPerformanceStateChange(PerformanceStateChangeDelegate listener)
    {
        OnPerformanceStateChange += listener;
    }
    public void StopListeningForPerformanceStateChange(PerformanceStateChangeDelegate listener)
    {
        OnPerformanceStateChange -= listener;
    }

    public IWorkoutState this[int tick]
    {
        get => GetPerformanceState(tick);
        set => SetPerformanceState(tick, value);
    }

    public bool RepHasReachedApex => performanceStates.Count <= 0 ? false : performanceStates[performanceStates.Count - 1].HasReachedApex; 

    protected float load;
    public float Load
    {
        get => GetLoad();
        set => SetLoad(value);
    }
    public virtual void SetLoad(float load)
    {
        this.load = Math.Min(load, workout.loadLimit);
    }
    public virtual float GetLoad() => load;

    protected WorkoutLoadChangeRequirements loadChangeRequirements = WorkoutLoadChangeRequirements.Static;
    public WorkoutLoadChangeRequirements LoadChangeRequirements => loadChangeRequirements;
    protected float loadChangeTimer = 0f;
    public bool CanChangeLoad
    {
        get
        {
            if (loadChangeRequirements == WorkoutLoadChangeRequirements.Static) return false;

            if (loadChangeRequirements.HasFlag(WorkoutLoadChangeRequirements.PerformerIdling))
            {
                if (performerState != WorkoutPerformerState.IdlingWorkout) return false;
            }
            if (loadChangeRequirements.HasFlag(WorkoutLoadChangeRequirements.PerformerAtRest))
            {
                if (performerState != WorkoutPerformerState.Resting) return false;
            }

            if (loadChangeRequirements.HasFlag(WorkoutLoadChangeRequirements.SpotterRequired))
            {
                if (!HasSpotter) return false;
            }

            if (loadChangeRequirements.HasFlag(WorkoutLoadChangeRequirements.WaitForLastChangeToComplete))
            {
                if (loadChangeTimer > 0f) return false;
            }

            return true;
        }
    }
    protected float loadChangeDelay = 0f;
    public float LoadChangeDelay => loadChangeDelay;
    protected float loadChangeDelaySpotter = 0f;
    public float LoadChangeDelaySpotter => loadChangeDelaySpotter;

    protected float maxLiftableLoad = 0f;
    public float MaxLiftableLoad => maxLiftableLoad;
    public void RecalculateMaxLiftableLoad()
    {
        maxLiftableLoad = 0f;
        float loadContr = 0f;

        if (muscleLoadContributions != null)
        {
            float loadRange = workout.loadRangeMax - workout.loadRangeMin;
            foreach (var entry in muscleLoadContributions)
            {
                var muscleName = entry.Key;
                if (string.IsNullOrWhiteSpace(muscleName)) continue;

                var muscleState = GetMuscleState(muscleName);
                if (!muscleLoadContributions.TryGetValue(muscleName, out var loadCurve)) continue;

                muscleFatigueLoadMultipliers.TryGetValue(muscleName, out ValueCurve fatigueCurve);

                float contr = loadCurve.GetValue(RepPosition) * (fatigueCurve == null ? 1f : fatigueCurve.GetValue(muscleState.fatigue));

                float mass = Math.Max(0f, muscleState.mass);
                maxLiftableLoad += (workout.loadRangeMin + (loadRange * (strengthCurve == null ? mass : strengthCurve.GetValue(mass)))) * contr;
                loadContr += contr;
            }
        }

        if (loadContr > 1f)
        {
            maxLiftableLoad = maxLiftableLoad / loadContr;
        }
    }

    public bool AllowSpotter => workout.allowSpotter;

    protected bool hasSpotter = false;
    public bool HasSpotter => hasSpotter;

    protected Dictionary<string, ValueCurve> muscleFatigueLoadMultipliers = new Dictionary<string, ValueCurve>();
    public Dictionary<string, ValueCurve> MuscleFatigueLoadMultipliers
    {
        get => GetMuscleFatigueLoadMultipliers();
        set => SetMuscleFatigueLoadMultipliers(value);
    }
    public void SetMuscleFatigueLoadMultipliers(Dictionary<string, ValueCurve> fatigueLoadMultipliers) => muscleFatigueLoadMultipliers = fatigueLoadMultipliers;
    public Dictionary<string, ValueCurve> GetMuscleFatigueLoadMultipliers() => muscleFatigueLoadMultipliers;
    public float GetCurrentFatigueLoadMultiplier(string muscleName)
    {
        if (muscleFatigueLoadMultipliers.TryGetValue(muscleName, out var curve))
        {
            if (curve == null) return 1f;

            var muscleState = GetMuscleState(muscleName);
            return curve.GetValue(muscleState.fatigue);
        }

        return 1f;
    }

    protected Dictionary<string, ValueCurve> muscleLoadContributions = new Dictionary<string, ValueCurve>();
    public Dictionary<string, ValueCurve> MuscleLoadContributions
    {
        get => GetMuscleLoadContributions();
        set => SetMuscleLoadContributions(value);
    }
    public void SetMuscleLoadContributions(Dictionary<string, ValueCurve> loadContributions) => muscleLoadContributions = loadContributions;
    public Dictionary<string, ValueCurve> GetMuscleLoadContributions() => muscleLoadContributions;
    public float GetCurrentLoadContribution(string muscleName)
    {
        if (muscleLoadContributions.TryGetValue(muscleName, out var curve))
        {
            if (curve == null) return 0f;
            return curve.GetValue(RepPosition);
        }

        return 0f;
    }

    protected Dictionary<string, ValueCurve> muscleRepPosGrowths = new Dictionary<string, ValueCurve>();
    public Dictionary<string, ValueCurve> MuscleRepPosGrowths
    {
        get => GetMuscleRepPosGrowths();
        set => SetMuscleRepPosGrowths(value);
    }
    public void SetMuscleRepPosGrowths(Dictionary<string, ValueCurve> repPosGrowths) => muscleRepPosGrowths = repPosGrowths;
    public Dictionary<string, ValueCurve> GetMuscleRepPosGrowths() => muscleRepPosGrowths;
    public float GetCurrentRepPosGrowth(string muscleName)
    {
        if (muscleRepPosGrowths.TryGetValue(muscleName, out var curve))
        {
            if (curve == null) return 0f;
            return curve.GetValue(RepPosition);
        }

        return 0f;
    }

    protected Dictionary<string, ValueCurve> muscleLoadGrowths = new Dictionary<string, ValueCurve>();
    public Dictionary<string, ValueCurve> MuscleLoadGrowths
    {
        get => GetMuscleLoadGrowths();
        set => SetMuscleLoadGrowths(value);
    }
    public void SetMuscleLoadGrowths(Dictionary<string, ValueCurve> loadGrowths) => muscleLoadGrowths = loadGrowths;
    public Dictionary<string, ValueCurve> GetMuscleLoadGrowths() => muscleLoadGrowths;
    public float GetCurrentLoadGrowth(string muscleName)
    {
        if (muscleLoadGrowths.TryGetValue(muscleName, out var curve))
        {
            if (curve == null) return 0f;

            float maxLoad = MaxLiftableLoad;
            return curve.GetValue(maxLoad > 0f ? (Load / maxLoad) : 0f);
        }

        return 0f;
    }

    protected Dictionary<string, WorkoutMuscleState> muscleStates = new Dictionary<string, WorkoutMuscleState>();
    public Dictionary<string, WorkoutMuscleState> MuscleStates
    {
        get => GetMuscleStates();
        set => SetMuscleStates(value);
    }
    public void SetMuscleStates(Dictionary<string, WorkoutMuscleState> muscleStates)
    {
        this.muscleStates = muscleStates;
        //RecalculateMaxLiftableLoad();
    }
    public Dictionary<string, WorkoutMuscleState> GetMuscleStates() => muscleStates;
    public void SetMuscleState(string muscleName, WorkoutMuscleState muscleState)
    {
        muscleStates[muscleName] = muscleState;
        //RecalculateMaxLiftableLoad();
    }
    public WorkoutMuscleState GetMuscleState(string muscleName)
    {
        if (muscleStates.TryGetValue(muscleName, out var state))
        {
            return state;
        }

        return default;
    }


    protected ValueCurve speedByAlignmentCurve;

    protected ValueCurve speedNerfMaxByLoadCurve;
    protected ValueCurve speedNerfMaxByBurnCurve;

    protected ValueCurve loadRatioByRepPositionCurve;
    protected ValueCurve strengthCurve;

    protected ValueCurve staminaDrainByLoadCurve;
    protected ValueCurve staminaDrainByRepPositionCurve;

    public PointCurve RepCurve => workout.repCurve;

    public float MaxGravity => workout.maxGravity;

    public float MinRepSpeed => workout.minRepSpeed;

    public float DefaultRepSpeed => workout.defaultRepSpeed;

    public BaseWorkoutProvider(WorkoutBase workout)
    {
        this.workout = workout;

        this.speedByAlignmentCurve = workout.SpeedByAlignmentCurve;

        this.speedNerfMaxByLoadCurve = workout.SpeedNerfMaxByLoadCurve;
        this.speedNerfMaxByBurnCurve = workout.SpeedNerfMaxByBurnCurve;

        this.staminaDrainByLoadCurve = workout.StaminaDrainByLoadCurve;

        this.staminaDrainByRepPositionCurve = workout.StaminaDrainByRepPositionCurve;
        this.loadRatioByRepPositionCurve = workout.LoadRatioByRepPositionCurve;
    }

    public void Dispose() { }

    public virtual void Advance()
    {
        float tickRateDelta = TickRateDelta;

        if (IsTransitioningPerformerState)
        {
            performerStateTransitionTimeLeft -= tickRateDelta;
            if (performerStateTransitionTimeLeft <= 0f)
            {
                transitionPerformerState = false;
                performerStateTransitionTime = performerStateTransitionTimeLeft = 0f; 
                SetPerformerState(nextPerformerState); 
                nextPerformerState = WorkoutPerformerState.Null;
            }
        }

        RecalculateMaxLiftableLoad();
        float liftableLoadRatio = MaxLiftableLoad > 0f ? (Load / MaxLiftableLoad) : 1f;

        var prevState = (WorkoutPerformanceState)GetCurrentPerformanceState();
        if (performanceStates.Count <= 0)
        {
            var baseState = GetState();
            prevState[nameof(WorkoutPerformanceState.Attributes.stamina)] = baseState[nameof(BaseWorkoutState.Attributes.stamina)];
            prevState[nameof(WorkoutPerformanceState.Attributes.alignment)] = 1f;
        }

        var newState = prevState;
        if (newState.HasReachedApex)
        {
            if (RepPosition < 0.001f) newState.attributes.reachedApex = 0f; 
        }
        else 
        {
            if (RepPosition >= 0.999f) newState.attributes.reachedApex = 1f; 
        }

        float stamina = prevState[nameof(WorkoutPerformanceState.Attributes.stamina)];

        float inputX = 0f;
        float inputY = 0f;

        if (!ignoreInput)
        {
            inputX = inputManager[DefaultWorkoutInputManager._inputX];
            inputY = inputManager[DefaultWorkoutInputManager._inputY];
        }

        bool inputIsActive = inputX != 0f || inputY != 0f;

        float repSign = (newState.HasReachedApex ? -1f : 1f);
        float alignment = 0f;
        if (inputIsActive)
        {
            var targetDir = workout.repCurve.GetDirection(prevState[nameof(WorkoutPerformanceState.Attributes.repPosition)]) * repSign;
            alignment = Vec2.Dot(new Vec2(inputX, inputY).normalized, targetDir);
            if (stamina <= 0f)
            {
                alignment = Math.Min(alignment, prevState[nameof(WorkoutPerformanceState.Attributes.alignment)]); // can't improve alignment when out of stamina
            }
            newState[nameof(WorkoutPerformanceState.Attributes.alignment)] = alignment;
        }
        
        float repSpeed = newState[nameof(WorkoutPerformanceState.Attributes.repSpeed)];
        float targetRepSpeedFactor = speedByAlignmentCurve.GetValue(alignment);
        float targetRepSpeed = 0f;

        if (liftableLoadRatio > 1f)
        {
            targetRepSpeed = -workout.maxGravity * (inputIsActive ? (liftableLoadRatio - Mathf.Max(0f, alignment)) : liftableLoadRatio);
        } 
        else 
        {
            targetRepSpeed = inputIsActive ? ((workout.defaultRepSpeed * targetRepSpeedFactor) + (workout.minRepSpeed * Mathf.Max(0f, 1f - targetRepSpeedFactor))) * repSign : (-workout.maxGravity * liftableLoadRatio);   
        }

        repSpeed = repSpeed + Math.Clamp(targetRepSpeed - repSpeed, -workout.repDeceleration * tickRateDelta, workout.repAcceleration * tickRateDelta);
        //Debug.Log($"algn {alignment} :: trsf {targetRepSpeedFactor} :: {speedByAlignmentCurve.Length} :: rs: {repSpeed} ::: trs {targetRepSpeed}"); 

        float repPosition = newState[nameof(WorkoutPerformanceState.Attributes.repPosition)];
        repPosition = Math.Clamp(repPosition + repSpeed * tickRateDelta, 0f, 1f);
        newState[nameof(WorkoutPerformanceState.Attributes.repPosition)] = repPosition;
        newState[nameof(WorkoutPerformanceState.Attributes.repSpeed)] = repSpeed; 

        performanceStates.Add(newState);
    }

    protected float repPosition;
    public float RepPosition => repPosition;

    private float frameTime;
    public virtual void Update(float deltaTime)
    {
        if (workout == null) return;

        inputManager.Update(deltaTime);

        float tickRateDelta = TickRateDelta;

        frameTime += deltaTime;
        while(frameTime > tickRateDelta)
        {
            frameTime -= tickRateDelta;
            Advance();
        }

        var currentPerformanceState = GetCurrentPerformanceState();

        var targetRepPosition = currentPerformanceState["repPosition"];
        //repPosition = repPosition + (targetRepPosition - repPosition) * Math.Clamp(deltaTime * 5f, 0f, 1f);
        repPosition = targetRepPosition;
    }



}

/*public class NetworkedWeightLiftingWorkout : WeightLiftingWorkout, INetworkedWorkoutProvider
{
    public NetworkedWeightLiftingWorkout(
        float loadRangeMin, float loadRangeMax, float loadLimit, float minRepSpeed, float defaultRepSpeed, float repAcceleration, float repDeceleration, float baseStaminaDrain,
        PointCurve repCurve, ValueCurve speedByAlignmentCurve, ValueCurve speedNerfMaxByLoadCurve, ValueCurve speedNerfMaxByBurnCurve, ValueCurve gravityByLoadCurve, ValueCurve staminaDrainByLoadCurve, ValueCurve staminaDrainByRepPositionCurve = null, ValueCurve loadRatioByRepPositionCurve = null)
        : base(
        loadRangeMin, loadRangeMax, loadLimit, minRepSpeed, defaultRepSpeed, repAcceleration, repDeceleration, baseStaminaDrain,
        repCurve, speedByAlignmentCurve, speedNerfMaxByLoadCurve, speedNerfMaxByBurnCurve, gravityByLoadCurve, staminaDrainByLoadCurve, staminaDrainByRepPositionCurve, loadRatioByRepPositionCurve)
    {
    }
}*/

public interface IWorkoutPerformer
{

    public MuscleDataLR GetMuscleDataUnsafe(int groupIndex);
    public MuscleDataLR GetMuscleData(int groupIndex);
    public void SetMuscleDataUnsafe(int groupIndex, MuscleDataLR data);
    public void SetMuscleData(int groupIndex, MuscleDataLR data);

    public int IndexOfMuscleGroup(string groupName, bool caseSensitive = false);

}