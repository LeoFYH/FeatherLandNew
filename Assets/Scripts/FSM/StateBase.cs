using FSM;

// State Base Class

public abstract class StateBase
{
    protected StateMachine currMachine;
    
    public StateBase(StateMachine machine)
    {
        currMachine = machine;
    }

    public abstract void OnEnter();
    public abstract void OnUpdate();
    public abstract void OnExit();
}
