using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen.rigging
{
    public class SkinnedDriverController : DriverController
    {

        public Transform root;

        new public SkinnedMeshRenderer renderer;

        public override void start()
        {

            DeserializeDrivers();

            if (drivers != null) foreach (DriverLogic.IDriver driver in drivers)
                {

                    if (driver == null) continue;

                    driver.Initialize(renderer, gameObject);

                    DriverLogic.IVariable[] vars = driver.GetVars();

                    foreach (DriverLogic.IVariable variable in vars)
                    {

                        if (variable is DriverLogic.TransformVariable) ((DriverLogic.TransformVariable)variable).SetTransformFromRoot(root);

                    }

                }

        }

    }

}
