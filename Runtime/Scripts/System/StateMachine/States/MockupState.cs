using Concept.Core;
using Twinny.Core;
using Twinny.Multiplatform.Env;

namespace Twinny.Multiplatform
{
    internal sealed class MockupState : IState
    {
        private readonly PlatformManager _manager;

        public MockupState(PlatformManager manager)
        {
            _manager = manager;
        }

        public void Enter()
        {
            if (_manager == null) return;
            SkyboxHandler.SwitchSkybox(0);
            CallbackHub.CallAction<IPlatformCallbacks>(callback => callback.OnEnterMockupMode());
        }

        public void Exit() {
            CallbackHub.CallAction<IPlatformCallbacks>(callback => callback.OnExitMockupMode());
        }

        public void Update() { }
    }
}
