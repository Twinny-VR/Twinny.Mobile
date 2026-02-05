using Concept.Core;
using Twinny.Core;
using UnityEngine;

namespace Twinny.Mobile
{
    public class TwinnyMobileManager : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Initialize();
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void Initialize()
        {
            StateMachine.ChangeState(new IdleState(this));
        }
    }
}
