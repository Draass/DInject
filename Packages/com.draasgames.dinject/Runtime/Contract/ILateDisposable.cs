using System;

namespace DInject
{
    public interface ILateDisposable
    {
        void LateDispose();
    }
}
