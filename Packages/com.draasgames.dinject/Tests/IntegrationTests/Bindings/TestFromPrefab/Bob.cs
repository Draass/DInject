using System;
using UnityEngine;

namespace DInject.Tests.Bindings.FromPrefab
{
    public class Bob : MonoBehaviour
    {
        [NonSerialized]
        [Inject]
        public Jim Jim;
    }
}
