using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NewHorizons.Utility.DebugUtilities
{
    class DebugArrow : MonoBehaviour
    {
        public Transform target;

        public static void CreateArrow(GameObject parent)
        {
            var arrowGO = new GameObject("ArrowGO");
            arrowGO.AddComponent<DebugArrow>();

            arrowGO.transform.parent = parent.transform;
            arrowGO.transform.localPosition = new Vector3(0, 0, 1f);
        }

        void Start()
        {
            // make the mesh in code so we don't need an assetbundle or anything
            /*            G
             *            /\
             *           /  \
             *         E C||D F
             *            ||
             *           A  B
             */

            Vector3[] topVerts = new Vector3[]
            {
                new Vector3(-0.1f, 0.1f, -0.5f), // A
                new Vector3( 0.1f, 0.1f, -0.5f), // B
                new Vector3(-0.1f, 0.1f,    0f), // C
                new Vector3( 0.1f, 0.1f,    0f), // D

                new Vector3(-0.5f, 0.1f,    0f), // E
                new Vector3( 0.5f, 0.1f,    0f), // F
                new Vector3(   0f, 0.1f,  0.5f), // G
            };
            Vector3[] bottomVerts = topVerts.Select(vert => new Vector3(vert.x, -vert.y, vert.z)).ToArray();
            Vector3[] sideVerts = topVerts.Concat(bottomVerts).ToArray();

            // note: A' is the bottom version of A
            var A = 0;
            var B = 1;
            var C = 2;
            var D = 3;
            var E = 4;
            var F = 5;
            var G = 6;

            int prime = topVerts.Length;
            int[] topTris = new int[]
            {
                A, C, B, // rectangle bit
                B, C, D,
                
                F, E, G, // pointy bit
            };

            int[] bottomTris = 
            {
                A+prime, B+prime, C+prime,  // rectangle bit
                B+prime, D+prime, C+prime, 
                 
                F+prime, G+prime, E+prime,  // pointy bit
            };

            
            /*            G
             *            /\
             *           /  \
             *         E C||D F
             *            ||
             *           A  B
             *    
             *    Right side view
             *     B        D F   G         
             *     +---------+---+    
             *     |    1    | 2 |     
             *     +---------+---+     
             *     B'      D' F'  G'
             *     
             *   Left Side view
             *    G  E C         A
             *    +---+----------+
             *    | 3 |     4    |
             *    +---+----------+
             *   G' E' C'        A'
             *   
             *  Back view
             *   E  C   D  F
             *    +-+---+-+
             *    |5| 6 |7|
             *    +-+---+-+
             *   E' C'  D' F'
             */
            int[] sideTris = new int[]
            {
                B+prime, B, D+prime, // 1
                D+prime, B, D,      

                F+prime, F, G+prime, // 2
                G+prime, F, G,       
                
                G+prime, G, E+prime, // 3
                E+prime, G, E,      
                
                C+prime, C, A+prime, // 4
                A+prime, C, A,      
                
                E+prime, E, C+prime,  // 5
                C+prime, E, C,       
                            
                C+prime, D+prime, C,  // 6
                D+prime, D,       C, 
                            
                D+prime, D, F+prime,  // 7
                F+prime, D, F,       
            }.Select(vIdx => vIdx + topVerts.Length+bottomVerts.Length).ToArray();

            Mesh m = new Mesh();
            m.name = "DebugArrow";
            m.vertices = topVerts.Concat(bottomVerts).Concat(sideVerts).ToArray();
            m.triangles = topTris.Concat(bottomTris).Concat(sideTris).ToArray();
            m.RecalculateNormals();
            m.RecalculateBounds();

            this.gameObject.AddComponent<MeshFilter>().mesh = m;
            this.gameObject.AddComponent<MeshRenderer>();
        }

        private void Update()
        {
            if (target == null) return;

            this.transform.LookAt(target);
        }
    }
}
