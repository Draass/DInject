using DInject.Internal;
using UnityEngine;

namespace DInject.Tests.IntegrationTests.Async.Addressable
{
    public partial class AddressableTargetComponent : MonoBehaviour, IPoolable<int, IMemoryPool>
    {
        [InjectOptional] public int InjectedValue;
        
        public partial class Factory : PlaceholderFactory<int, AddressableTargetComponent>
        {
        }

        public void OnDespawned()
        {
            
        }

        public void OnSpawned(int p1, IMemoryPool p2)
        {
            InjectedValue = p1;
            Assert.IsNotNull(p2);
        }
    }
}