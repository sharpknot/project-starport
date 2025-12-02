using UnityEngine;
using Starport.Tools;
using UnityEngine.Events;
using NaughtyAttributes;

namespace Starport.Characters
{
    public class CharacterToolsHandler : MonoBehaviour
    {
        [SerializeField] private int _toolCount = 2;
        [field: SerializeField] 
        public Transform ToolHoldPosition { get; private set; }

        public ToolBase[] Tools
        {
            get
            {
                _tools ??= new ToolBase[_toolCount];
                return _tools;
            }
        }
        private ToolBase[] _tools = null;

        public static event UnityAction<int, ToolBase[]> OnToolsUpdate;

        [field: SerializeField, ReadOnly]
        public int CurrentToolIndex { get; private set; } = 0;

        public ToolBase CurrentTool => Tools[CurrentToolIndex];

        private bool _unholsterCurrentTool = true;

        private void Start()
        {
            _ = Tools.Length;
            Debug.Log($"[CharacterToolsHandler] started, tools length {Tools.Length}");
            OnToolsUpdate?.Invoke(CurrentToolIndex, Tools);
        }

        private void OnValidate()
        {
            _toolCount = Mathf.Max(1, _toolCount);
        }

        public void SetUnholsterCurrentTool(bool unholsterCurrentTool)
        {
            _unholsterCurrentTool = unholsterCurrentTool;
            if (CurrentTool == null) return;

            if(_unholsterCurrentTool)
            {
                CurrentTool.Unholster();
            }
            else
            {
                CurrentTool.Holster();
            }
        }

        public void Equip(ToolBase tool, int toolIndex, PlayerStateManager stateManager)
        {
            if (toolIndex < 0 || toolIndex >= Tools.Length)
            {
                Debug.LogError($"[CharacterToolsHandler] Equip failed: invalid toolIndex ({toolIndex}). Tools count {Tools.Length}");
                return;
            }

            // Holster if current tool
            if(toolIndex == CurrentToolIndex)
            {
                if (Tools[toolIndex] != null)
                {
                    Debug.Log($"[CharacterToolsHandler] Equip: Holstering current indexed ({toolIndex}), {Tools[toolIndex].ToolName}");
                    Tools[toolIndex].Holster();
                }  
            }

            // Unequip and destroy
            if(Tools[toolIndex] != null)
            {
                Tools[toolIndex].Unequip();

                Debug.Log($"[CharacterToolsHandler] Equip: Unequipping current indexed ({toolIndex}), {Tools[toolIndex].ToolName} to be destroyed");

                GameObject g = Tools[toolIndex].gameObject;
                Destroy(g);
            }

            Tools[toolIndex] = null;

            if(tool == null)
            {
                Debug.Log($"[CharacterToolsHandler] Equip: New tool is null, completed");
                OnToolsUpdate?.Invoke(CurrentToolIndex, Tools);
                return;
            }

            if (stateManager == null)
            {
                Debug.LogError($"[CharacterToolsHandler] Equip failed: null state manager");
                OnToolsUpdate?.Invoke(CurrentToolIndex, Tools);
                return;
            }

            Debug.Log($"[CharacterToolsHandler] Equip: Equipping new tool {tool.ToolName}");
            tool.Equip(stateManager);
            Tools[toolIndex] = tool;

            if(CurrentToolIndex == toolIndex)
            {
                Debug.Log($"[CharacterToolsHandler] Equip: Unholstering new tool {tool.ToolName}, current index ({CurrentToolIndex})");
                Tools[toolIndex].Unholster();

                if(!_unholsterCurrentTool)
                {
                    Debug.Log($"[CharacterToolsHandler] Equip: Holstering new tool {tool.ToolName}, current index ({CurrentToolIndex}) due to current tool unholster setting");
                    Tools[toolIndex].Holster();
                }
            }
            else
            {
                Debug.Log($"[CharacterToolsHandler] Equip: Holstering new tool {tool.ToolName}, current index ({CurrentToolIndex})");
                Tools[toolIndex].Holster();
            }

            

            Debug.Log($"[CharacterToolsHandler] Equip: Equipping new tool {tool.ToolName} at index {toolIndex} complete!");
            OnToolsUpdate?.Invoke(CurrentToolIndex, Tools);
        }

        public void SetCurrentTool(int toolIndex)
        {
            if(toolIndex < 0 || toolIndex >= Tools.Length)
            {
                Debug.LogError($"[CharacterToolsHandler] SetCurrentTool failed: invalid toolIndex ({toolIndex}). Tools count {Tools.Length}");
                return;
            }

            if(toolIndex == CurrentToolIndex)
            {
                Debug.LogWarning($"[CharacterToolsHandler] SetCurrentTool failed: New tool index {toolIndex} is the same of the current tool index");
                return;
            }

            if (Tools[CurrentToolIndex] != null)
            {
                Debug.Log($"[CharacterToolsHandler] SetCurrentTool: Holstering current indexed ({toolIndex}), {Tools[CurrentToolIndex].ToolName}");
                Tools[CurrentToolIndex].Holster();
            }

            CurrentToolIndex = toolIndex;
            Debug.Log($"[CharacterToolsHandler] SetCurrentTool: new current tool index {CurrentToolIndex}");

            if (Tools[CurrentToolIndex] == null)
            {
                Debug.Log($"[CharacterToolsHandler] SetCurrentTool: New index is null, failed to unholster");
                OnToolsUpdate?.Invoke(CurrentToolIndex, Tools);
                return;
            }

            Tools[CurrentToolIndex].Unholster();
            Debug.Log($"[CharacterToolsHandler] SetCurrentTool: Tool ({Tools[CurrentToolIndex].ToolName}) at index {CurrentToolIndex} unholstered");

            if(!_unholsterCurrentTool)
            {
                Tools[CurrentToolIndex].Holster();
                Debug.Log($"[CharacterToolsHandler] SetCurrentTool: Tool ({Tools[CurrentToolIndex].ToolName}) at index {CurrentToolIndex} reholstered due to current tool unholster setting");
            }

            OnToolsUpdate?.Invoke(CurrentToolIndex, Tools);
        }

        public void PrimaryAction()
        {
            if (CurrentTool == null) return;
            CurrentTool.PrimaryAction();
        }

        public void SecondaryAction()
        {
            if (CurrentTool == null) return;
            CurrentTool.SecondaryAction();
        }

        public void PrimaryPressedAction(float deltaTime)
        {
            if (CurrentTool == null) return;
            CurrentTool.PrimaryPressedAction(deltaTime);
        }

        public void SecondaryPressedAction(float deltaTime)
        {
            if (CurrentTool == null) return;
            CurrentTool.SecondaryPressedAction(deltaTime);
        }

    }
}
