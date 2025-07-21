using System.Collections;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 统一规范协程， 所有协程在此调用
    /// </summary>
    public interface IMonoSystem : ISystem
    {
        Coroutine StartCoroutine(string methodName);
        Coroutine StartCoroutine(string methodName, object value);
        Coroutine StartCoroutine(IEnumerator routine);
        void StopCoroutine(string method);
        void StopCoroutine(IEnumerator routine);
        void StopCoroutine(Coroutine coroutine);
        void StopAllCoroutines();
    }

    public class MonoSystem : AbstractSystem, IMonoSystem
    {
        private GameEntry gameEntry;
        
        protected override void OnInit()
        {
            gameEntry = GameObject.Find("GameEntry").GetComponent<GameEntry>();
            GameObject.DontDestroyOnLoad(gameEntry.gameObject);
        }

        public Coroutine StartCoroutine(string methodName)
        {
            return gameEntry.StartCoroutine(methodName);
        }

        public Coroutine StartCoroutine(string methodName, object value)
        {
            return gameEntry.StartCoroutine(methodName, value);
        }

        public Coroutine StartCoroutine(IEnumerator routine)
        {
            return gameEntry.StartCoroutine(routine);
        }

        public void StopCoroutine(string method)
        {
            gameEntry.StopCoroutine(method);
        }

        public void StopCoroutine(IEnumerator routine)
        {
            gameEntry.StopCoroutine(routine);
        }

        public void StopCoroutine(Coroutine coroutine)
        {
            gameEntry.StopCoroutine(coroutine);
        }

        public void StopAllCoroutines()
        {
            gameEntry.StopAllCoroutines();
        }
    }
}