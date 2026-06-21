using UnityEngine;

namespace DInject.Tests.Factories.BindFactoryOne
{
    //[CreateAssetMenu(fileName = "Bar", menuName = "Installers/Bar")]
    public partial class Bar : ScriptableObject
    {
        [Inject]
        public void Init(string value)
        {
            Value = value;
        }

        public string Value
        {
            get;
            private set;
        }

        public partial class Factory : PlaceholderFactory<string, Bar>
        {
        }
    }
}

