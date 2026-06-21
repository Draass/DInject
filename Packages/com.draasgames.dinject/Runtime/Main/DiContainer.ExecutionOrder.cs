using System;
using DInject.Internal;
using DInject.Internal.Util;

namespace DInject
{
    public partial class DiContainer
    {
        public void BindExecutionOrder<T>(int order)
        {
            BindExecutionOrder(typeof(T), order);
        }

        public void BindExecutionOrder(Type type, int order)
        {
            Assert.That(
                type.DerivesFrom<ITickable>() || type.DerivesFrom<IInitializable>() ||
                type.DerivesFrom<IDisposable>() || type.DerivesFrom<ILateDisposable>() ||
                type.DerivesFrom<IFixedTickable>() || type.DerivesFrom<ILateTickable>() ||
                type.DerivesFrom<IPoolable>(),
                "Expected type '{0}' to derive from one or more of the following interfaces: ITickable, IInitializable, ILateTickable, IFixedTickable, IDisposable, ILateDisposable",
                type);

            if (type.DerivesFrom<ITickable>())
            {
                BindTickableExecutionOrder(type, order);
            }

            if (type.DerivesFrom<IInitializable>())
            {
                BindInitializableExecutionOrder(type, order);
            }

            if (type.DerivesFrom<IDisposable>())
            {
                BindDisposableExecutionOrder(type, order);
            }

            if (type.DerivesFrom<ILateDisposable>())
            {
                BindLateDisposableExecutionOrder(type, order);
            }

            if (type.DerivesFrom<IFixedTickable>())
            {
                BindFixedTickableExecutionOrder(type, order);
            }

            if (type.DerivesFrom<ILateTickable>())
            {
                BindLateTickableExecutionOrder(type, order);
            }

            if (type.DerivesFrom<IPoolable>())
            {
                BindPoolableExecutionOrder(type, order);
            }
        }

        public CopyNonLazyBinder BindTickableExecutionOrder<T>(int order)
            where T : ITickable
        {
            return BindTickableExecutionOrder(typeof(T), order);
        }

        public CopyNonLazyBinder BindTickableExecutionOrder(Type type, int order)
        {
            Assert.That(type.DerivesFrom<ITickable>(),
                "Expected type '{0}' to derive from ITickable", type);

            return BindInstance(
                ValuePair.New(type, order)).WhenInjectedInto<TickableManager>();
        }

        public CopyNonLazyBinder BindInitializableExecutionOrder<T>(int order)
            where T : IInitializable
        {
            return BindInitializableExecutionOrder(typeof(T), order);
        }

        public CopyNonLazyBinder BindInitializableExecutionOrder(Type type, int order)
        {
            Assert.That(type.DerivesFrom<IInitializable>(),
                "Expected type '{0}' to derive from IInitializable", type);

            return BindInstance(
                ValuePair.New(type, order)).WhenInjectedInto<InitializableManager>();
        }

        public CopyNonLazyBinder BindDisposableExecutionOrder<T>(int order)
            where T : IDisposable
        {
            return BindDisposableExecutionOrder(typeof(T), order);
        }

        public CopyNonLazyBinder BindLateDisposableExecutionOrder<T>(int order)
            where T : ILateDisposable
        {
            return BindLateDisposableExecutionOrder(typeof(T), order);
        }

        public CopyNonLazyBinder BindDisposableExecutionOrder(Type type, int order)
        {
            Assert.That(type.DerivesFrom<IDisposable>(),
                "Expected type '{0}' to derive from IDisposable", type);

            return BindInstance(
                ValuePair.New(type, order)).WhenInjectedInto<DisposableManager>();
        }

        public CopyNonLazyBinder BindLateDisposableExecutionOrder(Type type, int order)
        {
            Assert.That(type.DerivesFrom<ILateDisposable>(),
                "Expected type '{0}' to derive from ILateDisposable", type);

            return BindInstance(
                ValuePair.New(type, order)).WithId("Late").WhenInjectedInto<DisposableManager>();
        }

        public CopyNonLazyBinder BindFixedTickableExecutionOrder<T>(int order)
            where T : IFixedTickable
        {
            return BindFixedTickableExecutionOrder(typeof(T), order);
        }

        public CopyNonLazyBinder BindFixedTickableExecutionOrder(Type type, int order)
        {
            Assert.That(type.DerivesFrom<IFixedTickable>(),
                "Expected type '{0}' to derive from IFixedTickable", type);

            return Bind<ValuePair<Type, int>>().WithId("Fixed")
                .FromInstance(ValuePair.New(type, order)).WhenInjectedInto<TickableManager>();
        }

        public CopyNonLazyBinder BindLateTickableExecutionOrder<T>(int order)
            where T : ILateTickable
        {
            return BindLateTickableExecutionOrder(typeof(T), order);
        }

        public CopyNonLazyBinder BindLateTickableExecutionOrder(Type type, int order)
        {
            Assert.That(type.DerivesFrom<ILateTickable>(),
                "Expected type '{0}' to derive from ILateTickable", type);

            return Bind<ValuePair<Type, int>>().WithId("Late")
                .FromInstance(ValuePair.New(type, order)).WhenInjectedInto<TickableManager>();
        }

        public CopyNonLazyBinder BindPoolableExecutionOrder<T>(int order)
            where T : IPoolable
        {
            return BindPoolableExecutionOrder(typeof(T), order);
        }

        public CopyNonLazyBinder BindPoolableExecutionOrder(Type type, int order)
        {
            Assert.That(type.DerivesFrom<IPoolable>(),
                "Expected type '{0}' to derive from IPoolable", type);

            return Bind<ValuePair<Type, int>>()
                .FromInstance(ValuePair.New(type, order)).WhenInjectedInto<PoolableManager>();
        }
    }
}