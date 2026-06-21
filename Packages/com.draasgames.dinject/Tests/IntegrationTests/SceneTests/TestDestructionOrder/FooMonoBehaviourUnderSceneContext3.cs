using UnityEngine;

namespace DInject.Tests.TestDestructionOrder
{
    public partial class FooMonoBehaviourUnderSceneContext3 : MonoBehaviour
    {
        public void OnDestroy()
        {
            Debug.Log("Destroyed FooMonoBehaviourUnderSceneContext3");
        }
    }
}
