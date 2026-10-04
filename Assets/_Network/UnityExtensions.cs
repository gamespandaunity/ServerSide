using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace UnityExtensions
{
    // ==================== TRANSFORM EXTENSIONS ====================
    public static class TransformExtensions
    {
        /// <summary>
        /// Reset transform to default values
        /// </summary>
        public static Transform Reset(this Transform transform)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            return transform;
        }

        /// <summary>
        /// Set position with optional axis masking
        /// </summary>
        public static Transform SetPosition(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            Vector3 pos = transform.position;
            if (x.HasValue) pos.x = x.Value;
            if (y.HasValue) pos.y = y.Value;
            if (z.HasValue) pos.z = z.Value;
            transform.position = pos;
            return transform;
        }

        /// <summary>
        /// Set local position with optional axis masking
        /// </summary>
        public static Transform SetLocalPosition(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            Vector3 pos = transform.localPosition;
            if (x.HasValue) pos.x = x.Value;
            if (y.HasValue) pos.y = y.Value;
            if (z.HasValue) pos.z = z.Value;
            transform.localPosition = pos;
            return transform;
        }

        /// <summary>
        /// Set scale with optional axis masking
        /// </summary>
        public static Transform SetScale(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            Vector3 scale = transform.localScale;
            if (x.HasValue) scale.x = x.Value;
            if (y.HasValue) scale.y = y.Value;
            if (z.HasValue) scale.z = z.Value;
            transform.localScale = scale;
            return transform;
        }

        /// <summary>
        /// Set uniform scale
        /// </summary>
        public static Transform SetScale(this Transform transform, float uniformScale)
        {
            transform.localScale = Vector3.one * uniformScale;
            return transform;
        }

        /// <summary>
        /// Destroy all children
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Object.Destroy(transform.GetChild(i).gameObject);
                else
                    Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Get all children transforms
        /// </summary>
        public static Transform[] GetChildren(this Transform transform)
        {
            Transform[] children = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                children[i] = transform.GetChild(i);
            return children;
        }

        /// <summary>
        /// Find child by name recursively
        /// </summary>
        public static Transform FindDeep(this Transform transform, string name)
        {
            Transform result = transform.Find(name);
            if (result != null) return result;

            foreach (Transform child in transform)
            {
                result = child.FindDeep(name);
                if (result != null) return result;
            }
            return null;
        }
    }

    // ==================== GAMEOBJECT EXTENSIONS ====================
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Get or add component
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// Check if has component
        /// </summary>
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() != null;
        }

        /// <summary>
        /// Set layer recursively
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
                child.gameObject.SetLayerRecursively(layer);
        }

        /// <summary>
        /// Set active and return self for chaining
        /// </summary>
        public static GameObject SetActive(this GameObject gameObject, bool active)
        {
            gameObject.SetActive(active);
            return gameObject;
        }

        /// <summary>
        /// Toggle active state
        /// </summary>
        public static GameObject ToggleActive(this GameObject gameObject)
        {
            gameObject.SetActive(!gameObject.activeSelf);
            return gameObject;
        }
    }

    // ==================== COMPONENT EXTENSIONS ====================
    public static class ComponentExtensions
    {
        /// <summary>
        /// Get component in parent or children
        /// </summary>
        public static T GetComponentInFamily<T>(this Component component) where T : Component
        {
            T result = component.GetComponent<T>();
            if (result == null) result = component.GetComponentInParent<T>();
            if (result == null) result = component.GetComponentInChildren<T>();
            return result;
        }

        /// <summary>
        /// Destroy component safely
        /// </summary>
        public static void DestroySafe(this Component component)
        {
            if (component != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(component);
                else
                    Object.DestroyImmediate(component);
            }
        }
    }

    // ==================== VECTOR EXTENSIONS ====================
    public static class VectorExtensions
    {
        /// <summary>
        /// Set individual components of Vector3
        /// </summary>
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }

        /// <summary>
        /// Set individual components of Vector2
        /// </summary>
        public static Vector2 With(this Vector2 vector, float? x = null, float? y = null)
        {
            return new Vector2(x ?? vector.x, y ?? vector.y);
        }

        /// <summary>
        /// Convert Vector3 to Vector2 (drop Z)
        /// </summary>
        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        /// <summary>
        /// Convert Vector2 to Vector3 (add Z)
        /// </summary>
        public static Vector3 ToVector3(this Vector2 vector, float z = 0f)
        {
            return new Vector3(vector.x, vector.y, z);
        }

        /// <summary>
        /// Get random point within bounds
        /// </summary>
        public static Vector3 RandomPoint(this Bounds bounds)
        {
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }
    }

    // ==================== COLLECTION EXTENSIONS ====================
    public static class CollectionExtensions
    {
        /// <summary>
        /// Get random element from array
        /// </summary>
        public static T RandomElement<T>(this T[] array)
        {
            return array.Length == 0 ? default(T) : array[Random.Range(0, array.Length)];
        }

        /// <summary>
        /// Get random element from list
        /// </summary>
        public static T RandomElement<T>(this List<T> list)
        {
            return list.Count == 0 ? default(T) : list[Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Shuffle list in place
        /// </summary>
        public static void Shuffle<T>(this List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        /// <summary>
        /// Check if list is null or empty
        /// </summary>
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        /// <summary>
        /// Add multiple items to list
        /// </summary>
        public static void AddRange<T>(this List<T> list, params T[] items)
        {
            list.AddRange(items);
        }
    }

    // ==================== UI EXTENSIONS ====================
    public static class UIExtensions
    {
        /// <summary>
        /// Set alpha of CanvasGroup
        /// </summary>
        public static CanvasGroup SetAlpha(this CanvasGroup canvasGroup, float alpha)
        {
            canvasGroup.alpha = alpha;
            return canvasGroup;
        }

        /// <summary>
        /// Set interactable state of CanvasGroup
        /// </summary>
        public static CanvasGroup SetInteractable(this CanvasGroup canvasGroup, bool interactable)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
            return canvasGroup;
        }

        /// <summary>
        /// Set color of Image
        /// </summary>
        public static Image SetColor(this Image image, Color color)
        {
            image.color = color;
            return image;
        }

        /// <summary>
        /// Set alpha of Image
        /// </summary>
        public static Image SetAlpha(this Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
            return image;
        }

        /// <summary>
        /// Set text content
        /// </summary>
        public static Text SetText(this Text text, string content)
        {
            text.text = content;
            return text;
        }

        /// <summary>
        /// Set TextMeshPro text content
        /// </summary>
        public static TextMeshProUGUI SetText(this TextMeshProUGUI text, string content)
        {
            text.text = content;
            return text;
        }

        /// <summary>
        /// Set Button interactable state
        /// </summary>
        public static Button SetInteractable(this Button button, bool interactable)
        {
            button.interactable = interactable;
            return button;
        }
    }

    // ==================== COLOR EXTENSIONS ====================
    public static class ColorExtensions
    {
        /// <summary>
        /// Set alpha of color
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        /// <summary>
        /// Create color from hex string
        /// </summary>
        public static Color FromHex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;
            return Color.white;
        }

        /// <summary>
        /// Convert color to hex string
        /// </summary>
        public static string ToHex(this Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }
    }

    // ==================== RIGIDBODY EXTENSIONS ====================
    public static class RigidbodyExtensions
    {
        /// <summary>
        /// Set velocity with optional axis masking
        /// </summary>
        public static Rigidbody SetVelocity(this Rigidbody rb, float? x = null, float? y = null, float? z = null)
        {
            Vector3 velocity = rb.linearVelocity;
            if (x.HasValue) velocity.x = x.Value;
            if (y.HasValue) velocity.y = y.Value;
            if (z.HasValue) velocity.z = z.Value;
            rb.linearVelocity = velocity;
            return rb;
        }

        /// <summary>
        /// Add force in direction
        /// </summary>
        public static Rigidbody AddForceInDirection(this Rigidbody rb, Vector3 direction, float force, ForceMode mode = ForceMode.Force)
        {
            rb.AddForce(direction.normalized * force, mode);
            return rb;
        }

        /// <summary>
        /// Stop all movement
        /// </summary>
        public static Rigidbody Stop(this Rigidbody rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return rb;
        }
    }

    // ==================== ANIMATION EXTENSIONS ====================
    public static class AnimationExtensions
    {
        /// <summary>
        /// Play animation and get its length
        /// </summary>
        public static float PlayAndGetLength(this Animator animator, string stateName, int layer = 0)
        {
            animator.Play(stateName, layer);
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            return stateInfo.length;
        }

        /// <summary>
        /// Check if animation is playing
        /// </summary>
        public static bool IsPlaying(this Animator animator, string stateName, int layer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
        }
    }

    // ==================== DELAY AND TIMING EXTENSIONS ====================
    public static class DelayExtensions
    {
        /// <summary>
        /// Execute action after delay
        /// </summary>
        public static Coroutine Delay(this MonoBehaviour mono, float seconds, System.Action action)
        {
            return mono.StartCoroutine(DelayCoroutine(seconds, action));
        }

        /// <summary>
        /// Execute action after delay with condition check
        /// </summary>
        public static Coroutine DelayIf(this MonoBehaviour mono, float seconds, System.Func<bool> condition, System.Action action)
        {
            return mono.StartCoroutine(DelayIfCoroutine(seconds, condition, action));
        }

        /// <summary>
        /// Execute action after frame delay
        /// </summary>
        public static Coroutine DelayFrame(this MonoBehaviour mono, System.Action action, int frames = 1)
        {
            return mono.StartCoroutine(DelayFrameCoroutine(action, frames));
        }

        /// <summary>
        /// Execute action at end of frame
        /// </summary>
        public static Coroutine DelayEndOfFrame(this MonoBehaviour mono, System.Action action)
        {
            return mono.StartCoroutine(DelayEndOfFrameCoroutine(action));
        }

        /// <summary>
        /// Execute action when condition becomes true
        /// </summary>
        public static Coroutine DelayUntil(this MonoBehaviour mono, System.Func<bool> condition, System.Action action)
        {
            return mono.StartCoroutine(DelayUntilCoroutine(condition, action));
        }

        /// <summary>
        /// Execute action while condition is true, then execute final action
        /// </summary>
        public static Coroutine DelayWhile(this MonoBehaviour mono, System.Func<bool> condition, System.Action whileAction, System.Action finalAction = null)
        {
            return mono.StartCoroutine(DelayWhileCoroutine(condition, whileAction, finalAction));
        }

        /// <summary>
        /// Execute action repeatedly with delay
        /// </summary>
        public static Coroutine DelayRepeat(this MonoBehaviour mono, float interval, System.Action action, int times = -1)
        {
            return mono.StartCoroutine(DelayRepeatCoroutine(interval, action, times));
        }

        /// <summary>
        /// Execute action with random delay between min and max
        /// </summary>
        public static Coroutine DelayRandom(this MonoBehaviour mono, float minSeconds, float maxSeconds, System.Action action)
        {
            float randomDelay = Random.Range(minSeconds, maxSeconds);
            return mono.StartCoroutine(DelayCoroutine(randomDelay, action));
        }

        // Coroutine implementations
        private static System.Collections.IEnumerator DelayCoroutine(float seconds, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }

        private static System.Collections.IEnumerator DelayIfCoroutine(float seconds, System.Func<bool> condition, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            if (condition?.Invoke() == true)
                action?.Invoke();
        }

        private static System.Collections.IEnumerator DelayFrameCoroutine(System.Action action, int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;
            action?.Invoke();
        }

        private static System.Collections.IEnumerator DelayEndOfFrameCoroutine(System.Action action)
        {
            yield return new WaitForEndOfFrame();
            action?.Invoke();
        }

        private static System.Collections.IEnumerator DelayUntilCoroutine(System.Func<bool> condition, System.Action action)
        {
            yield return new WaitUntil(condition);
            action?.Invoke();
        }

        private static System.Collections.IEnumerator DelayWhileCoroutine(System.Func<bool> condition, System.Action whileAction, System.Action finalAction)
        {
            while (condition?.Invoke() == true)
            {
                whileAction?.Invoke();
                yield return null;
            }
            finalAction?.Invoke();
        }

        private static System.Collections.IEnumerator DelayRepeatCoroutine(float interval, System.Action action, int times)
        {
            int count = 0;
            while (times < 0 || count < times)
            {
                action?.Invoke();
                yield return new WaitForSeconds(interval);
                count++;
            }
        }
    }

    // ==================== EASING EXTENSIONS ====================
    public static class EasingExtensions
    {
        public enum EaseType
        {
            Linear,
            EaseInQuad, EaseOutQuad, EaseInOutQuad,
            EaseInCubic, EaseOutCubic, EaseInOutCubic,
            EaseInQuart, EaseOutQuart, EaseInOutQuart,
            EaseInQuint, EaseOutQuint, EaseInOutQuint,
            EaseInSine, EaseOutSine, EaseInOutSine,
            EaseInExpo, EaseOutExpo, EaseInOutExpo,
            EaseInCirc, EaseOutCirc, EaseInOutCirc,
            EaseInBack, EaseOutBack, EaseInOutBack,
            EaseInElastic, EaseOutElastic, EaseInOutElastic,
            EaseInBounce, EaseOutBounce, EaseInOutBounce
        }

        /// <summary>
        /// Animate float value with easing
        /// </summary>
        public static Coroutine AnimateFloat(this MonoBehaviour mono, float from, float to, float duration,
            System.Action<float> updateAction, EaseType easeType = EaseType.Linear, System.Action onComplete = null)
        {
            return mono.StartCoroutine(AnimateFloatCoroutine(from, to, duration, updateAction, easeType, onComplete));
        }

        /// <summary>
        /// Animate Vector3 with easing
        /// </summary>
        public static Coroutine AnimateVector3(this MonoBehaviour mono, Vector3 from, Vector3 to, float duration,
            System.Action<Vector3> updateAction, EaseType easeType = EaseType.Linear, System.Action onComplete = null)
        {
            return mono.StartCoroutine(AnimateVector3Coroutine(from, to, duration, updateAction, easeType, onComplete));
        }

        /// <summary>
        /// Animate Color with easing
        /// </summary>
        public static Coroutine AnimateColor(this MonoBehaviour mono, Color from, Color to, float duration,
            System.Action<Color> updateAction, EaseType easeType = EaseType.Linear, System.Action onComplete = null)
        {
            return mono.StartCoroutine(AnimateColorCoroutine(from, to, duration, updateAction, easeType, onComplete));
        }

        /// <summary>
        /// Ease transform position
        /// </summary>
        public static Coroutine EasePosition(this Transform transform, Vector3 targetPosition, float duration,
            EaseType easeType = EaseType.EaseOutQuad, System.Action onComplete = null)
        {
            MonoBehaviour mono = transform.GetComponent<MonoBehaviour>();
            if (mono == null) return null;

            Vector3 startPosition = transform.position;
            return mono.AnimateVector3(startPosition, targetPosition, duration,
                pos => transform.position = pos, easeType, onComplete);
        }

        /// <summary>
        /// Ease transform scale
        /// </summary>
        public static Coroutine EaseScale(this Transform transform, Vector3 targetScale, float duration,
            EaseType easeType = EaseType.EaseOutBack, System.Action onComplete = null)
        {
            MonoBehaviour mono = transform.GetComponent<MonoBehaviour>();
            if (mono == null) return null;

            Vector3 startScale = transform.localScale;
            return mono.AnimateVector3(startScale, targetScale, duration,
                scale => transform.localScale = scale, easeType, onComplete);
        }

        /// <summary>
        /// Ease CanvasGroup alpha
        /// </summary>
        public static Coroutine EaseAlpha(this CanvasGroup canvasGroup, float targetAlpha, float duration,
            EaseType easeType = EaseType.EaseOutQuad, System.Action onComplete = null)
        {
            MonoBehaviour mono = canvasGroup.GetComponent<MonoBehaviour>();
            if (mono == null) return null;

            float startAlpha = canvasGroup.alpha;
            return mono.AnimateFloat(startAlpha, targetAlpha, duration,
                alpha => canvasGroup.alpha = alpha, easeType, onComplete);
        }

        /// <summary>
        /// Ease Image color
        /// </summary>
        public static Coroutine EaseColor(this Image image, Color targetColor, float duration,
            EaseType easeType = EaseType.EaseOutQuad, System.Action onComplete = null)
        {
            MonoBehaviour mono = image.GetComponent<MonoBehaviour>();
            if (mono == null) return null;

            Color startColor = image.color;
            return mono.AnimateColor(startColor, targetColor, duration,
                color => image.color = color, easeType, onComplete);
        }

        // Coroutine implementations
        private static System.Collections.IEnumerator AnimateFloatCoroutine(float from, float to, float duration,
            System.Action<float> updateAction, EaseType easeType, System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEasing(t, easeType);
                float value = Mathf.Lerp(from, to, easedT);
                updateAction?.Invoke(value);
                yield return null;
            }
            updateAction?.Invoke(to);
            onComplete?.Invoke();
        }

        private static System.Collections.IEnumerator AnimateVector3Coroutine(Vector3 from, Vector3 to, float duration,
            System.Action<Vector3> updateAction, EaseType easeType, System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEasing(t, easeType);
                Vector3 value = Vector3.Lerp(from, to, easedT);
                updateAction?.Invoke(value);
                yield return null;
            }
            updateAction?.Invoke(to);
            onComplete?.Invoke();
        }

        private static System.Collections.IEnumerator AnimateColorCoroutine(Color from, Color to, float duration,
            System.Action<Color> updateAction, EaseType easeType, System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEasing(t, easeType);
                Color value = Color.Lerp(from, to, easedT);
                updateAction?.Invoke(value);
                yield return null;
            }
            updateAction?.Invoke(to);
            onComplete?.Invoke();
        }

        // Easing function implementations
        private static float ApplyEasing(float t, EaseType easeType)
        {
            switch (easeType)
            {
                case EaseType.Linear: return t;
                case EaseType.EaseInQuad: return t * t;
                case EaseType.EaseOutQuad: return t * (2f - t);
                case EaseType.EaseInOutQuad: return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
                case EaseType.EaseInCubic: return t * t * t;
                case EaseType.EaseOutCubic: return (--t) * t * t + 1f;
                case EaseType.EaseInOutCubic: return t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;
                case EaseType.EaseInSine: return 1f - Mathf.Cos(t * Mathf.PI / 2f);
                case EaseType.EaseOutSine: return Mathf.Sin(t * Mathf.PI / 2f);
                case EaseType.EaseInOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case EaseType.EaseInBack: return 2.70158f * t * t * t - 1.70158f * t * t;
                case EaseType.EaseOutBack: return 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
                case EaseType.EaseInOutBack:
                    const float c1 = 1.70158f;
                    const float c2 = c1 * 1.525f;
                    return t < 0.5f
                        ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
                case EaseType.EaseOutBounce:
                    const float n1 = 7.5625f;
                    const float d1 = 2.75f;
                    if (t < 1f / d1) return n1 * t * t;
                    else if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
                    else if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
                    else return n1 * (t -= 2.625f / d1) * t + 0.984375f;
                case EaseType.EaseInBounce: return 1f - ApplyEasing(1f - t, EaseType.EaseOutBounce);
                case EaseType.EaseInOutBounce:
                    return t < 0.5f
                        ? (1f - ApplyEasing(1f - 2f * t, EaseType.EaseOutBounce)) / 2f
                        : (1f + ApplyEasing(2f * t - 1f, EaseType.EaseOutBounce)) / 2f;
                default: return t;
            }
        }
    }

    // ==================== UTILITY EXTENSIONS ====================
    public static class UtilityExtensions
    {
        /// <summary>
        /// Remap value from one range to another
        /// </summary>
        public static float Remap(this float value, float fromLow, float fromHigh, float toLow, float toHigh)
        {
            return toLow + (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow);
        }

        /// <summary>
        /// Check if float is approximately zero
        /// </summary>
        public static bool IsApproximatelyZero(this float value, float threshold = 0.01f)
        {
            return Mathf.Abs(value) < threshold;
        }

        /// <summary>
        /// Clamp angle between -180 and 180
        /// </summary>
        public static float ClampAngle(this float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// Execute action after delay (legacy method - use DelayExtensions instead)
        /// </summary>
        public static Coroutine DelayedAction(this MonoBehaviour mono, System.Action action, float delay)
        {
            return mono.Delay(delay, action);
        }
    }

    // ==================== LAYERMASK EXTENSIONS ====================
    public static class LayerMaskExtensions
    {
        /// <summary>
        /// Check if layer is in LayerMask
        /// </summary>
        public static bool Contains(this LayerMask layerMask, int layer)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        /// <summary>
        /// Add layer to LayerMask
        /// </summary>
        public static LayerMask Add(this LayerMask layerMask, int layer)
        {
            return layerMask | (1 << layer);
        }

        /// <summary>
        /// Remove layer from LayerMask
        /// </summary>
        public static LayerMask Remove(this LayerMask layerMask, int layer)
        {
            return layerMask & ~(1 << layer);
        }
    }
}