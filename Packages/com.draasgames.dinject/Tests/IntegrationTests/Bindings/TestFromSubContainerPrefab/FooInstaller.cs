using UnityEngine;

#pragma warning disable 649

namespace DInject.Tests.Bindings.FromSubContainerPrefab
{
    public partial class FooInstaller : MonoInstaller
    {
        [SerializeField]
        Bar _bar;

        public override void InstallBindings()
        {
            Container.BindInstance(_bar);
            Container.Bind<Gorp>().WithId("gorp").AsSingle();
        }
    }
}
