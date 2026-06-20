using UnityEngine;

namespace DInject.Tests.IntegrationTests.MemoryPool
{
    public class FooMemoryPool : MonoBehaviour
    {
        public class Pool : MonoMemoryPool<FooMemoryPool>
        {
        }
    }
}