using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using TMPro;

using Miniscript;

namespace Swole.Script
{

    public class SwoleScriptEditor : CodeEditor, ICodeEditor
    {

        [Header("Settings")]
        public GameObject rootObject;
        public object RootObject
        {

            get => rootObject == null ? gameObject : rootObject;

            set
            {

                if (value is GameObject obj) rootObject = obj; else rootObject = null;

            }

        }

        [Header("UI")]
        public Text titleText;
        public TMP_Text titleTextTMP;

        public object TitleObject
        {

            get => titleTextTMP == null ? titleText : titleTextTMP;

            set
            {

                if (value is Text txt) titleText = txt; else if (value is TMP_Text txtTMP) titleTextTMP = txtTMP;

            }

        }

        public void SetTitle(string title)
        {
            if (titleText != null) titleText.text = title;
            if (titleTextTMP != null) titleTextTMP.SetText(title);
        }

        private static readonly char[] _newLineArray = Environment.NewLine.ToCharArray();
        private static readonly string _newLineStr = Environment.NewLine; 
        public string Code { get => source; set => source = string.IsNullOrWhiteSpace(value) ? string.Empty : (value.TrimEnd(_newLineArray) + _newLineStr); }   

        public int UndoHistoryCapacity { get => undoLimit; set => undoLimit = value; }

        public void Redo() => EditRedo();
        public void Undo() => EditUndo();

        public void RebuildLayout() => AdjustContentSize();

        private Dictionary<ICodeEditor.CodeEditCallback, UnityAction<string>> boundActions = new Dictionary<ICodeEditor.CodeEditCallback, UnityAction<string>>();

        [Serializable]
        public class OnEditorCloseEvent : UnityEvent<string> { }

        [SerializeField]
        private OnEditorCloseEvent onEditorClose = new OnEditorCloseEvent();

        [Serializable]
        public class OnCodeChangeEvent : UnityEvent<string> { }

        [SerializeField]
        private OnCodeChangeEvent onCodeChange = new OnCodeChangeEvent();

        /// <summary>
        /// Act as if the editor was closed but don't destroy it.
        /// </summary>
        public void SpoofClose()
        {

            onEditorClose?.Invoke(Code);
            onEditorClose?.RemoveAllListeners();

            onCodeChange?.RemoveAllListeners();

            SetTitle("");
            Code = "";

        }

        protected override void OnDestroy()
        {

            onEditorClose?.Invoke(Code);
            onEditorClose?.RemoveAllListeners();

            boundActions?.Clear();
            boundActions = null;

            base.OnDestroy();

        }

        public float autoNotifyInterval = 3;
        private float autoNotify;
        private string prevCode;

        protected virtual void LateUpdate()
        {

            autoNotify += Time.deltaTime;

            string code = Code;
            if (autoNotify >= autoNotifyInterval || prevCode != code)
            {
                autoNotify = 0;
                prevCode = code; 
                NotifyListeners();
            }

        }

        protected void NotifyListeners() => onCodeChange?.Invoke(Code);  

        public virtual void ListenForClosure(ICodeEditor.CodeEditCallback callback)
        {
            if (boundActions == null) return;
            if (!boundActions.TryGetValue(callback, out var action))
            {
                action = new UnityAction<string>(callback);
                boundActions[callback] = action;
            }
            onEditorClose?.AddListener(action);
        }

        public virtual bool StopListeningForClosure(ICodeEditor.CodeEditCallback callback)
        {
            if (boundActions == null) return false;
            if (!boundActions.TryGetValue(callback, out var action)) return false;
            onEditorClose?.RemoveListener(action);
            return true;
        }

        public virtual void ListenForChanges(ICodeEditor.CodeEditCallback callback)
        {
            if (boundActions == null) return;
            if (!boundActions.TryGetValue(callback, out var action))
            {
                action = new UnityAction<string>(callback);
                boundActions[callback] = action;
            }
            onCodeChange?.AddListener(action);
        }

        public virtual bool StopListeningForChanges(ICodeEditor.CodeEditCallback callback)
        {
            if (boundActions == null) return false;
            if (!boundActions.TryGetValue(callback, out var action)) return false;
            onCodeChange?.RemoveListener(action);
            return true;
        }

        public void ClearAllListeners()
        {
            ClearChangeListeners();
            ClearClosureListeners();
        }

        public void ClearChangeListeners()
        {
            onCodeChange?.RemoveAllListeners();
        }

        public void ClearClosureListeners()
        {
            onEditorClose?.RemoveAllListeners();
        }

    }

}