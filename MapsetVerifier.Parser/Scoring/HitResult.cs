// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace MapsetVerifier.Parser.Scoring
{
    public enum HitResult
    {
        /// <summary>
        ///     Indicates that the object has not been judged yet.
        /// </summary>
        [Description(@""), EnumMember(Value = "none")] 
        None,

        /// <summary>
        ///     Indicates that the object has been judged as a miss.
        /// </summary>
        /// <remarks>
        ///     This miss window should determine how early a hit can be before it is considered for judgement (as opposed to being
        ///     ignored as
        ///     "too far in the future). It should also define when a forced miss should be triggered (as a result of no user input
        ///     in time).
        /// </remarks>
        [Description(@"Miss"), EnumMember(Value = "miss")] 
        Miss,

        [Description(@"Meh"), EnumMember(Value = "meh")] 
        Meh,

        [Description(@"OK"), EnumMember(Value = "ok")] 
        Ok,

        [Description(@"Good"), EnumMember(Value = "good")] 
        Good,

        [Description(@"Great"), EnumMember(Value = "great")] 
        Great,

        /// <summary>
        ///     This is an optional timing window tighter than <see cref="Great" />.
        /// </summary>
        /// <remarks>
        ///     By default, this does not give any bonus accuracy or score.
        ///     To have it affect scoring, consider adding a nested bonus object.
        /// </remarks>
        [Description(@"Perfect"), EnumMember(Value = "perfect")] 
        Perfect,

        /// <summary>
        ///     Indicates small tick miss.
        /// </summary>
        [EnumMember(Value = "small_tick_miss")]
        SmallTickMiss,

        /// <summary>
        ///     Indicates a small tick hit.
        /// </summary>
        [Description(@"S Tick"), EnumMember(Value = "small_tick_hit")] 
        SmallTickHit,

        /// <summary>
        ///     Indicates a large tick miss.
        /// </summary>
        [EnumMember(Value = "large_tick_miss")]
        LargeTickMiss,

        /// <summary>
        ///     Indicates a large tick hit.
        /// </summary>
        [Description(@"L Tick"), EnumMember(Value = "large_tick_hit")] 
        LargeTickHit,

        /// <summary>
        ///     Indicates a small bonus.
        /// </summary>
        [Description("S Bonus"), EnumMember(Value = "small_bonus")] 
        SmallBonus,

        /// <summary>
        ///     Indicates a large bonus.
        /// </summary>
        [Description("L Bonus"), EnumMember(Value = "large_bonus")] 
        LargeBonus,

        /// <summary>
        ///     Indicates a miss that should be ignored for scoring purposes.
        /// </summary>
        [EnumMember(Value = "ignore_miss")] IgnoreMiss,

        /// <summary>
        ///     Indicates a hit that should be ignored for scoring purposes.
        /// </summary>
        [EnumMember(Value = "ignore_hit")] IgnoreHit,

        /// <summary>
        ///     Indicates that a combo break should occur, but does not otherwise affect score.
        /// </summary>
        /// <remarks>
        ///     May be paired with <see cref="IgnoreHit" />.
        /// </remarks>
        [EnumMember(Value = "combo_break")] ComboBreak,

        /// <summary>
        ///     A special result used as a padding value for legacy rulesets. It is a hit type and affects combo, but does not
        ///     affect the base score (does not affect accuracy).
        /// </summary>
        /// <remarks>
        ///     DO NOT USE.
        /// </remarks>
        [EnumMember(Value = "legacy_combo_increase"), Obsolete("Do not use.")] 
        LegacyComboIncrease = 99
    }
}