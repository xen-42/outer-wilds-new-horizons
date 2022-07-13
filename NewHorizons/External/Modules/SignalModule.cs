using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NewHorizons.Utility;
using Newtonsoft.Json;

namespace NewHorizons.External.Modules
{
    [JsonObject]
    public class SignalModule
    {
        [Obsolete("Signal module and its signals list is deprecated. Directly use the Signals list.")]
        public SignalInfo[] signals;

        [JsonObject]
        public class SignalInfo
        {
            /// <summary>
            /// Name of an existing AudioClip in the game that will player over the signal.
            /// </summary>
            public string audioClip;

            /// <summary>
            /// Relative filepath to the .wav file to use as the audio. Mutually exclusive with audioClip.
            /// </summary>
            public string audioFilePath;

            /// <summary>
            /// How close the player must get to the signal to detect it. This is when you get the "Unknown Signal Detected"
            /// notification.
            /// </summary>
            [Range(0f, double.MaxValue)] public float detectionRadius;

            /// <summary>
            /// The frequency ID of the signal. The built-in game values are `Default`, `Traveler`, `Quantum`, `EscapePod`,
            /// `Statue`, `WarpCore`, `HideAndSeek`, and `Radio`. You can also put a custom value.
            /// </summary>
            public string frequency;

            /// <summary>
            /// How close the player must get to the signal to identify it. This is when you learn its name.
            /// </summary>
            [DefaultValue(10f)] [Range(0f, double.MaxValue)]
            public float identificationRadius = 10f;

            /// <summary>
            /// Only set to `true` if you are putting this signal inside a cloaking field.
            /// </summary>
            public bool insideCloak;

            /// <summary>
            /// The unique ID of the signal.
            /// </summary>
            public string name;

            /// <summary>
            /// `false` if the player can hear the signal without equipping the signal-scope.
            /// </summary>
            [DefaultValue(true)] public bool onlyAudibleToScope = true;

            /// <summary>
            /// Position of the signal's source
            /// </summary>
            public MVector3 position;

            /// <summary>
            /// A ship log fact to reveal when the signal is identified.
            /// </summary>
            [DefaultValue("")] public string reveals = "";

            /// <summary>
            /// Radius of the sphere giving off the signal.
            /// </summary>
            [DefaultValue(1f)] public float sourceRadius = 1f;
        }
    }
}