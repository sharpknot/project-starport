using UnityEngine;
using Starport.Characters;

namespace Starport.PlayerState
{
    [CreateAssetMenu(fileName = "PSBaseLook", menuName = "Player State/Base/Look")]
    public class PSBaseLook : PlayerStateBase
    {
        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);
            UpdateLook();
        }
    }
}
