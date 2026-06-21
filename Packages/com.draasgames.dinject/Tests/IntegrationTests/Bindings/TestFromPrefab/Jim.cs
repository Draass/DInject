using System;
using UnityEngine;

namespace DInject.Tests.Bindings.FromPrefab
{
    public partial class Jim : MonoBehaviour
    {
        [NonSerialized]
        [Inject]
        public Bob Bob;
    }
}
