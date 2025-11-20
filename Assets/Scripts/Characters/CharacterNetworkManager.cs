using Unity.Netcode;
using UnityEngine;

namespace Starport.Characters
{
    [RequireComponent(typeof(NetworkObject), typeof(PlayerStateManager))]
    public class CharacterNetworkManager : NaughtyNetworkBehaviour
    {
        public PlayerStateManager StateManager
        {
            get
            {
                if (_stateManager == null)
                    _stateManager = GetComponent<PlayerStateManager>();
                return _stateManager;
            }
        }
        private PlayerStateManager _stateManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                StateManager.InitializeStateManager();
                StateManager.EnableAndUseCamera();
            }
            else
            {
                StateManager.DisableCamera();
            }
        }
    }
}
