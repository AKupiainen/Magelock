#if UNITY_EDITOR
using UnityEngine;

namespace MageLock.ModelRenderer
{
    public static class ModelRenderGraphicMenu
    {
        [UnityEditor.MenuItem("GameObject/UI/Model Render Graphic", false, 2009)]
        public static void AddModelRenderGraphicToContextMenu()
        {
            foreach (GameObject selectedObject in UnityEditor.Selection.gameObjects)
            {
                GameObject newGameObject = new("Model Render Graphic", typeof(RectTransform), typeof(CanvasRenderer));

                ModelRenderGraphic graphic = newGameObject.AddComponent<ModelRenderGraphic>();
                newGameObject.transform.SetParent(selectedObject.transform, false);
                newGameObject.transform.localPosition = Vector3.zero;

                RectTransform rectTransform = newGameObject.transform as RectTransform;

                if (rectTransform != null)
                {
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    rectTransform.localScale = Vector3.one;
                    rectTransform.sizeDelta = new Vector2(300, 300);
                }

                graphic.raycastTarget = false;
            }
        }

        [UnityEditor.MenuItem("GameObject/UI/Model Render Graphic", true, 2009)]
        public static bool AddModelRenderGraphicToContextMenuValidation()
        {
            foreach (GameObject selectedObject in UnityEditor.Selection.gameObjects)
            {
                if (HasCanvasParent(selectedObject))
                {
                    return true;
                }
            }
            
            return false;
            
            static bool HasCanvasParent(GameObject gameObject)
            {
                Transform currentTransform = gameObject.transform;

                while (currentTransform != null)
                {
                    if (currentTransform.GetComponent<Canvas>() != null)
                    {
                        return true;
                    }

                    currentTransform = currentTransform.parent;
                }

                return false;
            }
        }
    }
}
#endif