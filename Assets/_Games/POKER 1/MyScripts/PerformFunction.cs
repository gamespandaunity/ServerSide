//
using System.Collections;
using UnityEngine;

namespace POKER
{
    public class PerformFunction : MonoBehaviour
    {
        public float TimeDelay = 1.5f;
        public enum Task
        {
            None,
            ToggleState,
            Destroy
        }
        public Task CurrentTask;

        // Start is called before the first frame update
        void Start()
        {

        }
        Coroutine myCoroutine;
        private void OnEnable()
        {
            if (CurrentTask == Task.ToggleState)
            {
                myCoroutine = StartCoroutine(ToggleState(TimeDelay));
            }
            else if (CurrentTask == Task.Destroy)
            {
                Invoke("DestroyObj", TimeDelay);
            }
        }

        public void StopPrevRoutine()
        {
            if (myCoroutine != null)
                StopCoroutine("ToggleState");
        }

        void DestroyObj()
        {
            Destroy(gameObject);
        }

        IEnumerator ToggleState(float timeDelay)
        {
            yield return new WaitForSeconds(timeDelay);
            if (gameObject.name == "ChatMessageMox")
                yield return new WaitUntil(() => Pot.instance.tipDialogueTextObject == null);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            StopPrevRoutine();
        }
    }
}