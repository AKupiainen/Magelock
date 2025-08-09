using TMPro;
using UnityEngine;

namespace MageLock.Gameplay
{
    [RequireComponent(typeof(TextMeshPro))]
    public class BillboardText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;
        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Awake()
        {
            if (textMesh == null)
                textMesh = GetComponent<TextMeshPro>();
        }
        
        private void LateUpdate()
        {
            FaceCameraPlane();
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
        
        public void SetText(string message)
        {
            if (textMesh != null)
                textMesh.text = message;
        }
    }
}