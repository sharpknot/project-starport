using Starport.Tools;
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

        [SerializeField]
        private ToolBase _repairTool, _pickupTool;

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

            EquipPlayerTool(_repairTool, 0);
            EquipPlayerTool(_pickupTool, 1);
        }

        private void EquipPlayerTool(ToolBase tool, int index)
        {
            if (StateManager.ToolsHandler == null) return;
            if(tool == null) return;

            GameObject g = Instantiate(tool.gameObject, transform.position, Quaternion.identity);
            ToolBase t = g.GetComponent<ToolBase>();

            StateManager.ToolsHandler.Equip(t, index, StateManager);
        }
    }
}
