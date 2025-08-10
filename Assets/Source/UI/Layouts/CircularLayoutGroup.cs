using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Magelock.UI
{
    [AddComponentMenu("Layout/Circular Layout Group", 153)]
    public class CircularLayoutGroup : LayoutGroup
    {
        [Header("Circular Layout Settings")]
        [SerializeField] private float _radius = 100f;
        public float Radius { get { return _radius; } set { SetProperty(ref _radius, value); } }
        
        [SerializeField] private float _startAngle = 0f;
        public float StartAngle { get { return _startAngle; } set { SetProperty(ref _startAngle, value); } }
        
        [SerializeField] private bool _clockwise = true;
        public bool Clockwise { get { return _clockwise; } set => SetProperty(ref _clockwise, value);
        }
        
        [SerializeField] private bool _placeCenterItem = true;
        public bool PlaceCenterItem { get { return _placeCenterItem; } set => SetProperty(ref _placeCenterItem, value);
        }
        
        [Header("Child Settings")]
        [SerializeField] private bool _childForceExpandWidth = false;
        public bool ChildForceExpandWidth { get { return _childForceExpandWidth; } set { SetProperty(ref _childForceExpandWidth, value); } }
        
        [SerializeField] private bool _childForceExpandHeight = false;
        public bool ChildForceExpandHeight { get { return _childForceExpandHeight; } set { SetProperty(ref _childForceExpandHeight, value); } }
        
        [SerializeField] private bool _controlChildWidth = true;
        public bool ControlChildWidth { get { return _controlChildWidth; } set { SetProperty(ref _controlChildWidth, value); } }
        
        [SerializeField] private bool _controlChildHeight = true;
        public bool ControlChildHeight { get { return _controlChildHeight; } set { SetProperty(ref _controlChildHeight, value); } }
        
        [SerializeField] private Vector2 _childSize = new Vector2(100f, 100f);
        public Vector2 ChildSize { get { return _childSize; } set { SetProperty(ref _childSize, value); } }
        
        [SerializeField] private bool _rotateElements = false;
        public bool RotateElements { get { return _rotateElements; } set { SetProperty(ref _rotateElements, value); } }
        
        [SerializeField] private float _rotationOffset = 0f;
        public float RotationOffset { get { return _rotationOffset; } set { SetProperty(ref _rotationOffset, value); } }
        
        [SerializeField] private bool _keepChildRotation = false;
        public bool KeepChildRotation { get { return _keepChildRotation; } set { SetProperty(ref _keepChildRotation, value); } }
        
        [Header("Arc Settings")]
        [SerializeField] private bool _useArc = false;
        public bool UseArc { get { return _useArc; } set { SetProperty(ref _useArc, value); } }
        
        [SerializeField] private float _arcAngle = 360f;
        public float ArcAngle { get { return _arcAngle; } set { SetProperty(ref _arcAngle, value); } }
        
        [SerializeField] private bool _spreadEvenly = true;
        public bool SpreadEvenly { get { return _spreadEvenly; } set { SetProperty(ref _spreadEvenly, value); } }
        
        protected CircularLayoutGroup() {}
        
#if UNITY_EDITOR
        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }
#endif
        
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            
            float minWidth = _radius * 2 + padding.horizontal;
            float preferredWidth = minWidth;
            
            SetLayoutInputForAxis(minWidth, preferredWidth, -1, 0);
        }
        
        public override void CalculateLayoutInputVertical()
        {
            float minHeight = _radius * 2 + padding.vertical;
            float preferredHeight = minHeight;
            
            SetLayoutInputForAxis(minHeight, preferredHeight, -1, 1);
        }
        
        public override void SetLayoutHorizontal()
        {
            SetCellsAlongAxis(0);
        }
        
        public override void SetLayoutVertical()
        {
            SetCellsAlongAxis(1);
        }
        
        private void SetCellsAlongAxis(int axis)
        {
            var rectChildren = new List<RectTransform>();
            
            for (int i = 0; i < rectTransform.childCount; i++)
            {
                RectTransform child = rectTransform.GetChild(i) as RectTransform;
                if (child && child.gameObject.activeInHierarchy)
                {
                    rectChildren.Add(child);
                }
            }
            
            if (rectChildren.Count == 0) return;
            
            float centerX = padding.left + (rectTransform.rect.width - padding.horizontal) * 0.5f;
            float centerY = padding.top + (rectTransform.rect.height - padding.vertical) * 0.5f;
            
            int startIndex = 0;
            
            if (_placeCenterItem && rectChildren.Count > 0)
            {
                RectTransform centerChild = rectChildren[0];
                
                if (_controlChildWidth)
                    centerChild.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _childSize.x);
                if (_controlChildHeight)
                    centerChild.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _childSize.y);
                
                SetChildAlongAxis(centerChild, 0, centerX - _childSize.x * 0.5f, _childSize.x);
                SetChildAlongAxis(centerChild, 1, centerY - _childSize.y * 0.5f, _childSize.y);
                
                if (!_keepChildRotation)
                {
                    if (_rotateElements)
                    {
                        centerChild.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        centerChild.localRotation = Quaternion.identity;
                    }
                }
                
                startIndex = 1;
            }
            
            int itemsOnCircle = rectChildren.Count - startIndex;
            if (itemsOnCircle <= 0) return;
            
            float totalAngle = _useArc ? _arcAngle : 360f;
            float angleStep;
            
            if (_spreadEvenly)
            {
                angleStep = itemsOnCircle > 1 ? totalAngle / (itemsOnCircle - (_useArc ? 1 : 0)) : 0;
            }
            else
            {
                angleStep = totalAngle / itemsOnCircle;
            }
            
            float currentAngle = _startAngle;
            
            for (int i = startIndex; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                
                if (_controlChildWidth)
                    child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _childSize.x);
                if (_controlChildHeight)
                    child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _childSize.y);
                
                float angleRad = currentAngle * Mathf.Deg2Rad;
                float x = centerX + Mathf.Cos(angleRad) * _radius - _childSize.x * 0.5f;
                float y = centerY + Mathf.Sin(angleRad) * _radius - _childSize.y * 0.5f;
                
                SetChildAlongAxis(child, 0, x, _childSize.x);
                SetChildAlongAxis(child, 1, y, _childSize.y);
                
                if (!_keepChildRotation)
                {
                    if (_rotateElements)
                    {
                        float rotation = currentAngle + _rotationOffset;
                        if (!_clockwise) rotation = -rotation;
                        child.localRotation = Quaternion.Euler(0, 0, rotation);
                    }
                    else
                    {
                        child.localRotation = Quaternion.identity;
                    }
                }
                
                if (_clockwise)
                    currentAngle -= angleStep;
                else
                    currentAngle += angleStep;
            }
        }
        
        protected new bool SetProperty<T>(ref T currentValue, T newValue)
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;
            
            currentValue = newValue;
            SetDirty();
            return true;
        }
        
        protected new void SetDirty()
        {
            if (!IsActive())
                return;
            
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
        
#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _radius = 100f;
            _childSize = new Vector2(100f, 100f);
        }
#endif
    }
}