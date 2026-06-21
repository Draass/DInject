
// In order to run this, install dotMemoryPeek through nuget and then change this to 1
#if false

using System;
using System.Diagnostics;
using JetBrains.dotMemoryUnit;
using DInject.Internal;
using NUnit.Framework;
using Assert=DInject.Internal.Assert;
using System.Linq;

namespace DInject.Tests.Other
{
    [TestFixture]
    public partial class TestAllocs : ZenjectUnitTestFixture
    {
        interface IFoo
        {
        }

        public partial class Foo1 : IFoo
        {
        }

        public partial class Foo2 : IFoo
        {
        }

        [Test]
        [DotMemoryUnit(CollectAllocations=true)]
        public void TryStuff()
        {
            Container.Bind<IFoo>().To<Foo1>().AsSingle();

            Container.TryResolve<IFoo>();

            Log.Info("Starting memory check");

            var point1 = dotMemory.Check();

            Container.TryResolve<IFoo>();

            dotMemory.Check(memory =>
                {
                    var traffic = memory.GetTrafficFrom(point1).Where(x => x.Namespace.Like("DInject"));
                    var bytesAllocated = traffic.AllocatedMemory.SizeInBytes;

                    if (bytesAllocated != 0)
                    {
                        Log.Info("Found unnecessary memory allocations ({0} kb) in Container.Resolve. Allocated Types: \n{1}",
                            (float)bytesAllocated / 1024f, traffic.GroupByType().OrderByDescending(x => x.AllocatedMemoryInfo.SizeInBytes)
                            .Select(x => "{0} bytes: ({1})".Fmt(x.AllocatedMemoryInfo.SizeInBytes, x.Type.PrettyName())).Join("\n"));
                    }
                });

            Log.Info("Done memory check");
        }
    }
}

#endif
