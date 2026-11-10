using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using Unity.Mathematics;

using Swole.UI;

using FCP;

using TMPro;

namespace Swole.API.Unity 
{
    public class SandboxCharacterCreator : MonoBehaviour
    {

        [Header("Camera")]
        new public Camera camera;

        public Vector3 camera_distanceAxis = Vector3.forward; 

        public Vector3 camera_focusFloorPosition;

        public Vector2 camera_focusHeightRange;
        public float camera_startFocusHeight = 0.8f;
        protected float camera_focusHeight;

        public Vector2 camera_heightRange;
        public float camera_startHeight = 0.5f;
        protected float camera_height;

        public Vector2 camera_zoomRange;
        public float camera_startZoom = 0.8f;
        protected float camera_zoom;

        public float camera_startAngle = 0f;
        protected float camera_angle;

        public Volume volume;
        public float dofStartDistanceFar = 2.5f;
        public float dofEndDistanceFar = 100f;
        public float dofStartDistanceNear = 1f;
        public float dofEndDistanceNear = 1.5f;

        [Header("Character")]
        [SerializeField]
        protected SandboxCharacter character;

        public RectTransform customizableFeaturesGroupContainer;
        public GameObject customizableFeaturesGroupPrototype;
        private PrefabPool customizableFeaturesGroupPool;

        public RectTransform customizableFeaturesPanel;
        private UICategorizedList customizableFeaturesList;

        public class CustomizableFeatureGroup
        {
            public string name;
            public CustomizableFeatureGroup(string name)
            {
                this.name = name;
            }

            public readonly List<string> customizableFeatures = new List<string>();
            private static readonly List<SandboxCharacter.CustomizableFeatureController> _tempCustomizableFeatures = new List<SandboxCharacter.CustomizableFeatureController>();
            public void SortFeatures(SandboxCharacter character)
            {
                _tempCustomizableFeatures.Clear();

                foreach (var feature in customizableFeatures)
                {
                    var controller = character.GetFeatureController(feature);
                    if (controller == null) continue;

                    _tempCustomizableFeatures.Add(controller);
                }
                _tempCustomizableFeatures.Sort();
                customizableFeatures.Clear();
                foreach (var feature in _tempCustomizableFeatures)
                {
                    customizableFeatures.Add(feature.feature.id);
                }
                _tempCustomizableFeatures.Clear();
            }
        }
        private readonly Dictionary<string, CustomizableFeatureGroup> customizableFeatureGroups = new Dictionary<string, CustomizableFeatureGroup>();
        private readonly List<string> customizableFeatureGroupsOrdered = new List<string>();
        private static readonly List<string> _tempStrings = new List<string>();
        [Serializable]
        public struct CategorySortPriority
        {
            public string name;
            public string comesBefore;
            public string comesAfter;
            public bool forceIndex;
            public int forcedIndex;
        }
        private static bool TryGetCategorySortPriority(List<CategorySortPriority> sortPriorities, string categoryName, out CategorySortPriority sortPriority)
        {
            sortPriority = default;

            foreach (var priority in sortPriorities)
            {
                if (priority.name == categoryName)
                {
                    sortPriority = priority;
                    return true;
                }
            }

            return false;
        }

        private List<Transform> categoryTransforms = new List<Transform>();
        public List<CategorySortPriority> categorySortPriorities = new List<CategorySortPriority>();
        public void SortCustomizableFeatureGroups() 
        {
            _tempStrings.Clear();
            _tempStrings.AddRange(customizableFeatureGroupsOrdered);

            foreach(var category in _tempStrings)
            {
                if (TryGetCategorySortPriority(categorySortPriorities, category, out var sortPriority))
                {
                    int currentIndex = customizableFeatureGroupsOrdered.IndexOf(category);

                    if (sortPriority.forceIndex && sortPriority.forcedIndex >= 0 && sortPriority.forcedIndex < customizableFeatureGroups.Count - 1)
                    {
                        if (currentIndex >= 0)
                        {
                            customizableFeatureGroupsOrdered.RemoveAt(currentIndex);
                            customizableFeatureGroupsOrdered.Insert(sortPriority.forcedIndex, category);
                            currentIndex = sortPriority.forcedIndex;
                        }
                    }
                    
                    if (!string.IsNullOrWhiteSpace(sortPriority.comesBefore))
                    {
                        
                        if (currentIndex >= 0)
                        {
                            customizableFeatureGroupsOrdered.RemoveAt(currentIndex);
                            int targetIndex = customizableFeatureGroupsOrdered.IndexOf(sortPriority.comesBefore); 
                            if (targetIndex >= 0)
                            {
                                customizableFeatureGroupsOrdered.Insert(targetIndex, category);
                            } 
                            else
                            {
                                customizableFeatureGroupsOrdered.Insert(currentIndex, category);
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(sortPriority.comesAfter))
                    {
                        if (currentIndex >= 0)
                        {
                            customizableFeatureGroupsOrdered.RemoveAt(currentIndex);
                            int targetIndex = customizableFeatureGroupsOrdered.IndexOf(sortPriority.comesAfter);
                            if (targetIndex >= 0)
                            {
                                customizableFeatureGroupsOrdered.Insert(targetIndex + 1, category);  
                            }
                            else
                            {
                                customizableFeatureGroupsOrdered.Insert(currentIndex, category);
                            }
                        }
                    }
                }
            }

            _tempStrings.Clear();

            if (customizableFeaturesGroupContainer != null)
            {
                categoryTransforms.Clear();
                for (int a = 0; a < customizableFeaturesGroupContainer.childCount; a++)
                {
                    var child = customizableFeaturesGroupContainer.GetChild(a);
                    categoryTransforms.Add(child);
                }
                foreach(var child in categoryTransforms)
                {
                    int ind = customizableFeatureGroupsOrdered.IndexOf(child.name); 
                    if (ind >= 0) 
                    {
                        child.SetSiblingIndex(ind);
                    }
                }
            }
        }

        private static readonly List<UICategorizedList.Category> _tempCategories = new List<UICategorizedList.Category>();
        public List<CategorySortPriority> subCategorySortPriorities = new List<CategorySortPriority>();
        public void SortCustomizableFeaturesList()
        {
            customizableFeaturesList.SortCategoryListBySiblingIndex();

            _tempCategories.Clear();
            for (int a = 0; a < customizableFeaturesList.CategoryCount; a++)
            {
                var category = customizableFeaturesList.GetCategory(a);
                if (category == null) continue;

                _tempCategories.Add(category);
            }

            foreach(var category in _tempCategories)
            {
                if (TryGetCategorySortPriority(subCategorySortPriorities, category.name, out var sortPriority))
                {
                    if (!string.IsNullOrWhiteSpace(sortPriority.comesBefore))
                    {
                        int targetIndex = customizableFeaturesList.GetCategoryIndex(sortPriority.comesBefore);
                        if (targetIndex >= 0)
                        {
                            customizableFeaturesList.MoveCategory(category, targetIndex);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(sortPriority.comesAfter))
                    {
                        int targetIndex = customizableFeaturesList.GetCategoryIndex(sortPriority.comesAfter);
                        if (targetIndex >= 0)
                        {
                            customizableFeaturesList.MoveCategory(category, targetIndex + 1);
                        }
                    }
                } 
            }

            _tempCategories.Clear();
        }

        private class SliderListenerGroup : IDisposable
        {
            public Slider slider;

            public UnityAction<float> sliderListener;

            public SandboxCharacter.ValueGroup valueGroup;
            public UnityAction valueGroupListener;

            public void Dispose()
            {
                if (slider != null && slider.onValueChanged != null && sliderListener != null) slider.onValueChanged.RemoveListener(sliderListener);
                if (valueGroup != null && valueGroup.OnValueChange != null && valueGroupListener != null) valueGroup.OnValueChange.RemoveListener(valueGroupListener); 
            }
        }
        private readonly List<SliderListenerGroup> sliderListeners = new List<SliderListenerGroup>();  
        private void UnsubscribeValueListeners()
        {
            foreach(var entry in sliderListeners)
            {
                entry.Dispose();
            }

            sliderListeners.Clear();
        }

        [Header("UI Elements")]
        public TabGroup mainTabGroup;
        public GameObject leftPanel;
        public GameObject rightPanel;

        [Serializable]
        public enum MainTab
        {
            Identity, Look, Fit, Traits, Finalize
        }
        protected MainTab mainTab = MainTab.Identity;
        public void SetMainTab(MainTab tab)
        {
            CloseLeftPanel();

            mainTab = tab;
            if (mainTabGroup != null)
            {
                mainTabGroup.SetSelectedTab(mainTabGroup.IndexOf(tab.ToString()));
            }

            if (leftPanel != null)
            {
                leftPanel.SetActive(true);
            }
            switch (tab)
            {
                case MainTab.Identity:
                    if (identityPanel != null)
                    {
                        identityPanel.gameObject.SetActive(true);
                    }
                    break;

                case MainTab.Look:
                    OpenLookTab();
                    break;

                case MainTab.Fit:
                    if (fitPanel != null)
                    {
                        fitPanel.gameObject.SetActive(true);
                    }
                    break;

                case MainTab.Traits:
                    if (traitsPanel != null)
                    {
                        traitsPanel.gameObject.SetActive(true);
                    }
                    break;

                case MainTab.Finalize:
                    if (finalizePanel != null)
                    {
                        finalizePanel.gameObject.SetActive(true);
                    }
                    break;
            }

            CloseRightPanel();
        }
        public RectTransform identityPanel;
        public RectTransform lookPanel;
        public RectTransform fitPanel;
        public RectTransform traitsPanel;
        public RectTransform finalizePanel;
        public void CloseLeftPanel()
        {
            if (identityPanel != null)
            {
                identityPanel.gameObject.SetActive(false);
            }
            if (lookPanel != null)
            {
                lookPanel.gameObject.SetActive(false);
            }
            if (fitPanel != null)
            {
                fitPanel.gameObject.SetActive(false);
            }
            if (traitsPanel != null)
            {
                traitsPanel.gameObject.SetActive(false);
            }
            if (finalizePanel != null)
            {
                finalizePanel.gameObject.SetActive(false);
            }

            if (leftPanel != null)
            {
                leftPanel.SetActive(false);
            }
        }

        public Button backgroundColorButton;
        public Color backgroundStartColor;
        public Material backgroundColorMaterial;

        [Serializable]
        public class TabGroup
        {
            [Serializable]
            public class Tab
            {
                public string id;
                public Button button;
                [NonSerialized]
                public Color baseColor;
                public bool overrideSelectedColor;
                public Color selectedColor;
            }

            public Color selectedColor;
            public List<Tab> tabs = new List<Tab>();

            public void SetSelectedTab(int index)
            {
                if (tabs == null || tabs.Count == 0) return;
                for (int a = 0; a < tabs.Count; a++)
                {
                    var tab = tabs[a];
                    if (tab == null || tab.button == null) continue;

                    var colors = tab.button.colors;
                    colors.normalColor = (a == index) ? (tab.overrideSelectedColor ? tab.selectedColor : selectedColor) : tab.baseColor;
                    tab.button.colors = colors;
                }
            }

            public Tab FindTab(string id)
            {
                if (tabs == null || tabs.Count == 0 || id == null) return null;

                id = id.AsID();
                foreach(var tab in tabs)
                {
                    if (tab == null || tab.id == null) continue;
                    if (tab.id.AsID() == id) return tab;
                }
                return null;
            }
            public int IndexOf(string id)
            {
                if (tabs == null || tabs.Count == 0 || id == null) return -1;

                id = id.AsID();
                for(int a = 0; a < tabs.Count; a++)
                {
                    var tab = tabs[a];
                    if (tab == null || tab.id == null) continue;
                    if (tab.id.AsID() == id) return a;
                }
                return -1;
            }

            public void Initialize()
            {
                if (tabs != null)
                {
                    for (int a = 0; a < tabs.Count; a++)
                    {
                        var tab = tabs[a];
                        if (tab == null || tab.button == null) continue;

                        int index = a;
                        if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                        tab.button.onClick.AddListener(() => SetSelectedTab(index));

                        tab.baseColor = tab.button.colors.normalColor;
                    }
                }
            }
        }

        public void CloseRightPanel()
        {
            if (customizableFeaturesPanel != null)
            {
                customizableFeaturesPanel.gameObject.SetActive(false);
            }
            if (eyeCustomizationWindow != null)
            {
                eyeCustomizationWindow.gameObject.SetActive(false);
            }
            if (skinCustomizationWindow != null)
            {
                skinCustomizationWindow.gameObject.SetActive(false);
            }
            if (makeupCustomizationWindow != null)
            {
                makeupCustomizationWindow.gameObject.SetActive(false);
            }
            if (sexCustomizationWindow != null)
            {
                sexCustomizationWindow.gameObject.SetActive(false);
            }
            if (hairCustomizationWindow != null)
            {
                hairCustomizationWindow.gameObject.SetActive(false); 
            }

            if (rightPanel != null)
            {
                rightPanel.SetActive(false);
            }
        }

        public string graphic_colorPropertyName = "_Color";
        public string graphic_hdrColorPropertyName = "_ColorHDR";

        [Serializable]
        public class ColorPickableGraphic
        {
            public string id;
            public Graphic graphic;
            public Button button;
            public bool instantiateMaterial = true;
            public Material material;
            public ColorPickerID colorPickerID;
            public bool ignoreAlpha = true;

            [NonSerialized]
            public string colorPropertyName;
            [NonSerialized]
            public Color storedColor = Color.white;

            public void SetColor(Color color)
            {
                storedColor = color;

                if (material != null) 
                { 
                    material.SetColor(colorPropertyName, ignoreAlpha ? color.Opaque() : color); 
                    if (graphic != null)
                    {
                        graphic.enabled = false;
                        graphic.enabled = true;
                    }
                }
            }

            public UnityEvent<Color> onColorChange = new UnityEvent<Color>();
            public void AddListener(UnityAction<Color> listener)
            {
                if (onColorChange == null) onColorChange = new UnityEvent<Color>();
                onColorChange.AddListener(listener);
            }
            public void RemoveListener(UnityAction<Color> listener)
            {
                if (onColorChange == null || listener == null) return;
                onColorChange.RemoveListener(listener);
            }
            public void ClearListeners()
            {
                if (onColorChange == null) return;
                onColorChange.RemoveAllListeners();
            }
        }

        [Serializable]
        public enum ColorPickerID
        {
            left, leftAlpha, leftHDR, leftHDRAlpha, middle, middleAlpha, middleHDR, middleHDRAlpha, right, rightAlpha, rightHDR, rightHDRAlpha
        }

        public FlexibleColorPicker colorPickerLeft;
        public FlexibleColorPicker colorPickerLeftAlpha;
        public FCP_HDR colorPickerLeftHDR;
        public FCP_HDR colorPickerLeftHDRAlpha;

        public FlexibleColorPicker colorPickerRight;
        public FlexibleColorPicker colorPickerRightAlpha;
        public FCP_HDR colorPickerRightHDR;
        public FCP_HDR colorPickerRightHDRAlpha;

        public void OpenColorPicker(FlexibleColorPicker fcp, Vector3 worldPosition, UnityAction<Color> listener, Color startColor)
        {
            if (fcp == null || listener == null) return;

            if (fcp.onColorChange == null) fcp.onColorChange = new FlexibleColorPicker.ColorUpdateEvent();
            fcp.onColorChange.RemoveAllListeners();
            fcp.gameObject.SetActive(true);
            fcp.SetColorWithoutNotify(startColor);
            fcp.onColorChange.AddListener(listener);

            var popup = fcp.GetComponent<UIPopup>();
            if (popup != null)
            {
                popup.MoveTo(worldPosition);
            }
            else
            {
                fcp.transform.position = worldPosition;
            }
        }
        public void OpenColorPicker(FCP_HDR fcp_hdr, Vector3 worldPosition, UnityAction<Color> listener, Color startColorHDR)
        {
            if (fcp_hdr == null || listener == null) return;

            if (fcp_hdr.onColorChange == null) fcp_hdr.onColorChange = new UnityEvent<Color>();
            fcp_hdr.onColorChange.RemoveAllListeners();
            fcp_hdr.gameObject.SetActive(true);
            fcp_hdr.SetColorWithoutNotify(startColorHDR);
            fcp_hdr.onColorChange.AddListener(listener);

            var popup = fcp_hdr.GetComponent<UIPopup>();
            if (popup != null)
            {
                popup.MoveTo(worldPosition);
            }
            else
            {
                fcp_hdr.transform.position = worldPosition; 
            }

        }
        public void OpenColorPicker(ColorPickerID id, Vector3 worldPosition, UnityAction<Color> listener, Color startColor)
        {
            switch(id)
            {
                case ColorPickerID.leftHDR:
                    OpenColorPicker(colorPickerLeftHDR, worldPosition, listener, startColor);
                    break;
                case ColorPickerID.leftHDRAlpha:
                    OpenColorPicker(colorPickerLeftHDRAlpha, worldPosition, listener, startColor);
                    break;
                case ColorPickerID.left:
                    OpenColorPicker(colorPickerLeft, worldPosition, listener, startColor);
                    break;
                case ColorPickerID.leftAlpha:
                    OpenColorPicker(colorPickerLeftAlpha, worldPosition, listener, startColor);
                    break;

                case ColorPickerID.rightHDR:
                    OpenColorPicker(colorPickerRightHDR, worldPosition, listener, startColor);
                    break;
                case ColorPickerID.rightHDRAlpha:
                    OpenColorPicker(colorPickerRightHDRAlpha, worldPosition, listener, startColor); 
                    break;
                case ColorPickerID.right:
                    OpenColorPicker(colorPickerRight, worldPosition, listener, startColor);
                    break;
                case ColorPickerID.rightAlpha:
                    OpenColorPicker(colorPickerRightAlpha, worldPosition, listener, startColor);
                    break;
            }
        }

        public List<ColorPickableGraphic> colorPickableGraphics = new List<ColorPickableGraphic>();
        public ColorPickableGraphic GetColorPickableGraphic(string id)
        {
            if (colorPickableGraphics == null) return null;

            foreach (var entry in colorPickableGraphics)
            {
                if (entry.id == id) return entry;
            }
            return null;
        }
        public ColorPickableGraphic GetColorPickableGraphic(Graphic graphic)
        {
            if (colorPickableGraphics == null) return null;

            foreach (var entry in colorPickableGraphics)
            {
                if (entry.graphic == graphic) return entry;
            }
            return null;
        }

        #region Eyes
        public const string _eyeCustomizationCategoryName = "Eye Color";
        public const string id_irisColor1 = "irisColor1";
        public const string id_irisColor2 = "irisColor2";
        public const string id_irisColor3 = "irisColor3";
        public const string id_scleraTint = "scleraTint";
        public const string id_scleraColor1 = "scleraColor1";
        public const string id_scleraColor2 = "scleraColor2";
        public const string id_scleraColor3 = "scleraColor3";

        [Header("Eyes")]
        public RectTransform eyeCustomizationWindow;
        public TabGroup eyeEditModeGroup;

        public RectTransform irisStyleLayoutTransform;
        private PrefabPool irisStylePool;
        public GameObject irisStylePrototype;
        public Material irisStyleMaterial;
        public Material scleraStyleColorMaskMaterial;
        private readonly List<Graphic> eyeStyleMaterialUsers = new List<Graphic>();
        private readonly List<Graphic> scleraStyleDefaultMaterialUsers = new List<Graphic>();
        private void RefreshEyeStyleMaterials()
        {
            var singleEyeEditMode = SingleEyeEditMode;

            Color irisColorA = character.LeftIrisColorA.Opaque();
            Color irisColorB = character.LeftIrisColorB.Opaque();
            Color irisColorC = character.LeftIrisColorC.Opaque();
            Color scleraTint = character.LeftScleraTint.Opaque();
            Color scleraColorA = character.LeftScleraColorA.Opaque();
            Color scleraColorB = character.LeftScleraColorB.Opaque();
            Color scleraColorC = character.LeftScleraColorC.Opaque(); 
            switch (singleEyeEditMode)
            {
                case EyeEditMode.Both:
                case EyeEditMode.Left:
                    if (scleraColorsRoot != null)
                    {
                        scleraColorsRoot.gameObject.SetActive(character.LeftEyeMaterial.GetFloat(SandboxCharacter.matProp_scleraMapIsMask) > 0f);
                    }
                    break;

                case EyeEditMode.Right:
                    if (scleraColorsRoot != null)
                    {
                        scleraColorsRoot.gameObject.SetActive(character.RightEyeMaterial.GetFloat(SandboxCharacter.matProp_scleraMapIsMask) > 0f); 
                    }

                    irisColorA = character.RightIrisColorA.Opaque();
                    irisColorB = character.RightIrisColorB.Opaque();
                    irisColorC = character.RightIrisColorC.Opaque();
                    scleraTint = character.RightScleraTint.Opaque();
                    scleraColorA = character.RightScleraColorA.Opaque();
                    scleraColorB = character.RightScleraColorB.Opaque();
                    scleraColorC = character.RightScleraColorC.Opaque();
                    break;
            }

            irisStyleMaterial.SetColor("_ColorA", irisColorA);
            irisStyleMaterial.SetColor("_ColorB", irisColorB);
            irisStyleMaterial.SetColor("_ColorC", irisColorC);
            scleraStyleColorMaskMaterial.SetColor("_Tint", scleraTint);
            scleraStyleColorMaskMaterial.SetColor("_ColorA", scleraColorA);
            scleraStyleColorMaskMaterial.SetColor("_ColorB", scleraColorB);
            scleraStyleColorMaskMaterial.SetColor("_ColorC", scleraColorC);
            foreach (var img in eyeStyleMaterialUsers)
            {
                img.enabled = false;
                img.enabled = true;
            }
            foreach (var img in scleraStyleDefaultMaterialUsers) img.color = scleraTint;
        }
        private void ClearEyeStyles()
        {
            if (irisStyleLayoutTransform != null && irisStylePool != null)
            {
                irisStylePool.ReleaseAllClaims();
                for (int a = 0; a < irisStyleLayoutTransform.childCount; a++)
                {
                    var child = irisStyleLayoutTransform.GetChild(a);
                    if (child == null) continue;
                    child.gameObject.SetActive(false);
                }
            }
        }
        private GameObject ClaimNewEyeStyleInstance(out RawImage irisImage, out RawImage scleraImage)
        {
            irisImage = null;
            scleraImage = null;
            if (irisStylePool == null || !irisStylePool.TryGetNewInstance(out GameObject instance)) return null;
            if (instance == null) return null;

            instance.SetActive(true);

            var irisObj = instance.transform.FindDeepChildLiberal("Iris");
            irisImage = irisObj == null ? null : irisObj.GetComponentInChildren<RawImage>(true);
            if (irisImage != null) irisImage.material = irisStyleMaterial;

            var scleraObj = instance.transform.FindDeepChildLiberal("Sclera");
            scleraImage = scleraObj == null ? null : scleraObj.GetComponentInChildren<RawImage>(true);
            if (scleraImage != null) scleraImage.material = scleraImage.defaultMaterial;
            
            return instance;
        }

        public RectTransform irisColorLayoutTransform;
        private PrefabPool irisColorPool;
        public GameObject irisColorPrototype;
        private ObjectPool<Material> irisColorMaterials;
        private ObjectPool<Material> scleraColorMaterials;
        private void RefreshEyeColorPresets()
        {
            var singleEyeEditMode = SingleEyeEditMode;

            string irisTextureID = character.LeftIrisTextureID;
            switch (singleEyeEditMode)
            {
                case EyeEditMode.Right:
                    irisTextureID = character.RightIrisTextureID; 
                    break;
            }
            if (string.IsNullOrWhiteSpace(irisTextureID))
            {
                if (character.irisTextures == null || character.irisTextures.Count <= 0)
                {
                    ClearEyeColorPresets();
                    return;
                } 
                else
                {
                    irisTextureID = character.irisTextures.GetTexture(0).id; 
                }
            }

            Texture irisTexture = null;
            Texture scleraTexture = null;
            bool scleraIsColorMask = false;
            if (character.irisTextures != null && character.irisTextures.TryGetTexture(irisTextureID, out var irisTexture_))
            {
                irisTexture = irisTexture_.texture;

                if (character.scleraTextures != null && character.scleraTextures.TryGetTexture(irisTexture_.GetAttributeValue(SandboxCharacter.attr_irisScleraPair), out var scleraTexture_))
                {
                    scleraTexture = scleraTexture_.texture;
                    scleraIsColorMask = scleraTexture_.HasAttribute(SandboxCharacter.attr_isMask);
                } 
            }

            ClearEyeColorPresets();
            if (character.eyeColorPresets != null)
            {
                for (int a = 0; a < character.eyeColorPresets.Count; a++)
                {
                    var preset = character.eyeColorPresets.GetPalette(a);
                    if (preset == null) continue;

                    var inst = ClaimNewEyeColorInstance(scleraIsColorMask, out var irisImage, out var scleraImage, out var irisMaterial, out var scleraMaterial);
                    if (inst != null)
                    {
                        
                        inst.name = preset.name;
                        Color irisColorA = preset.GetColor("irisA").Opaque();
                        Color irisColorB = preset.GetColor("irisB").Opaque();
                        Color irisColorC = preset.GetColor("irisC").Opaque();

                        Color scleraTint;
                        if (!preset.TryGetColor(out scleraTint, "scleraTint")) scleraTint = Color.white; else scleraTint = scleraTint.Opaque();
                        Color scleraColorA;
                        if (!preset.TryGetColor(out scleraColorA, "scleraA")) scleraColorA = Color.red; else scleraColorA = scleraColorA.Opaque();
                        Color scleraColorB;
                        if (!preset.TryGetColor(out scleraColorB, "scleraB")) scleraColorB = Color.green; else scleraColorB = scleraColorB.Opaque();
                        Color scleraColorC;
                        if (!preset.TryGetColor(out scleraColorC, "scleraC")) scleraColorC = Color.blue; else scleraColorC = scleraColorC.Opaque();

                        irisMaterial.SetColor("_ColorA", irisColorA);
                        irisMaterial.SetColor("_ColorB", irisColorB);
                        irisMaterial.SetColor("_ColorC", irisColorC);

                        if (scleraMaterial != null)
                        {
                            scleraMaterial.SetColor("_Tint", scleraTint);
                            scleraMaterial.SetColor("_ColorA", scleraColorA);
                            scleraMaterial.SetColor("_ColorB", scleraColorB);
                            scleraMaterial.SetColor("_ColorC", scleraColorC);

                            scleraImage.color = Color.white;
                        }
                        else
                        {
                            scleraImage.color = scleraTint;
                        }

                        irisImage.texture = irisTexture;
                        scleraImage.texture = scleraTexture; 

                        irisImage.enabled = false;
                        irisImage.enabled = true;

                        scleraImage.enabled = false;
                        scleraImage.enabled = true;

                        var button = inst.GetComponentInChildren<Button>(true);
                        if (button != null)
                        {
                            if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                            button.onClick.RemoveAllListeners();
                            button.onClick.AddListener(() =>
                            {
                                ColorPickableGraphic colorPickable;
                                colorPickable = GetColorPickableGraphic(id_irisColor1);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(irisColorA);
                                }
                                colorPickable = GetColorPickableGraphic(id_irisColor2);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(irisColorB);
                                }
                                colorPickable = GetColorPickableGraphic(id_irisColor3);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(irisColorC);
                                }

                                colorPickable = GetColorPickableGraphic(id_scleraTint);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(scleraTint);
                                }
                                colorPickable = GetColorPickableGraphic(id_scleraColor1);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(scleraColorA);
                                }
                                colorPickable = GetColorPickableGraphic(id_scleraColor2);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(scleraColorB);
                                }
                                colorPickable = GetColorPickableGraphic(id_scleraColor3);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(scleraColorC);
                                }

                                switch(eyeEditMode)
                                {
                                    case EyeEditMode.Both:
                                        character.LeftIrisColorA = irisColorA;
                                        character.LeftIrisColorB = irisColorB;
                                        character.LeftIrisColorC = irisColorC;
                                        character.LeftScleraTint = scleraTint;
                                        character.LeftScleraColorA = scleraColorA;
                                        character.LeftScleraColorB = scleraColorB;
                                        character.LeftScleraColorC = scleraColorC;

                                        character.RightIrisColorA = irisColorA;
                                        character.RightIrisColorB = irisColorB;
                                        character.RightIrisColorC = irisColorC;
                                        character.RightScleraTint = scleraTint;
                                        character.RightScleraColorA = scleraColorA;
                                        character.RightScleraColorB = scleraColorB;
                                        character.RightScleraColorC = scleraColorC;
                                        break;

                                    case EyeEditMode.Left:
                                        character.LeftIrisColorA = irisColorA;
                                        character.LeftIrisColorB = irisColorB;
                                        character.LeftIrisColorC = irisColorC;
                                        character.LeftScleraTint = scleraTint;
                                        character.LeftScleraColorA = scleraColorA;
                                        character.LeftScleraColorB = scleraColorB;
                                        character.LeftScleraColorC = scleraColorC;
                                        break;

                                    case EyeEditMode.Right:
                                        character.RightIrisColorA = irisColorA;
                                        character.RightIrisColorB = irisColorB;
                                        character.RightIrisColorC = irisColorC;
                                        character.RightScleraTint = scleraTint;
                                        character.RightScleraColorA = scleraColorA;
                                        character.RightScleraColorB = scleraColorB;
                                        character.RightScleraColorC = scleraColorC; 
                                        break;
                                }

                                RefreshEyeStyleMaterials();
                            });
                        }
                    }
                }
            }
        }
        private void ClearEyeColorPresets()
        {
            if (irisColorLayoutTransform != null && irisColorPool != null)
            {
                irisColorPool.ReleaseAllClaims();
                for (int a = 0; a < irisColorLayoutTransform.childCount; a++)
                {
                    var child = irisColorLayoutTransform.GetChild(a);
                    if (child == null) continue;
                    child.gameObject.SetActive(false);
                }
            }
        }
        private GameObject ClaimNewEyeColorInstance(bool useScleraColorMaskMaterial, out RawImage irisImage, out RawImage scleraImage, out Material irisMaterial, out Material scleraMaterial)
        {
            irisImage = null;
            scleraImage = null;
            irisMaterial = null;
            scleraMaterial = null;
            if (irisColorPool == null || !irisColorPool.TryGetNewInstance(out GameObject instance)) return null;
            if (instance == null) return null;

            instance.SetActive(true);

            var irisObj = instance.transform.FindDeepChildLiberal("Iris");
            irisImage = irisObj == null ? null : irisObj.GetComponentInChildren<RawImage>(true);
            if (irisImage != null)
            {
                irisMaterial = irisImage.material;
                if (irisMaterial == null || !irisColorMaterials.IsInPool(irisMaterial))
                {
                    irisColorMaterials.TryGetNewInstance(out irisMaterial);
                    if (irisMaterial == null)
                    {
                        irisImage = null;
                        instance.SetActive(false);
                        irisColorPool.Release(instance);
                        return null;
                    }
                }

                irisImage.material = irisMaterial;
            }

            var scleraObj = instance.transform.FindDeepChildLiberal("Sclera");
            scleraImage = scleraObj == null ? null : scleraObj.GetComponentInChildren<RawImage>(true);
            if (scleraImage != null)
            {
                scleraMaterial = scleraImage.material;
                if (useScleraColorMaskMaterial)
                {
                    if (scleraMaterial == null || !scleraColorMaterials.IsInPool(scleraMaterial))
                    {
                        scleraColorMaterials.TryGetNewInstance(out scleraMaterial);
                        if (scleraMaterial == null)
                        {
                            irisImage = null;
                            scleraImage = null;
                            scleraMaterial = null;
                            instance.SetActive(false);
                            irisColorPool.Release(instance);
                            return null;
                        }

                        scleraImage.material = scleraMaterial;
                    }
                }
                else
                {
                    if (scleraMaterial != null && scleraColorMaterials.IsInPool(scleraMaterial))
                    {
                        scleraColorMaterials.Release(scleraMaterial);
                    }

                    scleraMaterial = null;
                    scleraImage.material = scleraImage.defaultMaterial; 
                }
            }

            return instance;
        }

        //private RectTransform irisColorsRoot;
        private RectTransform scleraColorsRoot;
        //private RectTransform scleraTintRoot;
        [Serializable]
        public enum EyeEditMode
        {
            Both, Left, Right
        }
        protected EyeEditMode eyeEditMode = EyeEditMode.Both; 
        protected EyeEditMode singleEyeEditMode = EyeEditMode.Left;
        protected EyeEditMode SingleEyeEditMode => eyeEditMode == EyeEditMode.Both ? singleEyeEditMode : eyeEditMode;
        public void SetEyeEditMode(EyeEditMode mode) => SetEyeEditMode(mode, false);
        public void SetEyeEditMode(EyeEditMode mode, bool forceUpdate)
        {
            var prevMode = eyeEditMode;
            eyeEditMode = mode;
            if (mode == prevMode && !forceUpdate) return;
            if (singleEyeEditMode != prevMode && prevMode != EyeEditMode.Both) singleEyeEditMode = prevMode; 

            if (eyeEditModeGroup != null)
            {
                eyeEditModeGroup.SetSelectedTab(eyeEditModeGroup.IndexOf(mode.ToString()));
            }

            var singleEditMode = SingleEyeEditMode;
            if (character != null)
            {
                ColorPickableGraphic colorPickable; 

                string irisTextureID = character.LeftIrisTextureID;
                Color irisColorA = character.LeftIrisColorA.Opaque();
                Color irisColorB = character.LeftIrisColorB.Opaque();
                Color irisColorC = character.LeftIrisColorC.Opaque();
                Color scleraTint = character.LeftScleraTint.Opaque();
                Color scleraColorA = character.LeftScleraColorA.Opaque();
                Color scleraColorB = character.LeftScleraColorB.Opaque();
                Color scleraColorC = character.LeftScleraColorC.Opaque();
                switch (singleEditMode) 
                {
                    case EyeEditMode.Both:
                    case EyeEditMode.Left:
                        colorPickable = GetColorPickableGraphic(id_irisColor1);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.LeftIrisColorA);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            {
                                character.LeftIrisColorA = color.Opaque(); 
                                if (eyeEditMode == EyeEditMode.Both) character.RightIrisColorA = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        colorPickable = GetColorPickableGraphic(id_irisColor2);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.LeftIrisColorB);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.LeftIrisColorB = color.Opaque(); 
                                if (eyeEditMode == EyeEditMode.Both) character.RightIrisColorB = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        colorPickable = GetColorPickableGraphic(id_irisColor3);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.LeftIrisColorC);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.LeftIrisColorC = color.Opaque(); 
                                if (eyeEditMode == EyeEditMode.Both) character.RightIrisColorC = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }

                        colorPickable = GetColorPickableGraphic(id_scleraTint);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.LeftScleraTint);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.LeftScleraTint = color.Opaque(); 
                                if (eyeEditMode == EyeEditMode.Both) character.RightScleraTint = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        colorPickable = GetColorPickableGraphic(id_scleraColor1);
                        if (colorPickable != null && colorPickable.material != null) 
                        {
                            colorPickable.SetColor(character.LeftScleraColorA);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.LeftScleraColorA = color.Opaque(); 
                                if (eyeEditMode == EyeEditMode.Both) character.RightScleraColorA = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        colorPickable = GetColorPickableGraphic(id_scleraColor2);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.LeftScleraColorB);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.LeftScleraColorB = color.Opaque(); 
                                if (eyeEditMode == EyeEditMode.Both) character.RightScleraColorB = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        colorPickable = GetColorPickableGraphic(id_scleraColor3);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.LeftScleraColorC);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.LeftScleraColorC = color.Opaque(); 
                                if (eyeEditMode == EyeEditMode.Both) character.RightScleraColorC = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        break;

                    case EyeEditMode.Right:
                        colorPickable = GetColorPickableGraphic(id_irisColor1);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.RightIrisColorA);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.RightIrisColorA = color.Opaque();
                                if (eyeEditMode == EyeEditMode.Both) character.LeftIrisColorA = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        colorPickable = GetColorPickableGraphic(id_irisColor2);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.RightIrisColorB);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.RightIrisColorB = color.Opaque();
                                if (eyeEditMode == EyeEditMode.Both) character.LeftIrisColorB = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        colorPickable = GetColorPickableGraphic(id_irisColor3);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.RightIrisColorC);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.RightIrisColorC = color.Opaque();
                                if (eyeEditMode == EyeEditMode.Both) character.LeftIrisColorC = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }

                        colorPickable = GetColorPickableGraphic(id_scleraTint);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.RightScleraTint);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.RightScleraTint = color.Opaque();
                                if (eyeEditMode == EyeEditMode.Both) character.LeftScleraTint= color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        colorPickable = GetColorPickableGraphic(id_scleraColor1);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.RightScleraColorA);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.RightScleraColorA = color.Opaque();
                                if (eyeEditMode == EyeEditMode.Both) character.LeftScleraColorA = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        colorPickable = GetColorPickableGraphic(id_scleraColor2);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.RightScleraColorB);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.RightScleraColorB = color.Opaque();
                                if (eyeEditMode == EyeEditMode.Both) character.LeftScleraColorB = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }
                        colorPickable = GetColorPickableGraphic(id_scleraColor3);
                        if (colorPickable != null && colorPickable.material != null) 
                        { 
                            colorPickable.SetColor(character.RightScleraColorC);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) => 
                            { 
                                character.RightScleraColorC = color.Opaque();
                                if (eyeEditMode == EyeEditMode.Both) character.LeftScleraColorC = color.Opaque();
                                RefreshEyeStyleMaterials();
                            });
                        }

                        irisTextureID = character.RightIrisTextureID;
                        irisColorA = character.RightIrisColorA.Opaque();
                        irisColorB = character.RightIrisColorB.Opaque();
                        irisColorC = character.RightIrisColorC.Opaque();
                        scleraTint = character.RightScleraTint.Opaque();
                        scleraColorA = character.RightScleraColorA.Opaque();
                        scleraColorB = character.RightScleraColorB.Opaque();
                        scleraColorC = character.RightScleraColorC.Opaque();
                        break;
                }
                if (string.IsNullOrWhiteSpace(irisTextureID))
                {
                    if (character.irisTextures != null && character.irisTextures.Count > 0) 
                    {
                        irisTextureID = character.irisTextures.GetTexture(0).id;
                    }
                }

                ClearEyeStyles();
                eyeStyleMaterialUsers.Clear();
                scleraStyleDefaultMaterialUsers.Clear(); 
                if (character.irisTextures != null)
                {
                    if (character.irisTextures.TryGetTexture(irisTextureID, out var currentTex))
                    {
                        bool scleraIsColorMask = false;
                        if (character.scleraTextures != null && character.scleraTextures.TryGetTexture(currentTex.GetAttributeValue(SandboxCharacter.attr_irisScleraPair), out var currentScleraTex))
                        {
                            scleraIsColorMask = currentScleraTex.HasTag(SandboxCharacter.attr_isMask);
                        }

                        if (scleraColorsRoot != null)
                        {
                            scleraColorsRoot.gameObject.SetActive(scleraIsColorMask); 
                        }
                    }

                    for (int a = 0; a < character.irisTextures.Count; a++)
                    {
                        var irisTexture = character.irisTextures.GetTexture(a);
                        if (irisTexture.texture == null) continue;

                        var inst = ClaimNewEyeStyleInstance(out RawImage irisImage, out RawImage scleraImage);
                        if (inst != null)
                        {
                            if (irisImage != null) 
                            { 
                                irisImage.texture = irisTexture.previewSprite == null ? irisTexture.texture : irisTexture.previewSprite.texture;
                                eyeStyleMaterialUsers.Add(irisImage);
                            }

                            bool scleraMapIsColorMask = false;
                            if (scleraImage != null)
                            {
                                var asset = character.scleraTextures == null ? default : character.scleraTextures.Find(irisTexture.GetAttributeValue(SandboxCharacter.attr_irisScleraPair));
                                if (asset is SelectableTextureGroup.SelectableTexture scleraTexture && scleraTexture.texture != null)
                                {
                                    bool isColorMask = scleraTexture.HasTag(SandboxCharacter.attr_isMask);
                                    if (isColorMask)
                                    {
                                        scleraMapIsColorMask = true;
                                        scleraImage.color = Color.white;
                                        eyeStyleMaterialUsers.Add(scleraImage);  
                                    }
                                    else
                                    {
                                        scleraImage.color = scleraTint;
                                        scleraStyleDefaultMaterialUsers.Add(scleraImage); 
                                    }

                                    scleraImage.material = isColorMask ? scleraStyleColorMaskMaterial : scleraImage.defaultMaterial;
                                    scleraImage.texture = scleraTexture.previewSprite == null ? scleraTexture.texture : scleraTexture.previewSprite.texture;
                                }
                                else
                                {
                                    scleraImage.material = scleraImage.defaultMaterial;
                                    scleraImage.texture = null;
                                    scleraStyleDefaultMaterialUsers.Add(scleraImage); 
                                }
                            }

                            var button = inst.GetComponentInChildren<Button>(true);
                            if (button != null)
                            {
                                if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                                button.onClick.RemoveAllListeners();
                                button.onClick.AddListener(() => 
                                {
                                    switch (eyeEditMode)
                                    {
                                        case EyeEditMode.Both:
                                            character.SetLeftIrisTexture(irisTexture.id);
                                            character.SetRightIrisTexture(irisTexture.id);
                                            break;
                                        case EyeEditMode.Left:
                                            character.SetLeftIrisTexture(irisTexture.id);
                                            break;
                                        case EyeEditMode.Right:
                                            character.SetRightIrisTexture(irisTexture.id);
                                            break;
                                    }

                                    RefreshEyeColorPresets();

                                    if (scleraColorsRoot != null)
                                    {
                                        scleraColorsRoot.gameObject.SetActive(scleraMapIsColorMask); 
                                    }
                                });
                            }
                        }
                    }
                }

                var pupilShapeRoot = eyeCustomizationWindow.FindDeepChildLiberal("pupilShape");
                if (pupilShapeRoot != null)
                {
                    var sliderHor = pupilShapeRoot.FindDeepChildLiberal("shapeHorizontal")?.GetComponent<Slider>();
                    if (sliderHor != null)
                    {
                        if (sliderHor.onValueChanged == null) sliderHor.onValueChanged = new Slider.SliderEvent();
                        sliderHor.onValueChanged.RemoveAllListeners();
                        sliderHor.SetValueWithoutNotify(eyeEditMode != EyeEditMode.Right ? character.PupilLeftShapeX : character.PupilRightShapeX);
                        sliderHor.onValueChanged.AddListener((value) => 
                        { 
                            switch(eyeEditMode)
                            {
                                case EyeEditMode.Both:
                                    character.PupilLeftShapeX = value;
                                    character.PupilRightShapeX = value; 
                                    break;
                                case EyeEditMode.Left:
                                    character.PupilLeftShapeX = value;
                                    break;
                                case EyeEditMode.Right:
                                    character.PupilRightShapeX = value; 
                                    break;
                            }
                        });
                    }

                    var sliderVer = pupilShapeRoot.FindDeepChildLiberal("shapeVertical")?.GetComponent<Slider>();
                    if (sliderVer != null)
                    {
                        if (sliderVer.onValueChanged == null) sliderVer.onValueChanged = new Slider.SliderEvent();
                        sliderVer.onValueChanged.RemoveAllListeners();
                        sliderVer.SetValueWithoutNotify(eyeEditMode != EyeEditMode.Right ? character.PupilLeftShapeY : character.PupilRightShapeY);
                        sliderVer.onValueChanged.AddListener((value) =>
                        {
                            switch (eyeEditMode)
                            {
                                case EyeEditMode.Both:
                                    character.PupilLeftShapeY = value;
                                    character.PupilRightShapeY = value;
                                    break;
                                case EyeEditMode.Left:
                                    character.PupilLeftShapeY = value;
                                    break;
                                case EyeEditMode.Right:
                                    character.PupilRightShapeY = value; 
                                    break;
                            }
                        });
                    }

                    CustomEditorUtils.SetButtonOnClickActionByName(pupilShapeRoot, "reset", () =>
                    {
                        switch (eyeEditMode)
                        {
                            case EyeEditMode.Both:
                                character.PupilLeftShapeX = 1f;
                                character.PupilRightShapeX = 1f;
                                character.PupilLeftShapeY = 1f;
                                character.PupilRightShapeY = 1f;
                                break;
                            case EyeEditMode.Left:
                                character.PupilLeftShapeX = 1f;
                                character.PupilLeftShapeY = 1f;
                                break;
                            case EyeEditMode.Right:
                                character.PupilRightShapeX = 1f;
                                character.PupilRightShapeY = 1f; 
                                break;
                        }

                        if (sliderHor != null) sliderHor.SetValueWithoutNotify(1f);
                        if (sliderVer != null) sliderVer.SetValueWithoutNotify(1f);
                    }, true, true, true);
                }

                var irisSizeRoot = eyeCustomizationWindow.FindDeepChildLiberal("irisSize");
                if (irisSizeRoot != null)
                {
                    var slider = irisSizeRoot.GetComponentInChildren<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();
                        slider.SetValueWithoutNotify(eyeEditMode != EyeEditMode.Right ? character.IrisLeftSize : character.IrisRightSize);
                        slider.onValueChanged.AddListener((value) =>
                        {
                            switch (eyeEditMode)
                            {
                                case EyeEditMode.Both:
                                    character.IrisLeftSize = value;
                                    character.IrisRightSize = value;
                                    break;
                                case EyeEditMode.Left:
                                    character.IrisLeftSize = value;
                                    break;
                                case EyeEditMode.Right:
                                    character.IrisRightSize = value;
                                    break;
                            }
                        });
                    }

                    CustomEditorUtils.SetButtonOnClickActionByName(irisSizeRoot, "reset", () =>
                    {
                        switch (eyeEditMode)
                        {
                            case EyeEditMode.Both:
                                character.IrisLeftSize = SandboxCharacter.eyeIrisSize_default;
                                character.IrisRightSize = SandboxCharacter.eyeIrisSize_default;
                                break;
                            case EyeEditMode.Left:
                                character.IrisLeftSize = SandboxCharacter.eyeIrisSize_default;
                                break;
                            case EyeEditMode.Right:
                                character.IrisRightSize = SandboxCharacter.eyeIrisSize_default;
                                break;
                        }

                        if (slider != null) slider.SetValueWithoutNotify(SandboxCharacter.eyeIrisSize_default);
                    }, true, true, true);
                }

                var refractionRoot = eyeCustomizationWindow.FindDeepChildLiberal("refraction");
                if (refractionRoot != null)
                {
                    var sliderIOR = refractionRoot.FindDeepChildLiberal("IOR")?.GetComponent<Slider>();
                    if (sliderIOR != null)
                    {
                        if (sliderIOR.onValueChanged == null) sliderIOR.onValueChanged = new Slider.SliderEvent();
                        sliderIOR.onValueChanged.RemoveAllListeners();
                        sliderIOR.SetValueWithoutNotify(math.remap(SandboxCharacter.eyeIOR_min, SandboxCharacter.eyeIOR_max, 0f, 1f, eyeEditMode != EyeEditMode.Right ? character.IrisLeftIOR : character.IrisRightIOR));
                        sliderIOR.onValueChanged.AddListener((value) =>
                        {
                            float ior = math.remap(0f, 1f, SandboxCharacter.eyeIOR_min, SandboxCharacter.eyeIOR_max, value);  
                            switch (eyeEditMode)
                            {
                                case EyeEditMode.Both:
                                    character.IrisLeftIOR = ior;
                                    character.IrisRightIOR = ior;
                                    break;
                                case EyeEditMode.Left:
                                    character.IrisLeftIOR = ior;
                                    break;
                                case EyeEditMode.Right:
                                    character.IrisRightIOR = ior;
                                    break;
                            }
                        });
                    }

                    var sliderOffset = refractionRoot.FindDeepChildLiberal("Offset")?.GetComponent<Slider>();
                    if (sliderOffset != null)
                    {
                        if (sliderOffset.onValueChanged == null) sliderOffset.onValueChanged = new Slider.SliderEvent();
                        sliderOffset.onValueChanged.RemoveAllListeners();
                        sliderOffset.SetValueWithoutNotify(eyeEditMode != EyeEditMode.Right ? character.IrisLeftRefractionOffset : character.IrisRightRefractionOffset);
                        sliderOffset.onValueChanged.AddListener((value) =>
                        {
                            switch (eyeEditMode)
                            {
                                case EyeEditMode.Both:
                                    character.IrisLeftRefractionOffset = value;
                                    character.IrisRightRefractionOffset = value;
                                    break;
                                case EyeEditMode.Left:
                                    character.IrisLeftRefractionOffset = value;
                                    break;
                                case EyeEditMode.Right:
                                    character.IrisRightRefractionOffset = value;
                                    break;
                            }
                        });
                    }

                    CustomEditorUtils.SetButtonOnClickActionByName(refractionRoot, "reset", () =>
                    {
                        switch (eyeEditMode)
                        {
                            case EyeEditMode.Both:
                                character.IrisLeftIOR = SandboxCharacter.eyeIOR_default;
                                character.IrisRightIOR = SandboxCharacter.eyeIOR_default;
                                character.IrisLeftRefractionOffset = SandboxCharacter.eyeRefractionOffset_default;
                                character.IrisRightRefractionOffset = SandboxCharacter.eyeRefractionOffset_default;
                                break;
                            case EyeEditMode.Left:
                                character.IrisLeftIOR = SandboxCharacter.eyeIOR_default;
                                character.IrisLeftRefractionOffset = SandboxCharacter.eyeRefractionOffset_default;
                                break;
                            case EyeEditMode.Right:
                                character.IrisRightIOR = SandboxCharacter.eyeIOR_default;
                                character.IrisRightRefractionOffset = SandboxCharacter.eyeRefractionOffset_default;
                                break;
                        }

                        if (sliderIOR != null) sliderIOR.SetValueWithoutNotify(math.remap(SandboxCharacter.eyeIOR_min, SandboxCharacter.eyeIOR_max, 0f, 1f, SandboxCharacter.eyeIOR_default));
                        if (sliderOffset != null) sliderOffset.SetValueWithoutNotify(SandboxCharacter.eyeRefractionOffset_default);
                    }, true, true, true);
                }

                var wetnessRoot = eyeCustomizationWindow.FindDeepChildLiberal("wetness");
                if (wetnessRoot != null)
                {
                    var sliderCornea = wetnessRoot.FindDeepChildLiberal("Cornea")?.GetComponent<Slider>();
                    if (sliderCornea != null)
                    {
                        if (sliderCornea.onValueChanged == null) sliderCornea.onValueChanged = new Slider.SliderEvent();
                        sliderCornea.onValueChanged.RemoveAllListeners();
                        sliderCornea.SetValueWithoutNotify(eyeEditMode != EyeEditMode.Right ? character.CorneaLeftWetness : character.CorneaRightWetness);
                        sliderCornea.onValueChanged.AddListener((value) =>
                        {
                            switch (eyeEditMode)
                            {
                                case EyeEditMode.Both:
                                    character.CorneaLeftWetness = value;
                                    character.CorneaRightWetness = value;
                                    break;
                                case EyeEditMode.Left:
                                    character.CorneaLeftWetness = value;
                                    break;
                                case EyeEditMode.Right:
                                    character.CorneaRightWetness = value;
                                    break;
                            }
                        });
                    }

                    var sliderSclera = wetnessRoot.FindDeepChildLiberal("Sclera")?.GetComponent<Slider>();
                    if (sliderSclera != null)
                    {
                        if (sliderSclera.onValueChanged == null) sliderSclera.onValueChanged = new Slider.SliderEvent();
                        sliderSclera.onValueChanged.RemoveAllListeners();
                        sliderSclera.SetValueWithoutNotify(eyeEditMode != EyeEditMode.Right ? character.ScleraLeftWetness : character.ScleraRightWetness);
                        sliderSclera.onValueChanged.AddListener((value) => 
                        {
                            switch (eyeEditMode)
                            {
                                case EyeEditMode.Both:
                                    character.ScleraLeftWetness = value;
                                    character.ScleraRightWetness = value;
                                    break;
                                case EyeEditMode.Left:
                                    character.ScleraLeftWetness = value;
                                    break;
                                case EyeEditMode.Right:
                                    character.ScleraRightWetness = value;
                                    break;
                            }
                        });
                    }

                    CustomEditorUtils.SetButtonOnClickActionByName(wetnessRoot, "reset", () =>
                    {
                        switch (eyeEditMode)
                        {
                            case EyeEditMode.Both:
                                character.CorneaLeftWetness = SandboxCharacter.eyeCorneaWetness_default;
                                character.CorneaRightWetness = SandboxCharacter.eyeCorneaWetness_default;
                                character.ScleraLeftWetness = SandboxCharacter.eyeScleraWetness_default;
                                character.ScleraRightWetness = SandboxCharacter.eyeScleraWetness_default;
                                break;
                            case EyeEditMode.Left:
                                character.CorneaLeftWetness = SandboxCharacter.eyeCorneaWetness_default;
                                character.ScleraLeftWetness = SandboxCharacter.eyeScleraWetness_default;
                                break;
                            case EyeEditMode.Right:
                                character.CorneaRightWetness = SandboxCharacter.eyeCorneaWetness_default;
                                character.ScleraRightWetness = SandboxCharacter.eyeScleraWetness_default;  
                                break;
                        }

                        if (sliderSclera != null) sliderSclera.SetValueWithoutNotify(SandboxCharacter.eyeScleraWetness_default);
                        if (sliderCornea != null) sliderCornea.SetValueWithoutNotify(SandboxCharacter.eyeCorneaWetness_default);
                    }, true, true, true);
                }

                RefreshEyeStyleMaterials();
                RefreshEyeColorPresets();
            }
        }
        public void OpenEyeCustomization()
        {
            CloseRightPanel();
            if (rightPanel != null)
            {
                rightPanel.SetActive(true);
            }

            if (eyeCustomizationWindow != null)
            {
                eyeCustomizationWindow.gameObject.SetActive(true);
                SetEyeEditMode(character == null ? eyeEditMode : (character.IsHeterochromic ? (eyeEditMode != EyeEditMode.Both ? eyeEditMode : EyeEditMode.Left) : EyeEditMode.Both), true);   
            }
        }
        #endregion

        #region Skin

        public const string _skinCustomizationCategoryName = "Skin";
        public const string id_skinTint = "skinTint";
        public const string id_sssTint = "sssTint";
        public const string id_veinTint = "veinTint";
        public const string id_markingsColor = "markingsColor";

        [Header("Skin")]
        public RectTransform skinCustomizationWindow;

        public RectTransform skinPresetLayoutTransform;
        private PrefabPool skinPresetPool;
        public GameObject skinPresetPrototype;
        public Material skinPresetMaterial;
        private ObjectPool<Material> skinPresetMaterials;
        private void RefreshSkinPresets()
        {
            ClearSkinPresets();
            if (character.skinColorPresets != null)
            {
                for (int a = 0; a < character.skinColorPresets.Count; a++)
                {
                    var preset = character.skinColorPresets.GetPalette(a);
                    if (preset == null) continue;

                    var inst = ClaimNewSkinPresetInstance(out var lightSkinImage, out var darkSkinImage);
                    if (inst != null)
                    {
                        float melanin = float.Parse(preset.GetAttributeValue("melanin:"));
                        Color skinTint = preset.GetColor("skinTint");
                        Color skinScatterTint = preset.GetColor("skinScatterTint");
                        Color veinTint = preset.GetColor("veinTint");

                        //lightSkinImage.color = new Color(skinTint.r, skinTint.g, skinTint.b, 1f);
                        //darkSkinImage.color = new Color(skinTint.r, skinTint.g, skinTint.b, melanin);  

                        //lightSkinImage.color = new Color(1f, 1f, 1f, 1f);
                        var tempCol = darkSkinImage.color;
                        darkSkinImage.color = new Color(tempCol.r, tempCol.g, tempCol.b, melanin);

                        var imgMat = lightSkinImage.material;
                        imgMat.SetColor("_ColorHDR", skinTint);
                        imgMat = darkSkinImage.material;
                        imgMat.SetColor("_ColorHDR", skinTint);

                        lightSkinImage.enabled = false;
                        lightSkinImage.enabled = true;

                        darkSkinImage.enabled = false;
                        darkSkinImage.enabled = true;

                        var button = inst.GetComponentInChildren<Button>(true);
                        if (button != null)
                        {
                            if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                            button.onClick.RemoveAllListeners();
                            button.onClick.AddListener(() =>
                            {
                                if (melaninSlider != null) melaninSlider.SetValueWithoutNotify(melanin);  

                                ColorPickableGraphic colorPickable;
                                colorPickable = GetColorPickableGraphic(id_skinTint);
                                if (colorPickable != null)
                                {
                                    colorPickable.SetColor(skinTint);
                                }
                                colorPickable = GetColorPickableGraphic(id_sssTint);
                                if (colorPickable != null)
                                {
                                    colorPickable.SetColor(skinScatterTint);
                                }
                                colorPickable = GetColorPickableGraphic(id_veinTint);
                                if (colorPickable != null)
                                {
                                    colorPickable.SetColor(veinTint); 
                                }

                                character.SkinMelanin = melanin;
                                character.SkinTint = skinTint;
                                character.SkinScatterTint = skinScatterTint;
                                character.VeinTint = veinTint;
                            });


                        }
                    }
                }
            }
        }
        private void ClearSkinPresets()
        {
            if (skinPresetLayoutTransform != null && skinPresetPool != null)
            {
                skinPresetPool.ReleaseAllClaims();
                for (int a = 0; a < skinPresetLayoutTransform.childCount; a++)
                {
                    var child = skinPresetLayoutTransform.GetChild(a);
                    if (child == null) continue;
                    child.gameObject.SetActive(false);
                }
            }
        }
        private GameObject ClaimNewSkinPresetInstance(out RawImage lightSkinImage, out RawImage darkSkinImage)
        {
            lightSkinImage = null;
            darkSkinImage = null;
            if (skinPresetPool == null || !skinPresetPool.TryGetNewInstance(out GameObject instance)) return null;
            if (instance == null) return null;

            instance.SetActive(true);

            var lightSkinObj = instance.transform.FindDeepChildLiberal("SkinLight");
            lightSkinImage = lightSkinObj == null ? null : lightSkinObj.GetComponentInChildren<RawImage>(true);
            if (lightSkinImage != null)
            {
                var mat = lightSkinImage.material;
                if (mat == null || !skinPresetMaterials.IsInPool(mat)) 
                {
                    skinPresetMaterials.TryGetNewInstance(out mat);
                    lightSkinImage.material = mat;
                }
            }

            var darkSkinObj = instance.transform.FindDeepChildLiberal("SkinDark");
            darkSkinImage = darkSkinObj == null ? null : darkSkinObj.GetComponentInChildren<RawImage>(true);
            if (darkSkinImage != null)
            {
                var mat = darkSkinImage.material;
                if (mat == null || !skinPresetMaterials.IsInPool(mat))
                {
                    skinPresetMaterials.TryGetNewInstance(out mat);
                    darkSkinImage.material = mat;
                }
            }

            return instance;
        }

        //private RectTransform skinColorRoot;
        //private RectTransform veinColorRoot;
        private Slider melaninSlider;

        public TabGroup skinDetailTabGroup;
        [Serializable]
        public enum SkinDetailEditMode
        {
            Freckles, Moles, Pimples, Scarring
        }
        protected SkinDetailEditMode skinDetailEditMode = SkinDetailEditMode.Freckles;
        public void SetSkinDetailEditMode(SkinDetailEditMode mode) => SetSkinDetailEditMode(mode, false);
        public void SetSkinDetailEditMode(SkinDetailEditMode mode, bool forceUpdate)
        {
            if (mode == skinDetailEditMode && !forceUpdate) return;

            skinDetailEditMode = mode;

            if (skinDetailTabGroup != null)
            {
                skinDetailTabGroup.SetSelectedTab(skinDetailTabGroup.IndexOf(mode.ToString()));
            }

            if (character != null)
            {
                ColorPickableGraphic colorPickable = GetColorPickableGraphic(id_markingsColor);
                if (colorPickable != null)
                {
                    switch (skinDetailEditMode)
                    {
                        case SkinDetailEditMode.Freckles:
                            colorPickable.SetColor(character.FreckleColor);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) =>
                            {
                                character.FreckleColor = color;
                            });
                            break;
                        case SkinDetailEditMode.Moles:
                            colorPickable.SetColor(character.MoleColor);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) =>
                            {
                                character.MoleColor = color;
                            });
                            break;
                        case SkinDetailEditMode.Pimples:
                            colorPickable.SetColor(character.PimpleColor);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) =>
                            {
                                character.PimpleColor = color;
                            });
                            break;
                        case SkinDetailEditMode.Scarring:
                            colorPickable.SetColor(character.ScarringColor);
                            colorPickable.ClearListeners();
                            colorPickable.AddListener((color) =>
                            {
                                character.ScarringColor = color;
                            });
                            break;
                    }
                }

                var strengthRoot = skinCustomizationWindow.FindDeepChildLiberal("markingsStrength");
                if (strengthRoot != null)
                {
                    var slider = strengthRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners(); 

                        switch (skinDetailEditMode)
                        {
                            case SkinDetailEditMode.Freckles:
                                slider.SetValueWithoutNotify(character.SkinDetailStrengths.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailStrengths;
                                    val.x = value;
                                    character.SkinDetailStrengths = val;
                                });
                                break;

                            case SkinDetailEditMode.Moles:
                                slider.SetValueWithoutNotify(character.SkinDetailStrengths.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailStrengths;
                                    val.y = value;
                                    character.SkinDetailStrengths = val;
                                });
                                break;

                            case SkinDetailEditMode.Pimples:
                                slider.SetValueWithoutNotify(character.SkinDetailStrengths.z);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailStrengths;
                                    val.z = value;
                                    character.SkinDetailStrengths = val;
                                });
                                break;

                            case SkinDetailEditMode.Scarring:
                                slider.SetValueWithoutNotify(character.SkinDetailStrengths.w);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailStrengths;
                                    val.w = value;
                                    character.SkinDetailStrengths = val;
                                });
                                break;
                        }
                    }
                }

                var varianceRoot = skinCustomizationWindow.FindDeepChildLiberal("variance");
                if (varianceRoot != null)
                {
                    var sliderSize = varianceRoot.FindDeepChildLiberal("size")?.GetComponent<Slider>();
                    if (sliderSize != null)
                    {
                        if (sliderSize.onValueChanged == null) sliderSize.onValueChanged = new Slider.SliderEvent();
                        sliderSize.onValueChanged.RemoveAllListeners();

                        switch (skinDetailEditMode)
                        {
                            case SkinDetailEditMode.Freckles:
                                sliderSize.SetValueWithoutNotify(character.SkinDetailMaskScales.x);
                                sliderSize.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskScales;
                                    val.x = value;
                                    character.SkinDetailMaskScales = val;
                                });
                                break;

                            case SkinDetailEditMode.Moles:
                                sliderSize.SetValueWithoutNotify(character.SkinDetailMaskScales.y);
                                sliderSize.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskScales;
                                    val.y = value;
                                    character.SkinDetailMaskScales = val;
                                });
                                break;

                            case SkinDetailEditMode.Pimples:
                                sliderSize.SetValueWithoutNotify(character.SkinDetailMaskScales.z);
                                sliderSize.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskScales;
                                    val.z = value;
                                    character.SkinDetailMaskScales = val;
                                });
                                break;

                            case SkinDetailEditMode.Scarring:
                                sliderSize.SetValueWithoutNotify(character.SkinDetailMaskScales.w);
                                sliderSize.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskScales;
                                    val.w = value;
                                    character.SkinDetailMaskScales = val;
                                });
                                break;
                        }
                    }

                    var sliderPower = varianceRoot.FindDeepChildLiberal("power")?.GetComponent<Slider>();
                    if (sliderPower != null)
                    {
                        if (sliderPower.onValueChanged == null) sliderPower.onValueChanged = new Slider.SliderEvent();
                        sliderPower.onValueChanged.RemoveAllListeners();

                        switch (skinDetailEditMode)
                        {
                            case SkinDetailEditMode.Freckles:
                                sliderPower.SetValueWithoutNotify(character.SkinDetailMaskStrengths.x);
                                sliderPower.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskStrengths;
                                    val.x = value;
                                    character.SkinDetailMaskStrengths = val;
                                });
                                break;

                            case SkinDetailEditMode.Moles:
                                sliderPower.SetValueWithoutNotify(character.SkinDetailMaskStrengths.y);
                                sliderPower.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskStrengths;
                                    val.y = value;
                                    character.SkinDetailMaskStrengths = val;
                                });
                                break;

                            case SkinDetailEditMode.Pimples:
                                sliderPower.SetValueWithoutNotify(character.SkinDetailMaskStrengths.z);
                                sliderPower.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskStrengths;
                                    val.z = value;
                                    character.SkinDetailMaskStrengths = val;
                                });
                                break;

                            case SkinDetailEditMode.Scarring:
                                sliderPower.SetValueWithoutNotify(character.SkinDetailMaskStrengths.w);
                                sliderPower.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskStrengths;
                                    val.w = value;
                                    character.SkinDetailMaskStrengths = val;
                                });
                                break;
                        }
                    }

                    var sliderSeed = varianceRoot.FindDeepChildLiberal("seed")?.GetComponent<Slider>();
                    if (sliderSeed != null)
                    {
                        if (sliderSeed.onValueChanged == null) sliderSeed.onValueChanged = new Slider.SliderEvent();
                        sliderSeed.onValueChanged.RemoveAllListeners();

                        switch (skinDetailEditMode)
                        {
                            case SkinDetailEditMode.Freckles:
                                sliderSeed.SetValueWithoutNotify(character.SkinDetailMaskSeeds.x);
                                sliderSeed.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskSeeds;
                                    val.x = value;
                                    character.SkinDetailMaskSeeds = val;
                                });
                                break;

                            case SkinDetailEditMode.Moles:
                                sliderSeed.SetValueWithoutNotify(character.SkinDetailMaskSeeds.y);
                                sliderSeed.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskSeeds;
                                    val.y = value;
                                    character.SkinDetailMaskSeeds = val;
                                });
                                break;

                            case SkinDetailEditMode.Pimples:
                                sliderSeed.SetValueWithoutNotify(character.SkinDetailMaskSeeds.z);
                                sliderSeed.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskSeeds;
                                    val.z = value;
                                    character.SkinDetailMaskSeeds = val;
                                });
                                break;

                            case SkinDetailEditMode.Scarring:
                                sliderSeed.SetValueWithoutNotify(character.SkinDetailMaskSeeds.w);
                                sliderSeed.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.SkinDetailMaskSeeds;
                                    val.w = value;
                                    character.SkinDetailMaskSeeds = val;
                                });
                                break;
                        }
                    }
                }

                var markingsScaleRoot = skinCustomizationWindow.FindDeepChildLiberal("markingsScale");
                if (markingsScaleRoot != null)
                {
                    var slider = markingsScaleRoot.GetComponentInChildren<Slider>(true);
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        slider.SetValueWithoutNotify(character.SkinDetailScale);
                        slider.onValueChanged.AddListener((value) =>
                        {
                            character.SkinDetailScale = value;
                        });
                    }
                }

                var markingsSeedRoot = skinCustomizationWindow.FindDeepChildLiberal("markingsSeed");
                if (markingsSeedRoot != null)
                {
                    var slider = markingsSeedRoot.GetComponentInChildren<Slider>(true);
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        slider.SetValueWithoutNotify(character.SkinDetailSeed);
                        slider.onValueChanged.AddListener((value) =>
                        {
                            character.SkinDetailSeed = value;
                        });
                    }
                }

                var maskingRoot = skinCustomizationWindow.FindDeepChildLiberal("masking");
                if (maskingRoot != null)
                {
                    var pool = maskingRoot.gameObject.GetComponent<PrefabPool>();
                    if (pool == null)
                    {
                        var prototype = maskingRoot.FindDeepChildLiberal("prototype");

                        if (prototype != null)
                        {
                            var container = prototype.transform.parent;

                            pool = maskingRoot.gameObject.AddComponent<PrefabPool>();

                            pool.Reinitialize(prototype.gameObject, PoolGrowthMethod.Incremental, 1, 1, 100);
                            pool.SetContainerTransform(container, true, false, false);
                            pool.forceParentOnClaim = true;
                            pool.worldPositionStaysWhenParented = false;
                            prototype.gameObject.SetActive(false);
                        }
                    }

                    if (pool != null)
                    {
                        pool.ReleaseAllClaims();

                        void CreateSliderLR(string title, float startValueL, UnityAction<float> listenerL, float startValueR, UnityAction<float> listenerR)
                        {
                            if (pool.TryGetNewInstance(out GameObject inst))
                            {
                                var titleObj = inst.transform.FindDeepChildLiberal("title");
                                if (titleObj != null) CustomEditorUtils.SetComponentText(titleObj, title);

                                var leftObj = inst.transform.FindDeepChildLiberal("left");
                                if (leftObj != null)
                                {
                                    var slider = leftObj.GetComponentInChildren<Slider>(true);
                                    slider.minValue = 0f;
                                    slider.maxValue = 1f;

                                    slider.SetValueWithoutNotify(startValueL);
                                    if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                                    slider.onValueChanged.RemoveAllListeners();
                                    slider.onValueChanged.AddListener(listenerL);
                                }

                                var rightObj = inst.transform.FindDeepChildLiberal("right");
                                if (rightObj != null)
                                {
                                    var slider = rightObj.GetComponentInChildren<Slider>(true);
                                    slider.minValue = 0f; 
                                    slider.maxValue = 1f;

                                    slider.SetValueWithoutNotify(startValueR);
                                    if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                                    slider.onValueChanged.RemoveAllListeners();
                                    slider.onValueChanged.AddListener(listenerR);
                                }

                                inst.SetActive(true); 
                            }
                        }

                        switch (skinDetailEditMode)
                        {
                            case SkinDetailEditMode.Freckles:

                                CreateSliderLR("Face",
                                 character.SkinDetailMaskFaceL.x, (float value) =>
                                {
                                    var val = character.SkinDetailMaskFaceL;
                                    val.x = value;
                                    character.SkinDetailMaskFaceL = val;
                                },
                                 character.SkinDetailMaskFaceR.x, (float value) =>
                                {
                                    var val = character.SkinDetailMaskFaceR;
                                    val.x = value;
                                    character.SkinDetailMaskFaceR = val;
                                });
                                CreateSliderLR("Head",
                                 character.SkinDetailMaskHeadL.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHeadL;
                                     val.x = value;
                                     character.SkinDetailMaskHeadL = val;
                                 },
                                 character.SkinDetailMaskHeadR.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHeadR;
                                     val.x = value;
                                     character.SkinDetailMaskHeadR = val;
                                 });
                                CreateSliderLR("Neck",
                                 character.SkinDetailMaskNeckL.x, (float value) =>
                                {
                                    var val = character.SkinDetailMaskNeckL;
                                    val.x = value;
                                    character.SkinDetailMaskNeckL = val;
                                },
                                 character.SkinDetailMaskNeckR.x, (float value) =>
                                {
                                    var val = character.SkinDetailMaskNeckR;
                                    val.x = value;
                                    character.SkinDetailMaskNeckR = val;
                                });
                                CreateSliderLR("Chest",
                                 character.SkinDetailMaskChestL.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskChestL;
                                     val.x = value;
                                     character.SkinDetailMaskChestL = val;
                                 },
                                 character.SkinDetailMaskChestR.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskChestR;
                                     val.x = value;
                                     character.SkinDetailMaskChestR = val;
                                 });
                                CreateSliderLR("Back",
                                 character.SkinDetailMaskBackL.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskBackL;
                                     val.x = value;
                                     character.SkinDetailMaskBackL = val;
                                 },
                                 character.SkinDetailMaskBackR.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskBackR;
                                     val.x = value;
                                     character.SkinDetailMaskBackR = val;
                                 });
                                CreateSliderLR("Torso",
                                 character.SkinDetailMaskTorsoL.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskTorsoL;
                                     val.x = value;
                                     character.SkinDetailMaskTorsoL = val;
                                 },
                                 character.SkinDetailMaskTorsoR.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskTorsoR;
                                     val.x = value;
                                     character.SkinDetailMaskTorsoR = val;
                                 });
                                CreateSliderLR("Arms",
                                 character.SkinDetailMaskArmsL.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskArmsL;
                                     val.x = value;
                                     character.SkinDetailMaskArmsL = val;
                                 },
                                 character.SkinDetailMaskArmsR.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskArmsR;
                                     val.x = value;
                                     character.SkinDetailMaskArmsR = val;
                                 });
                                CreateSliderLR("Hands",
                                 character.SkinDetailMaskHandsL.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHandsL;
                                     val.x = value;
                                     character.SkinDetailMaskHandsL = val;
                                 },
                                 character.SkinDetailMaskHandsR.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHandsR;
                                     val.x = value;
                                     character.SkinDetailMaskHandsR = val;
                                 });
                                CreateSliderLR("Legs",
                                 character.SkinDetailMaskLegsL.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskLegsL;
                                     val.x = value;
                                     character.SkinDetailMaskLegsL = val;
                                 },
                                 character.SkinDetailMaskLegsR.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskLegsR;
                                     val.x = value;
                                     character.SkinDetailMaskLegsR = val;
                                 });
                                CreateSliderLR("Feet",
                                 character.SkinDetailMaskFeetL.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFeetL;
                                     val.x = value;
                                     character.SkinDetailMaskFeetL = val;
                                 },
                                 character.SkinDetailMaskFeetR.x, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFeetR;
                                     val.x = value;
                                     character.SkinDetailMaskFeetR = val;
                                 });

                                break;

                            case SkinDetailEditMode.Moles:


                                CreateSliderLR("Face",
                                 character.SkinDetailMaskFaceL.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFaceL;
                                     val.y = value;
                                     character.SkinDetailMaskFaceL = val;
                                 },
                                 character.SkinDetailMaskFaceR.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFaceR;
                                     val.y = value;
                                     character.SkinDetailMaskFaceR = val;
                                 });
                                CreateSliderLR("Head",
                                 character.SkinDetailMaskHeadL.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHeadL;
                                     val.y = value;
                                     character.SkinDetailMaskHeadL = val;
                                 },
                                 character.SkinDetailMaskHeadR.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHeadR;
                                     val.y = value;
                                     character.SkinDetailMaskHeadR = val;
                                 });
                                CreateSliderLR("Neck",
                                 character.SkinDetailMaskNeckL.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskNeckL;
                                     val.y = value;
                                     character.SkinDetailMaskNeckL = val;
                                 },
                                 character.SkinDetailMaskNeckR.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskNeckR;
                                     val.y = value;
                                     character.SkinDetailMaskNeckR = val;
                                 });
                                CreateSliderLR("Chest",
                                 character.SkinDetailMaskChestL.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskChestL;
                                     val.y = value;
                                     character.SkinDetailMaskChestL = val;
                                 },
                                 character.SkinDetailMaskChestR.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskChestR;
                                     val.y = value;
                                     character.SkinDetailMaskChestR = val;
                                 });
                                CreateSliderLR("Back",
                                 character.SkinDetailMaskBackL.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskBackL;
                                     val.y = value;
                                     character.SkinDetailMaskBackL = val;
                                 },
                                 character.SkinDetailMaskBackR.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskBackR;
                                     val.y = value;
                                     character.SkinDetailMaskBackR = val;
                                 });
                                CreateSliderLR("Torso",
                                 character.SkinDetailMaskTorsoL.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskTorsoL;
                                     val.y = value;
                                     character.SkinDetailMaskTorsoL = val;
                                 },
                                 character.SkinDetailMaskTorsoR.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskTorsoR;
                                     val.y = value;
                                     character.SkinDetailMaskTorsoR = val;
                                 });
                                CreateSliderLR("Arms",
                                 character.SkinDetailMaskArmsL.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskArmsL;
                                     val.y = value;
                                     character.SkinDetailMaskArmsL = val;
                                 },
                                 character.SkinDetailMaskArmsR.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskArmsR;
                                     val.y = value;
                                     character.SkinDetailMaskArmsR = val;
                                 });
                                CreateSliderLR("Hands",
                                 character.SkinDetailMaskHandsL.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHandsL;
                                     val.y = value;
                                     character.SkinDetailMaskHandsL = val;
                                 },
                                 character.SkinDetailMaskHandsR.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHandsR;
                                     val.y = value;
                                     character.SkinDetailMaskHandsR = val;
                                 });
                                CreateSliderLR("Legs",
                                 character.SkinDetailMaskLegsL.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskLegsL;
                                     val.y = value;
                                     character.SkinDetailMaskLegsL = val;
                                 },
                                 character.SkinDetailMaskLegsR.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskLegsR;
                                     val.y = value;
                                     character.SkinDetailMaskLegsR = val;
                                 });
                                CreateSliderLR("Feet",
                                 character.SkinDetailMaskFeetL.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFeetL;
                                     val.y = value;
                                     character.SkinDetailMaskFeetL = val;
                                 },
                                 character.SkinDetailMaskFeetR.y, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFeetR;
                                     val.y = value;
                                     character.SkinDetailMaskFeetR = val;
                                 });

                                break;

                            case SkinDetailEditMode.Pimples:

                                CreateSliderLR("Face",
                                 character.SkinDetailMaskFaceL.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFaceL;
                                     val.z = value;
                                     character.SkinDetailMaskFaceL = val;
                                 },
                                 character.SkinDetailMaskFaceR.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFaceR;
                                     val.z = value;
                                     character.SkinDetailMaskFaceR = val;
                                 });
                                CreateSliderLR("Head",
                                 character.SkinDetailMaskHeadL.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHeadL;
                                     val.z = value;
                                     character.SkinDetailMaskHeadL = val;
                                 },
                                 character.SkinDetailMaskHeadR.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHeadR;
                                     val.z = value;
                                     character.SkinDetailMaskHeadR = val;
                                 });
                                CreateSliderLR("Neck",
                                 character.SkinDetailMaskNeckL.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskNeckL;
                                     val.z = value;
                                     character.SkinDetailMaskNeckL = val;
                                 },
                                 character.SkinDetailMaskNeckR.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskNeckR;
                                     val.z = value;
                                     character.SkinDetailMaskNeckR = val;
                                 });
                                CreateSliderLR("Chest",
                                 character.SkinDetailMaskChestL.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskChestL;
                                     val.z = value;
                                     character.SkinDetailMaskChestL = val;
                                 },
                                 character.SkinDetailMaskChestR.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskChestR;
                                     val.z = value;
                                     character.SkinDetailMaskChestR = val;
                                 });
                                CreateSliderLR("Back",
                                 character.SkinDetailMaskBackL.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskBackL;
                                     val.z = value;
                                     character.SkinDetailMaskBackL = val;
                                 },
                                 character.SkinDetailMaskBackR.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskBackR;
                                     val.z = value;
                                     character.SkinDetailMaskBackR = val;
                                 });
                                CreateSliderLR("Torso",
                                 character.SkinDetailMaskTorsoL.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskTorsoL;
                                     val.z = value;
                                     character.SkinDetailMaskTorsoL = val;
                                 },
                                 character.SkinDetailMaskTorsoR.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskTorsoR;
                                     val.z = value;
                                     character.SkinDetailMaskTorsoR = val;
                                 });
                                CreateSliderLR("Arms",
                                 character.SkinDetailMaskArmsL.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskArmsL;
                                     val.z = value;
                                     character.SkinDetailMaskArmsL = val;
                                 },
                                 character.SkinDetailMaskArmsR.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskArmsR;
                                     val.z = value;
                                     character.SkinDetailMaskArmsR = val;
                                 });
                                CreateSliderLR("Hands",
                                 character.SkinDetailMaskHandsL.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHandsL;
                                     val.z = value;
                                     character.SkinDetailMaskHandsL = val;
                                 },
                                 character.SkinDetailMaskHandsR.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHandsR;
                                     val.z = value;
                                     character.SkinDetailMaskHandsR = val;
                                 });
                                CreateSliderLR("Legs",
                                 character.SkinDetailMaskLegsL.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskLegsL;
                                     val.z = value;
                                     character.SkinDetailMaskLegsL = val;
                                 },
                                 character.SkinDetailMaskLegsR.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskLegsR;
                                     val.z = value;
                                     character.SkinDetailMaskLegsR = val;
                                 });
                                CreateSliderLR("Feet",
                                 character.SkinDetailMaskFeetL.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFeetL;
                                     val.z = value;
                                     character.SkinDetailMaskFeetL = val;
                                 },
                                 character.SkinDetailMaskFeetR.z, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFeetR;
                                     val.z = value;
                                     character.SkinDetailMaskFeetR = val;
                                 });

                                break;

                            case SkinDetailEditMode.Scarring:


                                CreateSliderLR("Face",
                                 character.SkinDetailMaskFaceL.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFaceL;
                                     val.w = value;
                                     character.SkinDetailMaskFaceL = val;
                                 },
                                 character.SkinDetailMaskFaceR.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFaceR;
                                     val.w = value;
                                     character.SkinDetailMaskFaceR = val;
                                 });
                                CreateSliderLR("Head",
                                 character.SkinDetailMaskHeadL.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHeadL;
                                     val.w = value;
                                     character.SkinDetailMaskHeadL = val;
                                 },
                                 character.SkinDetailMaskHeadR.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHeadR;
                                     val.w = value;
                                     character.SkinDetailMaskHeadR = val;
                                 });
                                CreateSliderLR("Neck",
                                 character.SkinDetailMaskNeckL.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskNeckL;
                                     val.w = value;
                                     character.SkinDetailMaskNeckL = val;
                                 },
                                 character.SkinDetailMaskNeckR.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskNeckR;
                                     val.w = value;
                                     character.SkinDetailMaskNeckR = val;
                                 });
                                CreateSliderLR("Chest",
                                 character.SkinDetailMaskChestL.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskChestL;
                                     val.w = value;
                                     character.SkinDetailMaskChestL = val;
                                 },
                                 character.SkinDetailMaskChestR.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskChestR;
                                     val.w = value;
                                     character.SkinDetailMaskChestR = val;
                                 });
                                CreateSliderLR("Back",
                                 character.SkinDetailMaskBackL.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskBackL;
                                     val.w = value;
                                     character.SkinDetailMaskBackL = val;
                                 },
                                 character.SkinDetailMaskBackR.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskBackR;
                                     val.w = value;
                                     character.SkinDetailMaskBackR = val;
                                 });
                                CreateSliderLR("Torso",
                                 character.SkinDetailMaskTorsoL.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskTorsoL;
                                     val.w = value;
                                     character.SkinDetailMaskTorsoL = val;
                                 },
                                 character.SkinDetailMaskTorsoR.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskTorsoR;
                                     val.w = value;
                                     character.SkinDetailMaskTorsoR = val;
                                 });
                                CreateSliderLR("Arms",
                                 character.SkinDetailMaskArmsL.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskArmsL;
                                     val.w = value;
                                     character.SkinDetailMaskArmsL = val;
                                 },
                                 character.SkinDetailMaskArmsR.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskArmsR;
                                     val.w = value;
                                     character.SkinDetailMaskArmsR = val;
                                 });
                                CreateSliderLR("Hands",
                                 character.SkinDetailMaskHandsL.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHandsL;
                                     val.w = value;
                                     character.SkinDetailMaskHandsL = val;
                                 },
                                 character.SkinDetailMaskHandsR.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskHandsR;
                                     val.w = value;
                                     character.SkinDetailMaskHandsR = val;
                                 });
                                CreateSliderLR("Legs",
                                 character.SkinDetailMaskLegsL.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskLegsL;
                                     val.w = value;
                                     character.SkinDetailMaskLegsL = val;
                                 },
                                 character.SkinDetailMaskLegsR.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskLegsR;
                                     val.w = value;
                                     character.SkinDetailMaskLegsR = val;
                                 });
                                CreateSliderLR("Feet",
                                 character.SkinDetailMaskFeetL.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFeetL;
                                     val.w = value;
                                     character.SkinDetailMaskFeetL = val;
                                 },
                                 character.SkinDetailMaskFeetR.w, (float value) =>
                                 {
                                     var val = character.SkinDetailMaskFeetR;
                                     val.w = value;
                                     character.SkinDetailMaskFeetR = val; 
                                 });

                                break;
                        }
                    }
                }
            }
        }

        public void OpenSkinCustomization()
        {
            CloseRightPanel();
            if (rightPanel != null)
            {
                rightPanel.SetActive(true);
            }
            
            if (skinCustomizationWindow != null)
            {
                skinCustomizationWindow.gameObject.SetActive(true);

                if (character != null)
                {
                    ColorPickableGraphic colorPickable;

                    float melanin = character.SkinMelanin;
                    Color skinTint = character.SkinTint;
                    Color sssTint = character.SkinScatterTint;
                    Color veinTint = character.VeinTint;

                    colorPickable = GetColorPickableGraphic(id_skinTint);
                    if (colorPickable != null)
                    {
                        colorPickable.SetColor(skinTint);
                        colorPickable.ClearListeners();
                        colorPickable.AddListener((color) =>
                        {
                            character.SkinTint = color;
                        });
                    }
                    colorPickable = GetColorPickableGraphic(id_sssTint);
                    if (colorPickable != null)
                    {
                        colorPickable.SetColor(sssTint);
                        colorPickable.ClearListeners();
                        colorPickable.AddListener((color) =>
                        {
                            character.SkinScatterTint = color;
                        });
                    }
                    colorPickable = GetColorPickableGraphic(id_veinTint);
                    if (colorPickable != null)
                    {
                        colorPickable.SetColor(veinTint);
                        colorPickable.ClearListeners();
                        colorPickable.AddListener((color) => 
                        {
                            character.VeinTint = color; 
                        });
                    }

                    var melaninRoot = skinCustomizationWindow.FindDeepChildLiberal("melanin");
                    if (melaninRoot != null)
                    {
                        melaninSlider = melaninRoot.GetComponentInChildren<Slider>(true);
                        if (melaninSlider != null)
                        {
                            if (melaninSlider.onValueChanged == null) melaninSlider.onValueChanged = new Slider.SliderEvent();
                            melaninSlider.onValueChanged.RemoveAllListeners();
                            melaninSlider.SetValueWithoutNotify(melanin);
                            melaninSlider.onValueChanged.AddListener((value) =>
                            {
                                character.SkinMelanin = value;
                            });
                        }
                    }

                    RefreshSkinPresets();

                    SetSkinDetailEditMode(skinDetailEditMode, true); 

                }
            }
        }

        #endregion

        #region Makeup

        public const string _makeupCustomizationCategoryName = "Makeup";

        public const string id_bodyPaintColor = "bodyPaintColor";
        public const string id_bodyPaintSpecColor = "bodyPaintSpecColor";

        public const string id_lipstickColor = "lipstickColor";
        public const string id_lipstickSpecColor = "lipstickSpecColor";

        public const string id_eyelinerColor = "eyelinerColor";
        public const string id_eyelinerSpecColor = "eyelinerSpecColor";

        public const string id_eyeshadowColor = "eyeshadowColor";
        public const string id_eyeshadowSpecColor = "eyeshadowSpecColor";

        public const string id_blushColor = "blushColor";
        public const string id_blushSpecColor = "blushSpecColor";

        [Header("Makeup")]
        public RectTransform makeupCustomizationWindow;

        public RectTransform makeupStyleLayoutTransform;
        private PrefabPool makeupStylePool;
        public GameObject makeupStylePrototype;
        public Material makeupStyleMaterial;
        private ObjectPool<Material> makeupStyleMaterials;
        private void RefreshMakeupStyles()
        {
            ClearMakeupStyles();
            if (character.makeupStyles != null)
            {
                for (int a = 0; a < character.makeupStyles.Count; a++)
                {
                    var preset = character.makeupStyles.GetTexture(a);
                    if (preset.texture == null) continue;

                    var texIndexStr = preset.GetAttributeValue("indexInAtlas:");
                    if (string.IsNullOrWhiteSpace(texIndexStr) || !float.TryParse(texIndexStr, out float texIndex)) continue;

                    var inst = ClaimNewMakeupStyleInstance(out var lightSkinImage, out var darkSkinImage, out var makeupImage);
                    if (inst != null)
                    {
                        float melanin = character.SkinMelanin;
                        Color skinTint = character.SkinTint;

                        //lightSkinImage.color = new Color(skinTint.r, skinTint.g, skinTint.b, 1f);
                        //darkSkinImage.color = new Color(skinTint.r, skinTint.g, skinTint.b, melanin);  

                        //lightSkinImage.color = new Color(1f, 1f, 1f, 1f);
                        var tempCol = darkSkinImage.color;
                        darkSkinImage.color = new Color(tempCol.r, tempCol.g, tempCol.b, melanin);

                        var imgMat = lightSkinImage.material;
                        imgMat.SetColor("_ColorHDR", skinTint);
                        imgMat = darkSkinImage.material;
                        imgMat.SetColor("_ColorHDR", skinTint);
                        
                        lightSkinImage.enabled = false;
                        lightSkinImage.enabled = true;

                        darkSkinImage.enabled = false;
                        darkSkinImage.enabled = true;

                        if (makeupImage != null)
                        {
                            makeupImage.texture = preset.texture;
                            imgMat = makeupImage.material;

                            switch (makeupEditLayer)
                            {
                                case MakeupEditLayer.Layer1:
                                    imgMat.SetColor("_ColorA", character.MakeupColorR_1.Opaque());
                                    imgMat.SetColor("_ColorB", character.MakeupColorG_1.Opaque());
                                    imgMat.SetColor("_ColorC", character.MakeupColorB_1.Opaque());
                                    imgMat.SetColor("_ColorD", character.MakeupColorA_1.Opaque());
                                    break;

                                case MakeupEditLayer.Layer2:
                                    imgMat.SetColor("_ColorA", character.MakeupColorR_2.Opaque());
                                    imgMat.SetColor("_ColorB", character.MakeupColorG_2.Opaque());
                                    imgMat.SetColor("_ColorC", character.MakeupColorB_2.Opaque());
                                    imgMat.SetColor("_ColorD", character.MakeupColorA_2.Opaque());
                                    break;

                                case MakeupEditLayer.Layer3:
                                    imgMat.SetColor("_ColorA", character.MakeupColorR_3.Opaque());
                                    imgMat.SetColor("_ColorB", character.MakeupColorG_3.Opaque());
                                    imgMat.SetColor("_ColorC", character.MakeupColorB_3.Opaque());
                                    imgMat.SetColor("_ColorD", character.MakeupColorA_3.Opaque());
                                    break;
                            }                          
                        }

                        var selectedObj = inst.transform.FindDeepChildLiberal("selected");
                        Image selectedImg = null;
                        if (selectedObj != null) selectedImg = selectedObj.GetComponentInChildren<Image>(true);
                        if (selectedImg != null)
                        {
                            switch(makeupEditLayer)
                            {
                                case MakeupEditLayer.Layer1:
                                    selectedImg.enabled = a == (int)character.MakeupStyle_1;
                                    break;

                                case MakeupEditLayer.Layer2:
                                    selectedImg.enabled = a == (int)character.MakeupStyle_2;
                                    break;

                                case MakeupEditLayer.Layer3:
                                    selectedImg.enabled = a == (int)character.MakeupStyle_3;
                                    break;
                            }
                        }

                        void SetAsSelection()
                        {
                            for (int b = 0; b < inst.transform.parent.childCount; b++)
                            {
                                var child = inst.transform.parent.GetChild(b);
                                if (child.gameObject == inst) continue;

                                var selectedObj_ = child.FindDeepChildLiberal("selected");
                                if (selectedObj_ != null)
                                {
                                    var selectedImg_ = selectedObj_.GetComponentInChildren<Image>(true);
                                    if (selectedImg_ != null) selectedImg_.enabled = false;
                                }
                            }

                            if (selectedImg != null) selectedImg.enabled = true;
                        }

                        var button = inst.GetComponentInChildren<Button>(true);
                        if (button != null)
                        {
                            if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                            button.onClick.RemoveAllListeners();
                            button.onClick.AddListener(() =>
                            {
                                switch (makeupEditLayer)
                                {
                                    case MakeupEditLayer.Layer1:
                                        character.MakeupStyle_1 = texIndex;
                                        SetAsSelection();
                                        break;

                                    case MakeupEditLayer.Layer2:
                                        character.MakeupStyle_2 = texIndex;
                                        SetAsSelection();
                                        break;

                                    case MakeupEditLayer.Layer3:
                                        character.MakeupStyle_3 = texIndex;
                                        SetAsSelection();
                                        break;
                                }
                            });

                        }
                    }
                }
            }
        }
        private void ClearMakeupStyles()
        {
            if (makeupStyleLayoutTransform != null && makeupStylePool != null)
            {
                makeupStylePool.ReleaseAllClaims();
                for (int a = 0; a < makeupStyleLayoutTransform.childCount; a++)
                {
                    var child = makeupStyleLayoutTransform.GetChild(a);
                    if (child == null) continue;
                    child.gameObject.SetActive(false); 
                }
            }
        }
        private GameObject ClaimNewMakeupStyleInstance(out RawImage lightSkinImage, out RawImage darkSkinImage, out RawImage makeupImage)
        {
            lightSkinImage = null;
            darkSkinImage = null;
            makeupImage = null;
            if (makeupStylePool == null || !makeupStylePool.TryGetNewInstance(out GameObject instance)) return null;
            if (instance == null) return null;

            instance.SetActive(true);

            var lightSkinObj = instance.transform.FindDeepChildLiberal("SkinLight");
            lightSkinImage = lightSkinObj == null ? null : lightSkinObj.GetComponentInChildren<RawImage>(true);
            if (lightSkinImage != null)
            {
                var mat = lightSkinImage.material;
                if (mat == null || !skinPresetMaterials.IsInPool(mat))
                {
                    skinPresetMaterials.TryGetNewInstance(out mat);
                    lightSkinImage.material = mat;
                }
            }

            var darkSkinObj = instance.transform.FindDeepChildLiberal("SkinDark");
            darkSkinImage = darkSkinObj == null ? null : darkSkinObj.GetComponentInChildren<RawImage>(true);
            if (darkSkinImage != null)
            {
                var mat = darkSkinImage.material;
                if (mat == null || !skinPresetMaterials.IsInPool(mat))
                {
                    skinPresetMaterials.TryGetNewInstance(out mat);
                    darkSkinImage.material = mat;
                }
            }

            var makeupObj = instance.transform.FindDeepChildLiberal("MakeupTex");
            makeupImage = makeupObj == null ? null : makeupObj.GetComponentInChildren<RawImage>(true);
            if (makeupImage != null)
            {
                var mat = makeupImage.material;
                if (mat == null || !makeupStyleMaterials.IsInPool(mat))
                {
                    makeupStyleMaterials.TryGetNewInstance(out mat);
                    makeupImage.material = mat; 
                }
            }

            return instance;
        }

        public TabGroup makeupLayerTabGroup;
        [Serializable]
        public enum MakeupEditLayer
        {
            Layer1, Layer2, Layer3
        }
        protected MakeupEditLayer makeupEditLayer = MakeupEditLayer.Layer1;
        public void SetMakeupEditLayer(MakeupEditLayer layer) => SetMakeupEditLayer(layer, false);
        public void SetMakeupEditLayer(MakeupEditLayer layer, bool forceUpdate)
        {
            if (layer == makeupEditLayer && !forceUpdate) return;

            makeupEditLayer = layer;

            if (makeupLayerTabGroup != null)
            {
                makeupLayerTabGroup.SetSelectedTab(makeupLayerTabGroup.IndexOf(layer.ToString()));
            }

            if (character != null)
            {
                ColorPickableGraphic colorPickableR = GetColorPickableGraphic(id_lipstickColor);
                ColorPickableGraphic colorPickableR_Spec = GetColorPickableGraphic(id_lipstickSpecColor);

                ColorPickableGraphic colorPickableG = GetColorPickableGraphic(id_eyelinerColor);
                ColorPickableGraphic colorPickableG_Spec = GetColorPickableGraphic(id_eyelinerSpecColor);

                ColorPickableGraphic colorPickableB = GetColorPickableGraphic(id_eyeshadowColor);
                ColorPickableGraphic colorPickableB_Spec = GetColorPickableGraphic(id_eyeshadowSpecColor);

                ColorPickableGraphic colorPickableA = GetColorPickableGraphic(id_blushColor);
                ColorPickableGraphic colorPickableA_Spec = GetColorPickableGraphic(id_blushSpecColor);

                switch (makeupEditLayer)
                {
                    case MakeupEditLayer.Layer1:

                        colorPickableR.SetColor(character.MakeupColorR_1);
                        colorPickableR.ClearListeners();
                        colorPickableR.AddListener((color) =>
                        {
                            character.MakeupColorR_1 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableR_Spec.SetColor(character.MakeupSpecColorR_1.Opaque());
                        colorPickableR_Spec.ClearListeners();
                        colorPickableR_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorR_1.a;
                            character.MakeupSpecColorR_1 = color;
                        });

                        colorPickableG.SetColor(character.MakeupColorG_1);
                        colorPickableG.ClearListeners();
                        colorPickableG.AddListener((color) =>
                        {
                            character.MakeupColorG_1 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableG_Spec.SetColor(character.MakeupSpecColorG_1.Opaque());
                        colorPickableG_Spec.ClearListeners();
                        colorPickableG_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorG_1.a;
                            character.MakeupSpecColorG_1 = color;
                        });

                        colorPickableB.SetColor(character.MakeupColorB_1);
                        colorPickableB.ClearListeners();
                        colorPickableB.AddListener((color) =>
                        {
                            character.MakeupColorB_1 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableB_Spec.SetColor(character.MakeupSpecColorB_1.Opaque());
                        colorPickableB_Spec.ClearListeners();
                        colorPickableB_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorB_1.a;
                            character.MakeupSpecColorB_1 = color;
                        });

                        colorPickableA.SetColor(character.MakeupColorA_1);
                        colorPickableA.ClearListeners();
                        colorPickableA.AddListener((color) =>
                        {
                            character.MakeupColorA_1 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableA_Spec.SetColor(character.MakeupSpecColorA_1.Opaque());
                        colorPickableA_Spec.ClearListeners();
                        colorPickableA_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorA_1.a;
                            character.MakeupSpecColorA_1 = color;
                        });

                        break;

                    case MakeupEditLayer.Layer2:

                        colorPickableR.SetColor(character.MakeupColorR_2);
                        colorPickableR.ClearListeners();
                        colorPickableR.AddListener((color) =>
                        {
                            character.MakeupColorR_2 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableR_Spec.SetColor(character.MakeupSpecColorR_2.Opaque());
                        colorPickableR_Spec.ClearListeners();
                        colorPickableR_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorR_2.a;
                            character.MakeupSpecColorR_2 = color;
                        });

                        colorPickableG.SetColor(character.MakeupColorG_2);
                        colorPickableG.ClearListeners();
                        colorPickableG.AddListener((color) =>
                        {
                            character.MakeupColorG_2 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableG_Spec.SetColor(character.MakeupSpecColorG_2.Opaque());
                        colorPickableG_Spec.ClearListeners();
                        colorPickableG_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorG_2.a;
                            character.MakeupSpecColorG_2 = color;
                        });

                        colorPickableB.SetColor(character.MakeupColorB_2);
                        colorPickableB.ClearListeners();
                        colorPickableB.AddListener((color) =>
                        {
                            character.MakeupColorB_2 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableB_Spec.SetColor(character.MakeupSpecColorB_2.Opaque());
                        colorPickableB_Spec.ClearListeners();
                        colorPickableB_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorB_2.a;
                            character.MakeupSpecColorB_2 = color;
                        });

                        colorPickableA.SetColor(character.MakeupColorA_2);
                        colorPickableA.ClearListeners();
                        colorPickableA.AddListener((color) =>
                        {
                            character.MakeupColorA_2 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableA_Spec.SetColor(character.MakeupSpecColorA_2.Opaque());
                        colorPickableA_Spec.ClearListeners();
                        colorPickableA_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorA_2.a;
                            character.MakeupSpecColorA_2 = color;
                        });

                        break;

                    case MakeupEditLayer.Layer3:

                        colorPickableR.SetColor(character.MakeupColorR_3);
                        colorPickableR.ClearListeners();
                        colorPickableR.AddListener((color) =>
                        {
                            character.MakeupColorR_3 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableR_Spec.SetColor(character.MakeupSpecColorR_3.Opaque());
                        colorPickableR_Spec.ClearListeners();
                        colorPickableR_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorR_3.a;
                            character.MakeupSpecColorR_3 = color;
                        });

                        colorPickableG.SetColor(character.MakeupColorG_3);
                        colorPickableG.ClearListeners();
                        colorPickableG.AddListener((color) =>
                        {
                            character.MakeupColorG_3 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableG_Spec.SetColor(character.MakeupSpecColorG_3.Opaque());
                        colorPickableG_Spec.ClearListeners();
                        colorPickableG_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorG_3.a;
                            character.MakeupSpecColorG_3 = color;
                        });

                        colorPickableB.SetColor(character.MakeupColorB_3);
                        colorPickableB.ClearListeners();
                        colorPickableB.AddListener((color) =>
                        {
                            character.MakeupColorB_3 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableB_Spec.SetColor(character.MakeupSpecColorB_3.Opaque());
                        colorPickableB_Spec.ClearListeners();
                        colorPickableB_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorB_3.a;
                            character.MakeupSpecColorB_3 = color;
                        });

                        colorPickableA.SetColor(character.MakeupColorA_3);
                        colorPickableA.ClearListeners();
                        colorPickableA.AddListener((color) =>
                        {
                            character.MakeupColorA_3 = color;
                            RefreshMakeupStyles();
                        });
                        colorPickableA_Spec.SetColor(character.MakeupSpecColorA_3.Opaque());
                        colorPickableA_Spec.ClearListeners();
                        colorPickableA_Spec.AddListener((color) =>
                        {
                            color.a = character.MakeupSpecColorA_3.a;
                            character.MakeupSpecColorA_3 = color;
                        });

                        break;
                }

                var blendModeRoot = makeupCustomizationWindow.FindDeepChildLiberal("lipBlendMode");
                if (blendModeRoot != null)
                {
                    var dropdown = blendModeRoot.FindDeepChildLiberal("value")?.GetComponent<TMP_Dropdown>();
                    if (dropdown != null)
                    {
                        if (dropdown.onValueChanged == null) dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
                        dropdown.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeR_1);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeR_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeR_2);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeR_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeR_3);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeR_3 = value;
                                });
                                break;
                        }
                    }
                }
                blendModeRoot = makeupCustomizationWindow.FindDeepChildLiberal("eyelinerBlendMode");
                if (blendModeRoot != null)
                {
                    var dropdown = blendModeRoot.FindDeepChildLiberal("value")?.GetComponent<TMP_Dropdown>();
                    if (dropdown != null)
                    {
                        if (dropdown.onValueChanged == null) dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
                        dropdown.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeG_1);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeG_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeG_2);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeG_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeG_3);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeG_3 = value;
                                });
                                break;
                        }
                    }
                }
                blendModeRoot = makeupCustomizationWindow.FindDeepChildLiberal("eyeshadowBlendMode");
                if (blendModeRoot != null)
                {
                    var dropdown = blendModeRoot.FindDeepChildLiberal("value")?.GetComponent<TMP_Dropdown>();
                    if (dropdown != null)
                    {
                        if (dropdown.onValueChanged == null) dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
                        dropdown.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeB_1);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeB_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeB_2);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeB_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeB_3);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeB_3 = value;
                                });
                                break;
                        }
                    }
                }
                blendModeRoot = makeupCustomizationWindow.FindDeepChildLiberal("blushBlendMode");
                if (blendModeRoot != null)
                {
                    var dropdown = blendModeRoot.FindDeepChildLiberal("value")?.GetComponent<TMP_Dropdown>();
                    if (dropdown != null)
                    {
                        if (dropdown.onValueChanged == null) dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
                        dropdown.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeA_1);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeA_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeA_2);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeA_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                dropdown.SetValueWithoutNotify((int)character.MakeupBlendTypeA_3);
                                dropdown.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupBlendTypeA_3 = value;
                                });
                                break;
                        }
                    }
                }

                var blendRoot = makeupCustomizationWindow.FindDeepChildLiberal("makeupBlend");
                if (blendRoot != null)
                {
                    var slider = blendRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupFinalBlend_1);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupFinalBlend_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupFinalBlend_2);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupFinalBlend_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupFinalBlend_3);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupFinalBlend_3 = value;
                                });
                                break;
                        }
                    }
                }

                blendRoot = makeupCustomizationWindow.FindDeepChildLiberal("lipBlend");
                if (blendRoot != null)
                {
                    var slider = blendRoot.FindDeepChildLiberal("left")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupBlendR_1.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendR_1;
                                    val.x = value;
                                    character.MakeupBlendR_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupBlendR_2.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendR_2;
                                    val.x = value;
                                    character.MakeupBlendR_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupBlendR_3.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendR_3;
                                    val.x = value;
                                    character.MakeupBlendR_3 = val;
                                });
                                break;
                        }
                    }
                    slider = blendRoot.FindDeepChildLiberal("right")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupBlendR_1.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendR_1;
                                    val.y = value;
                                    character.MakeupBlendR_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupBlendR_2.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendR_2;
                                    val.y = value;
                                    character.MakeupBlendR_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupBlendR_3.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendR_3;
                                    val.y = value;
                                    character.MakeupBlendR_3 = val;
                                });
                                break;
                        }
                    }
                }
                blendRoot = makeupCustomizationWindow.FindDeepChildLiberal("eyelinerBlend");
                if (blendRoot != null)
                {
                    var slider = blendRoot.FindDeepChildLiberal("left")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupBlendG_1.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendG_1;
                                    val.x = value;
                                    character.MakeupBlendG_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupBlendG_2.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendG_2;
                                    val.x = value;
                                    character.MakeupBlendG_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupBlendG_3.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendG_3;
                                    val.x = value;
                                    character.MakeupBlendG_3 = val;
                                });
                                break;
                        }
                    }
                    slider = blendRoot.FindDeepChildLiberal("right")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupBlendG_1.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendG_1;
                                    val.y = value;
                                    character.MakeupBlendG_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupBlendG_2.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendG_2;
                                    val.y = value;
                                    character.MakeupBlendG_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupBlendG_3.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendG_3;
                                    val.y = value;
                                    character.MakeupBlendG_3 = val;
                                });
                                break;
                        }
                    }
                }
                blendRoot = makeupCustomizationWindow.FindDeepChildLiberal("eyeshadowBlend");
                if (blendRoot != null)
                {
                    var slider = blendRoot.FindDeepChildLiberal("left")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupBlendB_1.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendB_1;
                                    val.x = value;
                                    character.MakeupBlendB_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupBlendB_2.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendB_2;
                                    val.x = value;
                                    character.MakeupBlendB_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupBlendB_3.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendB_3;
                                    val.x = value;
                                    character.MakeupBlendB_3 = val;
                                });
                                break;
                        }
                    }
                    slider = blendRoot.FindDeepChildLiberal("right")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupBlendB_1.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendB_1;
                                    val.y = value;
                                    character.MakeupBlendB_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupBlendB_2.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendB_2;
                                    val.y = value;
                                    character.MakeupBlendB_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupBlendB_3.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendB_3;
                                    val.y = value;
                                    character.MakeupBlendB_3 = val;
                                });
                                break;
                        }
                    }
                }
                blendRoot = makeupCustomizationWindow.FindDeepChildLiberal("blushBlend");
                if (blendRoot != null)
                {
                    var slider = blendRoot.FindDeepChildLiberal("left")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupBlendA_1.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendA_1;
                                    val.x = value;
                                    character.MakeupBlendA_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupBlendA_2.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendA_2;
                                    val.x = value;
                                    character.MakeupBlendA_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupBlendA_3.x);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendA_3;
                                    val.x = value;
                                    character.MakeupBlendA_3 = val;
                                });
                                break;
                        }
                    }
                    slider = blendRoot.FindDeepChildLiberal("right")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupBlendA_1.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendA_1;
                                    val.y = value;
                                    character.MakeupBlendA_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupBlendA_2.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendA_2;
                                    val.y = value;
                                    character.MakeupBlendA_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupBlendA_3.y);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupBlendA_3;
                                    val.y = value;
                                    character.MakeupBlendA_3 = val;
                                });
                                break;
                        }
                    }
                }

                var strengthRoot = makeupCustomizationWindow.FindDeepChildLiberal("lipStrength");
                if (strengthRoot != null)
                {
                    var slider = strengthRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupStrengthR_1);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthR_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupStrengthR_2);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthR_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupStrengthR_3);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthR_3 = value;
                                });
                                break;
                        }
                    }
                }
                strengthRoot = makeupCustomizationWindow.FindDeepChildLiberal("eyelinerStrength");
                if (strengthRoot != null)
                {
                    var slider = strengthRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupStrengthG_1);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthG_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupStrengthG_2);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthG_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupStrengthG_3);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthG_3 = value;
                                });
                                break;
                        }
                    }
                }
                strengthRoot = makeupCustomizationWindow.FindDeepChildLiberal("eyeshadowStrength");
                if (strengthRoot != null)
                {
                    var slider = strengthRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupStrengthB_1);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthB_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupStrengthB_2);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthB_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupStrengthB_3);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthB_3 = value;
                                });
                                break;
                        }
                    }
                }
                strengthRoot = makeupCustomizationWindow.FindDeepChildLiberal("blushStrength");
                if (strengthRoot != null)
                {
                    var slider = strengthRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupStrengthA_1);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthA_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupStrengthA_2);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthA_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupStrengthA_3);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupStrengthA_3 = value;
                                });
                                break;
                        }
                    }
                }

                var specularityRoot = makeupCustomizationWindow.FindDeepChildLiberal("lipSpecularity");
                if (specularityRoot != null)
                {
                    var slider = specularityRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorR_1.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorR_1;
                                    val.a = value;
                                    character.MakeupSpecColorR_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorR_2.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorR_2;
                                    val.a = value;
                                    character.MakeupSpecColorR_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorR_3.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorR_3;
                                    val.a = value;
                                    character.MakeupSpecColorR_3 = val;
                                });
                                break;
                        }
                    }
                }
                specularityRoot = makeupCustomizationWindow.FindDeepChildLiberal("eyelinerSpecularity");
                if (specularityRoot != null)
                {
                    var slider = specularityRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorG_1.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorG_1;
                                    val.a = value;
                                    character.MakeupSpecColorG_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorG_2.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorG_2;
                                    val.a = value;
                                    character.MakeupSpecColorG_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorG_3.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorG_3;
                                    val.a = value;
                                    character.MakeupSpecColorG_3 = val;
                                });
                                break;
                        }
                    }
                }
                specularityRoot = makeupCustomizationWindow.FindDeepChildLiberal("eyeshadowSpecularity");
                if (specularityRoot != null)
                {
                    var slider = specularityRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorB_1.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorB_1;
                                    val.a = value;
                                    character.MakeupSpecColorB_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorB_2.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorB_2;
                                    val.a = value;
                                    character.MakeupSpecColorB_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorB_3.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorB_3;
                                    val.a = value;
                                    character.MakeupSpecColorB_3 = val;
                                });
                                break;
                        }
                    }
                }
                specularityRoot = makeupCustomizationWindow.FindDeepChildLiberal("blushSpecularity");
                if (specularityRoot != null)
                {
                    var slider = specularityRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorA_1.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorA_1;
                                    val.a = value;
                                    character.MakeupSpecColorA_1 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorA_2.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorA_2;
                                    val.a = value;
                                    character.MakeupSpecColorA_2 = val;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupSpecColorA_3.a);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    var val = character.MakeupSpecColorA_3;
                                    val.a = value;
                                    character.MakeupSpecColorA_3 = val;
                                });
                                break;
                        }
                    }
                }

                var smoothingRoot = makeupCustomizationWindow.FindDeepChildLiberal("lipSmoothing");
                if (smoothingRoot != null)
                {
                    var slider = smoothingRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingR_1);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingR_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingR_2);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingR_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingR_3);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingR_3 = value;
                                });
                                break;
                        }
                    }
                }
                smoothingRoot = makeupCustomizationWindow.FindDeepChildLiberal("eyelinerSmoothing");
                if (smoothingRoot != null)
                {
                    var slider = smoothingRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingG_1);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingG_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingG_2);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingG_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingG_3);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingG_3 = value;
                                });
                                break;
                        }
                    }
                }
                smoothingRoot = makeupCustomizationWindow.FindDeepChildLiberal("eyeshadowSmoothing");
                if (smoothingRoot != null)
                {
                    var slider = smoothingRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingB_1);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingB_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingB_2);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingB_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingB_3);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingB_3 = value;
                                });
                                break;
                        }
                    }
                }
                smoothingRoot = makeupCustomizationWindow.FindDeepChildLiberal("blushSmoothing");
                if (smoothingRoot != null)
                {
                    var slider = smoothingRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();

                        switch (makeupEditLayer)
                        {
                            case MakeupEditLayer.Layer1:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingA_1);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingA_1 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer2:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingA_2);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingA_2 = value;
                                });
                                break;

                            case MakeupEditLayer.Layer3:
                                slider.SetValueWithoutNotify(character.MakeupSmoothingA_3);
                                slider.onValueChanged.AddListener((value) =>
                                {
                                    character.MakeupSmoothingA_3 = value;
                                });
                                break;
                        }
                    }
                }

            }

            RefreshMakeupStyles();
        }

        /*protected Slider makeupFinalBlendSlinder;

        protected Dropdown lipstickBlendTypeDropdown;
        protected Slider lipstickBlendSlider;
        protected Slider lipstickStrengthSlider;
        protected Slider lipstickSpecularitySlider;
        protected Slider lipstickSmoothingSlider;

        protected Dropdown eyelinerBlendTypeDropdown;
        protected Slider eyelinerBlendSlider;
        protected Slider eyelinerStrengthSlider;
        protected Slider eyelinerSpecularitySlider;
        protected Slider eyelinerSmoothingSlider;

        protected Dropdown eyeshadowBlendTypeDropdown;
        protected Slider eyeshadowBlendSlider;
        protected Slider eyeshadowStrengthSlider;
        protected Slider eyeshadowSpecularitySlider;
        protected Slider eyeshadowSmoothingSlider;

        protected Dropdown blushBlendTypeDropdown;
        protected Slider blushBlendSlider;
        protected Slider blushStrengthSlider;
        protected Slider blushSpecularitySlider;
        protected Slider blushSmoothingSlider;*/

        public void OpenMakeupCustomization()
        {
            CloseRightPanel();
            if (rightPanel != null)
            {
                rightPanel.SetActive(true);
            }

            if (makeupCustomizationWindow != null)
            {
                makeupCustomizationWindow.gameObject.SetActive(true);

                if (character != null)
                {
                    ColorPickableGraphic colorPickable;

                    colorPickable = GetColorPickableGraphic(id_bodyPaintColor);
                    if (colorPickable != null)
                    {
                        colorPickable.SetColor(character.BodyPaintColor);
                        colorPickable.ClearListeners();
                        colorPickable.AddListener((color) =>
                        {
                            character.BodyPaintColor = color;
                        });
                    }
                    colorPickable = GetColorPickableGraphic(id_bodyPaintSpecColor);
                    if (colorPickable != null)
                    {
                        colorPickable.SetColor(character.BodyPaintSpecColor);
                        colorPickable.ClearListeners();
                        colorPickable.AddListener((color) =>
                        {
                            color.a = character.BodyPaintSpecColor.a;
                            character.BodyPaintSpecColor = color;
                        });
                    }

                    var objRoot = makeupCustomizationWindow.FindDeepChildLiberal("bodyPaintSpecularity");
                    if (objRoot != null)
                    {
                        var slider = objRoot.GetComponentInChildren<Slider>(true);
                        if (slider != null)
                        {
                            if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                            slider.onValueChanged.RemoveAllListeners();
                            slider.SetValueWithoutNotify(character.BodyPaintSpecColor.a);
                            slider.onValueChanged.AddListener((value) =>
                            {
                                var col = character.BodyPaintSpecColor;
                                col.a = value;
                                character.BodyPaintSpecColor = col;
                            });
                        }
                    }

                    objRoot = makeupCustomizationWindow.FindDeepChildLiberal("bodyPaintSmoothing");
                    if (objRoot != null)
                    {
                        var slider = objRoot.GetComponentInChildren<Slider>(true);
                        if (slider != null)
                        {
                            if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                            slider.onValueChanged.RemoveAllListeners();
                            slider.SetValueWithoutNotify(character.BodyPaintSmoothing);
                            slider.onValueChanged.AddListener((value) =>
                            {
                                character.BodyPaintSmoothing = value;
                            });
                        }
                    }

                    SetMakeupEditLayer(makeupEditLayer, true); 

                }
            }
        }

        #endregion

        #region Body

        public const string _sexCustomizationCategoryName = "Sex Appeal";

        [Header("Body")]
        public RectTransform sexCustomizationWindow;

        public void OpenSexCustomization()
        {
            CloseRightPanel();
            if (rightPanel != null)
            {
                rightPanel.SetActive(true);
            }

            if (sexCustomizationWindow != null)
            {
                sexCustomizationWindow.gameObject.SetActive(true);

                if (character != null)
                {
                    var objRoot = sexCustomizationWindow.FindDeepChildLiberal("bustSize");
                    if (objRoot != null)
                    {
                        var slider = objRoot.GetComponentInChildren<Slider>(true);
                        if (slider != null)
                        {
                            if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                            slider.onValueChanged.RemoveAllListeners();
                            slider.SetValueWithoutNotify(character.CharacterBustSize);
                            slider.onValueChanged.AddListener((value) =>
                            {
                                character.SetBustSize(value);
                            });
                        }
                    }

                }
            }
        }

        #endregion

        #region Hair

        public const string _hairCustomizationCategoryName = "Hairstyle";
        public const string id_hairPrimaryColor = "hairPrimaryColor";
        public const string id_hairSecondaryColor = "hairSecondaryColor";
        public const string id_hairSpecColor = "hairSpecColor";
        public const string id_hairPrimarySpecTint = "hairPrimarySpecTint";
        public const string id_hairSecondarySpecTint = "hairSecondarySpecTint";

        [Header("Hair")]
        public RectTransform hairCustomizationWindow;

        public RectTransform hairstyleLayoutTransform;
        private PrefabPool hairstylePool;
        public GameObject hairstylePrototype;
        public Material hairstyleMaterial;
        private readonly List<Graphic> hairstyleMaterialUsers = new List<Graphic>();
        private void RefreshHairstyleMaterials()
        {
            Color hairPrimaryColor = character.HairColor.Opaque();
            Color hairPrimaryLobeSpecTint = character.HairPrimarySpecularTint.Opaque() * character.HairSpecularColor.Opaque();
            Color hairSecondaryLobeSpecTint = character.HairSecondarySpecularTint.Opaque() * character.HairSpecularColor.Opaque();

            hairstyleMaterial.SetColor("_ColorA", hairPrimaryColor);
            hairstyleMaterial.SetColor("_ColorB", hairPrimaryColor + hairPrimaryLobeSpecTint * 1.45f);
            hairstyleMaterial.SetColor("_ColorC", hairPrimaryColor + hairSecondaryLobeSpecTint * 1.45f); 
            foreach (var img in hairstyleMaterialUsers)
            {
                img.enabled = false;
                img.enabled = true;
            }
        }
        private void ClearHairstyles()
        {
            hairstyleMaterialUsers.Clear();

            if (hairstyleLayoutTransform != null && hairstylePool != null)
            {
                hairstylePool.ReleaseAllClaims();
                for (int a = 0; a < hairstyleLayoutTransform.childCount; a++)
                {
                    var child = hairstyleLayoutTransform.GetChild(a);
                    if (child == null) continue;
                    child.gameObject.SetActive(false);
                }
            }
        }
        private GameObject ClaimNewHairstyleInstance(out Image hairImage)
        {
            hairImage = null;
            if (hairstylePool == null || !hairstylePool.TryGetNewInstance(out GameObject instance)) return null;
            if (instance == null) return null;

            instance.SetActive(true);

            var hairObj = instance.transform.FindDeepChildLiberal("hairImage");
            hairImage = hairObj == null ? null : hairObj.GetComponentInChildren<Image>(true);
            if (hairImage != null) hairImage.material = hairstyleMaterial;

            return instance;
        }

        public RectTransform hairColorLayoutTransform;
        private PrefabPool hairColorPool;
        public GameObject hairColorPrototype;
        private ObjectPool<Material> hairColorMaterials;
        private void RefreshHairColorPresets()
        {
            ClearHairColorPresets();
            if (character.hairColorPresets != null)
            {
                for (int a = 0; a < character.hairColorPresets.Count; a++)
                {
                    var preset = character.hairColorPresets.GetPalette(a);
                    if (preset == null) continue;

                    var inst = ClaimNewHairColorInstance(out var hairImage, out var hairMaterial);
                    if (inst != null)
                    {

                        inst.name = preset.name;
                        Color colorA = preset.GetColor("primaryColor").Opaque();
                        Color specColor = preset.GetColor("specColor").Opaque();
                        Color colorB = preset.GetColor("specPrimaryTint").Opaque();
                        Color colorC = preset.GetColor("specSecondaryTint").Opaque();

                        hairMaterial.SetColor("_ColorA", colorA);
                        hairMaterial.SetColor("_ColorB", colorA + colorB * specColor * 1.45f);
                        hairMaterial.SetColor("_ColorC", colorA + colorC * specColor * 1.45f);

                        hairImage.enabled = false;
                        hairImage.enabled = true;

                        var button = inst.GetComponentInChildren<Button>(true);
                        if (button != null)
                        {
                            if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                            button.onClick.RemoveAllListeners();
                            button.onClick.AddListener(() =>
                            {
                                ColorPickableGraphic colorPickable;
                                colorPickable = GetColorPickableGraphic(id_hairPrimaryColor);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(colorA);
                                    character.HairColor = colorA;
                                }
                                colorPickable = GetColorPickableGraphic(id_hairSecondaryColor);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    var col = preset.GetColor("secondaryColor").Opaque();
                                    colorPickable.SetColor(col);
                                    character.HairSecondaryColor = col;
                                }
                                colorPickable = GetColorPickableGraphic(id_hairSpecColor);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(specColor);
                                    character.HairSpecularColor = specColor;
                                }

                                colorPickable = GetColorPickableGraphic(id_hairPrimarySpecTint);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(colorB);
                                    character.HairPrimarySpecularTint = colorB;
                                }
                                colorPickable = GetColorPickableGraphic(id_hairSecondarySpecTint);
                                if (colorPickable != null && colorPickable.material != null)
                                {
                                    colorPickable.SetColor(colorC);
                                    character.HairSecondarySpecularTint = colorC; 
                                }

                                character.HairSmoothness = preset.HasPrefixAttribute("smoothness:") ? float.Parse(preset.GetAttributeValue("smoothness:")) : SandboxCharacter._default_hairSmoothness;
                                character.HairPrimaryLobeSmoothness = preset.HasPrefixAttribute("smoothnessPrimaryLobe:") ? float.Parse(preset.GetAttributeValue("smoothnessPrimaryLobe:")) : SandboxCharacter._default_hairPrimaryLobeSmoothness;
                                character.HairSecondaryLobeSmoothness = preset.HasPrefixAttribute("smoothnessSecondaryLobe:") ? float.Parse(preset.GetAttributeValue("smoothnessSecondaryLobe:")) : SandboxCharacter._default_hairSecondaryLobeSmoothness;

                                if (hairSmoothnessSlider != null)
                                {
                                    hairSmoothnessSlider.SetValueWithoutNotify(character.HairSmoothness);
                                }
                                if (hairSmoothnessPrimaryLobeSlider != null)
                                {
                                    hairSmoothnessPrimaryLobeSlider.SetValueWithoutNotify(character.HairPrimaryLobeSmoothness);
                                }
                                if (hairSmoothnessSecondaryLobeSlider != null)
                                {
                                    hairSmoothnessSecondaryLobeSlider.SetValueWithoutNotify(character.HairSecondaryLobeSmoothness); 
                                }

                                RefreshHairstyleMaterials();
                            });
                        }
                    }
                }
            }
        }
        private void ClearHairColorPresets()
        {
            if (hairColorLayoutTransform != null && hairColorPool != null)
            {
                hairColorPool.ReleaseAllClaims();
                for (int a = 0; a < hairColorLayoutTransform.childCount; a++)
                {
                    var child = hairColorLayoutTransform.GetChild(a);
                    if (child == null) continue;
                    child.gameObject.SetActive(false);
                }
            }
        }
        private GameObject ClaimNewHairColorInstance(out Image hairImage, out Material hairMaterial)
        {
            hairImage = null;
            hairMaterial = null;
            if (hairColorPool == null || !hairColorPool.TryGetNewInstance(out GameObject instance)) return null;
            if (instance == null) return null;

            instance.SetActive(true);

            var hairObj = instance.transform.FindDeepChildLiberal("hairImage");
            hairImage = hairObj == null ? null : hairObj.GetComponentInChildren<Image>(true);
            if (hairImage != null)
            {
                hairMaterial = hairImage.material;
                if (hairMaterial == null || !hairColorMaterials.IsInPool(hairMaterial))
                {
                    hairColorMaterials.TryGetNewInstance(out hairMaterial); 
                    if (hairMaterial == null)
                    {
                        hairImage = null;
                        instance.SetActive(false);
                        hairColorPool.Release(instance);
                        return null;
                    }
                }

                hairImage.material = hairMaterial;
            }

            return instance;
        }

        private Slider hairSmoothnessSlider;
        private Slider hairSmoothnessPrimaryLobeSlider;
        private Slider hairSmoothnessSecondaryLobeSlider;

        public void OpenHairCustomization()
        {
            CloseRightPanel(); 
            if (rightPanel != null)
            {
                rightPanel.SetActive(true);
            }

            if (hairCustomizationWindow != null)
            {
                hairCustomizationWindow.gameObject.SetActive(true);

                ClearHairstyles();
                if (character.hairstyles != null)
                {
                    for(int a = 0; a < character.hairstyles.Count; a++)
                    {
                        var style = character.hairstyles.GetAddition(a);
                        if (style == null) continue;

                        int styleIndex = a;

                        var inst = ClaimNewHairstyleInstance(out var hairImg);

                        hairstyleMaterialUsers.Add(hairImg);

                        hairImg.sprite = style.icon != null ? style.icon : null;  

                        hairImg.enabled = false;
                        hairImg.enabled = true;

                        var button = inst.GetComponentInChildren<Button>(true);
                        if (button != null)
                        {
                            if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                            button.onClick.RemoveAllListeners();
                            button.onClick.AddListener(() =>
                            {
                                character.HairStyle = styleIndex + 1;
                            });
                        }
                    }
                }

                var colorPickable = GetColorPickableGraphic(id_hairPrimaryColor);
                if (colorPickable != null && colorPickable.material != null)
                {
                    colorPickable.SetColor(character.HairColor);
                    colorPickable.ClearListeners();
                    colorPickable.AddListener((color) =>
                    {
                        character.HairColor = color.Opaque();
                        RefreshHairstyleMaterials();
                    });
                }
                colorPickable = GetColorPickableGraphic(id_hairSecondaryColor);
                if (colorPickable != null && colorPickable.material != null)
                {
                    colorPickable.SetColor(character.HairSecondaryColor);
                    colorPickable.ClearListeners();
                    colorPickable.AddListener((color) =>
                    {
                        character.HairSecondaryColor = color.Opaque();
                        RefreshHairstyleMaterials();
                    });
                }

                colorPickable = GetColorPickableGraphic(id_hairSpecColor);
                if (colorPickable != null && colorPickable.material != null)
                {
                    colorPickable.SetColor(character.HairSpecularColor);
                    colorPickable.ClearListeners();
                    colorPickable.AddListener((color) =>
                    {
                        character.HairSpecularColor = color.Opaque();
                        RefreshHairstyleMaterials();
                    });
                }

                colorPickable = GetColorPickableGraphic(id_hairPrimarySpecTint);
                if (colorPickable != null && colorPickable.material != null)
                {
                    colorPickable.SetColor(character.HairPrimarySpecularTint);
                    colorPickable.ClearListeners();
                    colorPickable.AddListener((color) =>
                    {
                        character.HairPrimarySpecularTint = color.Opaque();
                        RefreshHairstyleMaterials();
                    });
                }
                colorPickable = GetColorPickableGraphic(id_hairSecondarySpecTint);
                if (colorPickable != null && colorPickable.material != null)
                {
                    colorPickable.SetColor(character.HairSecondarySpecularTint);
                    colorPickable.ClearListeners();
                    colorPickable.AddListener((color) =>
                    {
                        character.HairSecondarySpecularTint = color.Opaque();
                        RefreshHairstyleMaterials(); 
                    });
                }

                var panelRoot = hairCustomizationWindow.FindDeepChildLiberal("smoothness");
                if (panelRoot != null)
                {
                    var slider = panelRoot.FindDeepChildLiberal("value")?.GetComponent<Slider>();
                    hairSmoothnessSlider = slider;
                    if (slider != null)
                    {
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();
                        slider.SetValueWithoutNotify(character.HairSmoothness);
                        slider.onValueChanged.AddListener((value) =>
                        {
                            character.HairSmoothness = value;
                        });
                    }

                    CustomEditorUtils.SetButtonOnClickActionByName(panelRoot, "reset", () =>
                    {
                        character.HairSmoothness = SandboxCharacter._default_hairSmoothness;
                        if (slider != null) slider.SetValueWithoutNotify(character.HairSmoothness);
                    }, true, true, true);
                }

                panelRoot = hairCustomizationWindow.FindDeepChildLiberal("lobeSmoothness");
                if (panelRoot != null)
                {
                    var sliderPrimary = panelRoot.FindDeepChildLiberal("primary")?.GetComponent<Slider>();
                    hairSmoothnessPrimaryLobeSlider = sliderPrimary;
                    if (sliderPrimary != null)
                    {
                        if (sliderPrimary.onValueChanged == null) sliderPrimary.onValueChanged = new Slider.SliderEvent();
                        sliderPrimary.onValueChanged.RemoveAllListeners();
                        sliderPrimary.SetValueWithoutNotify(character.HairPrimaryLobeSmoothness);
                        sliderPrimary.onValueChanged.AddListener((value) =>
                        {
                            character.HairPrimaryLobeSmoothness = value;
                        });
                    }

                    var sliderSecondary = panelRoot.FindDeepChildLiberal("secondary")?.GetComponent<Slider>();
                    hairSmoothnessSecondaryLobeSlider = sliderSecondary;
                    if (sliderSecondary != null)
                    {
                        if (sliderSecondary.onValueChanged == null) sliderSecondary.onValueChanged = new Slider.SliderEvent();
                        sliderSecondary.onValueChanged.RemoveAllListeners();
                        sliderSecondary.SetValueWithoutNotify(character.HairSecondaryLobeSmoothness);
                        sliderSecondary.onValueChanged.AddListener((value) =>
                        {
                            character.HairSecondaryLobeSmoothness = value;
                        });
                    }

                    CustomEditorUtils.SetButtonOnClickActionByName(panelRoot, "reset", () =>
                    {
                        character.HairPrimaryLobeSmoothness = SandboxCharacter._default_hairPrimaryLobeSmoothness;
                        character.HairSecondaryLobeSmoothness = SandboxCharacter._default_hairSecondaryLobeSmoothness;

                        if (sliderPrimary != null) sliderPrimary.SetValueWithoutNotify(character.HairPrimaryLobeSmoothness);
                        if (sliderSecondary != null) sliderSecondary.SetValueWithoutNotify(character.HairSecondaryLobeSmoothness);
                    }, true, true, true);
                }

                RefreshHairColorPresets();
                RefreshHairstyleMaterials();
            }
        }
        #endregion

        protected void Awake()
        {
            camera_focusHeight = camera_startFocusHeight;
            camera_height = camera_startHeight;
            camera_zoom = camera_startZoom;
            camera_angle = camera_startAngle;

            customizableFeaturesList = customizableFeaturesPanel.GetComponentInChildren<UICategorizedList>(); 

            customizableFeaturesGroupPool = new GameObject(nameof(customizableFeaturesGroupPool)).AddComponent<PrefabPool>();

            customizableFeaturesGroupPool.transform.SetParent(transform, false);
          
            customizableFeaturesGroupPool.Reinitialize(customizableFeaturesGroupPrototype, PoolGrowthMethod.Incremental, 1, 1, 512);
            customizableFeaturesGroupPool.SetContainerTransform(customizableFeaturesGroupContainer, true, false, false); 
            customizableFeaturesGroupPool.forceParentOnClaim = true;
            customizableFeaturesGroupPool.worldPositionStaysWhenParented = false;
            if (customizableFeaturesGroupPrototype.transform.parent != null) customizableFeaturesGroupPrototype.transform.SetParent(customizableFeaturesGroupPrototype.transform.parent.parent, false); // re-parent to upper parent so it stops intefering with ordering by sibling index
            customizableFeaturesGroupPrototype.SetActive(false);

            if (mainTabGroup != null) 
            {
                mainTabGroup.Initialize(); 
                var tab = mainTabGroup.FindTab(MainTab.Identity.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent(); 
                    tab.button.onClick.AddListener(() => SetMainTab(MainTab.Identity));
                }
                tab = mainTabGroup.FindTab(MainTab.Look.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetMainTab(MainTab.Look));
                }
                tab = mainTabGroup.FindTab(MainTab.Fit.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetMainTab(MainTab.Fit));
                }
                tab = mainTabGroup.FindTab(MainTab.Traits.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetMainTab(MainTab.Traits));
                }
                tab = mainTabGroup.FindTab(MainTab.Finalize.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetMainTab(MainTab.Finalize));
                }
            }

            if (backgroundColorButton != null && backgroundColorMaterial != null)
            {
                backgroundColorMaterial.color = backgroundStartColor;

                var canvas = backgroundColorButton.gameObject.GetComponentInParent<Canvas>(true);

                if (backgroundColorButton.onClick == null) backgroundColorButton.onClick = new Button.ButtonClickedEvent();
                backgroundColorButton.onClick.AddListener(() =>
                {
                    OpenColorPicker(ColorPickerID.left, canvas.transform.TransformPoint(canvas.ScreenToCanvasSpace(CursorProxy.ScreenPosition)), (Color color) => backgroundColorMaterial.color = color, backgroundColorMaterial.color);
                });
            }

            RectTransform panelRoot;

            #region Eyes
            if (eyeEditModeGroup != null) 
            { 
                eyeEditModeGroup.Initialize();
                var tab = eyeEditModeGroup.FindTab(EyeEditMode.Both.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetEyeEditMode(EyeEditMode.Both));
                }

                tab = eyeEditModeGroup.FindTab(EyeEditMode.Left.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetEyeEditMode(EyeEditMode.Left));
                }
                tab = eyeEditModeGroup.FindTab(EyeEditMode.Right.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetEyeEditMode(EyeEditMode.Right)); 
                }
            }
            if (eyeCustomizationWindow != null)
            {
                if (irisColorLayoutTransform != null)
                {
                    irisColorPool = irisColorLayoutTransform.gameObject.AddOrGetComponent<PrefabPool>();
                    irisColorPool.Reinitialize(irisColorPrototype, PoolGrowthMethod.Incremental, 1, 1, 2048);
                    irisColorPool.SetContainerTransform(irisColorLayoutTransform, true, false, false);
                    irisColorPool.forceParentOnClaim = true;
                    irisColorPool.worldPositionStaysWhenParented = false;
                    irisColorPrototype.SetActive(false);
                    irisColorMaterials = new ObjectPool<Material>();
                    irisColorMaterials.Reinitialize(irisStyleMaterial, PoolGrowthMethod.Incremental, 1, 1, 2048);
                    scleraColorMaterials = new ObjectPool<Material>();
                    scleraColorMaterials.Reinitialize(scleraStyleColorMaskMaterial, PoolGrowthMethod.Incremental, 1, 1, 2048);

                    scleraStyleColorMaskMaterial = Instantiate(scleraStyleColorMaskMaterial);
                }
                if (irisStyleLayoutTransform != null)
                {
                    irisStylePool = irisStyleLayoutTransform.gameObject.AddOrGetComponent<PrefabPool>();
                    irisStylePool.Reinitialize(irisStylePrototype, PoolGrowthMethod.Incremental, 1, 1, 2048);
                    irisStylePool.SetContainerTransform(irisStyleLayoutTransform, true, false, false);
                    irisStylePool.forceParentOnClaim = true;
                    irisStylePool.worldPositionStaysWhenParented = false;
                    irisStylePrototype.SetActive(false);

                    irisStyleMaterial = Instantiate(irisStyleMaterial);
                }

                panelRoot = (RectTransform)eyeCustomizationWindow.FindDeepChildLiberal("irisColors");
                if (panelRoot != null)
                {
                    if (GetColorPickableGraphic(id_irisColor1) == null)
                    {
                        var baseRT = panelRoot.FindDeepChildLiberal("color1");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_irisColor1, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                    if (GetColorPickableGraphic(id_irisColor2) == null)
                    {
                        var baseRT = panelRoot.FindDeepChildLiberal("color2");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_irisColor2, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                    if (GetColorPickableGraphic(id_irisColor3) == null)
                    {
                        var baseRT = panelRoot.FindDeepChildLiberal("color3");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_irisColor3, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                }

                scleraColorsRoot = (RectTransform)eyeCustomizationWindow.FindDeepChildLiberal("scleraColors");
                if (scleraColorsRoot != null)
                {
                    if (GetColorPickableGraphic(id_scleraColor1) == null)
                    {
                        var baseRT = scleraColorsRoot.FindDeepChildLiberal("color1");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_scleraColor1, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                    if (GetColorPickableGraphic(id_scleraColor2) == null)
                    {
                        var baseRT = scleraColorsRoot.FindDeepChildLiberal("color2");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_scleraColor2, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                    if (GetColorPickableGraphic(id_scleraColor3) == null)
                    {
                        var baseRT = scleraColorsRoot.FindDeepChildLiberal("color3");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_scleraColor3, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                }

                panelRoot = (RectTransform)eyeCustomizationWindow.FindDeepChildLiberal("scleraTint");
                if (panelRoot != null)
                {
                    if (GetColorPickableGraphic(id_scleraTint) == null)
                    {
                        var baseRT = panelRoot.FindDeepChildLiberal("tint");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_scleraTint, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true }); 
                            }
                        }
                    }
                }
            }
            #endregion

            #region Skin

            if (skinDetailTabGroup != null)
            {
                skinDetailTabGroup.Initialize();

                var tab = skinDetailTabGroup.FindTab(SkinDetailEditMode.Freckles.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetSkinDetailEditMode(SkinDetailEditMode.Freckles));
                }

                tab = skinDetailTabGroup.FindTab(SkinDetailEditMode.Moles.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetSkinDetailEditMode(SkinDetailEditMode.Moles));
                }

                tab = skinDetailTabGroup.FindTab(SkinDetailEditMode.Pimples.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetSkinDetailEditMode(SkinDetailEditMode.Pimples));
                }

                tab = skinDetailTabGroup.FindTab(SkinDetailEditMode.Scarring.ToString());
                if (tab != null && tab.button != null)
                {
                    if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                    tab.button.onClick.AddListener(() => SetSkinDetailEditMode(SkinDetailEditMode.Scarring));
                }
            }

            if (skinPresetLayoutTransform != null)
            {
                skinPresetPool = skinPresetLayoutTransform.gameObject.AddOrGetComponent<PrefabPool>();
                skinPresetPool.Reinitialize(skinPresetPrototype, PoolGrowthMethod.Incremental, 1, 1, 2048);
                skinPresetPool.SetContainerTransform(skinPresetLayoutTransform, true, false, false);
                skinPresetPool.forceParentOnClaim = true;
                skinPresetPool.worldPositionStaysWhenParented = false;
                skinPresetPrototype.SetActive(false);

                skinPresetMaterials = new ObjectPool<Material>();
                skinPresetMaterials.Reinitialize(skinPresetMaterial, PoolGrowthMethod.Incremental, 1, 1, 2048);
            }

            panelRoot = (RectTransform)skinCustomizationWindow.FindDeepChildLiberal("skinColor");
            if (panelRoot != null)
            {
                if (GetColorPickableGraphic(id_skinTint) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("skinTint");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>(); 
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_skinTint, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDRAlpha, ignoreAlpha = true });
                        }
                    }
                }
                if (GetColorPickableGraphic(id_sssTint) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("skinScatterTint");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_sssTint, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDRAlpha, ignoreAlpha = true });
                        }
                    }
                }
            }

            panelRoot = (RectTransform)skinCustomizationWindow.FindDeepChildLiberal("veinColor");
            if (panelRoot != null)
            {
                if (GetColorPickableGraphic(id_veinTint) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("veinTint");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_veinTint, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDRAlpha, ignoreAlpha = true }); 
                        }
                    }
                }
            }

            panelRoot = (RectTransform)skinCustomizationWindow.FindDeepChildLiberal("markingsColor");
            if (panelRoot != null)
            {
                if (GetColorPickableGraphic(id_markingsColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("color");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_markingsColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDRAlpha, ignoreAlpha = true });
                        }
                    }
                }
            }

            #endregion

            #region Makeup

            if (makeupLayerTabGroup != null)
            {
                makeupLayerTabGroup.Initialize();

                var vals = Enum.GetValues(typeof(MakeupEditLayer));
                for(int a = 0; a < vals.Length; a++)
                {
                    var val = (MakeupEditLayer)vals.GetValue(a);

                    var tab = makeupLayerTabGroup.FindTab(val.ToString());
                    if (tab != null && tab.button != null)
                    {
                        if (tab.button.onClick == null) tab.button.onClick = new Button.ButtonClickedEvent();
                        tab.button.onClick.AddListener(() => SetMakeupEditLayer(val));
                    }
                }
            }

            if (makeupStyleLayoutTransform != null)
            {
                makeupStylePool = makeupStyleLayoutTransform.gameObject.AddOrGetComponent<PrefabPool>();
                makeupStylePool.Reinitialize(makeupStylePrototype, PoolGrowthMethod.Incremental, 1, 1, 2048);
                makeupStylePool.SetContainerTransform(makeupStyleLayoutTransform, true, false, false);
                makeupStylePool.forceParentOnClaim = true;
                makeupStylePool.worldPositionStaysWhenParented = false; 
                makeupStylePrototype.SetActive(false);

                makeupStyleMaterials = new ObjectPool<Material>();
                makeupStyleMaterials.Reinitialize(makeupStyleMaterial, PoolGrowthMethod.Incremental, 1, 1, 2048);
            }

            panelRoot = (RectTransform)makeupCustomizationWindow.FindDeepChildLiberal("bodyPaintColor");
            if (panelRoot != null)
            {
                if (GetColorPickableGraphic(id_bodyPaintColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("color");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_bodyPaintColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDRAlpha, ignoreAlpha = true });
                        }
                    }
                }
                if (GetColorPickableGraphic(id_bodyPaintSpecColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("specColor");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_bodyPaintSpecColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                        }
                    }
                }
            }

            panelRoot = (RectTransform)makeupCustomizationWindow.FindDeepChildLiberal("lipColor");
            if (panelRoot != null)
            {
                if (GetColorPickableGraphic(id_lipstickColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("color");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_lipstickColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDRAlpha, ignoreAlpha = true });
                        }
                    }
                }
                if (GetColorPickableGraphic(id_lipstickSpecColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("specColor");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_lipstickSpecColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                        }
                    }
                }
            }

            panelRoot = (RectTransform)makeupCustomizationWindow.FindDeepChildLiberal("eyelinerColor");
            if (panelRoot != null)
            {
                if (GetColorPickableGraphic(id_eyelinerColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("color");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_eyelinerColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDRAlpha, ignoreAlpha = true });
                        }
                    }
                }
                if (GetColorPickableGraphic(id_eyelinerSpecColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("specColor");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_eyelinerSpecColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                        }
                    }
                }
            }

            panelRoot = (RectTransform)makeupCustomizationWindow.FindDeepChildLiberal("eyeshadowColor");
            if (panelRoot != null)
            {
                if (GetColorPickableGraphic(id_eyeshadowColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("color"); 
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_eyeshadowColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDRAlpha, ignoreAlpha = true });
                        }
                    }
                }
                if (GetColorPickableGraphic(id_eyeshadowSpecColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("specColor");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_eyeshadowSpecColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                        }
                    }
                }
            }

            panelRoot = (RectTransform)makeupCustomizationWindow.FindDeepChildLiberal("blushColor");
            if (panelRoot != null)
            {
                if (GetColorPickableGraphic(id_blushColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("color");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_blushColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDRAlpha, ignoreAlpha = true });
                        }
                    }
                }
                if (GetColorPickableGraphic(id_blushSpecColor) == null)
                {
                    var baseRT = panelRoot.FindDeepChildLiberal("specColor");
                    if (baseRT != null)
                    {
                        var graphic = baseRT.GetComponent<Graphic>();
                        var button = baseRT.GetComponent<Button>();
                        if (graphic != null && button != null)
                        {
                            colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_blushSpecColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                        }
                    }
                }
            }

            #endregion

            #region Hair

            if (hairCustomizationWindow != null)
            {
                if (hairColorLayoutTransform != null)
                {
                    hairColorPool = hairColorLayoutTransform.gameObject.AddOrGetComponent<PrefabPool>();
                    hairColorPool.Reinitialize(hairColorPrototype, PoolGrowthMethod.Incremental, 1, 1, 2048);
                    hairColorPool.SetContainerTransform(hairColorLayoutTransform, true, false, false);
                    hairColorPool.forceParentOnClaim = true;
                    hairColorPool.worldPositionStaysWhenParented = false;
                    hairColorPrototype.SetActive(false);
                    hairColorMaterials = new ObjectPool<Material>();
                    hairColorMaterials.Reinitialize(hairstyleMaterial, PoolGrowthMethod.Incremental, 1, 1, 2048);
                }
                if (hairstyleLayoutTransform != null)
                {
                    hairstylePool = hairstyleLayoutTransform.gameObject.AddOrGetComponent<PrefabPool>();
                    hairstylePool.Reinitialize(hairstylePrototype, PoolGrowthMethod.Incremental, 1, 1, 2048); 
                    hairstylePool.SetContainerTransform(hairstyleLayoutTransform, true, false, false);
                    hairstylePool.forceParentOnClaim = true;
                    hairstylePool.worldPositionStaysWhenParented = false;
                    hairstylePrototype.SetActive(false);

                    hairstyleMaterial = Instantiate(hairstyleMaterial);
                }

                panelRoot = (RectTransform)hairCustomizationWindow.FindDeepChildLiberal("baseColors");
                if (panelRoot != null)
                {
                    if (GetColorPickableGraphic(id_hairPrimaryColor) == null)
                    {
                        var baseRT = panelRoot.FindDeepChildLiberal("primary");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_hairPrimaryColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                    if (GetColorPickableGraphic(id_hairSecondaryColor) == null)
                    {
                        var baseRT = panelRoot.FindDeepChildLiberal("secondary");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_hairSecondaryColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                }

                panelRoot = (RectTransform)hairCustomizationWindow.FindDeepChildLiberal("lobeColors");
                if (panelRoot != null)
                {
                    if (GetColorPickableGraphic(id_hairPrimarySpecTint) == null)
                    {
                        var baseRT = panelRoot.FindDeepChildLiberal("primary");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_hairPrimarySpecTint, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                    if (GetColorPickableGraphic(id_hairSecondarySpecTint) == null)
                    {
                        var baseRT = panelRoot.FindDeepChildLiberal("secondary");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_hairSecondarySpecTint, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                }

                panelRoot = (RectTransform)hairCustomizationWindow.FindDeepChildLiberal("specColor"); 
                if (panelRoot != null)
                {
                    if (GetColorPickableGraphic(id_hairSpecColor) == null)
                    {
                        var baseRT = panelRoot.FindDeepChildLiberal("value");
                        if (baseRT != null)
                        {
                            var graphic = baseRT.GetComponent<Graphic>();
                            var button = baseRT.GetComponent<Button>();
                            if (graphic != null && button != null)
                            {
                                colorPickableGraphics.Add(new ColorPickableGraphic() { id = id_hairSpecColor, colorPropertyName = graphic_hdrColorPropertyName, graphic = graphic, button = button, instantiateMaterial = true, colorPickerID = ColorPickerID.rightHDR, ignoreAlpha = true });
                            }
                        }
                    }
                }
            }
            #endregion

            foreach (var graphic in colorPickableGraphics)
            {
                if (graphic.graphic == null) continue;

                var mat = graphic.material == null ? (graphic.graphic.material == null ? graphic.graphic.defaultMaterial : graphic.graphic.material) : graphic.material;
                if (mat != null && graphic.instantiateMaterial)
                {
                    mat = graphic.material = Instantiate(mat);
                    graphic.graphic.material = graphic.material;
                }

                var graphic_ = graphic;
                if (mat != null && graphic.button != null)
                {
                    var canvas = graphic.graphic.gameObject.GetComponentInParent<Canvas>(true);
                    if (graphic.button.onClick == null) graphic.button.onClick = new Button.ButtonClickedEvent(); 
                    graphic.button.onClick.RemoveAllListeners();
                    graphic.button.onClick.AddListener(() =>
                    {
                        OpenColorPicker(graphic_.colorPickerID, canvas.transform.TransformPoint(canvas.ScreenToCanvasSpace(CursorProxy.ScreenPosition)), (color) =>
                        {
                            graphic_.storedColor = color;

                            if (mat != null)
                            {
                                mat.SetColor(graphic.colorPropertyName, graphic_.ignoreAlpha ? color.Opaque() : color); 
                                if (graphic.graphic != null)
                                {
                                    graphic.graphic.enabled = false;
                                    graphic.graphic.enabled = true;
                                }
                            }

                            graphic_.onColorChange?.Invoke(color); 

                        }, graphic_.storedColor);
                    });
                }
            }

            SetMainTab(MainTab.Identity);
        }

        protected void OnDestroy()
        {
            if (colorPickableGraphics != null)
            {
                foreach(var graphic in colorPickableGraphics)
                {
                    if (graphic == null) continue;
                    if (graphic.instantiateMaterial && graphic.material != null)
                    {
                        Destroy(graphic.material);
                    }
                }
            }

            if (irisStyleMaterial != null)
            {
#if UNITY_EDITOR
                if (!UnityEditor.AssetDatabase.IsNativeAsset(irisStyleMaterial))
                {
#endif
                    Destroy(irisStyleMaterial);
#if UNITY_EDITOR
                }
#endif
            }
            if (scleraStyleColorMaskMaterial != null)
            {
#if UNITY_EDITOR
                if (!UnityEditor.AssetDatabase.IsNativeAsset(scleraStyleColorMaskMaterial))
                {
#endif
                    Destroy(scleraStyleColorMaskMaterial);
#if UNITY_EDITOR
                }
#endif
            }
            if (hairstyleMaterial != null)
            {
#if UNITY_EDITOR
                if (!UnityEditor.AssetDatabase.IsNativeAsset(hairstyleMaterial))
                {
#endif
                    Destroy(hairstyleMaterial);
#if UNITY_EDITOR
                }
#endif
            }

            if (irisColorMaterials != null)
            {
                irisColorMaterials.Dispose();
                irisColorMaterials = null;
            }
            if (scleraColorMaterials != null)
            {
                scleraColorMaterials.Dispose();
                scleraColorMaterials = null;
            }


            if (skinPresetMaterials != null)
            {
                skinPresetMaterials.Dispose();
                skinPresetMaterials = null;
            }

            if (makeupStyleMaterials != null)
            {
                makeupStyleMaterials.Dispose();
                makeupStyleMaterials = null;
            }


            if (hairColorMaterials != null)
            {
                hairColorMaterials.Dispose();
                hairColorMaterials = null;
            }
        }

        private float CameraHeightOffset => (character.CharacterHeight == null ? 0f : character.CharacterHeight.StandingHeightContribution);

        public void Update()
        {
            var hoveredObj = CursorProxy.FirstObjectUnderCursor;
            if (hoveredObj == null)
            {
                camera_zoom = Mathf.Clamp01(camera_zoom - InputProxy.Scroll * 0.2f);

                if (InputProxy.FreeLookSecondaryAction)
                {
                    float inputX = InputProxy.MainRightJoystickHorizontal + InputProxy.CursorAxisX;
                    float inputY = InputProxy.MainRightJoystickVertical + InputProxy.CursorAxisY;

                    if (Mathf.Abs(inputX) > Mathf.Abs(inputY))
                    {
                        inputY = inputY * Mathf.Max(0f, 1f - Mathf.Abs(inputX));
                    } 
                    else
                    {
                        inputX = inputX * Mathf.Max(0f, 1f - Mathf.Abs(inputY)); 
                    }

                    if (Mathf.Abs(inputX) > 0.01f) camera_angle = camera_angle + inputX * 2.5f; 
                    if (Mathf.Abs(inputY) > 0.01f) camera_focusHeight = Mathf.Clamp01(camera_focusHeight - inputY * 0.015f); 
                }
            }
        }
        public void LateUpdate()
        { 
            Vector3 cameraFocusPos = camera_focusFloorPosition + Vector3.LerpUnclamped(Vector3.up * camera_focusHeightRange.x, Vector3.up * (camera_focusHeightRange.y + CameraHeightOffset), camera_focusHeight);
            Vector3 cameraPos = cameraFocusPos + Vector3.LerpUnclamped(Vector3.up * camera_heightRange.x, Vector3.up * camera_heightRange.y, camera_height) + Quaternion.Euler(0f, camera_angle, 0f) * camera_distanceAxis * Mathf.LerpUnclamped(camera_zoomRange.x, camera_zoomRange.y, camera_zoom);

            camera.transform.position = cameraPos;
            camera.transform.rotation = Quaternion.LookRotation(cameraFocusPos - cameraPos, Vector3.up);

            if (volume != null)
            {
                var profile = volume.profile;
                if (profile != null)
                {
                    profile.TryGet<DepthOfField>(out var DOF);
                    DOF.gaussianStart.value = Mathf.LerpUnclamped(dofStartDistanceNear, dofStartDistanceFar, camera_zoom);
                    DOF.gaussianEnd.value = Mathf.LerpUnclamped(dofEndDistanceNear, dofEndDistanceFar, camera_zoom);
                }
            }
        }

        public const string _proportionsCategoryName = "Proportions";
        public const string _movementCategoryName = "Movement";
        private static readonly List<string> _ignoredFeatureCategories = new List<string> { _proportionsCategoryName };
        protected static bool ShouldCreateFeatureCategoryTabFor(string categoryName)
        {
            return !_ignoredFeatureCategories.Contains(categoryName);
        }

        public const string _collapsedCategoryPrefix = "+ ";

        public void OpenLookTab()
        {
            if (lookPanel != null)
            {
                lookPanel.gameObject.SetActive(true);
            }

            UnsubscribeValueListeners();
            customizableFeatureGroups.Clear();
            customizableFeatureGroupsOrdered.Clear();

            customizableFeaturesGroupPool.ReleaseAllClaims();
            customizableFeaturesList.Clear();
            customizableFeaturesList.gameObject.SetActive(true);  

            if (character != null)
            {
                if (character.CharacterHeight != null)
                {
                    if (customizableFeaturesGroupPool.TryGetNewInstance(out GameObject uiInstance))
                    {
                        uiInstance.name = _proportionsCategoryName;
                        uiInstance.SetActive(true);
                        CustomEditorUtils.SetComponentTextByName(uiInstance, "Name", _proportionsCategoryName);
                        CustomEditorUtils.SetButtonOnClickAction(uiInstance, OpenProportionCustomization);

                        customizableFeatureGroupsOrdered.Add(_proportionsCategoryName);
                    }
                }

                if (character != null)
                {
                    if (customizableFeaturesGroupPool.TryGetNewInstance(out GameObject uiInstance))
                    {
                        uiInstance.name = _movementCategoryName;
                        uiInstance.SetActive(true);
                        CustomEditorUtils.SetComponentTextByName(uiInstance, "Name", _movementCategoryName);
                        CustomEditorUtils.SetButtonOnClickAction(uiInstance, OpenMovementCustomization);

                        customizableFeatureGroupsOrdered.Add(_movementCategoryName);
                    }
                }

                if (eyeEditModeGroup != null)
                {
                    if (customizableFeaturesGroupPool.TryGetNewInstance(out GameObject uiInstance))
                    {
                        uiInstance.name = _eyeCustomizationCategoryName;
                        uiInstance.SetActive(true);
                        CustomEditorUtils.SetComponentTextByName(uiInstance, "Name", _eyeCustomizationCategoryName);
                        CustomEditorUtils.SetButtonOnClickAction(uiInstance, OpenEyeCustomization);

                        customizableFeatureGroupsOrdered.Add(_eyeCustomizationCategoryName);
                    }
                }

                if (skinCustomizationWindow != null)
                {
                    if (customizableFeaturesGroupPool.TryGetNewInstance(out GameObject uiInstance))
                    {
                        uiInstance.name = _skinCustomizationCategoryName;
                        uiInstance.SetActive(true);
                        CustomEditorUtils.SetComponentTextByName(uiInstance, "Name", _skinCustomizationCategoryName);
                        CustomEditorUtils.SetButtonOnClickAction(uiInstance, OpenSkinCustomization);

                        customizableFeatureGroupsOrdered.Add(_skinCustomizationCategoryName);
                    }
                }

                if (makeupCustomizationWindow != null)
                {
                    if (customizableFeaturesGroupPool.TryGetNewInstance(out GameObject uiInstance))
                    {
                        uiInstance.name = _makeupCustomizationCategoryName;
                        uiInstance.SetActive(true);
                        CustomEditorUtils.SetComponentTextByName(uiInstance, "Name", _makeupCustomizationCategoryName);
                        CustomEditorUtils.SetButtonOnClickAction(uiInstance, OpenMakeupCustomization);

                        customizableFeatureGroupsOrdered.Add(_makeupCustomizationCategoryName);
                    }
                }

                if (sexCustomizationWindow != null)
                {
                    if (customizableFeaturesGroupPool.TryGetNewInstance(out GameObject uiInstance))
                    {
                        uiInstance.name = _sexCustomizationCategoryName;
                        uiInstance.SetActive(true);
                        CustomEditorUtils.SetComponentTextByName(uiInstance, "Name", _sexCustomizationCategoryName);
                        CustomEditorUtils.SetButtonOnClickAction(uiInstance, OpenSexCustomization);

                        customizableFeatureGroupsOrdered.Add(_sexCustomizationCategoryName);
                    }
                }

                if (hairCustomizationWindow != null)
                {
                    if (customizableFeaturesGroupPool.TryGetNewInstance(out GameObject uiInstance))
                    {
                        uiInstance.name = _hairCustomizationCategoryName;
                        uiInstance.SetActive(true);
                        CustomEditorUtils.SetComponentTextByName(uiInstance, "Name", _hairCustomizationCategoryName);
                        CustomEditorUtils.SetButtonOnClickAction(uiInstance, OpenHairCustomization);

                        customizableFeatureGroupsOrdered.Add(_hairCustomizationCategoryName);
                    }
                }

                if (character.customizableFeatures != null)
                {
                    for(int index = 0; index < character.customizableFeatures.Count; index++)
                    {
                        var feature = character.customizableFeatures[index];
                        
                        if (!customizableFeatureGroups.TryGetValue(feature.category, out var group))
                        {
                            group = new CustomizableFeatureGroup(feature.category);
                            customizableFeatureGroups[feature.category] = group;

                            if (ShouldCreateFeatureCategoryTabFor(feature.category))
                            {
                                if (customizableFeaturesGroupPool.TryGetNewInstance(out GameObject uiInstance))
                                {
                                    //int groupIndex = customizableFeatureGroupsOrdered.Count; // list will be sorted later so the index is inaccurate

                                    uiInstance.name = feature.category;
                                    uiInstance.SetActive(true);
                                    CustomEditorUtils.SetComponentTextByName(uiInstance, "Name", feature.category);
                                    CustomEditorUtils.SetButtonOnClickAction(uiInstance, () => SetActiveLookCustomizableFeatureGroup(feature.category/*groupIndex*/));
                                }
                            }

                            customizableFeatureGroupsOrdered.Add(feature.category);
                        }

                        group.customizableFeatures.Add(feature.id);
                    }

                    SetActiveLookCustomizableFeatureGroup(0);
                }
            }

            SortCustomizableFeatureGroups();
        }

        protected void OpenProportionCustomization()
        {
            CloseRightPanel();
            if (rightPanel != null)
            {
                rightPanel.SetActive(true);
            }

            UnsubscribeValueListeners();
            customizableFeaturesList.Clear();
            customizableFeaturesList.gameObject.SetActive(true);

            void CreateSlider(string categoryName, string memberName, float currentValue, UnityAction<float> listener)
            {
                var categoryNameRetracted = _collapsedCategoryPrefix + categoryName;

                var cat = customizableFeaturesList.FindCategory(categoryName);
                if (cat == null) cat = customizableFeaturesList.FindCategory(categoryNameRetracted);

                if (cat == null)
                {
                    cat = customizableFeaturesList.AddNewCategory(categoryName);
                    cat.SetDisplayName(categoryNameRetracted);
                    cat.OnExpand.AddListener(() =>
                    {
                        cat.SetDisplayName(categoryName);
                    });
                    cat.OnRetract.AddListener(() =>
                    {
                        cat.SetDisplayName(categoryNameRetracted);
                    });
                }

                var mem = customizableFeaturesList.AddNewListMember(memberName, cat, null);

                var slider = mem.gameObject.GetComponentInChildren<Slider>(true);
                slider.minValue = -1f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.SetValueWithoutNotify(currentValue);
                if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();

                var sliderListenerGroup = new SliderListenerGroup(); 
                sliderListeners.Add(sliderListenerGroup);

                sliderListenerGroup.slider = slider;
                sliderListenerGroup.sliderListener = listener;

                slider.onValueChanged.AddListener(listener);
            }

            CreateSlider("Base", "Height", character.CharacterBaseHeight, character.SetBaseHeight);
            CreateSlider("Neck", "Length", character.CharacterNeckLength, character.SetNeckLength);
            CreateSlider("Spine", "Length", character.CharacterSpineLength, character.SetSpineLength);
            CreateSlider("Shoulders", "Width", character.CharacterShoulderWidth, character.SetShoulderWidth);
            CreateSlider("Arms", "Upper Arm Length", character.CharacterUpperArmLength, character.SetUpperArmLength);
            CreateSlider("Arms", "Forearm Length", character.CharacterForearmLength, character.SetForearmLength);
            CreateSlider("Hips", "Width", character.CharacterHipWidth, character.SetHipWidth);
            CreateSlider("Legs", "Thigh Length", character.CharacterThighLength, character.SetThighLength);
            CreateSlider("Legs", "Calf Length", character.CharacterCalfLength, character.SetCalfLength); 

            SetActiveLookCustomizableFeatureGroup(_proportionsCategoryName, false, false); 
        }

        private delegate float FloatLookupDelegate();
        protected void OpenMovementCustomization()
        {
            CloseRightPanel();
            if (rightPanel != null)
            {
                rightPanel.SetActive(true);
            }

            UnsubscribeValueListeners();
            customizableFeaturesList.Clear();
            customizableFeaturesList.gameObject.SetActive(true);

            Slider CreateSlider(string categoryName, string memberName, float currentValue, UnityAction<float> listener)
            {
                var categoryNameRetracted = _collapsedCategoryPrefix + categoryName;

                var cat = customizableFeaturesList.FindCategory(categoryName);
                if (cat == null) cat = customizableFeaturesList.FindCategory(categoryNameRetracted);

                if (cat == null)
                {
                    cat = customizableFeaturesList.AddNewCategory(categoryName);
                    cat.SetDisplayName(categoryNameRetracted);
                    cat.OnExpand.AddListener(() =>
                    {
                        cat.SetDisplayName(categoryName);
                    });
                    cat.OnRetract.AddListener(() =>
                    {
                        cat.SetDisplayName(categoryNameRetracted);
                    });
                }

                var mem = customizableFeaturesList.AddNewListMember(memberName, cat, null);

                var slider = mem.gameObject.GetComponentInChildren<Slider>(true);
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.SetValueWithoutNotify(currentValue);
                if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();

                var sliderListenerGroup = new SliderListenerGroup();
                sliderListeners.Add(sliderListenerGroup);

                sliderListenerGroup.slider = slider;
                sliderListenerGroup.sliderListener = listener;

                slider.onValueChanged.AddListener(listener);

                return slider;
            }

            Slider auraSlider = null;
            Slider vibesSlider = null;
            Slider flairSlider = null;
            Slider leanSlider = null;

            void ResetValues()
            {
                auraSlider.SetValueWithoutNotify(character.IdleAura);
                vibesSlider.SetValueWithoutNotify(character.IdleVibes);
                flairSlider.SetValueWithoutNotify(character.IdleFlair);
                leanSlider.SetValueWithoutNotify(character.IdleLean);
            }

            auraSlider = CreateSlider("Idle", "Aura", character.IdleAura, (float val) =>
            {
                character.SetIdleAura(val);
                ResetValues();
            });
            vibesSlider = CreateSlider("Idle", "Vibes", character.IdleVibes, (float val) =>
            {
                character.SetIdleVibes(val);
                ResetValues();
            });
            flairSlider = CreateSlider("Idle", "Flair", character.IdleFlair, (float val) =>
            {
                character.SetIdleFlair(val);
                ResetValues();
            });
            leanSlider = CreateSlider("Idle", "Lean", character.IdleLean, (float val) =>
            {
                character.SetIdleLean(val);
                ResetValues();
            });

            SetActiveLookCustomizableFeatureGroup(_movementCategoryName, false, false);  
        }

        public void SetActiveLookCustomizableFeatureGroup(string groupName) 
        {
            CloseRightPanel();
            if (rightPanel != null)
            {
                rightPanel.SetActive(true);
            }
            if (customizableFeaturesPanel != null)
            {
                customizableFeaturesPanel.gameObject.SetActive(true);
            }

            SetActiveLookCustomizableFeatureGroup(groupName, true, true); 
        }
        public void SetActiveLookCustomizableFeatureGroup(string groupName, bool clearFeaturesList, bool unsubscribeValueListeners)
        {
            int index = customizableFeatureGroupsOrdered.IndexOf(groupName);
            if (index < 0) return;

            SetActiveLookCustomizableFeatureGroup(index, clearFeaturesList, unsubscribeValueListeners); 
        }
        protected void SetActiveLookCustomizableFeatureGroup(int index) => SetActiveLookCustomizableFeatureGroup(index, true, true);
        protected void SetActiveLookCustomizableFeatureGroup(int index, bool clearFeaturesList, bool unsubscribeValueListeners)
        {
            if (unsubscribeValueListeners) UnsubscribeValueListeners();
            if (clearFeaturesList) customizableFeaturesList.Clear();

            if (customizableFeatureGroups.TryGetValue(customizableFeatureGroupsOrdered[index], out var group))
            {
                foreach(var feature in group.customizableFeatures)
                {
                    var controller = character.GetFeatureController(feature);
                    if (controller == null) continue;

                    bool isCategory = false;
                    var categoryName = controller.feature.subCategory;
                    var categoryNameRetracted = _collapsedCategoryPrefix + categoryName;
                    if (string.IsNullOrEmpty(categoryName))
                    {
                        categoryName = controller.feature.displayName;
                        isCategory = true;
                    }

                    var cat = customizableFeaturesList.FindCategory(categoryName);
                    if (cat == null) cat = customizableFeaturesList.FindCategory(categoryNameRetracted);

                    if (cat == null)
                    {
                        cat = customizableFeaturesList.AddNewCategory(categoryName);
                        cat.SetDisplayName(categoryNameRetracted);
                        cat.OnExpand.AddListener(() =>
                        {
                            cat.SetDisplayName(categoryName);
                        });
                        cat.OnRetract.AddListener(() =>
                        {
                            cat.SetDisplayName(categoryNameRetracted);   
                        });
                    }

                    var mem = customizableFeaturesList.AddOrGetListMember(isCategory ? "Value" : controller.feature.displayName, cat, null);
                    
                    var slider = mem.gameObject.GetComponentInChildren<Slider>(true);
                    slider.minValue = 0f;
                    slider.maxValue = 1f;
                    slider.wholeNumbers = false;
                    slider.SetValueWithoutNotify(controller.Value); 
                    if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();

                    void SliderListener(float value)
                    {
                        controller.SetValueNoUpdate(value, true);
                    }

                    var sliderListenerGroup = new SliderListenerGroup();
                    sliderListeners.Add(sliderListenerGroup);

                    sliderListenerGroup.slider = slider;
                    sliderListenerGroup.sliderListener = SliderListener;

                    slider.onValueChanged.AddListener(SliderListener);

                    if (!string.IsNullOrWhiteSpace(controller.feature.valueGroup))
                    {
                        var valueGroup = character.GetValueGroup(controller.feature.valueGroup);
                        if (valueGroup != null)
                        {
                            void ValueGroupListener()
                            {
                                if (slider != null)
                                {
                                    slider.SetValueWithoutNotify(controller.Value);  
                                }
                            }

                            sliderListenerGroup.valueGroup = valueGroup;
                            sliderListenerGroup.valueGroupListener = ValueGroupListener;
                            valueGroup.OnValueChange.AddListener(ValueGroupListener); 
                        }
                    }
                }
            }
            
            SortCustomizableFeaturesList();
        }

    }
}