using UnityEngine;
using DInject;

namespace DInject.Tests.Installers.CompositeScriptableObjectInstallers
{
    // [CreateAssetMenu(fileName = "DummyInstaller", menuName = "Installers/DummyInstaller")]
    public partial class DummyInstaller : ScriptableObjectInstaller<DummyInstaller>
    {
        public override void InstallBindings()
        {
        }
    }
}