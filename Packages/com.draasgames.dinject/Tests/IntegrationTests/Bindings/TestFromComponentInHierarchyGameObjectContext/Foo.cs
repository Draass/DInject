using System;
using UnityEngine;

namespace DInject.Tests.Bindings.FromComponentInHierarchyGameObjectContext
{
    public partial class Foo : MonoBehaviour
    {
        [NonSerialized]
        [Inject]
        public Gorp Gorp;
    }
}
