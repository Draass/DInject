
using System.Collections;
using DInject.Internal;
using UnityEngine;
using UnityEngine.TestTools;

namespace DInject.Tests.Bindings.FromPrefabInstaller
{
    public partial class TestFromPrefabInstaller : ZenjectIntegrationTestFixture
    {
        GameObject FooPrefab
        {
            get { return FixtureUtil.GetPrefab(FooPrefabResourcePath); }
        }

        string FooPrefabResourcePath
        {
            get { return "TestFromPrefabInstaller/Foo"; }
        }

        [UnityTest]
        public IEnumerator TestInstaller()
        {
            PreInstall();

            Container.Bind<Qux>().FromSubContainerResolve()
                .ByNewPrefabInstaller<FooInstaller>(FooPrefab).AsCached();

            PostInstall();

            Assert.IsEqual(Container.Resolve<Qux>().Data, "asdf");
            yield break;
        }

        [UnityTest]
        public IEnumerator TestInstallerGetter()
        {
            PreInstall();

            Container.Bind<Qux>().FromSubContainerResolve()
                .ByNewPrefabInstaller<FooInstaller>(_ => FooPrefab).AsCached();

            PostInstall();

            Assert.IsEqual(Container.Resolve<Qux>().Data, "asdf");
            yield break;
        }

        [UnityTest]
        public IEnumerator TestMethod()
        {
            PreInstall();

            Container.Bind<Qux>().FromSubContainerResolve()
                .ByNewPrefabMethod(FooPrefab, InstallFoo).AsCached();

            PostInstall();

            Assert.IsEqual(Container.Resolve<Qux>().Data, "asdf");
            yield break;
        }

        [UnityTest]
        public IEnumerator TestMethodGetter()
        {
            PreInstall();

            Container.Bind<Qux>().FromSubContainerResolve()
                .ByNewPrefabMethod((context) => FooPrefab, InstallFoo).AsCached();

            PostInstall();

            Assert.IsEqual(Container.Resolve<Qux>().Data, "asdf");
            yield break;
        }

        [UnityTest]
        public IEnumerator TestResourceInstaller()
        {
            PreInstall();

            Container.Bind<Qux>().FromSubContainerResolve()
                .ByNewPrefabResourceInstaller<FooInstaller>(FooPrefabResourcePath).AsCached();

            PostInstall();

            Assert.IsEqual(Container.Resolve<Qux>().Data, "asdf");
            yield break;
        }

        [UnityTest]
        public IEnumerator TestResourceMethod()
        {
            PreInstall();

            Container.Bind<Qux>().FromSubContainerResolve()
                .ByNewPrefabResourceMethod(FooPrefabResourcePath, InstallFoo).AsCached();

            PostInstall();

            Assert.IsEqual(Container.Resolve<Qux>().Data, "asdf");
            yield break;
        }

        void InstallFoo(DiContainer subContainer)
        {
            subContainer.Bind<Qux>().AsSingle().WithArguments("asdf");
        }

        public partial class Qux
        {
            [Inject]
            public string Data;

            [Inject]
            public Foo Foo;
        }

        public partial class FooInstaller : Installer<FooInstaller>
        {
            public override void InstallBindings()
            {
                Container.Bind<Qux>().AsSingle().WithArguments("asdf");
            }
        }
    }
}

