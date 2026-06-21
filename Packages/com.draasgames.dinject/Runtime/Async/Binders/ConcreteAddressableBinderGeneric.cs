#if EXTENJECT_INCLUDE_ADDRESSABLE_BINDINGS
namespace DInject
{
    [NoReflectionBaking]
    public class ConcreteAddressableBinderGeneric<TContract> : AddressableFromBinderGeneric<TContract, TContract>
    {
        public ConcreteAddressableBinderGeneric(
            DiContainer bindContainer, BindInfo bindInfo,
            BindStatement bindStatement)
            : base(bindContainer, bindInfo, bindStatement)
        {
            bindInfo.ToChoice = ToChoices.Self;
        }

        public AddressableFromBinderGeneric<TContract, TConcrete> To<TConcrete>()
            where TConcrete : TContract
        {
            return new AddressableFromBinderGeneric<TContract, TConcrete>(
                BindContainer, BindInfo, BindStatement);
        }
    }
}
#endif