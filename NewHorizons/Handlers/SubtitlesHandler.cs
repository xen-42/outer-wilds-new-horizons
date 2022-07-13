using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using NewHorizons.Utility;
using OWML.Common;
using Logger = NewHorizons.Utility.Logger;

namespace NewHorizons.Handlers
{
    class SubtitlesHandler : MonoBehaviour
    {
        public static int SUBTITLE_HEIGHT = 97;
        public static int SUBTITLE_WIDTH = 669; // nice

        public Graphic graphic;
        public Image image;

        public float fadeSpeed = 0.005f;
        public float fade = 1;
        public bool fadingAway = true;

        public List<Sprite> possibleSubtitles = new List<Sprite>();
        public bool eoteSubtitleHasBeenInserted = false;
        public Sprite eoteSprite;
        public int subtitleIndex;

        public System.Random randomizer;

        public static readonly int PAUSE_TIMER_MAX = 50;
        public int pauseTimer = PAUSE_TIMER_MAX;

        public void CheckForEOTE()
        {
            if (!eoteSubtitleHasBeenInserted)
            {
                if (Main.HasDLC)
                {
                    if (eoteSprite != null) possibleSubtitles.Insert(0, eoteSprite); // ensure that the Echoes of the Eye subtitle always appears first
                    eoteSubtitleHasBeenInserted = true;
                }
            }
        }

        public void Start()
        {
            randomizer = new System.Random();

            GetComponent<CanvasGroup>().alpha = 1;
            graphic = GetComponent<Graphic>();
            image =   GetComponent<UnityEngine.UI.Image>();

            graphic.enabled = true;
            image.enabled = true;

            eoteSprite = image.sprite;

            CheckForEOTE();

            image.sprite = null; // Just in case. I don't know how not having the dlc changes the subtitle game object

            AddSubtitles();
        }

        private void AddSubtitles()
        {
            foreach (var mod in Main.MountedAddons.Where(mod => File.Exists($"{mod.ModHelper.Manifest.ModFolderPath}subtitle.png")))
            {
                AddSubtitle(mod, "subtitle.png");
            }
        }

        public void AddSubtitle(IModBehaviour mod, string filepath)
        {
            Logger.Log($"Adding subtitle for {mod.ModHelper.Manifest.Name}");

            var tex = ImageUtilities.GetTexture(mod, filepath, false);
            if (tex == null) return;

            var sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, SUBTITLE_HEIGHT), new Vector2(0.5f, 0.5f), 100.0f);
            AddSubtitle(sprite);
        }

        public void AddSubtitle(Sprite sprite)
        {
            possibleSubtitles.Add(sprite);
        }

        public void Update()
        {
            CheckForEOTE();

            if (possibleSubtitles.Count == 0) return;

            if (image.sprite == null) image.sprite = possibleSubtitles[0];

            // don't fade transition subtitles if there's only one subtitle
            if (possibleSubtitles.Count <= 1) return;

            if (pauseTimer > 0)
            {
                pauseTimer--;
                return;
            }

            if (fadingAway)
            {
                fade -= fadeSpeed;
            
                if (fade <= 0)
                {
                    fade = 0;
                    ChangeSubtitle();
                    fadingAway = false;
                }
            }
            else
            {
                fade += fadeSpeed;
            
                if (fade >= 1)
                {
                    fade = 1;
                    fadingAway = true;
                    pauseTimer = PAUSE_TIMER_MAX;
                }
            }

            graphic.color = new Color(1, 1, 1, fade);
        }

        public void ChangeSubtitle()
        {
            // to pick a new random subtitle without requiring retries, we generate a random offset less than the length of the possible subtitles array
            // we then add that offset to the current index, modulo NUMBER_OF_POSSIBLE_SUBTITLES
            // since the offset can never be NUMBER_OF_POSSIBLE_SUBTITLES, it will never wrap all the way back around to the initial subtitleIndex

            // note, this makes the code more confusing, but Random.Next(min, max) generates a random number on the range [min, max)
            // that is, the below code will generate numbers up to and including Count-1, not Count.
            var newIndexOffset = randomizer.Next(1, possibleSubtitles.Count);
            subtitleIndex = (subtitleIndex + newIndexOffset) % possibleSubtitles.Count;
            
            image.sprite = possibleSubtitles[subtitleIndex];
        }
    }
}
