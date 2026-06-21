#if !(UNITY_WSA && ENABLE_DOTNET)

using System;

namespace DInject.Tests.Convention
{
    public partial class ConventionTestAttribute : Attribute
    {
        public ConventionTestAttribute(int num)
        {
            Num = num;
        }

        public int Num
        {
            get;
            private set;
        }
    }

    public interface IFoo
    {
    }

    public partial class Foo1 : IFoo
    {
    }

    [ConventionTest(0)]
    public partial class Foo2 : IFoo
    {
    }

    [ConventionTest(1)]
    public partial class Foo3 : IFoo
    {
    }
}

#endif
