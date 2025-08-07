using System;
using UnityEngine;

namespace BrawlLine.GameModes
{
    [RequireComponent(typeof(Collider))]
    public class ChildCollisionDetector : MonoBehaviour
    {
        public event Action<Collider> OnChildTriggerEnter;
        public event Action<Collider> OnChildCollisionEnter;

        private void OnTriggerEnter(Collider other)
        {
            OnChildTriggerEnter?.Invoke(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            OnChildCollisionEnter?.Invoke(collision.collider);
        }
    }
}