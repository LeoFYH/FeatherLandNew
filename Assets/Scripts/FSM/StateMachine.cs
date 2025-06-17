using System;
using System.Collections.Generic;
using UnityEngine;

// State Machine Manager

namespace FSM
{
    public class StateMachine
    {
        private Dictionary<Type, StateBase> stateDic = new Dictionary<Type, StateBase>();
        private StateBase currentState;

        public GameObject currObj;

        public StateMachine(GameObject obj)
        {
            currObj = obj;
        }

        public void AddState<T>(T state) where T : StateBase
        {
            var type = typeof(T);
            if (stateDic.ContainsKey(type))
            {
                Debug.Log($"已添加状态[{type.FullName}],请勿重复添加");
                return;
            }
            stateDic.Add(type, state);
            if (currentState == null)
            {
                currentState = state;
                currentState.OnEnter();
            }
        }

        public void ChangeState<T>() where T : StateBase
        {
            var type = typeof(T);
            if (!stateDic.ContainsKey(type))
            {
                Debug.Log($"不存在状态[{type.FullName}]");
                return;
            }
            currentState?.OnExit();
            currentState = stateDic[type];
            currentState.OnEnter();
        }

        public void OnUpdate()
        {
            currentState?.OnUpdate();
        }
    }
}