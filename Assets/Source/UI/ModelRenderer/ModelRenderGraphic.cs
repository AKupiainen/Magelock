using System.Collections.Generic;
using JetBrains.Annotations;

namespace BrawlLine.ModelRenderer
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
        
        private Camera renderCamera;
        private Vector3 additionalModelOffset;
        private RenderTexture renderTexture;
        private new CanvasRenderer canvasRenderer;
        private Bounds? cachedBounds;
        private int temporaryRenderLayer = -1;
        private bool isVisible = true;

        protected override void Awake()
        {
            base.Awake();

            if (canvasRenderer == null)
            {
                canvasRenderer = GetComponent<CanvasRenderer>();
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
            AdjustCameraToModel(renderCamera);
            bool currentlyVisible = isActiveAndEnabled && canvasRenderer.cull == false;

            if (currentlyVisible != isVisible)
            {
                isVisible = currentlyVisible;
                ToggleCameraRendering(isVisible);
            }
        }

        private void ToggleCameraRendering(bool enable)
        {
            if (renderCamera == null)
            {
                return;
            }

            renderCamera.enabled = enable;
        }
        
        private void InitializeCamera()
        {
            if (renderCamera != null)
            {
                return;
            }

            GameObject cameraObject = new($"RenderCamera for ({gameObject.name})")
            {
                hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable
            };

            renderCamera = cameraObject.AddComponent<Camera>();
            ApplyCameraSettings();
        }

        private void ApplyCameraSettings()
        {
            if (renderCamera == null)
            {
                return;
            }

            if (UseUniqueLayer && temporaryRenderLayer == -1)
            {
                temporaryRenderLayer = TemporaryLayerManager.GetAvailableLayer();
            }

            renderCamera.cullingMask = UseUniqueLayer ? 1 << temporaryRenderLayer : cameraCullingMask;
            renderCamera.clearFlags = cameraClearFlags;
            renderCamera.backgroundColor = cameraBackgroundColor;
            renderCamera.fieldOfView = cameraFieldOfView;
            renderCamera.nearClipPlane = cameraNearClipPlane;
            renderCamera.farClipPlane = cameraFarClipPlane;
            renderCamera.depth = cameraDepth;
            renderCamera.targetTexture = renderTexture;
        }

        private void ForceRenderTextureUpdate()
        {
            if (canvasRenderer == null)
            {
                canvasRenderer = GetComponent<CanvasRenderer>();
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                renderTexture = null;
            }
            
            renderTexture = GetOrCreateRenderTexture();
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
            if (modelTransform == null || cachedBounds != null)
            {
                return;
            }

            cachedBounds = CalculateBoundsFromMeshes(modelTransform);
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
            
            if (cachedBounds == null)
            {
                CacheBounds();
            }
            
            return cachedBounds ?? new Bounds(Vector3.zero, Vector3.zero);
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
            if (renderCamera != null)
            {
                GameObject cameraObject = renderCamera.gameObject;

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

            RenderTexturePool.ReleaseTexture(renderTexture);
            renderTexture = null;
        }

        public void UpdateGraphicFromPreviewCamera(Camera previewCamera, RenderTexture previewTexture)
        {
            if (previewCamera == null || previewTexture == null)
            {
                return;
            }

            AdjustCameraToModel(previewCamera);
            renderTexture = previewTexture;
            canvasRenderer.SetTexture(renderTexture);
        }
        
        public void UpdateModelTransform(Transform newModelTransform, Vector3 additionalModelOffset = new())
        {
            this.additionalModelOffset = additionalModelOffset;
            modelTransform = newModelTransform;
            modelTransform.rotation = Quaternion.Euler(cameraRotation);
            cachedBounds = null; 
            
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

            int assignedLayer = UseUniqueLayer ? temporaryRenderLayer : modelTransform.gameObject.layer;
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
                TemporaryLayerManager.ReleaseLayer(temporaryRenderLayer);
                DestroyCamera();
            }
        }
        
        [UsedImplicitly] public Vector3 GetModelOffset() => modelOffset + additionalModelOffset;
        
        public override Texture mainTexture
        {
            get
            {
                if (renderTexture != null)
                {
                    return renderTexture;
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

        private static readonly HashSet<int> usedLayers = new();

        public static int GetAvailableLayer()
        {
            for (int i = MinLayer; i <= MaxLayer; i++)
            {
                if (!string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                {
                    continue;
                }

                if (usedLayers.Add(i))
                {
                    return i;
                }
            }

            Debug.LogWarning("No available layers! Defaulting to 'Default' layer.");
            return 0;
        }

        public static void ReleaseLayer(int layer)
        {
            usedLayers.Remove(layer);
        }
    }
}