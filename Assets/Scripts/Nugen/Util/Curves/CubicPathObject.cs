using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace nugen
{

    [System.Serializable]
    public class CubicPathObject : BezierPathObject
    {

        public CubicBezierPath path;

        public override BezierPath GetPath()
        {

            return path;

        }

        public override BezierPath GetNewPath()
        {

            CubicBezierPath p = new CubicBezierPath();

            p.path = new Vector3[path.path.Length];

            path.path.CopyTo(p.path, 0);

            return p;

        }

    }

}
