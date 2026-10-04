using UnityEngine;

namespace BallPool.Mechanics
{
    public class PocketDetector : MonoBehaviour
    {
        public int id;
        [SerializeField] private Transform pocketTarget;
        public Vector3 target{ get{ return pocketTarget.position; }}
    }
}
