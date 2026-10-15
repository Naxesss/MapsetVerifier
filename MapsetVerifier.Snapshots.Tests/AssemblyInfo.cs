using Xunit;

// Snapshotter's save path is process-wide static state (Snapshotter.ConfigurePath), so tests
// across different classes in this assembly must not run concurrently with each other.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
