using Starport.Characters;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.Text;

namespace Starport
{
    [RequireComponent(typeof(Collider))]
    public class InteractableController : NaughtyNetworkBehaviour
    {
        private readonly NetworkVariable<bool> _allowInteract = new(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public bool IsInteractionAllowed() => _allowInteract.Value;

        /// <summary>
        /// Only call this on the server. Clients should not be able to toggle this directly.
        /// </summary>
        public void SetInteractionAllowed(bool allowed)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[InteractableController] SetInteractionAllowed called from non-server. Ignored.");
                return;
            }
            _allowInteract.Value = allowed;
        }

        private readonly NetworkVariable<FixedString128Bytes> _description = new(
            new FixedString128Bytes("Interactable description"),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public string GetDescription() => _description.Value.ToString();

        /// <summary>
        /// Only call this on the server. Clients should not be able to change this directly.
        /// </summary>
        public void SetDescription(string description)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[InteractableController] SetDescription called from non-server. Ignored.");
                return;
            }

            _description.Value = new FixedString128Bytes(description);
        }

        // Client-side callback: only invoked on the client that made the attempt
        public event UnityAction<bool> OnInteractAttemptResultClient;

        // Server-side callback: invoked on the server when an attempt happens (with the character that attempted)
        public event UnityAction<bool, CharacterNetworkManager> OnInteractAttemptResultServer;

        // Simple local lock to prevent the local client from spamming attempts while waiting for a response
        private bool _hasInteractAttempt = false;

        // Optional: per-sender cooldown / pending attempts tracking on server (prevent spam from malicious clients)
        // This keeps a short-lived set of client IDs that currently have a pending request.
        // You can expand this to cooldown timers if needed.
        private readonly HashSet<ulong> _pendingRequests = new();

        /// <summary>
        /// Called by local player code to attempt an interaction.
        /// This sends a ServerRpc to the server and will receive the result via a targeted ClientRpc.
        /// </summary>
        public void AttemptInteract(CharacterNetworkManager character)
        {
            if (character == null || _hasInteractAttempt)
            {
                OnInteractAttemptResultClient?.Invoke(false);
                return;
            }

            // locally prevent multiple attempts while waiting for a response
            _hasInteractAttempt = true;

            Debug.Log($"[InteractableController] {gameObject.name} interact attempt start by client {character.OwnerClientId}");

            // pass the character's NetworkObject as a reference; server will validate ownership
            RequestInteractServerRpc(new NetworkObjectReference(character.NetworkObject), character.OwnerClientId);
        }

        // ---------- SERVER RPC ----------
        // RequireOwnership = false so any client can call this (we validate sender server-side)
        [Rpc(SendTo.Server)]
        private void RequestInteractServerRpc(NetworkObjectReference characterRef, ulong senderClientId)
        {
            Debug.Log($"[InteractableController] Server side {gameObject.name}, received interact request from {senderClientId} ");

            // simple server-side spam prevention: if same client already has pending request, reject
            if (_pendingRequests.Contains(senderClientId))
            {
                Debug.LogWarning($"[InteractableController] Server side {gameObject.name}, client {senderClientId} already has pending request!");
                // send failure back to the single requester
                ResultInteractClientRpc(false, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { senderClientId } }
                });
                return;
            }

            _pendingRequests.Add(senderClientId);

            // Resolve the character reference
            if (!characterRef.TryGet(out NetworkObject charNetObj))
            {
                Debug.LogWarning($"[InteractableController] Server side {gameObject.name}, client {senderClientId} is missing networkObject!");
                SendResultAndClearPending(senderClientId, false);
                return;
            }

            // Validate the character belongs to the sender (prevent spoofing)
            if (charNetObj.OwnerClientId != senderClientId)
            {
                Debug.LogWarning($"[InteractableController] Server side {gameObject.name}, client {senderClientId} spoofed!");
                SendResultAndClearPending(senderClientId, false);
                return;
            }

            // Get the CharacterNetworkManager component (your code expects this)
            CharacterNetworkManager cm = charNetObj.GetComponent<CharacterNetworkManager>();
            if (cm == null)
            {
                Debug.LogWarning($"[InteractableController] Server side {gameObject.name}, client {senderClientId} missing CharacterNetworkManager!");
                SendResultAndClearPending(senderClientId, false);
                return;
            }

            // Server-side validation whether interaction is allowed
            bool canInteract = _allowInteract.Value;

            // fire server-side event for game logic
            OnInteractAttemptResultServer?.Invoke(canInteract, cm);

            // reply only to the requesting client
            SendResultAndClearPending(senderClientId, canInteract);
        }

        // helper to reply to single client and clear pending flag
        private void SendResultAndClearPending(ulong targetClientId, bool success)
        {
            Debug.Log($"[InteractableController] Server side {gameObject.name}, sending {success} result to {targetClientId}");
            // send targeted ClientRpc
            ResultInteractClientRpc(success, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
            });

            // clear pending marker
            _pendingRequests.Remove(targetClientId);
        }

        // ---------- CLIENT RPC ----------
        // Result is sent only to the requester (see SendResultAndClearPending)
        [ClientRpc]
        private void ResultInteractClientRpc(bool success, ClientRpcParams clientRpcParams = default)
        {
            // run on the client that requested interaction
            _hasInteractAttempt = false;
            OnInteractAttemptResultClient?.Invoke(success);

            Debug.Log($"[InteractableController] {gameObject.name} interact attempt: {success}");
        }

        // Optional: tidy up pending requests when server despawns this object
        public override void OnNetworkDespawn()
        {
            _pendingRequests.Clear();
            base.OnNetworkDespawn();
        }
    }
}
