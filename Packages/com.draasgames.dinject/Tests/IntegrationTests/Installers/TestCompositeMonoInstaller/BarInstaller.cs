using UnityEngine;
using DInject;

namespace DInject.Tests.Installers.CompositeMonoInstallers
{
    public partial class BarInstaller : MonoInstaller<BarInstaller>
    {
        [SerializeField] string _value;

        public override void InstallBindings()
        {
            Container.BindInstance(_value);
        }
    }
}
