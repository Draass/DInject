using System;
using UnityEngine;

namespace DInject.Tests.TestDestructionOrder
{
    public partial class FooDisposable3 : IDisposable
    {
        public void Dispose()
        {
            Debug.Log("Destroyed FooDisposable3");
        }
    }
}
