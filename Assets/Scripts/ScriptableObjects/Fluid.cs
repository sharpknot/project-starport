using UnityEngine;

namespace Starport
{
    [CreateAssetMenu(fileName = "Fluid", menuName = "Scriptable Objects/Fluid")]
    public class Fluid : ScriptableObject
    {
        [field:SerializeField] public string FluidName { get; private set; }
    }
}
