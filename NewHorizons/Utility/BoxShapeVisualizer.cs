using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Popcron;

namespace NewHorizons.Utility
{
    public class BoxShapeVisualizer : MonoBehaviour
    {
        BoxShape box;

        void Awake()
        {
            box = GetComponent<BoxShape>();
        }

        void OnRenderObject()
        {
            Popcron.Gizmos.Cube(transform.TransformPoint(box.center), transform.rotation, Vector3.Scale(box.size, transform.lossyScale));
        }
    }
}
