using TMPro;
using UnityEngine;

namespace MageLock.Gameplay
{
    [RequireComponent(typeof(TextMeshPro))]
    public class BillboardText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;
        
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
            
            if (Camera.main != null)
            {
                forward = Camera.main.transform.forward;
                up = Camera.main.transform.up;
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