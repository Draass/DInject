using UnityEngine;
using Zenject;

namespace Zenject.Tests.CodeGenPlayMode
{
    // Leaf service bound as an instance (never constructed via TypeAnalyzer).
    public class PlayService
    {
    }

    // A real runtime MonoBehaviour (non-Editor assembly) so it can be AddComponent'd onto a
    // GameObject. The DInject generator emits its __zenCreateInjectTypeInfo (null factory, since
    // Unity owns Component construction) + the [Inject] field setter.
    public partial class PlayConsumerMono : MonoBehaviour
    {
        [Inject] public PlayService Service;
    }
}
