using System;
using System.Collections;
using UnityEngine;

namespace AZUR
{
    internal static class AzurCoroutineRunner
    {
        private sealed class RunnerBehaviour : MonoBehaviour
        {
        }

        private static RunnerBehaviour _runner;

        public static void RunDelayed(double seconds, Action action)
        {
            if (action == null)
            {
                return;
            }

            EnsureRunner().StartCoroutine(RunDelayedCoroutine((float)Math.Max(0d, seconds), action));
        }

        private static RunnerBehaviour EnsureRunner()
        {
            if (_runner != null)
            {
                return _runner;
            }

            var gameObject = new GameObject("[AZUR] Coroutine Runner");
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            _runner = gameObject.AddComponent<RunnerBehaviour>();
            return _runner;
        }

        private static IEnumerator RunDelayedCoroutine(float seconds, Action action)
        {
            if (seconds > 0f)
            {
                yield return new WaitForSecondsRealtime(seconds);
            }

            action();
        }
    }
}
