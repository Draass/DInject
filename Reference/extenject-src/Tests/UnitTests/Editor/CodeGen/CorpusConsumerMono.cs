using UnityEngine;
using Zenject;

namespace Zenject.Tests.CodeGen
{
    // Realistic consumer: a MonoBehaviour that injects a service built via a generated constructor
    // factory (CorpusGreeter) plus a leaf service. Own file so the file name matches the type name.
    public partial class CorpusConsumerMono : MonoBehaviour
    {
        [Inject] public CorpusGreeter Greeter;
        [Inject] public CorpusSimpleService Service;
    }
}
