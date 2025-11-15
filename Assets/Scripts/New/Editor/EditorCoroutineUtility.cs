using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace BirdGame.Editor
{
    public static class EditorCoroutineUtility
    {
        private class CoroutineRunner : EditorWindow
        {
            static CoroutineRunner()
            {
                EditorApplication.update += Update;
            }

            private static void Update()
            {
                if (coroutines.Count > 0)
                {
                    var current = coroutines.Peek();
                    if (!current.MoveNext())
                    {
                        coroutines.Dequeue();
                    }
                }
            }

            private static readonly Queue<IEnumerator> coroutines = new Queue<IEnumerator>();

            public static void StartCoroutine(IEnumerator routine)
            {
                coroutines.Enqueue(routine);
            }
        }

        public static void StartCoroutine(IEnumerator routine, object owner)
        {
            CoroutineRunner.StartCoroutine(routine);
        }
    }
}