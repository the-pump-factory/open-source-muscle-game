using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen
{

    [System.Serializable]
    public class QuadraticPathObject : BezierPathObject
    {

        public QuadraticBezierPath path;

        public override BezierPath GetPath()
        {

            return path;

        }

        public override BezierPath GetNewPath()
        {

            QuadraticBezierPath p = new QuadraticBezierPath();

            p.path = new Vector3[path.path.Length];

            path.path.CopyTo(p.path, 0);

            return p;

        }

    }

}
