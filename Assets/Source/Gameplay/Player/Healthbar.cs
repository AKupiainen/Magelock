using UnityEngine;
using MageLock.Events;
using MageLock.Controls;

namespace MageLock.UI
{
    [RequireComponent(typeof(MeshRenderer))]
    public class HealthBar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector3 offset = new(0, 2f, 0);
        [SerializeField] private bool hideWhenFull = true;
        [SerializeField] private float damageBarShrinkDelay = 0.5f;
        [SerializeField] private float damageBarShrinkSpeed = 0.5f;
        
        [Header("Shader Properties")]
        [SerializeField] private string healthPropertyName = "_Health";
        [SerializeField] private string damagePropertyName = "_Damage";
        [SerializeField] private string alphaPropertyName = "_Alpha";
        
        private NetworkPlayer _networkPlayer;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private Camera _camera;
        
        private float _healthPercent = 1f;
        private float _damagePercent = 1f;
        private float _damageBarTimer;
        private ulong _playerId;
        
        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
        }
        
        private void Start()
        {
            _camera = Camera.main;
            _networkPlayer = GetComponentInParent<NetworkPlayer>();
            
            if (_networkPlayer == null)
            {
                enabled = false;
                return;
            }
            
            _playerId = _networkPlayer.OwnerClientId;
            
            transform.localPosition = offset;
            
            float currentHealth = _networkPlayer.GetHealth();
            float maxHealth = _networkPlayer.GetMaxHealth();
            UpdateHealthBar(currentHealth, maxHealth);
            
            if (EventsBus.IsInitialized)
            {
                EventsBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
                EventsBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
            }
        }
        
        private void OnDestroy()
        {
            if (EventsBus.IsInitialized)
            {
                EventsBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
                EventsBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
            }
        }
        
        private void LateUpdate()
        {
            FaceCameraPlane();
            HandleVisibility();
            UpdateDamageBar();
        }
        
        private void UpdateDamageBar()
        {
            if (_damagePercent > _healthPercent)
            {
                _damageBarTimer += Time.deltaTime;
                
                if (_damageBarTimer >= damageBarShrinkDelay)
                {
                    _damagePercent = Mathf.MoveTowards(_damagePercent, _healthPercent, 
                        damageBarShrinkSpeed * Time.deltaTime);
                    UpdateShaderProperties();
                }
            }
        }
        
        private void FaceCameraPlane()
        {
            Vector3 forward = Vector3.forward;
            Vector3 up = Vector3.up;
            
            if (_camera)
            {
                forward = _camera.transform.forward;
                up = _camera.transform.up;
            }
            
            Quaternion targetRotation = Quaternion.LookRotation(forward, up);
            transform.rotation = targetRotation;
        }
        
        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (maxHealth <= 0) return;
            
            float newHealthPercent = currentHealth / maxHealth;
            
            if (newHealthPercent < _healthPercent)
            {
                _damagePercent = _healthPercent;
                _damageBarTimer = 0f;
            }
            else if (newHealthPercent > _damagePercent)
            {
                _damagePercent = newHealthPercent;
                _damageBarTimer = 0f;
            }
            
            _healthPercent = newHealthPercent;
            UpdateShaderProperties();
        }
        
        private void UpdateShaderProperties()
        {
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            
            _propertyBlock.SetFloat(healthPropertyName, _healthPercent);
            _propertyBlock.SetFloat(damagePropertyName, _damagePercent);
            
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
            
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(alphaPropertyName, targetAlpha);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }
        
        private void OnHealthChanged(PlayerHealthChangedEvent evt)
        {
            if (evt.ClientId != _playerId) return;
            UpdateHealthBar(evt.CurrentHealth, evt.MaxHealth);
        }
        
        private void OnPlayerDeath(PlayerDeathEvent evt)
        {
            if (evt.ClientId != _playerId) return;
            
            _healthPercent = 0;
            _damagePercent = 0;
            UpdateShaderProperties();
        }
    }
}