using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NewHorizons.Utility.DebugUtilities
{
    struct DebugRaycastData
    {
        public bool hit;
        public Vector3 pos;
        public Vector3 norm;
        public DebugRaycastPlane plane;

        public string bodyName;
        public string bodyPath;
        public GameObject hitBodyGameObject;
        public GameObject hitObject;
    }

    struct DebugRaycastPlane
    {
        public Vector3 origin;
        public Vector3 normal;
        public Vector3 u; // the "+x" direction on the plane
        public Vector3 v; // the "+y" direction on the plane
    }
}
