using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using Swole;
using Swole.API.Unity;
using Swole.UI;
using Swole.Modding;

using TriLibCore.SFB;

namespace BulkOut
{

    public class workoutLoader : MonoBehaviour
    {

        public const string prefix_workoutAssetFile = "workout.";
        public const string prefix_workoutThumbnail = "thumbnail."; 

        public const string prefix_animationAsset = "anim:";
        public const string prefix_soundAsset = "sound:";

        public void OpenURL(string url)
        {
            Application.OpenURL(url);
        }

        [Serializable]
        public enum NativeWorkout
        {
            preacher_curls, shoulder_press, tricep_pushdown, bench_press
        }

        public animatedObjectData workoutAnimatedObject;
        public nativeWorkoutData[] nativeWorkouts;
        public bool TryGetNativeWorkout(int workoutId, out workoutGameTest.Workout workout) => TryGetNativeWorkout((NativeWorkout)workoutId, out workout);
        public bool TryGetNativeWorkout(NativeWorkout workoutId, out workoutGameTest.Workout workout) => TryGetNativeWorkout(workoutId.ToString(), out workout);
        public bool TryGetNativeWorkout(string workoutId, out workoutGameTest.Workout workout)
        {
            workout = null;
            if (nativeWorkouts == null || string.IsNullOrWhiteSpace(workoutId)) return false;

            workoutId = workoutId.AsID();

            foreach(var nativeWorkout in nativeWorkouts)
            {
                if (nativeWorkout == null || nativeWorkout.data == null) continue;

                if (nativeWorkout.data.name.AsID() == workoutId || nativeWorkout.name.AsID() == workoutId)
                {
                    workout = nativeWorkout.data; 
                    return true;
                }
            }

            return false;
        }

        public UIPopupMessageFadable infoMessage;

        public UIRecyclingList localWorkoutList;
        public UIRecyclingList importedWorkoutList;
        public Color listMemberDefaultColor;
        public Color listMemberSelectedColor;

        public Sprite defaultThumbnail;

        public GameObject packageCreatorWindow;

        public Button exportAllSelectedWorkoutsButton;

        public RectTransform windowRoot;
        public SceneSwap sceneSwapper;
        public int gameSceneIndex;

        public RectTransform createNewWorkoutWindow;
        public RectTransform editWorkoutWindow;
        public RectTransform transferToPackageWindow;
        public void OpenTransferToPackageWindow() => OpenTransferToPackageWindow(transferToPackageWindow);
        public void OpenTransferToPackageWindow(RectTransform transferToPackageWindow)
        {
            if (transferToPackageWindow == null || editedWorkout == null || editedWorkout.injector == null) return;

            transferToPackageWindow.gameObject.SetActive(true);
            UIPopupMessageFadable errorMessage = transferToPackageWindow.GetComponentInChildren<UIPopupMessageFadable>(true);

            CustomEditorUtils.SetButtonOnClickActionByName(transferToPackageWindow, "transfer", () =>
            {
                string pkg = CustomEditorUtils.GetInputOrTextComponentTextByName(transferToPackageWindow, "package");
                if (!ContentManager.TryFindLocalPackage(new PackageIdentifier(pkg), out var lpkg))
                {
                    if (errorMessage != null) errorMessage.SetMessageAndShow($"Local package '{pkg}' was not found!");
                    return;
                }

                try
                {
                    SwolePackage spkg;
                    ImageAsset thumbnail = null;
                    if (ContentManager.TryFindLocalPackage(new PackageIdentifier(editedWorkout.package), out var originalPackage) && !ReferenceEquals(lpkg, originalPackage))
                    {
                        originalPackage.Content.TryFind(out thumbnail, $"{prefix_workoutThumbnail}{editedWorkout.injector.Name}", true);

                        spkg = SwolePackage.Create(originalPackage);
                        spkg.RemoveAll(i => i == null || ((i.Name == editedWorkout.injector.Name || i.Name == name || i.Name == $"{prefix_workoutAssetFile}{editedWorkout.injector.Name}") && (typeof(workoutInjector).IsAssignableFrom(i.GetType()) || typeof(workoutInjector.Serialized).IsAssignableFrom(i.GetType()) || typeof(JsonData).IsAssignableFrom(i.GetType()) || typeof(JsonData.Serialized).IsAssignableFrom(i.GetType()))));
                        if (thumbnail != null) spkg.Remove(thumbnail, true, false);
                        originalPackage.Content = spkg;

                        ContentManager.SavePackage(originalPackage);

                        try
                        {
                            if (thumbnail != null)
                            {
                                if (!string.IsNullOrWhiteSpace(thumbnail.imagePath))
                                {
                                    var thumbnailPath = Path.Combine(originalPackage.workingDirectory.FullName, thumbnail.imagePath);
                                    if (File.Exists(thumbnailPath))
                                    {
                                        var newThumbnailPath = Path.Combine(lpkg.workingDirectory.FullName, thumbnail.imagePath);
                                        File.Move(thumbnailPath, newThumbnailPath);
                                    }
                                }

                                spkg = SwolePackage.Create(lpkg);
                                spkg.AddOrReplace(thumbnail); 
                                lpkg.Content = spkg;
                            }
                        } 
                        catch(Exception ex)
                        {
                            swole.LogError(ex);
                        }
                    }

                    editedWorkout.package = lpkg.GetIdentityString(); 
                    StartEditingWorkout(editedWorkout);   
                
                    transferToPackageWindow.gameObject.SetActive(false); 
                } 
                catch(Exception ex)
                {
                    if (errorMessage != null) errorMessage.SetMessageAndShow(ex.Message);
                }

            });
        }

        public RectTransform localWorkoutsWindow;
        public RectTransform importedWorkoutsWindow;

        public RectTransform assetPropertyEditorRoot;
        public RectTransform taskPropertyEditorRoot;
        public RectTransform workoutPropertyEditorRoot;
        protected ObjectPropertyEditor assetPropertyEditor;
        protected ObjectPropertyEditor taskPropertyEditor;
        protected ObjectPropertyEditor workoutPropertyEditor; 

        public RectTransform overridablePropertyListRoot;
        protected UIRecyclingList overridablePropertyList;
        public void OpenOverridablePropertyList()
        {
            if (editedWorkout == null || editedWorkout.injector == null || overridablePropertyListRoot == null) return;
            if (overridablePropertyList == null) overridablePropertyList = overridablePropertyListRoot.GetComponentInChildren<UIRecyclingList>(true);
            if (overridablePropertyList == null) return;

            overridablePropertyListRoot.gameObject.SetActive(true);

            workoutGameTest.Workout nativeWorkout = null;
            if (!TryGetNativeWorkout(editedWorkout.injector.referenceWorkout, out nativeWorkout)) nativeWorkout = null;
        
            overridablePropertyList.Clear();
            var workoutFields = typeof(workoutParameters).GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (workoutFields != null)
            { 
                object workoutParams = editedWorkout.injector.parameters;
                foreach (var field in workoutFields)
                {
                    if (!typeof(IOverridable).IsAssignableFrom(field.FieldType)) continue;

                    IOverridable currentVal = (IOverridable)field.GetValue(workoutParams);
                    if (currentVal.IsOverridden) continue;

                    var aliasAttr = field.GetCustomAttribute<EditableAliasAttribute>(); 
                 
                    overridablePropertyList.AddNewMember(aliasAttr == null ? field.Name : aliasAttr.Alias, () =>
                    {
                        overridablePropertyListRoot.gameObject.SetActive(false);
                        currentVal.IsOverridden = true;
                        if (nativeWorkout != null)
                        {
                            try
                            {
                                var nativeField = typeof(workoutGameTest.Workout).GetField(field.Name);
                                if (nativeField != null)
                                {
                                    if (typeof(OverridableTask).IsAssignableFrom(field.FieldType))
                                    {
                                        int referenceTaskIndex = (int)nativeField.GetValue(nativeWorkout);
                                        if (referenceTaskIndex >= 0) currentVal.Value = $"{workoutInjector.prefix_native}{workoutAnimatedObject.data.tasks[referenceTaskIndex].name}";
                                    }
                                    else if (typeof(OverridableTask.Array).IsAssignableFrom(field.FieldType))
                                    {
                                        var referenceArray = ReferenceEquals(nativeField.GetValue(nativeWorkout), null) ? null : (int[])nativeField.GetValue(nativeWorkout);
                                        if (referenceArray != null)
                                        {
                                            var localArray = new string[referenceArray.Length];
                                            for (int a = 0; a < referenceArray.Length; a++)
                                            {
                                                int referenceTaskIndex = referenceArray[a];
                                                if (referenceTaskIndex >= 0) localArray[a] = $"{workoutInjector.prefix_native}{workoutAnimatedObject.data.tasks[referenceTaskIndex].name}";  
                                            }
                                            currentVal.Value = localArray; 
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            currentVal.Value = nativeField.GetValue(UnityEngineHook.ConvertToSwoleType(nativeWorkout));
                                        } 
                                        catch
                                        {
                                            try
                                            {
                                                currentVal.Value = nativeField.GetValue(UnityEngineHook.ConvertToUnityType(nativeWorkout));
                                            }
                                            catch
                                            {
                                                try
                                                {
                                                    currentVal.Value = nativeField.GetValue(nativeWorkout);
                                                } 
                                                catch(Exception ex)
                                                {
    #if UNITY_EDITOR
                                                    Debug.LogError($"Invalid cast for overridable field {field.Name}! Tried to cast native type {nativeField.FieldType.Name} to overridable type {field.FieldType.Name}");
                                                    Debug.LogException(ex);
    #endif
                                                }
                                            }
                                        }
                                    }
                                }
                            } 
                            catch(Exception ex) 
                            {
    #if UNITY_EDITOR
                                Debug.LogException(ex);
    #endif
                            }
                        }
                        field.SetValue(workoutParams, currentVal);
                        editedWorkout.injector.parameters = (workoutParameters)workoutParams;
                        StartEditingWorkoutParameters(editedWorkout); 
                    }, 
                    false/*, (UIRecyclingList.MemberData mem, GameObject instance) =>
                    {
                     
                    }*/);
                }

                overridablePropertyList.Refresh();

                CustomEditorUtils.SetInputFieldOnValueChangeActionByName(overridablePropertyListRoot.gameObject, "Search", (string str) => overridablePropertyList.FilterMembersByStartString(str, false, true));
            }
        }

        public RectTransform yesNoPrompt; 

        protected WorkoutAsset editedWorkout;

        public void SaveEditedWorkout() => SaveEditedWorkout(editedWorkout);
        public void SaveEditedWorkout(WorkoutAsset asset)
        {
            if (ReferenceEquals(asset, null)) return;
            asset.Save();
        }
        public void RenameAndSaveEditedWorkout(string newId) => RenameAndSaveEditedWorkout(editedWorkout, newId);
        public void RenameAndSaveEditedWorkout(WorkoutAsset asset, string newId)
        {
            if (ReferenceEquals(asset, null)) return; 
            asset.RenameAndSave(newId);  
        }

        public void AddNewWorkoutThumbnail(Image display) => AddNewWorkoutThumbnail(editedWorkout, display);
        public void AddNewWorkoutThumbnail(WorkoutAsset asset, Image display)
        {
            if (asset == null)
            {
                if (display != null) display.sprite = defaultThumbnail;
                return;
            }

            asset.AddNewWorkoutThumbnail((Sprite sprite, bool isCompressed, bool isLinear, ExternalAssets.ILoaderContext context) =>
            {
                if (display != null) display.sprite = sprite == null ? defaultThumbnail : sprite;
            }); 
        }

        protected float saveWorkoutTimer;

        protected virtual void AddFieldValueConverters(Dictionary<string, ObjectPropertyEditor.FieldValueConverter> fieldValueConverters, Dictionary<string, ObjectPropertyEditor.ObjectWithContextConverter> objectWithContextConverters)
        {
            AddFieldValueConverter(GameStateController.converterId_assetId, fieldValueConverters, objectWithContextConverters);
            AddFieldValueConverter(GameStateController.converterId_animationId, fieldValueConverters, objectWithContextConverters);
            AddFieldValueConverter(GameStateController.converterId_taskId, fieldValueConverters, objectWithContextConverters); 
        }
        protected virtual void AddFieldValueConverter(string converterId, Dictionary<string, ObjectPropertyEditor.FieldValueConverter> fieldValueConverters, Dictionary<string, ObjectPropertyEditor.ObjectWithContextConverter> objectWithContextConverters)
        {
            if (string.IsNullOrWhiteSpace(converterId)) return;

            Type fieldValueConversionInputType = null;
            Type objectWithContextConversionInputType = null;

            ObjectPropertyEditor.ConvertFieldDelegate delConvertFieldValue = null;
            ObjectPropertyEditor.ConvertObjectWithContextDelegate delConverObjectWithContext = null;

            switch (converterId)
            {
                case GameStateController.converterId_assetId:
                    fieldValueConversionInputType = typeof(string);
                    objectWithContextConversionInputType = typeof(int);
                    delConvertFieldValue = (ObjectPropertyEditor.EditorInstance.CustomFieldInfo[] fieldInfoChain, string fieldName, object val) =>
                    {
                        if (val is string strId)
                        {
                            bool isNative = false;
                            if (strId.StartsWith(workoutInjector.prefix_native))
                            {
                                isNative = true;
                                strId = strId.Substring(workoutInjector.prefix_native.Length);
                            }

                            if (strId.StartsWith(workoutInjector.prefix_copyNative))
                            {
                                strId = strId.Substring(workoutInjector.prefix_copyNative.Length);
                                if (TryGetNativeWorkout(strId, out var workout))
                                {
                                    var fieldToCopy = typeof(workoutGameTest.Workout).GetField(fieldName);
                                    if (fieldToCopy != null) return (-1 - (int)System.Convert.ChangeType(fieldToCopy.GetValue(workout), typeof(int))); else return default;
                                }
                            }

                            if (strId.StartsWith(prefix_animationAsset))
                            {
                                strId = strId.Substring(prefix_animationAsset.Length);
                                if (isNative)
                                {
                                    if (workoutAnimatedObject.data.TryGetAnimation(strId, out _, out int index)) return -1 - index;
                                }
                                else if (editedWorkout.injector.TryGetAnimation(strId, out _, out int index)) return index;
                            }
                            else if (strId.StartsWith(prefix_soundAsset))
                            {
                                strId = strId.Substring(prefix_soundAsset.Length);
                                if (isNative)
                                {
                                    if (workoutAnimatedObject.data.TryGetSound(strId, out _, out int index)) return -1 - index;
                                }
                                else if (editedWorkout.injector.TryGetSound(strId, out _, out int index)) return index;
                            }
                            else
                            {
                                if (isNative)
                                {
                                    if (workoutAnimatedObject.data.TryGetAsset(strId, out _, out int index)) return -1 - index;
                                }
                                else if (editedWorkout.injector.TryGetAsset(strId, out _, out int index)) return index;
                            }

                        }

                        return default;
                    };
                    delConverObjectWithContext = (ObjectPropertyEditor.EditorInstance.CustomFieldInfo[] fieldInfoChain, object input, object context) =>
                    {
                        bool isNative = false;
                        object output = input;
                        if (input is int intVal)
                        {
                            isNative = intVal < 0;
                            if (isNative) intVal = 1 - intVal; 

                            if (!ReferenceEquals(fieldInfoChain, null))
                            {
                                for (int a = fieldInfoChain.Length - 1; a >= 0; a--)
                                {
                                    var info = fieldInfoChain[a];
                                    if (typeof(Array).IsAssignableFrom(info.FieldType))
                                    {
                                        if (info.HasFieldInfo)
                                        {
                                            var attr = info.FieldInfo.GetCustomAttribute<PropertyTypeConverterAttribute>(false);
                                            if (attr != null)
                                            {
                                                if (attr.ConverterID == GameStateController.converterId_animationIdCollection)
                                                {
                                                    if (isNative) output = (intVal < 0 || intVal >= workoutAnimatedObject.data.animations.Length ? null : workoutAnimatedObject.data.animations[intVal]); else output = (intVal < 0 || intVal >= editedWorkout.injector.animations.Length ? null : editedWorkout.injector.animations[intVal]);
                                                }
                                                else if (attr.ConverterID == GameStateController.converterId_soundIdCollection) 
                                                {
                                                    if (isNative) output = (intVal < 0 || intVal >= workoutAnimatedObject.data.sounds.Length ? null : workoutAnimatedObject.data.sounds[intVal]); else output = (intVal < 0 || intVal >= editedWorkout.injector.sounds.Length ? null : editedWorkout.injector.sounds[intVal]); 
                                                }
                                            }
                                        }
                                        break;
                                    }
                                    else if (typeof(IList).IsAssignableFrom(info.FieldType))
                                    {
                                        if (info.HasFieldInfo)
                                        {
                                            var attr = info.FieldInfo.GetCustomAttribute<PropertyTypeConverterAttribute>(false);
                                            if (attr != null)
                                            {
                                                if (attr.ConverterID == GameStateController.converterId_animationIdCollection)
                                                {
                                                    if (isNative) output = (intVal < 0 || intVal >= workoutAnimatedObject.data.animations.Length ? null : workoutAnimatedObject.data.animations[intVal]); else output = (intVal < 0 || intVal >= editedWorkout.injector.animations.Length ? null : editedWorkout.injector.animations[intVal]);
                                                }
                                                else if (attr.ConverterID == GameStateController.converterId_soundIdCollection)
                                                {
                                                    if (isNative) output = (intVal < 0 || intVal >= workoutAnimatedObject.data.sounds.Length ? null : workoutAnimatedObject.data.sounds[intVal]); else output = (intVal < 0 || intVal >= editedWorkout.injector.sounds.Length ? null : editedWorkout.injector.sounds[intVal]);
                                                }
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }

                        if (output is Animation anim) return $"{prefix_animationAsset}{(anim.binding == null || string.IsNullOrWhiteSpace(anim.binding.id) ? (anim.settings == null ? "null" : (string.IsNullOrWhiteSpace(anim.settings.name) ? (string.IsNullOrWhiteSpace(anim.settings.displayName) ? "null" : anim.settings.displayName) : anim.settings.name)) : anim.binding.id)}";
                        if (output is Sound sound) return $"{prefix_soundAsset}{(sound.binding == null || string.IsNullOrWhiteSpace(sound.binding.id) ? "null" : sound.binding.id)}";

                        if (output is GameStateController.ImportedAnimation animObj) return $"{(isNative ? workoutInjector.prefix_native : string.Empty)}{prefix_animationAsset}{(string.IsNullOrWhiteSpace(animObj.name) ? (string.IsNullOrWhiteSpace(animObj.displayName) ? "null" : animObj.displayName) : animObj.name)}";
                        if (output is GameStateController.Sound soundObj) return $"{(isNative ? workoutInjector.prefix_native : string.Empty)}{prefix_soundAsset}{(soundObj.clip == null ? "null" : soundObj.clip.name)}";

                        return output;
                    };
                    break;

                case GameStateController.converterId_animationId:
                    fieldValueConversionInputType = typeof(string);
                    objectWithContextConversionInputType = typeof(int);
                    delConvertFieldValue = (ObjectPropertyEditor.EditorInstance.CustomFieldInfo[] fieldInfoChain, string fieldName, object val) =>
                    {
                        if (val is string strId)
                        {
                            if (strId.StartsWith(workoutInjector.prefix_copyNative))
                            {
                                strId = strId.Substring(workoutInjector.prefix_copyNative.Length);
                                if (TryGetNativeWorkout(strId, out var workout))
                                {
                                    var fieldToCopy = typeof(workoutGameTest.Workout).GetField(fieldName);
                                    if (fieldToCopy != null) return (-1 - (int)System.Convert.ChangeType(fieldToCopy.GetValue(workout), typeof(int))); else return null;
                                }
                            }
                            else if (strId.StartsWith(workoutInjector.prefix_native))
                            {
                                strId = strId.Substring(workoutInjector.prefix_native.Length);
                                if (workoutAnimatedObject.data.TryGetAnimation(strId, out _, out int index)) return -1 - index;
                            }
                            else if (editedWorkout.injector.TryGetAnimation(strId, out _, out int index)) return index;
                        }

                        return null;
                    };
                    delConverObjectWithContext = (ObjectPropertyEditor.EditorInstance.CustomFieldInfo[] fieldInfoChain, object input, object context) =>
                    {
                        bool isNative = false;
                        object output = input;
                        if (input is int intVal)
                        {
                            isNative = intVal < 0;
                            if (isNative) 
                            { 
                                intVal = 1 - intVal;
                                if (intVal >= 0 && intVal < workoutAnimatedObject.data.animations.Length) output = workoutAnimatedObject.data.animations[intVal]; else output = null;
                            } 
                            else
                            {
                                if (intVal >= 0 && intVal < editedWorkout.injector.animations.Length) output = editedWorkout.injector.animations[intVal]; else output = null;
                            }
                        }             

                        if (output is Animation anim) return $"{(isNative ? workoutInjector.prefix_native : string.Empty)}{(anim.binding == null || string.IsNullOrWhiteSpace(anim.binding.id) ? (anim.settings == null ? "null" : (string.IsNullOrWhiteSpace(anim.settings.name) ? (string.IsNullOrWhiteSpace(anim.settings.displayName) ? "null" : anim.settings.displayName) : anim.settings.name)) : anim.binding.id)}";
                        if (output is GameStateController.ImportedAnimation animObj) return $"{(isNative ? workoutInjector.prefix_native : string.Empty)}{(animObj == null ? "null" : (string.IsNullOrWhiteSpace(animObj.name) ? (string.IsNullOrWhiteSpace(animObj.displayName) ? "null" : animObj.displayName) : animObj.name))}";

                        return output;
                    };
                    break;
                case GameStateController.converterId_taskId:
                    fieldValueConversionInputType = typeof(string);
                    objectWithContextConversionInputType = typeof(int); 
                    delConvertFieldValue = (ObjectPropertyEditor.EditorInstance.CustomFieldInfo[] fieldInfoChain, string fieldName, object val) =>
                    {
                        if (val is string strId)
                        {
                            if (strId.StartsWith(workoutInjector.prefix_copyNative))
                            {
                                strId = strId.Substring(workoutInjector.prefix_copyNative.Length);
                                if (TryGetNativeWorkout(strId, out var workout))
                                {
                                    var fieldToCopy = typeof(workoutGameTest.Workout).GetField(fieldName);
                                    if (fieldToCopy != null) return (-1 - (int)System.Convert.ChangeType(fieldToCopy.GetValue(workout), typeof(int))); else return default;
                                }
                            }
                            else if (strId.StartsWith(workoutInjector.prefix_native))
                            {
                                strId = strId.Substring(workoutInjector.prefix_native.Length);
                                if (workoutAnimatedObject.data.TryGetTask(strId, out _, out int index)) return -1 - index;
                            }
                            else if (editedWorkout.injector.TryGetTask(strId, out _, out int index)) return index;
                        }

                        return default;
                    };
                    delConverObjectWithContext = (ObjectPropertyEditor.EditorInstance.CustomFieldInfo[] fieldInfoChain, object input, object context) =>
                    {
                        bool isNative = false;
                        object output = input; 
                        if (input is int intVal)
                        {
                            isNative = intVal < 0;
                            if (isNative)
                            {
                                intVal = 1 - intVal;
                                if (intVal >= 0 && intVal < workoutAnimatedObject.data.tasks.Length) output = workoutAnimatedObject.data.tasks[intVal]; else output = null;
                            }
                            else
                            {
                                if (intVal >= 0 && intVal < editedWorkout.injector.tasks.Length) output = editedWorkout.injector.tasks[intVal]; else output = null;
                            } 
                        }

                        if (output is Task task) return $"{(isNative ? workoutInjector.prefix_native : string.Empty)}{(task.binding == null || string.IsNullOrWhiteSpace(task.binding.id) ? (task.settings == null ? "null" : (string.IsNullOrWhiteSpace(task.settings.name) ? "null" : task.settings.name)) : task.binding.id)}";
                        if (output is GameStateController.AnimationTask taskObj) return $"{(isNative ? workoutInjector.prefix_native : string.Empty)}{(taskObj == null ? "null" : (string.IsNullOrWhiteSpace(taskObj.name) ? "null" : taskObj.name))}";

                        return output;
                    };
                    break;
            }

            if (fieldValueConversionInputType != null && delConvertFieldValue != null) fieldValueConverters[converterId] = new ObjectPropertyEditor.FieldValueConverter(fieldValueConversionInputType, delConvertFieldValue);
            if (objectWithContextConversionInputType != null && delConverObjectWithContext != null) objectWithContextConverters[converterId] = new ObjectPropertyEditor.ObjectWithContextConverter(objectWithContextConversionInputType, delConverObjectWithContext);
        }

        [Serializable]
        public enum AnimatorId
        {
            Character, Bulkinator, Camera
        }
        protected virtual void AddEnumIdsToTypes(Dictionary<string, Type> enumIdsToTypes)
        {
            enumIdsToTypes[GameStateController.enumId_animatorIndex] = typeof(AnimatorId); 
        }

        public void StartEditingWorkout(WorkoutAsset asset)
        {
            if (editedWorkout != null) editedWorkout.StopEditing();

            editedWorkout = asset;
            if (asset == null)
            {
                editWorkoutWindow.gameObject.SetActive(false);
                return;
            }

            editWorkoutWindow.gameObject.SetActive(true);

            var popup = editWorkoutWindow.GetComponentInChildren<UIPopup>();
            if (popup != null)
            {
                if (popup.OnClose == null) popup.OnClose = new UnityEngine.Events.UnityEvent(); else popup.OnClose.RemoveAllListeners();
                popup.OnClose.AddListener(StopEditingWorkout);
            }

            if (asset.injector == null) asset.injector = new workoutInjector();
            if (asset.injector.animations == null) asset.injector.animations = new Animation[0];
            if (asset.injector.tasks == null) asset.injector.tasks = new Task[0];

            var identity = editWorkoutWindow.FindDeepChildLiberal("identity");
            if (identity != null)
            {
                try
                {
                    var thumbnailRoot = identity.FindDeepChildLiberal("thumbnail");
                    if (thumbnailRoot != null)
                    {

                        ImageAsset imgAsset = null;
                        if (!string.IsNullOrWhiteSpace(asset.package) && ContentManager.TryFindLocalPackage(new PackageIdentifier(asset.package), out var lpkg))
                        {
                            lpkg.Content.TryFind(out imgAsset, $"{prefix_workoutThumbnail}{asset.injector.Name}", true);
                        }

                        var thumbnailButton = thumbnailRoot.GetComponentInChildren<Button>();
                        if (thumbnailButton != null)
                        {
                            if (thumbnailButton.onClick == null) thumbnailButton.onClick = new Button.ButtonClickedEvent(); else thumbnailButton.onClick.RemoveAllListeners();
                            thumbnailButton.onClick.AddListener(() => AddNewWorkoutThumbnail(asset, thumbnailButton.image));
                            if (thumbnailButton.image != null)
                            {
                                if (imgAsset != null) thumbnailButton.image.sprite = imgAsset.Sprite == null ? defaultThumbnail : imgAsset.Sprite; else thumbnailButton.image.sprite = defaultThumbnail;
                            }
                        }
                    }
                } 
                catch(Exception ex)
                {
                    swole.LogError(ex);
                }

                CustomEditorUtils.SetInputFieldOnEndEditActionByName(identity, "ID", (string newId) =>
                {
                    if (asset.injector == null) return;

                    try
                    {
                        if (newId != asset.injector.Name && !string.IsNullOrWhiteSpace(asset.package) && ContentManager.TryFindLocalPackage(new PackageIdentifier(asset.package), out var lpkg))
                        {
                            bool flag = true;
                            int i = 1;
                            string incrementedId = newId;
                            while (flag)
                            {
                                flag = false;
                                string fileName = $"{prefix_workoutAssetFile}{incrementedId}";
                                if (lpkg.Content.TryFind<workoutInjector>(out _, incrementedId, true) || lpkg.Content.TryFind<JsonData>(out _, incrementedId, true) || lpkg.Content.TryFind<workoutInjector>(out _, fileName, true) || lpkg.Content.TryFind<JsonData>(out _, fileName, true)) 
                                {
                                    i++;
                                    incrementedId = $"{incrementedId}({i})"; 
                                    flag = true;
                                    continue;
                                }
                            }

                            try
                            {
                                if (newId != incrementedId)
                                {
                                    CustomEditorUtils.SetInputFieldTextByName(identity, "ID", incrementedId, true, true); 
                                }
                            }
                            catch (Exception ex)
                            {
                                swole.LogError(ex);
                            }

                            newId = incrementedId;
                        }
                    } 
                    catch(Exception ex)
                    {
                        swole.LogError(ex);
                    }

                    RenameAndSaveEditedWorkout(asset, newId);
                });
                CustomEditorUtils.SetInputFieldTextByName(identity, "ID", asset.injector == null ? string.Empty : asset.injector.Name);

                CustomEditorUtils.SetInputFieldOnEndEditActionByName(identity, "DisplayName", (string newDisplayName) =>
                {
                    if (asset.injector == null) return;

                    var param = asset.injector.parameters;
                    param.displayName = newDisplayName;
                    asset.injector.parameters = param;

                    saveWorkoutTimer = 0.5f;  
                });
                CustomEditorUtils.SetInputFieldTextByName(identity, "DisplayName", asset.injector == null ? string.Empty : asset.injector.parameters.displayName);

                var dropdownRoot = identity.FindDeepChildLiberal("ReferenceWorkout");
                if (dropdownRoot != null)
                {
                    var dropdown = dropdownRoot.GetComponentInChildren<UIDynamicDropdown>(true);
                    if (dropdown.OnSelectionChanged == null) dropdown.OnSelectionChanged = new UnityEngine.Events.UnityEvent<string>(); else dropdown.OnSelectionChanged.RemoveAllListeners();
                    dropdown.OnSelectionChanged.AddListener((string val) =>
                    {
                        asset.injector.referenceWorkout = val; 

                        saveWorkoutTimer = 0.5f;
                    });
                    dropdown.SetSelectionText(asset.injector == null ? NativeWorkout.preacher_curls.ToString() : asset.injector.referenceWorkout, false); 
                }
            }
            var settings = editWorkoutWindow.FindDeepChildLiberal("settings");
            if (settings != null)
            {
                CustomEditorUtils.SetComponentTextByName(settings, "LocalPackageName", asset.package); 
                CustomEditorUtils.SetInputFieldOnEndEditActionByName(settings, "DefaultAssetPackage", (string newAssetPackage) =>
                {
                    if (asset.injector == null) return;

                    asset.injector.defaultAssetPackage = newAssetPackage;

                    saveWorkoutTimer = 0.5f;
                });
                CustomEditorUtils.SetInputFieldTextByName(settings, "DefaultAssetPackage", asset.injector == null ? string.Empty : asset.injector.defaultAssetPackage);  
            }

            var workout = editedWorkout;
            StartEditingWorkoutAnimations(workout);
            StartEditingWorkoutTasks(workout);
            StartEditingWorkoutParameters(workout);
        }
        protected void StartEditingWorkoutAnimations(WorkoutAsset workout)
        {
            if (workout == null) return;
            assetPropertyEditor.StartEditingObject(workout.injector.animations, (object val) => { workout.injector.animations = (Animation[])val; saveWorkoutTimer = 0.5f; }, () => workout.injector.animations, () => saveWorkoutTimer = 1f, null, AddFieldValueConverters, AddEnumIdsToTypes);
        }
        protected void StartEditingWorkoutTasks(WorkoutAsset workout)
        {
            if (workout == null) return;
            taskPropertyEditor.StartEditingObject(workout.injector.tasks, (object val) => { workout.injector.tasks = (Task[])val; saveWorkoutTimer = 0.5f; }, () => workout.injector.tasks, () => saveWorkoutTimer = 1f, null, AddFieldValueConverters, AddEnumIdsToTypes);
        }
        protected void StartEditingWorkoutParameters(WorkoutAsset workout)
        {
            if (workout == null) return;
            workoutPropertyEditor.StartEditingObject(workout.injector.parameters, (object val) => { workout.injector.parameters = (workoutParameters)val; saveWorkoutTimer = 0.5f; }, () => workout.injector.parameters, () => saveWorkoutTimer = 1f, null, AddFieldValueConverters, AddEnumIdsToTypes);
        } 
        public void StopEditingWorkout()
        {
            if (editedWorkout != null) editedWorkout.StopEditing();
            editedWorkout = null;
            editWorkoutWindow.gameObject.SetActive(false);

            RefreshLocalWorkoutAssets(); 
        }

        protected virtual void Update()
        {
            if (saveWorkoutTimer > 0)
            {
                saveWorkoutTimer -= Time.deltaTime;
                if (saveWorkoutTimer < 0)
                {
                    SaveEditedWorkout(); 
                }
            }

            if (exportAllSelectedWorkoutsButton != null)
            {
                bool interactable = false;
                if (localWorkoutAssets != null)
                {
                    foreach(var asset in localWorkoutAssets)
                    {
                        if (asset != null && asset.isSelected)
                        {
                            interactable = true;
                            break;
                        }
                    }
                }

                exportAllSelectedWorkoutsButton.interactable = interactable;
            } 
        }

        protected virtual void Awake()
        {
            if (windowRoot == null) windowRoot = GetComponent<RectTransform>();

            if (exportAllSelectedWorkoutsButton != null) CustomEditorUtils.SetButtonOnClickAction(exportAllSelectedWorkoutsButton.gameObject, ExportAllSelectedWorkouts);  

            var refWorkoutDropdown = createNewWorkoutWindow.GetComponentInChildren<UIDynamicDropdown>(true);
            refWorkoutDropdown.ClearMenuItems();
            foreach (var refWorkout in Enum.GetNames(typeof(NativeWorkout))) refWorkoutDropdown.CreateNewMenuItem(refWorkout);
            refWorkoutDropdown.SetSelectionText(NativeWorkout.preacher_curls.ToString());

            refWorkoutDropdown = editWorkoutWindow.GetComponentInChildren<UIDynamicDropdown>(true);
            refWorkoutDropdown.ClearMenuItems();
            foreach (var refWorkout in Enum.GetNames(typeof(NativeWorkout))) refWorkoutDropdown.CreateNewMenuItem(refWorkout);
            refWorkoutDropdown.SetSelectionText(NativeWorkout.preacher_curls.ToString()); 

            localWorkoutList.Clear();
            importedWorkoutList.Clear();

            assetPropertyEditor = assetPropertyEditorRoot.GetComponentInChildren<ObjectPropertyEditor>(true);
            taskPropertyEditor = taskPropertyEditorRoot.GetComponentInChildren<ObjectPropertyEditor>(true);
            workoutPropertyEditor = workoutPropertyEditorRoot.GetComponentInChildren<ObjectPropertyEditor>(true);
            if (workoutPropertyEditor.OnRedraw == null) workoutPropertyEditor.OnRedraw = new UnityEngine.Events.UnityEvent();
            workoutPropertyEditor.OnRedraw.AddListener(() => CustomEditorUtils.SetButtonOnClickActionByName(workoutPropertyEditorRoot, ObjectPropertyEditor.EditorInstance.button_AddMainElement, () =>
            {
                OpenOverridablePropertyList();  
            })); 

            overridablePropertyList = overridablePropertyListRoot.GetComponentInChildren<UIRecyclingList>(true);

            CustomEditorUtils.SetButtonOnClickActionByName(windowRoot, "StartGame", () =>
            {
                List<workoutGameTest.InjectableWorkout> injectors = new List<workoutGameTest.InjectableWorkout>();
                foreach (var asset in localWorkoutAssets) if (!asset.isDisabled && asset.injector != null) injectors.Add(new workoutGameTest.InjectableWorkout() { injector = asset.injector, sourcePackage = string.IsNullOrWhiteSpace(asset.package) ? null : ContentManager.FindPackage(new PackageIdentifier(asset.package)) });
                foreach (var asset in importedWorkoutAssets) if (!asset.isDisabled && asset.injector != null) injectors.Add(new workoutGameTest.InjectableWorkout() { injector = asset.injector, sourcePackage = string.IsNullOrWhiteSpace(asset.package) ? null : ContentManager.FindPackage(new PackageIdentifier(asset.package)) });
                workoutGameTest.workoutInjectors = injectors.ToArray();     
                sceneSwapper.Swap(gameSceneIndex);
            }, true, true, false);
        }
        protected virtual void Start()
        {
            RefreshLocalPackagesAndWorkoutAssets();
            RefreshExternalPackagesAndWorkoutAssets();

            if (Directory.Exists(workoutGameTest.DemoPackageLibraryPath))
            {
                ContentManager.LoadPackagesExternally(workoutGameTest.DemoPackageLibraryPath, true); 
            }

            LoadLoaderSettings();
        }

        public delegate void CreatePackageCallback(string packageId);
        public void CreateNewPackage() => CreateNewPackage(packageCreatorWindow, null);
        public void CreateNewPackage(CreatePackageCallback callback) => CreateNewPackage(packageCreatorWindow, callback);
        public void CreateNewPackage(GameObject window, CreatePackageCallback callback)
        {
            if (window == null) return;

            window.gameObject.SetActive(true);

            void Finalize()
            {
                if (!ProjectManager.CreateNewProjectFromWindow(window, out var manifest)) return;

                try
                {
                    ContentManager.ReloadLocalPackages();
                }
                catch (Exception ex) 
                { 
                    swole.LogError(ex);
                }
                try
                {
                    RefreshLocalWorkoutAssets(); 
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                window.gameObject.SetActive(false); 
                callback?.Invoke(manifest.GetIdentityString()); 
            }

            CustomEditorUtils.SetButtonOnClickActionByName(window, "Finalize", Finalize);
        }

        public void CreateNewWorkout() => CreateNewWorkout(createNewWorkoutWindow, null);
        public void CreateNewWorkout(WorkoutAsset toClone) => CreateNewWorkout(createNewWorkoutWindow, toClone);
        public void CreateNewWorkout(RectTransform selectionWindow) => CreateNewWorkout(selectionWindow, null);
        public void CreateNewWorkout(RectTransform selectionWindow, WorkoutAsset toClone)
        {
            if (selectionWindow == null) return;

            selectionWindow.gameObject.SetActive(true);

            CustomEditorUtils.SetComponentTextByName(selectionWindow, "ActionName", toClone == null ? "CREATING NEW WORKOUT" : $"CLONING WORKOUT '{(toClone.injector == null ? "null" : toClone.injector.Name)}'");

            var dropdownRoot = selectionWindow.FindDeepChildLiberal("ReferenceWorkout");
            if (dropdownRoot != null) dropdownRoot.gameObject.SetActive(toClone == null); 

            CustomEditorUtils.SetButtonOnClickActionByName(selectionWindow, "CreatePackage", () => CreateNewPackage((string newPackageId) => CustomEditorUtils.SetInputOrTextComponentTextByName(selectionWindow, "Package", newPackageId)));

            var errorMessage = selectionWindow.GetComponentInChildren<UIPopupMessageFadable>(true);
            void ShowErrorMessage(string msg, float duration)
            {
                if (errorMessage == null) return;
                errorMessage.SetDisplayTime(duration).SetMessageAndShow(msg);
            }
            void Finalize()
            {
                string author = CustomEditorUtils.GetInputOrTextComponentTextByName(selectionWindow, "Author");
                if (!ProjectManager.ValidateAuthorName(author, errorMessage)) return; 
                string id = CustomEditorUtils.GetInputOrTextComponentTextByName(selectionWindow, "WorkoutID");
                if (!ProjectManager.ValidateContentName(id, errorMessage)) return; 

                string displayName = CustomEditorUtils.GetInputOrTextComponentTextByName(selectionWindow, "WorkoutDisplayName");
                string package = CustomEditorUtils.GetInputOrTextComponentTextByName(selectionWindow, "Package");
                if (!ContentManager.CheckIfLocalPackageExists(new PackageIdentifier(package)))
                {
                    ShowErrorMessage($"The local package '{package}' does not exist!", 2);    
                    return;
                }

                NativeWorkout referenceWorkout = NativeWorkout.preacher_curls;
                if (toClone == null)
                {
                    var dropdown = dropdownRoot.GetComponentInChildren<UIDynamicDropdown>(true);
                    if (dropdown != null)
                    {
                        try
                        {
                            referenceWorkout = Enum.Parse<NativeWorkout>(dropdown.GetSelectionText());
                        }
                        catch (Exception ex)
                        {
                            ShowErrorMessage(ex.Message, 3);
                            return;
                        }
                    }
                }
                else 
                { 
                    referenceWorkout = Enum.Parse<NativeWorkout>(toClone.injector.referenceWorkout, true);
                }

                if (!ContentManager.TryFindLocalPackage(package, out var lpkg))
                {
                    ShowErrorMessage($"The local package '{package}' was not found!", 2);
                    return;
                }
                if (lpkg.Content.TryFind<JsonData>(out _, prefix_workoutAssetFile + id, false) || lpkg.Content.TryFind<workoutInjector>(out _, id, false))
                {
                    ShowErrorMessage($"A workout using the ID '{id}' already exists!", 2);  
                    return;
                }

                WorkoutAsset asset = null;
                ImageAsset thumbnail = null;
                if (toClone == null)
                {
                    asset = CreateNewWorkout(referenceWorkout, id, displayName, lpkg.GetIdentityString(), author);
                    if (asset == null || asset.injector == null)
                    {
                        ShowErrorMessage($"Failed to create workout asset!", 2);
                        return;
                    }

                    // set default overrides and properties
                    var props = asset.injector.parameters;
                    props.restOffsetTasks = new OverridableTask.Array() { doOverride = true };
                    props.repOffsetTasks = new OverridableTask.Array() { doOverride = true };
                    props.flexOffsetTasks = new OverridableTask.Array() { doOverride = true };
                    asset.injector.parameters = props;
                }
                else
                {
                    try
                    {
                        asset = toClone.Duplicate();
                        asset.package = package;
                        asset.name = $"{prefix_workoutAssetFile}{id}";
                        var info = asset.injector.contentInfo;
                        info.name = id;
                        info.author = author;
                        asset.injector.contentInfo = info;
                        var props = asset.injector.parameters;
                        props.displayName = displayName;
                        asset.injector.parameters = props; 
                    
                        if (ContentManager.TryFindLocalPackage(new PackageIdentifier(toClone.package), out var originalPackage))
                        {
                            originalPackage.Content.TryFind(out thumbnail, $"{prefix_workoutThumbnail}{toClone.injector.Name}", true);

                            try
                            {
                                if (thumbnail != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(thumbnail.imagePath))
                                    {
                                        var originalThumbnailPath = Path.Combine(originalPackage.workingDirectory.FullName, thumbnail.imagePath);
                                        if (File.Exists(originalThumbnailPath))
                                        {
                                            Sprite newSprite = null;
                                            if (thumbnail.Sprite != null)
                                            {
                                                if (thumbnail.Sprite.texture != null)
                                                {
                                                    // if this isn't done, then the sprite for the cloned workout will get deleted if this workout gets deleted
                                                    var tex = Instantiate(thumbnail.Sprite.texture); 
                                                    newSprite = Sprite.Create(tex, thumbnail.Sprite.rect, thumbnail.Sprite.pivot, thumbnail.Sprite.pixelsPerUnit);  
                                                } 
                                                else
                                                {
                                                    newSprite = Instantiate(thumbnail.Sprite);
                                                }
                                            }
                                            thumbnail = new ImageAsset($"{prefix_workoutThumbnail}{asset.injector.Name}", thumbnail.Author, DateTime.Now, DateTime.Now, thumbnail.Description, newSprite, thumbnail.IsCompressed, lpkg.Manifest, thumbnail.IsLinear);
                                            thumbnail.imagePath = $"{thumbnail.Name}{Path.GetExtension(originalThumbnailPath)}";
                                            var newThumbnailPath = Path.Combine(lpkg.workingDirectory.FullName, thumbnail.imagePath); 
                                            File.Copy(originalThumbnailPath, newThumbnailPath); 
                                        }
                                        else thumbnail = null;
                                    }
                                    else thumbnail = null;
                                }
                            }
                            catch (Exception ex)
                            {
                                ShowErrorMessage(ex.Message, 3); 
    #if UNITY_EDITOR
                                swole.LogError(ex);
    #endif
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage(ex.Message, 3);
    #if UNITY_EDITOR
                        swole.LogError(ex);
    #endif
                        return;
                    }
                }

                try
                {
                    var spkg = SwolePackage.Create(lpkg.Content);
                    spkg.Add(asset.injector);
                    if (thumbnail != null) spkg.AddOrReplace(thumbnail);
                    lpkg.Content = spkg;

                    ContentManager.SavePackage(lpkg);  
                } 
                catch(Exception ex)
                {
                    ShowErrorMessage(ex.Message, 3);
    #if UNITY_EDITOR
                    swole.LogError(ex);
    #endif
                    return;
                }

                selectionWindow.gameObject.SetActive(false);  

                StartEditingWorkout(asset);
                if (localWorkoutAssets == null) localWorkoutAssets = new WorkoutAsset[0];
                var temp = new WorkoutAsset[localWorkoutAssets.Length + 1]; 
                if (localWorkoutAssets.Length > 0) localWorkoutAssets.CopyTo(temp, 0);
                temp[localWorkoutAssets.Length] = asset;
                localWorkoutAssets = temp; 
            }

            CustomEditorUtils.SetButtonOnClickActionByName(selectionWindow, "Finalize", Finalize);
        }

        public WorkoutAsset CreateNewWorkout(NativeWorkout referenceWorkout, string id, string displayName, string package, string author)
        {
            var injector = new workoutInjector()
            {
                contentInfo = new ContentInfo()
                {
                    name = id,
                    author = author
                }
            };
            injector.referenceWorkout = referenceWorkout.ToString();

            var parameters = injector.parameters;

            parameters.displayName = displayName;

            injector.parameters = parameters;

            WorkoutAsset asset = new WorkoutAsset() { name = prefix_workoutAssetFile + id, package = package, injector = injector };

            var temp = new WorkoutAsset[localWorkoutAssets == null ? 1 : (localWorkoutAssets.Length + 1)];
            if (localWorkoutAssets != null) localWorkoutAssets.CopyTo(temp, 0);
            localWorkoutAssets = temp;
            localWorkoutAssets[localWorkoutAssets.Length - 1] = asset; 

            //AddWorkoutAssetToList(asset, localWorkoutList, true, null);
            RebuildLocalWorkoutList();

            return asset;
        }

        [Serializable]
        public class WorkoutAsset : ICloneable
        {

            #region ICloneable

            public WorkoutAsset Duplicate()
            {
                var clone = new WorkoutAsset();

                clone.name = name;
                clone.package = package;
                if (injector != null) clone.injector = injector.Duplicate();
             
                return clone;
            }
            public object Clone() => Duplicate();

            #endregion

            public string name;
            public static string WorkoutContentNameToWorkoutID(string contentName) => contentName == null ? null : (contentName.AsID().StartsWith(prefix_workoutAssetFile.AsID()) ? contentName.Substring(prefix_workoutAssetFile.Length) : contentName);
            public string WorkoutID => WorkoutContentNameToWorkoutID(name);
            public string package;
            [NonSerialized]
            public workoutInjector injector;
            [NonSerialized]
            public SwolePackage swolePackage;
            [NonSerialized]
            public ContentManager.LocalPackage localPackage;

            [NonSerialized]
            public ContentManager.ExternalPackage loadedFromExternal;

            [NonSerialized]
            public bool isSelected; 
            public bool isDisabled;
        
            public void CopySettingsTo(WorkoutAsset[] assets)
            {
                if (assets == null) return;

                foreach (var asset in assets) if (name == asset.name && package == asset.package)
                    {
                        asset.isSelected = isSelected;
                        asset.isDisabled = isDisabled;
                    }
            }

            public string ThumbnailName => $"{prefix_workoutThumbnail}{injector.Name}";

            public void AddNewWorkoutThumbnail(PackageViewer.LoadSpriteFromFilePickerDelegate callback = null)
            {
                if (injector == null || string.IsNullOrWhiteSpace(package)) return; 

                if (ContentManager.TryFindLocalPackage(new PackageIdentifier(package), out localPackage))
                {
                    PackageViewer.OpenFileBrowserAndLoadSprite((Sprite sprite, bool isCompressed, bool isLinear, ExternalAssets.ILoaderContext context) =>
                    {
                        if (localPackage.Content.TryFind<ImageAsset>(out var existingAsset, ThumbnailName, true))
                        {
                            if (existingAsset != null && !string.IsNullOrWhiteSpace(existingAsset.imagePath)) // try to delete existing thumbnail incase the extension changes
                            {
                                var fullPath = Path.Combine(localPackage.workingDirectory.FullName, existingAsset.imagePath);
                                if (File.Exists(fullPath)) File.Delete(fullPath);
                            }
                        }

                        var imageAsset = new ImageAsset(ThumbnailName, injector.Author, DateTime.Now, DateTime.Now, string.Empty, sprite, isCompressed, default, isLinear);
                        try
                        {
                            imageAsset.ignoreSpriteOnSerialization = true;
                            imageAsset.SaveTextureInPackageDirectory(localPackage, true, false);   

                            swolePackage = SwolePackage.Create(localPackage);
                            swolePackage.AddOrReplace(imageAsset); 
                            localPackage.Content = swolePackage;

                            ContentManager.SavePackage(localPackage);
                        }
                        catch (Exception ex)
                        {
                            swole.LogError(ex);
                        }

                        callback?.Invoke(sprite, isCompressed, isLinear, context); 
                    });
                }
            }

            public void Save()
            {
                if (injector == null || /*(localPackage == null && */!ContentManager.TryFindLocalPackage(package, out localPackage)/*)*/) return;

                try
                {
                    //if (swolePackage == null)
                    //{
                        swolePackage = SwolePackage.Create(localPackage.Content);
                        swolePackage.RemoveAll(i => i == null || ((i.Name == injector.Name || i.Name == name || i.Name == $"{prefix_workoutAssetFile}{injector.Name}") && (typeof(workoutInjector).IsAssignableFrom(i.GetType()) || typeof(workoutInjector.Serialized).IsAssignableFrom(i.GetType()) || typeof(JsonData).IsAssignableFrom(i.GetType()) || typeof(JsonData.Serialized).IsAssignableFrom(i.GetType()))));
                        swolePackage.AddOrReplace(injector); 
                        localPackage.Content = swolePackage;
                    //}

                    ContentManager.SavePackage(localPackage); 
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }
            }

            public void RenameAndSave(string newId)
            {
                if (injector == null || /*(localPackage == null && */!ContentManager.TryFindLocalPackage(package, out localPackage)/*)*/) return;

                try // rename thumbnail file if it exists
                {
                    string imgFileDir = localPackage.workingDirectory.FullName;
                    string imgFilePath;

                    if (localPackage.Content.TryFind(out ImageAsset imgAsset, ThumbnailName))
                    {
                        if (imgAsset != null)
                        {
                            var info = imgAsset.contentInfo;
                            info.name = $"{prefix_workoutThumbnail}{newId}"; 
                            imgAsset.contentInfo = info;
                            if (!string.IsNullOrWhiteSpace(imgAsset.imagePath))
                            {
                                var localPath = Path.GetDirectoryName(imgAsset.imagePath);
                                var originalFileName = Path.GetFileNameWithoutExtension(imgAsset.imagePath); 
                                if (!string.IsNullOrWhiteSpace(originalFileName) && originalFileName.AsID() == $"{prefix_workoutThumbnail}{injector.Name}".AsID())
                                {
                                    var imgExtension = Path.GetExtension(imgAsset.imagePath);
                                    imgFilePath = Path.Combine(imgFileDir, $"{imgAsset.imagePath}");
                                    if (File.Exists(imgFilePath))
                                    {
                                        var newImgLocalPath = (string.IsNullOrWhiteSpace(localPath) ? string.Empty : localPath) + $"{info.name}{imgExtension}";
                                        File.Move(imgFilePath, Path.Combine(imgFileDir, newImgLocalPath));
                                        imgAsset.imagePath = newImgLocalPath;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {

                        imgFilePath = Path.Combine(imgFileDir, $"{ThumbnailName}{ContentManager.fileExtension_GenericImage}");
                        if (File.Exists(imgFilePath))
                        {
                            File.Move(imgFilePath, Path.Combine(imgFileDir, $"{prefix_workoutThumbnail}{newId}{ContentManager.fileExtension_GenericImage}"));
                        }
                        imgFilePath = Path.Combine(imgFileDir, $"{ThumbnailName}{ContentManager.fileExtension_JPG}");
                        if (File.Exists(imgFilePath))
                        {
                            File.Move(imgFilePath, Path.Combine(imgFileDir, $"{prefix_workoutThumbnail}{newId}{ContentManager.fileExtension_JPG}"));
                        }
                        imgFilePath = Path.Combine(imgFileDir, $"{ThumbnailName}{ContentManager.fileExtension_PNG}");
                        if (File.Exists(imgFilePath))
                        {
                            File.Move(imgFilePath, Path.Combine(imgFileDir, $"{prefix_workoutThumbnail}{newId}{ContentManager.fileExtension_PNG}"));
                        }
                    }
                } 
                catch(Exception ex)
                {
                    swole.LogError(ex); 
                }

                try
                {
                    //if (swolePackage == null)
                    //{
                        swolePackage = SwolePackage.Create(localPackage.Content);
                        swolePackage.RemoveAll(i => i == null || ((i.Name == injector.Name || i.Name == name || i.Name == $"{prefix_workoutAssetFile}{injector.Name}") && (typeof(workoutInjector).IsAssignableFrom(i.GetType()) || typeof(workoutInjector.Serialized).IsAssignableFrom(i.GetType()) || typeof(JsonData).IsAssignableFrom(i.GetType()) || typeof(JsonData.Serialized).IsAssignableFrom(i.GetType()))));
                        var info = injector.contentInfo;
                        info.name = newId;
                        injector.contentInfo = info;
                        swolePackage.AddOrReplace(injector);
                        localPackage.Content = swolePackage;
                    //}

                    name = $"{prefix_workoutAssetFile}{injector.Name}"; 

                    ContentManager.SavePackage(localPackage);
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }
            }

            public void Delete()
            {
                if (injector == null || /*(localPackage == null && */!ContentManager.TryFindLocalPackage(package, out localPackage)/*)*/) return; 

                try
                {
                    localPackage.Content.TryFind(out ImageAsset thumbnail, $"{prefix_workoutThumbnail}{injector.Name}", true);
                    try
                    {
                        if (thumbnail != null)
                        {
                            if (!string.IsNullOrWhiteSpace(thumbnail.imagePath))
                            {
                                var thumbnailPath = Path.Combine(localPackage.workingDirectory.FullName, thumbnail.imagePath);
                                if (File.Exists(thumbnailPath)) File.Delete(thumbnailPath); 
                            }
                        }
                    }
                    catch (Exception ex)
                    {
    #if UNITY_EDITOR
                        swole.LogError(ex);
    #endif
                    }

                    //if (swolePackage == null)
                    //{
                    swolePackage = SwolePackage.Create(localPackage.Content);
                    swolePackage.RemoveAll(i => i == null || ((i.Name == injector.Name || i.Name == name || i.Name == $"{prefix_workoutAssetFile}{injector.Name}") && (typeof(workoutInjector).IsAssignableFrom(i.GetType()) || typeof(workoutInjector.Serialized).IsAssignableFrom(i.GetType()) || typeof(JsonData).IsAssignableFrom(i.GetType()) || typeof(JsonData.Serialized).IsAssignableFrom(i.GetType()))));
                    if (thumbnail != null) swolePackage.Remove(thumbnail);
                    localPackage.Content = swolePackage;
                    //}

                    ContentManager.SavePackage(localPackage);   
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }
            }
            public VoidParameterlessDelegate PrepareDeleteImport(IList<IContent> contentToBeDeleted)
            {
                if (injector == null || !ContentManager.TryFindExternalPackage(package, out var externalPackage) || externalPackage.content == null || string.IsNullOrWhiteSpace(externalPackage.cachedPath)) return null;

                string rootPath = ContentManager.GetRootPathFromEmbeddedPath(externalPackage.cachedPath);
                if (externalPackage.isNativeContent || string.IsNullOrWhiteSpace(rootPath)) return null;

                string prefix = prefix_workoutAssetFile.AsID();

                for (int p = 0; p < ContentManager.ExternalPackageCount; p++)
                {
                    var pkg = ContentManager.GetExternalPackage(p);
                    if (pkg.isNativeContent || string.IsNullOrWhiteSpace(pkg.cachedPath)) continue;

                    string rootPath_ = ContentManager.GetRootPathFromEmbeddedPath(pkg.cachedPath);
                    if (!rootPath.IsIdenticalPathIgnoreCase(rootPath_)) continue;

                    for (int a = 0; a < pkg.content.ContentCount; a++)
                    {
                        var contentObj = pkg.content[a];
                        if (contentObj == null || string.IsNullOrWhiteSpace(contentObj.Name)) continue; 

                        if (contentObj is JsonData && contentObj.Name.AsID().StartsWith(prefix))
                        {
                            contentToBeDeleted.Add(contentObj);
                        }
                        else if (contentObj is workoutInjector)
                        {
                            contentToBeDeleted.Add(contentObj);
                        }
                    }
                }

                void DeleteImport()
                {
                    try
                    {
                        if (File.Exists(rootPath))
                        {
                            File.Delete(rootPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }

                return DeleteImport;
            }

            public void StopEditing()
            {
                Save();

                localPackage = null;
                swolePackage = null; 
            }

        }
        [Serializable]
        public struct LoaderSettings
        {
            public WorkoutAsset[] localWorkoutAssets;
            public WorkoutAsset[] importedWorkoutAssets;
        }

        public WorkoutAsset[] localWorkoutAssets;
        public WorkoutAsset[] importedWorkoutAssets;

        public static string LoaderSettingsPath => Path.Combine(Application.persistentDataPath, "demo_workouts.json");
        public void SaveLoaderSettings()
        {
            var settings = new LoaderSettings() { localWorkoutAssets = localWorkoutAssets, importedWorkoutAssets = importedWorkoutAssets };
            try
            {
                var json = swole.ToJson(settings, true);
                var bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                File.WriteAllBytes(LoaderSettingsPath, bytes);
            }
            catch (Exception ex)
            {
                swole.LogWarning("Failed to save workout loader settings!");
    #if UNITY_EDITOR
                Debug.LogError(ex);
    #else
                swole.LogWarning(ex.Message);
    #endif
            }
        }
        public void LoadLoaderSettings()
        {
            LoaderSettings settings = default;

            try
            {
                if (File.Exists(LoaderSettingsPath))
                {
                    var bytes = File.ReadAllBytes(LoaderSettingsPath);
                    var json = DefaultJsonSerializer.StringEncoder.GetString(bytes);
                    settings = swole.FromJson<LoaderSettings>(json);
                }
            }
            catch (Exception ex)
            {
                swole.LogWarning("Failed to load workout loader settings!");
    #if UNITY_EDITOR
                Debug.LogError(ex);
    #else
                swole.LogWarning(ex.Message);
    #endif
            }

            LoadLoaderSettings(settings);
        }
        public void LoadLoaderSettings(LoaderSettings settings)
        {
            localWorkoutAssets = settings.localWorkoutAssets;
            importedWorkoutAssets = settings.importedWorkoutAssets;

            RefreshWorkoutAssets();
        }

        public List<WorkoutAsset> GetSelectedAssets(ICollection<WorkoutAsset> assets, List<WorkoutAsset> list = null) 
        {
            if (list == null) list = new List<WorkoutAsset>(); 

            foreach(var asset in assets) if (asset != null && asset.isSelected) list.Add(asset);

            return list;
        }
    
        public bool loadAllLocalWorkouts;

        private readonly List<WorkoutAsset> _tempWorkoutAssets = new List<WorkoutAsset>();
        public WorkoutAsset[] LoadLocalWorkoutAssets()
        {
            string prefix = prefix_workoutAssetFile.AsID();
            
            _tempPackages.Clear(); 
            _tempWorkoutAssets.Clear();
            for (int a = 0; a < ContentManager.LocalPackageCount; a++) 
            { 
                var pkg = ContentManager.GetLocalPackage(a);
                if (pkg == null || pkg.Content == null) continue;

                var id = pkg.GetIdentity();
                bool loadContent = loadAllLocalWorkouts;
                if (!loadContent && !_tempPackages.Contains(id.name))
                {
                    var newPkg = ContentManager.FindLocalPackage(id.name);
                    if (ReferenceEquals(pkg, newPkg)) loadContent = true;
                }

                if (loadContent)
                {
                    _tempPackages.Add(pkg.GetIdentity().name);

                    var content = pkg.Content;
                    for (int b = 0; b < content.ContentCount; b++)
                    {
                        try
                        {
                            var contentObj = content[b];
                            if (contentObj == null || string.IsNullOrWhiteSpace(contentObj.Name)) continue;

                            if (contentObj is JsonData json && contentObj.Name.AsID().StartsWith(prefix))
                            {
                                _tempWorkoutAssets.Add(new WorkoutAsset()
                                {
                                    name = contentObj.Name,
                                    package = pkg.GetIdentityString(),
                                    injector = swole.FromJson<workoutInjector.Serialized>(json.json).AsOriginalType()
                                });
                            } 
                            else if (contentObj is workoutInjector injector)
                            {
                                _tempWorkoutAssets.Add(new WorkoutAsset()
                                {
                                    name = contentObj.Name,
                                    package = pkg.GetIdentityString(),
                                    injector = injector
                                }); 
                            }
                        }
                        catch (Exception ex)
                        {
                            swole.LogError(ex);
                        }
                    }
                }
            }

            return _tempWorkoutAssets.ToArray();
        }

        public bool loadAllExternalWorkouts;

        public WorkoutAsset[] LoadExternalWorkoutAssets()
        {
            string prefix = prefix_workoutAssetFile.AsID();

            _tempPackages.Clear();
            _tempWorkoutAssets.Clear(); 
            for (int a = 0; a < ContentManager.ExternalPackageCount; a++)
            {
                var pkg = ContentManager.GetExternalPackage(a);
                if (pkg.Content == null) continue;

                var id = pkg.GetIdentity();
                bool loadContent = loadAllExternalWorkouts;
                if (!loadContent && !_tempPackages.Contains(id.name))
                {
                    var newPkg = ContentManager.FindExternalPackage(id.name);
                    if (id == newPkg.GetIdentity()) loadContent = true;
                }

                if (loadContent)
                {
                    _tempPackages.Add(id.name);

                    var content = pkg.Content;
                    for (int b = 0; b < content.ContentCount; b++)
                    {
                        try
                        {
                            var contentObj = content[b];
                            if (contentObj == null || string.IsNullOrWhiteSpace(contentObj.Name)) continue;

                            if (contentObj is JsonData json && contentObj.Name.AsID().StartsWith(prefix))
                            {
                                _tempWorkoutAssets.Add(new WorkoutAsset()
                                {
                                    name = contentObj.Name,
                                    package = pkg.GetIdentityString(),
                                    injector = swole.FromJson<workoutInjector.Serialized>(json.json).AsOriginalType(),
                                    loadedFromExternal = pkg
                                });
                            }
                            else if (contentObj is workoutInjector injector)
                            {
                                _tempWorkoutAssets.Add(new WorkoutAsset() 
                                {
                                    name = contentObj.Name,
                                    package = pkg.GetIdentityString(),
                                    injector = injector,
                                    loadedFromExternal = pkg
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            swole.LogError(ex);
                        }
                    }
                }
            }

            return _tempWorkoutAssets.ToArray();
        }
        protected void RefreshListMember(UIRecyclingList.MemberData memberData, GameObject instance)
        {
            var img = instance.GetComponent<Image>();
            if (img != null)
            {
                if (memberData.storage is WorkoutAsset asset)
                {
                    img.color = asset.isSelected ? listMemberSelectedColor : listMemberDefaultColor;
                } 
                else
                {
                    img.color = listMemberDefaultColor;  
                }
            }
        }

        public void DeselectAllLocalWorkoutAssets()
        {
            if (localWorkoutAssets != null)
            {
                foreach (var asset in localWorkoutAssets) asset.isSelected = false;
            }

            if (localWorkoutList != null) localWorkoutList.Refresh();
        }
        public void DeselectAllImportedWorkoutAssets()
        {
            if (importedWorkoutAssets != null)
            {
                foreach (var asset in importedWorkoutAssets) asset.isSelected = false;
            }

            if (importedWorkoutList != null) importedWorkoutList.Refresh();
        }
        public void DeselectAllWorkoutAssets()
        {
            DeselectAllLocalWorkoutAssets();  
            DeselectAllImportedWorkoutAssets();
        }
        protected void AddWorkoutAssetToList(WorkoutAsset asset, UIRecyclingList list, bool refreshUI, UIRecyclingList.OnRefreshMember onRefresh)
        {
            list.AddNewMember(asset.WorkoutID, () =>
            {

                if (!InputProxy.Modding_ModifyActionKey)
                {
                    for(int a = 0; a < list.Count; a++)
                    {
                        var member = list.GetMember(a);
                        if (member.storage is WorkoutAsset asset_)
                        {
                            asset_.isSelected = false;
                        }
                    }
                }

                asset.isSelected = true;

                list.Refresh(); 

            }, 
            refreshUI, 
            (UIRecyclingList.MemberData mem, GameObject inst) => 
            { 
                RefreshListMember(mem, inst);
                onRefresh?.Invoke(mem, inst);
            }, 
            asset);
        }
        public void RebuildLocalWorkoutList()
        {
            if (localWorkoutList == null) return;

            localWorkoutList.Clear();
            foreach (var asset in localWorkoutAssets)
            {
                var asset_ = asset;
                AddWorkoutAssetToList(asset_, localWorkoutList, false,
                (UIRecyclingList.MemberData mem, GameObject inst) =>
                {
                    CustomEditorUtils.SetButtonOnClickActionByName(inst, "Edit", () => StartEditingWorkout(asset_));

                    CustomEditorUtils.SetButtonOnClickActionByName(inst, "Export", () => ExportWorkout(asset_));

                    CustomEditorUtils.SetToggleOnValueChangeActionByName(inst, "Active", (bool val) =>
                    {
                        asset_.isDisabled = !val;
                        SaveLoaderSettings();
                    });
                    CustomEditorUtils.SetToggleValueByName(inst, "Active", !asset_.isDisabled);

                    CustomEditorUtils.SetButtonOnClickActionByName(inst, "Delete",
                    () =>
                    {
                        yesNoPrompt.gameObject.SetActive(true);
                        CustomEditorUtils.SetComponentTextByName(yesNoPrompt, "Message", $"Are you sure you want to delete {asset_.injector.Name}? This cannot be undone!");
                        CustomEditorUtils.SetButtonOnClickActionByName(yesNoPrompt, "Yes", () =>
                        {
                            yesNoPrompt.gameObject.SetActive(false);
                            asset_.Delete();
                            RefreshLocalWorkoutAssets();
                        });
                    });

                    CustomEditorUtils.SetButtonOnClickActionByName(inst, "Clone", () => CreateNewWorkout(asset_));
                });
            }
            localWorkoutList.Refresh(); 
        }

        private readonly List<IContent> tempContent = new List<IContent>();
        public void RebuildImportedWorkoutList()
        {
            if (importedWorkoutList == null) return;

            importedWorkoutList.Clear();
            foreach (var asset in importedWorkoutAssets)
            {
                var asset_ = asset;
                AddWorkoutAssetToList(asset, importedWorkoutList, false,
                (UIRecyclingList.MemberData mem, GameObject inst) =>
                {
                    CustomEditorUtils.SetToggleOnValueChangeActionByName(inst, "Active", (bool val) =>
                    {
                        asset_.isDisabled = !val;
                        SaveLoaderSettings();
                    });
                    CustomEditorUtils.SetToggleValueByName(inst, "Active", !asset_.isDisabled);

                    CustomEditorUtils.SetButtonOnClickActionByName(inst, "Clone", () => CreateNewWorkout(asset_));

                    if (asset_.loadedFromExternal.content != null && !asset_.loadedFromExternal.isNativeContent)
                    { 
                        CustomEditorUtils.SetButtonOnClickActionByName(inst, "Delete",
                        () =>
                        {
                            tempContent.Clear();
                            var confirmDeletion = asset_.PrepareDeleteImport(tempContent); 

                            if (tempContent.Count > 1)
                            {
                                string contentString = "";
                                for (int a = 0; a < tempContent.Count; a++) contentString = contentString + (a == tempContent.Count - 1 ? (tempContent.Count == 2 ? " and " : "and ") : "") + WorkoutAsset.WorkoutContentNameToWorkoutID(tempContent[a].Name) + ((a < tempContent.Count - 1 && tempContent.Count > 2) ? ", " : "");

                                yesNoPrompt.gameObject.SetActive(true);
                                CustomEditorUtils.SetComponentTextByName(yesNoPrompt, "Message", $"This will delete the imported workouts {contentString}. Are you sure?"); 
                                CustomEditorUtils.SetButtonOnClickActionByName(yesNoPrompt, "Yes", () =>
                                {
                                    yesNoPrompt.gameObject.SetActive(false);
                                    confirmDeletion?.Invoke();
                                    RefreshExternalPackagesAndWorkoutAssets();
                                    tempContent.Clear();
                                });
                            }
                            else
                            {
                                yesNoPrompt.gameObject.SetActive(true);
                                CustomEditorUtils.SetComponentTextByName(yesNoPrompt, "Message", $"Are you sure you want to delete the imported workout {(asset_.WorkoutID)}?");
                                CustomEditorUtils.SetButtonOnClickActionByName(yesNoPrompt, "Yes", () =>
                                {
                                    yesNoPrompt.gameObject.SetActive(false);
                                    confirmDeletion?.Invoke();
                                    RefreshExternalPackagesAndWorkoutAssets();
                                    tempContent.Clear();
                                });
                            }
                        });
                    } 
                    else
                    {
                        CustomEditorUtils.SetButtonInteractableByName(inst, "Delete", false);   
                    }
                });
            }
            importedWorkoutList.Refresh();
        }
        public void RebuildWorkoutLists()
        {
            RebuildLocalWorkoutList();
            RebuildImportedWorkoutList();
        }
      
        public void RefreshLocalPackagesAndWorkoutAssets()
        {
            ContentManager.ReloadLocalPackages();
            RefreshLocalWorkoutAssets();
        }
        public void RefreshLocalWorkoutAssets()
        {
            var localAssets = LoadLocalWorkoutAssets();
            if (localWorkoutAssets != null)
            {
                foreach (var asset in localWorkoutAssets) if (asset != null) asset.CopySettingsTo(localAssets);
            }
            localWorkoutAssets = localAssets;
        
            RebuildLocalWorkoutList();
        }

        public void RefreshExternalPackagesAndWorkoutAssets()
        {
            ContentManager.ReloadExternalPackages();
            RefreshImportedWorkoutAssets();
        }
        public void RefreshImportedWorkoutAssets()
        {
            var importedAssets = LoadExternalWorkoutAssets();
            if (importedWorkoutAssets != null)
            {
                foreach (var asset in importedWorkoutAssets) if (asset != null) asset.CopySettingsTo(importedAssets);
            }
            importedWorkoutAssets = importedAssets; 

            RebuildImportedWorkoutList();
        }
        public void RefreshWorkoutAssets()
        {
            RefreshLocalWorkoutAssets();
            RefreshImportedWorkoutAssets();
        }

        public static readonly ExtensionFilter[] _packageFileExtensions = new ExtensionFilter[] { new ExtensionFilter() { Name = "package", Extensions = new string[] { ContentManager.fileExtension_ZIP, ContentManager.swoleFileExtension_Package } } };
        private readonly List<ContentManager.ExternalPackage> tempPackages = new List<ContentManager.ExternalPackage>();
        public void ImportPackages()
        {
            string workoutPrefix = prefix_workoutAssetFile.AsID();
            var result = StandaloneFileBrowser.OpenFilePanel("Select one or more package files", Application.dataPath, _packageFileExtensions, true);

            if (result != null && result.Count > 0)
            {
                var dir = ContentManager.CachedPackageDirectoryPath;
                Directory.CreateDirectory(dir);
                foreach (var res in result)
                {
                    if (string.IsNullOrWhiteSpace(res.Name)) continue;
                 
                    try
                    {

                        tempPackages.Clear();
                        ContentManager.LoadPackage(false, string.Empty, res.Name, null, false, tempPackages);
                        foreach (var preloadPkg in tempPackages)
                        {
                            string id = preloadPkg.GetIdentityString();
                            if (string.IsNullOrWhiteSpace(id)) continue;

                            if (preloadPkg.content == null) continue;

                            bool containsWorkouts = false;
                            for(int a = 0; a < preloadPkg.content.ContentCount; a++)
                            {
                                var contentObj = preloadPkg.content.GetContent(a);
                                if (contentObj == null || string.IsNullOrWhiteSpace(contentObj.Name)) continue;

                                if (contentObj is JsonData json && contentObj.Name.AsID().StartsWith(workoutPrefix))
                                {
                                    containsWorkouts = true;
                                    break;
                                }
                                else if (contentObj is workoutInjector injector)
                                {
                                    containsWorkouts = true;
                                    break;
                                }
                            }
                            if (!containsWorkouts) continue;

                            for (int a = 0; a < ContentManager.ExternalPackageCount; a++)
                            {
                                var pkg = ContentManager.GetExternalPackage(a);
                                if (pkg.isNativeContent) continue;

                                if (id == pkg.GetIdentityString())
                                {
                                    if (!string.IsNullOrWhiteSpace(pkg.cachedPath))
                                    {
                                        string existingPath = pkg.cachedPath;
                                        if (existingPath.IsSubPathOf(ContentManager.AssetStreamingPath)) continue; 
                                    
                                        existingPath = ContentManager.GetRootPathFromEmbeddedPath(existingPath);
                                        if (existingPath.IsIdenticalPathIgnoreCase(res.Name)) continue;

                                        try
                                        {
                                            if (File.Exists(existingPath))
                                            {
                                                File.Delete(existingPath); 
                                                swole.Log($"Removed existing workout content at {existingPath} to make way for new workout content from {res.Name}");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            swole.LogError(ex);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        swole.LogError($"Failed to load package from {res.Name}");
                        swole.LogError(ex); 
                        continue;
                    }

                    try
                    {
                        string importPath = Path.Combine(dir, Path.GetFileName(res.Name));
                        if (File.Exists(importPath)) 
                        { 
                            File.Delete(importPath);
                            swole.Log($"Replacing {importPath}");
                        }
                        File.Copy(res.Name, importPath); 
                    } 
                    catch(Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }
            
                try
                {
                    ContentManager.ReloadExternalPackages();
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                RefreshWorkoutAssets();    
            }
        }

        protected static string GetPackageStringWithVersion(string packageString)
        {
            if (string.IsNullOrWhiteSpace(packageString)) return string.Empty;
            if (!string.IsNullOrWhiteSpace(new PackageIdentifier(packageString).version)) return packageString;

            if (ContentManager.TryFindPackage(packageString, out var package)) return package.GetIdentityString();
            return string.Empty;
        }
        private readonly HashSet<string> _tempPackages = new HashSet<string>();
        public void ExportAllSelectedWorkouts()
        {
            if (localWorkoutAssets == null) return;
            _tempWorkoutAssets.Clear();
            GetSelectedAssets(localWorkoutAssets, _tempWorkoutAssets);  
            if (_tempWorkoutAssets.Count <= 0) return;

            ExportPackages(_tempWorkoutAssets); 
        }
        public void ExportWorkout(WorkoutAsset asset)
        {
            _tempWorkoutAssets.Clear();
            _tempWorkoutAssets.Add(asset);

            ExportPackages(_tempWorkoutAssets); 
        }
        public void ExportPackages(ICollection<WorkoutAsset> workoutAssets)
        {
            _tempPackages.Clear();
            foreach (var asset in workoutAssets)
            {
                var workoutPkg = GetPackageStringWithVersion(asset.package);
                if (!string.IsNullOrWhiteSpace(workoutPkg)) _tempPackages.Add(workoutPkg);
            
                if (asset.injector != null)
                {
                    var defaultAssetPackage = GetPackageStringWithVersion(asset.injector.defaultAssetPackage);
                    if (!string.IsNullOrWhiteSpace(defaultAssetPackage)) _tempPackages.Add(defaultAssetPackage);

                    if (asset.injector.animations != null)
                    {
                        foreach(var anim in asset.injector.animations)
                        {
                            if (string.IsNullOrWhiteSpace(anim.settings.package)) continue;

                            var pkg = GetPackageStringWithVersion(anim.settings.package);
                            if (!string.IsNullOrWhiteSpace(pkg)) _tempPackages.Add(pkg);
                        }
                        foreach (var sound in asset.injector.sounds)
                        {
                            if (string.IsNullOrWhiteSpace(sound.package)) continue;

                            var pkg = GetPackageStringWithVersion(sound.package);
                            if (!string.IsNullOrWhiteSpace(pkg)) _tempPackages.Add(pkg);
                        }
                    }
                }
            }

            var result = StandaloneFileBrowser.SaveFilePanel("Set a destination to export to", Application.dataPath, "customWorkoutPackage", ContentManager.fileExtension_ZIP);
            if (result != null && !string.IsNullOrWhiteSpace(result.Name))
            {
                bool success = false;
                var tempDir = Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, $"temp/{Path.GetFileNameWithoutExtension(result.Name)}"));
                try
                {
                    foreach (var pkgId in _tempPackages)
                    {
                        string sourcePath = string.Empty;
                        if (!ContentManager.TryFindLocalPackage(pkgId, out var lpkg))
                        {
                            if (!ContentManager.TryFindExternalPackage(pkgId, out var epkg)) continue;
                            if (epkg.isNativeContent) continue; // don't include embedded content dependencies since everyone will have it already

                            string path = epkg.sourcePath;
                            if (string.IsNullOrWhiteSpace(path)) path = epkg.cachedPath;
                            if (string.IsNullOrWhiteSpace(path)) continue;

                            File.Copy(path, Path.Combine(tempDir.FullName, epkg.GetIdentityString() + "." + ContentManager.fileExtension_ZIP));
                        }
                        else
                        {
                            swole.Engine.CompressZIP(lpkg.workingDirectory, Path.Combine(tempDir.FullName, lpkg.GetIdentityString() + "." + ContentManager.fileExtension_ZIP));
                        }
                    }

                    swole.Engine.CompressZIP(tempDir, result.Name); 
                    success = true; 
                } 
                catch(Exception ex)
                {
                    swole.LogError(ex); 
                }
                Directory.Delete(tempDir.FullName, true);

                if (success && infoMessage != null) infoMessage.SetDisplayTime(3).SetMessageAndShow("Export successful!"); 
            }
        }

    }

}