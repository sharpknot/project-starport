using System.Collections.Generic;
using System.Collections;
using Starport.Characters;
using Starport.Pickups;
using UnityEngine;

namespace Starport
{
    [RequireComponent (typeof (Collider))]
    public class BoundaryResetter : NaughtyNetworkBehaviour
    {
        [SerializeField]
        private Transform[] _playerSpawns;



        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;
            if (!IsSpawned) return;

            if (other == null) return;

            PickupController pickup = other.GetComponent<PickupController>();
            if (pickup != null)
            {
                pickup.NetworkObject.Despawn(true);
                return;
            }

            CharacterNetworkManager cm = other.GetComponent<CharacterNetworkManager>();
            if(cm == null) return;

            CharacterMotionController motion = cm.GetComponent<CharacterMotionController>();
            if (motion == null) return;

            List<Vector3> validPos = new();
            if(_playerSpawns != null)
            {
                foreach (Transform t in _playerSpawns)
                {
                    if(t == null) continue;
                    validPos.Add(t.position);
                }
            }

            if(validPos.Count <= 0)
            {
                return;
            }

            motion.TeleportInstant(validPos[Random.Range(0, validPos.Count)]);
        }
    }
}
