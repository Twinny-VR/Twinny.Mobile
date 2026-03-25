using Concept.Core;
using Twinny.Core;
using Twinny.Multiplatform.Env;

namespace Twinny.Multiplatform
{
    internal sealed class ImmersiveState : IState
    {
        private readonly PlatformManager _manager;

        public ImmersiveState(PlatformManager manager)
        {
            _manager = manager;
        }

        public void Enter()
        {
            if (_manager == null) return;
            CallbackHub.CallAction<IPlatformCallbacks>(callback => callback.OnEnterImmersiveMode());
            SkyboxHandler.SwitchSkybox(1);
        }

        public void Exit() {
            CallbackHub.CallAction<IPlatformCallbacks>(callback => callback.OnExitImmersiveMode());
        }

        public void Update() { }
    }
}
