using NewHorizons.External.Configs;
using OWML.Common;
using UnityEngine;
namespace NewHorizons.Utility
{
    public class NewHorizonsBody
    {
        public NewHorizonsBody(PlanetConfig config, IModBehaviour mod, string relativePath = null)
        {
            Config = config;
            Mod = mod;
            RelativePath = relativePath;
        }

        public PlanetConfig Config;
        public IModBehaviour Mod;
        public string RelativePath;

        public GameObject Object;
    }
}
