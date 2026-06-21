using DInject.Internal;
using UnityEngine;

#pragma warning disable 649

namespace DInject.Tests.Bindings.DiContainerMethods
{
    //[CreateAssetMenu(fileName = "Gorp2", menuName = "Test/Gorp2")]
    public partial class Gorp2 : ScriptableObject
    {
        [Inject]
        string _arg;

        public string Arg
        {
            get { return _arg; }
        }

        [Inject]
        public void Initialize()
        {
            Log.Trace("Received arg '{0}' in Gorp", _arg);
        }
    }
}
