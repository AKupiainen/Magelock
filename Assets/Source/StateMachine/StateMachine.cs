using System;
using System.Collections.Generic;

namespace BrawlLine.StateMachine
{
    /// <summary>
    /// Basic state machine
    /// </summary>
    public class StateMachine
    {
        private Dictionary<Type, IState> states = new Dictionary<Type, IState>();
        private IState currentState;

        public IState CurrentState => currentState;
        public T CurrentStateAs<T>() where T : class, IState => currentState as T;

        /// <summary>
        /// Add a state to the machine
        /// </summary>
        public void AddState<T>(T state) where T : class, IState
        {
            if (state is BaseState baseState)
                baseState.Initialize(this);

            states[typeof(T)] = state;
        }

        /// <summary>
        /// Get a specific state
        /// </summary>
        public T GetState<T>() where T : class, IState
        {
            return states.TryGetValue(typeof(T), out IState state) ? state as T : null;
        }

        /// <summary>
        /// Change to a new state
        /// </summary>
        public void ChangeState<T>() where T : class, IState
        {
            if (states.TryGetValue(typeof(T), out IState newState))
            {
                currentState?.OnExit();
                currentState = newState;
                currentState.OnEnter();
            }
        }

        /// <summary>
        /// Update the current state
        /// </summary>
        public void Update(float deltaTime)
        {
            currentState?.OnUpdate(deltaTime);
        }

        /// <summary>
        /// Check if state exists
        /// </summary>
        public bool HasState<T>() where T : class, IState
        {
            return states.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Remove a state
        /// </summary>
        public void RemoveState<T>() where T : class, IState
        {
            states.Remove(typeof(T));
        }

        /// <summary>
        /// Clear all states
        /// </summary>
        public void Clear()
        {
            currentState?.OnExit();
            currentState = null;
            states.Clear();
        }
    }
}