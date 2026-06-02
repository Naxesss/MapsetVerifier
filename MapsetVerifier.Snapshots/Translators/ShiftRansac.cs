namespace MapsetVerifier.Snapshots.Translators
{
    internal static class ShiftRansac
    {
        // Finds the dominant integer shift across a sequence of pair-wise shifts via voting.
        // The osu! file format stores all times as integers, so the true offset is always an
        // integer ms; we round each pair-wise shift before voting so fractional DTW byproducts
        // don't split inliers across two neighbouring integers.
        //
        // Optional dedupKeys collapses concurrent objects (e.g. mania chords) into a single
        // vote — pass the new-object time (or old.time, new.time tuple) so a 7-note chord
        // doesn't outweigh 7 single notes in the consensus.
        //
        // Inliers are pairs whose rounded shift is within +/-tolerance of the winning
        // candidate. Returns (winning shift as a whole number, inlier count); both zero if
        // no candidate clears minInliers.
        public static (double shift, int inlierCount) FindDominantShift(
            IEnumerable<double?> shifts,
            double tolerance,
            int minInliers,
            IEnumerable<double>? dedupKeys = null
        )
        {
            var values = new List<long>();
            if (dedupKeys != null)
            {
                var seen = new HashSet<double>();
                using var shiftEnum = shifts.GetEnumerator();
                using var keyEnum = dedupKeys.GetEnumerator();
                while (shiftEnum.MoveNext() && keyEnum.MoveNext())
                {
                    if (!shiftEnum.Current.HasValue) continue;
                    if (!seen.Add(keyEnum.Current)) continue;
                    values.Add((long)System.Math.Round(shiftEnum.Current.Value));
                }
            }
            else
            {
                foreach (var s in shifts)
                    if (s.HasValue) values.Add((long)System.Math.Round(s.Value));
            }

            if (values.Count < minInliers)
                return (0.0, 0);

            long bestShift = 0;
            int bestCount = 0;

            // O(n^2) voting; n = number of matched pairs (timing lines or hit objects).
            // For sizes seen in practice (hundreds), this is negligible vs DTW (also O(n*m)).
            foreach (var candidate in values)
            {
                int count = 0;
                foreach (var v in values)
                    if (System.Math.Abs(v - candidate) <= tolerance)
                        count++;

                if (count > bestCount)
                {
                    bestCount = count;
                    bestShift = candidate;
                }
            }

            if (bestCount < minInliers)
                return (0.0, 0);

            // Refine: take the median of inliers, then round back to integer. Median is
            // robust to skewed inlier distributions (e.g. a tail of objects re-snapped just
            // barely inside tolerance pulling the average).
            var inliers = new List<long>();
            foreach (var v in values)
                if (System.Math.Abs(v - bestShift) <= tolerance)
                    inliers.Add(v);

            inliers.Sort();
            long median = inliers[inliers.Count / 2];
            return ((double)median, inliers.Count);
        }

        // Estimate the dominant shift directly from all candidate (old, new) pairs (not
        // through DTW). Used as a bias for DTW so the alignment doesn't get tricked into
        // cheap-but-wrong local matches when one side has many extra inserts.
        //
        // Caller supplies the times for both sides plus a compatibility predicate
        // (e.g. same uninherited flag, same lane) and an absolute time window.
        public static (double shift, int count) EstimateGlobalShift<TOld, TNew>(
            IReadOnlyList<TOld> oldList,
            IReadOnlyList<TNew> newList,
            System.Func<TOld, double> oldTime,
            System.Func<TNew, double> newTime,
            System.Func<TOld, TNew, bool> compatible,
            double maxWindow
        )
        {
            // Bucket pairwise shifts to integer ms; find the bin with the most pairs.
            var bucket = new Dictionary<long, int>();
            for (int i = 0; i < oldList.Count; i++)
            {
                double oT = oldTime(oldList[i]);
                for (int j = 0; j < newList.Count; j++)
                {
                    double nT = newTime(newList[j]);
                    if (System.Math.Abs(nT - oT) > maxWindow) continue;
                    if (!compatible(oldList[i], newList[j])) continue;
                    long shift = (long)System.Math.Round(nT - oT);
                    bucket.TryGetValue(shift, out int c);
                    bucket[shift] = c + 1;
                }
            }

            if (bucket.Count == 0) return (0, 0);

            long bestShift = 0;
            int bestCount = 0;
            foreach (var kvp in bucket)
            {
                if (kvp.Value > bestCount)
                {
                    bestCount = kvp.Value;
                    bestShift = kvp.Key;
                }
            }
            return (bestShift, bestCount);
        }
    }
}
