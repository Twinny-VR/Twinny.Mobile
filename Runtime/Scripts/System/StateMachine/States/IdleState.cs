using Twinny.Core;
using UnityEngine;

namespace Twinny.Multiplatform
{

    public class IdleState : IState
    {
        private PlatformManager m_manager;

        public IdleState(PlatformManager managerOwner) => m_manager = managerOwner;

        public void Enter() => SetGameMode();

        public void Exit() { }

        public void Update() { }

        private void SetGameMode()
        {
#if FUSION2
//TODO Check if we are in multiplayer session
            ChangeState(new TwinnyXRMultiplayer());
            return;
#endif
            GameMode.ChangeState(new PlatformSingleplayer(m_manager));
        }
    }
}
