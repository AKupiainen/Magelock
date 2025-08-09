using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace MageLock.UI
{
    public class SpellButtonComponent : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TMP_Text cooldownText;
        
        [Header("State")]
        [SerializeField, ReadOnly] private float cooldownTimer;
        [SerializeField, ReadOnly] private bool isOnCooldown;
        
        private Action<int> _onButtonPressed;
        private int _slotIndex;
        
        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            
            if (button != null)
                button.onClick.AddListener(HandleButtonPress);
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
            cooldownTimer = duration;
            isOnCooldown = true;
            SetCooldownActive(true);
            SetInteractable(false);
            UpdateCooldownDisplay(1f, Mathf.CeilToInt(duration));
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
                float fillAmount = cooldownTimer / GetMaxCooldown();
                int secondsRemaining = Mathf.CeilToInt(cooldownTimer);
                UpdateCooldownDisplay(fillAmount, secondsRemaining);
            }
        }
        
        private void CompleteCooldown()
        {
            cooldownTimer = 0f;
            isOnCooldown = false;
            SetCooldownActive(false);
            SetInteractable(true);
        }
        
        private void SetCooldownActive(bool active)
        {
            if (cooldownOverlay != null)
                cooldownOverlay.gameObject.SetActive(active);
            if (cooldownText != null)
                cooldownText.gameObject.SetActive(active);
        }
        
        private void UpdateCooldownDisplay(float fillAmount, int secondsRemaining)
        {
            if (cooldownOverlay)
                cooldownOverlay.fillAmount = fillAmount;
            if (cooldownText)
                cooldownText.text = secondsRemaining.ToString();
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
        
        private float _maxCooldown;
        public void SetMaxCooldown(float max)
        {
            _maxCooldown = max;
        }
        
        private float GetMaxCooldown()
        {
            return _maxCooldown > 0 ? _maxCooldown : 1f;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null)
                button = GetComponent<Button>();
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