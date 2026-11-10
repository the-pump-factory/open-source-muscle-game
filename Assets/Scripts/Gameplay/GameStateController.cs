using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;
using UnityEngine.Audio;

using Unity.Mathematics;

using Swole.Animation;
using Swole.API.Unity.Animation;
using Swole.Modding;

namespace Swole.API.Unity
{

    [ExecuteAlways]
    public class GameStateController : MonoBehaviour, Swole.IExecutableBehaviour
    {

        public const string converterId_assetId = "assetId";
        public const string converterId_taskId = "taskId";
        public const string converterId_animationId = "animationId";
        public const string converterId_animatorId = "animatorId";
        public const string converterId_soundSourceId = "soundSourceId";
        public const string converterId_soundAssetPath = "soundAssetPath";
        public const string converterId_soundMixerAssetPath = "soundMixerAssetPath";
        public const string converterId_creationAnimationCurvePath = "creationAnimationCurvePath";

        public const string converterId_animationIdCollection = "animationIdCollection";
        public const string converterId_soundIdCollection = "soundIdCollection";
        public const string converterId_taskIdCollection = "taskIdCollection";

        public const string enumId_animatorIndex = "animatorIndex";

#if UNITY_EDITOR

        /*protected virtual void OnValidate()
        {
            VerifyIndices(); 
        }*/
        protected virtual void VerifyIndices()
        {
            /*if (animatedObjects != null)
            {
                foreach (var obj in animatedObjects)
                {
                    if (obj == null) continue;
                    while (true) if (obj.VerifyIndices(null, null, null)) break;
                }
            }*/
        }

#endif

        public delegate void IndexChangeDelegate(int oldIndex, int newIndex);
        public static int EnsureIndexInCollection(int oldIndex, ICollection oldCollection, ICollection newCollection, IndexChangeDelegate onIndexChange)
        {
            int i = 0;
            foreach(var obj in oldCollection)
            {
                if (i == oldIndex)
                {
                    int j = 0;
                    foreach(var obj2 in newCollection)
                    {
                        if (ReferenceEquals(obj, obj2)) 
                        {
                            if (i != j) onIndexChange?.Invoke(i, j);
                            return j;
                        }

                        j++;
                    }

                    break;
                }

                i++;
            }

            return -1;// i;
        }

        public static int ExecutionPriority = CustomAnimatorUpdater.StartAnimationBehaviourPriority - 3; 
        public int Priority => ExecutionPriority;
        public int CompareTo(Swole.IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);

        public int UpdatePriority => ExecutionPriority;
        public int LateUpdatePriority => ExecutionPriority;
        public int FixedUpdatePriority => ExecutionPriority;

        public virtual void OnPreFixedUpdate() { }
        public virtual void OnPostFixedUpdate() { }

        [Serializable]
        public struct ModifyingTask
        {
            [PropertyTypeConverter(converterId_taskId)]
            public int taskIndex;
            public float mix;
        }
        [Serializable]
        public class ImportedAnimation : ICloneable
        {

            public object Clone() => Duplicate();
            public ImportedAnimation Duplicate()
            {
                var clone = new ImportedAnimation();

                var fields = typeof(ImportedAnimation).GetFields();
                foreach (var field in fields)
                {
                    var val = field.GetValue(this);
                    if (field.FieldType.IsArray || typeof(Array).IsAssignableFrom(field.FieldType) || typeof(ICollection).IsAssignableFrom(field.FieldType))
                    {
                        if (val is ICloneable cloneable) val = cloneable.Clone();
                    }

                    field.SetValue(clone, val);
                }

                return clone;
            }

            [EditableAlias("assetName")]
            public string name;
            [NonEditable]
            public string displayName;
            public string Name => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
            public string package;
            [NonSerialized]
            protected CustomAnimation asset;
            public CustomAnimation Asset
            {
                get
                {
                    if (asset == null) asset = Swole.ContentManager.FindContent<CustomAnimation>(name, new Swole.PackageIdentifier(package));
                    return asset;
                }
            }
            public float Length
            {
                get
                {
                    var asset = Asset;
                    return asset == null ? 0 : asset.LengthInSeconds;
                }
            }

            [EditAsEnum(enumId_animatorIndex)]
            public int animatorIndex;

            [NonSerialized]
            public IAnimationLayer layer;
            [NonSerialized]
            public int lastModifyingFrame;
            [NonSerialized]
            public readonly List<ModifyingTask> lastModifyingTasks = new List<ModifyingTask>();

            public bool updateAfterIndex;
            public int indexToUpdateAfter;

            public float baseMix = 1;

            public float speed = 1;
            public bool loop = false;

            public float startTimeNormalized;

            public bool usePauseTime;
            public float pauseTimeNormalized;

            public float fadeIn;
            public float fadeOut;

            [PropertyTypeConverter(converterId_animationIdCollection), PropertyTypeConverter(converterId_animationId, 1)]
            public int[] nullifyingDependencies; 
            public float GetMix(ImportedAnimation[] anims)
            {
                if (nullifyingDependencies == null) return 1;

                float nullMix = 0;
                foreach (var nullifier in nullifyingDependencies) nullMix = Mathf.Max(anims[nullifier].layer.Mix, nullMix);

                return Mathf.Max(0, 1 - nullMix);
            }
        }

        [Serializable]
        public class Sound : ICloneable
        {
            public object Clone() => Duplicate();
            public Sound Duplicate()
            {
                var clone = new Sound();

                var fields = typeof(Sound).GetFields();
                foreach (var field in fields)
                {
                    var val = field.GetValue(this);
                    if (field.FieldType.IsArray || typeof(Array).IsAssignableFrom(field.FieldType) || typeof(ICollection).IsAssignableFrom(field.FieldType))
                    {
                        if (val is ICloneable cloneable) val = cloneable.Clone();
                    }

                    field.SetValue(clone, val);
                }

                return clone;
            }

            [PropertyTypeConverter( converterId_soundAssetPath)]
            public AudioClip clip;
            public float volume = 1;
            public float pitch = 1;
            [PropertyTypeConverter(converterId_soundSourceId)]
            public AudioSource source;
            [PropertyTypeConverter(converterId_soundMixerAssetPath)]
            public AudioMixerGroup mixerGroup;
            public float startTimeNormalized;
            public float fadeIn;
            public float fadeOut;
            public float ClipLength
            {
                get
                {
                    return clip == null ? 0 : clip.length;
                }
            }
            public float Length
            {
                get
                {
                    return clip == null ? 0 : (pitch <= 0 ? 0 : (clip.length / pitch));
                }
            }
            public float NormalizedTime
            {
                get
                {
                    return source == null ? (claim == null || !claim.IsValid || ClipLength <= 0 ? 0 : (claim.Source.time / clip.length)) : (source.time / clip.length);
                }
            }
            public bool IsPlaying
            {
                get
                {
                    return source == null ? (claim == null || !claim.IsValid ? false : (claim.Source.isPlaying && ReferenceEquals(claim.Source.clip, clip))) : (source.isPlaying && ReferenceEquals(source.clip, clip)); 
                }
            }
            public float Volume
            {
                get
                {
                    return source == null ? (claim == null || !claim.IsValid ? 0 : claim.Source.volume) : source.volume;
                }
                
                set
                {
                    if (source == null)
                    {
                        if (claim == null || !claim.IsValid) return;

                        claim.Source.volume = value;
                    } 
                    else
                    {
                        source.volume = value;
                    }
                }
            }
            public float Pitch
            {
                get
                {
                    return source == null ? (claim == null || !claim.IsValid ? 0 : claim.Source.pitch) : source.pitch;
                }

                set
                {
                    if (source == null)
                    {
                        if (claim == null || !claim.IsValid) return;

                        claim.Source.pitch = value;
                    }
                    else
                    {
                        source.pitch = value;
                    }
                }
            }

            public void SetScaledVolume(float volumeScale)
            {
                Volume = volume * volumeScale;
            }

            private float lastPlayedTime = float.MinValue;
            public float LastPlayedTime => lastPlayedTime;
            [NonSerialized]
            public PersistentAudioPlayer.AudioSourceClaim claim;
            public void Play(float startTime, float volumeMul = 1, float pitchMul = 1)
            {
                if (clip == null) return;

                if (source == null)
                {
                    if (claim == null || !claim.IsValid) claim = PersistentAudioPlayer.Get2DSourceAndPlay(clip, volume * volumeMul, pitch * pitchMul, mixerGroup);
                    claim.Source.time = startTime;
                    claim.Source.Play();
                    lastPlayedTime = Time.timeSinceLevelLoad;
                } 
                else
                {
                    source.clip = clip;
                    source.time = startTime;
                    source.pitch = pitch * pitchMul;
                    source.volume = volume * volumeMul; 
                    source.outputAudioMixerGroup = mixerGroup;
                    source.Play();
                    lastPlayedTime = Time.timeSinceLevelLoad;
                }
            }
            public void Stop()
            {
                if (source == null)
                {
                    if (claim == null || !claim.IsValid) return;
                    claim.Source.Stop();
                } 
                else
                {
                    source.Stop(); 
                }
            }
        }

        [Serializable]
        public struct PlayRequirement
        {
            [PropertyTypeConverter(converterId_assetId)]
            public int index;
            public float timeSinceLastPlayed;
        }
        [Serializable]
        public struct SelectableIndex
        {
            [PropertyTypeConverter(converterId_assetId)]
            public int index;
            public bool ignoreChance;
            public float chance;
            public bool fulfillAllPlayRequirements;
            public PlayRequirement[] playRequirements;
        }
        [Serializable]
        public class EventReactionIndex
        {
            public string eventName;
            public bool dontNormalizeChances;
            public SelectableIndex[] indices;
            public bool avoidRepition;
            public Vector2 randomRange;
            public bool influencedBySenderMix;
            public float minMix;
            public bool influencedBySenderSpeed;
            public bool syncWithSender;
            public float f1;

            [NonSerialized]
            public int lastIndex;

            public CancellationTokenSource existingRoutineToken;
        }

        [Serializable]
        public struct AssetUsageParameters
        {
            [PropertyTypeConverter(converterId_assetId)]
            public int assetIndex;

            public float startTime;
            public float stopTime;
            public bool overrideLocalStartTimeNormalized;
            public float localStartTimeNormalized;
            public bool overrideLocalSpeed;
            public float localSpeed;
            public float mix;
            public float fadeIn;
            public float fadeOut;
            public bool dontNullify;
            public bool dontNullifyOther;

            public TaskDependency[] mixDependencies;
        }

        [Serializable]
        public enum TaskDependencyType
        {
            Delegate, CustomProperty, BreastSize, MuscleSize, ParentTaskMix, ParentTaskTime, ParentAnimationMix, ParentAnimationTime
        }
        [Serializable]
        public enum TaskDependencyMixType
        {
            Multiply, Divide, Add, Subtract, Min, Max
        }
        [Serializable]
        public enum TaskDependencyIndexWeightType
        {
            Average, Min, Max
        }
        [Serializable]
        public class TaskDependencyPair
        {
            public string name;

            [SerializeField, 
                PropertyTypeConverter(converterId_animationId, 0, -3, nameof(TaskDependency.type), TaskDependencyType.ParentAnimationTime, TaskDependencyType.ParentAnimationMix),
                PropertyTypeConverter(converterId_taskId, 0, -3, nameof(TaskDependency.type), TaskDependencyType.ParentTaskTime, TaskDependencyType.ParentTaskMix)]
            public int index; 
            public float weight;
             
            public int GetIndex(object owner = null)
            {
                if (index < 0)
                {
                    if (owner is MuscularRenderedCharacter characterMuscle)
                    {
                        index = characterMuscle.GetMuscleGroupIndex(name);
                    }
                }

                return index;
            }
            public void SetIndex(int index) => this.index = index;
        }
        [Serializable]
        public struct TaskDependency
        {
            [RedrawOnEdit]
            public TaskDependencyType type;
            public TaskDependencyMixType mixType;
            public string customPropertyName;
            public UnityEngine.Object customPropertyTarget;
            public UnityEngine.MonoBehaviour customPropertyTargetBehaviour;
            public float minValue;
            public float maxValue;
            [PropertyTypeConverter(converterId_creationAnimationCurvePath)]
            public AnimationCurve normalizedValueCurve; 
            public bool clamp;
            public bool remap;
            public Vector2 remapping;
            public TaskDependencyIndexWeightType indexWeightType;
            public TaskDependencyPair[] indices;
            public FloatParameterlessDelegate valueDelegate;

            private float GetNormalizedValue(float value)
            {
                value = (value - minValue) / (maxValue - minValue);
                if (clamp) value = Mathf.Clamp01(value); 
                if (normalizedValueCurve == null || normalizedValueCurve.length <= 0) return value;

                return normalizedValueCurve.Evaluate(value);
            }
            private float Mix(float inputMix, float deltaMix)
            {
                switch(mixType)
                {
                    case TaskDependencyMixType.Multiply:
                        return inputMix * deltaMix;
                    case TaskDependencyMixType.Divide:
                        return inputMix / deltaMix;
                    case TaskDependencyMixType.Add:
                        return inputMix + deltaMix;
                    case TaskDependencyMixType.Subtract:
                        return inputMix - deltaMix;
                    case TaskDependencyMixType.Min:
                        return Mathf.Min(inputMix, deltaMix);
                    case TaskDependencyMixType.Max:
                        return Mathf.Max(inputMix, deltaMix); 
                }
                return inputMix;
            }
            private float ModifyDeltaMix(TaskDependencyPair index, float inputMix, float inputValue)
            {
                switch (indexWeightType)
                {
                    case TaskDependencyIndexWeightType.Average:
                        return inputMix + inputValue * index.weight;
                    case TaskDependencyIndexWeightType.Min:
                        return Mathf.Min(inputMix, inputValue * index.weight);
                    case TaskDependencyIndexWeightType.Max:
                        return Mathf.Max(inputMix, inputValue * index.weight); 
                }

                return inputMix;
            }
            public float GetMix(bool isFirst, float inputMix, AnimationTask[] tasks, ImportedAnimation[] animations, MuscularRenderedCharacter characterMuscle)
            {
                float deltaMix = 1;
                switch (type)
                {
                    case TaskDependencyType.Delegate:
                        if (valueDelegate != null)
                        {
                            deltaMix = GetNormalizedValue(valueDelegate());
                        }
                        break;

                    case TaskDependencyType.CustomProperty:
                        deltaMix = 0;
                        if (customPropertyTargetBehaviour != null)
                        {
                            deltaMix = GetNormalizedValue((float)customPropertyTargetBehaviour.GetType().GetProperty(customPropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetValue(customPropertyTargetBehaviour));
                        }
                        else if (customPropertyTarget != null)
                        {
                            deltaMix = GetNormalizedValue((float)customPropertyTarget.GetType().GetProperty(customPropertyName, System.Reflection.BindingFlags.Public).GetValue(customPropertyTarget));
                        }
                        break;

                    case TaskDependencyType.BreastSize:
                        if (characterMuscle != null)
                        {
                            deltaMix = GetNormalizedValue(characterMuscle.BreastPresence);
                        }
                        break;

                    case TaskDependencyType.MuscleSize:
                        if (characterMuscle != null && indices != null && indices.Length > 0)
                        {
                            deltaMix = 0;
                            float weightTotal = 0;
                            foreach (var muscleIndex in indices)
                            {
                                weightTotal += muscleIndex.weight;
                                deltaMix = ModifyDeltaMix(muscleIndex, deltaMix, GetNormalizedValue(characterMuscle.GetMuscleGroupMass(muscleIndex.GetIndex(characterMuscle))));
                            }
                            if (indexWeightType == TaskDependencyIndexWeightType.Average) deltaMix = deltaMix / weightTotal;
                        }
                        break;

                    case TaskDependencyType.ParentTaskMix:
                        if (indices != null && indices.Length > 0)
                        {
                            deltaMix = indexWeightType == TaskDependencyIndexWeightType.Min ? float.MaxValue : (indexWeightType == TaskDependencyIndexWeightType.Max ? float.MinValue : 0);
                            float weightTotal = 0;
                            foreach (var taskIndex in indices)
                            {
                                weightTotal += taskIndex.weight;
                                deltaMix = ModifyDeltaMix(taskIndex, deltaMix, GetNormalizedValue(tasks[taskIndex.GetIndex()].DynamicMix));
                            }
                            if (indexWeightType == TaskDependencyIndexWeightType.Average) deltaMix = deltaMix / weightTotal;
                        }
                        break;

                    case TaskDependencyType.ParentTaskTime:
                        if (indices != null && indices.Length > 0)
                        {
                            deltaMix = indexWeightType == TaskDependencyIndexWeightType.Min ? float.MaxValue : (indexWeightType == TaskDependencyIndexWeightType.Max ? float.MinValue : 0);
                            float weightTotal = 0;
                            foreach (var taskIndex in indices)
                            {
                                weightTotal += taskIndex.weight;
                                deltaMix = ModifyDeltaMix(taskIndex, deltaMix, GetNormalizedValue(tasks[taskIndex.GetIndex()].Time));
                            }
                            if (indexWeightType == TaskDependencyIndexWeightType.Average) deltaMix = deltaMix / weightTotal;
                        }
                        break;

                    case TaskDependencyType.ParentAnimationMix:
                        if (indices != null && indices.Length > 0)
                        {
                            deltaMix = indexWeightType == TaskDependencyIndexWeightType.Min ? float.MaxValue : (indexWeightType == TaskDependencyIndexWeightType.Max ? float.MinValue : 0);
                            float weightTotal = 0;
                            foreach (var animationIndex in indices)
                            {
                                weightTotal += animationIndex.weight;
                                deltaMix = ModifyDeltaMix(animationIndex, deltaMix, GetNormalizedValue(animations[animationIndex.GetIndex()].layer.Mix));
                            }
                            if (indexWeightType == TaskDependencyIndexWeightType.Average) deltaMix = deltaMix / weightTotal;
                        }
                        break;

                    case TaskDependencyType.ParentAnimationTime:
                        if (indices != null && indices.Length > 0)
                        {
                            deltaMix = indexWeightType == TaskDependencyIndexWeightType.Min ? float.MaxValue : (indexWeightType == TaskDependencyIndexWeightType.Max ? float.MinValue : 0);
                            float weightTotal = 0;
                            foreach (var animationIndex in indices)
                            {
                                weightTotal += animationIndex.weight;
                                deltaMix = ModifyDeltaMix(animationIndex, deltaMix, GetNormalizedValue(animations[animationIndex.GetIndex()].layer.ActiveState.GetTime()));
                            }
                            if (indexWeightType == TaskDependencyIndexWeightType.Average) deltaMix = deltaMix / weightTotal;
                        }
                        break;
                }

                if (clamp) Mathf.Clamp01(deltaMix);
                if (remap) deltaMix = math.remap(0, 1, remapping.x, remapping.y, deltaMix);

                if (isFirst && (mixType == TaskDependencyMixType.Max)) inputMix = float.MinValue;

                return Mix(inputMix, deltaMix);
            }
        }
        [Serializable]
        public class AnimationTask : ICloneable
        {
            public object Clone() => Duplicate(); 
            public AnimationTask Duplicate()
            {
                var clone = new AnimationTask();

                var fields = typeof(AnimationTask).GetFields();
                foreach (var field in fields) 
                {
                    var val = field.GetValue(this);
                    if (field.FieldType.IsArray || typeof(Array).IsAssignableFrom(field.FieldType) || typeof(ICollection).IsAssignableFrom(field.FieldType))
                    {
                        if (val is ICloneable cloneable) val = cloneable.Clone();
                    } 

                    field.SetValue(clone, val);
                }

                return clone;
            }

            public event VoidParameterlessDelegate OnStart;
            public event VoidParameterlessDelegate OnFinishStartFadeIn;
            public event VoidParameterlessDelegate OnPreRun;
            public event VoidParameterlessDelegate OnPostRun;
            public event VoidParameterlessDelegate OnComplete;
            public event VoidParameterlessDelegate OnBeginEndFadeOut;
            public event VoidParameterlessDelegate OnStop;

            public void ClearListeners()
            {
                OnStart = null;
                OnPreRun = null;
                OnPostRun = null;
                OnComplete = null;
                OnStop = null;
            }

            [NonEditable]
            public string name;

            [PropertyTypeConverter(converterId_animationIdCollection)]
            public AssetUsageParameters[] animsToPlay;
            [PropertyTypeConverter(converterId_soundIdCollection)]
            public AssetUsageParameters[] soundsToPlay;

            private static List<int> tempIds = new List<int>();
            public void NullifyAnimationsWithRespectTo(AnimationTask task, ImportedAnimation[] animations, bool allowDuplicates = false)
            {
                if (task == null || animations == null || task.animsToPlay == null || animsToPlay == null) return; 

                foreach(var animId in animsToPlay)
                {
                    if (animId.dontNullify || animId.assetIndex < 0 || animId.assetIndex >= animations.Length) continue;
                    var anim = animations[animId.assetIndex]; 

                    tempIds.Clear();
                    if (anim.nullifyingDependencies != null) tempIds.AddRange(anim.nullifyingDependencies);
                    foreach (var animId2 in task.animsToPlay)
                    {
                        if (animId2.dontNullifyOther) continue;

                        bool isLocal = false;
                        foreach (var localAnimId in animsToPlay)
                        {
                            if (localAnimId.assetIndex == animId2.assetIndex)
                            {
                                isLocal = true;
                                break;
                            }
                        }
                        if (isLocal) continue; // ignore anims that are present in this task as well 

                        if (allowDuplicates || !tempIds.Contains(animId2.assetIndex)) tempIds.Add(animId2.assetIndex);
                    }
                    anim.nullifyingDependencies = tempIds.ToArray();
                }
            }
            public void NullifyAnimationsWithRespectTo(ImportedAnimation[] animations, bool allowDuplicates, params AnimationTask[] tasks)
            {
                if (tasks == null || animations == null || animsToPlay == null) return;

                foreach (var animId in animsToPlay)
                {
                    if (animId.dontNullify || animId.assetIndex < 0 || animId.assetIndex >= animations.Length) continue;
                    var anim = animations[animId.assetIndex];

                    tempIds.Clear();
                    if (anim.nullifyingDependencies != null) tempIds.AddRange(anim.nullifyingDependencies);
                    foreach (var task in tasks)
                    {
                        if (task.animsToPlay == null) continue;
                        foreach (var animId2 in task.animsToPlay)
                        {
                            if (animId2.dontNullifyOther) continue;

                            bool isLocal = false; 
                            foreach (var localAnimId in animsToPlay)
                            {
                                if (localAnimId.assetIndex == animId2.assetIndex)
                                {
                                    isLocal = true;  
                                    break;
                                }
                            }
                            if (isLocal) continue; // ignore anims that are present in this task as well 

                            if (allowDuplicates || !tempIds.Contains(animId2.assetIndex)) tempIds.Add(animId2.assetIndex);
                        }
                    }
                    anim.nullifyingDependencies = tempIds.ToArray(); 
                }

            }

            public bool loop;

            [NonSerialized]
            private bool firstRun;

            public float speed = 1;
            public float animationSpeed = 1;
            public float soundVolume = 1;
            public float soundPitch = 1;

            public float mix = 1;
            private float dynamicMix;
            public float DynamicMix => dynamicMix;

            public float fadeIn;
            public float fadeOut;

            [NonSerialized]
            private bool startFlag;
            public BoolParameterlessDelegate startCondition;
            public bool CanStart(bool resetFlag = true)
            {
                bool start = startFlag || (startCondition != null && startCondition());
                if (resetFlag) startFlag = false;

                return start;
            }
            public bool StartIfNotRunning()
            {
                if (IsRunning) return false;
                startFlag = true;
                return true;
            }

            [NonSerialized]
            private bool stopFlag;
            public BoolParameterlessDelegate stopCondition;
            public bool CanStop(bool resetFlag = true)
            {
                bool stop = stopFlag || (stopCondition != null && stopCondition());
                if (resetFlag) stopFlag = false;

                return stop;
            }
            public bool StopIfRunning()
            {
                if (!IsRunning) return false; 
                stopFlag = true;
                return true;
            }

            [NonSerialized]
            private bool isRunning;
            public bool IsRunning => isRunning;
            public bool IsOrIsGoingToRun => IsRunning || startFlag;
            public bool HasFullyStopped
            {
                get
                {
                    if (IsRunning) return false;
                    return (stopFadeTime <= 0 || time >= stoppedTime + stopFadeTime);
                }
            }
            public bool HasOrWillFullyStop(float deltaTime)
            {
                if (HasFullyStopped) return true;
                
                float t = time + deltaTime * speed;
                return IsRunning ? (stopFadeTime <= 0 && t >= endTime) : (stopFadeTime <= 0 || t >= stoppedTime + stopFadeTime); 
            }

            [NonSerialized]
            private float time;
            //public float Time => time;
            public float Time
            {
                set => time = value;
                get => time;
            }
            private float prevTime;
            public float PreviousTime => prevTime;
            public void SetAnimationTime(ImportedAnimation[] animations, float animationTime)
            {
                if (syncAnimationTimeWithTaskTime) Time = animationTime;

                if (animsToPlay != null)
                {
                    foreach (var assetInfo in animsToPlay)
                    {
                        //if (assetInfo.assetIndex < 0 || assetInfo.assetIndex >= animations.Length) continue;

                        var anim = animations[assetInfo.assetIndex];
                        anim.layer.ActiveState.SetTime(animationTime); 
                    }
                }
            }
            public void SetNormalizedAnimationTime(ImportedAnimation[] animations, float normalizedAnimationTime)
            {
                if (syncAnimationTimeWithTaskTime) Time = normalizedAnimationTime * endTime;

                if (animsToPlay != null)
                {
                    foreach (var assetInfo in animsToPlay)
                    {
                        //if (assetInfo.assetIndex < 0 || assetInfo.assetIndex >= animations.Length) continue;

                        var anim = animations[assetInfo.assetIndex];
                        anim.layer.ActiveState.SetNormalizedTime(normalizedAnimationTime);
                    }
                }
            }
            [NonSerialized]
            private float stoppedTime;
            public bool playOnStart;
            public bool syncAnimationTimeWithTaskTime;
            public bool autoCalculateEndTime = true;
            public float autoCalculationEndTimeScale = 1;
            public float endTime;
            public float startFadeTime;
            public float stopFadeTime;

            [NonSerialized]
            public bool skipStartFade;
            [NonSerialized]
            public bool skipStopFade;
            [NonSerialized]
            public bool runLate;

            public void AutoCalculateEndTime(ImportedAnimation[] animations, Sound[] sounds)
            {
                endTime = 0;

                if (animsToPlay != null)
                {
                    foreach (var animId in animsToPlay)
                    {
                        if (animId.assetIndex < 0 || animId.assetIndex >= animations.Length) continue;

                        var anim = animations[animId.assetIndex];

                        float localStartTime = ((animId.overrideLocalStartTimeNormalized ? animId.localStartTimeNormalized : anim.startTimeNormalized) * anim.Length);
                        float localSpeed = (animId.overrideLocalSpeed ? animId.localSpeed : anim.speed);
                        float tempSpeed = animationSpeed * localSpeed;
                        if (tempSpeed == 0) continue;

                        endTime = Mathf.Max((((tempSpeed < 0 ? localStartTime : (anim.Length - localStartTime)) / (Mathf.Abs(animationSpeed) * Mathf.Abs(localSpeed))) - 0.0001f) + animId.startTime, endTime);
                    }
                }

                if (soundsToPlay != null)
                {
                    foreach (var soundId in soundsToPlay)
                    {
                        if (soundId.assetIndex < 0 || soundId.assetIndex >= sounds.Length) continue;

                        var sound = sounds[soundId.assetIndex];
                        float localStartTime = ((soundId.overrideLocalStartTimeNormalized ? soundId.localStartTimeNormalized : sound.startTimeNormalized) * sound.ClipLength);
                        float localSpeed = (soundId.overrideLocalSpeed ? soundId.localSpeed : 1); 
                        float tempSpeed = soundPitch * sound.pitch * localSpeed;
                        if (tempSpeed == 0) continue; 

                        endTime = Mathf.Max(((tempSpeed < 0 ? localStartTime : (sound.ClipLength - localStartTime)) / tempSpeed) + soundId.startTime, endTime); 
                    }
                }

                if (autoCalculationEndTimeScale <= 0) autoCalculationEndTimeScale = 1; 
                endTime = endTime * autoCalculationEndTimeScale;
            }

            public void ResetToStartTimes(ImportedAnimation[] animations, Sound[] sounds)
            {
                if (animsToPlay != null)
                {
                    foreach (var assetInfo in animsToPlay)
                    {
                        //if (assetInfo.assetIndex < 0 || assetInfo.assetIndex >= animations.Length) continue;

                        var anim = animations[assetInfo.assetIndex];
                        anim.layer.ActiveState.SetNormalizedTime(assetInfo.overrideLocalStartTimeNormalized ? assetInfo.localStartTimeNormalized : anim.startTimeNormalized);
                    }
                }
            }
            public void Start(ImportedAnimation[] animations, Sound[] sounds)
            {
                isRunning = true;
                firstRun = true;
                time = 0;
                stoppedTime = float.MinValue;

                if (animsToPlay != null)
                {
                    foreach (var assetInfo in animsToPlay)
                    {
                        //if (assetInfo.assetIndex < 0 || assetInfo.assetIndex >= animations.Length) continue;

                        var anim = animations[assetInfo.assetIndex];

                        anim.layer.Mix = 0;
                        anim.layer.ActiveState.SetNormalizedTime(assetInfo.overrideLocalStartTimeNormalized ? assetInfo.localStartTimeNormalized : anim.startTimeNormalized);
                        var mc = anim.layer.GetMotionControllerUnsafe(anim.layer.ActiveState.MotionControllerIndex);
                        mc.BaseSpeed = 0;
                    }
                }

                OnStart?.Invoke();
            }
            public void StartImmediate(ImportedAnimation[] animations, Sound[] sounds)
            {
                Start(animations, sounds);
                firstRun = false;
            }
            public void Stop(ImportedAnimation[] animations, Sound[] sounds)
            {
                stoppedTime = time;
                isRunning = false;
                if (stopFadeTime <= 0)
                {
                    StopImmediate(animations, sounds);
                }
            }
            public void StopImmediate(ImportedAnimation[] animations, Sound[] sounds)
            {
                isRunning = false;
                skipStartFade = false;
                skipStopFade = false;
                time = Mathf.Max(endTime, stoppedTime) + Mathf.Max(0, stopFadeTime) + Mathf.Epsilon; 

                foreach (var soundId in soundsToPlay)
                {
                    if (soundId.assetIndex < 0 || soundId.assetIndex >= sounds.Length) continue;

                    var sound = sounds[soundId.assetIndex];
                    if (sound.claim != null && sound.claim.IsValid) sound.claim.Release();
                }

                OnStop?.Invoke();
            }

            public bool hasLateDependencies;
            public TaskDependency[] dependencies;
            private static float MixDependency(bool isFirst, float mixIn, TaskDependency dependency, AnimationTask[] tasks, ImportedAnimation[] animations, MuscularRenderedCharacter characterMuscle = null)
            {
                return dependency.GetMix(isFirst, mixIn, tasks, animations, characterMuscle);   
            }
            public void Run(int localTaskIndex, AnimationTask[] tasks, ImportedAnimation[] animations, Sound[] sounds, float deltaTime, MuscularRenderedCharacter characterMuscle = null)
            {
                prevTime = time;
                deltaTime = deltaTime * speed;
                time = time + deltaTime;

                float beginEndTime = endTime - stopFadeTime;
                if (IsRunning && time >= beginEndTime && beginEndTime > 0)
                {
                    if (loop)
                    {
                        if (time >= endTime)
                        {
                            time = time - endTime;
                            firstRun = false;
                        }
                    } 
                    else
                    {
                        OnBeginEndFadeOut?.Invoke();
                        Stop(animations, sounds); 
                    }
                }

                dynamicMix = mix;
                if (!IsRunning)
                {
                    float endFadeTime = stoppedTime + stopFadeTime;  
                    if (skipStopFade || stopFadeTime <= 0 || time >= endFadeTime) 
                    {
                        if (skipStopFade || prevTime < endFadeTime) 
                        {
                            OnComplete?.Invoke();  
                            StopImmediate(animations, sounds);   
                        }  
                        
                        return;  
                    }

                    // fade out on task stop
                    dynamicMix = dynamicMix * (1 - Mathf.Clamp01((time - stoppedTime) / stopFadeTime));
                }
                if (dependencies != null)
                {
                    for (int a = 0; a < dependencies.Length; a++)
                    {
                        var dependency = dependencies[a]; 
                        dynamicMix = MixDependency(a == 0, dynamicMix, dependency, tasks, animations, characterMuscle); 
                    }
                }

                OnPreRun?.Invoke();

                //deltaTime = time - prevTime;

                if (firstRun && !skipStartFade && time < startFadeTime && startFadeTime > 0) // task fade on start
                {
                    dynamicMix = dynamicMix * Mathf.Max(0.0001f, Mathf.Min(1, time / startFadeTime)); // prevent fade from multiplying by zero which can cause sounds to not play and events not to fire
                }
                else if (time >= startFadeTime && (prevTime < startFadeTime || (prevTime <= 0 && startFadeTime <= 0)))
                {
                    skipStartFade = false;
                    OnFinishStartFadeIn?.Invoke();   
                }

                if (time < fadeIn && fadeIn > 0) // task fade
                {
                    dynamicMix = dynamicMix * Mathf.Max(0.0001f, Mathf.Min(1, time / fadeIn)); // prevent fade from multiplying by zero which can cause sounds to not play and events not to fire
                }
                else if (time > fadeOut && fadeOut > 0 && fadeOut < endTime)
                {
                    dynamicMix = dynamicMix * (1 - Mathf.Clamp01((time - fadeOut) / (endTime - fadeOut)));
                }

                if (animsToPlay != null)
                {
                    foreach (var assetInfo in animsToPlay)
                    {
                        //if (assetInfo.assetIndex < 0 || assetInfo.assetIndex >= animations.Length) continue;

                        var anim = animations[assetInfo.assetIndex];
                        if (time < assetInfo.startTime || (assetInfo.stopTime > 0 && time > assetInfo.stopTime)) continue;

                        var activeState = anim.layer.ActiveState;

                        float prevAnimTime = prevTime;
                        float newAnimTime = time;
                        float animSpeed = speed;
                        if (!syncAnimationTimeWithTaskTime)
                        {
                            prevAnimTime = activeState.GetTime();
                            animSpeed = (assetInfo.overrideLocalSpeed ? assetInfo.localSpeed : anim.speed) * animationSpeed;
                            newAnimTime = prevAnimTime + deltaTime * animSpeed; 
                        } 

                        activeState.SetTime(newAnimTime);

                        float normTime = activeState.GetNormalizedTime();  
                        if (anim.usePauseTime && IsRunning) // only reset time to pause time if the task is still running
                        {                         
                            if (normTime >= anim.pauseTimeNormalized) 
                            {
                                activeState.SetNormalizedTime(anim.pauseTimeNormalized);
                                normTime = anim.pauseTimeNormalized;
                            }
                        } 
                        else if (dynamicMix > 0)
                        {
                            var mc = anim.layer.GetMotionController(activeState.MotionControllerIndex);
                            if (mc is IAnimationReference aref) 
                            {
                                var animPlayer = aref.AnimationPlayer;
                                animPlayer?.CallAnimationEvents(prevAnimTime, animPlayer.GetLoopedTime(newAnimTime, true), animSpeed, this);   
                            }
                        }


                        float fade = 1;
                        if (assetInfo.mixDependencies != null)
                        {
                            for (int a = 0; a < assetInfo.mixDependencies.Length; a++)
                            {
                                var dependency = assetInfo.mixDependencies[a];
                                fade = MixDependency(a == 0, fade, dependency, tasks, animations, characterMuscle);
                            }
                        }

                        float stopTime = endTime;
                        if (assetInfo.stopTime > 0 && assetInfo.stopTime < endTime) stopTime = assetInfo.stopTime;
                        if (time < assetInfo.fadeIn && assetInfo.fadeIn > 0) // asset usage fade
                        {
                            fade = fade * Mathf.Clamp01((time - assetInfo.startTime) / (assetInfo.fadeIn - assetInfo.startTime));
                        }
                        else if (time > assetInfo.fadeOut && assetInfo.fadeOut > 0 && assetInfo.fadeOut < stopTime)
                        {
                            fade = fade * (1 - Mathf.Clamp01((time - assetInfo.fadeOut) / (stopTime - assetInfo.fadeOut)));
                        }

                        if (normTime < anim.fadeIn && anim.fadeIn > 0) // animation local fade
                        {
                            fade = fade * Mathf.Clamp01(normTime / anim.fadeIn);
                        }
                        else if (normTime > anim.fadeOut && anim.fadeOut > 0 && anim.fadeOut < 1)
                        {
                            fade = fade * (1 - Mathf.Clamp01((normTime - anim.fadeOut) / (1 - anim.fadeOut)));
                        }

                        if (anim.lastModifyingFrame < UnityEngine.Time.frameCount) 
                        {
                            anim.lastModifyingFrame = UnityEngine.Time.frameCount;
                            anim.lastModifyingTasks.Clear();
                        } 

                        float finalMix = 0;
                        float localMix = dynamicMix * anim.baseMix * assetInfo.mix * fade;
                        bool flag = true;
                        for (int a = 0; a < anim.lastModifyingTasks.Count; a++)
                        {
                            var mod = anim.lastModifyingTasks[a];
                            if (mod.taskIndex == localTaskIndex)
                            {
                                mod.mix = localMix;
                                anim.lastModifyingTasks[a] = mod;
                                flag = false;
                            }
                            finalMix = finalMix + mod.mix;
                        }
                        if (flag)
                        {
                            finalMix = finalMix + localMix;
                            anim.lastModifyingTasks.Add(new ModifyingTask() { taskIndex = localTaskIndex, mix = localMix });  
                        }

                        anim.layer.Mix = finalMix;
                    }
                }
                if (soundsToPlay != null)
                {
                    float soundMix = dynamicMix * soundVolume;
                    foreach (var assetInfo in soundsToPlay)
                    {
                        if (assetInfo.assetIndex < 0 || assetInfo.assetIndex >= sounds.Length) continue;

                        var sound = sounds[assetInfo.assetIndex];

                        float localMix = soundMix * assetInfo.mix;
                        if (localMix > 0 && (prevTime < assetInfo.startTime || (assetInfo.startTime <= 0 && prevTime <= 0)) && time >= assetInfo.startTime && (assetInfo.stopTime <= 0 || time < assetInfo.stopTime))
                        {
                            sound.Play((time - assetInfo.startTime) + ((assetInfo.overrideLocalStartTimeNormalized ? assetInfo.localStartTimeNormalized : sound.startTimeNormalized) * sound.ClipLength), localMix, soundPitch * speed * (assetInfo.overrideLocalSpeed ? assetInfo.localSpeed : 1));
                        }

                        if (sound.IsPlaying)
                        {
                            float fade = 1;
                            if (assetInfo.mixDependencies != null)
                            {
                                for (int a = 0; a < assetInfo.mixDependencies.Length; a++)
                                {
                                    var dependency = assetInfo.mixDependencies[a];
                                    fade = MixDependency(a == 0, fade, dependency, tasks, animations, characterMuscle); 
                                }
                            } 

                            float stopTime = endTime;
                            if (assetInfo.stopTime > 0 && assetInfo.stopTime < endTime) stopTime = assetInfo.stopTime;
                            if (time < assetInfo.fadeIn && assetInfo.fadeIn > 0) // asset usage fade
                            {
                                fade = fade * Mathf.Clamp01((time - assetInfo.startTime) / (assetInfo.fadeIn - assetInfo.startTime));
                            }
                            else if (time > assetInfo.fadeOut && assetInfo.fadeOut > 0 && assetInfo.fadeOut < endTime)
                            {
                                fade = fade * (1 - Mathf.Clamp01((time - assetInfo.fadeOut) / (stopTime - assetInfo.fadeOut))); 
                            }

                            float normTime = sound.NormalizedTime;
                            if (normTime < sound.fadeIn && sound.fadeIn > 0) // sound local fade
                            {
                                fade = fade * Mathf.Clamp01(normTime / sound.fadeIn);
                            }
                            else if (normTime > sound.fadeOut && sound.fadeOut > 0 && sound.fadeOut < 1)
                            {
                                fade = fade * (1 - Mathf.Clamp01((normTime - sound.fadeOut) / (1 - sound.fadeOut)));
                            }
                            sound.SetScaledVolume(localMix * fade);
                            if (assetInfo.stopTime > 0 && time >= assetInfo.stopTime) sound.Stop();
                        }

                    }
                }

                OnPostRun?.Invoke();
            }
        }

        [Serializable]
        public class AnimatedObject : IDisposable
        {

            [Serializable]
            public class Animator : IDisposable
            {
                public CustomAnimator instance;
                [NonSerialized]
                private CustomAnimationController controller;
                public CustomAnimationController Controller => controller;

                private int index;
                public int Index => index;

                private static readonly List<ImportedAnimation> tempAnims = new List<ImportedAnimation>();
                private static readonly Dictionary<int, int> tempAnimsRemap = new Dictionary<int, int>();
                public void Initialize(int index, string controllerId, ImportedAnimation[] animations)
                {
                    this.index = index;

                    if (controller == null)
                    {
                        controller = ScriptableObject.CreateInstance<CustomAnimationController>();
                        controller.name = controllerId;

                        tempAnims.Clear();
                        tempAnimsRemap.Clear();
                        for (int a = 0; a < animations.Length; a++)
                        {
                            var anim = animations[a];
                            if (anim.animatorIndex == index) 
                            {
                                tempAnimsRemap[a] = tempAnims.Count;
                                tempAnims.Add(anim);
                            }
                        }
                        for (int a = 0; a < animations.Length; a++)
                        {
                            var anim = animations[a];
                            if (anim.animatorIndex != index || !anim.updateAfterIndex || anim.indexToUpdateAfter < 0 || anim.indexToUpdateAfter >= animations.Length) continue;

                            var afterAnim = animations[anim.indexToUpdateAfter]; 
                            var afterAnimIndex = tempAnims.IndexOf(afterAnim);
                            if (afterAnimIndex >= 0)
                            {
                                var animIndex = tempAnims.IndexOf(anim);
                                if (animIndex >= 0)
                                {
                                    tempAnims.RemoveAt(animIndex);
                                    afterAnimIndex = tempAnims.IndexOf(afterAnim);
                                    if (afterAnimIndex + 1 >= tempAnims.Count) tempAnims.Add(anim); else tempAnims.Insert(afterAnimIndex + 1, anim);  
                                }
                            }
                        }
                        var animRefs = new AnimationReference[tempAnims.Count];
                        var layers = new CustomAnimationLayer[tempAnims.Count];
                        for (int a = 0; a < tempAnims.Count; a++)
                        {
                            var anim = tempAnims[a];
                            animRefs[a] = new AnimationReference(anim.name, anim.Asset, anim.loop ? AnimationLoopMode.Loop : AnimationLoopMode.PlayOnce);

                            CustomAnimationLayer previewLayer = new CustomAnimationLayer();
                            layers[a] = previewLayer;

                            previewLayer.SetActive(true);
                            previewLayer.name = anim.Name;
                            previewLayer.blendParameterIndex = -1;
                            previewLayer.entryStateIndex = 0;
                            previewLayer.mix = 1;
                            previewLayer.IsAdditive = true;
                            previewLayer.motionControllerIdentifiers = new MotionControllerIdentifier[] { new MotionControllerIdentifier() { index = a, type = MotionControllerType.AnimationReference } };

                            CustomAnimationLayerState previewStateMachine = new CustomAnimationLayerState() { name = anim.Name, motionControllerIndex = 0 };
                            previewLayer.states = new CustomAnimationLayerState[] { previewStateMachine };
                        }
                        tempAnims.Clear();

                        controller.animationReferences = animRefs;
                        controller.layers = layers;
                    }

                    //RemoveController(true);
                    ApplyController();
                }

                public void ApplyController()
                {
                    if (instance == null || controller == null) return;

                    instance.RemoveControllerData(controller, true);
                    instance.ApplyController(controller);
                }
                public void RemoveController(bool disposeLayers = true)
                {
                    if (instance == null || controller == null) return;
                    instance.RemoveControllerData(controller, disposeLayers);
                }

                public void Dispose()
                {
                    try
                    {
                        RemoveController();
                    }
                    catch (Exception e)
                    {
                        swole.LogError(e);
                    }

                    try
                    {
                        if (controller != null) 
                        { 
                            if (controller.layers != null)
                            {
                                foreach (var layer in controller.layers) if (layer != null) layer.Dispose();
                                controller.layers = null;
                            }
                            ScriptableObject.Destroy(controller);
                        }
                        controller = null;
                    }
                    catch (Exception e)
                    {
                        swole.LogError(e);
                    }
                }
            }

            public event VoidParameterlessDelegate OnInit;
            public event VoidParameterlessDelegate OnPreUpdate;
            public event VoidParameterlessDelegate OnPostUpdate;
            public event Script.RuntimeEventListenerDelegate OnAnimationEvent;

            public void ClearListeners()
            {
                OnInit = null;
                OnPreUpdate = null;
                OnPostUpdate = null;
                OnAnimationEvent = null;
            }

            public Animator[] animators;
            private MuscularRenderedCharacter characterMuscle;
            public ImportedAnimation[] animations;
            public Sound[] sounds;
            public AnimationTask[] tasks;
            public EventReactionIndex[] soundEvents;

            public int IndexOfAnimation(string animName)
            {
                if (animations == null) return -1;
                for(int a = 0; a < animations.Length; a++) if (animations[a] != null && animations[a].name == animName) return a;
                return -1;
            }
            public bool TryGetAnimationIndex(string animName, out int index)
            {
                index = IndexOfAnimation(animName);
                return index >= 0;
            }

            public int IndexOfSound(string soundName)
            {
                if (sounds == null) return -1;
                for (int a = 0; a < sounds.Length; a++) if (sounds[a] != null && sounds[a].clip != null && sounds[a].clip.name == soundName) return a;
                return -1;
            }
            public bool TryGetSoundIndex(string soundName, out int index)
            {
                index = IndexOfSound(soundName);
                return index >= 0;
            }

            public int IndexOfTask(string taskName)
            {
                if (tasks == null) return -1;
                for (int a = 0; a < tasks.Length; a++) if (tasks[a] != null && tasks[a].name == taskName) return a;
                return -1;
            }
            public bool TryGetTaskIndex(string taskName, out int index)
            {
                index = IndexOfTask(taskName);
                return index >= 0;
            }
             
            public bool TryGetAnimation(string idOrName, out ImportedAnimation animation, out int index)
            {
                animation = default;
                index = -1;
                if (animations == null || string.IsNullOrWhiteSpace(idOrName)) return false;

                idOrName = idOrName.AsID();

                for (int a = 0; a < animations.Length; a++)
                {
                    var anim = animations[a];
                    if ((!string.IsNullOrWhiteSpace(anim.name) && anim.name.AsID() == idOrName) || (!string.IsNullOrWhiteSpace(anim.displayName) && anim.displayName.AsID() == idOrName))
                    {
                        index = a;
                        animation = anim;
                        return true;
                    }
                }

                return false;
            }
            public bool TryGetSound(string idOrName, out Sound sound, out int index)
            {
                sound = default;
                index = -1;
                if (sounds == null || string.IsNullOrWhiteSpace(idOrName)) return false;

                idOrName = idOrName.AsID();

                for (int a = 0; a < sounds.Length; a++)
                {
                    var snd = sounds[a];
                    if ((snd.clip != null && snd.clip.name.AsID() == idOrName))
                    {
                        index = a;
                        sound = snd;
                        return true;
                    }
                }

                return false;
            }
            public bool TryGetTask(string idOrName, out AnimationTask task, out int index)
            {
                task = default;
                index = -1;
                if (tasks == null || string.IsNullOrWhiteSpace(idOrName)) return false;

                idOrName = idOrName.AsID();

                for (int a = 0; a < tasks.Length; a++)
                {
                    var tsk = tasks[a];
                    if ((!string.IsNullOrWhiteSpace(tsk.name) && tsk.name.AsID() == idOrName))
                    {
                        index = a;
                        task = tsk;
                        return true;
                    }
                }

                return false;
            }
            public bool TryGetAsset(string idOrName, out object asset, out int index)
            {
                if (TryGetAnimation(idOrName, out var anim, out index))
                {
                    asset = anim;
                    return true;
                }
                if (TryGetSound(idOrName, out var sound, out index))
                {
                    asset = sound;
                    return true;
                }
                if (TryGetTask(idOrName, out var task, out index))
                {
                    asset = task;
                    return true;
                }

                asset = null;
                index = -1;
                return false;
            }

#if UNITY_EDITOR
            [HideInInspector]
            private ImportedAnimation[] animationsTemp;
            [HideInInspector]
            private Sound[] soundsTemp;
            [HideInInspector]
            private AnimationTask[] tasksTemp;

            private bool VerifyIndices<T>(ref T[] arrayTemp, T[] arrayMain, IndexChangeDelegate onIndexChange)
            {

                if (arrayTemp.Length < arrayMain.Length)
                {
                    var temp = new T[arrayMain.Length];
                    for (int a = 0; a < arrayTemp.Length; a++) temp[a] = arrayTemp[a];
                    arrayTemp = temp;
                }

                for (int a = 0; a < arrayMain.Length; a++)
                {
                    int b = EnsureIndexInCollection(a, arrayTemp, arrayMain, onIndexChange); 
                    if (a != b) 
                    {
                        arrayTemp[a] = arrayMain[a];
                        if (b >= 0) arrayTemp[b] = arrayMain[b]; 
                        return false;
                    }
                }

                return true;
            }
            public bool VerifyIndices(IndexChangeDelegate onAnimationIndexChange, IndexChangeDelegate onSoundIndexChange, IndexChangeDelegate onTaskIndexChange)
            {
                bool valid = true;

                if (animationsTemp != null && animationsTemp.Length > 0)
                {
                    if (!VerifyIndices(ref animationsTemp, animations, (int oldIndex, int newIndex) =>
                    {
                        if (animations != null)
                        {
                            foreach (var anim in animations)
                            {
                                if (anim.nullifyingDependencies != null)
                                {
                                    for (int a = 0; a < anim.nullifyingDependencies.Length; a++)
                                    {
                                        if (anim.nullifyingDependencies[a] == oldIndex) anim.nullifyingDependencies[a] = newIndex;
                                    }
                                }
                            }
                        }

                        if (tasks != null)
                        {
                            foreach (var task in tasks)
                            {
                                if (task.animsToPlay != null)
                                {
                                    for (int a = 0; a < task.animsToPlay.Length; a++)
                                    {
                                        var asset = task.animsToPlay[a];
                                        if (asset.assetIndex == oldIndex)
                                        {
                                            asset.assetIndex = newIndex;
                                        }
                                        if (asset.mixDependencies != null)
                                        {
                                            for (int b = 0; b < asset.mixDependencies.Length; b++)
                                            {
                                                var dep = asset.mixDependencies[b];
                                                if (dep.type != TaskDependencyType.ParentAnimationMix && dep.type != TaskDependencyType.ParentAnimationTime) continue;

                                                if (dep.indices != null)
                                                {
                                                    for (int c = 0; c < dep.indices.Length; c++)
                                                    {
                                                        var depIndex = dep.indices[c];
                                                        if (depIndex.GetIndex() == oldIndex) depIndex.SetIndex(newIndex);
                                                    }
                                                }
                                                asset.mixDependencies[b] = dep;
                                            }
                                        }

                                        task.animsToPlay[a] = asset;
                                    }
                                }

                                if (task.dependencies != null)
                                {
                                    for (int a = 0; a < task.dependencies.Length; a++)
                                    {
                                        var dep = task.dependencies[a];
                                        if (dep.type != TaskDependencyType.ParentAnimationMix && dep.type != TaskDependencyType.ParentAnimationTime) continue;

                                        if (dep.indices != null)
                                        {
                                            for (int b = 0; b < dep.indices.Length; b++)
                                            {
                                                var depIndex = dep.indices[b];
                                                if (depIndex.GetIndex() == oldIndex) depIndex.SetIndex(newIndex);
                                            }
                                        }
                                        task.dependencies[a] = dep;
                                    }
                                }
                            }
                        }

                        onAnimationIndexChange?.Invoke(oldIndex, newIndex);
                    }))
                    {
                        valid = false;
                    }
                }
                else if (animations != null) animationsTemp = (ImportedAnimation[])animations.Clone();

                if (soundsTemp != null && soundsTemp.Length > 0)
                {
                    if (!VerifyIndices(ref soundsTemp, sounds, (int oldIndex, int newIndex) =>
                    {
                        if (tasks != null)
                        {
                            foreach (var task in tasks)
                            {
                                if (task.soundsToPlay != null)
                                {
                                    for (int a = 0; a < task.animsToPlay.Length; a++)
                                    {
                                        var asset = task.animsToPlay[a];
                                        if (asset.assetIndex == oldIndex)
                                        {
                                            asset.assetIndex = newIndex;
                                            task.animsToPlay[a] = asset;
                                        }
                                    }
                                }
                            }

                            foreach (var soundEvent in soundEvents)
                            {
                                if (soundEvent.indices != null)
                                {
                                    for (int a = 0; a < soundEvent.indices.Length; a++)
                                    {
                                        var asset = soundEvent.indices[a];
                                        if (asset.index == oldIndex)
                                        {
                                            asset.index = newIndex;
                                            soundEvent.indices[a] = asset;
                                        }
                                    }
                                }
                            }
                        }

                        onSoundIndexChange?.Invoke(oldIndex, newIndex);
                    }))
                    {
                        valid = false;
                    }
                }
                else if (sounds != null) soundsTemp = (Sound[])sounds.Clone();

                if (tasksTemp != null && tasksTemp.Length > 0)
                {
                    if (!VerifyIndices(ref tasksTemp, tasks, (int oldIndex, int newIndex) =>
                    {
                        if (tasks != null)
                        {
                            foreach (var task in tasks)
                            {
                                if (task.animsToPlay != null)
                                {
                                    for (int a = 0; a < task.animsToPlay.Length; a++)
                                    {
                                        var asset = task.animsToPlay[a];
                                        if (asset.assetIndex == oldIndex)
                                        {
                                            asset.assetIndex = newIndex;
                                        }
                                        if (asset.mixDependencies != null)
                                        {
                                            for (int b = 0; b < asset.mixDependencies.Length; b++)
                                            {
                                                var dep = asset.mixDependencies[b];
                                                if (dep.type != TaskDependencyType.ParentTaskMix && dep.type != TaskDependencyType.ParentTaskTime) continue;

                                                if (dep.indices != null)
                                                {
                                                    for (int c = 0; c < dep.indices.Length; c++)
                                                    {
                                                        var depIndex = dep.indices[c];
                                                        if (depIndex.GetIndex() == oldIndex) depIndex.SetIndex(newIndex);
                                                    }
                                                }
                                                asset.mixDependencies[b] = dep;
                                            }
                                        }

                                        task.animsToPlay[a] = asset;
                                    }
                                }

                                if (task.dependencies != null)
                                {
                                    for (int a = 0; a < task.dependencies.Length; a++)
                                    {
                                        var dep = task.dependencies[a];
                                        if (dep.type != TaskDependencyType.ParentTaskMix && dep.type != TaskDependencyType.ParentTaskTime) continue;

                                        if (dep.indices != null)
                                        {
                                            for (int b = 0; b < dep.indices.Length; b++)
                                            {
                                                var depIndex = dep.indices[b];
                                                if (depIndex.GetIndex() == oldIndex) depIndex.SetIndex(newIndex);
                                            }
                                        }
                                        task.dependencies[a] = dep;
                                    }
                                }
                            }
                        }

                        onTaskIndexChange?.Invoke(oldIndex, newIndex);
                    }))
                    {
                        valid = false;
                    }
                }
                else if (tasks != null) tasksTemp = (AnimationTask[])tasks.Clone();

                return valid;
            }
#endif

            private Dictionary<string, EventReactionIndex> cachedSoundEvents;
            public void Initialize(string controllerId)
            {

                for (int a = 0; a < animators.Length; a++)
                {
                    var animator = animators[a];
                    if (characterMuscle == null) characterMuscle = animator.instance.GetComponentInChildren<MuscularRenderedCharacter>();
                    animator.Initialize(a, controllerId, animations); 
                }

                if (soundEvents != null)
                {
                    foreach(var soundEvent in soundEvents)
                    {
                        if (soundEvent.dontNormalizeChances || soundEvent.indices == null) continue;

                        float total = 0;
                        for(int a = 0; a < soundEvent.indices.Length; a++)
                        {
                            var index = soundEvent.indices[a];
                            if (index.ignoreChance) continue;

                            total += index.chance;
                        }
                        if (total > 0)
                        {
                            for (int a = 0; a < soundEvent.indices.Length; a++)
                            {
                                var index = soundEvent.indices[a];
                                if (index.ignoreChance) continue;

                                index.chance = index.chance / total;
                                soundEvent.indices[a] = index;
                            }
                        }
                    }
                }

                for (int a = 0; a < tasks.Length; a++)
                {
                    var task = tasks[a];

                    if (!task.hasLateDependencies && task.dependencies != null)
                    {
                        foreach (var dependency in task.dependencies)
                        {
                            if (task.hasLateDependencies) break;

                            if (dependency.type == TaskDependencyType.ParentTaskTime || dependency.type == TaskDependencyType.ParentTaskMix)
                            {
                                if (dependency.indices != null)
                                {
                                    foreach (var index in dependency.indices)
                                    {
                                        if (index.GetIndex() >= a)
                                        {
                                            task.hasLateDependencies = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (dependency.type == TaskDependencyType.ParentAnimationTime || dependency.type == TaskDependencyType.ParentAnimationMix)
                            {
                                if (dependency.indices != null)
                                {
                                    foreach (var index in dependency.indices)
                                    {
                                        for (int b = a + 1; b < tasks.Length; b++)
                                        {
                                            var task2 = tasks[b];
                                            if (task2.animsToPlay == null) continue;

                                            foreach (var anim in task2.animsToPlay)
                                            {
                                                if (index.GetIndex() == anim.assetIndex)
                                                {
                                                    task.hasLateDependencies = true; 
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Reset();
            }

            public void Reset()
            {
                if (cachedSoundEvents == null) cachedSoundEvents = new Dictionary<string, EventReactionIndex>();
                cachedSoundEvents.Clear();
                foreach(var soundEvent in soundEvents) cachedSoundEvents[soundEvent.eventName] = soundEvent;

                foreach (var sound in sounds) if (sound.claim != null && sound.claim.IsValid) sound.claim.Release();

                foreach (var anim in animations)
                {
                    anim.layer = animators[anim.animatorIndex].instance.FindLayer(animators[anim.animatorIndex].Controller.Prefix + anim.Name);
                    anim.layer.Mix = 0;
                    anim.layer.ActiveState.SetNormalizedTime(anim.startTimeNormalized);
                    var mc = anim.layer.GetMotionControllerUnsafe(anim.layer.ActiveState.MotionControllerIndex);
                    mc.BaseSpeed = 0;

                    if (anim.layer.GetMotionControllerUnsafe(anim.layer.ActiveState.MotionControllerIndex) is AnimationReference aref && aref.AnimationPlayer != null)
                    {
                        var animPlayer = aref.AnimationPlayer;
                        if (animPlayer != null)
                        {
                            animPlayer.UnsubscribePostEvent(ReceiveAnimationEvent);
                            animPlayer.SubscribePostEvent(ReceiveAnimationEvent);
                        }
                    }
                }

                for(int a = 0; a < tasks.Length; a++) 
                {
                    var task = tasks[a];

                    task.StopImmediate(animations, sounds);
                    if (task.autoCalculateEndTime) task.AutoCalculateEndTime(animations, sounds);
                }

                foreach (var task in tasks) if (task.playOnStart) task.StartIfNotRunning();

                OnInit?.Invoke();
            }

            protected List<int> _tempEventIndices = new List<int>();
            protected virtual void ReceiveAnimationEvent(string animationEvent, float time, object sender)
            {
                
                if (cachedSoundEvents.TryGetValue(animationEvent, out var soundEvent))
                {
                    float minTimeSinceLastPlayed = float.MaxValue;
                    if (soundEvent.indices != null && soundEvent.indices.Length > 0)
                    {
                        _tempEventIndices.Clear();
                        for (int a = 0; a < soundEvent.indices.Length; a++) _tempEventIndices.InsertRandomly(a);

                        int soundIndex = soundEvent.indices[_tempEventIndices[0]].index;   

                        int attempts = 0;
                        while (attempts < 5)
                        { 
                            attempts++;
                            bool picked = false;
                            for (int a = 0; a < _tempEventIndices.Count; a++)
                            {
                                var ind = soundEvent.indices[_tempEventIndices[a]];
                                if (!ind.ignoreChance && UnityEngine.Random.value >= ind.chance) continue;

                                picked = true;

                                float minTimeSinceLastPlayedLocal = float.MaxValue;
                                if (ind.playRequirements != null && ind.playRequirements.Length > 0)
                                {
                                    bool flag = true;
                                    foreach (var requirement in ind.playRequirements)
                                    {
                                        var depSound = sounds[requirement.index];
                                        float timeSinceLastPlayed = Time.timeSinceLevelLoad - depSound.LastPlayedTime;
                                        if (timeSinceLastPlayed <= requirement.timeSinceLastPlayed)
                                        {
                                            minTimeSinceLastPlayedLocal = Mathf.Min(minTimeSinceLastPlayedLocal, timeSinceLastPlayed);
                                            flag = false;
                                            if (!ind.fulfillAllPlayRequirements) break;

                                            continue;
                                        }

                                        flag = true;
                                        break;
                                    }

                                    if (flag) continue;
                                }

                                if (minTimeSinceLastPlayedLocal > minTimeSinceLastPlayed) continue;
                                soundIndex = ind.index;
                            }

                            if (soundEvent.indices.Length > 1)
                            {
                                if (picked)
                                {
                                    if (soundEvent.avoidRepition) 
                                    {
                                        if (soundEvent.lastIndex != soundIndex) break;
                                    }
                                    else break;
                                }
                            }
                            else break;
                        }

                        soundEvent.lastIndex = soundIndex;
                        var sound = sounds[soundIndex];

                        float volMul = 1;
                        float pitchMul = 1;
                        if (sender is IAnimationPlayer player)
                        {
                            if (soundEvent.influencedBySenderMix) volMul = player.DynamicMix;
                            if (soundEvent.influencedBySenderSpeed) pitchMul = player.InternalSpeed;  
                        }
                        else if (sender is AnimationTask task)
                        {
                            if (soundEvent.influencedBySenderMix) volMul = task.DynamicMix * task.soundVolume;
                            if (soundEvent.influencedBySenderSpeed) pitchMul = task.speed * task.soundPitch; 
                        }
                        
                        if (volMul > 0) 
                        {
                            if (soundEvent.minMix < 1) volMul = soundEvent.minMix + (1 - soundEvent.minMix) * (Mathf.Max(0, volMul - soundEvent.minMix) / (1 - soundEvent.minMix)); else volMul = soundEvent.minMix;
                            pitchMul = pitchMul * UnityEngine.Random.Range(soundEvent.randomRange.x, soundEvent.randomRange.y);
                            sound.Play(0, volMul == 0.5f ? volMul : Mathf.Pow(volMul, (volMul < 0.5f ? soundEvent.f1 : (1 / soundEvent.f1))), pitchMul);

                            if (soundEvent.syncWithSender && pitchMul > 0)
                            {
                                if (soundEvent.existingRoutineToken != null && !soundEvent.existingRoutineToken.IsCancellationRequested) soundEvent.existingRoutineToken.Cancel();

                                soundEvent.existingRoutineToken = new CancellationTokenSource();
                                var cancellationToken = soundEvent.existingRoutineToken.Token;
                                IEnumerator Sync()
                                {
                                    float t = 0;
                                    float maxTime = sound.Length / pitchMul;
                                    while(sound.IsPlaying && t < maxTime && !cancellationToken.IsCancellationRequested)
                                    {
                                        t += Time.deltaTime; 

                                        float volMul = 1;
                                        float pitchMul = 1;
                                        if (sender is IAnimationPlayer player)
                                        {
                                            if (soundEvent.influencedBySenderMix) volMul = player.DynamicMix;
                                            if (soundEvent.influencedBySenderSpeed) pitchMul = player.InternalSpeed;
                                        }
                                        else if (sender is AnimationTask task)
                                        {
                                            if (task.HasFullyStopped) break;
                                            if (soundEvent.influencedBySenderMix) volMul = task.DynamicMix * task.soundVolume;
                                            if (soundEvent.influencedBySenderSpeed) pitchMul = task.speed * task.soundPitch;
                                        }
                                        if (soundEvent.minMix < 1) volMul = soundEvent.minMix + (1 - soundEvent.minMix) * (Mathf.Max(0, volMul - soundEvent.minMix) / (1 - soundEvent.minMix)); else volMul = soundEvent.minMix;
                                        pitchMul = pitchMul * UnityEngine.Random.Range(soundEvent.randomRange.x, soundEvent.randomRange.y);
                                        volMul = volMul == 0.5f ? volMul : Mathf.Pow(volMul, (volMul < 0.5f ? soundEvent.f1 : (1 / soundEvent.f1))); 

                                        sound.Volume = sound.volume * volMul;
                                        sound.Pitch = sound.pitch * pitchMul;

                                        yield return null;
                                    }
                                }

                                CoroutineProxy.Start(Sync());
                            }
                        }
                    }
                }

                OnAnimationEvent?.Invoke(animationEvent, time, sender);
            }
            public void ApplyControllers()
            {
                if (animators == null) return;

                foreach(var animator in animators)
                {
                    if (animator == null) continue;
                    animator.ApplyController();
                }
            }
            public void RemoveControllers(bool disposeLayers = true)
            {
                if (animators == null) return;

                foreach (var animator in animators)
                {
                    if (animator == null) continue;
                    animator.RemoveController(disposeLayers);
                }
            }
            public void Dispose()
            {
                try
                {
                    foreach (var animator in animators) if (animator != null) animator.Dispose();
                }
                catch (Exception e)
                {
                    swole.LogError(e); 
                }

                ClearListeners();
            }
            public void StopAllTasks()
            {
                try
                {
                    foreach (var task in tasks) task.StopImmediate(animations, sounds);
                }
                catch (Exception e)
                {
                    swole.LogError(e);
                }
            }

            public bool disable;

            public void Update(float deltaTime)
            {

                if (disable) return;

                OnPreUpdate?.Invoke();

                if (animations != null)
                {
                    foreach (var anim in animations)
                    {
                        anim.layer.Mix = 0; 
                    }
                }
                for(int a = 0; a < tasks.Length; a++)
                {
                    var task = tasks[a];
                    if (task == null) continue;

                    try
                    {
                        if (task.HasFullyStopped)
                        {
                            if (task.CanStart(true)) task.Start(animations, sounds);
                        }
                        else if (task.IsRunning && task.CanStop(true))
                        {
                            task.Stop(animations, sounds);
                        }

                        task.Run(a, tasks, animations, sounds, deltaTime, characterMuscle); 
                    }
                    catch (Exception e)
                    {
                        swole.LogError($"Encountered exception while execeuting task '{task.name}'");
                        swole.LogError(e);
                        continue;
                    }
                }
                for (int a = 0; a < tasks.Length; a++)
                {
                    var task = tasks[a];
                    if (task == null) continue;

                    try
                    {
                        if (task.HasFullyStopped)
                        {
                            if (task.CanStart(true)) task.Start(animations, sounds); else continue;
                        }
                        else if (task.IsRunning && task.CanStop(true))
                        {
                            task.Stop(animations, sounds);
                        }
                        else if ((task.dependencies == null || task.dependencies.Length < 0 || !task.hasLateDependencies) && !task.runLate) continue;

                        task.runLate = false;
                        task.Run(a, tasks, animations, sounds, 0, characterMuscle);  
                    }
                    catch (Exception e)
                    {
                        swole.LogError($"Encountered exception while execeuting task '{task.name}'");
                        swole.LogError(e);
                        continue;
                    }
                }

                if (animations != null)
                {
                    foreach (var anim in animations)
                    {
                        anim.layer.Mix = anim.layer.Mix * anim.GetMix(animations);
                    }
                }

                OnPostUpdate?.Invoke();
            }

        }

        public AnimatedObject[] animatedObjects;

        private bool initialized;
        public void Initialize()
        {
            initialized = false;

            if (animatedObjects != null)
            {
                foreach (var obj in animatedObjects) obj.Initialize(name);
            }

            initialized = true;

#if UNITY_EDITOR
            VerifyIndices();
#endif
        }

        protected virtual void Awake()
        {
#if UNITY_EDITOR
            VerifyIndices();
            if (!Application.isPlaying) return;
#endif

            swole.Register(this);
        }

        public virtual void OnUpdate()
        {
            if (!initialized) return; 

            if (animatedObjects != null)
            {
                foreach (var obj in animatedObjects) obj.Update(Time.deltaTime); 
            }
        }
        public virtual void OnLateUpdate() { }
        public virtual void OnFixedUpdate() { }

        protected virtual void OnDestroy()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            swole.Unregister(this);

            if (animatedObjects != null)
            {
                foreach(var obj in animatedObjects)
                {
                    if (obj != null) obj.Dispose(); 
                }

                animatedObjects = null;
            }
        }

    }

}
