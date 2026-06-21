using System;
using UnityEngine;

namespace DInject.Tests.Bindings.FromPrefabResource
{
    public partial class Bob : MonoBehaviour
    {
        [NonSerialized]
        [Inject]
        public Jim Jim;
    }
}
