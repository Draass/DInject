namespace DInject
{
    public interface IDecoratableMonoKernel
    {
        bool ShouldInitializeOnStart();
        void Initialize();
        void Update();
        void FixedUpdate();
        void LateUpdate();
        void Dispose();
        void LateDispose();
    }
}
