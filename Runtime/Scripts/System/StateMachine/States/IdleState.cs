using Twinny.Core;
using UnityEngine;

namespace Twinny.Mobile
{

    public class IdleState : IState
    {
        private TwinnyMobileManager m_manager;

        public IdleState(TwinnyMobileManager managerOwner) => m_manager = managerOwner;

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
            GameMode.ChangeState(new TwinnyMobileSingleplayer(m_manager));
        }
    }
}
