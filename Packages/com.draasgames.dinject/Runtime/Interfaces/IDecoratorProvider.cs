using System.Collections.Generic;

namespace DInject.Internal
{
    public interface IDecoratorProvider
    {
        void GetAllInstances(
            IProvider provider, InjectContext context, List<object> buffer);
    }
}
