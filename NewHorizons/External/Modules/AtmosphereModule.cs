using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using NewHorizons.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NewHorizons.External.Modules
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CloudFluidType
    {
        [EnumMember(Value = @"none")] None = 0,

        [EnumMember(Value = @"water")] Water = 1,

        [EnumMember(Value = @"cloud")] Cloud = 2,

        [EnumMember(Value = @"sand")] Sand = 3,

        [EnumMember(Value = @"plasma")] Plasma = 4
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CloudPrefabType
    {
        [EnumMember(Value = @"giantsDeep")] GiantsDeep = 0,

        [EnumMember(Value = @"quantumMoon")] QuantumMoon = 1,

        [EnumMember(Value = @"basic")] Basic = 2,
    }

    [JsonObject]
    public class AtmosphereModule
    {
        /// <summary>
        /// Colour of atmospheric shader on the planet.
        /// </summary>
        public MColor atmosphereTint;

        /// <summary>
        /// Describes the clouds in the atmosphere
        /// </summary>
        public CloudInfo clouds;

        /// <summary>
        /// How dense the fog is, if you put fog.
        /// </summary>
        [Range(0f, 1f)] public float fogDensity;

        /// <summary>
        /// Radius of fog sphere, independent of the atmosphere. This has to be set for there to be fog.
        /// </summary>
        [Range(0f, double.MaxValue)] public float fogSize;

        /// <summary>
        /// Colour of fog on the planet, if you put fog.
        /// </summary>
        public MColor fogTint;

        /// <summary>
        /// Lets you survive on the planet without a suit.
        /// </summary>
        public bool hasOxygen;

        /// <summary>
        /// Does this planet have rain?
        /// </summary>
        public bool hasRain;

        /// <summary>
        /// Does this planet have snow?
        /// </summary>
        public bool hasSnow;

        /// <summary>
        /// Scale height of the atmosphere
        /// </summary>
        public float size;

        /// <summary>
        /// Whether we use an atmospheric shader on the planet. Doesn't affect clouds, fog, rain, snow, oxygen, etc. Purely
        /// visual.
        /// </summary>
        public bool useAtmosphereShader;

        // not an actual config thing, rip 
        public class AirInfo
        {
            public bool hasOxygen;
            public bool isRaining;
            public bool isSnowing;
            public float scale;
        }

        [JsonObject]
        public class CloudInfo
        {
            /// <summary>
            /// Should these clouds be based on Giant's Deep's banded clouds, or the Quantum Moon's non-banded clouds?
            /// </summary>
            public CloudPrefabType cloudsPrefab;

            /// <summary>
            /// Relative filepath to the cloud cap texture, if the planet has clouds.
            /// </summary>
            public string capPath;

            /// <summary>
            /// Fluid type for sounds/effects when colliding with this cloud.
            /// </summary>
            public CloudFluidType fluidType = CloudFluidType.Cloud;

            /// <summary>
            /// Add lightning to this planet like on Giant's Deep.
            /// </summary>
            public bool hasLightning;

            /// <summary>
            /// Radius from the center to the inner layer of the clouds.
            /// </summary>
            public float innerCloudRadius;

            /// <summary>
            /// Colour gradient of the lightning, time is in seconds.
            /// </summary>
            public MGradient[] lightningGradient;

            /// <summary>
            /// Radius from the center to the outer layer of the clouds.
            /// </summary>
            public float outerCloudRadius;

            /// <summary>
            /// Relative filepath to the cloud ramp texture, if the planet has clouds. If you don't put anything here it will be
            /// auto-generated.
            /// </summary>
            public string rampPath;

            /// <summary>
            /// Relative filepath to the cloud texture, if the planet has clouds.
            /// </summary>
            public string texturePath;

            /// <summary>
            /// Colour of the inner cloud layer.
            /// </summary>
            public MColor tint;

            /// <summary>
            /// If the top layer shouldn't have shadows. Set to true if you're making a brown dwarf for example.
            /// </summary>
            public bool unlit;


            
            #region Obsolete

            /// <summary>
            /// Set to `false` in order to use Giant's Deep's shader. Set to `true` to just apply the cloud texture as is.
            /// </summary>
            [Obsolete("useBasicCloudShader is deprecated, please use cloudsPrefab=\"basic\" instead")]
            public bool useBasicCloudShader;

            #endregion Obsolete

        }


        #region Obsolete

        [Obsolete("CloudTint is deprecated, please use CloudInfo instead")]
        public MColor cloudTint;

        [Obsolete("CloudTint is deprecated, please use CloudInfo instead")]
        public string cloud;

        [Obsolete("CloudCap is deprecated, please use CloudInfo instead")]
        public string cloudCap;

        [Obsolete("CloudRamp is deprecated, please use CloudInfo instead")]
        public string cloudRamp;

        [Obsolete("CloudFluidType is deprecated, please use CloudInfo instead")]
        public CloudFluidType fluidType;

        [Obsolete("UseBasicCloudShader is deprecated, please use CloudInfo instead")]
        public bool useBasicCloudShader;

        [DefaultValue(true)] [Obsolete("ShadowsOnClouds is deprecated, please use CloudInfo instead")]
        public bool shadowsOnClouds = true;

        [Obsolete("HasAtmosphere is deprecated, please use UseAtmosphereShader instead")]
        public bool hasAtmosphere;

        #endregion Obsolete
    }
}