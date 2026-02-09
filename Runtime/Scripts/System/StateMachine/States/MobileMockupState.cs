using Concept.Core;
using Twinny.Core;
using Twinny.Mobile.Env;

namespace Twinny.Mobile
{
    internal sealed class MobileMockupState : IState
    {
        private readonly TwinnyMobileManager _manager;

        public MobileMockupState(TwinnyMobileManager manager)
        {
            _manager = manager;
        }

        public void Enter()
        {
            if (_manager == null) return;
            SkyboxHandler.SwitchSkybox(0);
            CallbackHub.CallAction<ITwinnyMobileCallbacks>(callback => callback.OnEnterMockupMode());
        }

        public void Exit() { }

        public void Update() { }
    }
}
