using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Runtime.Serialization;

using Unity.Collections;
using Unity.Mathematics;

namespace nugen.rigging
{

    public static class DriverLogic
    {

        public interface IDriver : IDisposable, ISerializable
        {

            public void Initialize(params object[] input);

            public bool HasUpdated(bool reset = true);

            public void Apply();

            public void Queue(NativeList<float> variables, NativeList<Operation> operations, NativeList<Expression> expressionQueue, NativeList<DriverJob.Constituent> constituents);

            public void Update(DriverJob.Constituent constituent);

            public IVariable[] GetVars();

        }

        public enum Operations
        {

            Print, Zero, Result, Var, Store, Execute, ExecuteSequential, Add, Subtract, Multiply, Divide, GreaterThan, LessThan, Equal, NotEqual, GreaterThanOrEqual, LessThanOrEqual, If, IfElse, Min, Max, Clamp, Pow, Abs, Sqrt

        }

        [Serializable]
        public struct Operation
        {

            public Operations opType;

            public int indexA, indexB, indexC;

            public Operation(Operations opType, int indexA = 0, int indexB = 0, int indexC = 0)
            {

                this.opType = opType;

                this.indexA = indexA;
                this.indexB = indexB;
                this.indexC = indexC;

            }

        }

        [Serializable]
        public struct Expression
        {

            public int start;

            public float seedValue;

            /// <summary>
            /// Added for potential multi-expression functionality
            /// </summary>
            public int operationsIndex;

            public int operationsCount;

            public Expression(int opStart, float seedValue, int operationsIndex, int operationsCount)
            {

                this.start = opStart;

                this.seedValue = seedValue;

                this.operationsIndex = operationsIndex;

                this.operationsCount = operationsCount;

            }

            public float Evaluate(int varOffset, int opOffset, NativeList<float> variables, NativeList<Operation> operations)
            {

                return DriverLogic.Evaluate(varOffset, opOffset, variables, operations, this);

            }

        }

        public static string AsString(this Expression expression, List<Operation> operations, List<IVariable> variables, int opOffset = 0, int varOffset = 0)
        {

            string EvalVar(int index)
            {

                if (index == 0) return "def";

                IVariable variable = variables[index + varOffset];

                if (variable is ConstantFloat constf)
                {

                    return constf.GetName() + "(" + constf.GetValue() + ")" + "[" + index + "]";

                }
                else if (variable is InternalFloat)
                {

                    return variable.GetName() + "[" + index + "]";

                }
                else if (variable is DynamicDriverFloat)
                {

                    return variable.GetName() + "[" + index + "]";

                }
                else if (variable is ExpressionOutputFloat)
                {

                    return variable.GetName() + "[" + index + "]";

                }
                else return variable.GetName() + "[" + index + "]";

            }

            string EvalOp(int index)
            {

                Operation op = operations[opOffset + index];

                switch (op.opType)
                {

                    default:
                        return op.opType.ToString() + "(" + EvalVar(op.indexA) + ", " + EvalVar(op.indexB) + ", " + EvalVar(op.indexC) + ")[" + index + "]";

                    case Operations.Execute:
                        return "E_" + index + "{\n" + EvalOp(op.indexA) + "\n}";

                    case Operations.ExecuteSequential:
                        return "S_" + index + "{\n" + EvalOp(op.indexA) + " => " + EvalOp(op.indexB) + "\n}";

                    case Operations.IfElse:
                        return "IF (" + EvalOp(op.indexA) + ") THEN {{ " + EvalOp(op.indexB) + " }} ELSE {{ " + EvalOp(op.indexC) + " }}";

                }

            }

            return EvalOp(expression.start);

        }

        public static string AsString(this Expression expression, Operation[] operations, IVariable[] variables, int opOffset = 0, int varOffset = 0)
        {

            string EvalVar(int index)
            {

                if (index == 0) return "def";

                IVariable variable = variables[index + varOffset];

                if (variable is ConstantFloat constf)
                {

                    return constf.GetName() + "(" + +constf.GetValue() + ")" + "[" + index + "]";

                }
                else if (variable is InternalFloat)
                {

                    return variable.GetName() + "[" + index + "]";

                }
                else if (variable is DynamicDriverFloat)
                {

                    return variable.GetName() + "[" + index + "]";

                }
                else if (variable is ExpressionOutputFloat)
                {

                    return variable.GetName() + "[" + index + "]";

                }
                else return variable.GetName() + "[" + index + "]";

            }

            string EvalOp(int index)
            {

                Operation op = operations[opOffset + index];

                switch (op.opType)
                {

                    default:
                        return op.opType.ToString() + "(" + EvalVar(op.indexA) + ", " + EvalVar(op.indexB) + ", " + EvalVar(op.indexC) + ")[" + index + "]";

                    case Operations.Execute:
                        return "E_" + index + "{\n" + EvalOp(op.indexA) + "\n}";

                    case Operations.ExecuteSequential:
                        return "S_" + index + "{\n" + EvalOp(op.indexA) + " => " + EvalOp(op.indexB) + "\n}";

                }

            }

            return EvalOp(expression.start);

        }

        public static float Evaluate(int varOffset, int opOffset, NativeList<float> variables, NativeList<Operation> operations, Expression expression)
        {

            Operation Op(int opIndex)
            {

                opIndex = (opIndex + opOffset) + expression.operationsIndex;

                if (opIndex < 0 || opIndex >= operations.Length) return new Operation(Operations.Result);

                return operations[opIndex];

            }

            float Execute(Operation op, float input)
            {

                float start = input;

                float Var(int varIndex)
                {

                    varIndex = varIndex + varOffset;

                    if (varIndex < 0 || varIndex >= variables.Length) return input;

                    return variables[varIndex];

                }

                float Store(int varIndex)
                {

                    varIndex = varIndex + varOffset;

                    if (varIndex < 0 || varIndex >= variables.Length) return input;

                    variables[varIndex] = input;

                    return input;

                }

                switch (op.opType)
                {

                    case Operations.Print:
#if UNITY_EDITOR
                        float val1 = (op.indexA < 0 ? input : Var(op.indexA));
                        float val2 = (op.indexB < 0 ? 0 : Var(op.indexB));
                        float val3 = (op.indexC < 0 ? 0 : Var(op.indexC));
                        Debug.Log($"{op.indexA} : {val1} {val2} {val3}");  
#endif
                        return input;

                    case Operations.Zero:
                        return 0;

                    case Operations.Result:
                        return input;

                    case Operations.Var:
                        return Var(op.indexA);

                    case Operations.Store:
                        return Store(op.indexA);

                    case Operations.Execute:
                        return Execute(Op(op.indexA), input);

                    case Operations.ExecuteSequential:
                        return Execute(Op(op.indexB), Execute(Op(op.indexA), input));

                    case Operations.Add:
                        //Debug.Log(op.opType.ToString() + ": " + Var(op.indexA) + " + " + Var(op.indexB) + " => " + (Var(op.indexA) + Var(op.indexB)));
                        return Var(op.indexA) + Var(op.indexB);

                    case Operations.Subtract:
                        //Debug.Log(op.opType.ToString() + ": " + Var(op.indexA) + " - " + Var(op.indexB) + " => " + (Var(op.indexA) - Var(op.indexB)));
                        return Var(op.indexA) - Var(op.indexB);

                    case Operations.Multiply:
                        //Debug.Log(op.opType.ToString() + ": " + Var(op.indexA) + " * " + Var(op.indexB) + " => " + (Var(op.indexA) * Var(op.indexB)));
                        return Var(op.indexA) * Var(op.indexB);

                    case Operations.Divide:
                        //Debug.Log(op.opType.ToString() + ": " + Var(op.indexA) + " / " + Var(op.indexB) + " => " + (Var(op.indexA) / Var(op.indexB)));
                        float div = Var(op.indexB);
                        return Var(op.indexA) / math.select(div, 1, div == 0);
                        //return Var(op.indexA) / Var(op.indexB);

                    case Operations.GreaterThan:
                        return Var(op.indexA) > Var(op.indexB) ? 1 : 0;

                    case Operations.LessThan:
                        return Var(op.indexA) < Var(op.indexB) ? 1 : 0;

                    case Operations.Equal:
                        return Var(op.indexA) == Var(op.indexB) ? 1 : 0;

                    case Operations.NotEqual:
                        return Var(op.indexA) != Var(op.indexB) ? 1 : 0;

                    case Operations.GreaterThanOrEqual:
                        return Var(op.indexA) >= Var(op.indexB) ? 1 : 0;

                    case Operations.LessThanOrEqual:
                        return Var(op.indexA) <= Var(op.indexB) ? 1 : 0;

                    case Operations.If:
                        return Execute(Op(op.indexA), input) >= 1 ? Execute(Op(op.indexB), input) : input;

                    case Operations.IfElse:
                        return Execute(Op(op.indexA), input) >= 1 ? Execute(Op(op.indexB), input) : Execute(Op(op.indexC), input);

                    case Operations.Min:
                        //Debug.Log(op.opType.ToString() + ": " + Var(op.indexA) + ", " + Var(op.indexB) + " => " + (math.min(Var(op.indexA), Var(op.indexB))));
                        return math.min(Var(op.indexA), Var(op.indexB));

                    case Operations.Max:
                        //Debug.Log(op.opType.ToString() + ": " + Var(op.indexA) + ", " + Var(op.indexB) + " => " + (math.max(Var(op.indexA), Var(op.indexB))));
                        return math.max(Var(op.indexA), Var(op.indexB));

                    case Operations.Clamp:
                        //Debug.Log(op.opType.ToString() + ": " + Var(op.indexA) + " => " + (math.clamp(Var(op.indexA), 0, 1)));
                        return math.clamp(Var(op.indexA), 0, 1);

                    case Operations.Pow:
                        return math.pow(Var(op.indexA), Var(op.indexB));

                    case Operations.Abs:
                        //Debug.Log(op.opType.ToString() + ": " + Var(op.indexA) + " => " + (math.abs(Var(op.indexA))));
                        return math.abs(Var(op.indexA));

                    case Operations.Sqrt:
                        return math.sqrt(Var(op.indexA));

                }

                return input;

            }

            return Execute(Op(expression.start), expression.seedValue);

        }

        public interface IVariable
        {

            public string GetName();

            public void UpdateValue();

            public float GetValue();

            public float GetUpdatedValue();

            public bool HasUpdated(bool reset = true);

        }

        [Serializable]
        public struct ConstantFloat : IVariable
        {

            private string name;

            private float value;

            public ConstantFloat(string name, float value)
            {

                this.name = name;

                this.value = value;

            }

            public string GetName()
            {

                return name;

            }

            public float GetUpdatedValue()
            {

                return GetValue();

            }

            public float GetValue()
            {

                return value;

            }

            public void UpdateValue() { }

            public bool HasUpdated(bool reset = true) { return false; }

        }

        [Serializable]
        public class DynamicFloat : IVariable
        {

            private string name;

            private float value;

            [System.NonSerialized]
            private float prevValue;

            public DynamicFloat(string name)
            {

                this.name = name;

            }

            public string GetName()
            {

                return name;

            }

            public float GetUpdatedValue()
            {

                return GetValue();

            }

            public float GetValue()
            {

                return value;

            }

            public void UpdateValue() { }

            public void SetValue(float value)
            {

                this.value = value;

            }

            public bool HasUpdated(bool reset = true) {

                bool updated = prevValue != value;

                if (reset) prevValue = value;

                return updated; 
            
            }

        }

        [Serializable]
        public class InternalFloat : DynamicFloat
        {

            public InternalFloat(string name) : base(name)
            {

            }

        }

        [Serializable]
        public class DynamicDriverFloat : DynamicFloat
        {

            public DynamicDriverFloat(string name) : base(name)
            {

            }

        }

        [Serializable]
        public class ExpressionOutputFloat : DynamicFloat
        {

            public ExpressionOutputFloat(string name) : base(name)
            {

            }

        }

        [Serializable]
        public abstract class TransformVariable : IVariable
        {

            private string name;

            public string GetName()
            {

                return name;

            }

            public TransformVariable(string name)
            {

                this.name = name;

            }

            public string[] path;

            [NonSerialized]
            private Transform transform;

            public Transform Transform
            {

                get
                {

                    return transform;

                }

            }

            public abstract void Initialize(bool useCache = true);

            public bool SetTransformFromRoot(Transform root, bool useCache = true)
            {

                if (path == null) return false;

                Transform t = root;

                for (int a = 0; a < path.Length; a++) 
                {

                    string name = path[a];

                    bool found = false;

                    for(int b = 0; b < t.childCount; b++)
                    {

                        Transform child = t.GetChild(b);

                        if (child.name == name)
                        {

                            t = child;

                            found = true;

                            break;

                        }

                    }

                    if (!found) return false;

                }

                if (t.name == path[path.Length - 1])
                {

                    transform = t;

                    Initialize(useCache);

                    return true;

                }

                return false;

            }

            public void SetTransform(Transform transform, Transform root, bool useCache = true)
            {

                this.transform = transform;

                List<string> path_ = new List<string>();

                Transform t = transform;

                if (t != root) path_.Add(t.name);

                while(t != root && t.parent != null && t.parent != root)
                {

                    t = t.parent;

                    path_.Insert(0, t.name);

                }

                path = path_.ToArray();

                Initialize(useCache);

            }

            public float GetUpdatedValue()
            {

                UpdateValue();

                return GetValue();

            }

            public abstract float GetValue();
            public abstract void UpdateValue();

            public abstract bool HasUpdated(bool reset = true);

        }

        [Serializable]
        public class LocalPositionX : TransformVariable
        {

            public LocalPositionX(string name) : base(name) { }

            [NonSerialized]
            private float value, prevValue;

            public override float GetValue() { return value;  }

            public override void UpdateValue() { Transform t = Transform; if (t != null) value = t.localPosition.x; }

            public override bool HasUpdated(bool reset = true)
            {

                bool updated = prevValue != value;

                if (reset) prevValue = value;

                return updated;

            }

            public override void Initialize(bool useCache = true) { }

        }

        [Serializable]
        public class LocalPositionY : TransformVariable
        {

            public LocalPositionY(string name) : base(name) { }

            [NonSerialized]
            private float value, prevValue;

            public override float GetValue() { return value; }

            public override void UpdateValue() { Transform t = Transform; if (t != null) value = t.localPosition.y; }

            public override bool HasUpdated(bool reset = true)
            {

                bool updated = prevValue != value;

                if (reset) prevValue = value;

                return updated;

            }

            public override void Initialize(bool useCache = true) { }

        }

        [Serializable]
        public class LocalPositionZ : TransformVariable
        {

            public LocalPositionZ(string name) : base(name) { }

            [NonSerialized]
            private float value, prevValue;

            public override float GetValue() { return value; }

            public override void UpdateValue() { Transform t = Transform; if (t != null) value = t.localPosition.z; }

            public override bool HasUpdated(bool reset = true)
            {

                bool updated = prevValue != value;

                if (reset) prevValue = value;

                return updated;

            }

            public override void Initialize(bool useCache = true) { }

        }

        [Serializable]
        public abstract class LocalRotationVariable : TransformVariable
        {

            protected static List<LocalRotationVariable> cache = new List<LocalRotationVariable>();

            public static int GetCacheIndex(LocalRotationVariable lrv)
            {

                if (cache == null) cache = new List<LocalRotationVariable>();

                for (int a = 0; a < cache.Count; a++)
                    if (cache[a].Transform == lrv.Transform &&
                        cache[a].axisX == lrv.axisX &&
                        cache[a].axisY == lrv.axisY &&
                        cache[a].axisZ == lrv.axisZ &&
                        cache[a].forwardX == lrv.forwardX &&
                        cache[a].forwardY == lrv.forwardY &&
                        cache[a].forwardZ == lrv.forwardZ) return a + 1;

                cache.Add(lrv);

                lrv.isCacheRoot = true;

                return cache.Count;

            }

            protected bool nonZeroStart;

            protected float forwardX, forwardY, forwardZ, axisX, axisY, axisZ;

            [NonSerialized]
            protected float value, prevValue, rawRot;

            [NonSerialized]
            protected Vector3 startVec, forward, axis;

            [NonSerialized]
            protected int cacheIndex, lastFrameUpdate;

            public void ResetFrame()
            {

                lastFrameUpdate = -1;

            }

            [NonSerialized]
            protected bool isCacheRoot;

            public LocalRotationVariable(string name, Vector3 forward, Vector3 axis, bool nonZeroStart = false) : base(name) 
            { 
                
                this.nonZeroStart = nonZeroStart;

                forward = forward.normalized;
                axis = axis.normalized;

                forwardX = forward.x;
                forwardY = forward.y;
                forwardZ = forward.z;

                axisX = axis.x;
                axisY = axis.y;
                axisZ = axis.z;
            
            }

            public override void Initialize(bool useCache = true)
            {

                Transform t = Transform;

                lastFrameUpdate = -1;

                cacheIndex = useCache ? GetCacheIndex(this) : -1;

                forward = new Vector3(forwardX, forwardY, forwardZ);
                axis = new Vector3(axisX, axisY, axisZ);

                if (t != null)
                {

                    axis = t.localRotation * axis;

                    startVec = t.localRotation * forward;

                    rawRot = Maths.AngleOffsetAroundAxis(startVec, startVec, axis);

                    value = nonZeroStart ? rawRot * Mathf.Deg2Rad : 0;

                }

            }

            public override float GetValue() { return value; }

            public override void UpdateValue()
            {

                if (cacheIndex <= 0 || isCacheRoot)
                {

                    if (lastFrameUpdate == Time.frameCount) return;

                    Transform t = Transform;

                    if (t != null)
                    {

                        Vector3 newVec = t.localRotation * forward;

                        float newRaw = Maths.AngleOffsetAroundAxis(newVec, startVec, axis);

                        float delta = Maths.GetMinDeltaAngle(rawRot, newRaw);

                        rawRot = newRaw;

                        value = value + delta * Mathf.Deg2Rad;

                    }

                    lastFrameUpdate = Time.frameCount;

                    return;

                }

                LocalRotationVariable root = cache[cacheIndex - 1];

                if (lastFrameUpdate < -1)
                {

                    root.ForceUpdateValue();

                    lastFrameUpdate = Time.frameCount;

                } 
                else
                {

                    root.UpdateValue();

                }

                value = root.GetValue();

            }

            public void ForceUpdateValue()
            {

                lastFrameUpdate = -2;

                UpdateValue();

            }

            public override bool HasUpdated(bool reset = true)
            {

                bool updated = prevValue != value;

                if (reset) prevValue = value;

                return updated;

            }

        }

        [Serializable]
        public class LocalRotationX : LocalRotationVariable
        {

            public LocalRotationX(string name, bool nonZeroStart = false) : base(name, Vector3.forward, Vector3.right, nonZeroStart) { }

        }

        [Serializable]
        public class LocalRotationY : LocalRotationVariable
        {

            public LocalRotationY(string name, bool nonZeroStart = false) : base(name, Vector3.forward, Vector3.up, nonZeroStart) { }

        }

        [Serializable]
        public class LocalRotationZ : LocalRotationVariable
        {

            public LocalRotationZ(string name, bool nonZeroStart = false) : base(name, Vector3.right, Vector3.forward, nonZeroStart) { }

        }

        [Serializable]
        public class InvertedLocalRotationX : LocalRotationVariable
        {
            public InvertedLocalRotationX(string name, bool nonZeroStart = false) : base(name, Vector3.forward, Vector3.left, nonZeroStart) { }

        }

        [Serializable]
        public class InvertedLocalRotationY : LocalRotationVariable
        {

            public InvertedLocalRotationY(string name, bool nonZeroStart = false) : base(name, Vector3.forward, Vector3.down, nonZeroStart) { }

        }

        [Serializable] 
        public class InvertedLocalRotationZ : LocalRotationVariable
        {

            public InvertedLocalRotationZ(string name, bool nonZeroStart = false) : base(name, Vector3.right, Vector3.back, nonZeroStart) { }

        }

    }

}
