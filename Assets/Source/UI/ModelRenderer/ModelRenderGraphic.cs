using System.Collections.Generic;
using JetBrains.Annotations;

namespace MageLock.ModelRenderer
{
    using UnityEngine;
    using UnityEngine.UI;
    
    public enum RenderTextureSize
    {
        Size128 = 128,
        Size256 = 256,
        Size512 = 512,
        Size1024 = 1024,
        Size2048 = 2048
    }
    
    [RequireComponent(typeof(CanvasRenderer))]
    public class ModelRenderGraphic : MaskableGraphic
    {
        [Header("Render Settings")]
        [SerializeField] private RenderTextureSize renderTextureWidth = RenderTextureSize.Size512;
        [SerializeField] private RenderTextureSize renderTextureHeight = RenderTextureSize.Size512;

        [Header("Camera Settings")]
        [SerializeField] private LayerMask cameraCullingMask = ~0;
        [SerializeField] private Color cameraBackgroundColor = Color.clear;
        [SerializeField] private float cameraFieldOfView = 60f;
        [SerializeField] private float cameraNearClipPlane = 0.1f;
        [SerializeField] private float cameraFarClipPlane = 1000f;
        [SerializeField] private float cameraDepth = -1f;
        [SerializeField] private CameraClearFlags cameraClearFlags = CameraClearFlags.SolidColor;
        [SerializeField] private Transform modelTransform;
        [SerializeField] private Vector3 modelOffset;
        [SerializeField] private Vector3 cameraRotation;
        [SerializeField] private bool useUniqueLayer;
        
        private Camera _renderCamera;
        private Vector3 _additionalModelOffset;
        private RenderTexture _renderTexture;
        private CanvasRenderer _canvasRenderer;
        private Bounds? _cachedBounds;
        private int _temporaryRenderLayer = -1;
        private bool _isVisible = true;

        protected override void Awake()
        {
            base.Awake();

            if (_canvasRenderer == null)
            {
                _canvasRenderer = GetComponent<CanvasRenderer>();
            }

            if (Application.isPlaying && modelTransform != null)
            {
                UpdateModelTransform(modelTransform);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Dispose();
        }
        
        private void LateUpdate()
        {
            AdjustCameraToModel(_renderCamera);
            bool currentlyVisible = isActiveAndEnabled && _canvasRenderer.cull == false;

            if (currentlyVisible != _isVisible)
            {
                _isVisible = currentlyVisible;
                ToggleCameraRendering(_isVisible);
            }
        }

        private void ToggleCameraRendering(bool enable)
        {
            if (_renderCamera == null)
            {
                return;
            }

            _renderCamera.enabled = enable;
        }
        
        private void InitializeCamera()
        {
            if (_renderCamera != null)
            {
                return;
            }

            GameObject cameraObject = new($"RenderCamera for ({gameObject.name})")
            {
                hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable
            };

            _renderCamera = cameraObject.AddComponent<Camera>();
            ApplyCameraSettings();
        }

        private void ApplyCameraSettings()
        {
            if (_renderCamera == null)
            {
                return;
            }

            if (UseUniqueLayer && _temporaryRenderLayer == -1)
            {
                _temporaryRenderLayer = TemporaryLayerManager.GetAvailableLayer();
            }

            _renderCamera.cullingMask = UseUniqueLayer ? 1 << _temporaryRenderLayer : cameraCullingMask;
            _renderCamera.clearFlags = cameraClearFlags;
            _renderCamera.backgroundColor = cameraBackgroundColor;
            _renderCamera.fieldOfView = cameraFieldOfView;
            _renderCamera.nearClipPlane = cameraNearClipPlane;
            _renderCamera.farClipPlane = cameraFarClipPlane;
            _renderCamera.depth = cameraDepth;
            _renderCamera.targetTexture = _renderTexture;
        }

        private void ForceRenderTextureUpdate()
        {
            if (_canvasRenderer == null)
            {
                _canvasRenderer = GetComponent<CanvasRenderer>();
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                _renderTexture = null;
            }
            
            _renderTexture = GetOrCreateRenderTexture();
            SetMaterialDirty();
        }

        private RenderTexture GetOrCreateRenderTexture()
        {
            int width = (int)renderTextureWidth;
            int height = (int)renderTextureHeight;

            RenderTextureFormat bestFormat = GetBestRenderTextureFormat();
            RenderTexture texture = RenderTexturePool.AcquireTexture(width, height, depth: 16, bestFormat);
            
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            return texture;
            
            static RenderTextureFormat GetBestRenderTextureFormat()
            {
                if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
                {
                    return RenderTextureFormat.ARGB32; 
                }

                if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Default))
                {
                    return RenderTextureFormat.Default;
                }
                
                if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565))
                {
                    return RenderTextureFormat.RGB565; 
                }
                
                return RenderTextureFormat.ARGB4444;
            }
        }
        
        private void CacheBounds()
        {
            if (modelTransform == null || _cachedBounds != null)
            {
                return;
            }

            _cachedBounds = CalculateBoundsFromMeshes(modelTransform);
        }

        private Bounds CalculateBoundsFromMeshes(Transform root)
        {
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();

            if (meshFilters.Length == 0)
            {
                return new Bounds(root.position, Vector3.zero);
            }

            Bounds bounds = new(root.position, Vector3.zero);

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh == null)
                {
                    continue;
                }
                
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Matrix4x4 localToWorld = meshFilter.transform.localToWorldMatrix;
                Vector3[] corners = new Vector3[8];
                
                GetBoundsCorners(meshBounds, localToWorld, corners);
                
                foreach (Vector3 corner in corners)
                {
                    bounds.Encapsulate(corner);
                }
            }

            return bounds;
        }
           
        private void GetBoundsCorners(Bounds bounds, Matrix4x4 localToWorld, Vector3[] corners)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            corners[0] = localToWorld.MultiplyPoint3x4(center + new Vector3(-extents.x, -extents.y, -extents.z));
            corners[1] = localToWorld.MultiplyPoint3x4(center + new Vector3(-extents.x, -extents.y, extents.z));
            corners[2] = localToWorld.MultiplyPoint3x4(center + new Vector3(-extents.x, extents.y, -extents.z));
            corners[3] = localToWorld.MultiplyPoint3x4(center + new Vector3(-extents.x, extents.y, extents.z));
            corners[4] = localToWorld.MultiplyPoint3x4(center + new Vector3(extents.x, -extents.y, -extents.z));
            corners[5] = localToWorld.MultiplyPoint3x4(center + new Vector3(extents.x, -extents.y, extents.z));
            corners[6] = localToWorld.MultiplyPoint3x4(center + new Vector3(extents.x, extents.y, -extents.z));
            corners[7] = localToWorld.MultiplyPoint3x4(center + new Vector3(extents.x, extents.y, extents.z));
        }

        private Bounds GetBounds()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                return CalculateBoundsFromMeshes(modelTransform);
            }
            
            if (_cachedBounds == null)
            {
                CacheBounds();
            }
            
            return _cachedBounds ?? new Bounds(Vector3.zero, Vector3.zero);
        }

        public void AdjustCameraToModel(Camera targetCamera)
        {
            if (modelTransform == null || targetCamera == null)
            {
                return;
            }

            (Vector3 position, Vector3 lookAt) = GetCameraTransformData(targetCamera);

            targetCamera.transform.position = position;
            targetCamera.transform.LookAt(lookAt);
        }
        
        private (Vector3 position, Vector3 lookAt) GetCameraTransformData(Camera targetCamera)
        {
            if (modelTransform == null || targetCamera == null)
            {
                return (Vector3.zero, Vector3.zero);
            }

            Bounds bounds = GetBounds();
            float modelSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float distance = modelSize / Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            Vector3 targetPoint = bounds.center + GetModelOffset();
            Vector3 position = targetPoint + Vector3.back * distance;

            return (position, targetPoint);
        }
        
        private void DestroyCamera()
        {
            if (_renderCamera != null)
            {
                GameObject cameraObject = _renderCamera.gameObject;

                if (cameraObject == null)
                {
                    return;
                }

                if (!Application.isPlaying && Application.isEditor)
                {
                    DestroyImmediate(cameraObject);
                }
                else
                {
                    Destroy(cameraObject);
                }
            }

            RenderTexturePool.ReleaseTexture(_renderTexture);
            _renderTexture = null;
        }

        public void UpdateGraphicFromPreviewCamera(Camera previewCamera, RenderTexture previewTexture)
        {
            if (previewCamera == null || previewTexture == null)
            {
                return;
            }

            AdjustCameraToModel(previewCamera);
            _renderTexture = previewTexture;
            _canvasRenderer.SetTexture(_renderTexture);
        }
        
        public void UpdateModelTransform(Transform newModelTransform, Vector3 additionalModelOffset = new())
        {
            this._additionalModelOffset = additionalModelOffset;
            modelTransform = newModelTransform;
            modelTransform.rotation = Quaternion.Euler(cameraRotation);
            _cachedBounds = null; 
            
            ForceRenderTextureUpdate();
            InitializeCamera();
            CacheBounds();
            UpdateTransformLayer();
        }
        
        private void UpdateTransformLayer()
        {
            if (modelTransform == null)
            {
                return;
            }

            int assignedLayer = UseUniqueLayer ? _temporaryRenderLayer : modelTransform.gameObject.layer;
            SetLayerRecursively(modelTransform, assignedLayer);
        }
        
        private void SetLayerRecursively(Transform obj, int layer)
        {
            if (obj == null)
            {
                return;
            }

            obj.gameObject.layer = layer;

            foreach (Transform child in obj)
            {
                SetLayerRecursively(child, layer);
            }
        }

        public void Dispose()
        {
            if (Application.isPlaying)
            {
                TemporaryLayerManager.ReleaseLayer(_temporaryRenderLayer);
                DestroyCamera();
            }
        }
        
        [UsedImplicitly] public Vector3 GetModelOffset() => modelOffset + _additionalModelOffset;
        
        public override Texture mainTexture
        {
            get
            {
                if (_renderTexture != null)
                {
                    return _renderTexture;
                }
                
                return Texture2D.blackTexture;
            }
        }
        
        public RenderTextureSize RenderTextureWidth
        {
            get => renderTextureWidth;
            set => renderTextureWidth = value;
        }
        public RenderTextureSize RenderTextureHeight
        {
            get => renderTextureHeight;
            set => renderTextureHeight = value;
        }

        public LayerMask CameraCullingMask
        {
            get => cameraCullingMask;
            set => cameraCullingMask = value;
        }

        public Color CameraBackgroundColor
        {
            get => cameraBackgroundColor;
            set => cameraBackgroundColor = value;
        }

        public float CameraFieldOfView
        {
            get => cameraFieldOfView;
            set => cameraFieldOfView = value;
        }

        public float CameraNearClipPlane
        {
            get => cameraNearClipPlane;
            set => cameraNearClipPlane = value;
        }

        public float CameraFarClipPlane
        {
            get => cameraFarClipPlane;
            set => cameraFarClipPlane = value;
        }

        public CameraClearFlags CameraClearFlags
        {
            get => cameraClearFlags;
            set => cameraClearFlags = value;
        }

        public Transform ModelTransform
        {
            get => modelTransform;
            set => modelTransform = value;
        }

        public bool UseUniqueLayer
        {
            get => useUniqueLayer;
            set => useUniqueLayer = value;
        }

        public Vector3 ModelOffset
        {
            get => modelOffset;
            set => modelOffset = value;
        }

        public Vector3 CameraRotation
        {
            get => cameraRotation;
            set => cameraRotation = value;
        }
    }
    
    public static class TemporaryLayerManager
    {
        private const int MinLayer = 8;  
        private const int MaxLayer = 31; 

        private static readonly HashSet<int> UsedLayers = new();

        public static int GetAvailableLayer()
        {
            for (int i = MinLayer; i <= MaxLayer; i++)
            {
                if (!string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                {
                    continue;
                }

                if (UsedLayers.Add(i))
                {
                    return i;
                }
            }

            Debug.LogWarning("No available layers! Defaulting to 'Default' layer.");
            return 0;
        }

        public static void ReleaseLayer(int layer)
        {
            UsedLayers.Remove(layer);
        }
    }
}