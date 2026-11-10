using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen
{

    public abstract class BezierPathObject : MonoBehaviour
    {

        public abstract BezierPath GetPath();

        public abstract BezierPath GetNewPath();

    }

}
