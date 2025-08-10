using UnityEngine;
using UnityEngine.UI;
using System;
using Magelock.UI;

namespace MageLock.UI
{
    public class SpellButtonComponent : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private CircularImage circularCooldownOverlay; 
        
        [Header("Cooldown Settings")]
        [SerializeField] private bool useCircularCooldown;
        [SerializeField] private bool invertCooldownFill; 
        
        [Header("State")]
        [SerializeField, ReadOnly] private float cooldownTimer;
        [SerializeField, ReadOnly] private float maxCooldownDuration;
        [SerializeField, ReadOnly] private bool isOnCooldown;
        
        private Action<int> _onButtonPressed;
        private int _slotIndex;
        
        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            
            if (button != null)
                button.onClick.AddListener(HandleButtonPress);
            
            if (!useCircularCooldown && cooldownOverlay != null)
            {
                cooldownOverlay.type = Image.Type.Filled;
                cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
                cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
                cooldownOverlay.fillClockwise = true;
            }
        }
        
        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleButtonPress);
        }
        
        public void Initialize(int index, Action<int> onPressed)
        {
            _slotIndex = index;
            _onButtonPressed = onPressed;
            SetCooldownActive(false);
        }
        
        private void HandleButtonPress()
        {
            if (!isOnCooldown)
                _onButtonPressed?.Invoke(_slotIndex);
        }
        
        public void StartCooldown(float duration)
        {
            maxCooldownDuration = duration;
            cooldownTimer = duration;
            isOnCooldown = true;
            SetCooldownActive(true);
            SetInteractable(false);
            UpdateCooldownDisplay(1f);
        }
     
        public void UpdateCooldown(float deltaTime)
        {
            if (!isOnCooldown) return;
            
            cooldownTimer -= deltaTime;
            
            if (cooldownTimer <= 0f)
            {
                CompleteCooldown();
            }
            else
            {
                float progress = 1f - (cooldownTimer / maxCooldownDuration);
                float fillAmount = invertCooldownFill ? progress : 1f - progress;
                
                UpdateCooldownDisplay(fillAmount);
            }
        }
        
        private void CompleteCooldown()
        {
            cooldownTimer = 0f;
            isOnCooldown = false;
            SetCooldownActive(false);
            SetInteractable(true);
            UpdateCooldownDisplay(0f);
        }
        
        private void SetCooldownActive(bool active)
        {
            if (useCircularCooldown && circularCooldownOverlay != null)
            {
                circularCooldownOverlay.gameObject.SetActive(active);
            }
            else if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(active);
            }
        }
        
        private void UpdateCooldownDisplay(float fillAmount)
        {
            if (useCircularCooldown && circularCooldownOverlay != null)
            {
                circularCooldownOverlay.FillAmount = fillAmount;
            }
            else if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = fillAmount;
            }
        }
  
        public void SetInteractable(bool interactable)
        {
            if (button)
                button.interactable = interactable && !isOnCooldown;
        }
        
        public void SetIcon(Sprite sprite)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
        }

        public void Clear()
        {
            SetIcon(null);
            SetInteractable(false);
            CompleteCooldown();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null)
                button = GetComponent<Button>();
            
            if (circularCooldownOverlay == null && cooldownOverlay != null)
            {
                circularCooldownOverlay = cooldownOverlay.GetComponent<CircularImage>();
                if (circularCooldownOverlay != null)
                {
                    useCircularCooldown = true;
                }
            }
        }
#endif
    }
    
    public class ReadOnlyAttribute : PropertyAttribute { }
    
#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}