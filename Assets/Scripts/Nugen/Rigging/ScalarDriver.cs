using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;
using System.Runtime.Serialization;

namespace nugen.rigging
{

    [System.Serializable]
    public abstract class ScalarDriver : DriverLogic.IDriver
    {

        protected DriverLogic.Expression expression;

        public ScalarDriver(DriverLogic.Expression expression, DriverLogic.IVariable[] variables, DriverLogic.Operation[] operations)
        {

            this.expression = expression;

            this.variables = variables;

            this.operations = operations;

        }

        protected DriverLogic.IVariable[] variables;

        public DriverLogic.IVariable[] GetVars()
        {

            return variables;

        }

        public bool HasUpdated(bool reset = true)
        {

            bool updated = false; 

            foreach (DriverLogic.IVariable var_ in variables) 
            {

                var_.UpdateValue();

                if (var_.HasUpdated(reset)) updated = true;

            }

            return updated;

        }

        protected DriverLogic.Operation[] operations;

        public void Queue(NativeList<float> variables, NativeList<DriverLogic.Operation> operations, NativeList<DriverLogic.Expression> expressionQueue, NativeList<DriverJob.Constituent> constituents)
        {

            int expressionIndex = expressionQueue.Length;
            int variablesIndexOffset = variables.Length;
            int operationsIndexOffset = operations.Length;

            for (int a = 0; a < this.variables.Length; a++) variables.Add(this.variables[a].GetUpdatedValue());

            for (int a = 0; a < this.operations.Length; a++) operations.Add(this.operations[a]);

            expressionQueue.Add(expression);

            constituents.Add(new DriverJob.Constituent(expressionIndex, 1, variablesIndexOffset, operationsIndexOffset));

        }

        public virtual void Update(DriverJob.Constituent constituent)
        {

            output = constituent.outX;

        }

        [System.NonSerialized]
        protected float previousOutput;

        [System.NonSerialized]
        protected float output;

        public float GetPreviousOutput() { return previousOutput; }

        public float GetOutput() { return output; }

        public float Apply(float currentValue)
        {

            currentValue = (currentValue - previousOutput) + output;

            previousOutput = output;

            return currentValue;

        }

        public abstract void Initialize(params object[] input);
        public abstract void Apply();
        public abstract void Dispose();
        public abstract void GetObjectData(SerializationInfo info, StreamingContext context);

    }

}
