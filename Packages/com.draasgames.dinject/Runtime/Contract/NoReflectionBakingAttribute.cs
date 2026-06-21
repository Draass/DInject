using System;

namespace DInject
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [Obsolete("This class is no longer used and will be removed in future versions")]
    public class NoReflectionBakingAttribute : Attribute
    {
    }
}
