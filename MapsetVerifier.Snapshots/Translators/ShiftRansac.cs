namespace MapsetVerifier.Snapshots.Translators
{
    internal static class ShiftRansac
    {
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
