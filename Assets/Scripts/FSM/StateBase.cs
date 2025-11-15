using QFramework;

// State Base Class

namespace BirdGame
{
    public abstract class StateBase : IController
    {
        protected StateMachine currMachine;

        public StateBase(StateMachine machine)
        {
            currMachine = machine;
        }

        public abstract void OnEnter();
        public abstract void OnUpdate();
        public abstract void OnExit();

        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }
    }
}