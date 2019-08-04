# Mapset Verifier
Mapset Verifier saves you from much of the tedious checking required for a beatmap to be ranked, making it a way less time consuming and unrealistic process. Many rules in the Ranking Criteria are straightforward, but with how many there are and how you just do the same thing on each map, this becomes super slow and gets repetitive real quick. Luckily, many issues are rare, so you'll often get away with just checking a few things in a more reasonable amount of time, but risk overlooking obvious things doing that.

This is solved by automatic checking, in the same way AiMod and Modding Assistant work. Of course, not everything can be checked this way. Things like metadata and timing are a bit too complicated to fully cover with a program, but about half of the Ranking Criteria can be covered this way, which is pretty good.

![](https://i.imgur.com/F6HhxPU.gif)

# Features
- Automatic song folder detection
- Automatic snapshots
- Automatic updates
- Integrated documentation
- Changeable difficulty interpretation
- Verbose mode (minor issues)
- Plugin support (add check DLLs to /Mapset Verifier Externals/checks)
- Open source (see MapsetParser, MapsetChecks, etc)

# Examples of fixed false positives/negatives
### Modding Assistant
- counting extended break times as drain time.
- failing to account for minimum SV (0.1x).
- misinterpreting hit sounds on slider bodies as hit sounds on heads/tails.
- completely ignoring storyboard variables and animation frames.

### AiMod
- failing to detect unsnaps when they are 2 ms too early.
- not accounting for stacking.
- using a vastly outdated star rating system, saying you need an easy/normal when you already have one.
- using inaccurate playfield measurements to detect offscreen hit objects.

# Checks (as of 2019-08-03)
### General
- **(Resources)** Missing background.
- **(Resources)** Too high or low background resolution.
- **(Resources)** Multiple videos.
- **(Resources)** Too high sprite resolution.
- **(Resources)** Inconsistent video offset.
- **(Resources)** Too high video resolution.
- **(Metadata)** Additional markers in title.
- **(Metadata)** Inconsistent metadata.
- **(Metadata)** Incorrect marker format.
- **(Metadata)** Incorrect marker spacing.
- **(Metadata)** Unicode in romanized fields.
- **(Metadata)** Incorrect format of (TV Size) / (Game Ver.) / (Short Ver.) in title.
- **(Files)** Unused files.
- **(Files)** Issues with updating or downloading.
- **(Files)** 0-byte files.
- **(Audio)** Audio channels in video.
- **(Audio)** Too high or low audio bitrate.
- **(Audio)** Delayed hit sounds.
- **(Audio)** Incorrect hit sound format.
- **(Audio)** Imbalanced hit sounds.
- **(Audio)** Too short hit sounds.
- **(Audio)** Multiple audio files.
### All Modes
- **(Timing)** Two inherited or uninherited concurrent timing lines.
- **(Timing)** First line toggles kiai or is inherited.
- **(Timing)** Inconsistent uninherited lines, meter signatures or BPM.
- **(Timing)** Unsnapped kiai.
- **(Timing)** Inconsistent or unset preview time.
- **(Timing)** Unsnapped hit objects.
- **(Timing)** Unused uninherited lines.
- **(Timing)** Wrongly or inconsistently snapped hit objects.
- **(Spread)** Lowest difficulty too difficult for the given drain/play time(s).
- **(Settings)** Abnormal difficulty settings.
- **(Settings)** Inconsistent mapset id, countdown, epilepsy warning, etc.
- **(Settings)** Slider tick rates not aligning with any common beat snap divisor.
- **(Hit Sounds)** Low volume hit sounding.
- **(Events)** Breaks only achievable through .osu editing.
- **(Compose)** Abnormal amount of slider nodes.
- **(Compose)** More than 20% unused audio at the end.
- **(Compose)** Concurrent hit objects.
- **(Compose)** Too short drain time.
- **(Compose)** Invisible sliders.
### Standard
- **(Timing)** Hit object is slightly behind a line which would modify it.
- **(Spread)** Objects close in time not overlapping.
- **(Spread)** Multiple reverses on too short sliders.
- **(Spread)** Too short sliders.
- **(Spread)** Object too close or far away from previous.
- **(Spread)** Too short spinner time or spinner recovery time.
- **(Spread)** Perfect stacks too close in time.
- **(Settings)** Default combo colours without forced skin.
- **(Settings)** Too dark or bright combo colours or slider borders.
- **(Hit Sounds)** Long periods without hit sounding.
- **(Events)** Storyboarded hit sounds.
- **(Compose)** Perfectly overlapping combination of tail, head or red anchors.
- **(Compose)** Burai slider.
- **(Compose)** Too short spinner.
- **(Compose)** Obscured reverse arrows.
- **(Compose)** Offscreen hit objects.
### Taiko
- **(Timing)** Hit object is slightly behind a line which would modify it.
- **(Events)** Storyboarded hit sounds.
### Catch
- **(Timing)** Hit object is slightly behind a line which would modify it.
- **(Hit Sounds)** Long periods without hit sounding.
- **(Events)** Storyboarded hit sounds.
### Mania
- **(Hit Sounds)** Long periods without hit sounding.

Current taiko, catch, and mania checks are not specific to those modes, they just don't fit into All Modes due to one or more modes being excluded for the respective checks.

# Note
- Always use your own judgment. False positives and negatives may exist (as with any modding tool), meaning some detections will happen even if they shouldn't and visa versa.
- Game modes apart from standard do not have star ratings implemented and don't have specific checks either by default. The tool can still be used to check for general/all mode things like audio/metadata/files/unsnaps etc, though.
