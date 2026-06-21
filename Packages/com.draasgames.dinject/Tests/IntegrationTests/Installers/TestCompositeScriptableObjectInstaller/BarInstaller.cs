using UnityEngine;
using DInject;

namespace DInject.Tests.Installers.CompositeScriptableObjectInstallers
{
    // [CreateAssetMenu(fileName = "BarInstaller", menuName = "Installers/BarInstaller")]
    public partial class BarInstaller : ScriptableObjectInstaller<BarInstaller>
    {
        [SerializeField] string _value;

        public override void InstallBindings()
        {
            Container.BindInstance(_value);
        }
    }
}
