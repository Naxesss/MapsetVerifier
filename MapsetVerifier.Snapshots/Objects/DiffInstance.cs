using System;
using System.Collections.Generic;
using static MapsetVerifier.Snapshots.Snapshotter;

namespace MapsetVerifier.Snapshots.Objects
{
    public class DiffInstance
    {
        public DiffInstance(string diff, string section, DiffType diffType, List<string> details, DateTime snapshotCreationDate)
        {
            Section = section;
            Diff = diff;
            DiffType = diffType;
            Details = details;
            SnapshotCreationDate = snapshotCreationDate;
        }

        public List<string> Details { get; }
        public string Diff { get; }
        public DiffType DiffType { get; }
        public DateTime SnapshotCreationDate { get; }

        public string Section { get; set; }
    }
}