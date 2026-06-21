using System;
using DInject.Internal;

namespace DInject
{
    public static class ProviderUtil
    {
        public static Type GetTypeToInstantiate(Type contractType, Type concreteType)
        {
#if !(UNITY_WSA && ENABLE_DOTNET)
            // TODO: Is it possible to do this on WSA?

            if (concreteType.IsOpenGenericType())
            {
                return concreteType.MakeGenericType(contractType.GetGenericArguments());
            }
#endif

            Assert.DerivesFromOrEqual(concreteType, contractType);
            return concreteType;
        }
    }
}

