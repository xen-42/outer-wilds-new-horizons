using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NewHorizons.Utility.DebugUtilities
{
    class DebugNomaiTextPlacer : MonoBehaviour
    {
        public static bool active;
        public Action<DebugRaycastData> onRaycast;

        private DebugRaycaster _rc;

        private void Awake()
        {
            _rc = this.GetComponent<DebugRaycaster>();
        }

        void Update()
        {
            if (!Main.Debug) return;
            if (!active) return;

            if (Keyboard.current[Key.G].wasReleasedThisFrame)
            {
                DebugRaycastData data = _rc.Raycast();
                if (onRaycast != null) onRaycast.Invoke(data);
            }
        }
    }
}
