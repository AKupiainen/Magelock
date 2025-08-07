using UnityEngine;

namespace MageLock.UI
{
    public class CameraCanvasAssigner : MonoBehaviour
    {
        [SerializeField] private string cameraTag = "MainCamera";
        
        private void Awake()
        {
            AssignCameraToCanvas();
        }

        private void AssignCameraToCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            
            if (canvas == null)
            {
                Debug.LogError("No Canvas component found!");
                return;
            }
            
            Camera camera = GameObject.FindGameObjectWithTag(cameraTag)?.GetComponent<Camera>();

            if (camera == null)
            {
                Debug.LogError($"No Camera found with tag '{cameraTag}'!");
                return;
            }
            
            canvas.worldCamera = camera;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            
            Debug.Log($"Camera '{camera.name}' assigned to Canvas '{canvas.name}'");
        }
    }
}