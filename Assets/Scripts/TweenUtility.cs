using DG.Tweening;
using UnityEngine;

namespace Starport
{
    public static class TweenUtility
    {
        public static void KillAndDestroySequence(ref Sequence sequence)
        {
            if(sequence == null) return;

            sequence.Kill();
            sequence = null;
        }
    }
}
