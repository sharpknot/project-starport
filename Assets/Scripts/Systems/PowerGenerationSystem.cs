using Starport.Subsystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Systems
{
    public class PowerGenerationSystem : SystemBase
    {
        [field: SerializeField]
        public FluidPipeSubsystem[] PipeSubsystems { get; private set; }
        public event UnityAction<FluidPipeSubsystem[]> OnPipeSubsystemUpdated;

        [field: SerializeField]
        public FluidReservoirSubsystem[] ReservoirSubsystems { get; private set;  }
        public event UnityAction<FluidReservoirSubsystem[]> OnReservoirSubsystemsUpdated;

        [field: SerializeField]
        public ReactorSubsystem[] ReactorSubsystems { get; private set; }
        public event UnityAction<ReactorSubsystem[]> OnReactorSubsystemsUpdated;

        [field: SerializeField]
        public GeneratorSubsystem[] GeneratorSubsystems { get; private set; }
        public event UnityAction<GeneratorSubsystem[]> OnGeneratorSubsystemsUpdated;

        [field: SerializeField]
        public PowerRegulatorSubsystem[] RegulatorSubsystems { get; private set; }
        public event UnityAction<PowerRegulatorSubsystem[]> OnRegulatorSubsystemsUpdated;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            StartCoroutine(InitializeEvents());
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();

            UnsubscribePipeEvents();
            UnsubscribeReservoirEvents();
            UnsubscribeReactorEvents();
            UnsubscribeGeneratorEvents();
            UnsubscribeRegulatorEvents();

            base.OnNetworkDespawn();
        }

        public override void InitializeSystem(float completionAmount = 1)
        {
            base.InitializeSystem(completionAmount);

            List<SubsystemBase> completeSubsystems = new(GetCurrentValidSubsystems());
            if (completeSubsystems.Count <= 0) return;

            List <FluidReservoirSubsystem> reservoirSubsystems = GetReservoirSubystems(ref completeSubsystems);
            List<ReactorSubsystem> reactorSubsystems = GetReactorSubystems(ref completeSubsystems);

            float incompletePercent = 1f - Mathf.Clamp01(completionAmount);
            int incompleteCount = 0;

            if(incompletePercent >= 1f)
            {
                // Every system is incomplete
                incompleteCount = completeSubsystems.Count;
            }
            else if(incompletePercent < 1f && incompletePercent > 0f)
            {
                int toBeIncomplete = (int)(incompletePercent * (float)completeSubsystems.Count);
                incompleteCount = Mathf.Clamp(toBeIncomplete, 1, completeSubsystems.Count);
            }

            List<SubsystemBase> incompleteSubsystems = new();
            for (int i = 0; i < incompleteCount; i++)
            {
                int randomIndex = Random.Range(0, completeSubsystems.Count);
                incompleteSubsystems.Add(completeSubsystems[randomIndex]);
                completeSubsystems.RemoveAt(randomIndex);
            }

            foreach (SubsystemBase subsystem in completeSubsystems)
            {
                if (subsystem != null) 
                    subsystem.InitializeSubSystem(1f);
            }

            foreach(SubsystemBase subsystem in incompleteSubsystems)
            {
                if (subsystem != null)
                    subsystem.InitializeSubSystem(Random.Range(0f, 0.5f));
            }

            // Handle Reservoirs
            SetSubsystemsComplete(reservoirSubsystems.ToArray(), completionAmount);
            SetSubsystemsComplete(reactorSubsystems.ToArray(), completionAmount);
        }

        public override void Deinitialize()
        {
            foreach(var s in GetCurrentValidSubsystems())
            {
                s.Deinitialize();
            }

            base.Deinitialize();
        }

        private void SetSubsystemsComplete(SubsystemBase[] subsystems, float completionAmount)
        {
            bool completeRes = GetCompleted(completionAmount);
            foreach (var res in subsystems)
            {
                float final = 1f;
                if (!completeRes) final = Random.Range(0f, 0.5f);
                res.InitializeSubSystem(final);

                Debug.Log($"[SetSubsystemsComplete] {res.gameObject.name} completion amt {final}");
            }
        }

        private bool GetCompleted(float completionAmount)
        {
            float netCompletion = Mathf.Clamp01(completionAmount);

            if (netCompletion >= 1f) return true;
            if (netCompletion <= 0f) return false;

            return Random.Range(0f,1f) < netCompletion;
        }

        private List<FluidReservoirSubsystem> GetReservoirSubystems(ref List<SubsystemBase> validSubsystems)
        {
            List<FluidReservoirSubsystem> result = new();
            if(validSubsystems == null)
                return result;

            int index = 0;
            while(index < validSubsystems.Count)
            {
                SubsystemBase s = validSubsystems[index];
                if(s ==null)
                {
                    index++;
                    continue;
                }

                FluidReservoirSubsystem f = s as FluidReservoirSubsystem;
                if (f == null)
                {
                    index++;
                    continue;
                }

                result.Add(f);
                validSubsystems.RemoveAt(index);
            }

            return result;
        }

        private List<ReactorSubsystem> GetReactorSubystems(ref List<SubsystemBase> validSubsystems)
        {
            List<ReactorSubsystem> result = new();
            if (validSubsystems == null)
                return result;

            int index = 0;
            while (index < validSubsystems.Count)
            {
                SubsystemBase s = validSubsystems[index];
                if (s == null)
                {
                    index++;
                    continue;
                }

                ReactorSubsystem f = s as ReactorSubsystem;
                if (f == null)
                {
                    index++;
                    continue;
                }

                result.Add(f);
                validSubsystems.RemoveAt(index);
            }

            return result;
        }

        private SubsystemBase[] GetCurrentValidSubsystems()
        {
            List<SubsystemBase> subsystems = new();
            subsystems.AddRange(GetValidSubsystems(PipeSubsystems));
            subsystems.AddRange(GetValidSubsystems(ReservoirSubsystems));
            subsystems.AddRange(GetValidSubsystems(GeneratorSubsystems));
            subsystems.AddRange(GetValidSubsystems(RegulatorSubsystems));
            subsystems.AddRange(GetValidSubsystems(ReactorSubsystems));

            return subsystems.ToArray();
        }

        private SubsystemBase[] GetValidSubsystems(IEnumerable subSystems)
        {
            List<SubsystemBase> result = new();
            if(subSystems == null) return result.ToArray();
            foreach(SubsystemBase subsystem in subSystems)
            {
                if(subsystem == null) continue;
                if (!subsystem.NetworkObject.IsSpawned) continue;
                if(result.Contains(subsystem)) continue;

                result.Add(subsystem);
            }

            return result.ToArray();
        }

        private IEnumerator InitializeEvents()
        {
            while (!AreSubsystemsSpawned(PipeSubsystems)) yield return null;
            SubscribePipeEvents();
            OnPipeSubsystemUpdated?.Invoke(PipeSubsystems);

            while (!AreSubsystemsSpawned(ReservoirSubsystems)) yield return null;
            SubscribeReservoirEvents();
            OnReservoirSubsystemsUpdated?.Invoke(ReservoirSubsystems);

            while (!AreSubsystemsSpawned(ReactorSubsystems)) yield return null;
            SubscribeReactorEvents();            
            OnReactorSubsystemsUpdated?.Invoke(ReactorSubsystems);

            while(!AreSubsystemsSpawned(GeneratorSubsystems)) yield return null;
            SubscribeGeneratorEvents();
            OnGeneratorSubsystemsUpdated?.Invoke(GeneratorSubsystems);

            while (!AreSubsystemsSpawned(RegulatorSubsystems)) yield return null;
            SubscribeRegulatorEvents();
            OnRegulatorSubsystemsUpdated?.Invoke(RegulatorSubsystems);

            UpdateActivationState();
        }

        private bool AreSubsystemsSpawned(SubsystemBase[] subsystems)
        {
            if (subsystems == null) return true;
            foreach(SubsystemBase subsystem in subsystems)
            {
                if (subsystem == null) continue;
                if(!subsystem.NetworkObject.IsSpawned)
                    return false;
            }

            return true;
        }

        private void SubscribePipeEvents()
        {
            if(PipeSubsystems == null) return;

            List<FluidPipeSubsystem> result = new();
            foreach(var p in PipeSubsystems)
            {
                if (p == null) continue;
                if (result.Contains(p)) continue;
                p.OnCurrentFixAmountUpdate += OnPipeFixAmountUpdate;
                p.OnLocallyActiveUpdate += OnPipeActiveUpdate;
                p.OnCurrentlyActiveUpdate += OnPipeActiveUpdate;
            }
        }
        private void UnsubscribePipeEvents()
        {
            if (PipeSubsystems == null) return;

            foreach (var p in PipeSubsystems)
            {
                if (p == null) continue;
                p.OnCurrentFixAmountUpdate -= OnPipeFixAmountUpdate;
                p.OnLocallyActiveUpdate -= OnPipeActiveUpdate;
                p.OnCurrentlyActiveUpdate -= OnPipeActiveUpdate;
            }
        }
        private void OnPipeFixAmountUpdate(float fixAmountUpdate)
        {
            OnPipeSubsystemUpdated?.Invoke(PipeSubsystems);
            UpdateActivationState();
        }
        private void OnPipeActiveUpdate(bool active)
        {
            OnPipeSubsystemUpdated?.Invoke(PipeSubsystems);
            UpdateActivationState();
        }

        private void SubscribeReservoirEvents()
        {
            if (ReservoirSubsystems == null) return;

            List<FluidReservoirSubsystem> result = new();
            foreach (var p in ReservoirSubsystems)
            {
                if (p == null) continue;
                if (result.Contains(p)) continue;
                p.OnLocallyActiveUpdate += OnReservoirActiveUpdate;
                p.OnCapacityUpdated += OnReservoirCapacityUpdate;
                p.OnCurrentlyActiveUpdate += OnReservoirActiveUpdate;
            }
        }
        private void UnsubscribeReservoirEvents()
        {
            if (ReservoirSubsystems == null) return;

            foreach (var p in ReservoirSubsystems)
            {
                if (p == null) continue;
                p.OnLocallyActiveUpdate -= OnReservoirActiveUpdate;
                p.OnCapacityUpdated -= OnReservoirCapacityUpdate;
                p.OnCurrentlyActiveUpdate -= OnReservoirActiveUpdate;
            }
        }
        private void OnReservoirActiveUpdate(bool active)
        {
            OnReservoirSubsystemsUpdated?.Invoke(ReservoirSubsystems);
            UpdateActivationState();
        }
        private void OnReservoirCapacityUpdate(float capacity, bool active)
        {
            OnReservoirSubsystemsUpdated?.Invoke(ReservoirSubsystems);
            UpdateActivationState();
        }

        private void OnReactorActiveUpdate(bool active)
        {
            OnReactorSubsystemsUpdated?.Invoke(ReactorSubsystems);
            UpdateActivationState();
        }
        private void OnReactorEnergyUpdate(float current, float min)
        {
            OnReactorSubsystemsUpdated?.Invoke(ReactorSubsystems);
            UpdateActivationState();
        }
        private void SubscribeReactorEvents()
        {
            if (ReactorSubsystems == null) return;

            List<ReactorSubsystem> result = new();
            foreach (var p in ReactorSubsystems)
            {
                if (p == null) continue;
                if (result.Contains(p)) continue;
                p.OnLocallyActiveUpdate += OnReactorActiveUpdate;
                p.OnEnergyUpdate += OnReactorEnergyUpdate;
                p.OnCurrentlyActiveUpdate += OnReactorActiveUpdate;
            }
        }
        private void UnsubscribeReactorEvents()
        {
            if (ReactorSubsystems == null) return;

            foreach (var p in ReactorSubsystems)
            {
                if (p == null) continue;
                p.OnLocallyActiveUpdate -= OnReactorActiveUpdate;
                p.OnEnergyUpdate -= OnReactorEnergyUpdate;
                p.OnCurrentlyActiveUpdate -= OnReactorActiveUpdate;
            }
        }

        private void OnGeneratorActiveUpdate(bool active)
        {
            OnGeneratorSubsystemsUpdated?.Invoke(GeneratorSubsystems);
            UpdateActivationState();
        }
        private void OnGeneratorFixUpdate(float fix)
        {
            OnGeneratorSubsystemsUpdated?.Invoke(GeneratorSubsystems);
            UpdateActivationState();
        }
        private void SubscribeGeneratorEvents()
        {
            if (GeneratorSubsystems == null) return;

            List<GeneratorSubsystem> result = new();
            foreach (var p in GeneratorSubsystems)
            {
                if (p == null) continue;
                if (result.Contains(p)) continue;
                p.OnLocallyActiveUpdate += OnGeneratorActiveUpdate;
                p.OnCurrentFixAmountUpdate += OnGeneratorFixUpdate;
                p.OnCurrentlyActiveUpdate += OnGeneratorActiveUpdate;
            }
        }
        private void UnsubscribeGeneratorEvents()
        {
            if (GeneratorSubsystems == null) return;

            foreach (var p in GeneratorSubsystems)
            {
                if (p == null) continue;
                p.OnLocallyActiveUpdate -= OnGeneratorActiveUpdate;
                p.OnCurrentFixAmountUpdate -= OnGeneratorFixUpdate;
                p.OnCurrentlyActiveUpdate -= OnGeneratorActiveUpdate;
            }
        }

        private void OnRegulatorActiveUpdate(bool active)
        {
            OnGeneratorSubsystemsUpdated?.Invoke(GeneratorSubsystems);
            UpdateActivationState();
        }
        private void SubscribeRegulatorEvents()
        {
            if (RegulatorSubsystems == null) return;

            List<PowerRegulatorSubsystem> result = new();
            foreach (var p in RegulatorSubsystems)
            {
                if (p == null) continue;
                if (result.Contains(p)) continue;
                p.OnLocallyActiveUpdate += OnRegulatorActiveUpdate;
                p.OnCurrentlyActiveUpdate += OnRegulatorActiveUpdate;
            }
        }
        private void UnsubscribeRegulatorEvents()
        {
            if (RegulatorSubsystems == null) return;

            foreach (var p in RegulatorSubsystems)
            {
                if (p == null) continue;
                p.OnLocallyActiveUpdate -= OnRegulatorActiveUpdate;
                p.OnCurrentlyActiveUpdate -= OnRegulatorActiveUpdate;
            }
        }

        private void UpdateActivationState()
        {
            if (!IsServer) return;

            if(
                !AreSubsystemsActive(PipeSubsystems)
                || !AreSubsystemsActive(ReservoirSubsystems)
                || !AreSubsystemsActive(ReactorSubsystems)
                || !AreSubsystemsActive(GeneratorSubsystems)
                || !AreSubsystemsActive(RegulatorSubsystems)
                )
            {
                IsCurrentlyActive.Value = false;
                return;
            }

            IsCurrentlyActive.Value = true;
        }

        private bool AreSubsystemsActive(SubsystemBase[] subsystems)
        {
            if (subsystems == null) return true;
            foreach (var subsystem in subsystems)
            {
                if (subsystem == null) continue;
                if (subsystem.IsCurrentlyActive) continue;
                return false;
            }

            return true;
        }
    }
}
