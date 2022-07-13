using OWML.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NewHorizons
{
    public interface INewHorizons
    {
        [Obsolete("Create(Dictionary<string, object> config) is deprecated, please use LoadConfigs(IModBehaviour mod) instead")]
        void Create(Dictionary<string, object> config);

        [Obsolete("Create(Dictionary<string, object> config) is deprecated, please use LoadConfigs(IModBehaviour mod) instead")]
        void Create(Dictionary<string, object> config, IModBehaviour mod);

        void LoadConfigs(IModBehaviour mod);

        GameObject GetPlanet(string name);

        string GetCurrentStarSystem();

        UnityEvent<string> GetChangeStarSystemEvent();

        UnityEvent<string> GetStarSystemLoadedEvent();

        bool SetDefaultSystem(string name);

        bool ChangeCurrentStarSystem(string name);

        string[] GetInstalledAddons();

        GameObject SpawnObject(GameObject planet, Sector sector, string propToCopyPath, Vector3 position, Vector3 eulerAngles, 
            float scale, bool alignWithNormal);

        AudioSignal SpawnSignal(IModBehaviour mod, GameObject root, string audioSource, string name, string frequency, 
            float sourceRadius = 1f, float detectionRadius = 20f, float identificationRadius = 10f, bool insideCloak = false,
            bool onlyAudibleToScope = true, string reveals = "");
    }
}
