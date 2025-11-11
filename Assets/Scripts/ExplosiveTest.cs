using UnityEngine;

namespace Starport
{
    public class ExplosiveTest : MonoBehaviour
    {
        private float _countDownDuration = 5f;
        private float _currentCountdown = 0f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            if(_currentCountdown > 0f)
            {
                _countDownDuration -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_currentCountdown > 0f)
                return;

            if (other == null)
                return;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if(rb == null) return;

            rb.AddExplosionForce(50f, transform.position, 10f, 1f, ForceMode.Impulse);
            Debug.Log("Explode!");
        }
    }
}
