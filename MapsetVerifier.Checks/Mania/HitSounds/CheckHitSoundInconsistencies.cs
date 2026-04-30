using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

using static MapsetVerifier.Checks.Utils.ManiaUtils;

namespace MapsetVerifier.Checks.Mania.HitSounds
{
	[Check]
	public class CheckHitSoundInconsistencies : BeatmapSetCheck
	{
		public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
		{
			Modes = [Beatmap.Mode.Mania],
			Category = "Hit Sounds",
			Message = "Hitsound inconsistency detected",
			Author = "Tailsdk",

			Documentation = new Dictionary<string, string>
			{
				{
					"Purpose",
					@"
					Check for incosistent hitsounds between difficulties."
				},
				{
					"Reasoning",
					@"
					Hitsounds should generally be consistent between difficulties unless it is intentional."
				}
			}
		};

		public override Dictionary<string, IssueTemplate> GetTemplates()
		{
			return new Dictionary<string, IssueTemplate>
			{
				{ "Warning",
					new IssueTemplate(Issue.Level.Warning,
						"{0} has a hitsound inconsistency with {1}.",
						"timestamp", "beatmap")
					.WithCause(
						"There is a hitsound inconsistency") },
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} is used at {1} but does not exist.",
                        "hitsound", "timestamp")
                    .WithCause(
                        "Missing hitsound") },
                { "Double Hitsound",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} you have 2 of the same samples here make sure this is intentional {1}",
                        "timestamp", "hitsound")
                    .WithCause(
                        "Double hitsound") },
            };
		}

		public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
		{
			
			// List of objects
			List<List<(HitObject.HitSounds, double, string, HitSample.SamplesetType, string)>> beatmapListHS = [];
			List<List<(string, double, HitObject.HitSounds)>> beatmapListSI = [];
			List<int> beatmapsList = [];
			List<string> filesList = [];
            foreach (var s in beatmapSet.HitSoundFiles)
            {
                filesList.Add(s.ToLower());

            }
			List<(HitObject.HitSounds, HitSample.SamplesetType, string)> checkedHS = [];
            foreach (var item in beatmapSet.Beatmaps.Select((beatmap, i) => new { i, beatmap }))
			{
				beatmapsList.Add(item.i);
				if(item.beatmap.GeneralSettings.mode != Beatmap.Mode.Mania)
				{
					continue;
				}
				List<(HitObject.HitSounds, double, string, HitSample.SamplesetType, string)> hitsoundList = [];
				List<(string, double, HitObject.HitSounds)> sampleList = [];
				List<(double, HitSample.SamplesetType, string)> samplesetList = [];
				var index = 0;

				foreach (var tL in item.beatmap.TimingLines)
				{
					samplesetList.Add((tL.Offset, tL.Sampleset, tL.CustomIndex.ToString()));
				}
                samplesetList.Add((double.MaxValue, HitSample.SamplesetType.Normal, ""));
                List<(HitObject.HitSounds, string, HitSample.SamplesetType, string)> Added = [];
                List<(string, HitObject.HitSounds)> SampleAdded = [];
                double checkingTime = 0;

                foreach (var hitObject in item.beatmap.HitObjects)
				{
					if (hitObject.time.AlmostEqual(checkingTime)) 
					{
						checkingTime = hitObject.time;
						Added.Clear();
						SampleAdded.Clear();
					}
					while (samplesetList[index+1].Item1 <= hitObject.time)
					{
						index += 1;
					}
					// Adds the various hitsounds to the hitsound list
					if (hitObject.HasHitSound(HitObject.HitSounds.Clap))
					{

						(HitObject.HitSounds, double, string, HitSample.SamplesetType, string) hitsound = ( (hitObject.filename == null)? HitObject.HitSounds.Clap : HitObject.HitSounds.None, hitObject.time, hitObject.filename, (hitObject.addition == HitSample.SamplesetType.Auto) ? ((hitObject.sampleset == HitSample.SamplesetType.Auto) ? samplesetList[index].Item2 : hitObject.sampleset) : hitObject.addition, (samplesetList[index].Item3 == "1")? "" : samplesetList[index].Item3);
						if (!Added.Contains((hitsound.Item1,hitsound.Item3,hitsound.Item4,hitsound.Item5)))
						{
                            hitsoundList.Add(hitsound);
							Added.Add((hitsound.Item1, hitsound.Item3, hitsound.Item4, hitsound.Item5));
                            if (hitsound.Item1 != HitObject.HitSounds.None && hitsound.Item5 != "0" && !isHitNormalInList((hitsound.Item4.ToString().ToLower() + "-hit" + hitsound.Item1.ToString().ToLower() + hitsound.Item5), filesList))
                            {
                                yield return new Issue(GetTemplate("Problem"), item.beatmap, hitsound.Item4.ToString().ToLower() + "-hit" + hitsound.Item1.ToString().ToLower() + hitsound.Item5 + ".wav/ogg", Timestamp.Get(hitObject.time));
                            }
                        }
						else
						{
							yield return new Issue(GetTemplate("Double Hitsound"), item.beatmap, Timestamp.Get(hitObject.time), hitObject.hitSound);
                        }
					} 
					if (hitObject.HasHitSound(HitObject.HitSounds.Normal))
					{      
						(HitObject.HitSounds, double, string, HitSample.SamplesetType, string) hitsound = ((hitObject.filename == null) ? HitObject.HitSounds.Normal : HitObject.HitSounds.None, hitObject.time, hitObject.filename, (hitObject.addition == HitSample.SamplesetType.Auto) ? ((hitObject.sampleset == HitSample.SamplesetType.Auto) ? samplesetList[index].Item2 : hitObject.sampleset) : hitObject.addition, (samplesetList[index].Item3 == "1") ? "" : samplesetList[index].Item3);
                        if (!Added.Contains((hitsound.Item1, hitsound.Item3, hitsound.Item4, hitsound.Item5)))
                        {
                            hitsoundList.Add(hitsound);
                            Added.Add((hitsound.Item1, hitsound.Item3, hitsound.Item4, hitsound.Item5));
                            if (hitsound.Item1 != HitObject.HitSounds.None && hitsound.Item5 != "0" && !isHitNormalInList((hitsound.Item4.ToString().ToLower() + "-hit" + hitsound.Item1.ToString().ToLower() + hitsound.Item5), filesList))
                            {
                                yield return new Issue(GetTemplate("Problem"), item.beatmap, hitsound.Item4.ToString().ToLower() + "-hit" + hitsound.Item1.ToString().ToLower() + hitsound.Item5 + ".wav/ogg", Timestamp.Get(hitObject.time));
                            }
                        }
                    }
					if (hitObject.HasHitSound(HitObject.HitSounds.None))
					{
						(HitObject.HitSounds, double, string, HitSample.SamplesetType, string) hitsound = (HitObject.HitSounds.None, hitObject.time, hitObject.filename, (hitObject.addition == HitSample.SamplesetType.Auto) ? ((hitObject.sampleset == HitSample.SamplesetType.Auto) ? samplesetList[index].Item2 : hitObject.sampleset) : hitObject.addition, (samplesetList[index].Item3 == "1") ? "" : samplesetList[index].Item3);
                    }
					
					if (hitObject.HasHitSound(HitObject.HitSounds.Whistle))
					{
						(HitObject.HitSounds, double, string, HitSample.SamplesetType, string) hitsound = ((hitObject.filename == null) ? HitObject.HitSounds.Whistle : HitObject.HitSounds.None, hitObject.time, hitObject.filename, (hitObject.addition == HitSample.SamplesetType.Auto) ? ((hitObject.sampleset == HitSample.SamplesetType.Auto) ? samplesetList[index].Item2 : hitObject.sampleset) : hitObject.addition, (samplesetList[index].Item3 == "1") ? "" : samplesetList[index].Item3);
                        if (!Added.Contains((hitsound.Item1, hitsound.Item3, hitsound.Item4, hitsound.Item5)))
                        {
                            hitsoundList.Add(hitsound);
                            Added.Add((hitsound.Item1, hitsound.Item3, hitsound.Item4, hitsound.Item5));
                            if (hitsound.Item1 != HitObject.HitSounds.None && hitsound.Item5 != "0" && !isHitNormalInList((hitsound.Item4.ToString().ToLower() + "-hit" + hitsound.Item1.ToString().ToLower() + hitsound.Item5), filesList))
                            {
                                yield return new Issue(GetTemplate("Problem"), item.beatmap, hitsound.Item4.ToString().ToLower() + "-hit" + hitsound.Item1.ToString().ToLower() + hitsound.Item5 + ".wav/ogg", Timestamp.Get(hitObject.time));
                            }
                        }
                        else
                        {
                            yield return new Issue(GetTemplate("Double Hitsound"), item.beatmap, Timestamp.Get(hitObject.time), hitObject.hitSound);
                        }
                    }
					if (hitObject.HasHitSound(HitObject.HitSounds.Finish))
					{
						(HitObject.HitSounds, double, string, HitSample.SamplesetType, string) hitsound = ((hitObject.filename == null) ? HitObject.HitSounds.Finish : HitObject.HitSounds.None, hitObject.time, hitObject.filename, (hitObject.addition == HitSample.SamplesetType.Auto) ? ((hitObject.sampleset == HitSample.SamplesetType.Auto) ? samplesetList[index].Item2 : hitObject.sampleset) : hitObject.addition, (samplesetList[index].Item3 == "1") ? "" : samplesetList[index].Item3);
                        if (!Added.Contains((hitsound.Item1, hitsound.Item3, hitsound.Item4, hitsound.Item5)))
                        {
                            hitsoundList.Add(hitsound);
                            Added.Add((hitsound.Item1, hitsound.Item3, hitsound.Item4, hitsound.Item5));
                            if (hitsound.Item1 != HitObject.HitSounds.None && hitsound.Item5 != "0" && !isHitNormalInList((hitsound.Item4.ToString().ToLower() + "-hit" + hitsound.Item1.ToString().ToLower() + hitsound.Item5), filesList))
                            {
                                yield return new Issue(GetTemplate("Problem"), item.beatmap, hitsound.Item4.ToString().ToLower() + "-hit" + hitsound.Item1.ToString().ToLower() + hitsound.Item5 + ".wav/ogg", Timestamp.Get(hitObject.time));
                            }
                        }
                        else
                        {
                            yield return new Issue(GetTemplate("Double Hitsound"), item.beatmap, Timestamp.Get(hitObject.time), hitObject.hitSound);
                        }
                    }
					// Adds samples to the sample list
					if (!SampleAdded.Contains((hitObject.filename, hitObject.hitSound)))
					{
                        sampleList.Add((hitObject.filename, hitObject.time, hitObject.hitSound));
						SampleAdded.Add((hitObject.filename, hitObject.hitSound));
                    }
                    if (hitObject.filename != null && !filesList.Contains(hitObject.filename.ToLower()))
                    {
                        yield return new Issue(GetTemplate("Problem"), item.beatmap, hitObject.filename, Timestamp.Get(hitObject.time));
                    }
                }
				beatmapListHS.Add(hitsoundList);
				beatmapListSI.Add(sampleList);
			}
			// Checks if a hitsound could be placed on another difficulty
			for (var i = 0; i < beatmapListHS.Count; i++)
			{
				for (var j = 0; j < beatmapListHS.Count; j++)
				{
					if (j == i)
					{
						continue;
					}
					foreach (var T1 in beatmapListHS[i])
					{
						if (T1.Item1 != HitObject.HitSounds.None)
						{
                            var hasNote = false;
							foreach (var T2 in beatmapListHS[j])
							{

								if (T1.Item2 == T2.Item2 && T1.Item1 == T2.Item1 && T1.Item4 == T2.Item4 && T1.Item5 == T2.Item5)
								{
									break;
								}
								else if (T1.Item2 == T2.Item2 && T2.Item3 == null && T1.Item4 == T2.Item4)
								{
									hasNote = true;
								}
								else if (T1.Item2 < T2.Item2)
								{
									if (hasNote)
									{
										yield return new Issue(GetTemplate("Warning"), beatmapSet.Beatmaps[beatmapsList[i]], Timestamp.Get(T1.Item2), beatmapSet.Beatmaps[beatmapsList[j]]);
									}
									break;
								}
							}
						}
					}
				}
			}
            // Checks if a import sample could be placed on another difficulty
            for (var i = 0; i < beatmapListSI.Count; i++)
            {
                for (var j = 0; j < beatmapListSI.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }
                    foreach (var T1 in beatmapListSI[i])
					{
						if (T1.Item1 != null)
						{
                            var hasNote = false;
                            foreach (var T2 in beatmapListSI[j])
                            {

                                if (T1.Item2 == T2.Item2 && T1.Item1 == T2.Item1)
                                {
                                    break;
                                }
                                else if (T1.Item2 == T2.Item2 && T2.Item1 == null && (T2.Item3 == HitObject.HitSounds.Normal || T2.Item3 == HitObject.HitSounds.None) )
                                {
                                    hasNote = true;
                                }
                                else if (T1.Item2 < T2.Item2)
                                {
                                    if (hasNote)
                                    {
                                        yield return new Issue(GetTemplate("Warning"), beatmapSet.Beatmaps[beatmapsList[i]], Timestamp.Get(T1.Item2), beatmapSet.Beatmaps[beatmapsList[j]]);
                                    }
                                    break;
                                }
                            }
                        }
					}
                }
			}
		}
	}
}
