using UnityEngine;
using UnityEngine.UI;

namespace MageLock.UI
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(CanvasRenderer), typeof(ParticleSystem))]
    [AddComponentMenu("UI/Effects/Extensions/UIParticleSystem")]
    public class UIParticleSystem : MaskableGraphic
    {
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        [Tooltip("Having this enabled run the system in LateUpdate rather than in Update making it faster but less precise (more clunky)")]
        public bool fixedTime = true;

        [Tooltip("Enables 3d rotation for the particles")]
        public bool use3dRotation;

        private Transform _cachedTransform;
        private ParticleSystem _pSystem;
        private ParticleSystem.Particle[] _particles;
        private readonly UIVertex[] _quad = new UIVertex[4];
        private Vector4 _imageUV = Vector4.zero;
        private ParticleSystem.TextureSheetAnimationModule _textureSheetAnimation;
        private int _textureSheetAnimationFrames;
        private Vector2 _textureSheetAnimationFrameSize;
        private ParticleSystemRenderer _pRenderer;
        private bool _isInitialised;

        private Material _currentMaterial;
        private Texture _currentTexture;
        private ParticleSystem.MainModule _mainModule;

        public override Texture mainTexture => _currentTexture;

        private bool Initialize()
        {
            if (_cachedTransform == null)
            {
                _cachedTransform = transform;
            }
            if (_pSystem == null)
            {
                _pSystem = GetComponent<ParticleSystem>();

                if (_pSystem == null)
                {
                    return false;
                }

                _mainModule = _pSystem.main;
                if (_pSystem.main.maxParticles > 14000)
                {
                    _mainModule.maxParticles = 14000;
                }

                _pRenderer = _pSystem.GetComponent<ParticleSystemRenderer>();
                if (_pRenderer != null)
                    _pRenderer.enabled = false;
                
                _currentMaterial = material;

                if (_currentMaterial && _currentMaterial.HasProperty(MainTex))
                {
                    _currentTexture = _currentMaterial.mainTexture;
                    if (_currentTexture == null)
                        _currentTexture = Texture2D.whiteTexture;
                }

                material = _currentMaterial;
                _mainModule.scalingMode = ParticleSystemScalingMode.Hierarchy;

                _particles = null;
            }
            
            if (_particles == null)
                _particles = new ParticleSystem.Particle[_pSystem.main.maxParticles];

            _imageUV = new Vector4(0, 0, 1, 1);

            _textureSheetAnimation = _pSystem.textureSheetAnimation;
            _textureSheetAnimationFrames = 0;
            _textureSheetAnimationFrameSize = Vector2.zero;
            if (_textureSheetAnimation.enabled)
            {
                _textureSheetAnimationFrames = _textureSheetAnimation.numTilesX * _textureSheetAnimation.numTilesY;
                _textureSheetAnimationFrameSize = new Vector2(1f / _textureSheetAnimation.numTilesX, 1f / _textureSheetAnimation.numTilesY);
            }

            return true;
        }

        protected override void Awake()
        {
            base.Awake();
            if (!Initialize())
                enabled = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (!Initialize())
                {
                    return;
                }
            }
#endif
            vh.Clear();

            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (!_isInitialised && !_pSystem.main.playOnAwake)
            {
                _pSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                _isInitialised = true;
            }

            Vector2 temp = Vector2.zero;
            Vector2 corner1 = Vector2.zero;
            Vector2 corner2 = Vector2.zero;
            
            int count = _pSystem.GetParticles(_particles);

            for (int i = 0; i < count; ++i)
            {
                ParticleSystem.Particle particle = _particles[i];
                Vector2 position = _mainModule.simulationSpace == ParticleSystemSimulationSpace.Local ? particle.position : _cachedTransform.InverseTransformPoint(particle.position);
               
                float rotation = -particle.rotation * Mathf.Deg2Rad;
                float rotation90 = rotation + Mathf.PI / 2;

                Color32 currentColor = particle.GetCurrentColor(_pSystem);
                float size = particle.GetCurrentSize(_pSystem) * 0.5f;

                if (_mainModule.scalingMode == ParticleSystemScalingMode.Shape)
                    position /= canvas.scaleFactor;

                Vector4 particleUV = _imageUV;

                if (_textureSheetAnimation.enabled)
                {
                    float frameProgress = 1 - (particle.remainingLifetime / particle.startLifetime);

                    if (_textureSheetAnimation.frameOverTime.curveMin != null)
                    {
                        frameProgress = _textureSheetAnimation.frameOverTime.curveMin.Evaluate(1 - (particle.remainingLifetime / particle.startLifetime));
                    }
                    else if (_textureSheetAnimation.frameOverTime.curve != null)
                    {
                        frameProgress = _textureSheetAnimation.frameOverTime.curve.Evaluate(1 - (particle.remainingLifetime / particle.startLifetime));
                    }
                    else if (_textureSheetAnimation.frameOverTime.constant > 0)
                    {
                        frameProgress = _textureSheetAnimation.frameOverTime.constant - (particle.remainingLifetime / particle.startLifetime);
                    }

                    frameProgress = Mathf.Repeat(frameProgress * _textureSheetAnimation.cycleCount, 1);
                    int frame = 0;

                    switch (_textureSheetAnimation.animation)
                    {
                        case ParticleSystemAnimationType.WholeSheet:
                            frame = Mathf.FloorToInt(frameProgress * _textureSheetAnimationFrames);
                            break;

                        case ParticleSystemAnimationType.SingleRow:
                            frame = Mathf.FloorToInt(frameProgress * _textureSheetAnimation.numTilesX);

                            int row = _textureSheetAnimation.rowIndex;
                            frame += row * _textureSheetAnimation.numTilesX;
                            break;
                    }

                    frame %= _textureSheetAnimationFrames;

                    particleUV.x = (frame % _textureSheetAnimation.numTilesX) * _textureSheetAnimationFrameSize.x;
                    particleUV.y = 1.0f - Mathf.FloorToInt(frame / _textureSheetAnimation.numTilesX) * _textureSheetAnimationFrameSize.y;
                    particleUV.z = particleUV.x + _textureSheetAnimationFrameSize.x;
                    particleUV.w = particleUV.y + _textureSheetAnimationFrameSize.y;
                }

                temp.x = particleUV.x;
                temp.y = particleUV.y;

                _quad[0] = UIVertex.simpleVert;
                _quad[0].color = currentColor;
                _quad[0].uv0 = temp;

                temp.x = particleUV.x;
                temp.y = particleUV.w;
                _quad[1] = UIVertex.simpleVert;
                _quad[1].color = currentColor;
                _quad[1].uv0 = temp;

                temp.x = particleUV.z;
                temp.y = particleUV.w;
                _quad[2] = UIVertex.simpleVert;
                _quad[2].color = currentColor;
                _quad[2].uv0 = temp;

                temp.x = particleUV.z;
                temp.y = particleUV.y;
                _quad[3] = UIVertex.simpleVert;
                _quad[3].color = currentColor;
                _quad[3].uv0 = temp;

                if (rotation == 0)
                {
                    // no rotation
                    corner1.x = position.x - size;
                    corner1.y = position.y - size;
                    corner2.x = position.x + size;
                    corner2.y = position.y + size;

                    temp.x = corner1.x;
                    temp.y = corner1.y;
                    _quad[0].position = temp;
                    temp.x = corner1.x;
                    temp.y = corner2.y;
                    _quad[1].position = temp;
                    temp.x = corner2.x;
                    temp.y = corner2.y;
                    _quad[2].position = temp;
                    temp.x = corner2.x;
                    temp.y = corner1.y;
                    _quad[3].position = temp;
                }
                else
                {
                    if (use3dRotation)
                    {
                        Vector3 pos3d = _mainModule.simulationSpace == ParticleSystemSimulationSpace.Local ? particle.position : _cachedTransform.InverseTransformPoint(particle.position);

                        if (_mainModule.scalingMode == ParticleSystemScalingMode.Shape)
                            position /= canvas.scaleFactor;

                        Vector3[] verts = new Vector3[4]
                        {
                            new(-size, -size, 0),
                            new(-size, size, 0),
                            new(size, size, 0),
                            new(size, -size, 0)
                        };

                        Quaternion particleRotation = Quaternion.Euler(particle.rotation3D);

                        _quad[0].position = pos3d + particleRotation * verts[0];
                        _quad[1].position = pos3d + particleRotation * verts[1];
                        _quad[2].position = pos3d + particleRotation * verts[2];
                        _quad[3].position = pos3d + particleRotation * verts[3];
                    }
                    else
                    {
                        Vector2 right = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation)) * size;
                        Vector2 up = new Vector2(Mathf.Cos(rotation90), Mathf.Sin(rotation90)) * size;

                        _quad[0].position = position - right - up;
                        _quad[1].position = position - right + up;
                        _quad[2].position = position + right + up;
                        _quad[3].position = position + right - up;
                    }
                }

                vh.AddUIVertexQuad(_quad);
            }
        }

        private void Update()
        {
            if (!fixedTime && Application.isPlaying)
            {
                _pSystem.Simulate(Time.unscaledDeltaTime, false, false, true);
                SetAllDirty();

                if ((_currentMaterial != null && _currentTexture != _currentMaterial.mainTexture) ||
                    (material != null && _currentMaterial != null && material.shader != _currentMaterial.shader))
                {
                    _pSystem = null;
                    Initialize();
                }
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                SetAllDirty();
            }
            else
            {
                if (fixedTime)
                {
                    _pSystem.Simulate(Time.unscaledDeltaTime, false, false, true);
                    SetAllDirty();
                    if ((_currentMaterial != null && _currentTexture != _currentMaterial.mainTexture) ||
                        (material != null && _currentMaterial != null && material.shader != _currentMaterial.shader))
                    {
                        _pSystem = null;
                        Initialize();
                    }
                }
            }
            if (material == _currentMaterial)
                return;
            _pSystem = null;
            Initialize();
        }

        protected override void OnDestroy()
        {
            _currentMaterial = null;
            _currentTexture = null;
        }
    }
}