using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Swole;
using Swole.API.Unity;
using Swole.Modding;
using Swole.Script;

namespace BulkOut
{

    [Serializable]
    public class workoutInjector : IContent, ICloneable, ISwoleSerialization<workoutInjector, workoutInjector.Serialized>
    {

        #region IClonable

        public workoutInjector Duplicate()
        {
            var clone = new workoutInjector();

            var fields = typeof(workoutInjector).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields) field.SetValue(clone, field.GetValue(this));

            if (animations != null)
            {
                clone.animations = new Animation[animations.Length];
                for (int a = 0; a < clone.animations.Length; a++)
                {
                    var anim = animations[a];
                    anim.binding = new BoundIndexReference() { id = anim.binding.id, boundIndex = anim.binding.boundIndex };
                    anim.settings = anim.settings.Duplicate();
                    clone.animations[a] = anim;
                }
            }

            if (sounds != null)
            {
                clone.sounds = new Sound[sounds.Length];
                for (int a = 0; a < clone.sounds.Length; a++)
                {
                    var anim = sounds[a];
                    anim.binding = new BoundIndexReference() { id = anim.binding.id, boundIndex = anim.binding.boundIndex };
                    anim.settings = anim.settings.Duplicate();
                    clone.sounds[a] = anim;
                }
            }

            if (tasks != null)
            {
                clone.tasks = new Task[tasks.Length];
                for (int a = 0; a < clone.tasks.Length; a++)
                {
                    var task = tasks[a];
                    task.binding = new BoundIndexReference() { id = task.binding.id, boundIndex = task.binding.boundIndex };
                    task.settings = task.settings.Duplicate();
                    clone.tasks[a] = task;
                }
            }

            clone.parameters = parameters.Duplicate();

            return clone;
        }
        public object Clone() => Duplicate();

        #endregion

        public Type AssetType => GetType();
        public object Asset => this;

        public bool IsInternalAsset { get => false; set { } }

        public bool IsValid => true;
        public void Dispose() { }
        public void Delete() => Dispose();

        #region Serialization

        public string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<workoutInjector, workoutInjector.Serialized>
        {

            public ContentInfo contentInfo;
            public string SerializedName => contentInfo.name;

            public string referenceWorkout;
            public string defaultAssetPackage;
            public Animation[] animations;
            public Sound[] sounds;
            public Task[] tasks;
            public workoutParameters parameters;

            public workoutInjector AsOriginalType(PackageInfo packageInfo = default) => new workoutInjector(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public workoutInjector.Serialized AsSerializableStruct()
        {
            var data = new workoutInjector.Serialized() { contentInfo = contentInfo, defaultAssetPackage = defaultAssetPackage, referenceWorkout = referenceWorkout, animations = animations, sounds = sounds, tasks = tasks, parameters = parameters };
            var info = contentInfo;
            info.name = SerializedName;

            data.contentInfo = info;

            return data;
        }
        public object AsSerializableObject() => AsSerializableStruct();

        public workoutInjector(workoutInjector.Serialized serializable, PackageInfo packageInfo = default)
        {
            originPath = relativePath = string.Empty;
            this.packageInfo = packageInfo;

            var info = serializable.contentInfo;
            if (info.name.AsID().StartsWith(workoutLoader.prefix_workoutAssetFile.AsID())) info.name = info.name.Substring(workoutLoader.prefix_workoutAssetFile.Length);
            this.contentInfo = info;

            this.referenceWorkout = serializable.referenceWorkout;
            this.defaultAssetPackage = serializable.defaultAssetPackage;
            this.animations = serializable.animations;
            this.sounds = serializable.sounds;
            this.tasks = serializable.tasks;
            this.parameters = serializable.parameters;

        }

        #endregion

        public string originPath;
        public string OriginPath => originPath;
        public IContent SetOriginPath(string path)
        {
            var content = this;
            content.originPath = path;
            return content;
        }
        public string relativePath;
        public string RelativePath => relativePath;
        public IContent SetRelativePath(string path)
        {
            var content = this;
            content.relativePath = path;
            return content;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {

            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            return dependencies;

        }

        public override string ToString()
        {

            return $"{Name}{(string.IsNullOrEmpty(Author) ? "" : " created by" + Author)}{(this.HasPackage() ? " - imported from '" + packageInfo.GetIdentityString() + "'" : "")}";

        }

        public string Name => contentInfo.name;

        public string Author => contentInfo.author;
        public string CreationDate => contentInfo.creationDate;
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        public PackageInfo packageInfo;
        public PackageInfo PackageInfo => packageInfo;

        public ContentInfo contentInfo;
        public ContentInfo ContentInfo => contentInfo;
        public string SerializedName => workoutLoader.prefix_workoutAssetFile + contentInfo.name;

        public string CollectionID { get => null; set { } }

        public bool HasCollectionID => false;

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info) => new workoutInjector() { contentInfo = info, defaultAssetPackage = defaultAssetPackage, referenceWorkout = referenceWorkout, animations = animations, sounds = sounds, tasks = tasks, parameters = parameters };

        public const string prefix_native = "native:";
        public const string prefix_copyNative = "copyNative:";
        public const string prefix_copyFrom = "copyFrom:";

        public workoutInjector()
        {
        }
        public workoutInjector(string referenceWorkout, string defaultAssetPackage, Animation[] animations, Sound[] sounds, Task[] tasks, workoutParameters parameters)
        {
            this.referenceWorkout = referenceWorkout;
            this.defaultAssetPackage = defaultAssetPackage;
            this.animations = animations;
            this.sounds = sounds;
            this.tasks = tasks;
            this.parameters = parameters;
        }

        public string referenceWorkout;
        public string defaultAssetPackage;

        public Animation[] animations;
        public bool TryGetAnimation(string idOrName, out Animation animation, out int index)
        {
            animation = default;
            index = -1;
            if (animations == null || string.IsNullOrWhiteSpace(idOrName)) return false;

            idOrName = idOrName.AsID();

            for (int a = 0; a < animations.Length; a++)
            {
                var anim = animations[a];
                if ((anim.binding != null && !string.IsNullOrWhiteSpace(anim.binding.id) && anim.binding.id.AsID() == idOrName) ||
                    (anim.settings != null && ((!string.IsNullOrWhiteSpace(anim.settings.name) && anim.settings.name.AsID() == idOrName) || (!string.IsNullOrWhiteSpace(anim.settings.displayName) && anim.settings.displayName.AsID() == idOrName))))
                {
                    index = a;
                    animation = anim;
                    return true;
                }
            }

            return false;
        }
        public Sound[] sounds;
        public bool TryGetSound(string idOrName, out Sound sound, out int index)
        {
            sound = default;
            index = -1;
            if (sounds == null || string.IsNullOrWhiteSpace(idOrName)) return false;

            idOrName = idOrName.AsID();

            for (int a = 0; a < sounds.Length; a++)
            {
                var snd = sounds[a];
                if ((snd.binding != null && !string.IsNullOrWhiteSpace(snd.binding.id) && snd.binding.id.AsID() == idOrName))
                {
                    index = a;
                    sound = snd;
                    return true;
                }
            }

            return false;
        }
        public Task[] tasks;
        public bool TryGetTask(string idOrName, out Task task, out int index)
        {
            task = default;
            index = -1;
            if (tasks == null || string.IsNullOrWhiteSpace(idOrName)) return false;

            idOrName = idOrName.AsID();

            for (int a = 0; a < tasks.Length; a++)
            {
                var tsk = tasks[a];
                if ((tsk.binding != null && !string.IsNullOrWhiteSpace(tsk.binding.id) && tsk.binding.id.AsID() == idOrName) ||
                    (tsk.settings != null && ((!string.IsNullOrWhiteSpace(tsk.settings.name) && tsk.settings.name.AsID() == idOrName))))
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

        public bool TryFindAnimation(string localAnimId, out Animation result)
        {
            result = default;
            if (tasks == null) return false;

            foreach (var animation in animations) if (animation.binding.id == localAnimId)
                {
                    result = animation;
                    return true;
                }
            return false;
        }
        public bool TryFindSound(string localSoundId, out Sound result)
        {
            result = default;
            if (tasks == null) return false;

            foreach (var sound in sounds) if (sound.binding.id == localSoundId)
                {
                    result = sound;
                    return true;
                }
            return false;
        }
        public bool TryFindTask(string localTaskId, out Task result)
        {
            result = default;
            if (tasks == null) return false;

            foreach (var task in tasks) if (task.binding.id == localTaskId)
                {
                    result = task;
                    return true;
                }
            return false;
        }

        public workoutParameters parameters;

        [NonSerialized]
        protected workoutGameTest game;
        public bool Inject(ContentPackage sourcePackage, workoutGameTest game)
        {
            if (ReferenceEquals(this.game, game)) return false;

            if (!game.TryGetWorkout(referenceWorkout, out var refWorkout)) return false;

            var workout = refWorkout.Duplicate();

            if (animations != null)
            {
                List<GameStateController.ImportedAnimation> newAnimations = new List<GameStateController.ImportedAnimation>(game.workoutCharacterObject.animations);
                foreach (var animation in animations)
                {
                    animation.binding.boundIndex = newAnimations.Count;
                    var instance = animation.settings.Duplicate();
                    instance.displayName = $"{Name}.{animation.binding.id}";
                    if (string.IsNullOrWhiteSpace(instance.package)) instance.package = defaultAssetPackage;
                    newAnimations.Add(instance);
                }
                foreach (var animation in animations) // remap dependencies
                {
                    var instance = newAnimations[animation.binding.boundIndex];

                    if (instance.nullifyingDependencies != null)
                    {
                        for (int a = 0; a < instance.nullifyingDependencies.Length; a++)
                        {
                            var index = instance.nullifyingDependencies[a];
                            if (index < 0)
                            {
                                index = (-index) - 1;
                            }
                            else
                            {
                                index = index >= animations.Length ? -1 : animations[index].binding.boundIndex;
                            }
                        }
                    }
                }

                game.workoutCharacterObject.animations = newAnimations.ToArray();
            }

            if (sounds != null)
            {
                List<GameStateController.Sound> newSounds = new List<GameStateController.Sound>(game.workoutCharacterObject.sounds);
                foreach (var sound in sounds)
                {
                    sound.binding.boundIndex = newSounds.Count;
                    var instance = sound.settings.Duplicate();
                    //instance.displayName = $"{Name}.{animation.binding.id}";
                    //if (string.IsNullOrWhiteSpace(instance.package)) instance.package = defaultAssetPackage;
                    newSounds.Add(instance);
                }

                game.workoutCharacterObject.sounds = newSounds.ToArray();
            }

            GameStateController.TaskDependency RemapTaskDependency(GameStateController.TaskDependency dep)
            {
                switch (dep.type)
                {
                    case GameStateController.TaskDependencyType.ParentAnimationMix:
                    case GameStateController.TaskDependencyType.ParentAnimationTime:
                        if (dep.indices != null)
                        {
                            dep.indices = (GameStateController.TaskDependencyPair[])dep.indices.Clone();
                            for (int a = 0; a < dep.indices.Length; a++)
                            {
                                var pair = dep.indices[a];
                                int index = pair.index;
                                if (index < 0)
                                {
                                    index = (-index) - 1;
                                }
                                else
                                {
                                    index = index >= animations.Length ? -1 : animations[index].binding.boundIndex;
                                }

                                dep.indices[a] = new GameStateController.TaskDependencyPair() { index = index, name = pair.name, weight = pair.weight };
                            }
                        }
                        break;

                    case GameStateController.TaskDependencyType.ParentTaskMix:
                    case GameStateController.TaskDependencyType.ParentTaskTime:
                        if (dep.indices != null)
                        {
                            dep.indices = (GameStateController.TaskDependencyPair[])dep.indices.Clone();
                            for (int a = 0; a < dep.indices.Length; a++)
                            {
                                var pair = dep.indices[a];
                                int index = pair.index;
                                if (index < 0)
                                {
                                    index = (-index) - 1;
                                }
                                else
                                {
                                    index = index >= tasks.Length ? -1 : tasks[index].binding.boundIndex;
                                }

                                dep.indices[a] = new GameStateController.TaskDependencyPair() { index = index, name = pair.name, weight = pair.weight };
                            }
                        }
                        break;
                }

                return dep;
            }
            if (tasks != null)
            {
                List<GameStateController.AnimationTask> newTasks = new List<GameStateController.AnimationTask>(game.workoutCharacterObject.tasks);
                foreach (var task in tasks)
                {
                    task.binding.boundIndex = newTasks.Count;
                    var instance = task.settings.Duplicate();
                    instance.name = $"{Name}.{task.binding.id}";
                    newTasks.Add(instance);
                }
                foreach (var task in tasks) // remap dependencies
                {
                    var instance = newTasks[task.binding.boundIndex];

                    if (instance.animsToPlay != null)
                    {
                        for (int a = 0; a < instance.animsToPlay.Length; a++)
                        {
                            var asset = instance.animsToPlay[a];

                            var index = asset.assetIndex;
                            if (index < 0)
                            {
                                index = (-index) - 1;
                            }
                            else
                            {
                                index = index >= animations.Length ? -1 : animations[index].binding.boundIndex;
                            }
                            asset.assetIndex = index;

                            if (asset.mixDependencies != null)
                            {
                                asset.mixDependencies = (GameStateController.TaskDependency[])asset.mixDependencies.Clone();
                                for (int b = 0; b < asset.mixDependencies.Length; b++) asset.mixDependencies[b] = RemapTaskDependency(asset.mixDependencies[b]);
                            }

                            instance.animsToPlay[a] = asset;
                        }
                    }

                    if (instance.soundsToPlay != null)
                    {
                        for (int a = 0; a < instance.soundsToPlay.Length; a++)
                        {
                            var asset = instance.soundsToPlay[a]; 

                            var index = asset.assetIndex;
                            if (index < 0)
                            {
                                index = (-index) - 1;
                            }
                            else
                            {
                                index = index >= sounds.Length ? -1 : sounds[index].binding.boundIndex;
                            }
                            asset.assetIndex = index;

                            instance.soundsToPlay[a] = asset;
                        }
                    }

                    if (instance.dependencies != null)
                    {
                        for (int b = 0; b < instance.dependencies.Length; b++) instance.dependencies[b] = RemapTaskDependency(instance.dependencies[b]);
                    }
                }

                game.workoutCharacterObject.tasks = newTasks.ToArray();
            }

            parameters.ApplyTo(sourcePackage, game, game.workoutCharacterObject, this, workout);

            workoutGameTest.Workout[] workouts = new workoutGameTest.Workout[game.workouts.Length + 1];
            for (int a = 0; a < game.workouts.Length; a++) workouts[a] = game.workouts[a];
            workouts[game.workouts.Length] = workout;
            game.workouts = workouts;

            return true;
        }

        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info)
        {
            var clone = new workoutInjector();

            var fields = typeof(workoutInjector).GetFields(BindingFlags.Instance);
            foreach (var field in fields) field.SetValue(clone, field.GetValue(this));

            contentInfo = info;

            return clone;
        }

        public void DisposeSelf()
        {
        }

        public bool IsIdenticalAsset(ISwoleAsset otherAsset)
        {
            if (otherAsset is workoutInjector otherInjector)
            {
                return this == otherInjector;
            }

            return false;
        }
    }

    [Serializable]
    public class BoundIndexReference
    {
        [RedrawOnEdit]
        public string id;

        [NonSerialized]
        public int boundIndex;
    }
    [Serializable]
    public struct Animation
    {
        public BoundIndexReference binding;
        public GameStateController.ImportedAnimation settings;
    }
    [Serializable]
    public struct Sound
    {
        public BoundIndexReference binding;
        public string name;
        public string package;
        public string mixer;
        public string source;
        public GameStateController.Sound settings;
    }
    [Serializable]
    public struct Task
    {
        public BoundIndexReference binding;
        public GameStateController.AnimationTask settings;
    }

    [Serializable]
    public class RepCurveReference : ICloneable
    {

        public RepCurveReference Duplicate()
        {
            var clone = new RepCurveReference();

            clone.package = package;
            clone.creation = creation;
            clone.curveName = curveName;

            return clone;
        }
        public object Clone() => Duplicate();

        public string package;
        public string creation;
        public string curveName;

        [NonSerialized]
        public BezierCurveUnity curveInstance;
    }

    [Serializable]
    public struct OverridableTask : IOverridable
    {
        public bool doOverride;
        public string taskId;
        public object Value
        {
            get => taskId;
            set => this.taskId = (string)value;
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(string);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public string[] taskIds;
            public object Value
            {
                get => taskIds;
                set => this.taskIds = (string[])value;
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(string[]);
        }
    }
    [Serializable]
    public struct workoutParameters : ICloneable
    {

        #region ICloneable

        public workoutParameters Duplicate()
        {
            var copy = this;

            if (targetMuscles != null) copy.targetMuscles = (workoutGameTest.WorkoutTargetMuscle.ModdableData[])targetMuscles.Clone();
            if (repCurve != null) copy.repCurve = new RepCurveReference() { creation = repCurve.creation, curveName = repCurve.curveName, package = repCurve.package };

            object clone = copy;

            var fields = typeof(workoutParameters).GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                var val = field.GetValue(clone);
                var innerFields = field.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public);
                if (val is ICloneable cloneable) val = cloneable.Clone();
                if (!ReferenceEquals(val, null))
                {
                    foreach (var innerField in innerFields)
                    {
                        var val2 = innerField.GetValue(val);
                        if (val2 is ICloneable cloneable2) val2 = cloneable2.Clone();
                        innerField.SetValue(val, val2);
                    }
                }
                field.SetValue(clone, val);
            }

            return (workoutParameters)clone;
        }
        public object Clone() => Duplicate();

        #endregion

        [NonEditable]
        public string displayName;

        [NonEditable]
        public string thumbnail;

        [EditAsEnum(typeof(workoutGameTest.WeightSource))]
        public OverridableString weightSource;

        [UseEditablePrefix(true)]
        public OverridableRectTransformSnapshotEuler repCurveTransformState;
        [UseEditablePrefix(true)]
        public OverridableRectTransformSnapshotEuler weightDisplayTransformState;
        [UseEditablePrefix(true)]
        public OverridableRectTransformSnapshotEuler totalRepCountDisplayTransformState;
        [UseEditablePrefix(true)]
        public OverridableRectTransformSnapshotEuler workoutRepCountDisplayTransformState;
        [UseEditablePrefix(true)]
        public OverridableRectTransformSnapshotEuler burnOutDisplayTransformState;
        [UseEditablePrefix(true)]
        public OverridableRectTransformSnapshotEuler energyDisplayTransformState;

        public OverridableBool hideBench;
        public OverridableBool hidePecDeck;
        public OverridableBool hidePulldownBar;
        public OverridableBool foldBarbell;
        public OverridableBool hideBarbell;

        public OverridableFloat headTrembleMax;
        public OverridableFloat leftArmTrembleMax;
        public OverridableFloat rightArmTrembleMax;
        public OverridableFloat leftLegTrembleMax;
        public OverridableFloat rightLegTrembleMax;

        [OverrideSelectionAlias(nameof(repCurve))]
        public OverridableFlag overrideRepCurve;
        [OverrideDependency(nameof(overrideRepCurve))]
        public RepCurveReference repCurve;

        public OverridableBool invertRepAssist;
        //public OverridableFloat repMomentumAcceleration; // not used

        public OverridableFloat repSpeedMax;
        public OverridableFloat repSpeedMin;
        public OverridableFloat repSpeedBurnOut;
        public OverridableFloat maxRepGravity;
        public OverridableFloat repAlignmentGravityStart;
        [EditableAlias("burnOutGravityFactorExp")]
        public OverridableFloat staminaGravityFactorExp;

        public OverridableFloat repCompletionMaxStaminaRefund;
        public OverridableFloat perfectRepMinAccuracy;
        public OverridableFloat perfectRepStaminaRefundMultiplier;
        public OverridableFloat repStartPreventionStaminaThreshold;

        public OverridableInt softWeightMaximum;
        public OverridableInt weightMinimum;

        public OverridableBool halveWeightDisplay;

        [EditableAlias("staminaCostPerSecond")]
        public OverridableFloat staminaPerSecond;
        public OverridableFloat staminaRecoveryPerSecond;
        public OverridableFloat staminaRecoveryDelay;
        public OverridableFloat staminaRecoveryRestBonusMultiplier;

        [EditableAlias("energyCostPerSecond")]
        public OverridableFloat energyPerSecond;
        public OverridableFloat energyRecoveryPerSecond;
        public OverridableFloat energyRecoveryDelay;
        public OverridableFloat energyRecoveryRestBonusMultiplier;

        public OverridableFloat growthRate;
        public OverridableFloat fullRepGrowthBonus;
        public OverridableFloat eccentricGrowthMultiplier;
        public OverridableFloat burnOutGrowthMultiplier;
        public OverridableFloat burnOutRepGrowthBonusMultiplier;
        public OverridableFloat burnOutGrowthBonusFalloff;

        public OverridableFloat burnOutRepCounterGrowthMultiplier;
        public OverridableFloat burnOutRepCounterResetThresholdMin;
        public OverridableFloat burnOutRepCounterResetThresholdMax;
        public OverridableFloat burnOutRepCounterDifficultyMultiplier;

        public OverridableFloat repMaxHoldTime;
        public OverridableFloat repHoldBonusDifficultyFalloff;

        public OverridableFloat flexStartPoint;
        public OverridableFloat flexHardStartThreshold;
        public OverridableFloat flexProgressSpeed;
        public OverridableFloat flexHardDecay;
        public OverridableFloat flexHardDecayEXP;
        public OverridableFloat flexHardInputAddRatio;
        public OverridableVector2 flexRepProgressMapping;
        public OverridableVector2 flexInputAxis;
        public OverridableInt.Array flexLocalTargetMuscles;

        public OverridableTask restTask;
        public OverridableTask restTiredBlendTask;
        public OverridableTask restToRepTask;
        public OverridableTask repToRestTask;

        public OverridableTask.Array restOffsetTasks;

        public OverridableTask repBaseTask;
        public OverridableTask repAddTask;
        public OverridableTask repStruggleTask;
        public OverridableFloat repStruggleMix;
        public OverridableTask repExhaustionTask;

        public OverridableTask.Array repOffsetTasks;

        public OverridableTask flexBaseTask;
        public OverridableTask flexControlTask;
        public OverridableTask restToFlexTask;
        public OverridableTask flexToRestTask;

        public OverridableTask.Array flexOffsetTasks;

        public OverridableTask breatheLightTask;
        public OverridableTask breatheMedTask;
        public OverridableTask breatheHeavyTask;

        public OverridableTask restCamBaseTask;
        public OverridableTask restCamXTask;
        public OverridableTask restCamYTask;

        public OverridableTask repCamBaseTask;
        public OverridableTask repCamXTask;
        public OverridableTask repCamYTask;

        public OverridableTask flexCamBaseTask;
        public OverridableTask flexCamXTask;
        public OverridableTask flexCamYTask;

        public OverridableFloat restCamSpeedX;
        public OverridableFloat restCamSpeedY;
        public OverridableFloat restCamXStartPos;
        public OverridableFloat restCamYStartPos;
        public OverridableBool restCamAllowWrapX;
        public OverridableBool restCamAllowWrapY;

        public OverridableFloat repCamSpeedX;
        public OverridableFloat repCamSpeedY;
        public OverridableVector4 repCamTransitionMappingX;
        public OverridableVector4 repCamTransitionMappingY;
        public OverridableBool repCamAllowWrapX;
        public OverridableBool repCamAllowWrapY;

        public OverridableFloat flexCamSpeedX;
        public OverridableFloat flexCamSpeedY;
        public OverridableVector4 flexCamTransitionMappingX;
        public OverridableVector4 flexCamTransitionMappingY;
        public OverridableBool flexCamAllowWrapX;
        public OverridableBool flexCamAllowWrapY;

        [OverrideSelectionAlias(nameof(targetMuscles))]
        public OverridableFlag overrideTargetMuscles;

        [OverrideDependency(nameof(overrideTargetMuscles))]
        public int mainMuscleL;
        [OverrideDependency(nameof(overrideTargetMuscles))]
        public int mainMuscleR;
        [OverrideDependency(nameof(overrideTargetMuscles))]
        public workoutGameTest.WorkoutTargetMuscle.ModdableData[] targetMuscles;

        [OverrideSelectionAlias(nameof(bodyWeightSettings))]
        public OverridableFlag overrideBodyWeightSettings;
        [OverrideDependency(nameof(overrideBodyWeightSettings))]
        public workoutGameTest.BodyWeightSettings bodyWeightSettings;

        public void ApplyTo(ContentPackage sourcePackage, workoutGameTest game, GameStateController.AnimatedObject animatedObject, workoutInjector injector, workoutGameTest.Workout workout)
        {
            if (injector == null || workout == null) return;

            workout.name = injector.Name;
            workout.displayName = displayName;

            workout.thumbnail = null;
            if (string.IsNullOrWhiteSpace(thumbnail))
            {
                var thumbnailName = $"{workoutLoader.prefix_workoutThumbnail}{injector.Name}";
                if (sourcePackage != null && sourcePackage.TryFind(out ImageAsset imgAsset, thumbnailName)) workout.thumbnail = imgAsset.Sprite;
            }
            else
            {
                int firstSeparator = thumbnail.IndexOf(Path.DirectorySeparatorChar);
                if (firstSeparator < 0) firstSeparator = thumbnail.IndexOf(Path.AltDirectorySeparatorChar);
                if (firstSeparator >= 0)
                {
                    string potentialPkgName = thumbnail.Substring(0, firstSeparator);
                    int versionCharIndex = string.IsNullOrWhiteSpace(potentialPkgName) ? -1 : potentialPkgName.IndexOf(SwoleScriptSemantics.ssVersionPrefix);
                    if (versionCharIndex >= 0)
                    {
                        if (ContentManager.TryFindPackage(new PackageIdentifier(potentialPkgName), out var pkg))
                        {
                            var thumbnailName = Path.GetFileNameWithoutExtension(thumbnail);
                            if (pkg != null && pkg.TryFind(out ImageAsset imgAsset, thumbnailName)) workout.thumbnail = imgAsset.Sprite;
                        }
                    }
                    else
                    {
                        var thumbnailName = Path.GetFileNameWithoutExtension(thumbnail);
                        if (sourcePackage != null && sourcePackage.TryFind(out ImageAsset imgAsset, thumbnailName)) workout.thumbnail = imgAsset.Sprite;
                    }
                }
            }

            if (overrideRepCurve.IsOverridden && repCurve != null)
            {
                if (!string.IsNullOrWhiteSpace(repCurve.creation) && !string.IsNullOrWhiteSpace(repCurve.curveName))
                {
                    var pkgName = repCurve.package;
                    if (string.IsNullOrWhiteSpace(pkgName)) pkgName = injector.defaultAssetPackage;
                    if (!string.IsNullOrWhiteSpace(pkgName) && ContentManager.TryFindPackage(new PackageIdentifier(pkgName), out var pkg))
                    {
                        if (pkg.TryFind<Creation>(out var creation, repCurve.creation))
                        {
                            if (creation.TryGetCurve(repCurve.curveName, out var curve) && curve is IBezierCurve bezCurve)
                            {
                                if (repCurve.curveInstance != null) GameObject.Destroy(repCurve.curveInstance);
                                repCurve.curveInstance = BezierCurveUnity.Create(bezCurve);

                                // TODO: handle this a different way
                                repCurve.curveInstance.vertexAccuracy = workout.repCurve.vertexAccuracy;
                                repCurve.curveInstance.vertexSpacing = workout.repCurve.vertexSpacing;

                                workout.repCurve = repCurve.curveInstance;
                            }
                        }
                    }
                }
            }

            if (overrideTargetMuscles.IsOverridden)
            {
                if (targetMuscles == null || targetMuscles.Length <= 0)
                {
                    swole.LogError($"Custom workout '{injector.Name}' does not affect any muscle groups! Defaulting to fallback muscle groups.");
                }
                else
                {
                    workout.mainMuscleL = mainMuscleL;
                    workout.mainMuscleR = mainMuscleR;
                    workout.targetMuscles = new workoutGameTest.WorkoutTargetMuscle[targetMuscles.Length];
                    for (int a = 0; a < targetMuscles.Length; a++)
                    {
                        var tm = targetMuscles[a];
                        workout.targetMuscles[a] = new workoutGameTest.WorkoutTargetMuscle()
                        {
                            //muscleName = tm.muscleName, 
                            muscleName = tm.muscleGroup.ToString(),
                            sideOfBody = tm.sideOfBody,//Enum.TryParse<workoutGameTest.SideOfBody>(tm.sideOfBody, true, out var sideOfBody) ? sideOfBody : workoutGameTest.SideOfBody.Both,
                            liftRatio = tm.liftRatio,
                            growthRatio = tm.growthRatio,
                            pumpRatio = tm.pumpRatio,
                            mainMusclePumpMix = tm.mainMusclePumpMix
                        };
                    }
                }
            }

            if (overrideBodyWeightSettings.IsOverridden)
            {
                workout.bodyWeightSettings = bodyWeightSettings;
            }

            var localFields = typeof(workoutParameters).GetFields();
            foreach (var field in localFields)
            {
                if (field == null || !typeof(IOverridable).IsAssignableFrom(field.FieldType)) continue;

                var fieldToOverride = typeof(workoutGameTest.Workout).GetField(field.Name);
                if (fieldToOverride == null) continue;

                IOverridable overridable = (IOverridable)field.GetValue(this);
                if (!overridable.IsOverridden) continue;

                if (overridable is OverridableTask task)
                {
                    if (string.IsNullOrWhiteSpace(task.taskId))
                    {
                        fieldToOverride.SetValue(workout, -1);
                    }
                    else if (task.taskId.StartsWith(workoutInjector.prefix_native) && animatedObject.TryGetTaskIndex(task.taskId.Substring(workoutInjector.prefix_native.Length), out var nativeIndex))
                    {
                        fieldToOverride.SetValue(workout, nativeIndex);
                    }
                    else if (task.taskId.StartsWith(workoutInjector.prefix_copyNative))
                    {
                        string copyWorkoutName = task.taskId.Substring(workoutInjector.prefix_copyNative.Length);
                        string copyFieldName = field.Name;

                        if (!game.TryGetWorkout(copyWorkoutName, out var copyFromWorkout))
                        {
                            swole.LogError($"({injector.Name}:{field.Name}) Tried to copy task field '{copyFieldName}' from nonexistent workout '{copyWorkoutName}'");
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(copyFieldName))
                        {
                            var copyFromField = typeof(workoutGameTest.Workout).GetField(copyFieldName);
                            if (copyFromField != null)
                            {
                                fieldToOverride.SetValue(workout, copyFromField.GetValue(copyFromWorkout));
                            }
                            else swole.LogError($"({injector.Name}:{field.Name}) Tried to copy an unrecognized task field '{copyFieldName}' from workout '{copyWorkoutName}'");
                        }
                        else swole.LogError($"({injector.Name}:{field.Name}) Tried to copy a nameless task field from workout '{copyWorkoutName}'");
                    }
                    else if (task.taskId.StartsWith(workoutInjector.prefix_copyFrom) && task.taskId.LastIndexOf('.') > 0)
                    {
                        string taskId = task.taskId.Substring(workoutInjector.prefix_copyFrom.Length);
                        int dotIndex = taskId.LastIndexOf('.');
                        string copyWorkoutName = taskId.Substring(0, dotIndex);
                        string copyFieldName = dotIndex + 1 < taskId.Length ? taskId.Substring(dotIndex + 1) : string.Empty;

                        if (!game.TryGetWorkout(copyWorkoutName, out var copyFromWorkout))
                        {
                            swole.LogError($"({injector.Name}:{field.Name}) Tried to copy task field '{copyFieldName}' from nonexistent workout '{copyWorkoutName}'");
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(copyFieldName))
                        {
                            var copyFromField = typeof(workoutGameTest.Workout).GetField(copyFieldName);
                            if (copyFromField != null)
                            {
                                fieldToOverride.SetValue(workout, copyFromField.GetValue(copyFromWorkout));
                            }
                            else swole.LogError($"({injector.Name}:{field.Name}) Tried to copy an unrecognized task field '{copyFieldName}' from workout '{copyWorkoutName}'");
                        }
                        else swole.LogError($"({injector.Name}:{field.Name}) Tried to copy a nameless task field from workout '{copyWorkoutName}'");
                    }
                    else if (injector.TryFindTask(task.taskId, out var taskToInject))
                    {
                        fieldToOverride.SetValue(workout, taskToInject.binding.boundIndex);
                    }
                    else swole.LogError($"({injector.Name}:{field.Name}) Task '{task.taskId}' was not found!");
                }
                else if (overridable is OverridableTask.Array taskArray)
                {
                    int[] targetTaskArray = null;
                    if (taskArray.taskIds != null)
                    {
                        targetTaskArray = new int[taskArray.taskIds.Length];
                        for (int a = 0; a < taskArray.taskIds.Length; a++)
                        {
                            var localTaskId = taskArray.taskIds[a];

                            if (string.IsNullOrWhiteSpace(localTaskId))
                            {
                                targetTaskArray[a] = -1;
                            }
                            else if (localTaskId.StartsWith(workoutInjector.prefix_native) && animatedObject.TryGetTaskIndex(localTaskId.Substring(workoutInjector.prefix_native.Length), out var nativeIndex))
                            {
                                targetTaskArray[a] = nativeIndex;
                            }
                            else if (localTaskId.StartsWith(workoutInjector.prefix_copyFrom) && localTaskId.LastIndexOf('.') > 0)
                            {
                                int dotIndex = localTaskId.LastIndexOf('.');
                                string copyWorkoutName = localTaskId.Substring(0, dotIndex);
                                string copyFieldName = dotIndex + 1 < localTaskId.Length ? localTaskId.Substring(dotIndex + 1) : string.Empty;

                                if (!game.TryGetWorkout(copyWorkoutName, out var copyFromWorkout))
                                {
                                    swole.LogError($"({injector.Name}:{field.Name}[{a}]) Tried to copy task field '{copyFieldName}' from nonexistent workout '{copyWorkoutName}'");
                                    continue;
                                }

                                if (!string.IsNullOrWhiteSpace(copyFieldName))
                                {
                                    var copyFromField = typeof(workoutGameTest.Workout).GetField(copyFieldName);
                                    if (copyFromField != null)
                                    {
                                        int[] existingTaskArray = copyFromField.GetValue(copyFromWorkout) == null ? null : (int[])copyFromField.GetValue(copyFromWorkout);
                                        targetTaskArray[a] = existingTaskArray[a];
                                    }
                                    else swole.LogError($"({injector.Name}:{field.Name}[{a}]) Tried to copy an unrecognized task field '{copyFieldName}' from workout '{copyWorkoutName}'");
                                }
                                else swole.LogError($"({injector.Name}:{field.Name}[{a}]) Tried to copy a nameless task field from workout '{copyWorkoutName}'");
                            }
                            else if (injector.TryFindTask(localTaskId, out var taskToInject))
                            {
                                targetTaskArray[a] = taskToInject.binding.boundIndex; 
                            }
                            else
                            {
                                targetTaskArray[a] = -1;
                                swole.LogError($"({injector.Name}:{field.Name}[{a}]) Task '{localTaskId}' was not found!");
                            }
                        }
                    }
                    fieldToOverride.SetValue(workout, targetTaskArray);
                }
                else if (fieldToOverride.FieldType.IsEnum)
                {
                    if (overridable is OverridableInt oint)
                    {
                        fieldToOverride.SetValue(workout, Enum.ToObject(fieldToOverride.FieldType, oint.Value));
                    }
                    else if (overridable is OverridableString ostr)
                    {
                        fieldToOverride.SetValue(workout, Enum.Parse(fieldToOverride.FieldType, ostr.value, true));
                    }
                    else swole.LogError($"({injector.Name}:{field.Name}) Tried to set enum of type '{fieldToOverride.FieldType.Name}' using invalid type '{field.FieldType.Name}'");
                }
                else if (fieldToOverride.FieldType.IsArray && fieldToOverride.FieldType.GetElementType().IsEnum)
                {
                    if (overridable is OverridableInt.Array ointArray)
                    {
                        if (ointArray.value != null)
                        {
                            var newArray = Array.CreateInstance(fieldToOverride.FieldType, ointArray.value.Length);
                            for (int a = 0; a < ointArray.value.Length; a++)
                            {
                                try
                                {
                                    newArray.SetValue(Enum.ToObject(fieldToOverride.FieldType.GetElementType(), ointArray.value[a]), a);
                                }
                                catch
                                {
                                    swole.LogError($"({injector.Name}:{field.Name}[{a}]) Failed to convert integer '{ointArray.value[a]}' to enum type '{fieldToOverride.FieldType.GetElementType().Name}'");
                                }
                            }
                            fieldToOverride.SetValue(workout, newArray);
                        }
                        else
                        {
                            fieldToOverride.SetValue(workout, null);
                            swole.LogWarning($"({injector.Name}:{field.Name}) Array was null!");
                        }
                    }
                    else if (overridable is OverridableString.Array ostrArray)
                    {
                        if (ostrArray.value != null)
                        {
                            var newArray = Array.CreateInstance(fieldToOverride.FieldType, ostrArray.value.Length);
                            for (int a = 0; a < ostrArray.value.Length; a++)
                            {
                                try
                                {
                                    newArray.SetValue(Enum.Parse(fieldToOverride.FieldType.GetElementType(), ostrArray.value[a]), a);
                                }
                                catch
                                {
                                    swole.LogError($"({injector.Name}:{field.Name}[{a}]) Failed to parse string '{ostrArray.value[a]}' as enum type '{fieldToOverride.FieldType.GetElementType().Name}'");
                                }
                            }
                            fieldToOverride.SetValue(workout, newArray);
                        }
                        else
                        {
                            fieldToOverride.SetValue(workout, null);
                            swole.LogWarning($"({injector.Name}:{field.Name}) Array was null!");
                        }
                    }
                    else swole.LogError($"({injector.Name}:{field.Name}) Tried to set enum of type '{fieldToOverride.FieldType.Name}' using invalid type '{field.FieldType.Name}'");
                }
                else if (typeof(string).IsAssignableFrom(fieldToOverride.FieldType))
                {
                    if (overridable is OverridableString ostr)
                    {
                        fieldToOverride.SetValue(workout, overridable.Value);
                    }
                    else
                    {
                        fieldToOverride.SetValue(workout, overridable.Value.ToString());
                    }
                }
                /*else if (typeof(Vector2).IsAssignableFrom(fieldToOverride.FieldType))
                {
                    if (overridable.Value is EngineInternal.Vector2 inV2)
                    {
                        fieldToOverride.SetValue(workout, UnityEngineHook.AsUnityVector(inV2));
                    } 
                    else
                    {
                        fieldToOverride.SetValue(workout, overridable.Value);
                    }
                }
                else if (typeof(Vector3).IsAssignableFrom(fieldToOverride.FieldType))
                {
                    if (overridable.Value is EngineInternal.Vector3 inV3)
                    {
                        fieldToOverride.SetValue(workout, UnityEngineHook.AsUnityVector(inV3));
                    }
                    else
                    {
                        fieldToOverride.SetValue(workout, overridable.Value);
                    }
                }
                else if (typeof(Vector4).IsAssignableFrom(fieldToOverride.FieldType))
                {
                    if (overridable.Value is EngineInternal.Vector4 inV4)
                    {
                        fieldToOverride.SetValue(workout, UnityEngineHook.AsUnityVector(inV4));
                    }
                    else
                    {
                        fieldToOverride.SetValue(workout, overridable.Value);
                    }
                }
                else if (typeof(Quaternion).IsAssignableFrom(fieldToOverride.FieldType))
                {
                    if (overridable.Value is EngineInternal.Quaternion inQ)
                    {
                        fieldToOverride.SetValue(workout, UnityEngineHook.AsUnityQuaternion(inQ));
                    }
                    else
                    {
                        fieldToOverride.SetValue(workout, overridable.Value);
                    }
                }*/
                else
                {
                    try
                    {
                        fieldToOverride.SetValue(workout, UnityEngineHook.ConvertToUnityType(overridable.Value));
                    }
                    catch
                    {
                        try
                        {
                            fieldToOverride.SetValue(workout, UnityEngineHook.ConvertToSwoleType(overridable.Value));
                        }
                        catch
                        {
                            fieldToOverride.SetValue(workout, overridable.Value);
                        }
                    }
                }
            }
        }

    }

}

