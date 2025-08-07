using Unity.Netcode;
using UnityEngine;

namespace MageLock.GameModes
{
    public class SpawnPoint : NetworkBehaviour
    {
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}