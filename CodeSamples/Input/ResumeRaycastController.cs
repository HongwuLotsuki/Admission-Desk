using UnityEngine;
using UnityEngine.InputSystem;

namespace Project22.ResumeSystem
{
    [DisallowMultipleComponent]
    public sealed class ResumeRaycastController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask documentLayerMask = ~0;
        [Min(0.1f)]
        [SerializeField] private float maxRayDistance = 100f;

        private ResumeDraggable selectedResume;

        public void Configure(Camera cameraToUse)
        {
            targetCamera = cameraToUse;
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (WorkInput.Blocked || WorkInput.DesktopBlocked)
            {
                selectedResume?.EndDrag(); selectedResume = null; return;
            }
            Pointer pointer = Pointer.current;
            if (pointer == null || targetCamera == null)
            {
                return;
            }

            Vector2 screenPosition = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                if (UnityEngine.EventSystems.EventSystem.current == null ||
                    !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) TryBeginDrag(screenPosition);
            }

            if (selectedResume != null && pointer.press.isPressed)
            {
                selectedResume.Drag(targetCamera, screenPosition);
            }

            if (selectedResume != null && pointer.press.wasReleasedThisFrame)
            {
                selectedResume.EndDrag();
                selectedResume = null;
            }
        }

        private void TryBeginDrag(Vector2 screenPosition)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(
                ray,
                maxRayDistance,
                documentLayerMask);

            ResumeDraggable topResume = null;
            int topSortingOrder = int.MinValue;

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                ResumeDraggable resume = hit.collider.GetComponentInParent<ResumeDraggable>();
                if (resume != null && resume.SortingOrder > topSortingOrder)
                {
                    topResume = resume;
                    topSortingOrder = resume.SortingOrder;
                }
            }

            selectedResume = topResume;
            if (topResume != null)
            {
                var presentation = FindFirstObjectByType<DocumentPresentationController>();
                if (presentation != null)
                {
                    if (presentation.IsReading) presentation.PutDown();
                    else presentation.PickUp();
                    selectedResume = null;
                }
                else topResume.BeginDrag(targetCamera, screenPosition);
            }
        }
    }
}
