using NewHorizons.Components.Orbital;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NewHorizons.Utility.DebugUtilities
{
    [RequireComponent(typeof(OWRigidbody))]
    public class DebugRaycaster : MonoBehaviour
    {
        private OWRigidbody _rb;
        private GameObject _surfaceSphere;
        private GameObject _normalSphere1;
        private GameObject _normalSphere2;

        private GameObject _planeUpRightSphere;
        private GameObject _planeUpLeftSphere;
        private GameObject _planeDownRightSphere;
        private GameObject _planeDownLeftSphere;


        private void Awake()
        {
            _rb = this.GetRequiredComponent<OWRigidbody>();
        }
        

        private void Update()
        {
            if (!Main.Debug) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current[Key.P].wasReleasedThisFrame)
            {
                PrintRaycast();
            }
        }

       

        internal void PrintRaycast()
        {
            DebugRaycastData data = Raycast();

            if (!data.hit)
            {
                Logger.LogWarning("Debug Raycast Didn't Hit Anything! (Try moving closer)");
                return;
            }

            var posText = $"{{\"x\": {data.pos.x}, \"y\": {data.pos.y}, \"z\": {data.pos.z}}}";
            var normText = $"{{\"x\": {data.norm.x}, \"y\": {data.norm.y}, \"z\": {data.norm.z}}}";
        
            if(_surfaceSphere != null) GameObject.Destroy(_surfaceSphere);
            if(_normalSphere1 != null) GameObject.Destroy(_normalSphere1);
            if(_normalSphere2 != null) GameObject.Destroy(_normalSphere2);
            if(_planeUpRightSphere   != null) GameObject.Destroy(_planeUpRightSphere  );
            if(_planeUpLeftSphere    != null) GameObject.Destroy(_planeUpLeftSphere   );
            if(_planeDownLeftSphere  != null) GameObject.Destroy(_planeDownLeftSphere );
            if(_planeDownRightSphere != null) GameObject.Destroy(_planeDownRightSphere);

            _surfaceSphere = AddDebugShape.AddSphere(data.hitBodyGameObject, 0.1f, Color.green);
            _normalSphere1 = AddDebugShape.AddSphere(data.hitBodyGameObject, 0.01f, Color.red);
            _normalSphere2 = AddDebugShape.AddSphere(data.hitBodyGameObject, 0.01f, Color.red);

            _surfaceSphere.transform.localPosition = data.pos;
            _normalSphere1.transform.localPosition = data.pos + data.norm * 0.5f;
            _normalSphere2.transform.localPosition = data.pos + data.norm;
        
            // plane corners
            var planeSize = 0.5f;
            var planePointSize = 0.05f;
            _planeUpRightSphere   = AddDebugShape.AddSphere(data.hitBodyGameObject, planePointSize, Color.green);
            _planeUpLeftSphere    = AddDebugShape.AddSphere(data.hitBodyGameObject, planePointSize, Color.cyan) ;
            _planeDownLeftSphere  = AddDebugShape.AddSphere(data.hitBodyGameObject, planePointSize, Color.blue) ;
            _planeDownRightSphere = AddDebugShape.AddSphere(data.hitBodyGameObject, planePointSize, Color.cyan) ;
        
            _planeUpRightSphere  .transform.localPosition = data.plane.origin + data.plane.u*1*planeSize + data.plane.v*1*planeSize;
            _planeUpLeftSphere   .transform.localPosition = data.plane.origin + data.plane.u*-1*planeSize + data.plane.v*1*planeSize;
            _planeDownLeftSphere .transform.localPosition = data.plane.origin + data.plane.u*-1*planeSize + data.plane.v*-1*planeSize;
            _planeDownRightSphere.transform.localPosition = data.plane.origin + data.plane.u*1*planeSize + data.plane.v*-1*planeSize;

            Logger.Log($"Raycast hit \"position\": {posText}, \"normal\": {normText} on [{data.bodyName}] at [{data.bodyPath}]");
        }
        internal DebugRaycastData Raycast()
        {
            DebugRaycastData data = new DebugRaycastData();

            _rb.DisableCollisionDetection();
            int layerMask = OWLayerMask.physicalMask;
            var origin = Locator.GetActiveCamera().transform.position;
            var direction = Locator.GetActiveCamera().transform.TransformDirection(Vector3.forward);
            
            data.hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, 100f, layerMask);
            if (data.hit)
            {
                data.pos = hitInfo.transform.InverseTransformPoint(hitInfo.point);
                data.norm = hitInfo.transform.InverseTransformDirection(hitInfo.normal);
                var o = hitInfo.transform.gameObject;

                var hitAstroObject = o.GetComponent<AstroObject>() ?? o.GetComponentInParent<AstroObject>();

                data.bodyName = o.name;
                data.bodyPath = o.transform.GetPath();
                data.hitObject = o;
                data.hitBodyGameObject = hitAstroObject?.gameObject ?? o; 
                data.plane = ConstructPlane(data);
            }
            _rb.EnableCollisionDetection();

            return data;
        }

        
        internal DebugRaycastPlane ConstructPlane(DebugRaycastData data)
        {
            var U = data.pos - Vector3.zero; // U is the local "up" direction. the direction directly away from the center of the planet at this point. // pos is always relative to the body, so the body is considered to be at 0,0,0.
            var R = data.pos; // R is our origin point for the plane
            var N = data.norm.normalized; // N is the normal for this plane
        
            if (Vector3.Cross(U, N) == Vector3.zero) U = new Vector3(0, 0, 1);
            if (Vector3.Cross(U, N) == Vector3.zero) U = new Vector3(0, 1, 0); // if 0,0,1 was actually the same vector U already was (lol), try (0,1,0) instead

            Logger.LogVerbose("Up vector is " + U.ToString());

            // stackoverflow.com/a/9605695
            // I don't know exactly how this works, but I'm projecting a point that is located above the plane's origin, relative to the planet, onto the plane. this gets us our v vector
            var q = (2*U)-R;
            var dist = Vector3.Dot(N, q);
            var v_raw = 2*U - dist*N;
            var v = (R-v_raw).normalized;  
            
            var u = Vector3.Cross(N, v);

            DebugRaycastPlane p = new DebugRaycastPlane()
            {
                origin = R,
                normal = N,
                u = u,
                v = v
            };
            return p;
        }
    }
}
