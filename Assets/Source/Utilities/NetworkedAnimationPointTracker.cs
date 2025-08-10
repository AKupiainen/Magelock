using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MageLock.Utilities
{
    [System.Serializable]
    public class NetworkedTrackedPoint
    {
        public string name = string.Empty;
        public string boneName = string.Empty;
        public Vector3 localOffset = Vector3.zero;
        public Vector3 localRotationOffset = Vector3.zero;
        
        [HideInInspector] public Transform trackedBone;
        [HideInInspector] public int boneIndex = -1;
        
        public Vector3 GetWorldPosition()
        {
            if (trackedBone != null)
                return trackedBone.TransformPoint(localOffset);
            return Vector3.zero;
        }
        
        public Quaternion GetWorldRotation()
        {
            if (trackedBone != null)
                return trackedBone.rotation * Quaternion.Euler(localRotationOffset);
            return Quaternion.identity;
        }
    }
    
    public class NetworkedAnimationPointTracker : NetworkBehaviour
    {
        [Header("Target")]
        [SerializeField] private SkinnedMeshRenderer targetRenderer;
        
        [Header("Tracked Points")]
        [SerializeField] private List<NetworkedTrackedPoint> trackedPoints = new List<NetworkedTrackedPoint>();
        
        [Header("Network Settings")]
        [SerializeField] private bool syncOnStart = true;
        [SerializeField] private bool debugNetworking = false;
        
        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private float gizmoSize = 0.1f;
        
        private Dictionary<string, NetworkedTrackedPoint> _pointLookup = new Dictionary<string, NetworkedTrackedPoint>();
        private Transform[] _allBones;
        private bool _isSetup = false;
        
        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            SetupBones();
            
            if (syncOnStart && IsOwner)
            {
                SyncBonesServerRpc();
            }
        }
        
        private void Start()
        {
            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening)
            {
                SetupBones();
            }
        }
        
        private void SetupBones()
        {
            if (_isSetup) return;
            
            if (targetRenderer == null)
            {
                Debug.LogError($"[NetworkedAnimationPointTracker] No SkinnedMeshRenderer found on {gameObject.name}");
                return;
            }
            
            _allBones = targetRenderer.bones;
            
            _pointLookup.Clear();
            foreach (var point in trackedPoints)
            {
                if (string.IsNullOrEmpty(point.name) || string.IsNullOrEmpty(point.boneName))
                    continue;
                
                SetupPointLocally(point);
                _pointLookup[point.name] = point;
            }
            
            _isSetup = true;
            
            if (debugNetworking)
            {
                Debug.Log($"[NetworkedAnimationPointTracker] Setup complete on {(IsServer ? "Server" : "Client")}. Tracking {trackedPoints.Count} points.");
            }
        }
        
        private void SetupPointLocally(NetworkedTrackedPoint point)
        {
            point.trackedBone = null;
            point.boneIndex = -1;
            
            if (_allBones == null) return;
            
            for (int i = 0; i < _allBones.Length; i++)
            {
                if (_allBones[i] != null && _allBones[i].name == point.boneName)
                {
                    point.trackedBone = _allBones[i];
                    point.boneIndex = i;
                    
                    if (debugNetworking)
                    {
                        Debug.Log($"[NetworkedAnimationPointTracker] Point '{point.name}' found bone '{point.boneName}' at index {i}");
                    }
                    break;
                }
            }
            
            if (point.trackedBone == null)
            {
                Debug.LogWarning($"[NetworkedAnimationPointTracker] Bone '{point.boneName}' not found for point '{point.name}'");
                point.trackedBone = transform;
            }
        }
        
        [ServerRpc]
        private void SyncBonesServerRpc()
        {
            if (debugNetworking)
                Debug.Log("[NetworkedAnimationPointTracker] Server received bone sync request");
            
            SyncBonesClientRpc();
        }
        
        [ClientRpc]
        private void SyncBonesClientRpc()
        {
            if (!_isSetup)
                SetupBones();
            
            if (debugNetworking)
                Debug.Log("[NetworkedAnimationPointTracker] Client received bone sync");
        }
        
        public Vector3 GetPointPosition(string pointName)
        {
            if (!_isSetup) SetupBones();
            
            if (_pointLookup.TryGetValue(pointName, out NetworkedTrackedPoint point))
            {
                return point.GetWorldPosition();
            }
            return transform.position;
        }
        
        public Quaternion GetPointRotation(string pointName)
        {
            if (!_isSetup) SetupBones();
            
            if (_pointLookup.TryGetValue(pointName, out NetworkedTrackedPoint point))
                return point.GetWorldRotation();
            return transform.rotation;
        }
        
        public Transform GetPointTransform(string pointName)
        {
            if (!_isSetup) SetupBones();
            
            if (_pointLookup.TryGetValue(pointName, out NetworkedTrackedPoint point))
                return point.trackedBone;
            return transform;
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void UpdatePointOffsetServerRpc(string pointName, Vector3 offset, Vector3 rotationOffset)
        {
            UpdatePointOffsetClientRpc(pointName, offset, rotationOffset);
        }
        
        [ClientRpc]
        private void UpdatePointOffsetClientRpc(string pointName, Vector3 offset, Vector3 rotationOffset)
        {
            if (_pointLookup.TryGetValue(pointName, out NetworkedTrackedPoint point))
            {
                point.localOffset = offset;
                point.localRotationOffset = rotationOffset;
            }
        }
        
        public bool HasPoint(string pointName)
        {
            if (!_isSetup) SetupBones();
            return _pointLookup.ContainsKey(pointName);
        }
        
#if UNITY_EDITOR
        
        public void SetupBonesInEditor()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            
            if (targetRenderer == null)
            {
                Debug.LogError("[NetworkedAnimationPointTracker] No SkinnedMeshRenderer found!");
                return;
            }
            
            _allBones = targetRenderer.bones;
            
            foreach (var point in trackedPoints)
            {
                if (string.IsNullOrEmpty(point.boneName))
                    continue;
                
                SetupPointLocally(point);
            }
        }
        
        public string[] GetAvailableBoneNames()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            
            if (targetRenderer == null)
                return new string[0];
            
            return targetRenderer.bones
                .Where(b => b != null)
                .Select(b => b.name)
                .Distinct()
                .OrderBy(n => n)
                .ToArray();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || trackedPoints == null) return;
            
            if (!Application.isPlaying && targetRenderer != null)
            {
                SetupBonesInEditor();
            }
            
            foreach (var point in trackedPoints)
            {
                if (point == null || point.trackedBone == null) continue;
                
                Vector3 pos = point.GetWorldPosition();
                Quaternion rot = point.GetWorldRotation();
                
                Gizmos.color = IsServer ? Color.green : Color.cyan;
                Gizmos.DrawWireSphere(pos, gizmoSize);
                
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(pos, rot * Vector3.forward * gizmoSize * 3);
                
                string label = $"{point.name}\n({point.boneName})";
                if (Application.isPlaying && NetworkManager.Singleton != null)
                {
                    label += $"\n[{(IsServer ? "Server" : "Client")}]";
                    if (point.boneIndex >= 0)
                        label += $"\nIdx: {point.boneIndex}";
                }
                UnityEditor.Handles.Label(pos + Vector3.up * gizmoSize * 2, label);
                
                if (point.trackedBone != transform)
                {
                    Gizmos.color = new Color(0, 1, 1, 0.3f);
                    Gizmos.DrawLine(transform.position, pos);
                }
            }
        }
#endif
    }
}