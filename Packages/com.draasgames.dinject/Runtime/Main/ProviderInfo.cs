namespace DInject
{
    internal class ProviderInfo
    {
        public ProviderInfo(
            IProvider provider, BindingCondition condition, bool nonLazy, DiContainer container)
        {
            Provider = provider;
            Condition = condition;
            NonLazy = nonLazy;
            Container = container;
        }

        public readonly DiContainer Container;
        public readonly bool NonLazy;
        public readonly IProvider Provider;
        public readonly BindingCondition Condition;
    }
}