using UnityEngine;

namespace BrawlLine.ModelRenderer
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
        
        private GameObject currentModelInstance;
        
        public Transform CurrentModelTransform => currentModelInstance?.transform;
        
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
            
            if (currentModelInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(currentModelInstance);
                }
                else
                {
                    DestroyImmediate(currentModelInstance);
                }
                    
                currentModelInstance = null;
            }
            
            currentModelInstance = Instantiate(modelPrefab);
            currentModelInstance.name = $"{modelPrefab.name}_Instance";
            
            currentModelInstance.transform.position = Vector3.zero;
            currentModelInstance.transform.rotation = Quaternion.Euler(modelRotation);
            currentModelInstance.transform.localScale = modelScale;
            
            currentModelInstance.SetActive(true);
            
            modelRenderer.UpdateModelTransform(currentModelInstance.transform, modelOffset);
            
            return currentModelInstance.transform;
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
            
            if (currentModelInstance != null && modelRenderer != null)
            {
                modelRenderer.UpdateModelTransform(currentModelInstance.transform, modelOffset);
            }
        }
        
        public void UpdateModelRotation(Vector3 newRotation)
        {
            modelRotation = newRotation;
            
            if (currentModelInstance != null)
            {
                currentModelInstance.transform.rotation = Quaternion.Euler(modelRotation);
                
                if (modelRenderer != null)
                {
                    modelRenderer.UpdateModelTransform(currentModelInstance.transform, modelOffset);
                }
            }
        }
        
        public void UpdateModelScale(Vector3 newScale)
        {
            modelScale = newScale;
            
            if (currentModelInstance != null)
            {
                currentModelInstance.transform.localScale = modelScale;
                
                if (modelRenderer != null)
                {
                    modelRenderer.UpdateModelTransform(currentModelInstance.transform, modelOffset);
                }
            }
        }
        
        public void ForceModelRendererUpdate()
        {
            if (modelRenderer != null && currentModelInstance != null)
            {
                modelRenderer.UpdateModelTransform(currentModelInstance.transform, modelOffset);
            }
        }
        
        private void OnDestroy()
        {
            if (currentModelInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(currentModelInstance);
                }
                else
                {
                    DestroyImmediate(currentModelInstance);
                }
            }
        }
    }
}