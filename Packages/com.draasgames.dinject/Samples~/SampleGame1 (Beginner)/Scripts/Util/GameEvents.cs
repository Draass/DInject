using System;

namespace DInject.Asteroids
{
    // Lightweight event hub used in place of the original SignalBus-based ShipCrashedSignal.
    // This DInject sample omits the optional Signals subsystem; a plain injected event reproduces
    // the same fire/subscribe flow (bound AsSingle in GameInstaller, injected where needed).
    public partial class GameEvents
    {
        public event Action ShipCrashed;

        public void FireShipCrashed()
        {
            ShipCrashed?.Invoke();
        }
    }
}
