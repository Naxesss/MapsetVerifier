namespace MapsetVerifier.Snapshots.Translators;

internal static class SequenceAlignment
{
    // Keep two cost rows and one byte per traceback step instead of a full double matrix.
    public static List<Tuple<int, int>> Align<T>(
        IReadOnlyList<T> oldItems,
        IReadOnlyList<T> newItems,
        Func<T, double> time,
        Func<T, T, double> distance,
        double maxWindow
    )
    {
        const double gap = 300;
        var previous = new double[newItems.Count + 1];
        var current = new double[newItems.Count + 1];
        var directions = new byte[oldItems.Count + 1, newItems.Count + 1];

        for (var j = 1; j <= newItems.Count; j++)
        {
            previous[j] = j * gap;
            directions[0, j] = 2;
        }

        for (var i = 1; i <= oldItems.Count; i++)
        {
            current[0] = i * gap;
            directions[i, 0] = 1;
            for (var j = 1; j <= newItems.Count; j++)
            {
                var match =
                    Math.Abs(time(oldItems[i - 1]) - time(newItems[j - 1])) <= maxWindow
                        ? previous[j - 1] + distance(oldItems[i - 1], newItems[j - 1])
                        : double.PositiveInfinity;
                var delete = previous[j] + gap;
                var insert = current[j - 1] + gap;

                current[j] = Math.Min(match, Math.Min(delete, insert));
                // Preserve the original traceback preference for nearly equal costs.
                if (Math.Abs(current[j] - match) < 1e-5)
                    directions[i, j] = 0;
                else if (Math.Abs(current[j] - delete) < 1e-5)
                    directions[i, j] = 1;
                else
                    directions[i, j] = 2;
            }
            (previous, current) = (current, previous);
        }

        var path = new List<Tuple<int, int>>();
        var oldIndex = oldItems.Count;
        var newIndex = newItems.Count;
        while (oldIndex > 0 || newIndex > 0)
        {
            switch (directions[oldIndex, newIndex])
            {
                case 1:
                    path.Add(Tuple.Create(--oldIndex, -1));
                    break;
                case 2:
                    path.Add(Tuple.Create(-1, --newIndex));
                    break;
                default:
                    path.Add(Tuple.Create(--oldIndex, --newIndex));
                    break;
            }
        }
        path.Reverse();
        return path;
    }
}
