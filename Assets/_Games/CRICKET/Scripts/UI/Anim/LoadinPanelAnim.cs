
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace Cricket
{

    public class LoadinPanelAnim : Singleton<LoadinPanelAnim>
    {
        public Image[] ball;

        public GameObject holder;

        public void StartAnim()
        {
            holder.SetActive(value: true);

        }


        public void ResetAnim()
        {
            Sequence s = DOTween.Sequence();
            s.Insert(0f, ball[0].DOFade(0f, 0f));
            s.Insert(0f, ball[1].DOFade(0f, 0f));
            s.Insert(0f, ball[2].DOFade(0f, 0f));
        }


        // The looping fade sequence currently driving the dots. Kept so each new cycle can KILL the
        // previous one: Animation() runs on an InvokeRepeating and every call used to create a fresh
        // SetLoops(-1) sequence that was never disposed, so infinite tweens piled up for the whole app
        // lifetime, all fading the same Images.
        private Sequence _dotsSeq;

        public void StartAnim1()
        {
            CancelInvoke("Animation");   // never stack a second repeater on top of a running one
            InvokeRepeating("Animation", 0f, 1.8f);
        }


        public void Animation()
        {
            // A reconnect RELOADS the scene, so these Images are destroyed while this DontDestroyOnLoad
            // singleton (and its InvokeRepeating) survives. Every 1.8s the repeater then kept building
            // tweens against dead targets, which is the "DOTWEEN can't add elements to an inactive/killed
            // Sequence" flood that fills the tail of the reconnect logs. Stop the repeater instead —
            // StartAnim1 restarts it when the panel is legitimately shown again.
            if (ball == null || ball.Length < 3)
            {
                CancelInvoke("Animation");
                return;
            }
            for (int i = 0; i < ball.Length; i++)
            {
                if (ball[i] == null)
                {
                    CancelInvoke("Animation");
                    return;
                }
            }

            ResetAnim1();
            ball[0].gameObject.SetActive(value: true);
            ball[1].gameObject.SetActive(value: true);
            ball[2].gameObject.SetActive(value: true);

            // Dispose the previous cycle before starting a new one (see _dotsSeq).
            if (_dotsSeq != null && _dotsSeq.IsActive()) _dotsSeq.Kill();

            Sequence sequence = DOTween.Sequence();
            _dotsSeq = sequence;
            sequence.Insert(0f, ball[0].DOFade(1f, 0.4f));
            sequence.Insert(0.4f, ball[1].DOFade(1f, 0.4f));
            sequence.Insert(0.8f, ball[2].DOFade(1f, 0.4f));
            sequence.Insert(1.2f, ball[0].DOFade(0f, 0.2f));
            sequence.Insert(1.4f, ball[1].DOFade(0f, 0.2f));
            sequence.Insert(1.6f, ball[2].DOFade(0f, 0.2f));
            sequence.SetLoops(-1);
        }


        public void ResetAnim1()
        {
            Sequence s = DOTween.Sequence();
            s.Insert(0f, ball[0].DOFade(0f, 0f));
            s.Insert(0f, ball[1].DOFade(0f, 0f));
            s.Insert(0f, ball[2].DOFade(0f, 0f));

        }

        public void CancelAnim()
        {
            holder.SetActive(value: false);

            // Hiding the panel must also stop the work behind it: without this the repeater kept
            // running (and kept spawning infinite sequences) for the rest of the session.
            CancelInvoke("Animation");
            if (_dotsSeq != null && _dotsSeq.IsActive()) _dotsSeq.Kill();
            _dotsSeq = null;
        }

        private void OnDisable()
        {
            CancelInvoke("Animation");
            if (_dotsSeq != null && _dotsSeq.IsActive()) _dotsSeq.Kill();
            _dotsSeq = null;
        }

    }
}