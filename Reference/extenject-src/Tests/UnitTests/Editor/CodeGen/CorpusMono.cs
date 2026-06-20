using UnityEngine;
using Zenject;

namespace Zenject.Tests.CodeGen
{
    // In its own file so the file name matches the MonoBehaviour type name (Unity requirement).
    // Component-derived: the reflection oracle must produce a NULL constructor factory and only
    // field/property/method injection metadata (Unity owns Component construction).
    public partial class CorpusMono : MonoBehaviour
    {
        [Inject] public CorpusSimpleService Dep;
    }
}
