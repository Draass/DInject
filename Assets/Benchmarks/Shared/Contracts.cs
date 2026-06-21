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

    // How the graph is registered. Transient = a fresh instance per resolve (stresses construction).
    // Singleton = one cached instance (warm resolve is pure container lookup overhead, ~0 alloc target).
    public enum BindMode
    {
        Transient,
        Singleton
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

        // Build and fully initialise a container with the standard graph bound in the given mode.
        object Build(BindMode mode);

        // Resolve the graph root (deep + wide). Works for transient and singleton bindings.
        IServiceGraphRoot ResolveRoot(object container);

        // Resolve a single leaf (shallow). Isolates per-call container overhead from graph depth.
        ILeaf ResolveLeaf(object container);
    }
}
