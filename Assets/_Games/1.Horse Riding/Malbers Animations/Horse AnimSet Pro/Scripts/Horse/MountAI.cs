using UnityEngine;
using System.Collections;
using MalbersAnimations.HAP;
using UnityEngine.AI;

namespace MalbersAnimations
{
    public class MountAI : AnimalAIControl, IMountAI
    {
        public bool canBeCalled;
        protected Mountable animalMount;               //The Animal Mount Script
        protected bool isBeingCalled;

        public bool CanBeCalled
        {
            get { return canBeCalled; }
            set { canBeCalled = value; }
        }

        void Start()
        {
            animalMount = GetComponent<Mountable>();
            StartAgent();
        }

        void Update()
        {
            if (animalMount.Mounted)            //If the Animal is mounted
            {
                Agent.enabled = false;          //Disable the navmesh agent
                Stopped = true;
                return;                     
            }
            Agent.nextPosition = Agent.transform.position;                      //Update the Agent Position to the Transform position
            if (!Agent.isOnNavMesh || !Agent.enabled) return;


           if (isBeingCalled == true)
                Agent.SetDestination(target.position);                       //If there's a position to go to set it as destination

            UpdateAgent();
        }

        protected override void OnAnimationChanged(int animTag)
        {
            if (animalMount.Mounted) return;            //If the Animal is mounted
            base.OnAnimationChanged(animTag);
        }

        public virtual void CallAnimal(Transform target, bool call)
        {
            if (!CanBeCalled) return;           //If the animal cannot be called ignore this
            isBeingCalled = call;

            if (Agent)
            {
                Agent.enabled = true;

                if (Agent.isOnNavMesh)
                {
                    if (isBeingCalled) SetTarget(target);
                    Agent.isStopped = !isBeingCalled; //If isBeingCalled == true then isStopped = false;
                }
            }
        }
    }
}
