using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.Serialization;

namespace nugen.rigging
{

    [System.Serializable]
    public class MultiScalarDriver : ScalarDriver
    {

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {

            info.AddValue("subDrivers", subDrivers, typeof(ScalarDriver[]));

            info.AddValue("expression", expression, typeof(DriverLogic.Expression));

            info.AddValue("variables", variables, typeof(DriverLogic.IVariable[]));

            info.AddValue("operations", operations, typeof(DriverLogic.Operation[]));

        }

        public MultiScalarDriver(SerializationInfo info, StreamingContext context) : base(new DriverLogic.Expression(), null, null)
        {

            subDrivers = (ScalarDriver[])info.GetValue("subDrivers", typeof(ScalarDriver[]));

            expression = (DriverLogic.Expression)info.GetValue("expression", typeof(DriverLogic.Expression));

            variables = (DriverLogic.IVariable[])info.GetValue("variables", typeof(DriverLogic.IVariable[]));

            operations = (DriverLogic.Operation[])info.GetValue("operations", typeof(DriverLogic.Operation[]));

        }

        public ScalarDriver[] subDrivers;

        public MultiScalarDriver(ScalarDriver[] subDrivers, DriverLogic.Expression expression, DriverLogic.IVariable[] variables, DriverLogic.Operation[] operations) : base(expression, variables, operations)
        {

            this.subDrivers = subDrivers;

        }

        public override void Initialize(params object[] input)
        {

            for (int a = 0; a < subDrivers.Length; a++) subDrivers[a]?.Initialize(input);

        }

        public override void Apply()
        {

            for (int a = 0; a < subDrivers.Length; a++) subDrivers[a]?.Apply();

        }

        public override void Dispose()
        {

            for (int a = 0; a < subDrivers.Length; a++) subDrivers[a]?.Dispose();

        }

        public override void Update(DriverJob.Constituent constituent)
        {

            for (int a = 0; a < subDrivers.Length; a++) subDrivers[a]?.Update(constituent);

        }

    }

}
