using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using Unity.Mathematics;

using Swole;
using Swole.API.Unity;
using Swole.UI;

using BulkOut;

public class workoutGameTest : MonoBehaviour, Swole.IExecutableBehaviour
{

    public const string lastBuildReset = "0.1.1.0";
    public const string lastBuildProgressReset = "0.0.0.0";

    public static string DemoFolderPath => Path.Combine(Application.persistentDataPath, $"demo");
    public static string DemoPackageLibraryPath => Path.Combine(DemoFolderPath, $"packages");

    [Serializable]
    public struct DemoSettings
    {
        public string version;
        public int tutorialPrompt;
        public bool invertRepAssist;
        public bool easyRepperMode;
        public bool disableUI;
        public float growthRate;
        public bool disableUISounds;

        public static DemoSettings Default => new DemoSettings() { version = "0.0.0.0", tutorialPrompt = 0, easyRepperMode = false, growthRate = 1 }; 
    }

    private DemoSettings settings;
    private bool loadedInitialSettings;
    public DemoSettings Settings
    {
        get
        {
            if (!loadedInitialSettings)
            {
                loadedInitialSettings = true;

                try
                {
                    string fullPath = Path.Combine(Application.persistentDataPath, $"demo.json");
                    if (File.Exists(fullPath))
                    {
                        byte[] data = File.ReadAllBytes(fullPath);
                        var json = Swole.DefaultJsonSerializer.StringEncoder.GetString(data);
                        settings = Swole.swole.FromJson<DemoSettings>(json);
                    }
                    else
                    {
                        Settings = DemoSettings.Default;
                    }
                }
                catch (Exception ex)
                {
                    Swole.swole.LogError(ex);
                    settings = DemoSettings.Default; 
                }
            }

            return settings;
        }

        set
        {
            settings = value;

            try
            {
                string json = Swole.swole.ToJson(settings, true);
                byte[] bytes = Swole.DefaultJsonSerializer.StringEncoder.GetBytes(json);
                string fullPath = Path.Combine(Application.persistentDataPath, $"demo.json");

                File.WriteAllBytes(fullPath, bytes);
            }
            catch (Exception ex)
            {
                Swole.swole.LogError(ex);
            }
        }
    }

    public int Priority => GameStateController.ExecutionPriority - 1;
    public int CompareTo(Swole.IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);

    public int UpdatePriority => Priority;
    public int LateUpdatePriority => Priority;
    public int FixedUpdatePriority => Priority;

    public virtual void OnPreFixedUpdate() { }
    public virtual void OnPostFixedUpdate() { }

    protected void OnDrawGizmos()
    {

        if (weightMover != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(WeightMoverPlateStartL_World, 0.04f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(WeightMoverPlateStartL_World, WeightMoverPlateEndL_World);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(WeightMoverPlateEndL_World, 0.04f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(WeightMoverPlateStartR_World, 0.04f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(WeightMoverPlateStartR_World, WeightMoverPlateEndR_World);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(WeightMoverPlateEndR_World, 0.04f);
        }

        if (plateLoaderHeightBoneL != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(PlateLoaderPlateStartL_World, 0.04f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(PlateLoaderPlateStartL_World, PlateLoaderPlateEndL_World);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(PlateLoaderPlateEndL_World, 0.04f);
        }
        if (plateLoaderHeightBoneR != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(PlateLoaderPlateStartR_World, 0.04f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(PlateLoaderPlateStartR_World, PlateLoaderPlateEndR_World);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(PlateLoaderPlateEndR_World, 0.04f);   
        }

        if (plateLoaderPositionBoneL != null && plateLoaderPositionBoneR != null)
        {
            float dist = (plateLoaderPositionBoneR.position - plateLoaderPositionBoneL.position).magnitude;
            Vector3 dir = (plateLoaderPositionBoneR.position - plateLoaderPositionBoneL.position) / dist;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(plateLoaderPositionBoneL.parent.TransformPoint(plateLoaderStockpilePosition) - dir * dist * 0.5f, plateLoaderPositionBoneL.parent.TransformPoint(plateLoaderStockpilePosition) + dir * dist * 0.5f); 
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    public workoutCataloguePrototype catalogue;
    public GameObject demoNotice;
    public void CloseDemoNotice()
    {
        demoNotice.gameObject.SetActive(false);

        var settings = Settings;
        if (settings.tutorialPrompt < 1)
        {
            settings.tutorialPrompt = 1; 
            Settings = settings;
        }
    }
    public GameObject controlsPopup;
    public void CloseControlsPopup()
    {
        controlsPopup.gameObject.SetActive(false);

        var settings = Settings;
        if (settings.tutorialPrompt < 2)
        {
            settings.tutorialPrompt = 2;
            Settings = settings; 
        }
    }

    public GameObject pauseMenu;

    public CameraProxy cameraProxy;
    private Swole.EngineInternal.ITransform cameraSwoleTransform;
    public Camera mainCamera;

    public GameObject characterRoot;
    private Transform characterRootTransform; 
    private Swole.EngineInternal.ITransform characterRootSwoleTransform;
    private IMuscularBasic characterMuscle;
    private MuscleFlexProxy flexProxy;
    private TieredPumpController characterPump;

    public ToonSkinController skinController; 

    [Serializable]
    public struct SyncPoint
    {
        public float start;
        public float end;
    }

    [Serializable]
    public class PropertyChangeTracker
    {
        public float timeOut;
        public bool flipSign;
        public bool absoluteValue;
        public bool useMinAccumulation;
        public float minAccumulation;
        public bool useMaxAccumulation;
        public float maxAccumulation;
        [NonSerialized]
        public float accumulator;
        [NonSerialized]
        public float prevValue;
        [NonSerialized]
        public float timeOfLastChange;
        [NonSerialized]
        public Swole.FloatParameterlessDelegate getValue;
        public void Track()
        {
            float time = Time.timeSinceLevelLoad;
            float val = getValue();
            float change = val - prevValue;
            if (flipSign) change = -change;
            if (absoluteValue) change = Mathf.Abs(change);
            bool reset = time - timeOfLastChange >= timeOut;             
            if (change != 0) timeOfLastChange = time;     
            if (reset)
            {
                accumulator = 0;
                change = 0;
            }
            accumulator += change;
            if (useMinAccumulation) accumulator = Math.Max(minAccumulation, accumulator);
            if (useMaxAccumulation) accumulator = Math.Min(maxAccumulation, accumulator); 
            prevValue = val;
        }
    }
    [Serializable]
    public class MuscleMassChangeRequirement
    {
        public string muscleName;
        [NonSerialized]
        public int cachedIndex;
        public bool alwaysRequired;
        public bool onlyWhenAnotherIsSatisfied;
        public PropertyChangeTracker tracker;
        public Vector2 requirementRange;
        private IMuscularBasic characterMuscle;
        public bool IsSatisfied(IMuscularBasic characterMuscle)
        {
            if (characterMuscle == null) return false;

            this.characterMuscle = characterMuscle;
            if (cachedIndex <= 0)
            {
                cachedIndex = characterMuscle.FindMuscleGroup(muscleName) + 1;
                tracker.prevValue = characterMuscle.GetMuscleGroupMassUnsafe(cachedIndex - 1);
                tracker.timeOfLastChange = Time.timeSinceLevelLoad;
                tracker.getValue = () => this.characterMuscle.GetMuscleGroupMassUnsafe(cachedIndex - 1);
            }

            tracker.Track();
            return tracker.accumulator >= requirementRange.x && tracker.accumulator <= requirementRange.y;
        }
    }
    [Serializable]
    public class BreastSizeChangeRequirement
    {
        public PropertyChangeTracker tracker;
        public Vector2 requirementRange;
        private IMuscularBasic characterMuscle;
        public bool IsSatisfied(IMuscularBasic characterMuscle)
        {
            if (characterMuscle == null) return false;

            this.characterMuscle = characterMuscle;
            if (tracker.getValue == null)
            {
                tracker.prevValue = characterMuscle.BreastPresence;
                tracker.timeOfLastChange = Time.timeSinceLevelLoad;
                tracker.getValue = () => this.characterMuscle.BreastPresence;
            }

            tracker.Track(); 
            return tracker.accumulator >= requirementRange.x && tracker.accumulator <= requirementRange.y;
        }
    }
    [Serializable]
    public class MuscleMassRequirement
    {
        public string muscleName;
        [NonSerialized]
        public int cachedIndex;
        public Vector2 massRange;
        public bool IsSatisfied(IMuscularBasic characterMuscle)
        {
            if (characterMuscle == null) return false;

            if (cachedIndex <= 0)
            {
                cachedIndex = characterMuscle.FindMuscleGroup(muscleName) + 1;
            }

            float mass = characterMuscle.GetMuscleGroupMassUnsafe(cachedIndex - 1);
            return mass >= massRange.x && mass <= massRange.y;
        }
    }
    [Serializable]
    public class BreastSizeRequirement
    {
        public Vector2 sizeRange;
        public bool IsSatisfied(IMuscularBasic characterMuscle)
        {
            if (characterMuscle == null) return false;

            float size = characterMuscle.BreastPresence;
            return size >= sizeRange.x && size <= sizeRange.y; 
        }
    }
    [Serializable]
    public struct TaskRunningRequirement
    {
        public int index;
        public bool restrictToTimeRange;
        public Vector2[] timeRanges;
        public bool restrictToMixRange;
        public Vector2 mixRange;
        public bool IsSatisfied(bool isStopped, GameStateController.AnimationTask[] tasks)
        {
            var task = tasks[index];
            if (!task.IsRunning) return false; 

            if (restrictToMixRange)
            {
                float mix = task.DynamicMix;
                if (mix < mixRange.x || mix > mixRange.y) return false;
            }
            if (isStopped && restrictToTimeRange && timeRanges != null)
            {
                float time = task.Time;
                foreach (var timeRange in timeRanges)
                {
                    if (time < timeRange.x || time > timeRange.y) continue;

                    return true;
                }

                return false;
            }

            return true; 
        }
    }
    [Serializable]
    public struct ChanceTaskRequirement
    {
        public int index;
        public bool requirePlayCount;
        public Vector2Int[] playCountRanges;
        public bool requireCooldown;
        public Vector2 cooldownRange;

        public bool IsSatisfied(ChanceAnimationTask[] chanceTasks)
        {
            var task = chanceTasks[index];

            if (requirePlayCount && playCountRanges != null && playCountRanges.Length > 0)
            {
                bool flag = false;
                foreach(var range in playCountRanges)
                {
                    if (task.playCount < range.x || task.playCount > range.y) continue;

                    flag = true; 
                    break;
                }
                if (!flag) return false;
            } 

            if (requireCooldown && task.playCount > 0)
            {
                float cd = Time.timeSinceLevelLoad - task.lastPlayed;
                if (cd < cooldownRange.x || cd > cooldownRange.y) return false; 
            }

            return true;
        }
    }
    [Serializable]
    public class ChanceAnimationTask
    {
        public string name;
        public int taskIndex;
        public int parentTaskIndex = -1;

        [NonSerialized]
        protected object externalReference;

        public bool loop;
        public bool haultActionChanges;
        public bool immuneToHaulting;
        public float chance;
        public virtual float Chance => chance;
        public float cooldown;
        public int[] additionalTasksToPutOnCooldown;
        public bool useLimit;
        public int playLimit;

        public AudioSource[] audioSourcesToStopOnPlay;
        protected virtual void StopOnPlayAudioSources()
        {
            if (audioSourcesToStopOnPlay == null) return;
            foreach(var source in audioSourcesToStopOnPlay) if (source != null) source.Stop();
        }
        public AudioSource[] audioSourcesToStopOnCancel; 
        protected virtual void StopOnCancelAudioSources()
        {
            if (audioSourcesToStopOnCancel == null) return;
            foreach (var source in audioSourcesToStopOnCancel) if (source != null) source.Stop();
        }

        public float baseSpeed = 1;
        public Vector2 randomSpeed;
        public float baseMix = 1;
        public Vector2 randomMix;

        public bool restrictToTimeRange;
        public Vector2[] timeRanges;

        public bool onlyRequireOneIndexToBeActive; 
        public TaskRunningRequirement[] requiredIndices;
        public int[] restrictingIndices;

        public bool useBreastSizeRequirement;
        public BreastSizeRequirement[] breastSizeRequirements;

        public bool useBreastSizeChangeRequirement;
        public BreastSizeChangeRequirement breastSizeChangeRequirement;

        public bool useMuscleMassRequirement;
        public bool onlySatisfyOneMassRequirement;
        public MuscleMassRequirement[] muscleMassRequirements;

        public bool useMuscleMassChangeRequirement;
        public bool onlySatisfyOneMassChangeRequirement;
        public MuscleMassChangeRequirement[] muscleMassChangeRequirements;

        public bool requireChanceTaskState;
        public bool onlySatisfyOneChanceTaskStateRequirement;
        public ChanceTaskRequirement[] chanceTaskStateRequirements;

        [NonSerialized]
        public float lastPlayed;
        [NonSerialized]
        public int playCount;

        protected virtual void Start(GameStateController.AnimationTask task)
        {
            task.StartIfNotRunning();
            StopOnPlayAudioSources();
        }
        protected virtual void Stop(GameStateController.AnimationTask task)
        {
            task.StopIfRunning();
            StopOnCancelAudioSources();
        }
        public virtual bool CanPlay()
        {
            return ((playCount <= 0 || Time.timeSinceLevelLoad - lastPlayed > cooldown) && (!useLimit || (useLimit && playCount < playLimit)));
        }
        public virtual bool Check(GameStateController.AnimationTask[] tasks, ChanceAnimationTask[] chanceTasks, IMuscularBasic characterMuscle = null)
        {
            var task = tasks[taskIndex];
            if (parentTaskIndex >= 0)
            {
                var parentTask = tasks[parentTaskIndex];
                if (parentTask.HasFullyStopped)
                {
                    Stop(task);
                    return false;
                }
            }
            bool taskIsStopped = task.HasFullyStopped;
            if (!loop && !taskIsStopped) 
            {
                bool canContinue = true;
                if (requiredIndices != null && requiredIndices.Length > 0)
                {
                    canContinue = false;
                    foreach (var requirement in requiredIndices)
                    {
                        if (!requirement.IsSatisfied(taskIsStopped, tasks))
                        {
                            if (!onlyRequireOneIndexToBeActive)
                            {
                                canContinue = false;
                                break;
                            }
                        } 
                        else
                        {
                            canContinue = true;
                            if (onlyRequireOneIndexToBeActive) break; 
                        }
                    }
                }
                if (canContinue && restrictingIndices != null)
                {
                    foreach(var ind in restrictingIndices)
                    {
                        var restrictingTask = tasks[ind];
                        if (!ReferenceEquals(task, restrictingTask) && restrictingTask.IsRunning)
                        {
                            canContinue = false;
                            break; 
                        }
                    }
                }
                if (!canContinue) Stop(task);
                return false; 
            }
            
            if ((!loop || taskIsStopped) && (!CanPlay() || UnityEngine.Random.value > Chance)) return false;

            bool playing = TryPlay(task, taskIsStopped, tasks, chanceTasks, characterMuscle);
            if (!playing && !taskIsStopped && loop) Stop(task);

            return playing; 
        }
        protected virtual bool TryPlay(GameStateController.AnimationTask task, bool taskIsStopped, GameStateController.AnimationTask[] tasks, ChanceAnimationTask[] chanceTasks, IMuscularBasic characterMuscle = null)
        {
            bool canPlay_ = true;
            if (useMuscleMassChangeRequirement && muscleMassChangeRequirements != null)
            {
                canPlay_ = false;
                bool notSatisfied = false; // used so that all requirements can update their tracking if none are satisfied
                bool requireAdditional = false;
                int i = 0;
                foreach (var requirement in muscleMassChangeRequirements)
                {
                    if (!requirement.IsSatisfied(characterMuscle))
                    {
                        if (requirement.alwaysRequired || !onlySatisfyOneMassChangeRequirement)
                        {
                            notSatisfied = true;
                            canPlay_ = false;
                        }
                        else continue;
                    }
                    if (notSatisfied) continue;

                    canPlay_ = true;
                    if (requirement.onlyWhenAnotherIsSatisfied) requireAdditional = true;
                    i++;
                }
                if (requireAdditional && i <= 1) canPlay_ = false;
            }
            if (useBreastSizeChangeRequirement && breastSizeChangeRequirement != null)
            {
                if (!breastSizeChangeRequirement.IsSatisfied(characterMuscle)) return false;
            }
            if (!canPlay_) return false; // allows breast size tracker to update

            if (useBreastSizeRequirement && breastSizeRequirements != null)
            {
                bool canPlay = false;
                foreach (var requirement in breastSizeRequirements)
                {
                    if (!requirement.IsSatisfied(characterMuscle)) continue;
                    canPlay = true;
                    break;
                }
                if (!canPlay) return false;
            }
            if (useMuscleMassRequirement && muscleMassRequirements != null)
            {
                bool canPlay = false;
                foreach (var requirement in muscleMassRequirements)
                {
                    if (!requirement.IsSatisfied(characterMuscle))
                    {
                        if (!onlySatisfyOneMassRequirement) return false; else continue;
                    }
                    canPlay = true;
                    break;
                }
                if (!canPlay) return false;
            }


            GameStateController.AnimationTask parentTask = null;
            float time = 0;
            if (parentTaskIndex >= 0)
            {
                parentTask = tasks[parentTaskIndex];
                if (parentTask.HasFullyStopped) return false;

                time = parentTask.Time;
            }

            if (requiredIndices != null && requiredIndices.Length > 0)
            {
                bool canPlay = false;
                foreach (var requirement in requiredIndices)
                {
                    if (!requirement.IsSatisfied(taskIsStopped, tasks))
                    {
                        if (!onlyRequireOneIndexToBeActive) return false; else continue;
                    }
                    canPlay = true;
                    break;
                }
                if (!canPlay) return false;
            }

            if (restrictingIndices != null)
            {
                foreach (var index in restrictingIndices)
                {
                    var task_ = tasks[index];
                    if (task_.IsOrIsGoingToRun) return false;
                }
            }

            if (restrictToTimeRange && taskIsStopped && timeRanges != null)
            {
                bool canPlay = false;
                foreach (var timeRange in timeRanges)
                {
                    if (time < timeRange.x || time > timeRange.y) continue;

                    canPlay = true;
                    break;
                }
                if (!canPlay) return false;
            }

            if (requireChanceTaskState && taskIsStopped && chanceTaskStateRequirements != null)
            {
                bool canPlay = false;
                foreach (var requirement in chanceTaskStateRequirements)
                {
                    if (!requirement.IsSatisfied(chanceTasks))
                    {
                        if (!onlySatisfyOneChanceTaskStateRequirement)
                        {
                            canPlay = false;
                            break;
                        }
                        continue;
                    }

                    canPlay = true;
                    if (onlySatisfyOneChanceTaskStateRequirement) break; 
                }
                if (!canPlay) return false;
            }

            lastPlayed = Time.timeSinceLevelLoad;
            if (additionalTasksToPutOnCooldown != null)
            {
                foreach (var additionalTask in additionalTasksToPutOnCooldown) chanceTasks[additionalTask].lastPlayed = lastPlayed;
            }
            if (!taskIsStopped) return true;

            playCount++;
            task.speed = baseSpeed + ((UnityEngine.Random.value * randomSpeed.y) + (UnityEngine.Random.value * randomSpeed.x));
            task.mix = baseMix + ((UnityEngine.Random.value * randomMix.y) + (UnityEngine.Random.value * randomMix.x));
            Start(task);

            return true;
        }
    }

    #region Customization Portion

    public GameObject customizationUIRoot;

    public float maxCustomizationBust = 1.15f;
    public float defaultCustomizationBust = 0.63f;
    public float maxCustomizationMass = 0.85f;
    public float muscleMassIncrement = 0.025f;

    public Slider bustSlider;
    public Slider bicepSlider;
    public Slider tricepSlider;
    public Slider forearmSlider;
    public Slider shoulderSlider;
    public Slider pecSlider;
    public Slider neckSlider;
    public Slider trapSlider;
    public Slider backSlider;
    public Slider latSlider;
    public Slider abSlider;
    public Slider obliqueSlider;
    public Slider hipSlider;
    public Slider upperLegSlider;
    public Slider calfSlider;
    public Slider gluteSlider;

    public void SyncSliders(bool notifyListeners = false)
    {
        if (characterMuscle == null) return;

        if (notifyListeners)
        {
            bustSlider.value = characterMuscle.BreastPresence / maxCustomizationBust;  
            
            bicepSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Biceps_Left.ToString())) / maxCustomizationMass;
            tricepSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Triceps_Left.ToString())) / maxCustomizationMass;
            forearmSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Inner_Forearm_Left.ToString())) / maxCustomizationMass;
            shoulderSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Shoulders_Left.ToString())) / maxCustomizationMass;
            pecSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Pecs_Left.ToString())) / maxCustomizationMass;
            neckSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Neck.ToString())) / maxCustomizationMass;
            trapSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Traps_Left.ToString())) / maxCustomizationMass;
            backSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Scapula_Left.ToString())) / maxCustomizationMass;
            latSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Lats_Left.ToString())) / maxCustomizationMass;
            abSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Abs_A_Left.ToString())) / maxCustomizationMass;
            obliqueSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Lower_Obliques_Left.ToString())) / maxCustomizationMass;
            hipSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.TFL_Left.ToString())) / maxCustomizationMass;
            upperLegSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Quads_Left.ToString())) / maxCustomizationMass;
            calfSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Calves_Left.ToString())) / maxCustomizationMass;
            gluteSlider.value = characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Glutes_Left.ToString())) / maxCustomizationMass;
        } 
        else
        {
            bustSlider.SetValueWithoutNotify(characterMuscle.BreastPresence / maxCustomizationBust);

            bicepSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Biceps_Left.ToString())) / maxCustomizationMass);
            tricepSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Triceps_Left.ToString())) / maxCustomizationMass);
            forearmSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Inner_Forearm_Left.ToString())) / maxCustomizationMass);
            shoulderSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Shoulders_Left.ToString())) / maxCustomizationMass);
            pecSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Pecs_Left.ToString())) / maxCustomizationMass);
            neckSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Neck.ToString())) / maxCustomizationMass);
            trapSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Traps_Left.ToString())) / maxCustomizationMass);
            backSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Scapula_Left.ToString())) / maxCustomizationMass);
            latSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Lats_Left.ToString())) / maxCustomizationMass);
            abSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Abs_A_Left.ToString())) / maxCustomizationMass);
            obliqueSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Lower_Obliques_Left.ToString())) / maxCustomizationMass);
            hipSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.TFL_Left.ToString())) / maxCustomizationMass);
            upperLegSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Quads_Left.ToString())) / maxCustomizationMass);
            calfSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Calves_Left.ToString())) / maxCustomizationMass);
            gluteSlider.SetValueWithoutNotify(characterMuscle.GetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Glutes_Left.ToString())) / maxCustomizationMass);
        }
    }
    public void AddMass() 
    {
        if (characterMuscle == null) return;

        foreach (var group in MuscleFlexProxy.MuscleGroupNames)
        {
            int groupIndex = characterMuscle.FindMuscleGroup(group);
            characterMuscle.SetMuscleGroupMassUnsafe(groupIndex, Mathf.Clamp(characterMuscle.GetMuscleGroupMassUnsafe(groupIndex) + (characterMuscle.GetMuscleGroupMassUnsafe(groupIndex) <= maxCustomizationMass ? muscleMassIncrement : 0), 0, characterMuscle.GetMuscleGroupMassUnsafe(groupIndex) <= maxCustomizationMass ? maxCustomizationMass : 999));
        }
        SyncSliders(false); 
    }
    public void SubtractMass()
    {
        if (characterMuscle == null) return;

        foreach (var group in MuscleFlexProxy.MuscleGroupNames) 
        {
            int groupIndex = characterMuscle.FindMuscleGroup(group); 
            characterMuscle.SetMuscleGroupMassUnsafe(groupIndex, Mathf.Clamp(characterMuscle.GetMuscleGroupMassUnsafe(groupIndex) - muscleMassIncrement, 0, characterMuscle.GetMuscleGroupMassUnsafe(groupIndex) <= maxCustomizationMass ? maxCustomizationMass : 999)); 
        }
        SyncSliders(false);
    }

    public void SetBustSize(float size)
    {
        if (characterMuscle == null) return;

        characterMuscle.BreastPresence = size * maxCustomizationBust; 
    }
    public void SetBicepMass(float mass) 
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Biceps_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Biceps_Right.ToString()), mass * maxCustomizationMass);
    }
    public void SetTricepMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Triceps_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Triceps_Right.ToString()), mass * maxCustomizationMass);
    }
    public void SetForearmMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Inner_Forearm_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Inner_Forearm_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Outer_Forearm_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Outer_Forearm_Right.ToString()), mass * maxCustomizationMass);
    }
    public void SetPecMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Pecs_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Pecs_Right.ToString()), mass * maxCustomizationMass);
    }
    public void SetNeckMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Neck.ToString()), mass * maxCustomizationMass);
    }
    public void SetTrapMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Traps_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Traps_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Lower_Traps_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Lower_Traps_Right.ToString()), mass * maxCustomizationMass);
    }
    public void SetShoulderMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Shoulders_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Shoulders_Right.ToString()), mass * maxCustomizationMass);
    }
    public void SetUpperBackMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Scapula_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Scapula_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.TLF_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.TLF_Right.ToString()), mass * maxCustomizationMass);
    }
    public void SetLatMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Lats_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Lats_Right.ToString()), mass * maxCustomizationMass);
    }
    public void SetObliqueMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Upper_Obliques_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Upper_Obliques_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Lower_Obliques_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Lower_Obliques_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Serratus_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Serratus_Right.ToString()), mass * maxCustomizationMass);
    }
    public void SetHipMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.TFL_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.TFL_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Pelvic.ToString()), mass * maxCustomizationMass);  
    }
    public void SetAbdominalMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Abs_A_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Abs_A_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Abs_B_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Abs_B_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Abs_C_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Abs_C_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Abs_D_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Abs_D_Right.ToString()), mass * maxCustomizationMass);
    }
    public void SetUpperLegMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Quads_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Quads_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Inner_Leg_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Inner_Leg_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Hamstrings_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Hamstrings_Right.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Outer_Leg_Left.ToString()), mass * maxCustomizationMass);
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Outer_Leg_Right.ToString()), mass * maxCustomizationMass); 
    }
    public void SetCalfMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Calves_Left.ToString()), mass * maxCustomizationMass); 
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Calves_Right.ToString()), mass * maxCustomizationMass); 
    }
    public void SetGluteMass(float mass)
    {
        if (characterMuscle == null) return;

        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Glutes_Left.ToString()), mass * maxCustomizationMass);  
        characterMuscle.SetMuscleGroupMassUnsafe(characterMuscle.FindMuscleGroup(Swole.MuscleGroupsDefault.Glutes_Right.ToString()), mass * maxCustomizationMass);
    }

    public AudioSource customizationMusic;

    [Serializable]
    public enum CustomizationPose
    {
        IdleFront, IdleBack, TurnFrontBack, TurnBackFront, FlexPoseFront, FlexPoseBack
    }

    protected CustomizationPose customizationPose;
    public CustomizationPose targetCustomizationPose;

    public string defaultCustomizationCharacterPackage = "candy.workout.demo";

    public Swole.TransformSnapshotEuler customizationCharacterTransformState;
    public Swole.TransformSnapshotEuler customizationCameraTransformState;
    public Transform customizationCameraTargetParent;

    public GameStateController.AnimatedObject customizationCharacterObject;

    public int char_idleFrontTask;

    public int char_idleBackTask;

    public int char_turnFrontBackTask;
    public int char_turnBackFrontTask;

    [Serializable]
    public struct FlexPose
    {
        public string name;

        public bool isAdditive;
        public bool waitForSyncPoint;
        public SyncPoint[] syncPoints; 

        public int char_startTask;
        public int char_holdTask;
        public int char_returnTask;

        public bool isBackFacing;
        public GameObject uiButton;
    }

    public Button stopFlexButton;
    public void StopFlexPose()
    {
        targetFlexPose = -1;
    }

    public FlexPose[] flexPoses;
    protected int targetFlexPose = -1;
    protected int currentFlexPose = -1;
    protected float flexingMix;
    public float FlexingMix => flexingMix;
    protected bool haultActionChanges;
    public void SetTargetFlexPose(int flexPoseIndex)
    {
        if (flexPoseIndex < 0 || flexPoseIndex >= flexPoses.Length)
        {
            targetFlexPose = -1;
            return;
        }

        var flexPose = flexPoses[flexPoseIndex];
        targetCustomizationPose = flexPose.isBackFacing ? CustomizationPose.FlexPoseBack : CustomizationPose.FlexPoseFront;
        targetFlexPose = flexPoseIndex;
    }

    public ChanceAnimationTask[] customizationChanceAnimationTasks;

    protected void InitializeCustomization()
    {
        var idleFront = customizationCharacterObject.tasks[char_idleFrontTask];
        var idleBack = customizationCharacterObject.tasks[char_idleBackTask];
         
        var turnFrontBack = customizationCharacterObject.tasks[char_turnFrontBackTask];
        var turnBackFront = customizationCharacterObject.tasks[char_turnBackFrontTask];

        idleFront.NullifyAnimationsWithRespectTo(turnFrontBack, customizationCharacterObject.animations);
        idleFront.NullifyAnimationsWithRespectTo(turnBackFront, customizationCharacterObject.animations);
        idleBack.NullifyAnimationsWithRespectTo(turnFrontBack, customizationCharacterObject.animations);
        idleBack.NullifyAnimationsWithRespectTo(turnBackFront, customizationCharacterObject.animations);

        turnFrontBack.OnStart += () => { customizationPose = CustomizationPose.TurnFrontBack; };
        turnBackFront.OnStart += () => { customizationPose = CustomizationPose.TurnBackFront; };
        turnFrontBack.OnFinishStartFadeIn += () => { idleFront.StopIfRunning(); };
        turnBackFront.OnFinishStartFadeIn += () => { idleBack.StopIfRunning(); };
        turnFrontBack.OnBeginEndFadeOut += () => { idleBack.StartIfNotRunning(); };
        turnBackFront.OnBeginEndFadeOut += () => { idleFront.StartIfNotRunning(); };
        turnFrontBack.OnComplete += () => { customizationPose = CustomizationPose.IdleBack; };
        turnBackFront.OnComplete += () => { customizationPose = CustomizationPose.IdleFront; };

        for(int a = 0; a < flexPoses.Length; a++)
        {
            var flexPose = flexPoses[a];

            var start = customizationCharacterObject.tasks[flexPose.char_startTask];
            var hold = customizationCharacterObject.tasks[flexPose.char_holdTask]; 
            var re = customizationCharacterObject.tasks[flexPose.char_returnTask];

            start.NullifyAnimationsWithRespectTo(customizationCharacterObject.animations, false, hold, re);
            hold.NullifyAnimationsWithRespectTo(re, customizationCharacterObject.animations); 

            start.OnStart += () => { customizationPose = flexPose.isBackFacing ? CustomizationPose.FlexPoseBack : CustomizationPose.FlexPoseFront; };
            start.OnBeginEndFadeOut += () => { hold.StartIfNotRunning(); };
            hold.OnStart += () => { customizationPose = flexPose.isBackFacing ? CustomizationPose.FlexPoseBack : CustomizationPose.FlexPoseFront; };
            re.OnFinishStartFadeIn += () => 
            {
                hold.StopIfRunning(); 
            };
            re.OnComplete += () => 
            {
                customizationPose = flexPose.isBackFacing ? CustomizationPose.IdleBack : CustomizationPose.IdleFront;
                currentFlexPose = -1;
            };

            if (!flexPose.isAdditive)
            {
                start.OnFinishStartFadeIn += () => { if (flexPose.isBackFacing) idleBack.StopIfRunning(); else idleFront.StopIfRunning(); };
                re.OnBeginEndFadeOut += () => { if (flexPose.isBackFacing) idleBack.StartIfNotRunning(); else idleFront.StartIfNotRunning(); };

                if (flexPose.isBackFacing)
                {
                    idleBack.NullifyAnimationsWithRespectTo(customizationCharacterObject.animations, false, start, re);
                } 
                else
                {
                    idleFront.NullifyAnimationsWithRespectTo(customizationCharacterObject.animations, false, start, re);  
                }
            }

            int flexPoseIndex = a;
            CustomEditorUtils.SetButtonOnClickAction(flexPose.uiButton, () => SetTargetFlexPose(flexPoseIndex)); 
        }
        CustomEditorUtils.SetButtonOnClickAction(stopFlexButton.gameObject, StopFlexPose);
        targetFlexPose = currentFlexPose = -1;

        if (characterMuscle != null) characterMuscle.BreastPresence = defaultCustomizationBust;
        SyncSliders();
    }

    protected bool isFacingBack;
    public void UpdateCustomization()
    {

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Swole.InputProxy.JumpButtonDown)
        {
            SetGameState(GameState.Workout);
            return;
        }

        stopFlexButton.interactable = (customizationPose == CustomizationPose.FlexPoseFront || customizationPose == CustomizationPose.FlexPoseBack) && targetFlexPose >= 0;
         
        GameStateController.AnimationTask targetTask;
        switch (customizationPose)
        {

            case CustomizationPose.IdleFront:

                isFacingBack = false;
                flexingMix = 0;

                if (targetCustomizationPose == CustomizationPose.IdleFront)
                {
                    targetTask = customizationCharacterObject.tasks[char_idleFrontTask];
                    targetTask.StartIfNotRunning();
                }
                if (!haultActionChanges)
                {
                    switch (targetCustomizationPose)
                    {

                        case CustomizationPose.IdleBack:
                        case CustomizationPose.TurnFrontBack:
                        case CustomizationPose.FlexPoseBack:
                            targetTask = customizationCharacterObject.tasks[char_turnFrontBackTask];
                            targetTask.StartIfNotRunning();
                            break;

                        case CustomizationPose.FlexPoseFront:
                            if (targetFlexPose >= 0)
                            {
                                customizationPose = CustomizationPose.FlexPoseFront;
                            }
                            else
                            {
                                customizationPose = CustomizationPose.IdleFront;
                            }
                            currentFlexPose = targetFlexPose;
                            break;

                    }
                }
                break;

            case CustomizationPose.IdleBack:

                isFacingBack = true;
                flexingMix = 0;

                if (targetCustomizationPose == CustomizationPose.IdleFront)
                {
                    targetTask = customizationCharacterObject.tasks[char_idleBackTask];
                    targetTask.StartIfNotRunning();
                }
                if (!haultActionChanges)
                {
                    switch (targetCustomizationPose)
                    {

                        case CustomizationPose.IdleFront:
                        case CustomizationPose.TurnBackFront:
                        case CustomizationPose.FlexPoseFront:
                            targetTask = customizationCharacterObject.tasks[char_turnBackFrontTask];
                            targetTask.StartIfNotRunning();
                            break;

                        case CustomizationPose.FlexPoseBack:
                            if (targetFlexPose >= 0)
                            {
                                customizationPose = CustomizationPose.FlexPoseBack;
                            }
                            else
                            {
                                customizationPose = CustomizationPose.IdleBack;
                            }
                            currentFlexPose = targetFlexPose;
                            break;
                    }
                }
                break;

            case CustomizationPose.TurnBackFront:
                isFacingBack = false;
                flexingMix = 0;
                break;
            case CustomizationPose.TurnFrontBack:
                isFacingBack = true;
                flexingMix = 0;
                break;

            case CustomizationPose.FlexPoseBack:
            case CustomizationPose.FlexPoseFront:

                isFacingBack = customizationPose == CustomizationPose.FlexPoseBack;

                if (currentFlexPose < 0)
                {
                    customizationPose = isFacingBack ? CustomizationPose.IdleBack : CustomizationPose.IdleFront; 
                    break;
                }

                if (!haultActionChanges)
                {
                    var flexPose = flexPoses[currentFlexPose];
                    var flexStart = customizationCharacterObject.tasks[flexPose.char_startTask];
                    var flexHold = customizationCharacterObject.tasks[flexPose.char_holdTask];
                    var flexReturn = customizationCharacterObject.tasks[flexPose.char_returnTask];
                    if (isFacingBack != flexPose.isBackFacing)
                    {
                        flexStart.StopImmediate(customizationCharacterObject.animations, customizationCharacterObject.sounds);
                        flexHold.StopImmediate(customizationCharacterObject.animations, customizationCharacterObject.sounds);
                        flexReturn.StopImmediate(customizationCharacterObject.animations, customizationCharacterObject.sounds);
                        customizationPose = isFacingBack ? CustomizationPose.IdleBack : CustomizationPose.IdleFront;
                        flexingMix = 0;
                        break;
                    }
                    flexingMix = Mathf.Max((flexStart.HasFullyStopped ? 0 : flexStart.DynamicMix), (flexHold.HasFullyStopped ? 0 : flexHold.DynamicMix), (flexReturn.HasFullyStopped ? 0 : flexReturn.DynamicMix));

                    if (targetFlexPose != currentFlexPose || (isFacingBack ? targetCustomizationPose != CustomizationPose.FlexPoseBack : targetCustomizationPose != CustomizationPose.FlexPoseFront))
                    {
                        if (flexStart.HasFullyStopped)
                        {
                            if (flexHold.IsRunning)
                            {
                                flexReturn.StartIfNotRunning();
                            }
                            else if (flexReturn.HasFullyStopped)
                            {
                                customizationPose = isFacingBack ? CustomizationPose.IdleBack : CustomizationPose.IdleFront;
                            }
                        }
                    }
                    else
                    {
                        if (flexReturn.HasFullyStopped && flexHold.HasFullyStopped && flexStart.HasFullyStopped)
                        {
                            if (flexPose.waitForSyncPoint)
                            {
                                targetTask = customizationCharacterObject.tasks[isFacingBack ? char_idleBackTask : char_idleFrontTask];
                                if (targetTask.HasFullyStopped)
                                {
                                    flexStart.StartIfNotRunning();
                                }
                                else
                                {
                                    foreach (var syncPoint in flexPose.syncPoints)
                                    {
                                        if ((targetTask.speed >= 0 && (targetTask.PreviousTime < syncPoint.start || (targetTask.PreviousTime <= 0 && syncPoint.start <= 0) || targetTask.PreviousTime > targetTask.Time) && (targetTask.Time >= syncPoint.start && targetTask.Time <= syncPoint.end)) ||
                                            (targetTask.speed < 0 && (targetTask.PreviousTime > syncPoint.end || targetTask.PreviousTime < targetTask.Time) && (targetTask.Time >= syncPoint.start && targetTask.Time <= syncPoint.end)))
                                        {
                                            flexStart.StartIfNotRunning();
                                        }
                                    }
                                }
                            }
                            else flexStart.StartIfNotRunning();
                        }
                    }
                }
                break;

        }

        haultActionChanges = false;
        if (customizationChanceAnimationTasks != null)
        {

            foreach (var chanceTask in customizationChanceAnimationTasks)
            {
                var localTask = customizationCharacterObject.tasks[chanceTask.taskIndex];
                bool hasStopped = localTask.HasFullyStopped;

                if (chanceTask.immuneToHaulting || !hasStopped || !haultActionChanges)
                {
                    chanceTask.Check(customizationCharacterObject.tasks, customizationChanceAnimationTasks, characterMuscle);
                    if (chanceTask.haultActionChanges)
                    {
                        haultActionChanges = !hasStopped;  
                    }
                }
            }
        }

        foreach(var flexPose in flexPoses)
        {
            flexPose.uiButton.gameObject.SetActive(isFacingBack == flexPose.isBackFacing);
        }
    }
    public void Customization_SpinCharacter()
    {
        if (isFacingBack)
        {
            targetCustomizationPose = CustomizationPose.IdleFront;
        } 
        else
        {
            targetCustomizationPose = CustomizationPose.IdleBack;
        }
    }

    #endregion

    #region Workout Portion

    public float autoSaveInterval = 2;
    private float autoSaveTimer;
    [Serializable]
    public struct MuscleState
    {
        public string muscleId;
        public float mass;
        public float pump;
    }
    [Serializable]
    public struct WorkoutData
    {
        public string workoutId;
        public int repCount;
    }
    [Serializable]
    public struct UserData
    {
        public int activeWorkout; 

        public int barbellStackHeight;
        public float weightMoverLoad;

        public float bustSize;

        public int totalRepCount;

        public int selectedSkin;
        public int selectedSkinPalette;

        public string[] skinClothingRipStates;

        public WorkoutData[] workoutData;
        public MuscleState[] muscleStates;
    }

    public UserData SaveState
    {
        get
        {
            var data = new UserData();

            data.activeWorkout = targetWorkout;
            data.barbellStackHeight = BarbellStackHeight;
            data.weightMoverLoad = plateWeightTarget;
            
            data.totalRepCount = totalRepCount;

            if (skinController != null)
            {
                data.selectedSkin = skinController.ActiveSkinIndex;
                data.selectedSkinPalette = skinController.ActivePaletteIndex;
                var skin = skinController.ActiveSkin;
                if (skin != null)
                {
                    if (skin.clothing != null && skinController.clothingController != null)
                    {
                        if (data.skinClothingRipStates == null || data.skinClothingRipStates.Length != skin.clothing.Length) data.skinClothingRipStates = new string[skin.clothing.Length];
                        for(int a = 0; a < skin.clothing.Length; a++)
                        {
                            var clothing = skinController.clothingController.GetClothing(skin.clothing[a]);
                            if (clothing == null) continue;

                            data.skinClothingRipStates[a] = clothing.RipState;
                        }
                    }
                }
            }

            if (workouts != null)
            {
                data.workoutData = new WorkoutData[workouts.Length];
                for(int a = 0; a < workouts.Length; a++)
                {
                    var workout = workouts[a];
                    if (workout == null || string.IsNullOrEmpty(workout.name)) continue;

                    data.workoutData[a] = new WorkoutData() { workoutId = workout.name, repCount = workout.repCount };
                }
            }

            if (characterMuscle != null)
            {
                data.bustSize = characterMuscle.BreastPresence;

                data.muscleStates = new MuscleState[characterMuscle.MuscleGroupCount];
                for(int a = 0; a < characterMuscle.MuscleGroupCount; a++)
                {
                    string nme = characterMuscle.GetMuscleGroupNameUnsafe(a);

                    var state = new MuscleState() { muscleId = characterMuscle.GetMuscleGroupNameUnsafe(a), mass = characterMuscle.GetMuscleGroupMassUnsafe(a), pump = characterPump == null ? 0f : characterPump.GetPump(a) };
                    data.muscleStates[a] = state;
                }
            }

            return data;
        }

        set
        {
            SetTargetWorkout(value.activeWorkout);
            SetBarbellStackHeight(value.barbellStackHeight, true);
            SetBenchMoverTargetWeight(value.weightMoverLoad, true);

            delayedPlateWeightTarget = plateWeightTarget;

            totalRepCount = value.totalRepCount;

            try
            {
                if (skinController != null)
                {
                    skinController.SetAndResetActiveSkin(value.selectedSkin, value.selectedSkinPalette);
                    var skin = skinController.ActiveSkin;
                    if (skin != null)
                    {
                        if (value.skinClothingRipStates != null && skin.clothing != null && skinController.clothingController != null)
                        {
                            for (int a = 0; a < Mathf.Min(skin.clothing.Length, value.skinClothingRipStates.Length); a++)
                            {
                                var clothing = skinController.clothingController.GetClothing(skin.clothing[a]);
                                if (clothing == null) continue;

                                clothing.RipState = value.skinClothingRipStates[a];
                            }
                        }
                    }
                }
            } 
            catch(Exception ex)
            {
                Swole.swole.LogError(ex);
            }

            try
            {
                if (value.workoutData != null)
                {
                    foreach (var data in value.workoutData)
                    {
                        if (!TryGetWorkout(data.workoutId, out var workout)) continue;

                        workout.repCount = data.repCount;
                    }
                }
            }
            catch (Exception ex)
            {
                Swole.swole.LogError(ex);
            }

            try
            {
                if (characterMuscle != null)
                {
                    characterMuscle.BreastPresence = value.bustSize;

                    if (value.muscleStates != null)
                    {
                        foreach (var state in value.muscleStates)
                        {
                            var muscleIndex = characterMuscle.GetMuscleGroupIndexForArray(state.muscleId); 
                            if (muscleIndex < 0)
                            {
                                Debug.LogWarning($"Tried to load muscle state for unknown muscle '{state.muscleId}'");
                                continue;
                            }

                            characterMuscle.SetMuscleGroupMassUnsafe(muscleIndex, state.mass);
                            if (characterPump != null) 
                            {
                                if (!characterPump.IsInitialized) characterPump.Initialize();
                                Debug.Log($"SET PUMP: {state.muscleId} {muscleIndex} {state.pump}");  
                                characterPump.SetPumpUnsafe(muscleIndex, state.pump);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Swole.swole.LogError(ex);
            }

            SyncSliders(false);
        }
    }

    private bool isSaving;
    protected void SaveProgress() => SaveProgress(SaveState);
    protected void SaveProgress(UserData save) => SaveProgress($"demo_save.json", save);
    protected async void SaveProgress(string localFilePath, UserData save)
    {
        if (isSaving) return;

        isSaving = true;

        try
        {
            string json = Swole.swole.ToJson(save, true);
            byte[] bytes = Swole.DefaultJsonSerializer.StringEncoder.GetBytes(json);
            string fullPath = Path.Combine(Application.persistentDataPath, localFilePath);

            await File.WriteAllBytesAsync(fullPath, bytes);
        }
        catch (Exception ex)
        {
            Swole.swole.LogError(ex);
        }

        isSaving = false;
    }
    protected void SaveProgressImmediate() => SaveProgressImmediate(SaveState);
    protected void SaveProgressImmediate(UserData save) => SaveProgressImmediate($"demo_save.json", save);
    protected void SaveProgressImmediate(string localFilePath, UserData save)
    {
        if (isSaving) return;

        isSaving = true;

        try
        {
            string json = Swole.swole.ToJson(save, true);
            byte[] bytes = Swole.DefaultJsonSerializer.StringEncoder.GetBytes(json);
            string fullPath = Path.Combine(Application.persistentDataPath, localFilePath);

            File.WriteAllBytes(fullPath, bytes);
        }
        catch (Exception ex)
        {
            Swole.swole.LogError(ex);
        }

        isSaving = false;
    }

    protected bool LoadProgress()
    {
        if (LoadProgress(out var save))
        {
            SaveState = save;
            return true;
        }

        return false;
    }
    protected bool LoadProgress(out UserData save) => LoadProgress($"demo_save.json", out save);
    protected bool LoadProgress(string localFilePath, out UserData save)
    {
        save = new UserData();
        if (isSaving) return false;

        try
        {
            string fullPath = Path.Combine(Application.persistentDataPath, localFilePath);
            if (File.Exists(fullPath))
            {
                byte[] data = File.ReadAllBytes(fullPath);
                var json = Swole.DefaultJsonSerializer.StringEncoder.GetString(data);
                save = Swole.swole.FromJson<UserData>(json);  

                return true;
            }
        }
        catch (Exception ex)
        {
            Swole.swole.LogError(ex);
        }

        return false;
    }
    public void ResetProgress()  
    {
        if (characterMuscle != null)
        {
            for(int a = 0; a < characterMuscle.MuscleGroupCount; a++)
            {
                characterMuscle.SetMuscleGroupMassUnsafe(a, 0);
                characterMuscle.SetMuscleGroupPumpUnsafe(a, 0);
                if (characterPump != null) characterPump.SetPump(a, 0);
            }
        }

        SetBarbellStackHeight(0, true);
        SetBenchMoverTargetWeight(0, true);

        SyncSliders(false); 
    }
    
    public void ResetCurrentWorkoutRepCounter()
    {
        if (targetWorkout >= 0)
        {
            var workout = workouts[targetWorkout];
            if (workout != null) 
            { 
                workout.repCount = 0;
                RefreshRepText(workout); 
            }
        }
    }
    public void ResetAllWorkoutRepCounters()
    {
        foreach (var workout in workouts) if (workout != null) workout.repCount = 0;
        RefreshRepText(); 
    }
    public void ResetTotalRepCount()
    {
        totalRepCount = 0; 
    }

    public bool IsEasyRepperMode => Settings.easyRepperMode;
    public void SetEasyRepperMode(bool active)
    {
        var settings = Settings;
        settings.easyRepperMode = active;
        Settings = settings;

        if (easyRepperToggle != null) easyRepperToggle.SetIsOnWithoutNotify(active); 
    }
    public Toggle easyRepperToggle;

    public bool ShouldInvertRepAssist => Settings.invertRepAssist;
    public void SetInvertRepAssist(bool invert)
    {
        var settings = Settings;
        settings.invertRepAssist = invert; 
        Settings = settings;

        if (invertRepAssistToggle != null) invertRepAssistToggle.SetIsOnWithoutNotify(invert);
    }
    public Toggle invertRepAssistToggle;

    public Slider growthRateSlider;
    public TMP_InputField growthRateInputField;

    public Toggle disableUIToggle;
    public void SetDisableUI(bool disabled)
    {
        var settings = Settings;
        settings.disableUI = disabled;
        Settings = settings;

        if (disableUIToggle != null) disableUIToggle.SetIsOnWithoutNotify(disabled);
        if (workoutUIRoot != null) workoutUIRoot.SetActive(!disabled);
    }
    public GameObject workoutUIRoot;

    public Toggle disableUISoundsToggle;
    public void SetDisableUISounds(bool value)
    {
        var settings = Settings;
        settings.disableUISounds = value;
        Settings = settings;

        if (disableUISoundsToggle != null) disableUISoundsToggle.SetIsOnWithoutNotify(value);
    }

    public string defaultWorkoutCharacterPackage = "candy.workout.demo";

    public Swole.TransformSnapshotEuler workoutCharacterTransformState;
    public Swole.TransformSnapshotEuler workoutCameraTransformState;
    public Transform workoutCameraTargetParent;

    public GameStateController.AnimatedObject workoutCharacterObject;

    public int hideBenchTask;
    public int hidePecDeckTask;
    public int hidePulldownBarTask;
    public int foldBarbellTask;
    public int hideBarbellTask;

    protected float hideBenchProgress;
    protected float hidePecDeckProgress;
    protected float hidePulldownBarProgress;
    protected float foldBarbellProgress;
    protected float hideBarbellProgress;

    public float benchFoldSpeed = 0.65f;
    public float hideAwaySpeed = 1f;

    [Serializable]
    public struct ChanceMultiplierMapping
    {
        public bool clamp;
        public Vector4 mapping;
    }
    [Serializable]
    public class WorkoutChanceAnimationTask : ChanceAnimationTask
    {
        public workoutGameTest Game
        {
            get => externalReference == null ? null : (externalReference as workoutGameTest);
            set => externalReference = value;
        }

        public bool onlySpecifiedWorkouts;
        public int[] validWorkouts;
        
        public bool useStaminaRequirement;
        public Vector2[] staminaRanges;

        public bool useEnergyRequirement;
        public Vector2[] energyRanges;

        public bool useStaminaEnergyMixRequirement;
        public Vector2[] staminaEnergyMixRanges;

        [Serializable]
        public enum RepCompletionRequirement
        {
            OnComplete, BeforeCompletion, AfterCompletion
        }
        public bool useRepCompletionRequirement;
        public RepCompletionRequirement repCompletionRequirement;

        public bool useRepCompletionBurnOutRequirement;
        public Vector2[] repCompletionBurnOutRanges;

        public bool useRepProgressRequirement;
        public Vector2[] repProgressRanges;

        public bool useRepProgressChangeRequirement;
        public Vector2[] repProgressChangeRanges;

        public bool useInputStrengthRequirement;
        public Vector2[] inputStrengthRanges;

        public bool useRepPowerRequirement;
        public Vector2[] repPowerRanges;

        public bool useRepDifficultyRequirement;
        public Vector2[] repDifficultyRanges;

        public bool multiplyChanceBasedOnRepDifficulty;
        public ChanceMultiplierMapping repDifficultyToChanceMultiplierMapping;

        public bool useRepAlignmentRequirement;
        public Vector2[] repAlignmentRanges;

        public bool useRepAccuracyRequirement;
        public Vector2[] repAccuracyRanges;

        public bool useRepDurationRequirement;
        public Vector2[] repDurationRanges;

        public bool useLoadRatioRequirement;
        public Vector2[] loadRatioRanges;

        public bool useMainMassRequirement;
        public Vector2[] massRanges;

        public bool multiplyChanceBasedOnMainMass;
        public ChanceMultiplierMapping mainMassToChanceMultiplierMapping;

        public bool useMainFlexRequirement;
        public Vector2[] flexRanges;
        public bool useMainPumpRequirement;
        public Vector2[] pumpRanges;

        public bool useMainMassChangeRequirement;
        public Vector2[] massChangeRanges;
        public bool useMainFlexChangeRequirement; 
        public Vector2[] flexChangeRanges;
        public bool useMainPumpChangeRequirement;
        public Vector2[] pumpChangeRanges;

        public override float Chance
        {
            get
            {
                float val = chance;

                var game = Game;
                if (game != null)
                {
                    if (multiplyChanceBasedOnRepDifficulty)
                    {
                        float remap = math.remap(repDifficultyToChanceMultiplierMapping.mapping.x, repDifficultyToChanceMultiplierMapping.mapping.y, repDifficultyToChanceMultiplierMapping.mapping.z, repDifficultyToChanceMultiplierMapping.mapping.w, game.repDifficulty);
                        if (repDifficultyToChanceMultiplierMapping.clamp) remap = Mathf.Clamp(remap, repDifficultyToChanceMultiplierMapping.mapping.z, repDifficultyToChanceMultiplierMapping.mapping.w);
                        val = val * remap;
                    }
                    if (multiplyChanceBasedOnMainMass)
                    {
                        var activeWorkout = game.TargetWorkout; 

                        var mainMuscleL = activeWorkout.targetMuscles[activeWorkout.mainMuscleL];
                        var mainMuscleR = activeWorkout.targetMuscles[activeWorkout.mainMuscleR];

                        float mainMass = (mainMuscleL.currentMass + mainMuscleR.currentMass) * 0.5f;
                        float remap = math.remap(mainMassToChanceMultiplierMapping.mapping.x, mainMassToChanceMultiplierMapping.mapping.y, mainMassToChanceMultiplierMapping.mapping.z, mainMassToChanceMultiplierMapping.mapping.w, mainMass);
                        if (mainMassToChanceMultiplierMapping.clamp) remap = Mathf.Clamp(remap, mainMassToChanceMultiplierMapping.mapping.z, mainMassToChanceMultiplierMapping.mapping.w); 
                        val = val * remap;
                    }
                }

                return val;
            }
        }

        protected override bool TryPlay(GameStateController.AnimationTask task, bool taskIsStopped, GameStateController.AnimationTask[] tasks, ChanceAnimationTask[] chanceTasks, IMuscularBasic characterMuscle = null)
        {
            var game = Game;
             
            if (useRepCompletionRequirement)
            {
                switch(repCompletionRequirement)
                {
                    case RepCompletionRequirement.BeforeCompletion:
                        if (game.repComplete) return false;
                        break;

                    case RepCompletionRequirement.OnComplete:
                        if (!game.repComplete || !game.repCompletedThisFrame) return false;
                        if (useRepCompletionBurnOutRequirement && !CheckRangeRequirement(repCompletionBurnOutRanges, game.repCompletionBurnOut)) return false;
                        break;

                    case RepCompletionRequirement.AfterCompletion:
                        if (!game.repComplete) return false; 
                        if (useRepCompletionBurnOutRequirement && !CheckRangeRequirement(repCompletionBurnOutRanges, game.repCompletionBurnOut)) return false;
                        break; 

                }
            }
            
            bool CheckRangeRequirement(Vector2[] ranges, float value)
            {
                if (ranges != null)
                {
                    foreach (var range in ranges) if (value >= range.x && value <= range.y) return true;
                }

                return false;
            }

            if (onlySpecifiedWorkouts && validWorkouts != null)
            {
                bool canPlay = false;
                foreach(var workout in validWorkouts) if (workout == game.TargetWorkoutIndex)
                    {
                        canPlay = true;
                        break;
                    }
                if (!canPlay) return false;
            }

            if (useStaminaRequirement && !CheckRangeRequirement(staminaRanges, game.stamina)) return false;
            if (useEnergyRequirement && !CheckRangeRequirement(energyRanges, game.energy)) return false;
            if (useStaminaEnergyMixRequirement && !CheckRangeRequirement(staminaEnergyMixRanges, game.energy * game.stamina)) return false;

            if (useRepProgressRequirement && !CheckRangeRequirement(repProgressRanges, game.repProgress)) return false;
            if (useRepProgressChangeRequirement && !CheckRangeRequirement(repProgressChangeRanges, game.repProgress - game.prevRepProgress)) return false;

            if (useRepDurationRequirement && !CheckRangeRequirement(repDurationRanges, game.repDuration)) return false;

            if (useRepDifficultyRequirement && !CheckRangeRequirement(repDifficultyRanges, game.repDifficulty)) return false;
            if (useLoadRatioRequirement && !CheckRangeRequirement(loadRatioRanges, game.currentLiftRatio)) return false;

            if (useInputStrengthRequirement && !CheckRangeRequirement(inputStrengthRanges, game.inputStrength)) return false;

            if (useRepPowerRequirement && !CheckRangeRequirement(repPowerRanges, game.repPower)) return false;
            if (useRepAlignmentRequirement && !CheckRangeRequirement(repAlignmentRanges, game.repAlignment)) return false;
            if (useRepAccuracyRequirement && !CheckRangeRequirement(repAccuracyRanges, game.repAccuracy)) return false; 

            var activeWorkout = game.TargetWorkout;

            var mainMuscleL = activeWorkout.targetMuscles[activeWorkout.mainMuscleL];
            var mainMuscleR = activeWorkout.targetMuscles[activeWorkout.mainMuscleR];

            float mainMass = (mainMuscleL.currentMass + mainMuscleR.currentMass) * 0.5f;
            float mainFlex = (mainMuscleL.currentFlex + mainMuscleR.currentFlex) * 0.5f;
            float mainPump = (mainMuscleL.currentPump + mainMuscleR.currentPump) * 0.5f;

            if (useMainMassRequirement && !CheckRangeRequirement(massRanges, mainMass)) return false;
            if (useMainFlexRequirement && !CheckRangeRequirement(flexRanges, mainFlex)) return false;
            if (useMainPumpRequirement && !CheckRangeRequirement(pumpRanges, mainPump)) return false;

            if (useMainMassChangeRequirement && !CheckRangeRequirement(massChangeRanges, mainMass - ((mainMuscleL.previousMass + mainMuscleR.previousMass) * 0.5f))) return false;
            if (useMainFlexChangeRequirement && !CheckRangeRequirement(flexChangeRanges, mainFlex - ((mainMuscleL.previousFlex + mainMuscleR.previousFlex) * 0.5f))) return false;
            if (useMainPumpChangeRequirement && !CheckRangeRequirement(pumpChangeRanges, mainPump - ((mainMuscleL.previousPump + mainMuscleR.previousPump) * 0.5f))) return false;

            return base.TryPlay(task, taskIsStopped, tasks, chanceTasks, characterMuscle);
        }
    }

    public WorkoutChanceAnimationTask[] workoutChanceAnimationTasks;

    public int[] breathCancellingTasks;
    public int[] breathSoundCancellingTasks;

    [Serializable]
    public enum SideOfBody
    {
        Both, Left, Right
    }
    [Serializable]
    public class WorkoutTargetMuscle
    {
        public string muscleName;
        public SideOfBody sideOfBody;

        public float liftRatio = 1;
        public bool useLiftCurve;
        public AnimationCurve liftCurve;
        public float GetLiftContribution(float repProgress) => liftRatio * (useLiftCurve ? liftCurve.Evaluate(repProgress) : 1);      

        public float growthRatio = 1;
        public bool useGrowthCurve;
        public AnimationCurve growthCurve;
        public float GetGrowthContribution(float repProgress) => growthRatio * (useGrowthCurve ? growthCurve.Evaluate(repProgress) : 1);

        public float pumpRatio = 1;
        public bool usePumpCurve;
        public AnimationCurve pumpCurve;
        public float mainMusclePumpMix;
        public float GetPumpContribution(float repProgress) => pumpRatio * (usePumpCurve ? pumpCurve.Evaluate(repProgress) : 1);  

        [NonSerialized]
        private int muscleIndex;
        public int GetMuscleIndex(IMuscularBasic characterMuscle)
        {
            if (muscleIndex > 0) return muscleIndex - 1;
            muscleIndex = characterMuscle == null ? -1 : (characterMuscle.FindMuscleGroup(muscleName) + 1); 
            return muscleIndex - 1;
        }

        [NonSerialized]
        public float growthAccumulation;

        [NonSerialized]
        public float previousMass;
        [NonSerialized]
        public float currentMass;

        [NonSerialized]
        public float previousPump;
        [NonSerialized]
        public float currentPump;

        [NonSerialized]
        public float previousFlex;
        [NonSerialized]
        public float currentFlex;

        [Serializable]
        public struct ModdableData
        {
            //public string muscleName;
            //public string sideOfBody;
            public Swole.MuscleGroupsDefault muscleGroup; 
            public SideOfBody sideOfBody;

            public float liftRatio;
             
            public float growthRatio;

            public float pumpRatio;
            public float mainMusclePumpMix;
        }

        public ModdableData GetModdableData()
        {
            var moddable = new ModdableData();

            //moddable.muscleName = muscleName;
            moddable.muscleGroup = Enum.Parse<Swole.MuscleGroupsDefault>(muscleName);
            moddable.sideOfBody = sideOfBody;// sideOfBody.ToString();
            moddable.liftRatio = liftRatio;
            moddable.growthRatio = growthRatio;
            moddable.pumpRatio = pumpRatio;
            moddable.mainMusclePumpMix = mainMusclePumpMix;

            return moddable;
        }
        public void SetModdableData(ModdableData moddable)
        {
            muscleIndex = -1;
            muscleName = moddable.muscleGroup.ToString();//moddable.muscleName;
            sideOfBody = moddable.sideOfBody;//Enum.TryParse<SideOfBody>(moddable.sideOfBody, true, out var sob) ? sob : SideOfBody.Both;
            liftRatio = moddable.liftRatio;
            growthRatio = moddable.growthRatio;
            pumpRatio = moddable.pumpRatio;
            mainMusclePumpMix = moddable.mainMusclePumpMix; 
        }
    }
    [Serializable]
    public class BodyWeightMuscle
    {
        [NonSerialized]
        public int muscleIndex;

        public Swole.MuscleGroupsDefault muscleGroup;
        public float multiplier = 1;
        public bool overrideDefaultMuscleMassToPounds;
        [Tooltip("The weight in pounds that the muscle would have at full size (when its mass value is 1)")]
        public float muscleMassToPounds;
        public float MuscleMassToPounds => (overrideDefaultMuscleMassToPounds ? muscleMassToPounds : GetDefaultMuscleMassToPounds(muscleGroup)) * multiplier;

        public float GetMuscleMassToPounds() => (overrideDefaultMuscleMassToPounds ? muscleMassToPounds : BodyWeightMuscle.GetDefaultMuscleMassToPounds(muscleGroup)) * multiplier;

        public static float GetDefaultMuscleMassToPounds(Swole.MuscleGroupsDefault muscleGroup)
        {
            switch (muscleGroup)
            {
                case Swole.MuscleGroupsDefault.Neck:
                    return 2;

                case Swole.MuscleGroupsDefault.Inner_Forearm_Left:
                case Swole.MuscleGroupsDefault.Inner_Forearm_Right:
                case Swole.MuscleGroupsDefault.Outer_Forearm_Left:
                case Swole.MuscleGroupsDefault.Outer_Forearm_Right:
                    return 2;

                case Swole.MuscleGroupsDefault.Biceps_Left:
                case Swole.MuscleGroupsDefault.Biceps_Right:
                    return 3;

                case Swole.MuscleGroupsDefault.Triceps_Left:
                case Swole.MuscleGroupsDefault.Triceps_Right:
                    return 5;

                case Swole.MuscleGroupsDefault.Pecs_Left:
                case Swole.MuscleGroupsDefault.Pecs_Right:
                    return 5;

                case Swole.MuscleGroupsDefault.Lats_Left:
                case Swole.MuscleGroupsDefault.Lats_Right:
                    return 10;

                case Swole.MuscleGroupsDefault.Abs_A_Left:
                case Swole.MuscleGroupsDefault.Abs_A_Right:
                case Swole.MuscleGroupsDefault.Abs_B_Left:
                case Swole.MuscleGroupsDefault.Abs_B_Right:
                case Swole.MuscleGroupsDefault.Abs_C_Left:
                case Swole.MuscleGroupsDefault.Abs_C_Right:
                case Swole.MuscleGroupsDefault.Abs_D_Left:
                case Swole.MuscleGroupsDefault.Abs_D_Right:
                    return 1;

                case Swole.MuscleGroupsDefault.Quads_Left:
                case Swole.MuscleGroupsDefault.Quads_Right:
                case Swole.MuscleGroupsDefault.Hamstrings_Left:
                case Swole.MuscleGroupsDefault.Hamstrings_Right:
                    return 7;
            }

            return 4;
        }
    }
    [Serializable]
    public class BodyWeightSettings
    {
        [Tooltip("Multiplies the base body weight before muscle mass weight is added.")]
        public float baseBodyWeightMultiplier = 1;

        public bool onlyTargetMusclesInArray;
        [Tooltip("If this is left empty, all muscle groups will be targeted.")]
        public BodyWeightMuscle[] weightedMuscleGroups;
        [Tooltip("The muscle groups to ignore.")]
        public Swole.MuscleGroupsDefault[] excludedMuscleGroups; 
        public bool IsMuscleGroupExcluded(Swole.MuscleGroupsDefault muscleGroup)
        {
            if (excludedMuscleGroups == null) return false;
            foreach(var excludedGroup in excludedMuscleGroups) if (muscleGroup == excludedGroup) return true;
            return false;
        }
        public bool TryGetBodyWeightMuscle(Swole.MuscleGroupsDefault muscleGroup, out BodyWeightMuscle muscleWeightData)
        {
            muscleWeightData = null;
            if (weightedMuscleGroups == null) return false;

            foreach (var data in weightedMuscleGroups) if (data != null && data.muscleGroup == muscleGroup) 
                { 
                    muscleWeightData = data;
                    return true; 
                }

            return false;
        }

        private static Swole.MuscleGroupsDefault[] allMuscleGroups;
        private static int[] allMuscles;
        public float CalculateBodyWeight(float baseBodyWeight, IMuscularBasic characterMuscle)
        {
            float bodyWeight = baseBodyWeight * baseBodyWeightMultiplier;
            if (characterMuscle == null) return baseBodyWeight;

            if (weightedMuscleGroups == null || weightedMuscleGroups.Length <= 0 || !onlyTargetMusclesInArray)
            {
                if (allMuscles == null || allMuscleGroups == null) 
                {
                    allMuscleGroups = (Swole.MuscleGroupsDefault[])Enum.GetValues(typeof(Swole.MuscleGroupsDefault));
                    allMuscles = new int[allMuscleGroups.Length];
                    for (int a = 0; a < allMuscleGroups.Length; a++) allMuscles[a] = characterMuscle.GetMuscleGroupIndexForArray(allMuscleGroups[a].ToString());
                }

                if (weightedMuscleGroups != null && weightedMuscleGroups.Length > 0) 
                {
                    for (int a = 0; a < allMuscleGroups.Length; a++)
                    {
                        var muscleGroup = allMuscleGroups[a];
                        if (IsMuscleGroupExcluded(muscleGroup)) continue;
                        TryGetBodyWeightMuscle(muscleGroup, out var muscleWeightData);
                        var muscleIndex = allMuscles[a];
                        bodyWeight += characterMuscle.GetMuscleGroupMassUnsafe(muscleIndex) * (muscleWeightData == null ? BodyWeightMuscle.GetDefaultMuscleMassToPounds(muscleGroup) : muscleWeightData.GetMuscleMassToPounds());
                    }
                }
                else
                {
                    for (int a = 0; a < allMuscleGroups.Length; a++)
                    {
                        var musleGroup = allMuscleGroups[a];
                        if (IsMuscleGroupExcluded(musleGroup)) continue;
                        var muscleIndex = allMuscles[a];
                        bodyWeight += characterMuscle.GetMuscleGroupMassUnsafe(muscleIndex) * BodyWeightMuscle.GetDefaultMuscleMassToPounds(musleGroup);
                    }
                }
            } 
            else
            {
                for (int a = 0; a < weightedMuscleGroups.Length; a++)
                {
                    var muscleGroup = weightedMuscleGroups[a];
                    if (muscleGroup == null) continue;

                    if (muscleGroup.muscleIndex <= 0) 
                    { 
                        muscleGroup.muscleIndex = characterMuscle.GetMuscleGroupIndexForArray(muscleGroup.muscleGroup.ToString()) + 1; 
                        if (muscleGroup.muscleIndex <= 0) continue;
                    }

                    bodyWeight += characterMuscle.GetMuscleGroupMassUnsafe(muscleGroup.muscleIndex - 1) * muscleGroup.GetMuscleMassToPounds();
                }
            }

            return bodyWeight;
        }
    }
    [Serializable]
    public enum WeightSource
    {
        Pillars, Bench, Bodyweight
    }
    [Serializable]
    public enum WorkoutState
    {
        Resting, StartingReps, Active, StoppingReps, StartingFlex, Flexing, StoppingFlex
    }
    [Serializable]
    public class Workout : ICloneable
    {

        public object Clone() => Duplicate();
        public Workout Duplicate()
        {
            var clone = new Workout();

            var fields = typeof(Workout).GetFields();  
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

        public string name;

        public string displayName;

        public Sprite thumbnail;

        public WeightSource weightSource;

        public bool overrideRepCurveTransformState;
        public Swole.RectTransformSnapshotEuler repCurveTransformState;

        public bool overrideWeightDisplayTransformState;
        public Swole.RectTransformSnapshotEuler weightDisplayTransformState;

        public bool overrideTotalRepCountDisplayTransformState;
        public Swole.RectTransformSnapshotEuler totalRepCountDisplayTransformState;
         
        public bool overrideWorkoutRepCountDisplayTransformState;
        public Swole.RectTransformSnapshotEuler workoutRepCountDisplayTransformState; 

        public bool overrideBurnOutDisplayTransformState;
        public Swole.RectTransformSnapshotEuler burnOutDisplayTransformState;

        public bool overrideEnergyDisplayTransformState;
        public Swole.RectTransformSnapshotEuler energyDisplayTransformState;

        public bool hideBench;
        public bool hidePecDeck;
        public bool hidePulldownBar;
        public bool foldBarbell;
        public bool hideBarbell;

        public float headTrembleMax;
        public float leftArmTrembleMax;
        public float rightArmTrembleMax;
        public float leftLegTrembleMax;
        public float rightLegTrembleMax; 

        public BezierCurveUnity repCurve;

        public bool invertRepAssist;
        public float repMomentumAcceleration = 1.5f;

        public float repAlignmentLoadMinEXP = 6;
        public float repAlignmentLoadMaxEXP = 11;
        [Tooltip("The exponent of rep alignment according to rep progress.")]
        public AnimationCurve repAlignmentDifficultyCurve;

        [Tooltip("The maximum speed that the rep knob will move along the rep curve.")]
        public float repSpeedMax = 3;
        [Tooltip("The minimum speed that the rep knob will move along the rep curve.")]
        public float repSpeedMin = 0.5f;
        [Tooltip("A multiplier of speed along the rep curve that will be applied when stamina/energy is empty.")]
        public float repSpeedBurnOut = 0.2f;
        [Tooltip("The amount of gravity applied to a rep when the weight load is at its maximum. It is a ratio of " + nameof(repSpeedMax) + ". The value is blended in, starting from zero, when the weight load is below the maximum.")]
        public float maxRepGravity = 0.95f;
        [Tooltip("The ratio of rep alignment below which gravity will start being applied to the rep.")]
        public float repAlignmentGravityStart = 0.8f;
        [Tooltip("The exponent of burn out when it is factored into gravity calculations. [pow(1 - (stamina * energy), **exponent**)]")]
        public float staminaGravityFactorExp = 2;

        [Tooltip("The max amount of stamina refunded for completing a rep. This value is multiplied by the accuracy of the rep.")]
        public float repCompletionMaxStaminaRefund = 0.035f;
        [Tooltip("The minimum accuracy required for a rep to be considered perfect. Rep accuracy is measured within the range of 0 - 1.")]
        public float perfectRepMinAccuracy = 0.92f;
        [Tooltip("A multiplier that gets applied to the stamina refund on rep completion when the rep is considered perfect.")]
        public float perfectRepStaminaRefundMultiplier = 10;
        [Tooltip("The minimum amount of stamina needed to start a rep.")]
        public float repStartPreventionStaminaThreshold = 0.065f;

        [Tooltip("The maximum amount of weight the character can lift when all of their target muscles have a mass of 1. A mass of 1 is not the maximum, so the amount of weight they can lift will go beyond this value.")]
        public int softWeightMaximum;
        [Tooltip("The maximum amount of weight the character can lift when all of their target muscles have a mass of 0.")]
        public int weightMinimum;

        [Tooltip("Should the displayed weight be halved? Useful if the workout affects two limbs separately.")]
        public bool halveWeightDisplay;

        public bool useStrengthCurve;
        public AnimationCurve strengthCurve;
        public AnimationCurve strengthLossEnergyCurve;
        public float CalculateMaximumLift(float repProgress, float energy, AnimationCurve pumpStrengthBonusCurve, IMuscularBasic characterMuscle)
        {
            float lift = 0;
            float totalContribution = 0;
            if (characterMuscle != null)
            {
                foreach (var muscle in targetMuscles)
                {
                    int muscleIndex = muscle.GetMuscleIndex(characterMuscle);
                    float contribution = muscle.GetLiftContribution(repProgress);
                    lift += contribution * characterMuscle.GetMuscleGroupMassUnsafe(muscleIndex) * pumpStrengthBonusCurve.Evaluate(characterMuscle.GetMuscleGroupPumpUnsafe(muscleIndex));
                    totalContribution += contribution;
                }
            }

            lift = totalContribution == 0 ? 0 : (lift / totalContribution);  
            return Mathf.LerpUnclamped(weightMinimum, softWeightMaximum, (useStrengthCurve ? strengthCurve.Evaluate(lift) : lift) * strengthLossEnergyCurve.Evaluate(energy)); 
        }

        public float staminaPerSecond;
        public bool useStaminaLossCurve; 
        public AnimationCurve staminaLossCurve; 
        public float CalculateStaminaLoss(float repProgress, AnimationCurve pumpStaminaBonusCurve, IMuscularBasic characterMuscle)
        {
            float bonus = 0;
            float totalContribution = 0;
            if (characterMuscle != null)
            {
                foreach (var muscle in targetMuscles)
                {
                    int muscleIndex = muscle.GetMuscleIndex(characterMuscle);
                    float contribution = muscle.GetLiftContribution(repProgress);
                    bonus += contribution * pumpStaminaBonusCurve.Evaluate(characterMuscle.GetMuscleGroupPumpUnsafe(muscleIndex));
                    totalContribution += contribution;
                }
            }

            bonus = totalContribution == 0 ? 0 : (bonus / totalContribution); 
            return staminaPerSecond * (useStaminaLossCurve ? staminaLossCurve.Evaluate(repProgress) : 1) * bonus;  
        }

        public float staminaRecoveryPerSecond;
        public float staminaRecoveryDelay;
        public float staminaRecoveryRestBonusMultiplier;

        public float energyPerSecond;
        public bool useEnergyLossCurve;
        public AnimationCurve energyLossCurve;
        public float CalculateEnergyLoss(float repProgress, AnimationCurve pumpEnergyBonusCurve, IMuscularBasic characterMuscle)
        {
            float bonus = 0;
            float totalContribution = 0;
            if (characterMuscle != null)
            {
                foreach (var muscle in targetMuscles)
                {
                    int muscleIndex = muscle.GetMuscleIndex(characterMuscle);
                    float contribution = muscle.GetLiftContribution(repProgress);
                    bonus += contribution * pumpEnergyBonusCurve.Evaluate(characterMuscle.GetMuscleGroupPumpUnsafe(muscleIndex));
                    totalContribution += contribution;
                }
            }

            bonus = totalContribution == 0 ? 0 : (bonus / totalContribution);
            return energyPerSecond * (useEnergyLossCurve ? energyLossCurve.Evaluate(repProgress) : 1) * bonus;
        }

        public float energyRecoveryPerSecond;
        public float energyRecoveryDelay;
        public float energyRecoveryRestBonusMultiplier;

        [Tooltip("The base amount of mass that is added to target muscles over time (while the muscles are under load). Will be multiplied by the target muscle's growth ratio.")]
        public float growthRate = 0.0015f;
        [Tooltip("The base amount of mass that is added to target muscles after completing a rep. Will be multiplied by the target muscle's growth ratio.")]
        public float fullRepGrowthBonus = 0.0025f;
        [Tooltip("A multiplier that is applied to muscle growth during the eccentric phase of a rep. It starts at zero at the peak of the rep and increases the closer you are to ending the rep.")]
        public float eccentricGrowthMultiplier = 5f;
        [Tooltip("A multiplier that is applied to muscle growth based on stamina and energy. The full value of this multiplier is used when stamina or energy is empty, and the value is not used at all when stamina and energy are full.")]
        public float burnOutGrowthMultiplier = 3f;
        [Tooltip("A multiplier that is applied alongside " + nameof(burnOutGrowthMultiplier) + " when a rep is completed. The full value of this multiplier is used when stamina or energy is empty, and the value is not used at all when stamina and energy are full.")]
        public float burnOutRepGrowthBonusMultiplier = 1.35f;
        [Tooltip("The exponential falloff of burn out (low stamina/energy) growth multipliers. Causes the multipliers to be reduced quicker when stamina and energy are high. Setting this value to a decimal number below 1 (e.g. 0.5, 0.3, 0.1) will cause the multipliers to be reduced slower.")]
        public float burnOutGrowthBonusFalloff = 3;

        [Tooltip("A multiplier of growth that gets added multiple times over based on the counter of completed full burn out reps. The counter will reset when burn out drops below the threshold set by " + nameof(burnOutRepCounterResetThresholdMin) + " and " + nameof(burnOutRepCounterResetThresholdMax) + ". [growth + (growth * " + nameof(burnOutRepCounterGrowthMultiplier) + " * burnOutRepCounter)]")]
        public float burnOutRepCounterGrowthMultiplier = 0.1f; 
        [Tooltip("The minimum value of a threshold that's used to reset the counter which increments each time a rep is completed at full burn out. When burn out drops below this threshold, the counter is reset.")]
        public float burnOutRepCounterResetThresholdMin = 0.75f;
        [Tooltip("The maximum value of a threshold that's used to reset the counter which increments each time a rep is completed at full burn out. When burn out drops below this threshold, the counter is reset.")]
        public float burnOutRepCounterResetThresholdMax = 0.9f; 
        [Tooltip("An exponent of rep difficulty that's increased by a counter which increments each time a rep is completed at full burn out. This makes it harder and harder to keep performing reps at full burn out, until the counter is reset.")]
        public float burnOutRepCounterDifficultyExp = 1.001f;

        [Tooltip("A multiplier of growth that gets added multiple times over based on the size of the rep combo. [growth + (growth * " + nameof(repComboGrowthMultiplier) + " * repCombo)]")]
        public float repComboGrowthMultiplier = 0.025f;  

        public AnimationCurve repHoldGrowthBonusCurve;
        public float repMaxHoldTime = 1;
        public float repHoldBonusDifficultyFalloff = 6;

        [Range(0, 1)]
        public float flexStartPoint;
        [Range(0, 1)]
        public float flexHardStartThreshold = 0.9f;
        public float flexProgressSpeed = 2;
        public float flexHardDecay = 1.6f;
        public float flexHardDecayEXP = 1.45f;
        public float flexHardInputAddRatio = 0.13f;
        public Vector2 flexRepProgressMapping = new Vector2(0, 1);
        public Vector2 flexInputAxis = new Vector2(0, 1);
        public int[] flexLocalTargetMuscles;

        public int restTask;
        public int restTiredBlendTask;
        public int restToRepTask;
        public int repToRestTask;

        public int[] restOffsetTasks;

        public int repBaseTask;
        public int repAddTask;
        public int repStruggleTask;
        public float repStruggleMix = 1;
        public int repExhaustionTask; // zero stamina

        public int[] repOffsetTasks;

        public int flexBaseTask;
        public int flexControlTask;
        public int restToFlexTask;
        public int flexToRestTask;

        public int[] flexOffsetTasks;

        public int breatheLightTask;
        public int breatheMedTask; 
        public int breatheHeavyTask;

        public int restCamBaseTask;
        public int restCamXTask;
        public int restCamYTask;

        public int repCamBaseTask;
        public int repCamXTask;
        public int repCamYTask;

        public int flexCamBaseTask;
        public int flexCamXTask;
        public int flexCamYTask;

        public float restCamSpeedX = 1;
        public float restCamSpeedY = 1;
        [Range(0, 1)]
        public float restCamXStartPos = 0.5f;
        [Range(0, 1)]
        public float restCamYStartPos = 0.5f;
        public bool restCamAllowWrapX;
        public bool restCamAllowWrapY;

        public float repCamSpeedX = 1;
        public float repCamSpeedY = 1;
        public Vector4 repCamTransitionMappingX = new Vector4(0, 1, 0, 1);
        public Vector4 repCamTransitionMappingY = new Vector4(0, 1, 0, 1);
        public bool repCamAllowWrapX;
        public bool repCamAllowWrapY;

        public float flexCamSpeedX = 1;
        public float flexCamSpeedY = 1;
        public Vector4 flexCamTransitionMappingX = new Vector4(0, 1, 0, 1);
        public Vector4 flexCamTransitionMappingY = new Vector4(0, 1, 0, 1);
        public bool flexCamAllowWrapX;
        public bool flexCamAllowWrapY;

        public int mainMuscleL;
        public int mainMuscleR;
        public WorkoutTargetMuscle[] targetMuscles; 

        public BodyWeightSettings bodyWeightSettings;

        [NonSerialized]
        public int repCount;
    }

    public Workout[] workouts;
    public int GetWorkoutIndex(string workoutName)
    {
        if (workouts == null) return -1;

        workoutName = workoutName.ToLower().Trim();
        for(int a = 0; a < workouts.Length; a++)
        {
            var workout = workouts[a];
            if (workout == null) continue;

            if (workout.name.ToLower().Trim() == workoutName || workout.displayName.ToLower().Trim() == workoutName) return a;
        }

        return -1;
    }
    public bool TryGetWorkout(string workoutName, out Workout workout)
    {
        workout = null;

        var index = GetWorkoutIndex(workoutName);
        if (index < 0) return false;

        workout = workouts[index];
        return true;
    }

    public float cursorInputDecay = 10f;

    public RectTransform repKnob;
    private Image repKnobImage;
    public UIBezierCurveRenderer[] repCurveRenderers;
    public int repCurveFillRenderer;
    public int repCurveOutlineRenderer;
    public Color lightWeightColor = Color.cyan;
    public Color mediumWeightColor = Color.yellow;
    public Color heavyWeightColor = Color.red;
    public float2 repCurveThickness = new float2(4, 10);
    public float repCurveOutlineThickness = 4;
    private List<Vector3> repCurvePoints = new List<Vector3>();

    public float barbellBarWeight = 10;
    public float barbellPlateWeight = 10;
    public float benchMoverPlatformWeight = 20;

    public Material environmentMaterialCustomization;
    public Material environmentMaterialWorkout;

    public SkinnedMeshRenderer[] environmentSkinnedRenderers;
    public MeshRenderer[] environmentMeshRenderers;

    private int targetWorkout;
    public int TargetWorkoutIndex => targetWorkout;
    public Workout TargetWorkout => targetWorkout >= 0 ? workouts[targetWorkout] : null;
    private WorkoutState workoutState;
    public WorkoutState CurrentWorkoutState => workoutState;
    private WorkoutState targetWorkoutState;

    private BodyWeightSettings bodyWeightSettings;

    [NonSerialized]
    public int totalRepCount;
    [NonSerialized]
    public float repProgress;
    [NonSerialized]
    public float prevRepProgress;
    [NonSerialized]
    private bool forceRepUpdateFlag;
    [NonSerialized]
    public float repHoldTime;
    [NonSerialized]
    public float repPower;
    [NonSerialized]
    public float prevRepPower;
    [NonSerialized]
    public float repDeltaProgress;
    [NonSerialized]
    public float prevRepDeltaProgress;
    [NonSerialized]
    public float repMomentum;
    [NonSerialized]
    public float repAlignment;
    [NonSerialized]
    public float adjustedRepAlignment;
    [NonSerialized]
    public float prevRepAlignment;
    [NonSerialized]
    public float repDifficulty;
    [NonSerialized]
    public float prevRepDifficulty;
    [NonSerialized]
    public float currentLiftRatio;
    [NonSerialized]
    private float currentFlexRatio;
    [NonSerialized]
    private float delayedRawLiftRatio;
    [NonSerialized]
    private float progressLossTime;

    [NonSerialized]
    public float flexProgress;
    [NonSerialized]
    public float flexDecay;

    [NonSerialized]
    public float repAccuracy;
    [NonSerialized]
    public float repDuration;
    [NonSerialized]
    public bool repComplete;
    [NonSerialized]
    public bool repCompletedThisFrame;
    [NonSerialized, Tooltip("The burn out value of the last completed rep. (stamina * energy)")]
    public float repCompletionBurnOut;

    [NonSerialized, Tooltip("The amount of reps completed with full burn out.")]
    public int repBurnOutCounter;

    [NonSerialized, Tooltip("The amount of reps that have been completed without losing progress.")]
    public int repCombo;
    [NonSerialized, Tooltip("A flag that determines if a rep has lost progress at some point.")]
    public bool unsteadyRepFlag;

    [NonSerialized]
    public float inputStrength;
    [NonSerialized]
    public float prevInputStrength;

    [NonSerialized]
    public float straining;

    public float growthTransferRate = 1.0f;

    [NonSerialized]
    public float stamina;
    public float staminaRecoveryDelay;
    [NonSerialized]
    public float energy;
    public float energyRecoveryDelay;
    public float repProgressThresholdForMaxStaminaLoss = 0.05f;
    public AnimationCurve repAlignmentPunishCurve;
    [Tooltip("Multiplier for pump gain based on amount of burn out")]
    public float pumpEmptyStaminaBonus = 2.5f;
    public AnimationCurve pumpGrowthBonusCurve;
    public AnimationCurve pumpStrengthBonusCurve;
    public AnimationCurve pumpStaminaBonusCurve;
    public AnimationCurve pumpEnergyBonusCurve;
    public AnimationCurve burnOutEnergyBonusCurve;
    public AnimationCurve energyBurnOutBonusCurve;

    private float camX, repCamX, flexCamX;
    private float camY, repCamY, flexCamY;

    protected bool usingGamePad;
    public void SetIsUsingGamepad(bool isUsingGamepad) => usingGamePad = isUsingGamepad;
    public bool IsUsingGamepad => usingGamePad;

    public RectTransform pressToBegin;
    public Image pressToBeginImage;
    private LTDescr pressToBeginPulse;

    public Sprite gamePadNorth;
    public Sprite gamePadSouth;
    public Sprite gamePadEast;
    public Sprite gamePadWest;

    public Sprite keyboardNorth;
    public Sprite keyboardSouth;
    public Sprite keyboardEast;
    public Sprite keyboardWest;

    public RectTransform repCurveRoot;
    protected Swole.RectTransformSnapshot repCurveRootDefaultSnapshot;

    public RectTransform weightDisplayRoot;
    protected Swole.RectTransformSnapshot weightDisplayRootDefaultSnapshot;
    [NonSerialized]
    protected TMP_Text weightText;

    public RectTransform totalRepCountDisplayRoot;
    protected Swole.RectTransformSnapshot totalRepCountDisplayRootDefaultSnapshot;
    [NonSerialized]
    protected TMP_Text totalRepText;

    public RectTransform workoutRepCountDisplayRoot;
    protected Swole.RectTransformSnapshot workoutRepCountDisplayRootDefaultSnapshot;
    [NonSerialized]
    protected TMP_Text workoutRepText;

    public RectTransform burnOutDisplayRoot;
    protected Swole.RectTransformSnapshot burnOutDisplayRootDefaultSnapshot;
    public RectTransform burnOutNeedle;
    public float burnOutNeedleMinRot;
    public float burnOutNeedleMaxRot;

    public RectTransform energyDisplayRoot;
    protected Swole.RectTransformSnapshot energyDisplayRootDefaultSnapshot;
    public RectTransform energyNeedle;
    public float energyNeedleMinRot;
    public float energyNeedleMaxRot;

    public Color repComboStartColor = Color.white;
    public Color repComboMidColor = Color.cyan;
    public Color repComboFinalColor = Color.magenta;
    public RectTransform repComboDisplayRoot;
    protected Swole.RectTransformSnapshot repComboDisplayRootDefaultSnapshot;
    public RectTransform repComboCounterRoot;
    public AudioSource repComboCounterSound;
    public RectTransform repComboBreakRoot;
    public AudioSource repComboBreakSound;

    public Color overheatStartColor = Color.yellow;
    public Color overheatMildColor = (Color.yellow + Color.red) / 2f;
    public Color overheatHotColor = Color.red;
    public RectTransform repOverheatDisplayRoot;
    protected Swole.RectTransformSnapshot repOverheatDisplayRootDefaultSnapshot;
    public RectTransform repOverheatCounterRoot;
    public AudioSource repOverheatCounterSound;
    public RectTransform repOverheatExtinguishRoot;
    public AudioSource repOverheatExtinguishSound;

    public AudioSource plateLoaderTrunkServoL;
    public AudioSource plateLoaderTrunkServoR;
    public Vector2 plateLoaderTrunkServoVolumeRange = new Vector2(0.25f, 1);
    public Vector2 plateLoaderTrunkServoPitchRange = new Vector2(0.35f, 1);

    public AudioSource weightMoverPlateSlidingSoundL;
    public AudioSource weightMoverPlateSlidingSoundR;
    public float weightMoverPlateSlidingSoundVolume = 1f;
    public Vector2 weightMoverPlateSlidingSoundPitchRange = new Vector2(0.8f, 1);

    public AudioSource weightMoverPlateClunkSoundL;
    public AudioSource weightMoverPlateClunkSoundR;
    public Vector2 weightMoverPlateClunkSoundVolumeRange = new Vector2(0.8f, 1);
    public Vector2 weightMoverPlateClunkSoundPitchRange = new Vector2(0.8f, 1); 

    public AudioSource weightPillarPlateSlidingSoundL;
    public AudioSource weightPillarPlateSlidingSoundR;
    public float weightPillarPlateSlidingSoundVolume = 1f;
    public Vector2 weightPillarPlateSlidingSoundPitchRange = new Vector2(0.8f, 1);

    public AudioSource weightPillarPlateClunkSoundL;
    public AudioSource weightPillarPlateClunkSoundR;
    public Vector2 weightPillarPlateClunkSoundVolumeRange = new Vector2(0.8f, 1);
    public Vector2 weightPillarPlateClunkSoundPitchRange = new Vector2(0.8f, 1);

    public Transform plateLoaderDoorL;
    public Transform plateLoaderPositionBoneL;
    public Transform plateLoaderHeightBoneL;
    public Transform plateLoaderBarExtensionBoneL;
    public Transform plateLoaderBarConnectBoneL;
    public Vector3 plateLoaderPlateStartL;
    public Vector3 PlateLoaderPlateStartL_World => plateLoaderHeightBoneL.TransformPoint(plateLoaderPlateStartL);
    public Vector3 plateLoaderPlateEndL;
    public Vector3 PlateLoaderPlateEndL_World => plateLoaderHeightBoneL.TransformPoint(plateLoaderPlateEndL);

    public Transform plateLoaderDoorR;
    public Transform plateLoaderPositionBoneR;
    public Transform plateLoaderHeightBoneR;
    public Transform plateLoaderBarExtensionBoneR;
    public Transform plateLoaderBarConnectBoneR;
    public Vector3 plateLoaderPlateStartR;
    public Vector3 PlateLoaderPlateStartR_World => plateLoaderHeightBoneR.TransformPoint(plateLoaderPlateStartR);
    public Vector3 plateLoaderPlateEndR;
    public Vector3 PlateLoaderPlateEndR_World => plateLoaderHeightBoneR.TransformPoint(plateLoaderPlateEndR);

    public Transform weightMover;

    public Vector3 weightMoverPlateStartL;
    public Vector3 WeightMoverPlateStartL_World => weightMover.TransformPoint(weightMoverPlateStartL);
    public Vector3 weightMoverPlateEndL;
    public Vector3 WeightMoverPlateEndL_World => weightMover.TransformPoint(weightMoverPlateEndL);
    public Transform weightMoverPlateLockL;

    public Vector3 weightMoverPlateStartR;
    public Vector3 WeightMoverPlateStartR_World => weightMover.TransformPoint(weightMoverPlateStartR);
    public Vector3 weightMoverPlateEndR;
    public Vector3 WeightMoverPlateEndR_World => weightMover.TransformPoint(weightMoverPlateEndR); 
    public Transform weightMoverPlateLockR;

    public Transform barbellL;
    public Transform barbellR;

    public GameObject barbellWeightPrefab;
    public Vector3 barbellWeightOffset;
    public Vector3 barbellWeightRotation;
    public float barbellWeightSpacing = 0.045f;
    public float barbellWeightStartHeight = 10;
    public float barbellWeightSlotInTime = 3;
    private PrefabPool barbellWeightPool;

    private List<Transform> barbellWeightListL = new List<Transform>();
    private List<Transform> barbellWeightListR = new List<Transform>();
    private List<Transform> barbellAddWeightListL = new List<Transform>();
    private List<Transform> barbellAddWeightListR = new List<Transform>();
    private List<Transform> barbellRemovedWeightListL = new List<Transform>();
    private List<Transform> barbellRemovedWeightListR = new List<Transform>();
    public int BarbellTargetStackHeight => Mathf.Max(barbellWeightListL.Count + barbellAddWeightListL.Count, barbellWeightListR.Count + barbellAddWeightListR.Count);
    public int BarbellStackHeight => Mathf.Max(barbellWeightListL.Count, barbellWeightListR.Count);
    public float BarbellStackWeight => (barbellWeightListL.Count + barbellWeightListR.Count) * barbellPlateWeight;
    protected void SetBarbellStackCount(Transform root, List<Transform> addList, List<Transform> stackList, List<Transform> removeList, int count, AudioSource thudSource, bool instant)
    {
        count = Mathf.Clamp(count, 0, 500);
        while((stackList.Count + addList.Count) > count)
        {
            Transform removed;
            int index;
            if (addList.Count > 0)
            {
                index = stackList.Count + addList.Count - 1;
                removed = addList[addList.Count - 1];
                addList.RemoveAt(addList.Count - 1);
            }
            else
            {
                index = stackList.Count - 1;
                removed = stackList[stackList.Count - 1];
                stackList.RemoveAt(stackList.Count - 1);
            }

            removeList.Add(removed);

            var targetPos = barbellWeightOffset + new Vector3(0, index * barbellWeightSpacing, 0); 

            IEnumerator SlideUp()
            {
                float t = instant ? barbellWeightSlotInTime : (Mathf.Clamp01((removed.localPosition - targetPos).magnitude / barbellWeightStartHeight) * barbellWeightSlotInTime);
                bool cancel = false;
                while (t < barbellWeightSlotInTime)
                {
                    if (!removeList.Contains(removed))
                    {
                        cancel = true;
                        break;
                    }

                    t += Time.deltaTime;
                    removed.localPosition = targetPos + new Vector3(0, barbellWeightStartHeight * Mathf.Clamp01(t / barbellWeightSlotInTime), 0); //Mathf.SmoothStep(0, 1, t / barbellWeightSlotInTime), 0);  
                    yield return null;
                }

                if (!cancel)
                {
                    removed.localPosition = targetPos + new Vector3(0, barbellWeightStartHeight, 0);
                    removeList.Remove(removed);
                    removed.SetParent(null, true);
                    barbellWeightPool.Release(removed.gameObject);
                    removed.gameObject.SetActive(false);
                }
            }

            StartCoroutine(SlideUp());
        }
        while ((stackList.Count + addList.Count) < count)
        {
            int index = stackList.Count + addList.Count;
            var targetPos = barbellWeightOffset + new Vector3(0, index * barbellWeightSpacing, 0);
            Transform added;
            if (removeList.Count > 0)
            {
                added = removeList[removeList.Count - 1];
                removeList.RemoveAt(removeList.Count - 1);
            }
            else
            {
                if (!barbellWeightPool.TryGetNewInstance(out GameObject inst)) break;
                inst.SetActive(true);
                added = inst.transform;
                added.localPosition = targetPos + new Vector3(0, barbellWeightStartHeight, 0); 
            }

            addList.Add(added);

            added.SetParent(root, true);
            added.localRotation = Quaternion.Euler(barbellWeightRotation);  

            IEnumerator SlideDown()
            {
                float t = instant ? barbellWeightSlotInTime : ((1 - Mathf.Clamp01((added.localPosition - targetPos).magnitude / barbellWeightStartHeight)) * barbellWeightSlotInTime); 
                bool cancel = false;
                while (t < barbellWeightSlotInTime)
                {
                    if (!addList.Contains(added)) // cancel slide because plate will get removed
                    {
                        cancel = true;
                        break; 
                    }

                    t += Time.deltaTime;
                    added.localPosition = targetPos + new Vector3(0, barbellWeightStartHeight * Mathf.Clamp01(1 - (t / barbellWeightSlotInTime)), 0); //Mathf.SmoothStep(1, 0, t / barbellWeightSlotInTime), 0);
                    yield return null;
                }

                if (!cancel)
                {
                    if (thudSource != null && !instant)
                    {
                        thudSource.transform.position = added.position;
                        thudSource.loop = false;
                        thudSource.PlayOneShot(thudSource.clip, UnityEngine.Random.Range(weightPillarPlateClunkSoundVolumeRange.x, weightPillarPlateClunkSoundVolumeRange.y));
                        //thudSource.Play();
                        //thudSource.volume = UnityEngine.Random.Range(weightPillarPlateClunkSoundVolumeRange.x, weightPillarPlateClunkSoundVolumeRange.y);
                        //thudSource.pitch = UnityEngine.Random.Range(weightPillarPlateClunkSoundPitchRange.x, weightPillarPlateClunkSoundPitchRange.y); 
                    }

                    added.localPosition = targetPos;
                    addList.Remove(added);
                    if (index >= stackList.Count) stackList.Add(added); else stackList.Insert(index, added); 
                }
            }

            StartCoroutine(SlideDown()); 
        }
    }
    public void SetBarbellStackHeight(int count, bool instant)
    {
        SetBarbellStackCount(barbellL, barbellAddWeightListL, barbellWeightListL, barbellRemovedWeightListL, count, weightPillarPlateClunkSoundL, instant);
        SetBarbellStackCount(barbellR, barbellAddWeightListR, barbellWeightListR, barbellRemovedWeightListR, count, weightPillarPlateClunkSoundR, instant);
    }

    public float plateLoaderDoorRetractionDepth = 1.28f;
    protected float plateLoaderDoorRetraction;
    public float plateLoaderDoorOpenCloseTime = 0.8f;

    public Vector3 plateLoaderDoorRetractionAxis = Vector3.back;

    protected Vector3 plateLoaderDoorStartPositionL;
    protected Vector3 plateLoaderDoorStartPositionR;

    [Serializable]
    protected enum PlateLoaderDoorState
    {
        Open, Closed, Opening, Closing
    }

    protected PlateLoaderDoorState plateLoaderDoorState;
    protected PlateLoaderDoorState plateLoaderDoorTargetState;

    [Serializable]
    protected enum PlateLoaderState
    {
        Idle, UnloadingToPool, LoadingFromPool, TransportingToPool, TransportingToMover, UnloadingMover, LoadingMover, PreparingToUnloadMover
    }

    protected PlateLoaderState plateLoaderState;

    protected Vector3 plateLoaderStartPositionL;
    protected Vector3 plateLoaderStartPositionR;
    protected Vector3 plateLoaderStartPositionL_Root;
    protected Vector3 plateLoaderStartPositionR_Root;

    protected Vector3 plateLoaderHeightAxisL;
    protected Vector3 plateLoaderHeightAxisR;
    protected float plateLoaderDefaultHeightL;
    protected float plateLoaderDefaultHeightR;

    protected Vector3 plateLoaderBarStartPositionL;
    protected Vector3 plateLoaderBarStartPositionR;
    protected Vector3 plateLoaderBarStartPositionL_Root;
    protected Vector3 plateLoaderBarStartPositionR_Root;
    protected float plateLoaderArmLengthL;
    protected float plateLoaderArmLengthR;

    protected Vector3 plateLoaderConnectorStartPositionL;
    protected Vector3 plateLoaderConnectorStartPositionR;
    public Vector3 plateLoaderConnectorRetractionAxis = Vector3.down;
    public float plateLoaderConnectorRetractionLengthL = 0.55f;
    public float plateLoaderConnectorRetractionLengthR = 0.55f;
    protected float plateLoaderConnectorLengthL;
    protected float plateLoaderConnectorLengthR;
    public float plateLoaderConnectionGap = 0.09f;

    public Vector3 plateLoaderStockpilePosition;
    public Vector3 PlateLoaderStockpilePosition_World => plateLoaderPositionBoneL.parent.TransformPoint(plateLoaderStockpilePosition);

    public float plateLoaderSyncTime = 1f;
    public float plateLoaderTransferTime = 0.35f;
    public float plateLoaderConnectionTime = 0.5f;
    protected float plateLoaderWeightMoverSyncVal;
    protected float plateLoaderConnectionProgress;

    public float plateThickness = 0.039f;
    public int maxPlatesPerSide;

    [Serializable]
    public enum PlateWeight
    {
        Plate5, Plate10, Plate25, Plate35, Plate45, Max = Plate45
    }
    public static float GetWeightFloat(PlateWeight plateWeight)
    {
        switch (plateWeight)
        {
            case PlateWeight.Plate5:
                return 5;

            case PlateWeight.Plate10:
                return 10;

            case PlateWeight.Plate25:
                return 25;

            case PlateWeight.Plate35:
                return 35;

            case PlateWeight.Plate45:
                return 45;
        }

        return 0;
    }
    public static float GetNearestMinimumWeight(int plateCountPerSide, float targetWeight)
    {
        float w0 = GetWeightFloat(PlateWeight.Plate5) * 2;
        float w1 = GetWeightFloat(PlateWeight.Plate10) * 2;
        float w2 = GetWeightFloat(PlateWeight.Plate25) * 2;
        float w3 = GetWeightFloat(PlateWeight.Plate35) * 2;
        float w4 = GetWeightFloat(PlateWeight.Plate45) * 2;

        float remaningWeight = targetWeight;
        int plates = plateCountPerSide;

        int p4 = Mathf.Min(plates, Mathf.FloorToInt(remaningWeight / w4));
        remaningWeight -= p4 * w4;
        plates -= p4;

        int p3 = Mathf.Min(plates, Mathf.FloorToInt(remaningWeight / w3));
        remaningWeight -= p3 * w3;
        plates -= p3;

        int p2 = Mathf.Min(plates, Mathf.FloorToInt(remaningWeight / w2));
        remaningWeight -= p2 * w2;
        plates -= p2;

        int p1 = Mathf.Min(plates, Mathf.FloorToInt(remaningWeight / w1));
        remaningWeight -= p1 * w1;
        plates -= p1;

        int p0 = Mathf.Min(plates, Mathf.FloorToInt(remaningWeight / w0));
        remaningWeight -= p0 * w0;
        //plates -= p0;

        return targetWeight - remaningWeight;
    }
    protected PrefabPool poolPlate5; 
    public GameObject plate5Prefab;
    protected PrefabPool poolPlate10;
    public GameObject plate10Prefab;
    protected PrefabPool poolPlate25;
    public GameObject plate25Prefab;
    protected PrefabPool poolPlate35;
    public GameObject plate35Prefab;
    protected PrefabPool poolPlate45;
    public GameObject plate45Prefab;

    public class LoadedPlate
    {
        public float position;
        public PlateWeight weight;
        public Transform transform;
        public float Weight => GetWeightFloat(weight);

        public int cachedIndex;

        public void UpdateWorldPosition(int startIndex, int newIndex, Vector3 startPosWorld, Vector3 targetPosWorld, float spacing)
        {
            Vector3 offset = (targetPosWorld - startPosWorld).normalized * spacing;
            transform.position = Vector3.Lerp(startPosWorld + offset * startIndex, targetPosWorld - offset * newIndex, position);
        }

        public void ClaimNewInstance(PrefabPool poolPlate5, PrefabPool poolPlate10, PrefabPool poolPlate25, PrefabPool poolPlate35, PrefabPool poolPlate45)
        {
            ReleaseInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);

            GameObject inst;
            switch (weight)
            {
                case PlateWeight.Plate5:
                    if (poolPlate5.TryGetNewInstance(out inst)) transform = inst.transform;
                    break;
                case PlateWeight.Plate10:
                    if (poolPlate10.TryGetNewInstance(out inst)) transform = inst.transform; 
                    break;
                case PlateWeight.Plate25:
                    if (poolPlate25.TryGetNewInstance(out inst)) transform = inst.transform;
                    break;
                case PlateWeight.Plate35:
                    if (poolPlate35.TryGetNewInstance(out inst)) transform = inst.transform;
                    break;
                case PlateWeight.Plate45:
                    if (poolPlate45.TryGetNewInstance(out inst)) transform = inst.transform;
                    break;
            }

            if (transform != null) transform.gameObject.SetActive(true);
        }
        public void ReleaseInstance(PrefabPool poolPlate5, PrefabPool poolPlate10, PrefabPool poolPlate25, PrefabPool poolPlate35, PrefabPool poolPlate45)
        {
            if (transform == null) return;

            switch(weight)
            {
                case PlateWeight.Plate5:
                    poolPlate5.Release(transform.gameObject);
                    break;
                case PlateWeight.Plate10:
                    poolPlate10.Release(transform.gameObject);
                    break;
                case PlateWeight.Plate25:
                    poolPlate25.Release(transform.gameObject);
                    break;
                case PlateWeight.Plate35:
                    poolPlate35.Release(transform.gameObject);
                    break;
                case PlateWeight.Plate45:
                    poolPlate45.Release(transform.gameObject);
                    break;
            }

            if (transform.parent != null) transform.SetParent(null, true);
            transform.gameObject.SetActive(false);
        }
    }

    private List<LoadedPlate> loadedPlatesL = new List<LoadedPlate>();
    private List<LoadedPlate> loadedPlatesR = new List<LoadedPlate>();

    private List<LoadedPlate> transitPlatesL = new List<LoadedPlate>();
    private List<LoadedPlate> transitPlatesR = new List<LoadedPlate>();

    public float BenchMoverLoadedWeight
    {
        get
        {
            float total = 0;

            //foreach (var plate in loadedPlatesL) total += plate.Weight;
            //foreach (var plate in loadedPlatesR) total += plate.Weight;

            // Use parenting instead for realistic representation 
            foreach (var plate in loadedPlatesL) if (plate.transform.parent == weightMover) total += plate.Weight;
            foreach (var plate in loadedPlatesR) if (plate.transform.parent == weightMover) total += plate.Weight; 

            foreach (var plate in transitPlatesL) if (plate.transform.parent == weightMover) total += plate.Weight;
            foreach (var plate in transitPlatesR) if (plate.transform.parent == weightMover) total += plate.Weight;

            return total;
        }
    }
    public bool BenchMoverIsEmpty => loadedPlatesL.Count <= 0 && loadedPlatesR.Count <= 0;

    public float BenchMoverTransitWeight
    {
        get
        {
            float total = 0;
            foreach (var plate in transitPlatesL) total += plate.Weight;
            foreach (var plate in transitPlatesR) total += plate.Weight;
            return total;
        }
    }
    public bool PlateLoadersAreEmpty => transitPlatesL.Count <= 0 && transitPlatesR.Count <= 0;

    private float plateWeightTarget;
    protected List<PlateWeight> targetPlatesL = new List<PlateWeight>();
    protected List<PlateWeight> targetPlatesR = new List<PlateWeight>();
    private bool TargetPlatesMatchLoadedPlates
    {
        get
        {
            if (targetPlatesL.Count != loadedPlatesL.Count) return false;
            if (targetPlatesR.Count != loadedPlatesR.Count) return false;

            for (int a = 0; a < loadedPlatesL.Count; a++) if (targetPlatesL[a] != loadedPlatesL[a].weight) return false;
            for (int a = 0; a < loadedPlatesR.Count; a++) if (targetPlatesR[a] != loadedPlatesR[a].weight) return false; 

            return true;
        }
    }
    private bool TargetPlatesPartiallyMatchLoadedPlates
    {
        get
        {
            if (targetPlatesL.Count <= loadedPlatesL.Count) return false;
            if (targetPlatesR.Count <= loadedPlatesR.Count) return false;

            for (int a = 0; a < loadedPlatesL.Count; a++) if (targetPlatesL[a] != loadedPlatesL[a].weight) return false;
            for (int a = 0; a < loadedPlatesR.Count; a++) if (targetPlatesR[a] != loadedPlatesR[a].weight) return false;

            return true;
        }
    }
    private bool TargetPlatesMatchTransitPlates
    {
        get
        {
            if (transitPlatesL.Count <= 0 || targetPlatesL.Count != (loadedPlatesL.Count + transitPlatesL.Count)) return false;
            if (transitPlatesR.Count <= 0 || targetPlatesR.Count != (loadedPlatesR.Count + transitPlatesR.Count)) return false; 

            for (int a = 0; a < loadedPlatesL.Count; a++) if (targetPlatesL[a] != loadedPlatesL[a].weight) return false;
            for (int a = 0; a < loadedPlatesR.Count; a++) if (targetPlatesR[a] != loadedPlatesR[a].weight) return false;
            for (int a = 0; a < transitPlatesL.Count; a++) if (targetPlatesL[a + loadedPlatesL.Count] != transitPlatesL[transitPlatesL.Count - 1 - a].weight) return false;
            for (int a = 0; a < transitPlatesR.Count; a++) if (targetPlatesR[a + loadedPlatesR.Count] != transitPlatesR[transitPlatesR.Count - 1 - a].weight) return false;  

            return true;
        }
    }
    private bool TargetPlatesPartiallyMatchTransitPlates
    {
        get
        {
            if (transitPlatesL.Count <= 0 || targetPlatesL.Count <= (loadedPlatesL.Count + transitPlatesL.Count)) return false;
            if (transitPlatesR.Count <= 0 || targetPlatesR.Count <= (loadedPlatesR.Count + transitPlatesR.Count)) return false;

            for (int a = 0; a < loadedPlatesL.Count; a++) if (targetPlatesL[a] != loadedPlatesL[a].weight) return false;
            for (int a = 0; a < loadedPlatesR.Count; a++) if (targetPlatesR[a] != loadedPlatesR[a].weight) return false;
            for (int a = 0; a < transitPlatesL.Count; a++) if (targetPlatesL[a + loadedPlatesL.Count] != transitPlatesL[transitPlatesL.Count - 1 - a].weight) return false;
            for (int a = 0; a < transitPlatesR.Count; a++) if (targetPlatesR[a + loadedPlatesR.Count] != transitPlatesR[transitPlatesR.Count - 1 - a].weight) return false;

            return true;
        }
    }
    public float BenchMoverCurrentTargetWeight
    {
        get
        {
            float total = 0;

            foreach(var plate in targetPlatesL) total += GetWeightFloat(plate);
            foreach (var plate in targetPlatesR) total += GetWeightFloat(plate);

            return total;
        }
    }
    public float GetBenchMoverTargetWeight(int platesPerSide)
    {
        float total = 0;

        for(int a = 0; a < Mathf.Min(platesPerSide, targetPlatesL.Count); a++) total += GetWeightFloat(targetPlatesL[a]);
        for (int a = 0; a < Mathf.Min(platesPerSide, targetPlatesR.Count); a++) total += GetWeightFloat(targetPlatesR[a]);

        return total;
    }

    public float SetBenchMoverTargetWeight(float weight, bool instant)
    {
        float maxWeight = GetWeightFloat(PlateWeight.Plate45) * maxPlatesPerSide * 2;
        weight = Mathf.Clamp(weight, 0, maxWeight);

        plateWeightTarget = GetNearestMinimumWeight(maxPlatesPerSide, weight);

        if (instant)
        {
            var root = plateLoaderPositionBoneL.parent;
            var stockpilePos = root.TransformPoint(plateLoaderStockpilePosition);

            plateLoaderState = PlateLoaderState.Idle;

            foreach (var loadedPlate in loadedPlatesL)
            {
                loadedPlate.transform.position = stockpilePos;
                loadedPlate?.ReleaseInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);
            }
            foreach (var loadedPlate in loadedPlatesR)
            {
                loadedPlate.transform.position = stockpilePos;
                loadedPlate?.ReleaseInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);
            }
            loadedPlatesL.Clear();
            loadedPlatesR.Clear();

            foreach (var loadedPlate in transitPlatesL)
            {
                loadedPlate.transform.position = stockpilePos;
                loadedPlate?.ReleaseInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);
            }
            foreach (var loadedPlate in transitPlatesL)
            {
                loadedPlate.transform.position = stockpilePos;
                loadedPlate?.ReleaseInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);
            }
            transitPlatesL.Clear();
            transitPlatesR.Clear();

            targetPlatesL.Clear();
            targetPlatesR.Clear();
        }

        RefreshBenchMoverTargetPlates();

        if (instant)
        {   
            var weightMoverPlateStartL_World = WeightMoverPlateStartL_World;
            var weightMoverPlateStartR_World = WeightMoverPlateStartR_World;
            var weightMoverPlateEndL_World = WeightMoverPlateEndL_World;
            var weightMoverPlateEndR_World = WeightMoverPlateEndR_World;

            var dirL = (weightMoverPlateEndL_World - weightMoverPlateStartL_World).normalized;
            var dirR = (weightMoverPlateEndR_World - weightMoverPlateStartR_World).normalized; 

            for (int a = 0; a < targetPlatesL.Count; a++)
            { 
                var newPlate = new LoadedPlate() { weight = targetPlatesL[a], position = 0 };
                newPlate.ClaimNewInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);
                newPlate.transform.SetParent(weightMover, false);
                newPlate.transform.position = weightMoverPlateStartL_World + dirL * plateThickness * a;
                loadedPlatesL.Add(newPlate);
            }
            for (int a = 0; a < targetPlatesR.Count; a++)
            {
                var newPlate = new LoadedPlate() { weight = targetPlatesR[a], position = 0 };
                newPlate.ClaimNewInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);
                newPlate.transform.SetParent(weightMover, false);
                newPlate.transform.position = weightMoverPlateStartR_World + dirR * plateThickness * a;
                loadedPlatesR.Add(newPlate); 
            }
        }

        return plateWeightTarget;
    }

    public void RefreshBenchMoverTargetPlates()
    {
        while (targetPlatesL.Count > targetPlatesR.Count) targetPlatesL.RemoveAt(targetPlatesL.Count - 1);
        while (targetPlatesR.Count > targetPlatesL.Count) targetPlatesR.RemoveAt(targetPlatesR.Count - 1);

        bool TryAddPlates()
        {
            if (targetPlatesL.Count >= maxPlatesPerSide || targetPlatesR.Count >= maxPlatesPerSide) return false; 

            float remainingWeight = plateWeightTarget - BenchMoverCurrentTargetWeight;

            if (remainingWeight >= GetWeightFloat(PlateWeight.Plate45) * 2)
            {
                targetPlatesL.Add(PlateWeight.Plate45);
                targetPlatesR.Add(PlateWeight.Plate45);
                TryAddPlates(); 
                return true;
            }
            else if (remainingWeight >= GetWeightFloat(PlateWeight.Plate35) * 2)
            {
                targetPlatesL.Add(PlateWeight.Plate35);
                targetPlatesR.Add(PlateWeight.Plate35);
                TryAddPlates();
                return true;
            }
            else if (remainingWeight >= GetWeightFloat(PlateWeight.Plate25) * 2)
            {
                targetPlatesL.Add(PlateWeight.Plate25);
                targetPlatesR.Add(PlateWeight.Plate25);
                TryAddPlates();
                return true;
            }
            else if (remainingWeight >= GetWeightFloat(PlateWeight.Plate10) * 2)
            {
                targetPlatesL.Add(PlateWeight.Plate10);
                targetPlatesR.Add(PlateWeight.Plate10);
                TryAddPlates();
                return true;
            }
            else if (remainingWeight >= GetWeightFloat(PlateWeight.Plate5) * 2)
            {
                targetPlatesL.Add(PlateWeight.Plate5);
                targetPlatesR.Add(PlateWeight.Plate5);
                TryAddPlates();
                return true;
            }

            return false;
        }

        int i = 0;
        int j = int.MinValue;
        int k = int.MinValue;
        while(true)
        {
            var result = TryAddPlates();
            if (!result)
            {
                if (targetPlatesL.Count <= 0 && targetPlatesR.Count <= 0) break;
                if (targetPlatesL.Count > 0) targetPlatesL.RemoveAt(targetPlatesL.Count - 1);
                if (targetPlatesR.Count > 0) targetPlatesR.RemoveAt(targetPlatesR.Count - 1);  
            } 
            else if (Mathf.Abs(plateWeightTarget - BenchMoverCurrentTargetWeight) < 1)
            {
                break;
            } 
            else
            {
                float currentWeight = BenchMoverCurrentTargetWeight;
                for (int l = 1; l <= targetPlatesL.Count; l++)
                {
                    if (currentWeight > plateWeightTarget || (GetNearestMinimumWeight(l, plateWeightTarget - GetBenchMoverTargetWeight(targetPlatesL.Count - l)) + GetBenchMoverTargetWeight(targetPlatesL.Count - l)) > currentWeight)
                    {
                        if (targetPlatesL.Count <= 0 && targetPlatesR.Count <= 0) break;
                        targetPlatesL.RemoveRange(targetPlatesL.Count - l, l);
                        targetPlatesR.RemoveRange(targetPlatesR.Count - l, l);

                        if (l > 1)
                        {
                            result = false;
                            break;
                        }

                        currentWeight = BenchMoverCurrentTargetWeight;
                    }
                }
            }
            
            i += result ? 1 : -1; 
            if (k == i) // check if we're stuck in a loop, which would mean the weight cannot be perfectly represented by plates
            {
                if (BenchMoverCurrentTargetWeight < plateWeightTarget) TryAddPlates(); // Make sure we're at the highest plate weight possible for this target
                break; 
            }

            k = j;
            j = i;
        }
    }

    protected void InitializePlateLoaders()
    {

        poolPlate5 = new GameObject("plate5s").AddComponent<PrefabPool>();
        poolPlate5.Reinitialize(plate5Prefab, PoolGrowthMethod.Incremental, 1, 8, 30);
        plate5Prefab.SetActive(false);

        poolPlate10 = new GameObject("plate10s").AddComponent<PrefabPool>();
        poolPlate10.Reinitialize(plate10Prefab, PoolGrowthMethod.Incremental, 1, 8, 30);
        plate10Prefab.SetActive(false);

        poolPlate25 = new GameObject("plate25s").AddComponent<PrefabPool>();
        poolPlate25.Reinitialize(plate25Prefab, PoolGrowthMethod.Incremental, 1, 8, 30);
        plate25Prefab.SetActive(false);

        poolPlate35 = new GameObject("plate35s").AddComponent<PrefabPool>();
        poolPlate35.Reinitialize(plate35Prefab, PoolGrowthMethod.Incremental, 1, 8, 30);
        plate35Prefab.SetActive(false);

        poolPlate45 = new GameObject("plate45s").AddComponent<PrefabPool>();
        poolPlate45.Reinitialize(plate45Prefab, PoolGrowthMethod.Incremental, 1, 8, 30);  
        plate45Prefab.SetActive(false);   

        var root = plateLoaderPositionBoneL.parent;

        plateLoaderStartPositionL = plateLoaderPositionBoneL.localPosition;
        plateLoaderStartPositionR = plateLoaderPositionBoneR.localPosition;
        plateLoaderStartPositionL_Root = root.InverseTransformPoint(plateLoaderPositionBoneL.position);
        plateLoaderStartPositionR_Root = root.InverseTransformPoint(plateLoaderPositionBoneR.position);

        plateLoaderBarStartPositionL = plateLoaderBarExtensionBoneL.localPosition;
        plateLoaderBarStartPositionR = plateLoaderBarExtensionBoneR.localPosition;
        plateLoaderBarStartPositionL_Root = root.InverseTransformPoint(plateLoaderBarExtensionBoneL.position);
        plateLoaderBarStartPositionR_Root = root.InverseTransformPoint(plateLoaderBarExtensionBoneR.position);

        plateLoaderConnectorStartPositionL = plateLoaderBarConnectBoneL.localPosition;
        plateLoaderConnectorStartPositionR = plateLoaderBarConnectBoneR.localPosition;

        plateLoaderDefaultHeightL = plateLoaderHeightBoneL.localPosition.magnitude;
        plateLoaderHeightAxisL = plateLoaderHeightBoneL.localPosition / plateLoaderDefaultHeightL;

        plateLoaderDefaultHeightR = plateLoaderHeightBoneR.localPosition.magnitude;
        plateLoaderHeightAxisR = plateLoaderHeightBoneR.localPosition / plateLoaderDefaultHeightR;

        plateLoaderDoorStartPositionL = plateLoaderDoorL.localPosition;
        plateLoaderDoorStartPositionR = plateLoaderDoorR.localPosition;
        plateLoaderDoorState = plateLoaderDoorTargetState = PlateLoaderDoorState.Closed;
        plateLoaderDoorRetraction = 0;

        if (maxPlatesPerSide <= 0) maxPlatesPerSide = Mathf.CeilToInt((WeightMoverPlateEndL_World - WeightMoverPlateStartL_World).magnitude / plateThickness);
    }

    protected void SetPlateLoaderState(PlateLoaderState state)
    {
        plateLoaderState = state;
        plateLoaderWeightMoverSyncVal = 0;
    }
    protected bool stockpiling;
    protected void UpdatePlateLoaders()
    {

        float deltaTime = Time.deltaTime;

        var plateLoaderPlateStartL_World = PlateLoaderPlateStartL_World;
        var plateLoaderPlateStartR_World = PlateLoaderPlateStartR_World;
        var plateLoaderPlateEndL_World = PlateLoaderPlateEndL_World;
        var plateLoaderPlateEndR_World = PlateLoaderPlateEndR_World;

        var weightMoverPlateStartL_World = WeightMoverPlateStartL_World;
        var weightMoverPlateStartR_World = WeightMoverPlateStartR_World;
        var weightMoverPlateEndL_World = WeightMoverPlateEndL_World;
        var weightMoverPlateEndR_World = WeightMoverPlateEndR_World;

        #region Doors

        switch (plateLoaderDoorState)
        {
            case PlateLoaderDoorState.Closed:
                plateLoaderDoorRetraction = 0;
                switch (plateLoaderDoorTargetState)
                {
                    case PlateLoaderDoorState.Opening:
                    case PlateLoaderDoorState.Open:
                        plateLoaderDoorState = PlateLoaderDoorState.Opening;
                        break;
                }
                break;

            case PlateLoaderDoorState.Open:
                plateLoaderDoorRetraction = plateLoaderDoorOpenCloseTime;
                switch (plateLoaderDoorTargetState)
                {
                    case PlateLoaderDoorState.Closing:
                    case PlateLoaderDoorState.Closed:
                        plateLoaderDoorState = PlateLoaderDoorState.Closing;
                        break;
                }
                break;

            case PlateLoaderDoorState.Closing:
                float doorHeight = plateLoaderDoorL.position.y - 0.2f;
                plateLoaderDoorRetraction = Mathf.Clamp(plateLoaderDoorRetraction - (doorHeight > plateLoaderPlateStartL_World.y && doorHeight > plateLoaderPlateStartR_World.y ? Time.deltaTime : 0), 0, plateLoaderDoorOpenCloseTime);
                if (plateLoaderDoorRetraction <= 0)
                {
                    plateLoaderDoorState = PlateLoaderDoorState.Closed;
                }
                switch (plateLoaderDoorTargetState)
                {
                    case PlateLoaderDoorState.Opening:
                    case PlateLoaderDoorState.Open:
                        plateLoaderDoorState = PlateLoaderDoorState.Opening;
                        break;
                }
                break;

            case PlateLoaderDoorState.Opening:
                plateLoaderDoorRetraction = Mathf.Clamp(plateLoaderDoorRetraction + Time.deltaTime, 0, plateLoaderDoorOpenCloseTime);
                if (plateLoaderDoorRetraction >= plateLoaderDoorOpenCloseTime)
                {
                    plateLoaderDoorState = PlateLoaderDoorState.Open;
                }
                switch (plateLoaderDoorTargetState)
                {
                    case PlateLoaderDoorState.Closing:
                    case PlateLoaderDoorState.Closed:
                        plateLoaderDoorState = PlateLoaderDoorState.Closing;
                        break;
                }
                break;
        }

        float retraction = (plateLoaderDoorRetraction / plateLoaderDoorOpenCloseTime) * plateLoaderDoorRetractionDepth;
        plateLoaderDoorL.localPosition = plateLoaderDoorStartPositionL + plateLoaderDoorRetractionAxis * retraction;
        plateLoaderDoorR.localPosition = plateLoaderDoorStartPositionR + plateLoaderDoorRetractionAxis * retraction;

        #endregion

        var root = plateLoaderPositionBoneL.parent;

        var plateLoaderArmDirWorldL = (plateLoaderPlateEndL_World - plateLoaderPlateStartL_World).normalized;
        var plateLoaderArmDirWorldR = (plateLoaderPlateEndR_World - plateLoaderPlateStartR_World).normalized;

        // X
        var plateLoaderArmAxisWorldL = math.abs(plateLoaderArmDirWorldL);
        var plateLoaderArmAxisWorldR = math.abs(plateLoaderArmDirWorldR);

        // Y
        var plateLoaderHeightAxisWorldL = math.abs(plateLoaderHeightBoneL.parent.TransformDirection(plateLoaderHeightAxisL));
        var plateLoaderHeightAxisWorldR = math.abs(plateLoaderHeightBoneL.parent.TransformDirection(plateLoaderHeightAxisR));

        // Z
        var plateLoaderShiftAxisWorldL = math.abs(Vector3.Cross(plateLoaderHeightAxisWorldL, plateLoaderArmAxisWorldL).normalized);
        var plateLoaderShiftAxisWorldR = math.abs(Vector3.Cross(plateLoaderHeightAxisWorldR, plateLoaderArmAxisWorldR).normalized);

        var stockpileWorldPos = root.TransformPoint(plateLoaderStockpilePosition);
        plateLoaderDoorTargetState = PlateLoaderDoorState.Open;
        bool loaderDoorIsClosed = plateLoaderDoorState != PlateLoaderDoorState.Open;
        bool connected = false;
        float transferDeltaTime = deltaTime / plateLoaderTransferTime;
        LoadedPlate plate = null;
        switch (plateLoaderState)
        {
            default:
                stockpiling = true;
                plateLoaderDoorTargetState = PlateLoaderDoorState.Closed;
                //plateLoaderWeightMoverSyncVal = plateLoaderSyncTime;
                if (!TargetPlatesMatchLoadedPlates)
                {
                    if (TargetPlatesPartiallyMatchLoadedPlates)
                    {
                        if (PlateLoadersAreEmpty) SetPlateLoaderState(PlateLoaderState.LoadingFromPool); else SetPlateLoaderState(PlateLoaderState.UnloadingToPool);
                        break;
                    }
                    else
                    {
                        if (PlateLoadersAreEmpty)
                        {
                            stockpiling = false;
                            SetPlateLoaderState(PlateLoaderState.PreparingToUnloadMover);
                        }
                        else
                        {
                            SetPlateLoaderState(PlateLoaderState.UnloadingToPool);
                        }
                        break;
                    }
                }
                break;

            case PlateLoaderState.TransportingToPool:
                if (TargetPlatesMatchTransitPlates)
                {
                    SetPlateLoaderState(PlateLoaderState.TransportingToMover);
                    break;
                }
                if (PlateLoadersAreEmpty && !TargetPlatesPartiallyMatchLoadedPlates && !TargetPlatesMatchLoadedPlates)
                {
                    SetPlateLoaderState(PlateLoaderState.PreparingToUnloadMover); // if the loaders are empty and the mover plates don't match the target, go remove them
                    break;
                }
                if (plateLoaderWeightMoverSyncVal >= plateLoaderSyncTime) SetPlateLoaderState(PlateLoaderState.UnloadingToPool);
                break;

            case PlateLoaderState.UnloadingToPool:
                stockpiling = true;
                connected = true;
                //plateLoaderDoorTargetState = PlateLoaderDoorState.Closed;
                //plateLoaderWeightMoverSyncVal = plateLoaderSyncTime;
                if (plateLoaderConnectionProgress >= plateLoaderConnectionTime && plateLoaderWeightMoverSyncVal >= plateLoaderSyncTime - Mathf.Epsilon)
                {
                    if (transitPlatesL.Count > 0)
                    {
                        plate = transitPlatesL[transitPlatesL.Count - 1];
                        plate.position = Mathf.Clamp01(plate.position - transferDeltaTime);
                        if (plate.transform.parent != null) plate.transform.SetParent(null, true);
                        plate.UpdateWorldPosition(0, transitPlatesL.Count - 1, stockpileWorldPos, plateLoaderPlateStartL_World, plateThickness);
                        if (plate.position <= 0)
                        {
                            transitPlatesL.RemoveAt(transitPlatesL.Count - 1);
                            plate.ReleaseInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);

                        }
                        else
                        {
                            if (!weightMoverPlateSlidingSoundL.isPlaying)
                            {
                                weightMoverPlateSlidingSoundL.Play();
                                weightMoverPlateSlidingSoundL.volume = weightMoverPlateSlidingSoundVolume;
                                weightMoverPlateSlidingSoundL.pitch = UnityEngine.Random.Range(weightMoverPlateSlidingSoundPitchRange.x, weightMoverPlateSlidingSoundPitchRange.y);
                            }
                            weightMoverPlateSlidingSoundL.transform.position = plate.transform.position;
                        }
                    }
                    if (transitPlatesR.Count > 0)
                    {
                        plate = transitPlatesR[transitPlatesR.Count - 1];
                        plate.position = Mathf.Clamp01(plate.position - transferDeltaTime);
                        if (plate.transform.parent != null) plate.transform.SetParent(null, true);
                        plate.UpdateWorldPosition(0, transitPlatesR.Count - 1, stockpileWorldPos, plateLoaderPlateStartR_World, plateThickness);
                        if (plate.position <= 0)
                        {
                            transitPlatesR.RemoveAt(transitPlatesR.Count - 1);
                            plate.ReleaseInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);

                        }
                        else
                        {
                            if (!weightMoverPlateSlidingSoundR.isPlaying)
                            {
                                weightMoverPlateSlidingSoundR.Play();
                                weightMoverPlateSlidingSoundR.volume = weightMoverPlateSlidingSoundVolume;
                                weightMoverPlateSlidingSoundR.pitch = UnityEngine.Random.Range(weightMoverPlateSlidingSoundPitchRange.x, weightMoverPlateSlidingSoundPitchRange.y);
                            }
                            weightMoverPlateSlidingSoundR.transform.position = plate.transform.position;
                        }
                    }

                    if (plate == null)
                    {
                        weightMoverPlateSlidingSoundL.Stop();
                        weightMoverPlateSlidingSoundR.Stop();

                        SetPlateLoaderState(PlateLoaderState.Idle);
                    }
                }
                break;

            case PlateLoaderState.LoadingFromPool:
                stockpiling = true;
                connected = true;
                //plateLoaderWeightMoverSyncVal = plateLoaderSyncTime;
                if (plateLoaderConnectionProgress >= plateLoaderConnectionTime && plateLoaderWeightMoverSyncVal >= plateLoaderSyncTime * 0.9f)
                {
                    bool satisfied = TargetPlatesMatchLoadedPlates;
                    if (satisfied || !TargetPlatesMatchTransitPlates)
                    {
                        if (!satisfied && PlateLoadersAreEmpty)
                        {
                            if (!BenchMoverIsEmpty && !TargetPlatesPartiallyMatchLoadedPlates)
                            {
                                SetPlateLoaderState(PlateLoaderState.UnloadingToPool); // ensure plate loader is empty then go unload the mover first
                                break;
                            }

                            // ask for necessary plates 
                            for (int a = targetPlatesL.Count - 1; a >= loadedPlatesL.Count; a--)
                            {
                                var newPlate = new LoadedPlate() { weight = targetPlatesL[a], position = 0 };
                                newPlate.ClaimNewInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);
                                newPlate.transform.SetParent(plateLoaderHeightBoneL, false);
                                transitPlatesL.Add(newPlate);
                            }
                            for (int a = targetPlatesR.Count - 1; a >= loadedPlatesR.Count; a--)
                            {
                                var newPlate = new LoadedPlate() { weight = targetPlatesR[a], position = 0 };
                                newPlate.ClaimNewInstance(poolPlate5, poolPlate10, poolPlate25, poolPlate35, poolPlate45);
                                newPlate.transform.SetParent(plateLoaderHeightBoneR, false);
                                transitPlatesR.Add(newPlate);
                            }
                        }
                        else
                        {
                            SetPlateLoaderState(PlateLoaderState.UnloadingToPool);
                            break;
                        }
                    }

                    bool ready = true;
                    for (int a = 0; a < transitPlatesL.Count; a++)
                    {
                        plate = transitPlatesL[a];
                        plate.cachedIndex = a;

                        float prevPos = plate.position;
                        plate.position = Mathf.Clamp01(plate.position + transferDeltaTime);
                        plate.UpdateWorldPosition(0, a, stockpileWorldPos, plateLoaderPlateStartL_World, plateThickness);
                        if (plate.position < 1)
                        {
                            if (!weightMoverPlateSlidingSoundL.isPlaying)
                            {
                                weightMoverPlateSlidingSoundL.Play();
                                weightMoverPlateSlidingSoundL.volume = weightMoverPlateSlidingSoundVolume;
                                weightMoverPlateSlidingSoundL.pitch = UnityEngine.Random.Range(weightMoverPlateSlidingSoundPitchRange.x, weightMoverPlateSlidingSoundPitchRange.y);
                            }
                            weightMoverPlateSlidingSoundL.transform.position = plate.transform.position;

                            ready = false;
                            break;
                        }
                        else if (prevPos < 1)
                        {
                            weightMoverPlateClunkSoundL.Play();
                            weightMoverPlateClunkSoundL.volume = Mathf.Lerp(weightMoverPlateClunkSoundVolumeRange.x, weightMoverPlateClunkSoundVolumeRange.y, plate.Weight / GetWeightFloat(PlateWeight.Max));
                            weightMoverPlateClunkSoundL.pitch = Mathf.Lerp(weightMoverPlateClunkSoundPitchRange.y, weightMoverPlateClunkSoundPitchRange.x, plate.Weight / GetWeightFloat(PlateWeight.Max)) + UnityEngine.Random.Range(-0.01f, 0.01f);
                            weightMoverPlateClunkSoundR.time = UnityEngine.Random.value * 0.01f;
                            weightMoverPlateClunkSoundL.transform.position = plate.transform.position;
                        }
                    }
                    for (int a = 0; a < transitPlatesR.Count; a++)
                    {
                        plate = transitPlatesR[a];
                        plate.cachedIndex = a;

                        float prevPos = plate.position;
                        plate.position = Mathf.Clamp01(plate.position + transferDeltaTime);
                        plate.UpdateWorldPosition(0, a, stockpileWorldPos, plateLoaderPlateStartR_World, plateThickness);
                        if (plate.position < 1)
                        {
                            if (!weightMoverPlateSlidingSoundR.isPlaying)
                            {
                                weightMoverPlateSlidingSoundR.Play();
                                weightMoverPlateSlidingSoundR.volume = weightMoverPlateSlidingSoundVolume;
                                weightMoverPlateSlidingSoundR.pitch = UnityEngine.Random.Range(weightMoverPlateSlidingSoundPitchRange.x, weightMoverPlateSlidingSoundPitchRange.y);
                            }
                            weightMoverPlateSlidingSoundR.transform.position = plate.transform.position;

                            ready = false;
                            break;
                        }
                        else if (prevPos < 1)
                        {
                            weightMoverPlateClunkSoundR.Play();
                            weightMoverPlateClunkSoundR.volume = Mathf.Lerp(weightMoverPlateClunkSoundVolumeRange.x, weightMoverPlateClunkSoundVolumeRange.y, plate.Weight / GetWeightFloat(PlateWeight.Max));
                            weightMoverPlateClunkSoundR.pitch = Mathf.Lerp(weightMoverPlateClunkSoundPitchRange.y, weightMoverPlateClunkSoundPitchRange.x, plate.Weight / GetWeightFloat(PlateWeight.Max)) + UnityEngine.Random.Range(-0.01f, 0.01f);
                            weightMoverPlateClunkSoundR.time = UnityEngine.Random.value * 0.01f;
                            weightMoverPlateClunkSoundR.transform.position = plate.transform.position;
                        }
                    }

                    if (ready)
                    {
                        weightMoverPlateSlidingSoundL.Stop();
                        weightMoverPlateSlidingSoundR.Stop();

                        SetPlateLoaderState(PlateLoaderState.TransportingToMover);
                    }
                    else
                    {
                        if (plate == null)
                        {
                            weightMoverPlateSlidingSoundL.Stop();
                            weightMoverPlateSlidingSoundR.Stop();

                            SetPlateLoaderState(PlateLoaderState.Idle);
                        }
                    }
                }
                break;

            case PlateLoaderState.PreparingToUnloadMover:
                if ((transitPlatesL.Count > 0 || transitPlatesR.Count > 0) || TargetPlatesPartiallyMatchLoadedPlates || TargetPlatesMatchLoadedPlates)
                {
                    SetPlateLoaderState(PlateLoaderState.TransportingToPool);
                    break;
                }
                if (plateLoaderWeightMoverSyncVal >= plateLoaderSyncTime) SetPlateLoaderState(PlateLoaderState.UnloadingMover);
                break;

            case PlateLoaderState.TransportingToMover:
                if ((!TargetPlatesMatchTransitPlates && !TargetPlatesPartiallyMatchTransitPlates) || TargetPlatesMatchLoadedPlates)
                {
                    SetPlateLoaderState(PlateLoaderState.TransportingToPool);
                    break;
                }
                if (plateLoaderWeightMoverSyncVal >= plateLoaderSyncTime)
                {
                    for (int a = 0; a < transitPlatesL.Count; a++)
                    {
                        var pl = transitPlatesL[a];
                        pl.position = 0;
                        pl.cachedIndex = a;
                    }
                    for (int a = 0; a < transitPlatesR.Count; a++)
                    {
                        var pl = transitPlatesR[a];
                        pl.position = 0;
                        pl.cachedIndex = a;
                    }
                    SetPlateLoaderState(PlateLoaderState.LoadingMover);
                }
                break;

            case PlateLoaderState.UnloadingMover:
                stockpiling = false;
                connected = true;
                //plateLoaderWeightMoverSyncVal = plateLoaderSyncTime;
                if (!PlateLoadersAreEmpty)
                {
                    if (plateLoaderConnectionProgress >= plateLoaderConnectionTime && plateLoaderWeightMoverSyncVal >= plateLoaderSyncTime - Mathf.Epsilon)
                    {
                        bool done = true;
                        for (int a = 0; a < transitPlatesL.Count; a++)
                        {
                            plate = transitPlatesL[a];

                            float prevPos = plate.position;
                            plate.position = Mathf.Clamp01(plate.position + transferDeltaTime);
                            plate.UpdateWorldPosition(plate.cachedIndex, a, weightMoverPlateStartL_World, plateLoaderPlateStartL_World, plateThickness);
                            if (plate.position < 1)
                            {
                                if (!weightMoverPlateSlidingSoundL.isPlaying)
                                {
                                    weightMoverPlateSlidingSoundL.Play();
                                    weightMoverPlateSlidingSoundL.volume = weightMoverPlateSlidingSoundVolume;
                                    weightMoverPlateSlidingSoundL.pitch = UnityEngine.Random.Range(weightMoverPlateSlidingSoundPitchRange.x, weightMoverPlateSlidingSoundPitchRange.y);
                                }
                                weightMoverPlateSlidingSoundL.transform.position = plate.transform.position;

                                done = false;
                                break;
                            }
                            else if (prevPos < 1)
                            {
                                plate.transform.SetParent(plateLoaderHeightBoneL, true);

                                weightMoverPlateClunkSoundL.Play();
                                weightMoverPlateClunkSoundL.volume = Mathf.Lerp(weightMoverPlateClunkSoundVolumeRange.x, weightMoverPlateClunkSoundVolumeRange.y, plate.Weight / GetWeightFloat(PlateWeight.Max));
                                weightMoverPlateClunkSoundL.pitch = Mathf.Lerp(weightMoverPlateClunkSoundPitchRange.y, weightMoverPlateClunkSoundPitchRange.x, plate.Weight / GetWeightFloat(PlateWeight.Max)) + UnityEngine.Random.Range(-0.01f, 0.01f);
                                weightMoverPlateClunkSoundR.time = UnityEngine.Random.value * 0.01f;
                                weightMoverPlateClunkSoundL.transform.position = plate.transform.position;
                            }
                        }
                        for (int a = 0; a < transitPlatesR.Count; a++)
                        {
                            plate = transitPlatesR[a];

                            float prevPos = plate.position;
                            plate.position = Mathf.Clamp01(plate.position + transferDeltaTime);
                            plate.UpdateWorldPosition(plate.cachedIndex, a, weightMoverPlateStartR_World, plateLoaderPlateStartR_World, plateThickness);
                            if (plate.position < 1)
                            {
                                if (!weightMoverPlateSlidingSoundR.isPlaying)
                                {
                                    weightMoverPlateSlidingSoundR.Play();
                                    weightMoverPlateSlidingSoundR.volume = weightMoverPlateSlidingSoundVolume;
                                    weightMoverPlateSlidingSoundR.pitch = UnityEngine.Random.Range(weightMoverPlateSlidingSoundPitchRange.x, weightMoverPlateSlidingSoundPitchRange.y);
                                }
                                weightMoverPlateSlidingSoundR.transform.position = plate.transform.position;

                                done = false;
                                break;
                            }
                            else if (prevPos < 1)
                            {
                                plate.transform.SetParent(plateLoaderHeightBoneR, true);

                                weightMoverPlateClunkSoundR.Play();
                                weightMoverPlateClunkSoundR.volume = Mathf.Lerp(weightMoverPlateClunkSoundVolumeRange.x, weightMoverPlateClunkSoundVolumeRange.y, plate.Weight / GetWeightFloat(PlateWeight.Max));
                                weightMoverPlateClunkSoundR.pitch = Mathf.Lerp(weightMoverPlateClunkSoundPitchRange.y, weightMoverPlateClunkSoundPitchRange.x, plate.Weight / GetWeightFloat(PlateWeight.Max)) + UnityEngine.Random.Range(-0.01f, 0.01f);
                                weightMoverPlateClunkSoundR.time = UnityEngine.Random.value * 0.01f;
                                weightMoverPlateClunkSoundR.transform.position = plate.transform.position;
                            }
                        }

                        if (done || plate == null)
                        {
                            weightMoverPlateSlidingSoundL.Stop();
                            weightMoverPlateSlidingSoundR.Stop();

                            SetPlateLoaderState(PlateLoaderState.TransportingToPool);
                        }
                    }
                }
                else
                {
                    if (BenchMoverIsEmpty)
                    {
                        SetPlateLoaderState(PlateLoaderState.TransportingToPool);
                        break;
                    }
                    else if (plateLoaderConnectionProgress >= plateLoaderConnectionTime && plateLoaderWeightMoverSyncVal >= plateLoaderSyncTime - Mathf.Epsilon)
                    {
                        int startL = loadedPlatesL.Count;
                        int startR = loadedPlatesR.Count;

                        for (int a = 0; a < loadedPlatesL.Count; a++)
                        {
                            if (a >= targetPlatesL.Count)
                            {
                                startL = a;
                                break;
                            }

                            if (loadedPlatesL[a].weight != targetPlatesL[a])
                            {
                                startL = a;
                                break;
                            }
                        }
                        for (int a = 0; a < loadedPlatesR.Count; a++)
                        {
                            if (a >= targetPlatesR.Count)
                            {
                                startR = a;
                                break;
                            }

                            if (loadedPlatesR[a].weight != targetPlatesR[a])
                            {
                                startR = a;
                                break;
                            }
                        }

                        if (startL >= loadedPlatesL.Count && startR >= loadedPlatesR.Count)
                        {
                            SetPlateLoaderState(PlateLoaderState.TransportingToPool);
                            break;
                        }

                        for (int a = loadedPlatesL.Count - 1; a >= startL; a--)
                        {
                            var pl = loadedPlatesL[a];
                            //pl.transform.SetParent(plateLoaderHeightBoneL, true);
                            pl.position = 0;
                            pl.cachedIndex = a;
                            transitPlatesL.Add(pl);
                        }
                        for (int a = loadedPlatesR.Count - 1; a >= startR; a--)
                        {
                            var pl = loadedPlatesR[a];
                            //pl.transform.SetParent(plateLoaderHeightBoneR, true);
                            pl.position = 0;
                            pl.cachedIndex = a;
                            transitPlatesR.Add(pl);
                        }

                        loadedPlatesL.RemoveRange(startL, loadedPlatesL.Count - startL);
                        loadedPlatesR.RemoveRange(startR, loadedPlatesR.Count - startR);
                    }
                }
                break;

            case PlateLoaderState.LoadingMover:
                stockpiling = false;
                connected = true;
                //plateLoaderWeightMoverSyncVal = plateLoaderSyncTime;
                if (PlateLoadersAreEmpty)
                {
                    SetPlateLoaderState(PlateLoaderState.TransportingToPool);
                }
                else if (plateLoaderConnectionProgress >= plateLoaderConnectionTime && plateLoaderWeightMoverSyncVal >= plateLoaderSyncTime - Mathf.Epsilon)
                {
                    bool done = true;
                    for (int a = transitPlatesL.Count - 1; a >= 0; a--)
                    {
                        int b = transitPlatesL.Count - 1 - a;
                        plate = transitPlatesL[a];

                        float prevPos = plate.position;
                        plate.position = Mathf.Clamp01(plate.position + transferDeltaTime);
                        if (plate.transform.parent != weightMover) plate.transform.SetParent(weightMover, true);
                        plate.UpdateWorldPosition(a, loadedPlatesL.Count + b, plateLoaderPlateStartL_World, weightMoverPlateStartL_World, plateThickness);
                        if (plate.position < 1)
                        {
                            if (!weightMoverPlateSlidingSoundL.isPlaying)
                            {
                                weightMoverPlateSlidingSoundL.Play();
                                weightMoverPlateSlidingSoundL.volume = weightMoverPlateSlidingSoundVolume;
                                weightMoverPlateSlidingSoundL.pitch = UnityEngine.Random.Range(weightMoverPlateSlidingSoundPitchRange.x, weightMoverPlateSlidingSoundPitchRange.y);
                            }
                            weightMoverPlateSlidingSoundL.transform.position = plate.transform.position;

                            done = false;
                            break;
                        }
                        else if (prevPos < 1)
                        {
                            weightMoverPlateClunkSoundL.Play();
                            weightMoverPlateClunkSoundL.volume = Mathf.Lerp(weightMoverPlateClunkSoundVolumeRange.x, weightMoverPlateClunkSoundVolumeRange.y, plate.Weight / GetWeightFloat(PlateWeight.Max));
                            weightMoverPlateClunkSoundL.pitch = Mathf.Lerp(weightMoverPlateClunkSoundPitchRange.y, weightMoverPlateClunkSoundPitchRange.x, plate.Weight / GetWeightFloat(PlateWeight.Max)) + UnityEngine.Random.Range(-0.01f, 0.01f);
                            weightMoverPlateClunkSoundR.time = UnityEngine.Random.value * 0.01f;
                            weightMoverPlateClunkSoundL.transform.position = plate.transform.position;
                        }
                    }
                    for (int a = transitPlatesR.Count - 1; a >= 0; a--)
                    {
                        int b = transitPlatesR.Count - 1 - a;
                        plate = transitPlatesR[a];

                        float prevPos = plate.position;
                        plate.position = Mathf.Clamp01(plate.position + transferDeltaTime);
                        if (plate.transform.parent != weightMover) plate.transform.SetParent(weightMover, true);
                        plate.UpdateWorldPosition(a, loadedPlatesR.Count + b, plateLoaderPlateStartR_World, weightMoverPlateStartR_World, plateThickness);
                        if (plate.position < 1)
                        {
                            if (!weightMoverPlateSlidingSoundR.isPlaying)
                            {
                                weightMoverPlateSlidingSoundR.Play();
                                weightMoverPlateSlidingSoundR.volume = weightMoverPlateSlidingSoundVolume;
                                weightMoverPlateSlidingSoundR.pitch = UnityEngine.Random.Range(weightMoverPlateSlidingSoundPitchRange.x, weightMoverPlateSlidingSoundPitchRange.y);
                            }
                            weightMoverPlateSlidingSoundR.transform.position = plate.transform.position;

                            done = false;
                            break;
                        }
                        else if (prevPos < 1)
                        {
                            weightMoverPlateClunkSoundR.Play();
                            weightMoverPlateClunkSoundR.volume = Mathf.Lerp(weightMoverPlateClunkSoundVolumeRange.x, weightMoverPlateClunkSoundVolumeRange.y, plate.Weight / GetWeightFloat(PlateWeight.Max));
                            weightMoverPlateClunkSoundR.pitch = Mathf.Lerp(weightMoverPlateClunkSoundPitchRange.y, weightMoverPlateClunkSoundPitchRange.x, plate.Weight / GetWeightFloat(PlateWeight.Max)) + UnityEngine.Random.Range(-0.01f, 0.01f);
                            weightMoverPlateClunkSoundR.time = UnityEngine.Random.value * 0.01f;
                            weightMoverPlateClunkSoundR.transform.position = plate.transform.position;
                        }
                    }

                    if (done || plate == null)
                    {
                        weightMoverPlateSlidingSoundL.Stop();
                        weightMoverPlateSlidingSoundR.Stop();

                        for (int a = transitPlatesL.Count - 1; a >= 0; a--) loadedPlatesL.Add(transitPlatesL[a]);
                        for (int a = transitPlatesR.Count - 1; a >= 0; a--) loadedPlatesR.Add(transitPlatesR[a]);
                        transitPlatesL.Clear();
                        transitPlatesR.Clear();
                        SetPlateLoaderState(PlateLoaderState.TransportingToPool);
                    }
                }
                break;
        }

        Vector3 targetWorldPos = stockpiling ? stockpileWorldPos : (loaderDoorIsClosed ? stockpileWorldPos : weightMover.position);
        if (!connected)
        {
            float prev = plateLoaderConnectionProgress;
            plateLoaderConnectionProgress = Mathf.Clamp(plateLoaderConnectionProgress - deltaTime, 0, plateLoaderConnectionTime);
            if (plateLoaderConnectionProgress > 0)
            {
                plateLoaderWeightMoverSyncVal = plateLoaderSyncTime * 0.99f;
            }
            else
            {
                if (prev > 0)
                {
                    plateLoaderWeightMoverSyncVal = 0;
                    stockpiling = !stockpiling;
                }
                if (stockpiling || !loaderDoorIsClosed) plateLoaderWeightMoverSyncVal = Mathf.Clamp(plateLoaderWeightMoverSyncVal + deltaTime, 0, plateLoaderSyncTime);
            }
        }

        Vector3 targetBasePosL = plateLoaderStartPositionL + plateLoaderPositionBoneL.parent.InverseTransformVector(Vector3.Scale(targetWorldPos - root.TransformPoint(plateLoaderStartPositionL_Root), plateLoaderShiftAxisWorldL));
        Vector3 targetBasePosR = plateLoaderStartPositionR + plateLoaderPositionBoneR.parent.InverseTransformVector(Vector3.Scale(targetWorldPos - root.TransformPoint(plateLoaderStartPositionR_Root), plateLoaderShiftAxisWorldR));

        Vector3 targetHeightPosL = (plateLoaderHeightAxisL * plateLoaderDefaultHeightL) + plateLoaderPositionBoneL.InverseTransformVector(Vector3.Scale(targetWorldPos - root.TransformPoint(plateLoaderBarStartPositionL_Root), plateLoaderHeightAxisWorldL));
        Vector3 targetHeightPosR = (plateLoaderHeightAxisR * plateLoaderDefaultHeightR) + plateLoaderPositionBoneR.InverseTransformVector(Vector3.Scale(targetWorldPos - root.TransformPoint(plateLoaderBarStartPositionR_Root), plateLoaderHeightAxisWorldR));

        if (connected)
        {
            plateLoaderWeightMoverSyncVal = Mathf.Clamp(plateLoaderWeightMoverSyncVal + deltaTime, 0, plateLoaderSyncTime/*plateLoaderConnectionProgress < plateLoaderConnectionTime ? (plateLoaderSyncTime * 0.99f) : plateLoaderSyncTime*/);
            float alignment = Vector3.Dot((targetWorldPos - plateLoaderPlateEndL_World).normalized, plateLoaderArmDirWorldL);
            if (alignment >= 0.995f)
            {
                plateLoaderConnectionProgress = Mathf.Clamp(plateLoaderConnectionProgress + deltaTime, 0, plateLoaderConnectionTime);
            }
        }

        float sync = plateLoaderWeightMoverSyncVal / plateLoaderSyncTime;
        float connect = Mathf.Clamp01(plateLoaderConnectionProgress / plateLoaderConnectionTime);
        if (connect > 0)
        {
            Vector3 plateLoaderBarStartPositionL_World = root.TransformPoint(plateLoaderBarStartPositionL_Root);
            Vector3 plateLoaderBarStartPositionR_World = root.TransformPoint(plateLoaderBarStartPositionR_Root);

            targetBasePosL = targetBasePosL + plateLoaderPositionBoneL.parent.InverseTransformVector(Vector3.Scale(weightMoverPlateEndL_World - (plateLoaderBarStartPositionL_World + plateLoaderArmDirWorldL * plateLoaderConnectionGap), plateLoaderArmAxisWorldL)) * connect;
            targetBasePosR = targetBasePosR + plateLoaderPositionBoneR.parent.InverseTransformVector(Vector3.Scale(weightMoverPlateEndR_World - (plateLoaderBarStartPositionR_World + plateLoaderArmDirWorldR * plateLoaderConnectionGap), plateLoaderArmAxisWorldR)) * connect;
        }
        float connect_inverse = (1 - connect) * 0.8f;
        plateLoaderBarConnectBoneL.localPosition = plateLoaderConnectorStartPositionL - plateLoaderConnectorStartPositionL * connect_inverse;
        plateLoaderBarConnectBoneR.localPosition = plateLoaderConnectorStartPositionR - plateLoaderConnectorStartPositionR * connect_inverse;

        Vector3 plateLoaderPrevPosL = plateLoaderPositionBoneL.localPosition;
        Vector3 plateLoaderPrevPosR = plateLoaderPositionBoneR.localPosition;
        if (sync >= 1)
        {
            plateLoaderPositionBoneL.localPosition = targetBasePosL;
            plateLoaderPositionBoneR.localPosition = targetBasePosR;

            plateLoaderHeightBoneL.localPosition = targetHeightPosL;
            plateLoaderHeightBoneR.localPosition = targetHeightPosR; 
        }
        else
        {
            plateLoaderPositionBoneL.localPosition = Vector3.Lerp(plateLoaderPositionBoneL.localPosition, targetBasePosL, sync * deltaTime * 10);
            plateLoaderPositionBoneR.localPosition = Vector3.Lerp(plateLoaderPositionBoneR.localPosition, targetBasePosR, sync * deltaTime * 10);

            plateLoaderHeightBoneL.localPosition = Vector3.Lerp(plateLoaderHeightBoneL.localPosition, targetHeightPosL, sync * deltaTime * 10);
            plateLoaderHeightBoneR.localPosition = Vector3.Lerp(plateLoaderHeightBoneR.localPosition, targetHeightPosR, sync * deltaTime * 10);
        }


        float plateLoaderTranslationL = (plateLoaderPositionBoneL.localPosition - plateLoaderPrevPosL).magnitude;
        float plateLoaderTranslationR = (plateLoaderPositionBoneR.localPosition - plateLoaderPrevPosR).magnitude;

        if (plateLoaderTranslationL > 0.00001f && !plateLoaderTrunkServoL.isPlaying)
        {
            float vol = 0.001f;
            plateLoaderTrunkServoL.loop = true;
            plateLoaderTrunkServoL.volume = vol;

            IEnumerator Whir()
            {
                plateLoaderTrunkServoL.Play();
                //plateLoaderTrunkServoL.time = plateLoaderTrunkServoL.clip.length * UnityEngine.Random.value;

                float seed = UnityEngine.Random.Range(0.85f, 1.1f);
                while (plateLoaderTrunkServoL.isPlaying)
                {
                    var pos = plateLoaderPositionBoneL.localPosition;
                    plateLoaderTranslationL = (pos - plateLoaderPrevPosL).magnitude;
                    if (plateLoaderTranslationL <= 0.00001f)
                    {
                        vol = vol - Time.deltaTime * 2;
                        if (vol <= 0) break;
                    }
                    else
                    {
                        vol = Mathf.Min(1, vol + Time.deltaTime * 2);
                    }
                    plateLoaderPrevPosL = pos;

                    plateLoaderTrunkServoL.volume = Mathf.Lerp(plateLoaderTrunkServoL.volume, Mathf.Lerp(plateLoaderTrunkServoVolumeRange.x, plateLoaderTrunkServoVolumeRange.y, plateLoaderTranslationL / 0.001f) * vol, Time.deltaTime * 5 * seed);
                    plateLoaderTrunkServoL.pitch = Mathf.Clamp(Mathf.Lerp(plateLoaderTrunkServoL.pitch, Mathf.Lerp(plateLoaderTrunkServoPitchRange.x, plateLoaderTrunkServoPitchRange.y * seed, plateLoaderTranslationL / 0.002f), Time.deltaTime * 0.5f * seed), plateLoaderTrunkServoPitchRange.x, plateLoaderTrunkServoPitchRange.y * seed);

                    yield return null;
                }

                float baseVol = plateLoaderTrunkServoL.volume;
                while (vol > 0 && plateLoaderTrunkServoL.isPlaying)
                {
                    vol = vol - Time.deltaTime * 2;
                    plateLoaderTrunkServoL.volume = baseVol * vol;

                    yield return null;
                }

                plateLoaderTrunkServoL.Stop();
            }

            StartCoroutine(Whir());
        }

        if (plateLoaderTranslationR > 0.00001f && !plateLoaderTrunkServoR.isPlaying)
        {
            float vol = 0.001f;
            plateLoaderTrunkServoR.loop = true;
            plateLoaderTrunkServoR.volume = vol;

            float seed = UnityEngine.Random.Range(0.85f, 1.1f);
            IEnumerator Whir()
            {
                plateLoaderTrunkServoR.Play();
                plateLoaderTrunkServoR.time = plateLoaderTrunkServoR.clip.length * (0.3f + UnityEngine.Random.value * 0.4f);

                while (plateLoaderTrunkServoR.isPlaying)
                {
                    var pos = plateLoaderPositionBoneR.localPosition;
                    plateLoaderTranslationR = (pos - plateLoaderPrevPosR).magnitude;
                    if (plateLoaderTranslationR <= 0.00001f)
                    {
                        vol = vol - Time.deltaTime * 2;
                        if (vol <= 0) break;
                    }
                    else
                    {
                        vol = Mathf.Min(1, vol + Time.deltaTime * 2);
                    }
                    plateLoaderPrevPosR = pos;

                    plateLoaderTrunkServoR.volume = Mathf.Lerp(plateLoaderTrunkServoR.volume, Mathf.Lerp(plateLoaderTrunkServoVolumeRange.x, plateLoaderTrunkServoVolumeRange.y, plateLoaderTranslationR / 0.001f) * vol, Time.deltaTime * 5 * seed);
                    plateLoaderTrunkServoR.pitch = Mathf.Clamp(Mathf.Lerp(plateLoaderTrunkServoR.pitch, Mathf.Lerp(plateLoaderTrunkServoPitchRange.x, plateLoaderTrunkServoPitchRange.y * seed, plateLoaderTranslationR / 0.002f), Time.deltaTime * 0.5f * seed), plateLoaderTrunkServoPitchRange.x, plateLoaderTrunkServoPitchRange.y * seed);

                    yield return null;
                }

                float baseVol = plateLoaderTrunkServoR.volume;
                while (vol > 0 && plateLoaderTrunkServoR.isPlaying)
                {
                    vol = vol - Time.deltaTime * 2;
                    plateLoaderTrunkServoR.volume = baseVol * vol;

                    yield return null;
                }

                plateLoaderTrunkServoR.Stop();
            }

            StartCoroutine(Whir());
        }

        if (barbellAddWeightListL.Count > 0 || barbellRemovedWeightListL.Count > 0)
        {
            Transform pillarPlate = barbellAddWeightListL.Count > 0 ? barbellAddWeightListL[0] : barbellRemovedWeightListL[barbellRemovedWeightListL.Count - 1];
            weightPillarPlateSlidingSoundL.transform.position = pillarPlate.position;
            if (!weightPillarPlateSlidingSoundL.isPlaying)
            {
                weightPillarPlateSlidingSoundL.loop = true;
                weightPillarPlateSlidingSoundL.Play(); 
                weightPillarPlateSlidingSoundL.volume = weightPillarPlateSlidingSoundVolume;
                weightPillarPlateSlidingSoundL.pitch = UnityEngine.Random.Range(weightPillarPlateSlidingSoundPitchRange.x, weightPillarPlateSlidingSoundPitchRange.y);
            }
        }
        else
        {
            weightPillarPlateSlidingSoundL.Stop();
        }
        if (barbellAddWeightListR.Count > 0 || barbellRemovedWeightListR.Count > 0)
        {
            Transform pillarPlate = barbellAddWeightListR.Count > 0 ? barbellAddWeightListR[0] : barbellRemovedWeightListR[barbellRemovedWeightListR.Count - 1];
            weightPillarPlateSlidingSoundR.transform.position = pillarPlate.position;
            if (!weightPillarPlateSlidingSoundR.isPlaying)
            {
                weightPillarPlateSlidingSoundR.loop = true;
                weightPillarPlateSlidingSoundR.Play();
                weightPillarPlateSlidingSoundR.volume = weightPillarPlateSlidingSoundVolume;
                weightPillarPlateSlidingSoundR.pitch = UnityEngine.Random.Range(weightPillarPlateSlidingSoundPitchRange.x, weightPillarPlateSlidingSoundPitchRange.y);
                weightPillarPlateSlidingSoundR.time = weightPillarPlateSlidingSoundR.clip.length * (0.3f + UnityEngine.Random.value * 0.5f);
            }
        }
        else
        {
            weightPillarPlateSlidingSoundR.Stop();
        }

    }

    private GameStateController.AnimationTask dummyTask;
    protected void InitializeWorkouts()
    {
        barbellWeightPool = gameObject.AddOrGetComponent<PrefabPool>();
        barbellWeightPool.Reinitialize(barbellWeightPrefab, PoolGrowthMethod.Incremental, 1, 2, 500);
        barbellWeightPrefab.SetActive(false);

        InitializePlateLoaders();

        for(int a = 0; a < workouts.Length; a++)
        {
            var workout = workouts[a];

            catalogue.CreateSelection(workout.displayName, workout.thumbnail, a);

            workout.repCurve.Curve.SetPoints(workout.repCurve.GetNormalizedPoints());
        }

        repKnobImage = repKnob.GetComponentInChildren<Image>();

        dummyTask = new GameStateController.AnimationTask();
    }

    public void SetTargetWorkout(int workoutIndex)
    {
        if (targetWorkout == workoutIndex) return;

        if (targetWorkout >= 0) SetWorkoutActiveState(workouts[targetWorkout], false); 

        repProgress = 0;
        forceRepUpdateFlag = true;
        flexProgress = 0;
        decayingCursorInput = Vector2.zero;
        prevLoad = -1;

        targetWorkout = workoutIndex;
        workoutState = WorkoutState.Resting;
        targetWorkoutState = WorkoutState.Resting;
        if (workoutIndex >= 0 && workoutIndex < workouts.Length)
        {
            var workout = workouts[workoutIndex];

            bodyWeightSettings = workout.bodyWeightSettings;
            if (bodyWeightSettings == null) bodyWeightSettings = new BodyWeightSettings();
            
            flexProgress = workout.flexStartPoint;
            
            foreach (var muscle in workout.targetMuscles)
            {
                muscle.previousMass = -1;
                muscle.previousPump = -1;
                muscle.previousFlex = -1;
            }
             
            SetWorkoutActiveState(workout, true);

            camX = workout.restCamXStartPos;
            camY = workout.restCamYStartPos;
            repCamX = math.remap(workout.repCamTransitionMappingX.x, workout.repCamTransitionMappingX.y, workout.repCamTransitionMappingX.z, workout.repCamTransitionMappingX.w, camX);
            repCamY = math.remap(workout.repCamTransitionMappingY.x, workout.repCamTransitionMappingY.y, workout.repCamTransitionMappingY.z, workout.repCamTransitionMappingY.w, camY);
            flexCamX = math.remap(workout.flexCamTransitionMappingX.x, workout.flexCamTransitionMappingX.y, workout.flexCamTransitionMappingX.z, workout.flexCamTransitionMappingX.w, camX);
            flexCamY = math.remap(workout.flexCamTransitionMappingY.x, workout.flexCamTransitionMappingY.y, workout.flexCamTransitionMappingY.z, workout.flexCamTransitionMappingY.w, camY);

            RefreshRepText(workout);

            return;
        }
    }
    protected void SetWorkoutActiveState(Workout workout, bool active)
    { 
        #region UI

        if (active)
        {

            if (!workout.overrideRepCurveTransformState) repCurveRootDefaultSnapshot.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(repCurveRoot)); else workout.repCurveTransformState.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(repCurveRoot));
            if (!workout.overrideWeightDisplayTransformState) weightDisplayRootDefaultSnapshot.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(weightDisplayRoot)); else workout.weightDisplayTransformState.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(weightDisplayRoot));
            if (!workout.overrideTotalRepCountDisplayTransformState) totalRepCountDisplayRootDefaultSnapshot.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(totalRepCountDisplayRoot)); else workout.totalRepCountDisplayTransformState.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(totalRepCountDisplayRoot));
            if (!workout.overrideWorkoutRepCountDisplayTransformState) workoutRepCountDisplayRootDefaultSnapshot.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(workoutRepCountDisplayRoot)); else workout.workoutRepCountDisplayTransformState.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(workoutRepCountDisplayRoot));
            if (!workout.overrideBurnOutDisplayTransformState) burnOutDisplayRootDefaultSnapshot.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(burnOutDisplayRoot)); else workout.burnOutDisplayTransformState.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(burnOutDisplayRoot));
            if (!workout.overrideEnergyDisplayTransformState) energyDisplayRootDefaultSnapshot.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(energyDisplayRoot)); else workout.energyDisplayTransformState.ApplyTo(Swole.UnityEngineHook.AsSwoleRectTransform(energyDisplayRoot));

            var startPoint = workout.repCurve.GetPoint2D(0);
            var endPoint = workout.repCurve.GetPoint2D(workout.repCurve.PointCount - 1);
            bool isTopBottom = startPoint.y < endPoint.y; // ofc it's opposite
            Vector3[] startPoints = new Vector3[2];
            startPoints[0] = isTopBottom ? startPoint : endPoint;
            //startPoints[1] = (startPoint + endPoint) * 0.5f;
            //startPoints[2] = isTopBottom ? endPoint : startPoint;
            startPoints[1] = isTopBottom ? endPoint : startPoint;
            var startCurve = BezierCurveUnity.Create(startPoints);

            MorphingBezierCurve.WeightedCurve[] weightedCurves = new MorphingBezierCurve.WeightedCurve[]
            {
                new MorphingBezierCurve.WeightedCurve() { curve = startCurve, weight = 0f },
                new MorphingBezierCurve.WeightedCurve() { curve = workout.repCurve, weight = 1f }
            };

            MorphingBezierCurve finalRepCurve = new MorphingBezierCurve(weightedCurves, false);
            for (int i = 0; i < repCurveRenderers.Length; i++)
            {
                var renderer = repCurveRenderers[i];
                //renderer.curve = workout.repCurve;
                renderer.morphingCurve = finalRepCurve;
                renderer.useEqualUVPortionsPerSegment = true;
                renderer.uvRangeX = new Vector2(0.5f, 1);
                renderer.Rebuild();

                if (i == 0)
                {
                    repCurvePoints.Clear();
                    renderer.GetRenderedPoints(repCurvePoints);
                }
            }

        }

        #endregion

        #region Resting

        var rest = workoutCharacterObject.tasks[workout.restTask];
        rest.loop = true;
        if (active) rest.StartIfNotRunning(); else rest.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
        rest.mix = 1;

        var restCam = workoutCharacterObject.tasks[workout.restCamBaseTask];
        restCam.loop = true;
        restCam.speed = 0;
        if (active) restCam.StartIfNotRunning(); else restCam.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
        restCam.mix = 1;

        if (workout.restCamXTask >= 0)
        {
            var task = workoutCharacterObject.tasks[workout.restCamXTask];
            task.loop = true;
            task.speed = 0;
            if (active) task.StartIfNotRunning(); else task.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            task.SetNormalizedAnimationTime(workoutCharacterObject.animations, workout.restCamXStartPos);
            task.mix = 1;
        }
        if (workout.restCamYTask >= 0)
        {
            var task = workoutCharacterObject.tasks[workout.restCamYTask];
            task.loop = true;
            task.speed = 0;
            if (active) task.StartIfNotRunning(); else task.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            task.SetNormalizedAnimationTime(workoutCharacterObject.animations, workout.restCamYStartPos);
            task.mix = 1;
        }

        #endregion

        #region Reps

        if (workout.repBaseTask >= 0)
        {
            var repBase = workoutCharacterObject.tasks[workout.repBaseTask];
            repBase.loop = true;
            if (active) repBase.StartIfNotRunning(); else repBase.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);

            if (workout.repBaseTask != workout.restTask) repBase.mix = 0;
        }
        var repAdd = workoutCharacterObject.tasks[workout.repAddTask];
        repAdd.loop = true;
        repAdd.speed = 0;
        if (active) repAdd.StartIfNotRunning(); else repAdd.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);

        repAdd.mix = 0;

        if (workout.repCamBaseTask >= 0)
        {
            var task = workoutCharacterObject.tasks[workout.repCamBaseTask];
            task.loop = true;
            task.speed = 0;
            if (active) task.StartIfNotRunning(); else task.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);

            if (workout.repCamBaseTask != workout.restCamBaseTask) task.mix = 0;
        }
        if (workout.repCamXTask >= 0)
        {
            var task = workoutCharacterObject.tasks[workout.repCamXTask];
            task.loop = true;
            task.speed = 0;
            if (active) task.StartIfNotRunning(); else task.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            task.SetNormalizedAnimationTime(workoutCharacterObject.animations, math.remap(workout.repCamTransitionMappingX.x, workout.repCamTransitionMappingX.y, workout.repCamTransitionMappingX.z, workout.repCamTransitionMappingX.w, workout.restCamXStartPos));

            if (workout.repCamXTask != workout.restCamXTask) task.mix = 0;
        }
        if (workout.repCamYTask >= 0)
        {
            var task = workoutCharacterObject.tasks[workout.repCamYTask];
            task.loop = true;
            task.speed = 0;
            if (active) task.StartIfNotRunning(); else task.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            task.SetNormalizedAnimationTime(workoutCharacterObject.animations, math.remap(workout.repCamTransitionMappingY.x, workout.repCamTransitionMappingY.y, workout.repCamTransitionMappingY.z, workout.repCamTransitionMappingY.w, workout.restCamYStartPos));

            if (workout.repCamYTask != workout.restCamYTask) task.mix = 0;
        }

        #endregion

        #region Flexing

        if (workout.flexBaseTask >= 0)
        {
            var flexBase = workoutCharacterObject.tasks[workout.flexBaseTask];
            flexBase.loop = true;
            if (active) flexBase.StartIfNotRunning(); else flexBase.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);  
            flexBase.mix = 0;
        }

        if (workout.flexControlTask >= 0)
        {
            var flexControl = workoutCharacterObject.tasks[workout.flexControlTask];
            flexControl.loop = true;
            flexControl.speed = 0;
            if (active) flexControl.StartIfNotRunning(); else flexControl.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            flexControl.mix = 0;
        }

        if (workout.flexCamBaseTask >= 0)
        {
            var task = workoutCharacterObject.tasks[workout.flexCamBaseTask];
            task.loop = true;
            task.speed = 0;
            if (active) task.StartIfNotRunning(); else task.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);

            if (workout.flexCamBaseTask != workout.restCamBaseTask) task.mix = 0;
        }
        if (workout.flexCamXTask >= 0)
        {
            var task = workoutCharacterObject.tasks[workout.flexCamXTask];
            task.loop = true;
            task.speed = 0;
            if (active) task.StartIfNotRunning(); else task.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            task.SetNormalizedAnimationTime(workoutCharacterObject.animations, math.remap(workout.flexCamTransitionMappingX.x, workout.flexCamTransitionMappingX.y, workout.flexCamTransitionMappingX.z, workout.flexCamTransitionMappingX.w, workout.restCamXStartPos));

            if (workout.flexCamXTask != workout.restCamXTask) task.mix = 0;
        }
        if (workout.flexCamYTask >= 0)
        {
            var task = workoutCharacterObject.tasks[workout.flexCamYTask];
            task.loop = true;
            task.speed = 0;
            if (active) task.StartIfNotRunning(); else task.StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            task.SetNormalizedAnimationTime(workoutCharacterObject.animations, math.remap(workout.flexCamTransitionMappingY.x, workout.flexCamTransitionMappingY.y, workout.flexCamTransitionMappingY.z, workout.flexCamTransitionMappingY.w, workout.restCamYStartPos));

            if (workout.flexCamYTask != workout.restCamYTask) task.mix = 0;
        }

        #endregion


        #region Offsets

        if (!active)
        {
            if (workout.restOffsetTasks != null)
            {
                foreach(var taskIndex in workout.restOffsetTasks) workoutCharacterObject.tasks[taskIndex].StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            }
            if (workout.repOffsetTasks != null)
            {
                foreach (var taskIndex in workout.repOffsetTasks) workoutCharacterObject.tasks[taskIndex].StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            }
            if (workout.flexOffsetTasks != null)
            {
                foreach (var taskIndex in workout.flexOffsetTasks) workoutCharacterObject.tasks[taskIndex].StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            }
        }

        #endregion
         
        #region Transitions
         
        if (!active)
        {
            if (workout.restToRepTask >= 0) workoutCharacterObject.tasks[workout.restToRepTask].StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            if (workout.repToRestTask >= 0) workoutCharacterObject.tasks[workout.repToRestTask].StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
            if (workout.restToFlexTask >= 0) workoutCharacterObject.tasks[workout.restToFlexTask].StopImmediate(workoutCharacterObject.animations, workoutCharacterObject.sounds);
        }

        #endregion
    }

    private Vector3 GetRepCurvePosition(float time)
    {
        float repCurvePos = Mathf.Clamp(time * (repCurvePoints.Count - 1), 0, (repCurvePoints.Count - 1));
        int startIndex = Mathf.FloorToInt(repCurvePos);
        int endIndex = startIndex + 1;
        while (endIndex >= repCurvePoints.Count) endIndex -= repCurvePoints.Count;
        return Vector3.LerpUnclamped(repCurvePoints[startIndex], repCurvePoints[endIndex], repCurvePos - startIndex);
    }

    private IEnumerator PulseRT(RectTransform rectTransform, float duration, float maxScale, Swole.VoidParameterlessDelegate postPulse = null)
    {
        float time = 0;
        while (time < duration)
        {
            time = time + Time.deltaTime;
            float factor = Mathf.Clamp01(time / duration);
            float scale = 1 - Mathf.Abs((factor - 0.5f) / 0.5f);
            rectTransform.localScale = (new Vector3(scale, scale, scale) * maxScale) + Vector3.one; 
            yield return null;
        }

        rectTransform.localScale = Vector3.one;
        postPulse?.Invoke();
    }

    public float repTextPulseScale = 0.25f;
    public float repTextPulseDuration = 0.15f;
    public void RefreshRepText() => RefreshRepText(targetWorkout < 0 ? null : workouts[targetWorkout]);
    public void RefreshRepText(Workout workout)
    {
        if (totalRepText != null) totalRepText.SetText(totalRepCount + "x REPS");
        workoutRepText.SetText((workout == null ? 0 : workout.repCount) + "x REPS");
    }
    private void CompleteRep(Workout workout, float finalAccuracy, float liftRatio)
    {

        repCompletedThisFrame = true;
        repHoldTime = 0;

        float stamEng = stamina * energy;
        repCompletionBurnOut = 1 - stamEng;

        float burnOutStam = 1 - stamina;
        if (burnOutStam >= 0.985f) IncrementRepBurnOutCounter();
        
        totalRepCount++;
        workout.repCount++;

        if (unsteadyRepFlag)
        {
            unsteadyRepFlag = false;
        } 
        else
        {
            IncrementRepCombo();
        }

        RefreshRepText(workout);

        StartCoroutine(PulseRT(workoutRepText.rectTransform, repTextPulseDuration, repTextPulseScale));

        bool isPerfect = finalAccuracy >= workout.perfectRepMinAccuracy;

        float staminaRefund = workout.repCompletionMaxStaminaRefund * finalAccuracy;
        if (isPerfect)
        {
            staminaRefund *= workout.perfectRepStaminaRefundMultiplier;
        }
        staminaRefund = staminaRefund * (1 - Mathf.Min(1, repBurnOutCounter / 5f));
        stamina = Mathf.Clamp01(stamina + staminaRefund);

        float accuracyGrowthMul = isPerfect ? 1.5f : Mathf.LerpUnclamped(1, finalAccuracy, 0.35f);
        float burnOutMul = Mathf.LerpUnclamped(1, workout.burnOutGrowthMultiplier * workout.burnOutRepGrowthBonusMultiplier, Mathf.Pow(1 - Mathf.Lerp(1, stamEng, finalAccuracy * liftRatio), workout.burnOutGrowthBonusFalloff));

        float growth = liftRatio * workout.fullRepGrowthBonus * accuracyGrowthMul * burnOutMul;

        float repComboMul = workout.repComboGrowthMultiplier * liftRatio * repCombo;
        float fullBurnOutMul = workout.burnOutRepCounterGrowthMultiplier * liftRatio * repBurnOutCounter;

        growth = growth + (growth * repComboMul); // apply rep combo bonus
        growth = growth + (growth * fullBurnOutMul); // apply burn out rep counter bonus

        if (characterMuscle != null)
        {
            foreach (var muscle in workout.targetMuscles)
            {
                int index = muscle.GetMuscleIndex(characterMuscle);

                float muscleSize = characterMuscle.GetMuscleGroupMassUnsafe(index);
                //muscleSize = muscleSize + GetMassScaledGrowth(muscleSize, growth) * muscle.growthRatio;
                //characterMuscle.SetMuscleGroupMassUnsafe(index, muscleSize); // made growth happen over time instead with growth accumulation
                muscle.growthAccumulation += GetMassScaledGrowth(muscleSize, growth) * muscle.growthRatio;
            }
        }
    }

    public float repComboTextPulseScale = 0.35f;
    public float repComboBreakTextPulseScale = 0.45f;
    public float repComboTextPulseDuration = 0.25f;
    public float repComboBreakTextPulseDuration = 0.3f;

    private const string tag_FG = "FG";
    private const string tag_BG = "BG";

    public void ResetRepCombo()
    {
        if (repCombo > 1)
        {
            repComboBreakRoot.gameObject.SetActive(true);
            StartCoroutine(PulseRT(repComboBreakRoot, repComboBreakTextPulseDuration, repComboBreakTextPulseScale, () => repComboBreakRoot.gameObject.SetActive(false)));  

            if (!Settings.disableUISounds) repComboBreakSound.Play();
        }

        repCombo = 0;
        repComboCounterRoot.gameObject.SetActive(false);
    }
    public void IncrementRepCombo()
    {
        repCombo++;
        SetRepCombo(repCombo);
    }
    public void SetRepCombo(int combo)
    {
        repCombo = combo;

        if (combo > 1)
        {
            repComboCounterRoot.gameObject.SetActive(true);
            StartCoroutine(PulseRT(repComboCounterRoot, repComboTextPulseDuration, repComboTextPulseScale));

            string text = $"{repCombo}x COMBO";
            Color col = Color.Lerp(Color.Lerp(repComboStartColor, repComboMidColor, repCombo / 10f), repComboFinalColor, (repCombo - 10f) / 20f);
            Color colDark = col / 0.8f;
            colDark.a = 1;

            CustomEditorUtils.SetComponentTextAndColorByName(repComboCounterRoot, tag_BG, text, col);
            CustomEditorUtils.SetComponentTextAndColorByName(repComboCounterRoot, tag_FG, text, colDark);

            if (!Settings.disableUISounds) repComboCounterSound.Play();
        } 
        else
        {
            repComboCounterRoot.gameObject.SetActive(false); 
        }
    }

    public void ResetRepBurnOutCounter()
    {
        if (repBurnOutCounter > 0)
        {
            repOverheatExtinguishRoot.gameObject.SetActive(true);
            StartCoroutine(PulseRT(repOverheatExtinguishRoot, repComboBreakTextPulseDuration, repComboBreakTextPulseScale, () => repOverheatExtinguishRoot.gameObject.SetActive(false)));

            if (!Settings.disableUISounds) repOverheatExtinguishSound.Play();
        }

        repBurnOutCounter = 0;
        repOverheatCounterRoot.gameObject.SetActive(false);
    }
    public void IncrementRepBurnOutCounter()
    {
        repBurnOutCounter++;
        SetRepBurnOutCounter(repBurnOutCounter);
    }
    public void SetRepBurnOutCounter(int counter)
    {
        repBurnOutCounter = counter;

        if (repBurnOutCounter > 0)
        {
            repOverheatCounterRoot.gameObject.SetActive(true);
            StartCoroutine(PulseRT(repOverheatCounterRoot, repComboTextPulseDuration, repComboTextPulseScale));

            string text = $"{repBurnOutCounter}x OVERHEAT";
            Color col = Color.Lerp(Color.Lerp(overheatStartColor, overheatMildColor, repBurnOutCounter / 3f), overheatHotColor, (repBurnOutCounter - 3f) / 7f);
            Color colDark = col / 0.8f;
            colDark.a = 1;

            CustomEditorUtils.SetComponentTextAndColorByName(repOverheatCounterRoot, tag_BG, text, col);  
            CustomEditorUtils.SetComponentTextAndColorByName(repOverheatCounterRoot, tag_FG, text, colDark);

            if (!Settings.disableUISounds) repOverheatCounterSound.Play();
        }
        else
        {
            repOverheatCounterRoot.gameObject.SetActive(false);
        }
    }

    private const float repProgressMeasurementOffset = 0.001f;
    private Vector2 decayingCursorInput;
    private Vector2 lockedCursorInput;
    private Vector2 prevCursorInput;
    private float rotateRepAlignmentFadeIn;
    private float noStaminaRepAlignment;
    private float delayedPlateWeightTarget;
    private float setPlateWeightTargetDelay;
    private float repCurveDifficulty;
    public void RebuildRepCurves()
    {
        float repCurveDifficultyTarget = Mathf.Clamp01(currentLiftRatio);
        repCurveDifficulty = Mathf.MoveTowards(repCurveDifficulty, repCurveDifficultyTarget, Time.deltaTime * 0.25f);
        float thickness = Mathf.Lerp(repCurveThickness.y, repCurveThickness.x, repBurnOutCounter / 10f);
        for (int i = 0; i < repCurveRenderers.Length; i++)
        {
            float localThickness = thickness;
            if (i == repCurveOutlineRenderer) localThickness = thickness + repCurveOutlineThickness;
            var renderer = repCurveRenderers[i];
            renderer.uvRangeX = new Vector2(0.5f - (0.5f * repProgress), 1 - (0.5f * repProgress));
            renderer.SetLineThickness(localThickness);
            renderer.morphWeight = repCurveDifficulty;
            renderer.Rebuild();

            if (i == 0)
            {
                repCurvePoints.Clear();
                renderer.GetRenderedPoints(repCurvePoints);
            }
        }
    }
    public float GetMassScaledGrowth(float muscleSize, float baseGrowth) => baseGrowth < 1 ? Mathf.Pow(baseGrowth, Mathf.Max(1, 1 + ((muscleSize - 0.3f) * 0.29f))) : baseGrowth;
    public void AddScaledPump(float pumpAdd, int muscleIndex, float muscleSize, float mixedMuscleSize)
    {
        if (pumpAdd > 0f && characterPump != null)
        {
            float haultDecay = characterPump.pumpDecayDelay > 0 ? 0 : (characterPump.pumpDecayRate * Time.deltaTime); // prevent pump from decaying this frame

            float pump = characterPump.GetPumpUnsafe(muscleIndex);
            pumpAdd = (pumpAdd / Mathf.Max(1, pump / (Mathf.Lerp(0.003f, 0.000001f, (1 - repDifficulty) + (1 - Mathf.Pow(repProgress, 3))) * Mathf.Pow(Mathf.LerpUnclamped(muscleSize, mixedMuscleSize, 0.5f) + 1, 5)))) / Mathf.Lerp(3, 1, pump); // make gaining pump non-linear
            characterPump.SetPumpUnsafe(muscleIndex, Mathf.Min(1.35f, pump + pumpAdd + haultDecay));
        }
    }
    private float prevLoad;
    private Color repAlignmentColor = Color.green;
    private readonly List<int> tempIndices = new List<int>();
    public void UpdateWorkouts()
    {
        var settings = Swole.swole.Settings;
        var demoSettings = Settings;

        float mouseRepSensX = settings.mouseSensitivityBase * settings.mouseSensitivityX;
        float mouseRepSensY = settings.mouseSensitivityBase * settings.mouseSensitivityY;

        float mouseCamSensX = settings.mouseSensitivityBase2 * settings.mouseSensitivityX2;
        float mouseCamSensY = settings.mouseSensitivityBase2 * settings.mouseSensitivityY2;

        if (Swole.InputProxy.MOBA_AbilityEDown)
        {
            catalogue.gameObject.SetActive(!catalogue.gameObject.activeSelf);  
        }

        bool allowInput = !catalogue.isActiveAndEnabled; 

        if (controlsPopup.activeSelf || pauseMenu.activeSelf)
        {
            allowInput = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (allowInput)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        prevInputStrength = inputStrength;
        prevRepProgress = repProgress;
        prevRepPower = repPower;
        prevRepDeltaProgress = repDeltaProgress;
        prevRepDifficulty = repDifficulty;
        prevRepAlignment = repAlignment;
        repCompletedThisFrame = false;
        float repProgressAlignment = repAlignment;

        var activeWorkout = workouts[targetWorkout];

        var hideBench = workoutCharacterObject.tasks[hideBenchTask];
        var hidePecDeck = workoutCharacterObject.tasks[hidePecDeckTask];
        var hidePulldownBar = workoutCharacterObject.tasks[hidePulldownBarTask];
        var foldBarbell = workoutCharacterObject.tasks[foldBarbellTask];
        var hideBarbell = workoutCharacterObject.tasks[hideBarbellTask];

        hideBenchProgress = (activeWorkout.hideBench ? 1 : 0);//Mathf.Clamp01(hideBenchProgress + ((activeWorkout.hideBench ? 1 : -1) * Time.deltaTime * benchFoldSpeed));
        hidePecDeckProgress = (activeWorkout.hidePecDeck ? 1 : 0); //Mathf.Clamp01(hidePecDeckProgress + ((activeWorkout.hidePecDeck ? 1 : -1) * Time.deltaTime * hideAwaySpeed));
        hidePulldownBarProgress = (activeWorkout.hidePulldownBar ? 1 : 0);//Mathf.Clamp01(hidePulldownBarProgress + ((activeWorkout.hidePulldownBar ? 1 : -1) * Time.deltaTime * hideAwaySpeed));
        foldBarbellProgress = (activeWorkout.foldBarbell ? 1 : 0);//Mathf.Clamp01(foldBarbellProgress + ((activeWorkout.foldBarbell ? 1 : -1) * Time.deltaTime * hideAwaySpeed));
        hideBarbellProgress = (activeWorkout.hideBarbell ? 1 : 0);//Mathf.Clamp01(hideBarbellProgress + ((activeWorkout.hideBarbell ? 1 : -1) * Time.deltaTime * hideAwaySpeed));  

        if (hideBenchProgress <= 0)
        {
            hideBench.StopIfRunning();
        } 
        else
        {
            hideBench.StartIfNotRunning();
            hideBench.SetNormalizedAnimationTime(workoutCharacterObject.animations, hideBenchProgress);
        }
        if (hidePecDeckProgress <= 0)
        {
            hidePecDeck.StopIfRunning();
        }
        else
        {
            hidePecDeck.StartIfNotRunning();
            hidePecDeck.SetNormalizedAnimationTime(workoutCharacterObject.animations, hidePecDeckProgress);
        }
        if (hidePulldownBarProgress <= 0)
        {
            hidePulldownBar.StopIfRunning();
        }
        else
        {
            hidePulldownBar.StartIfNotRunning();
            hidePulldownBar.SetNormalizedAnimationTime(workoutCharacterObject.animations, hidePulldownBarProgress);
        }
        if (foldBarbellProgress <= 0)
        {
            foldBarbell.StopIfRunning();
        }
        else
        {
            foldBarbell.StartIfNotRunning();
            foldBarbell.SetNormalizedAnimationTime(workoutCharacterObject.animations, foldBarbellProgress);
        }
        if (hideBarbellProgress <= 0)
        {
            hideBarbell.StopIfRunning();
        }
        else
        {
            hideBarbell.StartIfNotRunning();
            hideBarbell.SetNormalizedAnimationTime(workoutCharacterObject.animations, hideBarbellProgress);
        }

        foreach (var muscle in activeWorkout.targetMuscles)
        {
            muscle.previousMass = muscle.currentMass;
            muscle.previousPump = muscle.currentPump;
            muscle.previousFlex = muscle.currentFlex;
        }
        
        float currentLoad = 0;
        float targetLoad = 0;
        bool pressedSubWeight = allowInput ? (Swole.InputProxy.LeftBumperDown || Input.GetKeyDown(KeyCode.A) || Swole.InputProxy.ScrollDown) : false;
        bool pressedAddWeight = allowInput ? (Swole.InputProxy.RightBumperDown || Input.GetKeyDown(KeyCode.D) || Swole.InputProxy.ScrollUp) : false;
        float weightDisplayDiv = (activeWorkout.halveWeightDisplay ? 2 : 1);
        switch (activeWorkout.weightSource)
        {
            case WeightSource.Pillars:
                if (pressedAddWeight)
                {
                    SetBarbellStackHeight(Mathf.Min(BarbellTargetStackHeight + 1, 150), false);
                }
                else if (pressedSubWeight)
                {
                    SetBarbellStackHeight(Mathf.Max(0, BarbellTargetStackHeight - 1), false);
                }

                currentLoad = BarbellStackWeight + barbellBarWeight;
                targetLoad = currentLoad + (barbellAddWeightListL.Count * barbellPlateWeight) + (barbellAddWeightListR.Count * barbellPlateWeight);
                if (currentLoad != targetLoad)
                {
                    weightText.SetText(Swole.swole.Settings.weightAsKilograms ? ($"{Mathf.FloorToInt(Swole.swole.ConvertPoundsToKilograms(currentLoad / weightDisplayDiv))} >> {Mathf.FloorToInt(Swole.swole.ConvertPoundsToKilograms(targetLoad / weightDisplayDiv))} KG") : ($"{(currentLoad / (activeWorkout.halveWeightDisplay ? 2 : 1))} >> {(targetLoad / (activeWorkout.halveWeightDisplay ? 2 : 1))} LBS"));
                    prevLoad = float.MinValue;
                }
                else if (prevLoad != currentLoad)
                {
                    weightText.SetText(Swole.swole.Settings.weightAsKilograms ? (Mathf.FloorToInt(Swole.swole.ConvertPoundsToKilograms(currentLoad / weightDisplayDiv)) + " KG") : ((currentLoad / weightDisplayDiv) + " LBS"));
                    prevLoad = currentLoad;
                }
                break;

            case WeightSource.Bench:
                if (setPlateWeightTargetDelay > 0)
                {
                    setPlateWeightTargetDelay -= Time.deltaTime;
                    if (setPlateWeightTargetDelay <= 0)
                    {
                        SetBenchMoverTargetWeight(delayedPlateWeightTarget, false); 
                    }
                }
                if (pressedAddWeight)
                {
                    delayedPlateWeightTarget = (setPlateWeightTargetDelay > 0 ? delayedPlateWeightTarget : plateWeightTarget) + GetWeightFloat(PlateWeight.Plate10);
                    setPlateWeightTargetDelay = 1; 
                }
                else if (pressedSubWeight)
                {
                    delayedPlateWeightTarget = (setPlateWeightTargetDelay > 0 ? delayedPlateWeightTarget : plateWeightTarget) - GetWeightFloat(PlateWeight.Plate10);
                    setPlateWeightTargetDelay = 1;
                }

                currentLoad = BenchMoverLoadedWeight + benchMoverPlatformWeight;
                targetLoad = delayedPlateWeightTarget + benchMoverPlatformWeight;
                if (currentLoad != targetLoad)
                {
                    weightText.SetText(Swole.swole.Settings.weightAsKilograms ? ($"{Mathf.FloorToInt(Swole.swole.ConvertPoundsToKilograms(currentLoad / weightDisplayDiv))} >> {Mathf.FloorToInt(Swole.swole.ConvertPoundsToKilograms(targetLoad / weightDisplayDiv))} KG") : ($"{Mathf.RoundToInt(currentLoad / weightDisplayDiv)} >> {Mathf.RoundToInt(targetLoad / weightDisplayDiv)} LBS"));
                    prevLoad = float.MinValue;
                }
                else if (prevLoad != currentLoad) 
                { 
                    weightText.SetText(Swole.swole.Settings.weightAsKilograms ? (Mathf.FloorToInt(Swole.swole.ConvertPoundsToKilograms(currentLoad / weightDisplayDiv)) + " KG") : (Mathf.RoundToInt(currentLoad / weightDisplayDiv) + " LBS")); 
                    prevLoad = currentLoad;
                }            
                break;

            case WeightSource.Bodyweight:
                currentLoad = activeWorkout.bodyWeightSettings.CalculateBodyWeight(100, characterMuscle);
                if (prevLoad != currentLoad)
                {
                    weightText.SetText(Swole.swole.Settings.weightAsKilograms ? (Mathf.FloorToInt(Swole.swole.ConvertPoundsToKilograms(currentLoad / weightDisplayDiv)) + " KG") : (Mathf.RoundToInt(currentLoad / weightDisplayDiv) + " LBS"));
                    prevLoad = currentLoad; 
                }
                break;
        }

        //GameStateController.AnimationTask tempTask;

        var restTask = workoutCharacterObject.tasks[activeWorkout.restTask];
        var restCamTask = workoutCharacterObject.tasks[activeWorkout.restCamBaseTask];
        GameStateController.AnimationTask restCamTaskX = null;
        GameStateController.AnimationTask restCamTaskY = null;
        if (activeWorkout.restCamXTask >= 0) restCamTaskX = workoutCharacterObject.tasks[activeWorkout.restCamXTask];        
        if (activeWorkout.restCamYTask >= 0) restCamTaskY = workoutCharacterObject.tasks[activeWorkout.restCamYTask];

        var repBaseTask = workoutCharacterObject.tasks[activeWorkout.repBaseTask < 0 ? activeWorkout.restTask : activeWorkout.repBaseTask];
        var repStruggleTask = workoutCharacterObject.tasks[activeWorkout.repStruggleTask];
        var repAddTask = workoutCharacterObject.tasks[activeWorkout.repAddTask];

        var repCamTask = workoutCharacterObject.tasks[activeWorkout.repCamBaseTask < 0 ? activeWorkout.restCamBaseTask : activeWorkout.repCamBaseTask];
        GameStateController.AnimationTask repCamTaskX = null;
        GameStateController.AnimationTask repCamTaskY = null;
        if (activeWorkout.repCamXTask >= 0) repCamTaskX = workoutCharacterObject.tasks[activeWorkout.repCamXTask]; else repCamTaskX = restCamTaskX;
        
        if (activeWorkout.repCamYTask >= 0) repCamTaskY = workoutCharacterObject.tasks[activeWorkout.repCamYTask]; else repCamTaskY = restCamTaskY;     

        var flexBaseTask = activeWorkout.flexBaseTask < 0 ? null : workoutCharacterObject.tasks[activeWorkout.flexBaseTask];
        var flexControlTask = activeWorkout.flexControlTask < 0 ? null : workoutCharacterObject.tasks[activeWorkout.flexControlTask];
        var flexCamTask = workoutCharacterObject.tasks[activeWorkout.flexCamBaseTask < 0 ? activeWorkout.restCamBaseTask : activeWorkout.flexCamBaseTask];
        GameStateController.AnimationTask flexCamTaskX = null;
        GameStateController.AnimationTask flexCamTaskY = null;
        if (activeWorkout.flexCamXTask >= 0) flexCamTaskX = workoutCharacterObject.tasks[activeWorkout.flexCamXTask]; else flexCamTaskX = restCamTaskX;
        if (activeWorkout.flexCamYTask >= 0) flexCamTaskY = workoutCharacterObject.tasks[activeWorkout.flexCamYTask]; else flexCamTaskY = restCamTaskY;

        var restToRepTask = activeWorkout.restToRepTask < 0 ? null : workoutCharacterObject.tasks[activeWorkout.restToRepTask];
        var repToRestTask = activeWorkout.repToRestTask < 0 ? null : workoutCharacterObject.tasks[activeWorkout.repToRestTask];

        var restToFlexTask = activeWorkout.restToFlexTask < 0 ? null : workoutCharacterObject.tasks[activeWorkout.restToFlexTask];
        var flexToRestTask = activeWorkout.flexToRestTask < 0 ? null : workoutCharacterObject.tasks[activeWorkout.flexToRestTask];

        var restOffsetTasks = activeWorkout.restOffsetTasks;
        var repOffsetTasks = activeWorkout.repBaseTask < 0 ? activeWorkout.restOffsetTasks : activeWorkout.repOffsetTasks;
        var flexOffsetTasks = activeWorkout.flexBaseTask < 0 ? activeWorkout.restOffsetTasks : activeWorkout.flexOffsetTasks;

        if (workoutState == WorkoutState.Resting || workoutState == WorkoutState.Flexing)
        {
            if (staminaRecoveryDelay > 0) staminaRecoveryDelay -= Time.deltaTime;
            if (staminaRecoveryDelay <= 0)
            {
                stamina = Mathf.Clamp01(stamina + activeWorkout.staminaRecoveryPerSecond * activeWorkout.staminaRecoveryRestBonusMultiplier * Time.deltaTime);
            }

            if (energyRecoveryDelay > 0) energyRecoveryDelay -= Time.deltaTime;
            if (energyRecoveryDelay <= 0)
            {
                energy = Mathf.Clamp01(energy + activeWorkout.energyRecoveryPerSecond * activeWorkout.energyRecoveryRestBonusMultiplier * Time.deltaTime);
            }
        }

        Vector2 mainCursorInput = allowInput ? new Vector2(Swole.InputProxy.CursorAxisX, Swole.InputProxy.CursorAxisY) : Vector2.zero;
        /*float mainCursorMag = mainCursorInput.magnitude;
        if (mainCursorMag > 0)
        {
            mainCursorInput = mainCursorInput / mainCursorMag;
            mainCursorMag = Mathf.Min(1, mainCursorMag);
            mainCursorInput = mainCursorInput * mainCursorMag;
        }*/
        straining = 0;
        float camDeltaX = 0;
        float camDeltaY = 0;
        float t;
        float gamepadMag;
        bool transitioning = targetWorkoutState != workoutState;
        Vector2 gamepadInput, input, cursorInput;

        switch (workoutState)
        {
            case WorkoutState.Resting:

                if (repCurveRoot.gameObject.activeSelf) 
                {
                    repCurveRoot.gameObject.SetActive(false);
                }
                /*if (weightDisplayRoot.gameObject.activeSelf)
                {
                    weightDisplayRoot.gameObject.SetActive(false);  
                }*/

                repStruggleTask.mix = 0;
                restTask.mix = 1;

                repProgress = 0;
                forceRepUpdateFlag = true;
                flexProgress = activeWorkout.flexStartPoint;

                if (allowInput)
                {
                    camDeltaX = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.x * mouseCamSensX : 0) + (Swole.InputProxy.MainRightJoystickHorizontal)) * activeWorkout.restCamSpeedX * Time.deltaTime;
                    camDeltaY = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.y * mouseCamSensY : 0) + (Swole.InputProxy.MainRightJoystickVertical)) * activeWorkout.restCamSpeedY * Time.deltaTime;

                    if (Swole.InputProxy.JumpButtonDown || ReferenceEquals(restTask, repBaseTask))
                    {
                        targetWorkoutState = WorkoutState.Active;
                        transitioning = true;
                    }
                    else if (Swole.InputProxy.MOBA_AbilityRDown)
                    {
                        SetGameState(GameState.Customization);
                        return;
                    }
                    else if (Swole.InputProxy.MOBA_AbilityWDown && flexBaseTask != null)
                    {
                        targetWorkoutState = WorkoutState.Flexing;
                        transitioning = true;
                    }
                }

                if (!pressToBegin.gameObject.activeSelf) pressToBegin.gameObject.SetActive(true);
                pressToBeginImage.sprite = Swole.InputSystemProxy.IsUsingGamepad ? gamePadSouth : keyboardSouth;
                if (pressToBeginPulse == null)
                {
                    pressToBeginPulse = LeanTween.value(0, 1, 0.35f).setEase(LeanTweenType.easeInOutExpo).setLoopPingPong(-1).setOnUpdate((float val) =>
                    {
                        if (pressToBegin == null) return;
                        float scale = 1 + val * 0.1f;
                        pressToBegin.localScale = new Vector3(scale, scale, scale);
                    });
                }

                if (transitioning)
                {
                    switch (targetWorkoutState)
                    {
                        case WorkoutState.Active:
                            pressToBegin.gameObject.SetActive(false);
                            workoutState = WorkoutState.StartingReps;
                            repCamX = math.remap(activeWorkout.repCamTransitionMappingX.x, activeWorkout.repCamTransitionMappingX.y, activeWorkout.repCamTransitionMappingX.z, activeWorkout.repCamTransitionMappingX.w, camX);
                            repCamY = math.remap(activeWorkout.repCamTransitionMappingY.x, activeWorkout.repCamTransitionMappingY.y, activeWorkout.repCamTransitionMappingY.z, activeWorkout.repCamTransitionMappingY.w, camY);

                            if (restToRepTask != null)
                            {
                                restTask.mix = 0;
                                restToRepTask.StartIfNotRunning();
                                restToRepTask.mix = 1;

                                restToRepTask.ClearListeners();
                                restToRepTask.OnComplete += () =>
                                {
                                    restToRepTask.StopIfRunning();
                                    workoutState = WorkoutState.Active;
                                    repBaseTask.StartIfNotRunning();
                                    repBaseTask.mix = 1;

                                    repBaseTask.runLate = true;
                                };
                            }
                            else if (!ReferenceEquals(restTask, repBaseTask))
                            {
                                restTask.mix = 0;
                                repBaseTask.StartIfNotRunning();
                                repBaseTask.mix = 1;
                            }
                            break;

                        case WorkoutState.Flexing:
                            pressToBegin.gameObject.SetActive(false);
                            workoutState = WorkoutState.StartingFlex;
                            flexCamX = math.remap(activeWorkout.flexCamTransitionMappingX.x, activeWorkout.flexCamTransitionMappingX.y, activeWorkout.flexCamTransitionMappingX.z, activeWorkout.flexCamTransitionMappingX.w, camX);
                            flexCamY = math.remap(activeWorkout.flexCamTransitionMappingY.x, activeWorkout.flexCamTransitionMappingY.y, activeWorkout.flexCamTransitionMappingY.z, activeWorkout.flexCamTransitionMappingY.w, camY);

                            if (restToFlexTask != null)
                            {
                                restTask.mix = 0;
                                restToFlexTask.StartIfNotRunning();
                                restToFlexTask.mix = 1;

                                restToFlexTask.ClearListeners();
                                restToFlexTask.OnComplete += () =>
                                {
                                    workoutState = WorkoutState.Flexing;

                                    flexBaseTask.StartIfNotRunning();
                                    flexBaseTask.mix = 1;
                                    flexControlTask.StartIfNotRunning();
                                    flexControlTask.mix = 1;
                                    flexControlTask.SetNormalizedAnimationTime(workoutCharacterObject.animations, flexProgress);
                                    flexBaseTask.runLate = true;
                                    flexControlTask.runLate = true; 
                                };
                            }
                            else
                            {
                                restTask.mix = 0;
                                flexBaseTask.StartIfNotRunning();
                                flexBaseTask.mix = 1;
                            }
                            break;
                    }
                }

                #region Offsets

                tempIndices.Clear();
                if (restOffsetTasks != null)
                {
                    foreach (var taskIndex in restOffsetTasks)
                    {
                        var task = workoutCharacterObject.tasks[taskIndex];
                        task.StartIfNotRunning();
                        task.mix = 1;
                        tempIndices.Add(taskIndex);
                    }
                }
                if (repOffsetTasks != null && !ReferenceEquals(restOffsetTasks, repOffsetTasks))
                {
                    foreach (var taskIndex in repOffsetTasks)
                    {
                        if (tempIndices.Contains(taskIndex)) continue;

                        var task = workoutCharacterObject.tasks[taskIndex];
                        task.StopIfRunning();
                    }
                }

                #endregion

                break;

            case WorkoutState.StartingReps:
                repProgress = 0;
                forceRepUpdateFlag = true;

                if (pressToBegin.gameObject.activeSelf) pressToBegin.gameObject.SetActive(false);

                t = 1;
                if (restToRepTask == null)
                {
                    workoutState = WorkoutState.Active;
                }
                else if (restToRepTask.HasOrWillFullyStop(Time.deltaTime) || (restToRepTask.endTime - restToRepTask.Time) < 0.01f)
                {
                    restToRepTask.StopIfRunning();
                    workoutState = WorkoutState.Active;
                    repBaseTask.StartIfNotRunning();
                    repBaseTask.ResetToStartTimes(workoutCharacterObject.animations, workoutCharacterObject.sounds);
                    repBaseTask.mix = 1;

                }
                else
                {
                    t = restToRepTask.Time / restToRepTask.endTime;

                    if (allowInput)
                    {
                        camDeltaX = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.x * mouseCamSensX : 0) + Swole.InputProxy.MainRightJoystickHorizontal) * Mathf.LerpUnclamped(activeWorkout.restCamSpeedX, activeWorkout.repCamSpeedX, t) * Time.deltaTime;
                        camDeltaY = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.y * mouseCamSensX : 0) + Swole.InputProxy.MainRightJoystickVertical) * Mathf.LerpUnclamped(activeWorkout.restCamSpeedY, activeWorkout.repCamSpeedY, t) * Time.deltaTime;
                    }

                    if (!ReferenceEquals(restCamTask, repCamTask))
                    {
                        restCamTask.mix = 1 - t;
                        repCamTask.mix = t;
                    }
                    if (!ReferenceEquals(restCamTaskX, repCamTaskX))
                    {
                        restCamTaskX.mix = 1 - t;
                        repCamTaskX.mix = t;
                    }
                    if (!ReferenceEquals(restCamTaskY, repCamTaskY))
                    {
                        restCamTaskY.mix = 1 - t;
                        repCamTaskY.mix = t;
                    }
                }

                #region Offsets

                if (ReferenceEquals(restOffsetTasks, repOffsetTasks))
                {
                    if (restOffsetTasks != null)
                    {
                        foreach (var taskIndex in restOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = 1;
                        }
                    }
                }
                else
                {
                    tempIndices.Clear();
                    if (restOffsetTasks != null)
                    {
                        foreach (var taskIndex in restOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = 1 - t;
                            tempIndices.Add(taskIndex);
                        }
                    }
                    if (repOffsetTasks != null)
                    {
                        foreach (var taskIndex in repOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = tempIndices.Contains(taskIndex) ? 1 : t;
                        }
                    }
                }

                #endregion

                break;

            case WorkoutState.StoppingReps:

                t = 1;
                if (repToRestTask == null)
                {
                    workoutState = WorkoutState.Resting;
                }
                else if (repToRestTask.HasOrWillFullyStop(Time.deltaTime) || (repToRestTask.endTime - repToRestTask.Time) < 0.01f)
                {
                    repToRestTask.StopIfRunning();
                    workoutState = WorkoutState.Resting;
                    restTask.StartIfNotRunning();
                    restTask.ResetToStartTimes(workoutCharacterObject.animations, workoutCharacterObject.sounds);
                    restTask.mix = 1;
                }
                else
                {
                    t = repToRestTask.Time / repToRestTask.endTime;
                    
                    if (allowInput)
                    {
                        camDeltaX = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.x * mouseCamSensX : 0) + (Swole.InputProxy.MainRightJoystickHorizontal)) * Mathf.LerpUnclamped(activeWorkout.repCamSpeedX, activeWorkout.restCamSpeedX, t) * Time.deltaTime;
                        camDeltaY = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.y * mouseCamSensY : 0) + (Swole.InputProxy.MainRightJoystickVertical)) * Mathf.LerpUnclamped(activeWorkout.repCamSpeedY, activeWorkout.restCamSpeedY, t) * Time.deltaTime;
                    }

                    if (!ReferenceEquals(restCamTask, repCamTask))
                    {
                        restCamTask.mix = t;
                        repCamTask.mix = 1 - t;
                    }
                    if (!ReferenceEquals(restCamTaskX, repCamTaskX))
                    {
                        restCamTaskX.mix = t;
                        repCamTaskX.mix = 1 - t;
                    }
                    if (!ReferenceEquals(restCamTaskY, repCamTaskY))
                    {
                        restCamTaskY.mix = t;
                        repCamTaskY.mix = 1 - t;
                    }
                }

                #region Offsets

                if (ReferenceEquals(restOffsetTasks, repOffsetTasks))
                {
                    if (restOffsetTasks != null)
                    {
                        foreach (var taskIndex in restOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = 1;
                        }
                    }
                }
                else
                {
                    tempIndices.Clear();
                    if (restOffsetTasks != null)
                    {
                        foreach (var taskIndex in restOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = t;
                            tempIndices.Add(taskIndex);
                        }
                    }
                    if (repOffsetTasks != null)
                    {
                        foreach (var taskIndex in repOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = tempIndices.Contains(taskIndex) ? 1 : (1 - t);
                        }
                    }
                }

                #endregion

                break;

            case WorkoutState.Active:

                if (!repCurveRoot.gameObject.activeSelf)
                {
                    repCurveRoot.gameObject.SetActive(true);
                }
                if (!weightDisplayRoot.gameObject.activeSelf)
                {
                    weightDisplayRoot.gameObject.SetActive(true);
                }

                if (allowInput)
                {
                    camDeltaX = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.x * mouseCamSensX : 0) + (Swole.InputProxy.MainRightJoystickHorizontal)) * activeWorkout.repCamSpeedX * Time.deltaTime;
                    camDeltaY = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.y * mouseCamSensY : 0) + (Swole.InputProxy.MainRightJoystickVertical)) * activeWorkout.repCamSpeedY * Time.deltaTime;

                    if (Swole.InputProxy.MOBA_AbilityRDown)
                    {
                        if (ReferenceEquals(restTask, repBaseTask))
                        {
                            SetGameState(GameState.Customization);
                            return;
                        }
                        else
                        {
                            targetWorkoutState = WorkoutState.Resting;
                            transitioning = true;
                        }
                    }
                }

                repStruggleTask.StartIfNotRunning();

                #region Offsets

                tempIndices.Clear();
                if (repOffsetTasks != null)
                {
                    foreach (var taskIndex in repOffsetTasks)
                    {
                        var task = workoutCharacterObject.tasks[taskIndex];
                        task.StartIfNotRunning();
                        task.mix = 1;
                        tempIndices.Add(taskIndex);
                    }
                }
                if (restOffsetTasks != null && !ReferenceEquals(restOffsetTasks, repOffsetTasks))
                {
                    foreach (var taskIndex in restOffsetTasks)
                    {
                        if (tempIndices.Contains(taskIndex)) continue;

                        var task = workoutCharacterObject.tasks[taskIndex];
                        task.StopIfRunning();
                    }
                }

                #endregion

                repProgress = Mathf.Clamp01(repProgress);

                float rt1;
                float rt2;
                if (repProgress + repProgressMeasurementOffset > 1)
                {
                    rt1 = repProgress - repProgressMeasurementOffset;
                    rt2 = repProgress;
                }
                else
                {
                    rt1 = repProgress;
                    rt2 = repProgress + repProgressMeasurementOffset;
                }

                float stamEng = stamina * energy;
                
                Vector2 nextProgressDir = (GetRepCurvePosition(rt2) - GetRepCurvePosition(rt1)).normalized;
                cursorInput = allowInput ? (Swole.InputProxy.FreeLookSecondaryAction ? Vector2.zero : Vector2.Scale(mainCursorInput, new Vector2(mouseRepSensX, mouseRepSensY))) : Vector2.zero;
                float cursorMag = cursorInput.magnitude;
                bool maintainInputStrength = allowInput ? Swole.InputProxy.FreeLookMainAction : false;
                if (maintainInputStrength)
                {
                    if (cursorMag > 0.0025f)
                    {
                        cursorInput = cursorInput / cursorMag;
                        rotateRepAlignmentFadeIn = Mathf.Min(1, rotateRepAlignmentFadeIn + Time.deltaTime * 2);
                        lockedCursorInput = Swole.Maths.RotateByDegrees(Vector2.zero, lockedCursorInput, ((activeWorkout.invertRepAssist ? -1 : 1) * (demoSettings.invertRepAssist ? -1 : 1)) * (Mathf.Sign(Vector2.Dot(Vector2.right, cursorInput)) * cursorMag * 2.5f * Mathf.Max(0, (rotateRepAlignmentFadeIn - 0.5f) / 0.5f)));
                        
                        float lockedMag = lockedCursorInput.magnitude;
                        Vector2 lockedDir = Vector2.zero;
                        if (lockedMag > 0) lockedDir = lockedCursorInput / lockedMag;

                        float repAlignmentABS = Mathf.Abs(adjustedRepAlignment);

                        lockedMag = Mathf.Lerp(lockedMag, 0, (1 - repAlignmentABS) * Time.deltaTime); // lose momentum with poor rep alignment
                        decayingCursorInput = lockedDir * Mathf.LerpUnclamped(lockedMag * 0.2f, lockedMag, repAlignmentABS);
                        lockedCursorInput = lockedDir * lockedMag;
                    }
                }
                else
                {
                    rotateRepAlignmentFadeIn = 0;
                    if (cursorMag > 0.0025f)
                    {
                        cursorInput = cursorInput / cursorMag;
                        decayingCursorInput = cursorInput * Mathf.Lerp(Mathf.Max(cursorMag, decayingCursorInput.magnitude), cursorMag, cursorInputDecay * Time.deltaTime);
                    }
                    else
                    {
                        decayingCursorInput = Vector2.Lerp(decayingCursorInput, cursorInput, cursorInputDecay * Time.deltaTime);
                    }

                    lockedCursorInput = decayingCursorInput;
                }
                cursorInput = decayingCursorInput;

                gamepadInput = allowInput ? new Vector2(Swole.InputProxy.MainLeftJoystickHorizontal, Swole.InputProxy.MainLeftJoystickVertical) : Vector2.zero;
                gamepadMag = gamepadInput.magnitude;
                if (gamepadMag > 0)
                {
                    gamepadInput = gamepadInput / gamepadMag;
                    gamepadMag = Mathf.Pow(gamepadMag, 2);
                    gamepadInput = gamepadInput * gamepadMag;
                }

                input = new Vector2(Mathf.Clamp(cursorInput.x + gamepadInput.x, -1, 1), Mathf.Clamp(cursorInput.y + gamepadInput.y, -1, 1)); 
                if (transitioning) input = Vector2.zero;

                inputStrength = input.magnitude;
                var nextRepAlignment = 0f;
                if (inputStrength > 0)
                {
                    input = input / inputStrength;
                    nextRepAlignment = Vector2.Dot(nextProgressDir, input); // base rep alignment
                    nextRepAlignment = Mathf.Pow(Mathf.Abs(nextRepAlignment), Mathf.LerpUnclamped(activeWorkout.repAlignmentLoadMinEXP, activeWorkout.repAlignmentLoadMaxEXP, ((1 - stamEng) * 0.5f) + ((1 - currentLiftRatio) * 0.5f))) * Mathf.Sign(nextRepAlignment); // make rep alignment more or less strict based on burn out and load
                }

                nextRepAlignment = Mathf.Pow(Mathf.Abs(nextRepAlignment), activeWorkout.repAlignmentDifficultyCurve.Evaluate(repProgress)) * Mathf.Sign(nextRepAlignment); // make rep alignment more or less strict based on where it is in the rep curve

                repAlignment = nextRepAlignment > repAlignment ? nextRepAlignment : Mathf.MoveTowards(repAlignment, nextRepAlignment, Time.deltaTime * 1.8f); 

                if (stamEng <= 0)
                {
                    noStaminaRepAlignment = repProgress < 1f ? Mathf.Min(repAlignment, noStaminaRepAlignment) : 0; 
                    repAlignment = noStaminaRepAlignment; 
                }
                else
                {
                    noStaminaRepAlignment = repAlignment;
                }
                adjustedRepAlignment = repAlignment;
                if (IsEasyRepperMode)
                {
                    if (adjustedRepAlignment < 0.85f)
                    {
                        adjustedRepAlignment = Mathf.Clamp01(Mathf.Min(0.85f, Mathf.Pow(Mathf.Abs(repAlignment) * 1.75f, 0.5f))) * Mathf.Sign(repAlignment);
                    }

                }
                repProgressAlignment = adjustedRepAlignment > 0 ? Mathf.Pow(adjustedRepAlignment, Mathf.Max(1, 1 + (Mathf.Max(0, (activeWorkout.burnOutRepCounterDifficultyExp - 1)) * repBurnOutCounter))) : adjustedRepAlignment; // nerf rep alignment each time a full burn out rep is completed
                 
                repDeltaProgress = 0;

                float maxLiftWeight = activeWorkout.CalculateMaximumLift(repProgress, energy, pumpStrengthBonusCurve, characterMuscle);
                float rawLiftRatio = currentLoad / maxLiftWeight;
                currentLiftRatio = Mathf.Clamp01(rawLiftRatio);
                delayedRawLiftRatio = rawLiftRatio < delayedRawLiftRatio ? rawLiftRatio : (repComplete || repProgress <= 0.3f ? rawLiftRatio : Mathf.Lerp(delayedRawLiftRatio, rawLiftRatio, Time.deltaTime * 0.5f)); // allows overload weight to be lifted for a moment
                float delayedCurrentLiftRatio = Mathf.Clamp01(delayedRawLiftRatio);
                if (repProgress > 0)
                {
                    repDuration += Time.deltaTime;

                    // subtract stamina & energy
                    float lossRatio = Mathf.Clamp01(repProgress / repProgressThresholdForMaxStaminaLoss);
                    lossRatio = lossRatio * delayedRawLiftRatio;

                    float loss = lossRatio * repAlignmentPunishCurve.Evaluate(Mathf.Abs(repProgressAlignment)) * Time.deltaTime;

                    stamina = Mathf.Max(0, stamina - (activeWorkout.CalculateStaminaLoss(repProgress, pumpStaminaBonusCurve, characterMuscle) * energyBurnOutBonusCurve.Evaluate(energy) * loss));
                    staminaRecoveryDelay = activeWorkout.staminaRecoveryDelay;

                    energy = Mathf.Max(0, energy - (activeWorkout.CalculateEnergyLoss(repProgress, pumpEnergyBonusCurve, characterMuscle) * burnOutEnergyBonusCurve.Evaluate(stamina) * loss));
                    energyRecoveryDelay = activeWorkout.energyRecoveryDelay;

                    stamEng = stamina * energy;

                    // subtract rep progress based on heaviness of load, amount of stamina, and rep alignment                                                                               vv nerf rep alignment when overloaded
                    float gravity = transitioning ? 1 : (activeWorkout.maxRepGravity * (1 - Mathf.Clamp01(((Mathf.Abs(repProgressAlignment) / (repProgressAlignment <= 0 ? 1 : Mathf.Max(1, 1 + Mathf.Pow(Mathf.Max(0, delayedRawLiftRatio - 1), 2)))) / activeWorkout.repAlignmentGravityStart) * (Mathf.Min(1, (inputStrength / 0.005f)) / Mathf.Max(1, delayedRawLiftRatio)))) * Mathf.Lerp(delayedCurrentLiftRatio, delayedCurrentLiftRatio + (1 - delayedCurrentLiftRatio) * 0.85f, Mathf.Pow(1 - stamEng, activeWorkout.staminaGravityFactorExp)));

                    repDeltaProgress = -gravity * activeWorkout.repSpeedMax;
                    repProgress = Mathf.Max(0, repProgress + repDeltaProgress * Time.deltaTime);
                }
                else
                {
                    repAccuracy = 0;

                    if (staminaRecoveryDelay > 0) staminaRecoveryDelay -= Time.deltaTime;
                    if (staminaRecoveryDelay <= 0)
                    {
                        stamina = Mathf.Clamp01(stamina + activeWorkout.staminaRecoveryPerSecond * Time.deltaTime);
                    }

                    if (energyRecoveryDelay > 0) energyRecoveryDelay -= Time.deltaTime;
                    if (energyRecoveryDelay <= 0)
                    {
                        energy = Mathf.Clamp01(energy + activeWorkout.energyRecoveryPerSecond * Time.deltaTime);
                    }

                    stamEng = stamina * energy;

                    if (transitioning)
                    {
                        workoutState = WorkoutState.StoppingReps;

                        camX = math.remap(activeWorkout.repCamTransitionMappingX.z, activeWorkout.repCamTransitionMappingX.w, activeWorkout.repCamTransitionMappingX.x, activeWorkout.repCamTransitionMappingX.y, repCamX);
                        camY = math.remap(activeWorkout.repCamTransitionMappingY.z, activeWorkout.repCamTransitionMappingY.w, activeWorkout.repCamTransitionMappingY.x, activeWorkout.repCamTransitionMappingY.y, repCamY);

                        repAddTask.mix = 0;
                        repStruggleTask.mix = 0;

                        if (repToRestTask != null)
                        {
                            repBaseTask.mix = 0;
                            repToRestTask.StartIfNotRunning();
                            repToRestTask.mix = 1;

                            repToRestTask.ClearListeners();
                            repToRestTask.OnComplete += () =>
                            {
                                repToRestTask.StopIfRunning();
                                workoutState = WorkoutState.Resting;
                                restTask.StartIfNotRunning();
                                restTask.mix = 1;

                                restTask.runLate = true;
                            };
                        }
                        else if (!ReferenceEquals(restTask, repBaseTask))
                        {
                            repBaseTask.mix = 0;
                            restTask.StartIfNotRunning();
                            restTask.mix = 1;
                        }
                    }
                }

                repDifficulty = rawLiftRatio + ((1 - stamEng) * 0.75f);
                if (inputStrength > 0.005f)
                {
                    inputStrength = Mathf.Clamp01((inputStrength - 0.005f) / (1 - 0.005f));

                    float repSpeed = (repAlignment < 0 ? activeWorkout.repSpeedMax : Mathf.Lerp(activeWorkout.repSpeedMin / Mathf.Pow(Mathf.Max(1, currentLoad - maxLiftWeight), 3), activeWorkout.repSpeedMax, repAlignment < 0 ? 1 : ((1 - delayedCurrentLiftRatio) * Mathf.Lerp(1, activeWorkout.repSpeedBurnOut, delayedCurrentLiftRatio >= 1 ? 0 : (1 - stamEng)))));
                        
                    repPower = adjustedRepAlignment * inputStrength * repSpeed;
                    if (repPower != 0 && repProgress > 0.01f)
                    {
                        float repGrowth = 1;
                        float growthLiftRatio = rawLiftRatio;  
                        if (repProgress >= 1)
                        {
                            growthLiftRatio = Mathf.Min(Mathf.Max(currentLiftRatio, rawLiftRatio / Mathf.Pow(1f + repHoldTime, 1.5f)), 300);   
                            repHoldTime += Time.deltaTime;
                            //Debug.Log(repHoldTime + " :::: " + Mathf.Pow(growthLiftRatio, activeWorkout.repHoldBonusDifficultyFalloff));
                            repGrowth = Mathf.Max(1, activeWorkout.repHoldGrowthBonusCurve.Evaluate(repHoldTime / activeWorkout.repMaxHoldTime) * Mathf.Pow(growthLiftRatio, activeWorkout.repHoldBonusDifficultyFalloff));

                        }
                        else 
                        {
                            growthLiftRatio = Mathf.Min(Mathf.LerpUnclamped(currentLiftRatio, repComplete ? rawLiftRatio : Mathf.Lerp(currentLiftRatio, rawLiftRatio, 0.3f), Mathf.Pow(repProgress, 2f)), 5); 
                            if (repComplete) repHoldTime = repProgress < 0.99f ? activeWorkout.repMaxHoldTime : repHoldTime; else repHoldTime = 0; 
                        }

                        //Debug.Log($"{activeWorkout.burnOutGrowthMultiplier} : {Mathf.Pow(1 - Mathf.Lerp(1, stamEng, Mathf.Max(0, repAlignment) * growthLiftRatio), activeWorkout.burnOutGrowthBonusFalloff) * repProgress} : {Mathf.LerpUnclamped(1, activeWorkout.burnOutGrowthMultiplier, Mathf.Pow(1 - Mathf.Lerp(1, stamEng, Mathf.Max(0, repAlignment) * growthLiftRatio), activeWorkout.burnOutGrowthBonusFalloff) * repProgress)}");
                        repGrowth = repGrowth * growthLiftRatio * activeWorkout.growthRate * Mathf.LerpUnclamped(1, activeWorkout.burnOutGrowthMultiplier, Mathf.Pow(1 - Mathf.Lerp(1, stamEng, Mathf.Max(0, repAlignment) * growthLiftRatio), activeWorkout.burnOutGrowthBonusFalloff) * repProgress) * Mathf.Abs(repAlignment); // include rep progress for burnout multiplier so it isn't being fully applied early when rep accuracy is high

                        if (repComplete) repGrowth = repGrowth + (repGrowth * (activeWorkout.eccentricGrowthMultiplier * (1 - repProgress))); // grow faster during eccentric phase the closer you are to ending the rep

                        float repComboMul = activeWorkout.repComboGrowthMultiplier * rawLiftRatio * repCombo * 0.5f;
                        float fullBurnOutMul = activeWorkout.burnOutRepCounterGrowthMultiplier * rawLiftRatio * repBurnOutCounter * 0.1f;

                        repGrowth = repGrowth + (repGrowth * repComboMul) + (repGrowth * fullBurnOutMul);

                        repGrowth = Mathf.Max(0, repGrowth * demoSettings.growthRate);   
                        
                        float pumpBaseAdd = (Time.deltaTime * currentLiftRatio * 0.25f);

                        float mainMuscleSizeL = 0f;
                        float mainMuscleSizeR = 0f;

                        if (characterMuscle != null)
                        {
                            if (activeWorkout.mainMuscleL >= 0) mainMuscleSizeL = characterMuscle.GetMuscleGroupMassUnsafe(activeWorkout.targetMuscles[activeWorkout.mainMuscleL].GetMuscleIndex(characterMuscle));
                            if (activeWorkout.mainMuscleR >= 0) mainMuscleSizeR = characterMuscle.GetMuscleGroupMassUnsafe(activeWorkout.targetMuscles[activeWorkout.mainMuscleR].GetMuscleIndex(characterMuscle));
                        }
                        float mainMuscleSizeAverage = (mainMuscleSizeL + mainMuscleSizeR) * 0.5f;

                        if (characterMuscle != null)
                        {
                            foreach (var muscle in activeWorkout.targetMuscles)
                            {
                                int index = muscle.GetMuscleIndex(characterMuscle);

                                float muscleSize = characterMuscle.GetMuscleGroupMass(index);
                                //muscleSize = muscleSize + GetMassScaledGrowth(muscleSize, muscle.GetGrowthContribution(repProgress) * repGrowth) * Time.deltaTime; 
                                //characterMuscle.SetMuscleGroupMassUnsafe(index, muscleSize);
                                muscle.growthAccumulation += GetMassScaledGrowth(muscleSize, muscle.GetGrowthContribution(repProgress) * repGrowth) * Time.deltaTime;

                                if (repAlignment > 0 && repProgress > 0.1f) // only add pump when progressing into the rep
                                {
                                    float mixedMuscleSize = muscleSize;
                                    if (muscle.mainMusclePumpMix > 0)
                                    {
                                        switch (muscle.sideOfBody)
                                        {
                                            case SideOfBody.Both:
                                                mixedMuscleSize = Mathf.LerpUnclamped(muscleSize, mainMuscleSizeAverage, muscle.mainMusclePumpMix);
                                                break;

                                            case SideOfBody.Left:
                                                mixedMuscleSize = Mathf.LerpUnclamped(muscleSize, mainMuscleSizeL, muscle.mainMusclePumpMix);
                                                break;

                                            case SideOfBody.Right:
                                                mixedMuscleSize = Mathf.LerpUnclamped(muscleSize, mainMuscleSizeR, muscle.mainMusclePumpMix);
                                                break;
                                        }
                                    }

                                    float pumpAdd = pumpBaseAdd * Mathf.LerpUnclamped(0.1f, 3, mixedMuscleSize) * Mathf.LerpUnclamped(pumpEmptyStaminaBonus, 1, Mathf.Pow(stamEng, 0.5f)) * muscle.GetPumpContribution(repProgress);
                                    AddScaledPump(pumpAdd, index, muscleSize, mixedMuscleSize);
                                }
                            }
                        }
                    }

                    if (stamina < activeWorkout.repStartPreventionStaminaThreshold && repProgress <= 0.001f) repPower = 0; // stop rep from starting if burn out is too high

                    if (repComplete && repPower > 0) // eccentric phase rep curve behaviour
                    {
                        float repPowerNormalized = (repPower / repSpeed);
                        float stam = Mathf.Min(1, stamina / 0.3f); 
                        var repEaseDown = Mathf.LerpUnclamped(Mathf.LerpUnclamped(repSpeed, activeWorkout.repSpeedMax, 0.2f), repSpeed, stam) * (1 - Mathf.LerpUnclamped(repPowerNormalized * 0.9f, Mathf.Min(1, repPowerNormalized / 0.7f), stam));
                        repPower = Mathf.Min(repPower, -repDeltaProgress); // resist gravity, but no more than that
                        repPower = repPower - repEaseDown;  
                    }

                    repProgress = Mathf.Clamp01(repProgress + (repPower * Time.deltaTime));

                    repDeltaProgress += repPower;
                    repMomentum = repDeltaProgress;
                    repDeltaProgress = repMomentum * Time.deltaTime;

                    if (repAlignment > 0)
                    {
                        straining = math.remap(activeWorkout.repSpeedMin, activeWorkout.repSpeedMax, 1, 0, repSpeed);
                        straining = Mathf.Max(0, straining - 0.75f) / 0.25f;
                        straining = straining * Math.Min(1, repProgress / 0.75f);
                        straining = Mathf.Pow(straining, 2);

                        if (Swole.InputSystemProxy.IsUsingGamepad && allowInput) Swole.InputProxy.RumbleMainGamepad(straining * currentLiftRatio, straining * (1 - currentLiftRatio), 0.2f, true, 0.1f);
                    }
                    else straining = 0;

                    repKnob.localRotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, input));
                }

                //Debug.Log(currentLiftRatio + " : " + repDifficulty); 
                if (prevRepProgress != repProgress || forceRepUpdateFlag)
                {
                    forceRepUpdateFlag = false;
                    
                    if (repProgress >= 1)
                    {
                        if (!repComplete)
                        {
                            repComplete = true;
                            CompleteRep(activeWorkout, repAccuracy, rawLiftRatio);    
                        }
                        repAccuracy = 0;
                    }
                    else if (repProgress <= 0)
                    {
                        if (repComplete)
                        {
                            repComplete = false;
                        }
                        repAccuracy = 0;
                        repDuration = 0;
                        repHoldTime = 0;
                    }
                    else
                    {
                        float repProgressDelta = (repProgress - prevRepProgress);
                        if (repProgressDelta < 0) progressLossTime += Time.deltaTime; else progressLossTime = 0;
                        //                                                                            v make losing rep progress more punishing 
                        repAccuracy = repAccuracy + Mathf.Lerp(repProgressDelta, repProgressDelta < 0 ? repProgressDelta * 1.25f : ((repAlignment <= 0 ? 0 : Mathf.Pow(repAlignment, 2)) * repProgressDelta), repProgress / 0.25f); // make early part of rep less punishing to accuracy
                        if (!unsteadyRepFlag && !repComplete && repProgressDelta < 0 && repProgress > 0.1f && progressLossTime > 0.2f) 
                        {
                            unsteadyRepFlag = true;
                            ResetRepCombo();
                        }
                    } 

                    repAddTask.StartIfNotRunning();
                    repAddTask.SetAnimationTime(workoutCharacterObject.animations, repProgress * repAddTask.endTime);

                    repAddTask.mix = 1;
                    
                    RebuildRepCurves();
                }

                repStruggleTask.mix = Mathf.LerpUnclamped(repStruggleTask.mix, ((straining * 0.65f) + (currentLiftRatio * Mathf.Min(1, repProgress / 0.25f) * 0.35f)) * activeWorkout.repStruggleMix, Time.deltaTime * 5);

                break;

            case WorkoutState.StartingFlex:

                if (pressToBegin.gameObject.activeSelf) pressToBegin.gameObject.SetActive(false);

                flexProgress = activeWorkout.flexStartPoint; 

                t = 1;
                if (restToFlexTask == null)
                {
                    workoutState = WorkoutState.Flexing;
                }
                else if (restToFlexTask.HasOrWillFullyStop(Time.deltaTime) || (restToFlexTask.endTime - restToFlexTask.Time) < 0.01f)
                {
                    restToFlexTask.StopIfRunning();
                    workoutState = WorkoutState.Flexing;

                    flexBaseTask.StartIfNotRunning();
                    flexBaseTask.ResetToStartTimes(workoutCharacterObject.animations, workoutCharacterObject.sounds);
                    flexBaseTask.mix = 1;
                    if (flexControlTask != null)
                    {
                        flexControlTask.StartIfNotRunning();
                        flexControlTask.mix = 1;
                        flexControlTask.SetNormalizedAnimationTime(workoutCharacterObject.animations, flexProgress);
                    }
                }
                else
                {
                    t = restToFlexTask.Time / restToFlexTask.endTime;

                    if (allowInput)
                    {
                        camDeltaX = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.x * mouseCamSensX : 0) + Swole.InputProxy.MainRightJoystickHorizontal) * Mathf.LerpUnclamped(activeWorkout.restCamSpeedX, activeWorkout.flexCamSpeedX, t) * Time.deltaTime;
                        camDeltaY = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.y * mouseCamSensY : 0) + Swole.InputProxy.MainRightJoystickVertical) * Mathf.LerpUnclamped(activeWorkout.restCamSpeedY, activeWorkout.flexCamSpeedY, t) * Time.deltaTime;
                    }

                    if (!ReferenceEquals(restCamTask, flexCamTask))
                    {
                        restCamTask.mix = 1 - t;
                        flexCamTask.mix = t;
                    }
                    if (!ReferenceEquals(restCamTaskX, flexCamTaskX))
                    {
                        restCamTaskX.mix = 1 - t;
                        flexCamTaskX.mix = t;
                    }
                    if (!ReferenceEquals(restCamTaskY, flexCamTaskY))
                    {
                        restCamTaskY.mix = 1 - t;
                        flexCamTaskY.mix = t;
                    }
                }

                #region Offsets

                if (ReferenceEquals(restOffsetTasks, flexOffsetTasks))
                {
                    if (restOffsetTasks != null)
                    {
                        foreach (var taskIndex in restOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = 1;
                        }
                    }
                }
                else
                {
                    tempIndices.Clear();
                    if (restOffsetTasks != null)
                    {
                        foreach (var taskIndex in restOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex]; 
                            task.StartIfNotRunning();
                            task.mix = 1 - t;
                            tempIndices.Add(taskIndex);
                        }
                    }
                    if (flexOffsetTasks != null)
                    {
                        foreach (var taskIndex in flexOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = tempIndices.Contains(taskIndex) ? 1 : t;
                        }
                    }
                }

                #endregion

                break;

            case WorkoutState.StoppingFlex:

                t = 1;
                if (flexToRestTask == null)
                {
                    workoutState = WorkoutState.Resting; 
                }
                else if (flexToRestTask.HasOrWillFullyStop(Time.deltaTime) || (flexToRestTask.endTime - flexToRestTask.Time) < 0.01f)
                {
                    flexToRestTask.StopIfRunning();
                    workoutState = WorkoutState.Resting;
                    restTask.StartIfNotRunning(); 
                    restTask.ResetToStartTimes(workoutCharacterObject.animations, workoutCharacterObject.sounds);
                    restTask.mix = 1;
                }
                else
                {
                    t = flexToRestTask.Time / flexToRestTask.endTime;

                    if (allowInput)
                    {
                        camDeltaX = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.x * mouseCamSensX : 0) + (Swole.InputProxy.MainRightJoystickHorizontal)) * Mathf.LerpUnclamped(activeWorkout.flexCamSpeedX, activeWorkout.restCamSpeedX, t) * Time.deltaTime;
                        camDeltaY = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.y * mouseCamSensY : 0) + (Swole.InputProxy.MainRightJoystickVertical)) * Mathf.LerpUnclamped(activeWorkout.flexCamSpeedY, activeWorkout.restCamSpeedY, t) * Time.deltaTime;
                    }

                    if (!ReferenceEquals(restCamTask, flexCamTask))
                    {
                        restCamTask.mix = t;
                        flexCamTask.mix = 1 - t;
                    }
                    if (!ReferenceEquals(restCamTaskX, flexCamTaskX))
                    {
                        restCamTaskX.mix = t;
                        flexCamTaskX.mix = 1 - t;
                    }
                    if (!ReferenceEquals(restCamTaskY, flexCamTaskY))
                    {
                        restCamTaskY.mix = t;
                        flexCamTaskY.mix = 1 - t;
                    }
                }

                #region Offsets

                if (ReferenceEquals(restOffsetTasks, flexOffsetTasks))
                {
                    if (restOffsetTasks != null)
                    {
                        foreach (var taskIndex in restOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = 1;
                        }
                    }
                }
                else
                {
                    tempIndices.Clear();
                    if (restOffsetTasks != null)
                    {
                        foreach (var taskIndex in restOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = t;
                            tempIndices.Add(taskIndex);
                        }
                    }
                    if (flexOffsetTasks != null)
                    {
                        foreach (var taskIndex in flexOffsetTasks)
                        {
                            var task = workoutCharacterObject.tasks[taskIndex];
                            task.StartIfNotRunning();
                            task.mix = tempIndices.Contains(taskIndex) ? 1 : (1 - t);
                        }
                    }
                }

                #endregion

                break;

            case WorkoutState.Flexing:

                if (allowInput)
                {
                    camDeltaX = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.x * mouseCamSensX : 0) + (Swole.InputProxy.MainRightJoystickHorizontal)) * activeWorkout.flexCamSpeedX * Time.deltaTime;
                    camDeltaY = ((Swole.InputProxy.FreeLookSecondaryAction ? mainCursorInput.y * mouseCamSensY : 0) + (Swole.InputProxy.MainRightJoystickVertical)) * activeWorkout.flexCamSpeedY * Time.deltaTime;

                    if (Swole.InputProxy.MOBA_AbilityRDown)
                    {
                        targetWorkoutState = WorkoutState.Resting;
                        transitioning = true;
                    }
                }

                #region Offsets

                tempIndices.Clear();
                if (flexOffsetTasks != null)
                {
                    foreach (var taskIndex in flexOffsetTasks)
                    {
                        var task = workoutCharacterObject.tasks[taskIndex];
                        task.StartIfNotRunning();
                        task.mix = 1;
                        tempIndices.Add(taskIndex);
                    }
                }
                if (restOffsetTasks != null && !ReferenceEquals(restOffsetTasks, flexOffsetTasks))
                {
                    foreach (var taskIndex in restOffsetTasks)
                    {
                        if (tempIndices.Contains(taskIndex)) continue;

                        var task = workoutCharacterObject.tasks[taskIndex];
                        task.StopIfRunning();
                    }
                }

                #endregion

                if (transitioning)
                {
                    if (Mathf.Abs(flexProgress) < 0.01)
                    {
                        workoutState = WorkoutState.StoppingFlex;
                        camX = math.remap(activeWorkout.flexCamTransitionMappingX.z, activeWorkout.flexCamTransitionMappingX.w, activeWorkout.flexCamTransitionMappingX.x, activeWorkout.flexCamTransitionMappingX.y, flexCamX);
                        camY = math.remap(activeWorkout.flexCamTransitionMappingY.z, activeWorkout.flexCamTransitionMappingY.w, activeWorkout.flexCamTransitionMappingY.x, activeWorkout.flexCamTransitionMappingY.y, flexCamY);

                        if (flexControlTask != null) flexControlTask.mix = 0;                    

                        if (flexToRestTask != null)
                        {
                            flexBaseTask.mix = 0;
                            flexToRestTask.StartIfNotRunning();
                            flexToRestTask.mix = 1;
                            
                            flexToRestTask.ClearListeners();
                            flexToRestTask.OnComplete += () =>
                            {
                                flexToRestTask.StopIfRunning();
                                workoutState = WorkoutState.Resting;
                                restTask.StartIfNotRunning();
                                restTask.mix = 1;

                                restTask.runLate = true;
                            };
                        }
                        else
                        {
                            flexBaseTask.mix = 0;
                            restTask.StartIfNotRunning();
                            restTask.mix = 1;
                        }
                    }
                    else
                    {
                        float diff = activeWorkout.flexStartPoint - flexProgress;
                        float move = Mathf.Min(Time.deltaTime * 2, Mathf.Abs(diff));
                        flexProgress = flexProgress + move * Mathf.Sign(diff);
                    }
                }
                else if (flexControlTask != null)
                {
                    cursorInput = Swole.InputProxy.FreeLookSecondaryAction || !allowInput ? Vector2.zero : Vector2.Scale(mainCursorInput, new Vector2(mouseRepSensX, mouseRepSensY));
                    gamepadInput = allowInput ? new Vector2(Swole.InputProxy.MainLeftJoystickHorizontal, Swole.InputProxy.MainLeftJoystickVertical) : Vector2.zero;
                    gamepadMag = gamepadInput.magnitude;
                    if (gamepadMag > 0)
                    {
                        gamepadInput = gamepadInput / gamepadMag;
                        gamepadMag = Mathf.Pow(gamepadMag, 2);
                        gamepadInput = gamepadInput * gamepadMag;
                    }

                    input = new Vector2(Mathf.Clamp(cursorInput.x + gamepadInput.x, -1, 1), Mathf.Clamp(cursorInput.y + gamepadInput.y, -1, 1));
                    float mag = input.magnitude;
                    float flexAlignment = 0;
                    if (mag > 0)
                    {
                        input = input / mag;
                        flexAlignment = Vector2.Dot(input, activeWorkout.flexInputAxis);
                        input = input * Mathf.Min(1, mag);
                    }

                    float flexHardMinThreshold = activeWorkout.flexStartPoint * (1 - activeWorkout.flexHardStartThreshold);
                    float flexHardMaxThreshold = ((1 - activeWorkout.flexStartPoint) * activeWorkout.flexHardStartThreshold) + activeWorkout.flexStartPoint;

                    float flexSpeed = activeWorkout.flexProgressSpeed * Time.deltaTime;
                    float flexAdd = flexAlignment < 0 ?
                        ((flexProgress > flexHardMinThreshold ? Mathf.Min(flexSpeed, flexProgress - flexHardMinThreshold) : 0) + (Swole.InputProxy.JumpButtonDown ? (activeWorkout.flexHardInputAddRatio * flexHardMinThreshold) : 0)) :
                        (flexProgress < flexHardMaxThreshold ? Mathf.Min(flexSpeed, flexHardMaxThreshold - flexProgress) : 0) + (Swole.InputProxy.JumpButtonDown ? (activeWorkout.flexHardInputAddRatio * (1 - flexHardMaxThreshold)) : 0);
                    float flexDecay = Mathf.Pow(flexAlignment < 0 ? Mathf.Max(0, flexHardMinThreshold - flexProgress) : Mathf.Max(0, flexProgress - flexHardMaxThreshold), activeWorkout.flexHardDecayEXP) * activeWorkout.flexHardDecay * Time.deltaTime;

                    float flexMove = (flexAdd * flexAlignment) - flexDecay;
                    if (flexAlignment < 0 && flexProgress >= flexHardMinThreshold && flexProgress + flexMove < flexHardMinThreshold) flexMove = (flexHardMinThreshold - flexProgress) - (0.03f * flexHardMinThreshold);
                    if (flexAlignment > 0 && flexProgress <= flexHardMaxThreshold && flexProgress + flexMove > flexHardMaxThreshold) flexMove = (flexHardMaxThreshold - flexProgress) + (0.03f * (1 - flexHardMaxThreshold));

                    flexProgress = flexProgress + flexMove;
                    repProgress = Mathf.Clamp01(flexAlignment < 0 ? (flexHardMinThreshold > 0 ? (Mathf.Max(0, flexHardMinThreshold - flexProgress) / flexHardMinThreshold) : 0) : ((1 - flexHardMaxThreshold) > 0 ? (Mathf.Max(0, flexProgress - flexHardMaxThreshold) / (1 - flexHardMaxThreshold)) : 0));
                    adjustedRepAlignment = repAlignment = inputStrength = Mathf.Sign(Mathf.Sign(flexMove) + Mathf.Sign(flexAlignment));
                    repPower = Mathf.Abs(flexMove) * repAlignment;
                    currentLiftRatio = rawLiftRatio = repDifficulty = 0.7f;// + (1.5f * );
                    repComplete = false;

                    float averageMuscleSize = 0;
                    if (activeWorkout.flexLocalTargetMuscles != null && activeWorkout.targetMuscles != null && characterMuscle != null)
                    {
                        float totalWeight = 0;
                        foreach (var flexMuscle in activeWorkout.flexLocalTargetMuscles)
                        {
                            var muscle = activeWorkout.targetMuscles[flexMuscle];
                            var muscleIndex = muscle.GetMuscleIndex(characterMuscle);
                            totalWeight += muscle.liftRatio;
                            averageMuscleSize += characterMuscle.GetMuscleGroupMassUnsafe(muscleIndex) * muscle.liftRatio;
                        }

                        if (totalWeight > 0) averageMuscleSize = averageMuscleSize / totalWeight;

                        if (repProgress > 0)
                        {
                            float pumpAdd = repProgress * 0.3f * averageMuscleSize * Time.deltaTime;
                            foreach (var flexMuscle in activeWorkout.flexLocalTargetMuscles)
                            {
                                var muscle = activeWorkout.targetMuscles[flexMuscle];
                                var muscleIndex = muscle.GetMuscleIndex(characterMuscle);

                                //characterPump.pumpDecayDelay = 1;
                                AddScaledPump(pumpAdd * muscle.pumpRatio, muscleIndex, characterMuscle.GetMuscleGroupMassUnsafe(muscleIndex), averageMuscleSize);
                            }
                        }
                    }

                    // rumble gamepad based on muscle size
                    if (Swole.InputSystemProxy.IsUsingGamepad && repProgress > 0 && allowInput) Swole.InputProxy.RumbleMainGamepad(repProgress * averageMuscleSize, repProgress * (1 - averageMuscleSize), 0.2f, true, 0.1f);
                }
                
                flexProgress = Mathf.Clamp01(flexProgress);
                if (flexControlTask != null)
                {
                    flexControlTask.StartIfNotRunning();
                    flexControlTask.mix = flexBaseTask.mix;
                    flexControlTask.SetNormalizedAnimationTime(workoutCharacterObject.animations, math.remap(0, 1, activeWorkout.flexRepProgressMapping.x, activeWorkout.flexRepProgressMapping.y, flexProgress));
                }

                break;
        }

        camX = activeWorkout.restCamAllowWrapX ? Swole.Maths.wrap(camX + camDeltaX, 1) : Mathf.Clamp01(camX + camDeltaX);
        camY = activeWorkout.restCamAllowWrapY ? Swole.Maths.wrap(camY + camDeltaY, 1) : Mathf.Clamp01(camY + camDeltaY);
        
        repCamX = activeWorkout.repCamAllowWrapX ? Swole.Maths.wrap(repCamX + camDeltaX, 1) : Mathf.Clamp01(repCamX + camDeltaX); 
        repCamY = activeWorkout.repCamAllowWrapY ? Swole.Maths.wrap(repCamY + camDeltaY, 1) : Mathf.Clamp01(repCamY + camDeltaY);

        flexCamX = activeWorkout.flexCamAllowWrapX ? Swole.Maths.wrap(flexCamX + camDeltaX, 1) : Mathf.Clamp01(flexCamX + camDeltaX);
        flexCamY = activeWorkout.flexCamAllowWrapY ? Swole.Maths.wrap(flexCamY + camDeltaY, 1) : Mathf.Clamp01(flexCamY + camDeltaY);  
         
        if (restCamTaskX != null) restCamTaskX.SetNormalizedAnimationTime(workoutCharacterObject.animations, camX);     
        if (restCamTaskY != null) restCamTaskY.SetNormalizedAnimationTime(workoutCharacterObject.animations, camY);       
        
        if (repCamTaskX != null && !ReferenceEquals(repCamTaskX, restCamTaskX)) repCamTaskX.SetNormalizedAnimationTime(workoutCharacterObject.animations, repCamX);       
        if (repCamTaskY != null && !ReferenceEquals(repCamTaskY, restCamTaskY)) repCamTaskY.SetNormalizedAnimationTime(workoutCharacterObject.animations, repCamY);

        if (flexCamTaskX != null && !ReferenceEquals(flexCamTaskX, restCamTaskX)) flexCamTaskX.SetNormalizedAnimationTime(workoutCharacterObject.animations, flexCamX);
        if (flexCamTaskY != null && !ReferenceEquals(flexCamTaskY, restCamTaskY)) flexCamTaskY.SetNormalizedAnimationTime(workoutCharacterObject.animations, flexCamY); 


        if (repCurvePoints.Count > 0)
        {
            repKnob.position = GetRepCurvePosition(repProgress);
            repAlignmentColor = Color.Lerp(repAlignmentColor, Color.Lerp(Color.red, Color.Lerp(Color.yellow, Color.green, Mathf.Max(0, Mathf.Abs(repProgressAlignment) - 0.5f) / 0.5f), Mathf.Abs(repProgressAlignment)), Time.deltaTime * 15);
            repKnobImage.color = repAlignmentColor;
            repCurveRenderers[repCurveFillRenderer].SetLineColor(Color.Lerp(Color.Lerp(lightWeightColor, mediumWeightColor, currentLiftRatio / 0.8f), heavyWeightColor, Mathf.Max(0, currentLiftRatio - 0.8f) / 0.2f));
        }

        burnOutNeedle.localRotation = Quaternion.Euler(0, 0, (burnOutNeedle.localRotation.eulerAngles.z + (Swole.Maths.GetMinDeltaAngle(burnOutNeedle.localRotation.eulerAngles.z, Mathf.LerpUnclamped(burnOutNeedleMaxRot, burnOutNeedleMinRot, stamina)) * Time.deltaTime * 8)) + (UnityEngine.Random.Range(-0.3f, 0.3f) * Mathf.Max(0, ((1 - stamina) - 0.5f) / 0.5f)) + (UnityEngine.Random.Range(-3, 3) * straining));
        energyNeedle.localRotation = Quaternion.Euler(0, 0, (energyNeedle.localRotation.eulerAngles.z + (Swole.Maths.GetMinDeltaAngle(energyNeedle.localRotation.eulerAngles.z, Mathf.LerpUnclamped(energyNeedleMinRot, energyNeedleMaxRot, energy)) * Time.deltaTime * 8)));

        if (1 - stamina < Mathf.LerpUnclamped(activeWorkout.burnOutRepCounterResetThresholdMin, activeWorkout.burnOutRepCounterResetThresholdMax, Mathf.Min(1, repBurnOutCounter / 10f))) ResetRepBurnOutCounter();   
         
        var breatheLightTask = activeWorkout.breatheLightTask < 0 ? dummyTask : workoutCharacterObject.tasks[activeWorkout.breatheLightTask];
        var breatheMedTask = activeWorkout.breatheMedTask < 0 ? dummyTask : workoutCharacterObject.tasks[activeWorkout.breatheMedTask];
        var breatheHeavyTask = activeWorkout.breatheHeavyTask < 0 ? dummyTask : workoutCharacterObject.tasks[activeWorkout.breatheHeavyTask]; 
        
        breatheLightTask.syncAnimationTimeWithTaskTime = true;
        breatheMedTask.syncAnimationTimeWithTaskTime = true;
        breatheHeavyTask.syncAnimationTimeWithTaskTime = true; 

        float breathingSpeed = breatheLightTask.speed = 1 + (1 - Mathf.Min(1, stamina / 0.6f)) * 0.5f; 
        breatheLightTask.speed = breathingSpeed;
        breatheMedTask.speed = breathingSpeed;
        breatheHeavyTask.speed = breathingSpeed;

        bool cancelBreathing = false;
        for(int a = 0; a < breathCancellingTasks.Length; a++)
        {
            var task = workoutCharacterObject.tasks[breathCancellingTasks[a]];
            if (!task.IsRunning) continue;

            cancelBreathing = true;
            break;
        }
        
        bool cancelBreathingSounds = cancelBreathing;
        if (!cancelBreathingSounds)
        {
            for (int a = 0; a < breathSoundCancellingTasks.Length; a++)
            {
                var task = workoutCharacterObject.tasks[breathSoundCancellingTasks[a]];
                if (!task.IsRunning) continue;

                cancelBreathingSounds = true;
                break;
            }
        }

        if (cancelBreathingSounds)
        {
            breatheLightTask.soundVolume = 0;
            breatheMedTask.soundVolume = 0;
            breatheHeavyTask.soundVolume = 0;
        } 
        else
        {
            breatheLightTask.soundVolume = 1;
            breatheMedTask.soundVolume = 1;
            breatheHeavyTask.soundVolume = 1;
        }

        if (cancelBreathing || energy > 0.45f)
        {
            breatheLightTask.StopIfRunning();
            breatheMedTask.StopIfRunning();
            breatheHeavyTask.StopIfRunning();

            breatheLightTask.mix = 0;
            breatheMedTask.mix = 0;
            breatheHeavyTask.mix = 0;
        }
        else if (energy > 0.3f)
        {
            breatheLightTask.StartIfNotRunning();
            breatheMedTask.StopIfRunning();
            breatheHeavyTask.StopIfRunning(); 
            breatheLightTask.mix = 1 - ((energy - 0.3f) / 0.15f); 
            breatheMedTask.mix = 0;
            breatheHeavyTask.mix = 0;
        }
        else if (energy > 0.15f)
        {
            float mix = ((energy - 0.15f) / 0.15f);

            breatheLightTask.StartIfNotRunning();
            breatheMedTask.StartIfNotRunning();
            breatheHeavyTask.StopIfRunning();

            breatheMedTask.Time = breatheLightTask.Time;

            breatheLightTask.mix = mix;
            breatheMedTask.mix = 1 - mix;
            breatheHeavyTask.mix = 0;
        }
        else
        {
            float mix = Mathf.Pow(energy / 0.15f, 0.35f);

            mix = Mathf.Min(1, mix / 0.65f);

            breatheMedTask.StartIfNotRunning();
            if (mix < 1) breatheHeavyTask.StartIfNotRunning(); else breatheHeavyTask.StopIfRunning(); 

            breatheHeavyTask.Time = breatheMedTask.Time;

            breatheLightTask.mix = 0;
            breatheMedTask.mix = mix;
            breatheHeavyTask.mix = 1 - mix;
        }
    }
    public void LateUpdateWorkouts()
    {
        float stamEng = stamina * energy;

        var activeWorkout = workouts[targetWorkout];

        switch (workoutState)
        {
            case WorkoutState.Resting:
                break;

            case WorkoutState.StartingReps:
                break;

            case WorkoutState.StoppingReps:
                break;

            case WorkoutState.Active:
                currentFlexRatio = math.lerp(currentFlexRatio, math.remap(0, 1, 0.65f, 1.25f, math.lerp(currentLiftRatio, 1, (1 - stamEng) * 0.75f)), Time.deltaTime);
                if (characterMuscle != null)
                {
                    foreach (var muscle in activeWorkout.targetMuscles)
                    {
                        var index = muscle.GetMuscleIndex(characterMuscle);
                        characterMuscle.SetMuscleGroupFlexUnsafe(index, characterMuscle.GetMuscleGroupFlexUnsafe(index) * currentFlexRatio, false, false);
                        if (characterMuscle is MuscularRenderedCharacter mrc) mrc.UpdateMuscleGroupMaterialPropertiesUnsafe(index);
                    }
                }
                break;
        }

        if (flexProxy != null)
        {
            flexProxy.trembleHead = Mathf.Max(flexProxy.trembleHead, straining * activeWorkout.headTrembleMax);
            flexProxy.trembleLeftArm = Mathf.Max(flexProxy.trembleLeftArm, straining * activeWorkout.leftArmTrembleMax);
            flexProxy.trembleRightArm = Mathf.Max(flexProxy.trembleRightArm, straining * activeWorkout.rightArmTrembleMax);
            flexProxy.trembleLeftLeg = Mathf.Max(flexProxy.trembleLeftLeg, straining * activeWorkout.leftLegTrembleMax);
            flexProxy.trembleRightLeg = Mathf.Max(flexProxy.trembleRightLeg, straining * activeWorkout.rightLegTrembleMax);
        }

        float growthTransferFrame = Mathf.Clamp01(Time.deltaTime * growthTransferRate);
        if (characterMuscle != null && characterPump != null)
        {
            foreach (var muscle in activeWorkout.targetMuscles)
            {
                var index = muscle.GetMuscleIndex(characterMuscle);
                muscle.currentMass = characterMuscle.GetMuscleGroupMassUnsafe(index);
                muscle.currentPump = characterPump.GetPumpUnsafe(index);
                muscle.currentFlex = characterMuscle.GetMuscleGroupFlexUnsafe(index);

                float growthTransfer = muscle.growthAccumulation * growthTransferFrame;
                if (growthTransfer > 0)
                {
                    muscle.growthAccumulation -= growthTransfer;
                    characterMuscle.SetMuscleGroupMassUnsafe(index, characterMuscle.GetMuscleGroupMassUnsafe(index) + growthTransfer);
                }
            }
        }

        haultActionChanges = false;
        if (workoutChanceAnimationTasks != null)
        {
            for (int a = workoutChanceAnimationTasks.Length - 1; a >= 0; a--)
            {
                var chanceTask = workoutChanceAnimationTasks[a]; 
                chanceTask.Game = this;

                var localTask = workoutCharacterObject.tasks[chanceTask.taskIndex];
                bool hasStopped = localTask.HasFullyStopped;

                if (chanceTask.immuneToHaulting || !hasStopped || !haultActionChanges)
                {
                    chanceTask.Check(workoutCharacterObject.tasks, workoutChanceAnimationTasks, characterMuscle);
                    if (chanceTask.haultActionChanges)
                    {
                        haultActionChanges = !hasStopped;
                    }
                }
            }
        }
    }

    #endregion

    private GameStateController gameStateController;

    private List<int> tempIds = new List<int>();
    public struct InjectableWorkout
    {
        public Swole.ContentPackage sourcePackage;
        public workoutInjector injector;
    }
    public static InjectableWorkout[] workoutInjectors;
    protected virtual void Awake()
    {

        if (workoutInjectors != null)
        {
            foreach(var injectable in workoutInjectors)
            {
                if (injectable.injector == null) continue;
                injectable.injector.Inject(injectable.sourcePackage, this);
            }
        } 

        var settings = Settings;
        try
        {
            var oldVersion = Version.Parse(settings.version);
            var resetVersion = Version.Parse(lastBuildReset);
            var progressResetVersion = Version.Parse(lastBuildProgressReset);
            var currentVersion = Version.Parse(Application.version);

            bool save = false;
            if (oldVersion <= resetVersion) 
            { 
                settings = DemoSettings.Default;
                save = true;
            }

            if (oldVersion <= progressResetVersion)
            {
                if (LoadProgress(out var existingData))
                {
                    existingData.muscleStates = new MuscleState[0];
                    SaveProgressImmediate(existingData);  
                }

                save = true;
            }

            if (oldVersion != currentVersion)
            {
                settings.version = currentVersion.ToString(); 
                save = true;
            }

            if (save) Settings = settings;
        } 
        catch
        {
            settings = DemoSettings.Default;
            settings.version = Application.version;
            Settings = settings;
        }

        if (settings.tutorialPrompt <= 0)
        {
            demoNotice.gameObject.SetActive(true);
            var confirm = demoNotice.GetComponentInChildren<UITabButton>();
            if (confirm != null)
            {
                if (confirm.OnClick == null) confirm.OnClick = new UnityEngine.Events.UnityEvent(); 
                confirm.OnClick.AddListener(CloseDemoNotice);
            } else CloseDemoNotice();
        }

        if (easyRepperToggle != null)
        {
            if (easyRepperToggle.onValueChanged == null) easyRepperToggle.onValueChanged = new Toggle.ToggleEvent();
            easyRepperToggle.onValueChanged.AddListener(SetEasyRepperMode);
            easyRepperToggle.SetIsOnWithoutNotify(settings.easyRepperMode);
        }
        if (invertRepAssistToggle != null)
        {
            if (invertRepAssistToggle.onValueChanged == null) invertRepAssistToggle.onValueChanged = new Toggle.ToggleEvent();
            invertRepAssistToggle.onValueChanged.AddListener(SetInvertRepAssist);
            invertRepAssistToggle.SetIsOnWithoutNotify(settings.invertRepAssist);
        }
        if (disableUIToggle != null)
        {
            if (disableUIToggle.onValueChanged == null) disableUIToggle.onValueChanged = new Toggle.ToggleEvent();
            disableUIToggle.onValueChanged.AddListener(SetDisableUI);
            disableUIToggle.SetIsOnWithoutNotify(settings.disableUI);
        }
        if (disableUISoundsToggle != null)
        {
            if (disableUISoundsToggle.onValueChanged == null) disableUISoundsToggle.onValueChanged = new Toggle.ToggleEvent();
            disableUISoundsToggle.onValueChanged.AddListener(SetDisableUISounds);
            disableUISoundsToggle.SetIsOnWithoutNotify(settings.disableUISounds); 
        }
        if (growthRateSlider != null)
        {
            if (growthRateSlider.onValueChanged == null) growthRateSlider.onValueChanged = new Slider.SliderEvent();
            growthRateSlider.onValueChanged.AddListener((float val) =>
            {
                var settings = Settings;
                settings.growthRate = val;
                Settings = settings;

                if (growthRateInputField != null) growthRateInputField.SetTextWithoutNotify(val.ToString());  
            });
            growthRateSlider.SetValueWithoutNotify(settings.growthRate);
        }
        if (growthRateInputField != null)
        {
            if (growthRateInputField.onEndEdit == null) growthRateInputField.onEndEdit = new TMP_InputField.SubmitEvent(); 
            growthRateInputField.onEndEdit.AddListener((string val) =>
            {
                if (float.TryParse(val, out var fval))
                {
                    var settings = Settings;
                    settings.growthRate = fval;
                    Settings = settings;

                    if (growthRateSlider != null) growthRateSlider.SetValueWithoutNotify(fval);
                }
            });
            growthRateInputField.SetTextWithoutNotify(settings.growthRate.ToString());
        }

        repCurveRootDefaultSnapshot = Swole.RectTransformSnapshot.CreateFromLocal(Swole.UnityEngineHook.AsSwoleRectTransform(repCurveRoot));

        weightDisplayRootDefaultSnapshot = Swole.RectTransformSnapshot.CreateFromLocal(Swole.UnityEngineHook.AsSwoleRectTransform(weightDisplayRoot));
        weightText = weightDisplayRoot.GetComponentInChildren<TMP_Text>(true);

        totalRepCountDisplayRootDefaultSnapshot = Swole.RectTransformSnapshot.CreateFromLocal(Swole.UnityEngineHook.AsSwoleRectTransform(totalRepCountDisplayRoot));
        totalRepText = totalRepCountDisplayRoot.GetComponentInChildren<TMP_Text>(true);

        workoutRepCountDisplayRootDefaultSnapshot = Swole.RectTransformSnapshot.CreateFromLocal(Swole.UnityEngineHook.AsSwoleRectTransform(workoutRepCountDisplayRoot));
        workoutRepText = workoutRepCountDisplayRoot.GetComponentInChildren<TMP_Text>(true);

        burnOutDisplayRootDefaultSnapshot = Swole.RectTransformSnapshot.CreateFromLocal(Swole.UnityEngineHook.AsSwoleRectTransform(burnOutDisplayRoot));
        energyDisplayRootDefaultSnapshot = Swole.RectTransformSnapshot.CreateFromLocal(Swole.UnityEngineHook.AsSwoleRectTransform(energyDisplayRoot)); 

        if (mainCamera == null) mainCamera = Camera.main;

        characterRootTransform = characterRoot.transform;
        characterRootSwoleTransform = Swole.UnityEngineHook.AsSwoleTransform(characterRootTransform);

        cameraSwoleTransform = Swole.UnityEngineHook.AsSwoleTransform(cameraProxy.transform);

        characterMuscle = characterRoot.GetComponentInChildren<IMuscularBasic>();
        flexProxy = characterRoot.GetComponentInChildren<MuscleFlexProxy>();
        characterPump = characterRoot.GetComponentInChildren<TieredPumpController>(); 

        targetWorkout = -1; 
        stamina = 1;
        energy = 1;

        ResetRepCombo();
        ResetRepBurnOutCounter();
    }
    protected virtual void Start()
    {

        cameraProxy.SetTargetCamera(mainCamera);
        cameraProxy.IsInRelativeMode = false; 

        InitializeCustomization();
        InitializeWorkouts();

        gameStateController = gameObject.AddComponent<GameStateController>();
        foreach (var anim in customizationCharacterObject.animations) if (string.IsNullOrWhiteSpace(anim.package)) anim.package = defaultCustomizationCharacterPackage;
        foreach (var anim in workoutCharacterObject.animations) if (string.IsNullOrWhiteSpace(anim.package)) anim.package = defaultWorkoutCharacterPackage; 
        gameStateController.animatedObjects = new GameStateController.AnimatedObject[] { customizationCharacterObject, workoutCharacterObject };
        gameStateController.Initialize();

        if (workoutChanceAnimationTasks != null)
        {
            List<int> startGruntTasks = new List<int>();
            List<int> endGruntTasks = new List<int>();
            List<int> startGruntChanceTasks = new List<int>();
            List<int> endGruntChanceTasks = new List<int>();

            List<int> tempIndices = new List<int>();

            for(int a = 0; a < workoutChanceAnimationTasks.Length; a++)
            {
                var task = workoutChanceAnimationTasks[a];

                if (task.name.ToLower().IndexOf("start_grunt") >= 0)
                {
                    startGruntTasks.Add(task.taskIndex);
                    startGruntChanceTasks.Add(a);
                }
                else if (task.name.ToLower().IndexOf("end_grunt") >= 0) 
                {
                    endGruntTasks.Add(task.taskIndex); 
                    endGruntChanceTasks.Add(a);
                }
            }

            var startGruntTasksArray = startGruntTasks.ToArray();
            var endGruntTasksArray = endGruntTasks.ToArray();
            tempIndices.Clear();
            tempIndices.AddRange(endGruntTasks);
            tempIndices.AddRange(startGruntTasks);
            var endGruntRestrictingTasksArray = tempIndices.ToArray(); 

            tempIndices.Clear();
            tempIndices.AddRange(startGruntChanceTasks);
            tempIndices.RemoveAll(i => workoutChanceAnimationTasks[i].name.ToLower().IndexOf("insane") >= 0);
            var startGruntChanceTasksArray = tempIndices.ToArray();

            tempIndices.Clear();
            tempIndices.AddRange(endGruntChanceTasks);
            tempIndices.RemoveAll(i => workoutChanceAnimationTasks[i].name.ToLower().IndexOf("insane") >= 0);
            var endGruntChanceTasksArray = tempIndices.ToArray();

            foreach (var startGrunt in startGruntChanceTasks)
            {
                var task = workoutChanceAnimationTasks[startGrunt];
                if (task.additionalTasksToPutOnCooldown != null && task.additionalTasksToPutOnCooldown.Length > 0)
                {
                    tempIndices.Clear();
                    tempIndices.AddRange(task.additionalTasksToPutOnCooldown);
                    tempIndices.AddRange(startGruntChanceTasksArray);
                    task.additionalTasksToPutOnCooldown = tempIndices.ToArray();
                }
                else task.additionalTasksToPutOnCooldown = startGruntChanceTasksArray;

                if (task.name.ToLower().IndexOf("insane") >= 0) continue;
                if (task.restrictingIndices != null && task.restrictingIndices.Length > 0)
                {
                    tempIndices.Clear();
                    tempIndices.AddRange(task.restrictingIndices);
                    tempIndices.AddRange(startGruntTasksArray);
                    task.restrictingIndices = tempIndices.ToArray();
                }
                else task.restrictingIndices = startGruntTasksArray;
            }
            foreach (var endGrunt in endGruntChanceTasks)
            {
                var task = workoutChanceAnimationTasks[endGrunt];
                if (task.additionalTasksToPutOnCooldown != null && task.additionalTasksToPutOnCooldown.Length > 0)
                {
                    tempIndices.Clear();
                    tempIndices.AddRange(task.additionalTasksToPutOnCooldown);
                    tempIndices.AddRange(endGruntChanceTasksArray);
                    task.additionalTasksToPutOnCooldown = tempIndices.ToArray();
                }
                else task.additionalTasksToPutOnCooldown = endGruntChanceTasksArray;

                if (task.name.ToLower().IndexOf("insane") >= 0) continue;
                if (task.restrictingIndices != null && task.restrictingIndices.Length > 0)
                {
                    tempIndices.Clear();
                    tempIndices.AddRange(task.restrictingIndices);
                    tempIndices.AddRange(endGruntRestrictingTasksArray);
                    task.restrictingIndices = tempIndices.ToArray();
                }
                else task.restrictingIndices = endGruntRestrictingTasksArray;
            }
        }

        SetGameState(GameState.Customization);

        Swole.swole.Register(this);

        IEnumerator Load()
        {
            yield return null;
            yield return null;
            yield return null;

            LoadProgress();
        }

        StartCoroutine(Load());
    }
    protected virtual void OnDestroy()
    {
        Swole.swole.Unregister(this);
    }

    [Serializable]
    public enum GameState
    {
        Customization, Workout
    }

    protected GameState gameState;
    public void SetGameState(GameState gameState)
    {
        this.gameState = gameState;
        switch(gameState)
        {
            case GameState.Customization:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                customizationUIRoot.SetActive(true);
                workoutUIRoot.SetActive(false);

                foreach(var renderer in environmentSkinnedRenderers)
                {
                    var mats = renderer.sharedMaterials;
                    mats[0] = environmentMaterialCustomization;
                    renderer.sharedMaterials = mats;
                }
                foreach (var renderer in environmentMeshRenderers)
                {
                    var mats = renderer.sharedMaterials;
                    mats[0] = environmentMaterialCustomization;
                    renderer.sharedMaterials = mats;
                }
                
                if (characterPump != null) characterPump.pumpDecayMultiplier = 0;
                customizationPose = CustomizationPose.IdleFront;
                targetCustomizationPose = CustomizationPose.IdleFront;  
                customizationMusic.Play();

                workoutCharacterObject.StopAllTasks();
                workoutCharacterObject.RemoveControllers();
                workoutCharacterObject.disable = true;
                customizationCharacterObject.ApplyControllers();
                customizationCharacterObject.Reset();
                customizationCharacterObject.disable = false;

                customizationCharacterTransformState.ApplyTo(characterRootSwoleTransform);
                cameraProxy.transform.SetParent(customizationCameraTargetParent, true);
                customizationCameraTransformState.ApplyTo(cameraSwoleTransform);

                SetTargetFlexPose(-1);
                SyncSliders(false);
                break;
            
            case GameState.Workout:

                if (settings.tutorialPrompt < 2 && !controlsPopup.gameObject.activeSelf)
                {
                    controlsPopup.gameObject.SetActive(true);

                    var confirm = controlsPopup.GetComponentInChildren<UITabButton>();
                    if (confirm != null)
                    {
                        if (confirm.OnClick == null) confirm.OnClick = new UnityEngine.Events.UnityEvent(); 
                        confirm.OnClick.RemoveAllListeners();
                        confirm.OnClick.AddListener(CloseControlsPopup); 
                    }
                    else CloseControlsPopup();
                }

                customizationUIRoot.SetActive(false);
                workoutUIRoot.SetActive(!Settings.disableUI);  

                foreach (var renderer in environmentSkinnedRenderers)
                {
                    var mats = renderer.sharedMaterials;
                    mats[0] = environmentMaterialWorkout;
                    renderer.sharedMaterials = mats; 
                }
                foreach (var renderer in environmentMeshRenderers)
                {
                    var mats = renderer.sharedMaterials;
                    mats[0] = environmentMaterialWorkout;
                    renderer.sharedMaterials = mats;
                }

                customizationMusic.Pause();
                if (characterPump != null) characterPump.pumpDecayMultiplier = 1;

                customizationCharacterObject.StopAllTasks();
                customizationCharacterObject.RemoveControllers();
                customizationCharacterObject.disable = true;
                workoutCharacterObject.ApplyControllers(); 
                workoutCharacterObject.Reset();
                workoutCharacterObject.disable = false;

                workoutCharacterTransformState.ApplyTo(characterRootSwoleTransform);
                cameraProxy.transform.SetParent(workoutCameraTargetParent, true);
                workoutCameraTransformState.ApplyTo(cameraSwoleTransform);

                int lastWorkout = targetWorkout;
                targetWorkout = -1;
                if (lastWorkout < 0 || lastWorkout >= workouts.Length) lastWorkout = 0; 
                SetTargetWorkout(lastWorkout);
                break;
        }
    }
    public void SetGameState(int stateIndex) => SetGameState((GameState)stateIndex);
    public void SetGameState(string stateName) => SetGameState(Enum.Parse<GameState>(stateName));

    public virtual void OnUpdate()
    {

        if (demoNotice.gameObject.activeSelf)
        {
            if (Swole.InputProxy.PauseButtonDown || Swole.InputProxy.JumpButtonDown || Swole.InputProxy.MOBA_AbilityQDown || Swole.InputProxy.MOBA_AbilityRDown) CloseDemoNotice();
        }
        if (controlsPopup.gameObject.activeSelf)
        {
            if (Swole.InputProxy.PauseButtonDown || Swole.InputProxy.JumpButtonDown || Swole.InputProxy.MOBA_AbilityQDown || Swole.InputProxy.MOBA_AbilityRDown) CloseControlsPopup();
        }
        if (Swole.InputProxy.PauseButtonDown)
        {
            pauseMenu.SetActive(!pauseMenu.activeSelf); 
        }

        switch (gameState)
        {
            case GameState.Customization:
                UpdateCustomization();   
                break;

            case GameState.Workout:
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    SetTargetWorkout(0);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    SetTargetWorkout(1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    SetTargetWorkout(2);
                }
                 
                UpdateWorkouts(); 
                break;  
        }

    }

    public virtual void OnLateUpdate()
    {
        UpdatePlateLoaders();

        switch (gameState)
        {
            case GameState.Customization:
                break;

            case GameState.Workout:
                LateUpdateWorkouts();
                break;
        }

        cameraProxy.ApplyChanges();

        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0;
            SaveProgress();   
        }
    }

    public virtual void OnFixedUpdate() { } 

}
