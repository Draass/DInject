using System;
using UnityEngine;

namespace DInject.Tests.Bindings.FromPrefab
{
    public partial class Bob : MonoBehaviour
    {
        [NonSerialized]
        [Inject]
        public Jim Jim;
    }
}
