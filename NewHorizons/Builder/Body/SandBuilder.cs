﻿using NewHorizons.External.Modules.VariableSize;
using NewHorizons.Utility;
using UnityEngine;

namespace NewHorizons.Builder.Body
{
    public static class SandBuilder
    {
        public static void Make(GameObject planetGO, Sector sector, OWRigidbody rb, SandModule module)
        {
            var sandGO = new GameObject("Sand");
            sandGO.SetActive(false);

            var sandSphere = Object.Instantiate(GameObject.Find("TowerTwin_Body/SandSphere_Draining/SandSphere"),
                sandGO.transform);
            if (module.Tint != null)
            {
                var oldMR = sandSphere.GetComponent<TessellatedSphereRenderer>();
                var sandMaterials = oldMR.sharedMaterials;
                var sandMR = sandSphere.AddComponent<TessellatedSphereRenderer>();
                sandMR.CopyPropertiesFrom(oldMR);
                sandMR.sharedMaterials = new[]
                {
                    new Material(sandMaterials[0]),
                    new Material(sandMaterials[1])
                };
                Object.Destroy(oldMR);
                sandMR.sharedMaterials[0].color = module.Tint;
                sandMR.sharedMaterials[1].color = module.Tint;
            }

            var collider = Object.Instantiate(GameObject.Find("TowerTwin_Body/SandSphere_Draining/Collider"),
                sandGO.transform);
            var sphereCollider = collider.GetComponent<SphereCollider>();
            collider.SetActive(true);

            var occlusionSphere =
                Object.Instantiate(GameObject.Find("TowerTwin_Body/SandSphere_Draining/OcclusionSphere"),
                    sandGO.transform);

            var proxyShadowCasterGO =
                Object.Instantiate(GameObject.Find("TowerTwin_Body/SandSphere_Draining/ProxyShadowCaster"),
                    sandGO.transform);
            var proxyShadowCaster = proxyShadowCasterGO.GetComponent<ProxyShadowCaster>();
            proxyShadowCaster.SetSuperGroup(sandGO.GetComponent<ProxyShadowCasterSuperGroup>());

            sandSphere.AddComponent<ChildColliderSettings>();

            if (module.Curve != null)
            {
                var levelController = sandGO.AddComponent<SandLevelController>();
                var curve = new AnimationCurve();
                foreach (var pair in module.Curve) curve.AddKey(new Keyframe(pair.Time, 2f * module.Size * pair.Value));
                levelController._scaleCurve = curve;
            }

            sandGO.transform.parent = sector?.transform ?? planetGO.transform;
            sandGO.transform.position = planetGO.transform.position;
            sandGO.transform.localScale = Vector3.one * module.Size * 2f;

            sandGO.SetActive(true);
        }
    }
}