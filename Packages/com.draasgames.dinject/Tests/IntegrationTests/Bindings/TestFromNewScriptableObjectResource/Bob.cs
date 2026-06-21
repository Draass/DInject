using UnityEngine;

namespace DInject.Tests.Bindings.FromNewScriptableObjectResource
{
    //[CreateAssetMenu(fileName = "Bob", menuName = "Test/Bob")]
    public partial class Bob : ScriptableObject
    {
        public string Arg
        {
            get;
            private set;
        }

        [Inject]
        public void Construct(string arg)
        {
            Arg = arg;
        }
    }
}

