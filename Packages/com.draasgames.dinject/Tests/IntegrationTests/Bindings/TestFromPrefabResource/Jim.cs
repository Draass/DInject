using System;
using UnityEngine;

namespace DInject.Tests.Bindings.FromPrefabResource
{
    public partial class Jim : MonoBehaviour
    {
        [NonSerialized]
        [Inject]
        public Bob Bob;
    }
}
