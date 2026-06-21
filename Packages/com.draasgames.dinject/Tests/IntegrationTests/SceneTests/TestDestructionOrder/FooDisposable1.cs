using System;
using UnityEngine;

namespace DInject.Tests.TestDestructionOrder
{
    public partial class FooDisposable1 : IDisposable
    {
        public void Dispose()
        {
            Debug.Log("Destroyed FooDisposable1");
        }
    }
}
