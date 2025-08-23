using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Aldha.Tweening
{
    public enum Ease
    {
        Linear,
        InOutSine,
        OutSine,
        InSine,
        InOutQuad,
        OutQuad,
        InQuad,
        InOutCubic,
        OutCubic,
        InCubic
    }

    public struct TweenHandle
    {
        internal Coroutine Routine;
        internal TweenRunner Runner;
        public bool IsValid => Runner != null && Routine != null;

        public void Cancel()
        {
            if (IsValid)
                Runner.StopCoroutine(Routine);
            Routine = null; Runner = null;
        }
    }

    /// <summary>
    /// Host invisible para correr coroutines sin depender de tus MonoBehaviours.
    /// </summary>
    internal class TweenRunner : MonoBehaviour
    {
        private static TweenRunner _instance;
        public static TweenRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[TweenRunner]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<TweenRunner>();
                }
                return _instance;
            }
        }

        public TweenHandle Run(IEnumerator routine)
        {
            var h = new TweenHandle { Runner = this, Routine = StartCoroutine(routine) };
            return h;
        }
    }

    internal static class EaseUtil
    {
        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.InOutSine:  return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case Ease.OutSine:    return Mathf.Sin(t * Mathf.PI * 0.5f);
                case Ease.InSine:     return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                case Ease.InOutQuad:  return t < 0.5f ? 2f*t*t : 1f - Mathf.Pow(-2f*t + 2f, 2f)/2f;
                case Ease.OutQuad:    return 1f - (1f - t)*(1f - t);
                case Ease.InQuad:     return t*t;
                case Ease.InOutCubic: return t < 0.5f ? 4f*t*t*t : 1f - Mathf.Pow(-2f*t + 2f, 3f)/2f;
                case Ease.OutCubic:   return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InCubic:    return t*t*t;
                default:              return t; // Linear
            }
        }
    }

    public static class TweenExtensions
    {
        #region --- CanvasGroup: Fade + Interactividad ---
        /// <summary>
        /// Hace fade del alpha actual al target. 
        /// Si manageInteractability es true: 
        ///     alpha > threshold => interactable=true, blocksRaycasts=true
        ///     alpha <= threshold => ambos false.
        /// </summary>
        public static TweenHandle FadeTo(this CanvasGroup cg, float targetAlpha, float duration,
                                         Ease ease = Ease.InOutSine, bool unscaledTime = false,
                                         bool manageInteractability = true, float threshold = 0.99f)
        {
            return TweenRunner.Instance.Run(FadeCanvasGroupRoutine(cg, targetAlpha, duration, ease, unscaledTime, manageInteractability, threshold));
        }

        public static TweenHandle FadeIn(this CanvasGroup cg, float duration, Ease ease = Ease.InOutSine,
                                         bool unscaledTime = false, bool manageInteractability = true, float threshold = 0.99f)
            => cg.FadeTo(1f, duration, ease, unscaledTime, manageInteractability, threshold);

        public static TweenHandle FadeOut(this CanvasGroup cg, float duration, Ease ease = Ease.InOutSine,
                                          bool unscaledTime = false, bool manageInteractability = true, float threshold = 0.01f)
            => cg.FadeTo(0f, duration, ease, unscaledTime, manageInteractability, threshold);

        private static IEnumerator FadeCanvasGroupRoutine(CanvasGroup cg, float target, float duration, Ease ease, bool unscaled, bool manage, float threshold)
        {
            if (cg == null) yield break;
    
            if (manage && target > cg.alpha)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            float start = cg.alpha;
            float t = 0f;

            while (t < duration)
            {
                if (cg == null) yield break;

                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                float k = EaseUtil.Evaluate(ease, p);

                cg.alpha = Mathf.Lerp(start, target, k);
                yield return null;
            }

            if (cg == null) yield break;
            cg.alpha = target;

            if (manage)
            {
                bool on = cg.alpha >= threshold;
                cg.interactable = on;
                cg.blocksRaycasts = on;
            }
        }
        #endregion

        #region --- Transform: Move / Scale / Rotate ---
        public static TweenHandle MoveTo(this Transform tr, Vector3 target, float duration,
                                         Ease ease = Ease.InOutSine, bool unscaledTime = false, bool local = false)
        {
            return TweenRunner.Instance.Run(LerpVectorRoutine(
                getter: () => local ? tr.localPosition : tr.position,
                setter: v => { if (local) tr.localPosition = v; else tr.position = v; },
                target: target, duration: duration, ease: ease, unscaled: unscaledTime));
        }

        public static TweenHandle ScaleTo(this Transform tr, Vector3 target, float duration,
                                          Ease ease = Ease.InOutSine, bool unscaledTime = false)
        {
            return TweenRunner.Instance.Run(LerpVectorRoutine(
                getter: () => tr.localScale,
                setter: v => tr.localScale = v,
                target: target, duration: duration, ease: ease, unscaled: unscaledTime));
        }

        public static TweenHandle RotateTo(this Transform tr, Quaternion target, float duration,
                                           Ease ease = Ease.InOutSine, bool unscaledTime = false, bool local = false)
        {
            Func<Quaternion> get = () => local ? tr.localRotation : tr.rotation;
            Action<Quaternion> set = q => { if (local) tr.localRotation = q; else tr.rotation = q; };

            return TweenRunner.Instance.Run(LerpQuatRoutine(get, set, target, duration, ease, unscaledTime));
        }
        #endregion

        #region --- UI Graphic & SpriteRenderer: Fade ---
        public static TweenHandle FadeTo(this Graphic g, float targetAlpha, float duration,
                                         Ease ease = Ease.InOutSine, bool unscaledTime = false)
        {
            return TweenRunner.Instance.Run(LerpFloatRoutine(
                getter: () => g ? g.color.a : 0f,
                setter: a => { if (g) { var c = g.color; c.a = a; g.color = c; } },
                target: targetAlpha, duration: duration, ease: ease, unscaled: unscaledTime));
        }

        public static TweenHandle FadeTo(this SpriteRenderer sr, float targetAlpha, float duration,
                                         Ease ease = Ease.InOutSine, bool unscaledTime = false)
        {
            return TweenRunner.Instance.Run(LerpFloatRoutine(
                getter: () => sr ? sr.color.a : 0f,
                setter: a => { if (sr) { var c = sr.color; c.a = a; sr.color = c; } },
                target: targetAlpha, duration: duration, ease: ease, unscaled: unscaledTime));
        }
        #endregion

        #region --- Routines genéricas ---
        private static IEnumerator LerpVectorRoutine(Func<Vector3> getter, Action<Vector3> setter,
                                                     Vector3 target, float duration, Ease ease, bool unscaled)
        {
            Vector3 start = getter();
            float t = 0f;
            while (t < duration)
            {
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                setter(Vector3.LerpUnclamped(start, target, EaseUtil.Evaluate(ease, p)));
                yield return null;
            }
            setter(target);
        }

        private static IEnumerator LerpQuatRoutine(Func<Quaternion> getter, Action<Quaternion> setter,
                                                   Quaternion target, float duration, Ease ease, bool unscaled)
        {
            Quaternion start = getter();
            float t = 0f;
            while (t < duration)
            {
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                setter(Quaternion.SlerpUnclamped(start, target, EaseUtil.Evaluate(ease, p)));
                yield return null;
            }
            setter(target);
        }

        private static IEnumerator LerpFloatRoutine(Func<float> getter, Action<float> setter,
                                                    float target, float duration, Ease ease, bool unscaled)
        {
            float start = getter();
            float t = 0f;
            while (t < duration)
            {
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                setter(Mathf.LerpUnclamped(start, target, EaseUtil.Evaluate(ease, p)));
                yield return null;
            }
            setter(target);
        }
        #endregion
    }
}
