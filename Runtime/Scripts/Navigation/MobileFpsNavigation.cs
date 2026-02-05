using System;
using Concept.Core;
using Twinny.Mobile;
using UnityEngine;
using UnityEngine.AI;

namespace Twinny.Mobile.Navigation
{
    /// <summary>
    /// Moves a NavMeshAgent to tapped positions and optionally reports interactable hits.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MobileFpsNavigation : MonoBehaviour, IMobileInputCallbacks
    {
        [Header("Navigation")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private float _maxSampleDistance = 3f;
        [SerializeField] private int _navMeshAreaMask = NavMesh.AllAreas;

        [Header("Raycast")]
        [SerializeField] private LayerMask _interactableMask;

        /// <summary>
        /// Fired when a valid NavMesh destination is chosen.
        /// </summary>
        public event Action<Vector3> OnNavMeshClick;

        /// <summary>
        /// Fired when an interactable is clicked (based on layer mask).
        /// </summary>
        public event Action<Transform> OnInteractableClick;

        private void OnEnable()
        {
            EnsureReferences();
            CallbackHub.RegisterCallback<IMobileInputCallbacks>(this);
        }

        private void OnDisable()
        {
            CallbackHub.UnregisterCallback<IMobileInputCallbacks>(this);
        }

        private void OnValidate()
        {
            EnsureReferences();
            if (_maxSampleDistance < 0f) _maxSampleDistance = 0f;
        }

        public void OnSelectHit(RaycastHit hit)
        {
            if (_agent == null)
            {
                Debug.LogWarning("[MobileFpsNavigation] NavMeshAgent is null.");
                return;
            }

            Debug.Log(
                $"[MobileFpsNavigation] OnSelectHit {hit.collider.name} at {hit.point} " +
                $"(agent enabled={_agent.enabled}, onNavMesh={_agent.isOnNavMesh})"
            );

            if (TryMoveTo(hit.point))
                return;

            if (IsInteractable(hit.transform))
            {
                Debug.Log($"[MobileFpsNavigation] Interactable hit {hit.transform.name}");
                OnInteractableClick?.Invoke(hit.transform);
            }
        }

        public void OnPrimaryDown(float x, float y) { }
        public void OnPrimaryUp(float x, float y) { }
        public void OnPrimaryDrag(float dx, float dy) { }
        public void OnSelect(GameObject target) { }
        public void OnCancel() { }
        public void OnZoom(float delta) { }
        public void OnTwoFingerTap(Vector2 position) { }
        public void OnTwoFingerLongPress(Vector2 position) { }
        public void OnTwoFingerSwipe(Vector2 direction, Vector2 startPosition) { }
        public void OnThreeFingerTap(Vector2 position) { }
        public void OnThreeFingerSwipe(Vector2 direction, Vector2 startPosition) { }
        public void OnThreeFingerPinch(float delta) { }
        public void OnFourFingerTap() { }
        public void OnFourFingerSwipe(Vector2 direction) { }
        public void OnEdgeSwipe(EdgeDirection edge) { }
        public void OnForceTouch(float pressure) { }
        public void OnHapticTouch() { }
        public void OnBackTap(int tapCount) { }
        public void OnShake() { }
        public void OnTilt(Vector3 tiltRotation) { }
        public void OnDeviceRotated(DeviceOrientation orientation) { }
        public void OnPickUp() { }
        public void OnPutDown() { }
        public void OnAccessibilityAction(string actionName) { }
        public void OnScreenReaderGesture(string gestureType) { }
        public void OnNotificationAction(bool isQuickAction) { }

        private bool IsInteractable(Transform target)
        {
            if (target == null) return false;
            if (_interactableMask == 0) return false;
            int layerMask = 1 << target.gameObject.layer;
            return (_interactableMask.value & layerMask) != 0;
        }

        private bool TryMoveTo(Vector3 worldPos)
        {
            if (!NavMesh.SamplePosition(worldPos, out NavMeshHit navHit, _maxSampleDistance, _navMeshAreaMask))
            {
                Debug.Log($"[MobileFpsNavigation] NavMesh.SamplePosition failed at {worldPos}");
                return false;
            }

            Debug.Log(
                $"[MobileFpsNavigation] Moving to {navHit.position} " +
                $"(isStopped={_agent.isStopped}, speed={_agent.speed}, remaining={_agent.remainingDistance})"
            );
            if (_agent.isStopped) _agent.isStopped = false;
            _agent.SetDestination(navHit.position);
            OnNavMeshClick?.Invoke(navHit.position);
            return true;
        }

        private void EnsureReferences()
        {
            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();
        }
    }
}
