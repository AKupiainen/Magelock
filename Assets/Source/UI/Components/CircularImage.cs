using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Magelock.UI
{
    [AddComponentMenu("UI/Circular Image", 12)]
    public class CircularImage : Image
    {
        [Header("Circle Settings")]
        [SerializeField] private int segments = 64;
        [SerializeField] private bool filled = true;
        [SerializeField] [Range(0f, 1f)] private new float fillAmount = 1f;
        [SerializeField] private float thickness = 5f;
        [SerializeField] [Range(0.1f, 2f)] private float zoom = 1f;
        
        [Header("Fill Settings")]
        [SerializeField] private new FillMethod fillMethod = FillMethod.Radial360;
        [SerializeField] private new bool fillClockwise = true;
        [SerializeField] private new int fillOrigin;
        
        public new enum FillMethod
        {
            None,
            Radial360,
            Radial180,
            Radial90
        }
        
        public int Segments
        {
            get => segments;
            set
            {
                segments = Mathf.Max(3, value);
                SetVerticesDirty();
            }
        }
        
        public bool Filled
        {
            get => filled;
            set
            {
                filled = value;
                SetVerticesDirty();
            }
        }
        
        public float FillAmount
        {
            get => fillAmount;
            set
            {
                fillAmount = Mathf.Clamp01(value);
                SetVerticesDirty();
            }
        }
        
        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = Mathf.Max(0, value);
                SetVerticesDirty();
            }
        }
        
        public float Zoom
        {
            get => zoom;
            set
            {
                zoom = Mathf.Clamp(value, 0.1f, 2f);
                SetVerticesDirty();
            }
        }
        
        public FillMethod FillType
        {
            get => fillMethod;
            set
            {
                fillMethod = value;
                SetVerticesDirty();
            }
        }
        
        public bool FillClockwise
        {
            get => fillClockwise;
            set
            {
                fillClockwise = value;
                SetVerticesDirty();
            }
        }
        
        public int FillOrigin
        {
            get => fillOrigin;
            set
            {
                fillOrigin = Mathf.Clamp(value, 0, 3);
                SetVerticesDirty();
            }
        }
        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            
            if (sprite == null && !filled)
                return;
            
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float radius = Mathf.Min(width, height) * 0.5f;  // Circle size stays constant
            
            Vector2 center = Vector2.zero;
            Vector4 uv = sprite != null ? DataUtility.GetOuterUV(sprite) : Vector4.zero;
            
            if (filled)
            {
                DrawFilledCircle(vh, center, radius, uv);
            }
            else
            {
                DrawCircleOutline(vh, center, radius, uv);
            }
        }
        
        private void DrawFilledCircle(VertexHelper vh, Vector2 center, float radius, Vector4 spriteUV)
        {
            float fillAngle = GetFillAngle();
            float startAngle = GetStartAngle();
            int actualSegments = Mathf.CeilToInt(segments * fillAmount);
            
            if (fillMethod == FillMethod.None)
                actualSegments = segments;
            
            float angleStep = fillAngle / segments;
            
            Vector2 centerUV = new Vector2(
                Mathf.Lerp(spriteUV.x, spriteUV.z, 0.5f),
                Mathf.Lerp(spriteUV.y, spriteUV.w, 0.5f)
            );
            vh.AddVert(center, color, centerUV);
            
            for (int i = 0; i <= actualSegments; i++)
            {
                float angle = startAngle + (fillClockwise ? -i : i) * angleStep;
                float rad = angle * Mathf.Deg2Rad;
                
                Vector2 pos = new Vector2(
                    center.x + Mathf.Cos(rad) * radius,
                    center.y + Mathf.Sin(rad) * radius
                );
                
                float uvX = 0.5f + (Mathf.Cos(rad) * 0.5f / zoom);
                float uvY = 0.5f + (Mathf.Sin(rad) * 0.5f / zoom);
                
                uvX = Mathf.Clamp(uvX, 0f, 1f);
                uvY = Mathf.Clamp(uvY, 0f, 1f);
                
                Vector2 uvPos = new Vector2(
                    Mathf.Lerp(spriteUV.x, spriteUV.z, uvX),
                    Mathf.Lerp(spriteUV.y, spriteUV.w, uvY)
                );
                
                vh.AddVert(pos, color, uvPos);
                
                if (i > 0)
                {
                    vh.AddTriangle(0, i, i + 1);
                }
            }
            
            if (fillMethod == FillMethod.None || fillAmount >= 1f)
            {
                vh.AddTriangle(0, actualSegments + 1, 1);
            }
        }
        
        private void DrawCircleOutline(VertexHelper vh, Vector2 center, float radius, Vector4 spriteUV)
        {
            float innerRadius = radius - thickness;
            
            float fillAngle = GetFillAngle();
            float startAngle = GetStartAngle();
            int actualSegments = Mathf.CeilToInt(segments * fillAmount);
            
            if (fillMethod == FillMethod.None)
                actualSegments = segments;
            
            float angleStep = fillAngle / segments;
            
            for (int i = 0; i <= actualSegments; i++)
            {
                float angle = startAngle + (fillClockwise ? -i : i) * angleStep;
                float rad = angle * Mathf.Deg2Rad;
                
                Vector2 outerPos = new Vector2(
                    center.x + Mathf.Cos(rad) * radius,
                    center.y + Mathf.Sin(rad) * radius
                );
                
                Vector2 innerPos = new Vector2(
                    center.x + Mathf.Cos(rad) * innerRadius,
                    center.y + Mathf.Sin(rad) * innerRadius
                );
                
                float outerUvX = 0.5f + (Mathf.Cos(rad) * 0.5f / zoom);
                float outerUvY = 0.5f + (Mathf.Sin(rad) * 0.5f / zoom);
                
                float innerRadiusRatio = innerRadius / radius;
                float innerUvX = 0.5f + (Mathf.Cos(rad) * 0.5f * innerRadiusRatio / zoom);
                float innerUvY = 0.5f + (Mathf.Sin(rad) * 0.5f * innerRadiusRatio / zoom);
                
                outerUvX = Mathf.Clamp(outerUvX, 0f, 1f);
                outerUvY = Mathf.Clamp(outerUvY, 0f, 1f);
                innerUvX = Mathf.Clamp(innerUvX, 0f, 1f);
                innerUvY = Mathf.Clamp(innerUvY, 0f, 1f);
                
                Vector2 outerUV = new Vector2(
                    Mathf.Lerp(spriteUV.x, spriteUV.z, outerUvX),
                    Mathf.Lerp(spriteUV.y, spriteUV.w, outerUvY)
                );
                
                Vector2 innerUV = new Vector2(
                    Mathf.Lerp(spriteUV.x, spriteUV.z, innerUvX),
                    Mathf.Lerp(spriteUV.y, spriteUV.w, innerUvY)
                );
                
                vh.AddVert(innerPos, color, innerUV);
                vh.AddVert(outerPos, color, outerUV);
                
                if (i > 0)
                {
                    int index = i * 2;
                    vh.AddTriangle(index - 2, index - 1, index);
                    vh.AddTriangle(index - 1, index + 1, index);
                }
            }
            
            if (fillMethod == FillMethod.None || fillAmount >= 1f)
            {
                int lastIndex = actualSegments * 2;
                vh.AddTriangle(lastIndex, lastIndex + 1, 0);
                vh.AddTriangle(lastIndex + 1, 1, 0);
            }
        }
        
        private float GetFillAngle()
        {
            switch (fillMethod)
            {
                case FillMethod.Radial360:
                    return 360f;
                case FillMethod.Radial180:
                    return 180f;
                case FillMethod.Radial90:
                    return 90f;
                default:
                    return 360f;
            }
        }
        
        private float GetStartAngle()
        {
            float baseAngle = 90f; 
            
            switch (fillOrigin)
            {
                case 0: 
                    baseAngle = 90f;
                    break;
                case 1: 
                    baseAngle = 0f;
                    break;
                case 2: 
                    baseAngle = 270f;
                    break;
                case 3: 
                    baseAngle = 180f;
                    break;
            }
            
            if (fillMethod == FillMethod.Radial180)
            {
                baseAngle += fillClockwise ? 90f : -90f;
            }
            else if (fillMethod == FillMethod.Radial90)
            {
                baseAngle += fillClockwise ? 45f : -45f;
            }
            
            return baseAngle;
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            segments = Mathf.Max(3, segments);
            fillAmount = Mathf.Clamp01(fillAmount);
            thickness = Mathf.Max(0, thickness);
            fillOrigin = Mathf.Clamp(fillOrigin, 0, 3);
            SetVerticesDirty();
        }
#endif
    }
}