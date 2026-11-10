using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.Serialization;

namespace nugen.rigging
{

    [System.Serializable]
    public class BlendShapeDriver : ScalarDriver
    {

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {

            info.AddValue("blendShapeName", blendShapeName, typeof(string));

            info.AddValue("expression", expression, typeof(DriverLogic.Expression));

            info.AddValue("variables", variables, typeof(DriverLogic.IVariable[]));

            info.AddValue("operations", operations, typeof(DriverLogic.Operation[]));

        }

        public BlendShapeDriver(SerializationInfo info, StreamingContext context) : base(new DriverLogic.Expression(), null, null)
        {

            blendShapeName = info.GetString("blendShapeName");

            expression = (DriverLogic.Expression)info.GetValue("expression", typeof(DriverLogic.Expression));

            variables = (DriverLogic.IVariable[])info.GetValue("variables", typeof(DriverLogic.IVariable[]));

            operations = (DriverLogic.Operation[])info.GetValue("operations", typeof(DriverLogic.Operation[]));

        }

        public string blendShapeName;

        public BlendShapeDriver(string blendShapeName, DriverLogic.Expression expression, DriverLogic.IVariable[] variables, DriverLogic.Operation[] operations) : base(expression, variables, operations)
        {

            this.blendShapeName = blendShapeName;

        }

        [System.NonSerialized]
        protected int blendShapeIndex;

        [System.NonSerialized]
        protected SkinnedMeshRenderer renderer;

        public SkinnedMeshRenderer Renderer
        {

            get
            {

                return renderer;

            }

            set
            {

                renderer = value;

                CacheBlendShapeIndex();

            }

        }

        private int CacheBlendShapeIndex()
        {

            if (renderer != null) blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendShapeName);

            return blendShapeIndex;

        }

        public override void Initialize(params object[] input)
        {

            if (input == null || input.Length == 0) return;

            for(int a = 0; a < input.Length; a++)
            {

                if (input[a] is SkinnedMeshRenderer)
                {

                    Renderer = (SkinnedMeshRenderer)input[a];

                    break;

                }

            }

            if (Renderer == null)
            {

                for (int a = 0; a < input.Length; a++)
                {

                    if (input[a] is GameObject)
                    {

                        Renderer = ((GameObject)input[a]).GetComponent<SkinnedMeshRenderer>();

                        if (Renderer != null) break;

                    }

                }

            }

        }

        public override void Apply()
        {

            if (renderer == null) return;

            renderer.SetBlendShapeWeight(blendShapeIndex, Apply(renderer.GetBlendShapeWeight(blendShapeIndex)));

        }

        public override void Dispose()
        {

        }

    }

}
