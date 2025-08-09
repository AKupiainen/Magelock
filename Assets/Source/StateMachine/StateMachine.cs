using System;
using System.Collections.Generic;

namespace MageLock.StateMachine
{
    /// <summary>
    /// Basic state machine
    /// </summary>
    public class StateMachine
    {
        private readonly Dictionary<Type, IState> states = new();
        private IState _currentState;

        public IState CurrentState => _currentState;
        public T CurrentStateAs<T>() where T : class, IState => _currentState as T;

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
                _currentState?.OnExit();
                _currentState = newState;
                _currentState.OnEnter();
            }
        }

        /// <summary>
        /// Update the current state
        /// </summary>
        public void Update(float deltaTime)
        {
            _currentState?.OnUpdate(deltaTime);
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
            _currentState?.OnExit();
            _currentState = null;
            states.Clear();
        }
    }
}