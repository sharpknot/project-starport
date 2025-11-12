using Unity.Netcode;
using UnityEngine;

namespace Starport
{
    public class GameSessionManager : MonoBehaviour
    {
        private RelayManager _relayManager;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _relayManager = RelayManager.Instance;
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
