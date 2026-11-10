using Miniscript;
using Swole.Script;
using Swole;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Mathematics;

public class msCombineClassTest : MonoBehaviour
{

    [TextArea(20, 100)]
    public string source;

    private Interpreter interpreter;

    private bool done = false;

    private void Awake()
    {

        if (string.IsNullOrEmpty(source)) return;
        
        string header = "Float = {}" + Environment.NewLine + "Float.value = 0";

        source = header + Environment.NewLine + source;
        
        interpreter = new Interpreter();
        interpreter.hostData = new DefaultRuntimeHost() { identifier = nameof(msCombineClassTest) };
        interpreter.standardOutput = interpreter.implicitOutput = (s, ignored) => swole.Log(s);
        interpreter.errorOutput = (s, ignored) => swole.LogError(s);
        
        interpreter.Reset(source);
        interpreter.Compile();
        if (interpreter.vm == null) // Didn't finish compilation
        {
            swole.LogError($"[{nameof(msCombineClassTest)}] Failed to compile!");
            interpreter = null;
            return;
        } 
        ValMap _Test = new ValMap();
        _Test["value"] = new ValNumber(-1);
        interpreter.SetGlobalValue("_Test", _Test);
    }

    private void Update()
    {
        if (interpreter == null || done) return;
        interpreter.RunUntilDone(0.01f);

        var output = interpreter.GetGlobalValue("_Test");
        if (output is ValMap outputClass) Debug.Log($"OUTPUT -> {outputClass["value"].FloatValue()}");

        if (!interpreter.Running()) done = true;

    }

}
