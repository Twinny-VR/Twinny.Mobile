using Twinny.Core;
using UnityEngine;

namespace Twinny.Mobile
{
    public interface ITwinnyMobileCallbacks : ICallbacks
    {
        void OnStartInteract(GameObject gameObject);
        void OnStopInteract(GameObject gameObject);
        void OnStartTeleport();
        void OnTeleport();
    }
}
