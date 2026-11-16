using UnityEngine;

namespace Project22.ResumeSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ResumeDraggable : MonoBehaviour
    {
        [SerializeField] private MeshRenderer paperRenderer;
        [SerializeField] private Canvas documentCanvas;
        [SerializeField] private bool keepInsideCamera = true;
        [SerializeField] private Vector2 documentSize = new(5f, 7f);


        private Vector3 dragOffset;
        private float dragPlaneZ;

        public int SortingOrder => paperRenderer != null ? paperRenderer.sortingOrder : 0;

        public void Configure(MeshRenderer renderer, Canvas canvas, Vector2 size)
        {
            paperRenderer = renderer;
            documentCanvas = canvas;
            documentSize = size;
        }

        public void BeginDrag(Camera targetCamera, Vector2 screenPosition)
        {
            if (WorkInput.Blocked || WorkInput.DesktopBlocked) return;
            dragPlaneZ = transform.position.z;
            dragOffset = transform.position - ScreenToDragPlane(targetCamera, screenPosition);

            // One active application: never raise paper above the modal/HUD canvas.
            const int sortingOrder = 20;
            if (paperRenderer != null)
            {
                paperRenderer.sortingOrder = sortingOrder;
            }

            if (documentCanvas != null)
            {
                documentCanvas.overrideSorting = true;
                documentCanvas.sortingOrder = sortingOrder + 1;
            }
        }

        public void Drag(Camera targetCamera, Vector2 screenPosition)
        {
            if (WorkInput.Blocked || WorkInput.DesktopBlocked) return;
            Vector3 targetPosition = ScreenToDragPlane(targetCamera, screenPosition) + dragOffset;
            targetPosition.z = dragPlaneZ;

            if (keepInsideCamera && targetCamera.orthographic)
            {
                targetPosition = ClampInsideCamera(targetCamera, targetPosition);
            }

            transform.position = targetPosition;
        }

        public void EndDrag()
        {
            // Reserved for snap zones, accept/reject trays and paper-ball actions.
        }

        private Vector3 ScreenToDragPlane(Camera targetCamera, Vector2 screenPosition)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            if (Mathf.Abs(ray.direction.z) < Mathf.Epsilon)
            {
                return transform.position;
            }

            float distance = (dragPlaneZ - ray.origin.z) / ray.direction.z;
            return ray.GetPoint(distance);
        }

        private Vector3 ClampInsideCamera(Camera targetCamera, Vector3 position)
        {
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;
            Vector3 cameraPosition = targetCamera.transform.position;
            Vector2 halfDocument = documentSize * 0.5f;

            position.x = Mathf.Clamp(
                position.x,
                cameraPosition.x - halfWidth + halfDocument.x,
                cameraPosition.x + halfWidth - halfDocument.x);
            position.y = Mathf.Clamp(
                position.y,
                cameraPosition.y - halfHeight + halfDocument.y,
                cameraPosition.y + halfHeight - halfDocument.y);

            return position;
        }
    }
}
