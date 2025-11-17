using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Starport
{
    [RequireComponent(typeof(Collider))]
    public class TriggerHelper : MonoBehaviour
    {
        private List<GameObject> _currentGameObjects;

        public event UnityAction<GameObject> OnTriggerEnterEvent, OnTriggerExitEvent;
        public GameObject[] CurrentObjects => GetCurrentObjects();


        private void OnTriggerEnter(Collider other)
        {
            _currentGameObjects ??= new();
            _currentGameObjects.RemoveAll(g => g == null);
            
            if (other == null) return;
            if (_currentGameObjects.Contains(other.gameObject)) return;

            _currentGameObjects.Add(other.gameObject);
            OnTriggerEnterEvent?.Invoke(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            _currentGameObjects ??= new();
            _currentGameObjects.RemoveAll(g => g == null);

            if(other.gameObject != null)
            {
                _currentGameObjects.Remove(other.gameObject);
            }

            OnTriggerExitEvent?.Invoke(other.gameObject);
        }

        private GameObject[] GetCurrentObjects()
        {
            _currentGameObjects ??= new();
            _currentGameObjects.RemoveAll(g => g == null);

            return _currentGameObjects.ToArray();
        }
    }
}
