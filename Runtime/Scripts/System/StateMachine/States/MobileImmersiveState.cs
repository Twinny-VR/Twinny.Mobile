using Concept.Core;
using Twinny.Core;
using Twinny.Mobile.Env;

namespace Twinny.Mobile
{
    internal sealed class MobileImmersiveState : IState
    {
        private readonly TwinnyMobileManager _manager;

        public MobileImmersiveState(TwinnyMobileManager manager)
        {
            _manager = manager;
        }

        public void Enter()
        {
            if (_manager == null) return;
            CallbackHub.CallAction<ITwinnyMobileCallbacks>(callback => callback.OnEnterImmersiveMode());
            SkyboxHandler.SwitchSkybox(1);
        }

        public void Exit() {
            CallbackHub.CallAction<ITwinnyMobileCallbacks>(callback => callback.OnExitImmersiveMode());
        }

        public void Update() { }
    }
}
