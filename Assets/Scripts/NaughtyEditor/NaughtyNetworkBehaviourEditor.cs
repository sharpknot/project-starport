using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;

namespace Starport
{
    [CustomEditor(typeof(NaughtyNetworkBehaviour), true)]
    public class NaughtyNetworkBehaviourEditor : NaughtyInspector
    {
    }
}
