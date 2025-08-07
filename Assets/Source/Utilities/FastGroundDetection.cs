namespace MageLock.Utilies
{
    using UnityEngine;
    
    public struct GroundResult
    {
        public bool IsGrounded;
        public Vector3 Normal;
        public Vector3 Point;
        public float Distance;
        public bool CrackedGround;
        
        public static GroundResult None => new()
        { 
            IsGrounded = false, 
            Normal = Vector3.up,
            CrackedGround = false
        };
    }

    public static class FastGroundDetection
    {
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];
        private static readonly Collider[] ColliderBuffer = new Collider[4];
        
        public static bool IsGrounded(Vector3 position, float radius, float distance, LayerMask groundLayer)
        {
            return Physics.CapsuleCast(
                position + Vector3.up * 0.1f,
                position + Vector3.up * 0.1f - Vector3.up * 0.1f,
                radius,
                Vector3.down,
                distance,
                groundLayer
            );
        }
        
        public static GroundResult CheckGround(Vector3 position, float radius = 0.3f, float distance = 0.2f, 
            LayerMask groundLayer = default, int rayPoints = 5, float maxCrackWidth = 0.5f)
        {
            if (groundLayer.value == 0) groundLayer = -1; 
            
            Vector3 center = position + Vector3.up * (radius + 0.02f);
            
            var multiResult = MultiPointCheck(center, distance, groundLayer, rayPoints, radius, maxCrackWidth);
            if (multiResult.IsGrounded) return multiResult;
            
            return CapsuleCheck(center, radius, distance, groundLayer);
        }
        
        public static GroundResult QuickCheck(Vector3 position, float radius = 0.3f, float distance = 0.2f, LayerMask groundLayer = default)
        {
            if (groundLayer.value == 0) groundLayer = -1; 
            
            Vector3 center = position + Vector3.up * radius;
            
            if (Physics.Raycast(center, Vector3.down, out RaycastHit hit, distance + radius, groundLayer))
            {
                return new GroundResult
                {
                    IsGrounded = true,
                    Normal = hit.normal,
                    Point = hit.point,
                    Distance = hit.distance - radius,
                    CrackedGround = false
                };
            }
            
            return GroundResult.None;
        }

        public static bool IsOverCrack(Vector3 position, float radius = 0.4f, float distance = 0.3f, 
            LayerMask groundLayer = default, float maxCrackWidth = 0.5f)
        {
            if (groundLayer.value == 0) groundLayer = -1; 
            
            Vector3 center = position + Vector3.up * 0.1f;
            
            bool centerHit = Physics.Raycast(center, Vector3.down, distance, groundLayer);
            if (centerHit) return false; 
            
            Vector3 forward = Vector3.forward * radius;
            Vector3 right = Vector3.right * radius;
            
            bool hasLeftHit = Physics.Raycast(center - right, Vector3.down, out RaycastHit leftHit, distance, groundLayer);
            bool hasRightHit = Physics.Raycast(center + right, Vector3.down, out RaycastHit rightHit, distance, groundLayer);
            bool hasForwardHit = Physics.Raycast(center + forward, Vector3.down, out RaycastHit forwardHit, distance, groundLayer);
            bool hasBackHit = Physics.Raycast(center - forward, Vector3.down, out RaycastHit backHit, distance, groundLayer);
            
            if ((hasLeftHit && hasRightHit) || (hasForwardHit && hasBackHit))
            {
                float crackWidth = hasLeftHit && hasRightHit ? 
                    Vector3.Distance(leftHit.point, rightHit.point) :
                    Vector3.Distance(forwardHit.point, backHit.point);
                
                return crackWidth <= maxCrackWidth;
            }
            
            return false;
        }
        
        public static Vector3 GetPlatformVelocity(Vector3 groundPoint, LayerMask groundLayer = default)
        {
            if (groundLayer.value == 0) groundLayer = -1; 
            
            int hitCount = Physics.OverlapSphereNonAlloc(groundPoint, 0.1f, ColliderBuffer, groundLayer);
            
            for (int i = 0; i < hitCount; i++)
            {
                if (ColliderBuffer[i].attachedRigidbody != null)
                {
                    return ColliderBuffer[i].attachedRigidbody.linearVelocity;
                }
            }
            
            return Vector3.zero;
        }
        
        private static GroundResult MultiPointCheck(Vector3 center, float distance, LayerMask groundLayer, 
            int rayPoints, float radius, float maxCrackWidth)
        {
            int hitCount = 0;
            Vector3 normalSum = Vector3.zero;
            Vector3 pointSum = Vector3.zero;
            float closestDistance = float.MaxValue;
            bool hasCenterHit = false;
            bool hasSideHits = false;
            
            if (Physics.Raycast(center, Vector3.down, out RaycastHit centerHit, distance, groundLayer))
            {
                HitBuffer[hitCount++] = centerHit;
                normalSum += centerHit.normal;
                pointSum += centerHit.point;
                closestDistance = Mathf.Min(closestDistance, centerHit.distance);
                hasCenterHit = true;
            }
            
            float angleStep = 360f / rayPoints;
            for (int i = 0; i < rayPoints && hitCount < HitBuffer.Length - 1; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 rayStart = center + offset;
                
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, distance, groundLayer))
                {
                    HitBuffer[hitCount++] = hit;
                    normalSum += hit.normal;
                    pointSum += hit.point;
                    closestDistance = Mathf.Min(closestDistance, hit.distance);
                    hasSideHits = true;
                }
            }
            
            if (hitCount == 0)
                return GroundResult.None;
            
            bool crackedGround = !hasCenterHit && hasSideHits && hitCount >= 2;
            
            if (crackedGround && CanBridgeCrack(hitCount, maxCrackWidth))
            {
                Vector3 bridgePoint = pointSum / hitCount;
                Vector3 bridgeNormal = (normalSum / hitCount).normalized;
                
                return new GroundResult
                {
                    IsGrounded = true,
                    Normal = bridgeNormal,
                    Point = bridgePoint,
                    Distance = closestDistance,
                    CrackedGround = true
                };
            }
            
            Vector3 avgNormal = (normalSum / hitCount).normalized;
            Vector3 avgPoint = pointSum / hitCount;
            
            return new GroundResult
            {
                IsGrounded = true,
                Normal = avgNormal,
                Point = avgPoint,
                Distance = closestDistance,
                CrackedGround = crackedGround
            };
        }
        
        private static GroundResult CapsuleCheck(Vector3 center, float radius, float distance, LayerMask groundLayer)
        {
            Vector3 capsuleTop = center + Vector3.up * 0.1f;
            Vector3 capsuleBottom = center - Vector3.up * 0.1f;
            
            if (Physics.CapsuleCast(capsuleTop, capsuleBottom, radius - 0.02f, Vector3.down, 
                out RaycastHit hit, distance, groundLayer))
            {
                return new GroundResult
                {
                    IsGrounded = true,
                    Normal = hit.normal,
                    Point = hit.point,
                    Distance = hit.distance,
                    CrackedGround = false
                };
            }
            
            return GroundResult.None;
        }
        
        private static bool CanBridgeCrack(int hitCount, float maxCrackWidth)
        {
            if (hitCount < 2) return false;
            
            float maxDistance = 0f;
            
            for (int i = 0; i < hitCount; i++)
            {
                for (int j = i + 1; j < hitCount; j++)
                {
                    float dist = Vector3.Distance(HitBuffer[i].point, HitBuffer[j].point);
                    maxDistance = Mathf.Max(maxDistance, dist);
                }
            }
            
            return maxDistance <= maxCrackWidth;
        }
    }
}