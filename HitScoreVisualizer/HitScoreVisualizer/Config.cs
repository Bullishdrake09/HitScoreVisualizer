using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

using static HitScoreVisualizer.Utils.ReflectionUtil;

namespace HitScoreVisualizer
{
    public class Config
    {
        public static Config instance;

        // If true, this config will not overwrite the existing config file.
        // (This gets set if a config from a newer version is detected.)
        [JsonIgnore]
        public bool noSerialize;

        public struct Judgment
        {
            // This judgment will be applied only to notes hit with score >= this number.
            // Note that if no judgment can be applied to a note, the text will appear as in the unmodded
            // game.
            [DefaultValue(0)]
            public int threshold;

            // The text to display (if judgment text is enabled).
            [DefaultValue("")]
            public string text;

            // 4 floats, 0-1; red, green, blue, glow (not transparency!)
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)] // leaving this out should look obviously wrong
            public float[] color;

            // If true, the text color will be lerped between this judgment's color and the previous
            // based on how close to the next threshold it is.
            // Specifying fade : true for the first judgment in the array is an error, and will crash the
            // plugin.
            [DefaultValue(false)]
            public bool fade;
        }

        // Judgments for individual parts of the swing (angle before, angle after, accuracy).
        public struct SegmentJudgment
        {
            // This judgment will be applied only when the appropriate part of the swing contributes score >= this number.
            // If no judgment can be applied, the judgment for this segment will be "" (the empty string).
            [DefaultValue(0)]
            public int threshold;
            // The text to replace the appropriate judgment specifier with (%B, %C, %A) when this judgment applies.
            public string text;
        }

        // If the version number (excluding patch version) of the config is higher than that of the plugin,
        // the config will not be loaded. If the version number of the config is lower than that of the
        // plugin, the file will be automatically converted. Conversion is not guaranteed to occur, or be
        // accurate, across major versions.
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int majorVersion;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int minorVersion;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int patchVersion;

        // If this is true, the config will be overwritten with the plugin's default settings after an
        // update rather than being converted.
        public bool isDefaultConfig;

        // If set to "format", displays the judgment text, with the following format specifiers allowed:
        // - %b: The score contributed by the part of the swing before cutting the block.
        // - %c: The score contributed by the accuracy of the cut.
        // - %a: The score contributed by the part of the swing after cutting the block.
        // - %B, %C, %A: As above, except using the appropriate judgment from that part of the swing (as configured for "beforeCutAngleJudgments", "accuracyJudgments", or "afterCutAngleJudgments").
        // - %s: The total score for the cut.
        // - %p: The percent out of 115 you achieved with your swing's score
        // - %%: A literal percent symbol.
        // - %n: A newline.
        //
        // If set to "numeric", displays only the note score.
        // If set to "textOnly", displays only the judgment text.
        // If set to "scoreOnTop", displays both (numeric score above judgment text).
        // Otherwise, displays both (judgment text above numeric score).
        [DefaultValue("")]
        public string displayMode;

        // If enabled, judgments will appear and stay at (fixedPosX, fixedPosY, fixedPosZ) rather than moving as normal.
        // Additionally, the previous judgment will disappear when a new one is created (so there won't be overlap).
        [DefaultValue(false)]
        public bool useFixedPos;
        [DefaultValue(0f)]
        public float fixedPosX;
        [DefaultValue(0f)]
        public float fixedPosY;
        [DefaultValue(0f)]
        public float fixedPosZ;

        // If enabled, judgments will be updated more frequently. This will make score popups more accurate during a brief period before the note's score is finalized, at some cost of performance.
        [DefaultValue(false)]
        public bool doIntermediateUpdates;

        // Order from highest threshold to lowest; the first matching judgment will be applied
        public Judgment[] judgments;

        // Judgments for the part of the swing before
