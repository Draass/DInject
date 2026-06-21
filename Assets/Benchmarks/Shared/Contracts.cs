namespace DInjectBench
{
    // The benchmark dependency graph expressed as container-agnostic interfaces.
    // Each container ships its OWN concrete implementations (in its own assembly) so that
    // container-specific attributes / codegen never cross-contaminate one another.
    //
    //   IServiceGraphRoot (root, width 4)
    //     +-- IMid  -> ILeaf      (depth)
    //     +-- IMid  -> ILeaf
    //     +-- ILeaf
    //     +-- ILeaf
    public interface ILeaf { }

    public interface IMid { }

    public interface IServiceGraphRoot
    {
        void Use();
    }

    // One adapter per container. Implementations live in per-container assemblies and are
    // discovered by reflection at test time (see BenchAdapters), so adding a new container
    // requires zero changes to the runner.
    public interface IContainerAdapter
    {
        // Display name used in SampleGroup labels, e.g. "DInject".
        string Name { get; }

        // null  => the container is ready to benchmark.
        // text  => a human-readable reason to skip (e.g. codegen/baking not active).
        string SelfCheck();

        // Build and fully initialise the container with the standard graph bound transient.
        object Build();

        // The measured operation: resolve the graph root from a built container.
        IServiceGraphRoot ResolveRoot(object container);
    }
}
