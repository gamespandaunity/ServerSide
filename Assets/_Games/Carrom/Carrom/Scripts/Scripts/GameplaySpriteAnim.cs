using UnityEngine;

namespace BEKStudio
{
    public class GameplaySpriteAnim : MonoBehaviour
    {
        public Vector3 StartingPos;
        public Vector3 EndingPos;
        public float Duration = 1f;
        public float Delay = 0.5f;

        private LTDescr tween;
        private bool isDestroyed = false;

        private void OnEnable()
        {
            StartTween();
        }

        private void OnDisable()
        {
            LeanTween.cancel(gameObject);
            transform.localPosition = StartingPos;
        }

        private void OnDestroy()
        {
            isDestroyed = true;
            LeanTween.cancel(gameObject);
        }

        private void StartTween()
        {
            if (isDestroyed) return;  // Prevent starting the tween if the object is destroyed

            gameObject.GetComponent<SpriteRenderer>().enabled = true;
            tween = LeanTween.moveLocal(gameObject, EndingPos, Duration).setOnComplete(() =>
            {
                if (isDestroyed) return;  // Prevent accessing the object if it is destroyed

                gameObject.GetComponent<SpriteRenderer>().enabled = false;
                LeanTween.delayedCall(Delay, () =>
                {
                    if (isDestroyed) return;  // Prevent accessing the object if it is destroyed

                    transform.localPosition = StartingPos;
                    StartTween();
                });
            });
        }
    }
}