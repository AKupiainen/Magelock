using UnityEngine;

namespace MageLock.ModelRenderer
{
    public class ModelPrefabInstantiator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ModelRenderGraphic modelRenderer;
        [SerializeField] private GameObject modelPrefab;
        
        [Header("Instantiation Settings")]
        [SerializeField] private Vector3 modelOffset = Vector3.zero;
        [SerializeField] private Vector3 modelRotation = Vector3.zero;
        [SerializeField] private Vector3 modelScale = Vector3.one;
        [SerializeField] private bool instantiateOnAwake = true;
        
        private GameObject _currentModelInstance;
        
        public Transform CurrentModelTransform => _currentModelInstance?.transform;
        
        private void Awake()
        {
            if (instantiateOnAwake)
            {
                InstantiateModelPrefab();
            }
        }
        
        public Transform InstantiateModelPrefab()
        {
            if (modelRenderer == null)
            {
                Debug.LogError("ModelRenderGraphic reference is missing!");
                return null;
            }
            
            if (modelPrefab == null)
            {
                Debug.LogError("Model prefab is missing!");
                return null;
            }
            
            if (_currentModelInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_currentModelInstance);
                }
                else
                {
                    DestroyImmediate(_currentModelInstance);
                }
                    
                _currentModelInstance = null;
            }
            
            _currentModelInstance = Instantiate(modelPrefab);
            _currentModelInstance.name = $"{modelPrefab.name}_Instance";
            
            _currentModelInstance.transform.position = Vector3.zero;
            _currentModelInstance.transform.rotation = Quaternion.Euler(modelRotation);
            _currentModelInstance.transform.localScale = modelScale;
            
            _currentModelInstance.SetActive(true);
            
            modelRenderer.UpdateModelTransform(_currentModelInstance.transform, modelOffset);
            
            return _currentModelInstance.transform;
        }
        
        public Transform ChangeModelPrefab(GameObject newPrefab)
        {
            if (newPrefab == null)
            {
                Debug.LogWarning("Attempted to change to a null prefab!");
                return null;
            }
            
            modelPrefab = newPrefab;
            return InstantiateModelPrefab();
        }
        
        public void UpdateModelOffset(Vector3 newOffset)
        {
            modelOffset = newOffset;
            
            if (_currentModelInstance != null && modelRenderer != null)
            {
                modelRenderer.UpdateModelTransform(_currentModelInstance.transform, modelOffset);
            }
        }
        
        public void UpdateModelRotation(Vector3 newRotation)
        {
            modelRotation = newRotation;
            
            if (_currentModelInstance != null)
            {
                _currentModelInstance.transform.rotation = Quaternion.Euler(modelRotation);
                
                if (modelRenderer != null)
                {
                    modelRenderer.UpdateModelTransform(_currentModelInstance.transform, modelOffset);
                }
            }
        }
        
        public void UpdateModelScale(Vector3 newScale)
        {
            modelScale = newScale;
            
            if (_currentModelInstance != null)
            {
                _currentModelInstance.transform.localScale = modelScale;
                
                if (modelRenderer != null)
                {
                    modelRenderer.UpdateModelTransform(_currentModelInstance.transform, modelOffset);
                }
            }
        }
        
        public void ForceModelRendererUpdate()
        {
            if (modelRenderer != null && _currentModelInstance != null)
            {
                modelRenderer.UpdateModelTransform(_currentModelInstance.transform, modelOffset);
            }
        }
        
        private void OnDestroy()
        {
            if (_currentModelInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_currentModelInstance);
                }
                else
                {
                    DestroyImmediate(_currentModelInstance);
                }
            }
        }
    }
}