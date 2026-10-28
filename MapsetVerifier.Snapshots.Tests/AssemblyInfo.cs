using Xunit;

// The snapshot store root is process-wide static state (SnapshotStore.ConfigurePath), so tests
// across different classes in this assembly must not run concurrently with each other.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
