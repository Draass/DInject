using UnityEngine;
using DInject;

namespace DInject.Tests.Installers.CompositeMonoInstallers
{
    public partial class FooInjectee
    {
        public FooInjectee(Foo foo)
        {
            Foo = foo;
        }

        public Foo Foo { get; }
    }
}