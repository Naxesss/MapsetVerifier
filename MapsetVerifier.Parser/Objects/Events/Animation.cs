using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MapsetVerifier.Parser.Objects.Events
{
    public class Animation : Sprite
    {
        // Animation,Fail,Centre,"spr\scn1_spr3_2_b.png",320,280,2,40,LoopForever
        // Animation, layer, origin, filename, x offset, y offset, frame count, frame delay, loop type

        public readonly int frameCount;
        public readonly double frameDelay;

        public readonly List<string> framePaths;
        public readonly bool loops;

        public Animation(string[] args) : base(args)
        {
            frameCount = GetFrameCount(args);
            frameDelay = GetFrameDelay(args);
            loops = IsLooping(args);

            framePaths = GetFramePaths().ToList();
        }

        /// <summary>
        ///     Returns the amount of frames this animation contains.
        ///     Determines how many "filename_i" to use, where i starts at 0.
        /// </summary>
        private int GetFrameCount(string[] args) => int.Parse(args[6]);

        /// <summary> Returns the delay between each frame of this animation in miliseconds. </summary>
        private double GetFrameDelay(string[] args) => double.Parse(args[7], CultureInfo.InvariantCulture);

        /// <summary> Returns whether the animation loops, by default true. </summary>
        private bool IsLooping(string[] args) =>
            // Does not exist in file version 5.
            args?[8] != "LoopOnce";

        /// <summary> Returns all relative file paths for all frames used. </summary>
        public IEnumerable<string> GetFramePaths()
        {
            if (path == null)
            {
                yield break;
            }
            
            for (var i = 0; i < frameCount; ++i)
                yield return path.Insert(path.LastIndexOf(".", StringComparison.Ordinal), i.ToString());
        }
    }
}