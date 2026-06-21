using UnityEngine;

namespace DInject.Tests.Factories.BindFactory
{
    //[CreateAssetMenu(fileName = "Bar", menuName = "Installers/Bar")]
    public partial class Bar : ScriptableObject
    {
        public partial class Factory : PlaceholderFactory<Bar>
        {
        }
    }
}

