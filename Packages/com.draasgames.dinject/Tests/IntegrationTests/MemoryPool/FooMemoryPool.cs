using UnityEngine;

namespace DInject.Tests.IntegrationTests.MemoryPool
{
    public partial class FooMemoryPool : MonoBehaviour
    {
        public partial class Pool : MonoMemoryPool<FooMemoryPool>
        {
        }
    }
}