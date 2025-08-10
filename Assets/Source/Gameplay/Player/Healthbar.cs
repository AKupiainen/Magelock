using UnityEngine;
using MageLock.Events;
using MageLock.Controls;
using DG.Tweening;
using Unity.Netcode;

namespace MageLock.UI
{
    [RequireComponent(typeof(MeshRenderer))]
    public class HealthBar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector3 offset = new(0, 2f, 0);
        [SerializeField] private bool hideWhenFull = true;
        [SerializeField] private float fadeSpeed = 0.3f;
        
        [Header("Shader Properties")]
        [SerializeField] private string healthPropertyName = "_Health";
        [SerializeField] private string damagePropertyName = "_Damage";
        [SerializeField] private string colorPropertyName = "_HealthColor";
        [SerializeField] private string alphaPropertyName = "_Alpha";
        
        [Header("Colors")]
        [SerializeField] private Gradient healthGradient;
        
        [Header("Animation")]
        [SerializeField] private float damageBarDelay = 0.5f;
        [SerializeField] private float damageBarDuration = 0.3f;
        [SerializeField] private Ease damageBarEase = Ease.OutQuad;
        [SerializeField] private float damagePulseScale = 1.15f;
        [SerializeField] private float pulseDuration = 0.2f;
        
        private NetworkPlayer _networkPlayer;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private Camera _mainCamera;
        
        private float _currentHealth;
        private float _maxHealth;
        private float _healthPercent = 1f;
        private float _damagePercent = 1f;
        private float _currentAlpha = 1f;
        private ulong _playerId;
        
        private Tween _damageTween;
        private Tween _fadeTween;
        private Tween _pulseTween;
        
        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
        }
        
        private void Start()
        {
            _networkPlayer = GetComponentInParent<NetworkPlayer>();
            _mainCamera = Camera.main;
            
            if (_networkPlayer == null)
            {
                enabled = false;
                return;
            }
            
            _playerId = _networkPlayer.OwnerClientId;
            
            transform.localPosition = offset;
            
            _currentHealth = _networkPlayer.GetHealth();
            _maxHealth = _networkPlayer.GetMaxHealth();
            UpdateHealthBar(_currentHealth, _maxHealth);
            
            EventsBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventsBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventsBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
            
        }
        
        private void OnDestroy()
        {
        
            EventsBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventsBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventsBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
            
            _damageTween?.Kill();
            _fadeTween?.Kill();
            _pulseTween?.Kill();
        }
        
        private void LateUpdate()
        {
            if (_mainCamera)
            {
                transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward);
            }
            
            HandleVisibility();
        }
        
        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (maxHealth <= 0) return;
            
            _currentHealth = currentHealth;
            _maxHealth = maxHealth;
            
            float newHealthPercent = currentHealth / maxHealth;
            
            if (newHealthPercent < _healthPercent)
            {
                AnimateDamageBar(newHealthPercent);
            }
            else if (newHealthPercent > _healthPercent)
            {
                _damagePercent = newHealthPercent;
                UpdateShaderProperties();
            }
            
            _healthPercent = newHealthPercent;
            UpdateShaderProperties();
        }
        
        private void AnimateDamageBar(float targetPercent)
        {
            _damageTween?.Kill();
            _damageTween = DOTween.Sequence()
                .AppendInterval(damageBarDelay)
                .Append(DOTween.To(() => _damagePercent, x => 
                {
                    _damagePercent = x;
                    UpdateShaderProperties();
                }, targetPercent, damageBarDuration))
                .SetEase(damageBarEase);
        }
        
        private void UpdateShaderProperties()
        {
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            
            _propertyBlock.SetFloat(healthPropertyName, _healthPercent);
            _propertyBlock.SetFloat(damagePropertyName, _damagePercent);
            _propertyBlock.SetFloat(alphaPropertyName, _currentAlpha);
            
            if (healthGradient != null)
            {
                Color healthColor = healthGradient.Evaluate(_healthPercent);
                _propertyBlock.SetColor(colorPropertyName, healthColor);
            }
            
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }
        
        private void HandleVisibility()
        {
            bool shouldShow = !(hideWhenFull && _healthPercent >= 0.99f);
            
            if (_networkPlayer != null && _networkPlayer.IsDead())
            {
                shouldShow = false;
            }
            
            float targetAlpha = shouldShow ? 1f : 0f;
            
            if (Mathf.Abs(_currentAlpha - targetAlpha) > 0.01f)
            {
                _fadeTween?.Kill();
                _fadeTween = DOTween.To(() => _currentAlpha, x => 
                {
                    _currentAlpha = x;
                    UpdateShaderProperties();
                }, targetAlpha, fadeSpeed);
            }
        }
        
        private void OnHealthChanged(PlayerHealthChangedEvent evt)
        {
            if (evt.ClientId != _playerId) return;
            UpdateHealthBar(evt.CurrentHealth, evt.MaxHealth);
        }
        
        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            if (evt.ClientId != _playerId) return;
            
            _pulseTween?.Kill();
            Vector3 originalScale = transform.localScale;
            _pulseTween = transform.DOPunchScale(originalScale * (damagePulseScale - 1f), 
                pulseDuration, 0, 0);
        }
        
        private void OnPlayerDeath(PlayerDeathEvent evt)
        {
            if (evt.ClientId != _playerId) return;
            
            _healthPercent = 0;
            _damagePercent = 0;
            UpdateShaderProperties();
            
            _fadeTween?.Kill();
            _fadeTween = DOTween.To(() => _currentAlpha, x => 
            {
                _currentAlpha = x;
                UpdateShaderProperties();
            }, 0, fadeSpeed * 2f);
        }
    }
}