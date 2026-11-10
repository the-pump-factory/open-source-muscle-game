using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

using TMPro;

using Swole;
using Swole.UI;

public class workoutCataloguePrototype : MonoBehaviour
{

    public RectTransform layoutRoot;
    public UIGridLayout gridLayout;

    public GameObject listMemberPrototype;

    public workoutGameTest game;

    public RectTransform selectionHighlight;

    public int selectedIndex = -1;
    protected int lastSelectedIndex = -2;
    protected float selectionChangeCooldown;

    protected struct SelectableWorkout
    {
        public Button button;
        public int workoutIndex;
    }
    protected List<SelectableWorkout> selections = new List<SelectableWorkout>(); 

    protected void Awake()
    {
        if (gridLayout == null) gridLayout = layoutRoot.GetComponent<UIGridLayout>();

        listMemberPrototype.SetActive(false);
        selectionHighlight.gameObject.SetActive(false); 
    }

    protected void OnEnable()
    {
        lastSelectedIndex = -1;
    }

    public void CreateSelection(string workoutName, Sprite thumbnail, int workoutIndex)
    {
        var member = GameObject.Instantiate(listMemberPrototype);
        member.gameObject.SetActive(true);
        var memberRT = member.AddOrGetComponent<RectTransform>();

        memberRT.SetParent(layoutRoot, false);

        var thumbnailRoot = memberRT.FindDeepChildLiberal("thumbnail"); 
        if (thumbnailRoot != null)
        {
            var thumbnailImg = thumbnailRoot.GetComponentInChildren<Image>();
            if (thumbnailImg != null) 
            {
                if (thumbnail != null) 
                { 
                    thumbnailImg.sprite = thumbnail;
                }
                else
                {
                    thumbnailImg.sprite = null;
                    thumbnailImg.color = Color.grey;
                }

                thumbnailImg.FitImageInParent(); 
            }
        }

        var nameRoot = memberRT.FindDeepChildLiberal("name");
        if (nameRoot != null)
        {
            var nameText = nameRoot.GetComponentInChildren<TMP_Text>();
            if (nameText != null) nameText.SetText(workoutName);
        }

        var button = member.GetComponentInChildren<Button>();
        if (button != null)
        {
            if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => 
            { 
                game.SetTargetWorkout(workoutIndex);

                IEnumerator Wait()
                {
                    yield return null;
                    yield return null;
                    yield return null;

                    gameObject.SetActive(false);
                }

                CoroutineProxy.Start(Wait());
            });

            selections.Add(new SelectableWorkout() { button = button, workoutIndex = workoutIndex });
        }
    }

    public void Update()
    {
        if (InputProxy.CloseOrQuitKeyDown || InputProxy.MOBA_AbilityRDown)
        {
            IEnumerator Wait()
            {
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                gameObject.SetActive(false);
            }

            CoroutineProxy.Start(Wait()); 
            return;
        }

        if (Mathf.Abs(InputProxy.CursorAxisX) > 0.01f || Mathf.Abs(InputProxy.CursorAxisY) > 0.01f)
        {
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true;
        }

        if (selectionChangeCooldown <= 0)
        {
            var input = new Vector2(InputProxy.MainLeftJoystickHorizontal, InputProxy.MainLeftJoystickVertical);

            if (input.x > 0.4f)
            {
                selectedIndex = Mathf.Max(0, selectedIndex + 1);
                selectionChangeCooldown = 0.2f;
            }
            else if (input.x < -0.4f)
            {
                selectedIndex = Mathf.Min(selectedIndex - 1, selections.Count - 1);
                selectionChangeCooldown = 0.2f;
            }

            if (input.y > 0.4f)
            {
                selectedIndex = Mathf.Max(0, selectedIndex - gridLayout.columns);
                selectionChangeCooldown = 0.2f;
            }
            else if (input.y < -0.4f)
            {
                selectedIndex = Mathf.Min(selectedIndex + gridLayout.columns, selections.Count - 1);
                selectionChangeCooldown = 0.2f;
            }
        }
        else
        {
            selectionChangeCooldown -= Time.deltaTime;
        }

        if (selectedIndex >= 0 && selectedIndex < selections.Count /*&& InputSystemProxy.IsUsingGamepad*/)
        {
            if (!selectionHighlight.gameObject.activeSelf) selectionHighlight.gameObject.SetActive(true);

            if (selectedIndex != lastSelectedIndex)
            {
                lastSelectedIndex = selectedIndex;

                var selection = selections[selectedIndex];
                if (selection.button != null)
                {
                    selection.button.Select(); 

                    var selectionRT = selection.button.GetComponent<RectTransform>();
                    if (selectionHighlight.parent != selectionRT) selectionHighlight.SetParent(selectionRT, false);
                    selectionHighlight.SetAnchor(AnchorPresets.StretchAll, Vector2.zero);
                    selectionHighlight.sizeDelta = Vector2.zero;
                }
                else
                {
                    if (selectionHighlight.gameObject.activeSelf) selectionHighlight.gameObject.SetActive(false);
                }
            }
        } 
        else
        {
            if (selectionHighlight.gameObject.activeSelf) selectionHighlight.gameObject.SetActive(false);
        }

        if (InputProxy.JumpButtonDown || InputProxy.ConfirmKeyDown)
        {
            if (selectedIndex >= 0 && selectedIndex < selections.Count)
            { 
                game.SetTargetWorkout(selections[selectedIndex].workoutIndex);
                gameObject.SetActive(false); 
            }
        }
    }

}
