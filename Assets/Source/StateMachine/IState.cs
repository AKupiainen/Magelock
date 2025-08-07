namespace MageLock.StateMachine
{
    /// <summary>
    /// Basic state interface
    /// </summary>
    public interface IState
    {
        void OnEnter();
        void OnUpdate(float deltaTime);
        void OnExit();
    }

    /// <summary>
    /// Base state class for easy implementation
    /// </summary>
    public abstract class BaseState : IState
    {
        protected StateMachine StateMachine;

        public virtual void Initialize(StateMachine stateMachine)
        {
            this.StateMachine = stateMachine;
        }

        public virtual void OnEnter() { }
        public virtual void OnUpdate(float deltaTime) { }
        public virtual void OnExit() { }
    }
}