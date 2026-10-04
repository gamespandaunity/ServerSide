using UnityEngine;
using BallPool.Mechanics;

namespace BallPool.AI
{
    /// <summary>
    /// The ball AI calculator, this is a base class for calculating AI, for shot into the pockets.
    /// </summary>
    public struct BallAICalculator
    {
        /// <summary>
        /// Finds the best target ball, for shot into the pockets.
        /// </summary>
        public static Ball FindBestTargetBall(Ball cueBall, PocketDetector[] targets, Ball[] balls, out int pocketId, out Vector3 cueBallPosition, out Vector3 shotPoint, out Vector3 aimpoint, ref float impulse, FindBestTargetBallException<int> FindException, bool changePosition, int ballsLayer, int boardLayer)
        {
            Ball targetBall = null;
            bool haveHitBall = false;
            pocketId = 0;
            float hitAngleCosine = 0.0f;
            float targetBallToTargetDistance = 100000.0f;
            cueBallPosition = cueBall.position;
            shotPoint = Vector3.zero;
            Vector3 targetPoint = Vector3.zero;
            aimpoint = Vector3.zero;

            foreach (PocketDetector pocket in targets)
            {
                foreach (Ball ball in balls)
                {
                    if (ball == cueBall || ball.inPocket || FindException(ball.id))
                    {
                        continue;
                    }
                    Vector3 targetBallShotDirection = (pocket.target - ball.position).normalized;
                    targetPoint = ball.position - (cueBall.radius + ball.radius) * targetBallShotDirection;
                    Vector3 shotDirection = Vector3.ProjectOnPlane(targetPoint - cueBallPosition, Vector3.up).normalized;
                    RaycastHit targetHit;
                    if (Vector3.Dot(targetBallShotDirection, shotDirection) <= 0.0f)
                    {
                        continue;
                    }
                    bool isCast = Physics.SphereCast(ball.position, ball.radius, targetBallShotDirection, out targetHit, Vector3.Distance(ball.position, pocket.target), ballsLayer);
                    if (isCast)
                    {
                        continue;
                    }
                    else
                    {
                        if (!targetBall)
                        {
                            aimpoint = targetPoint;
                            shotPoint = cueBallPosition - cueBall.radius * shotDirection;
                        }

                        float currentHitAngleCosine = Vector3.Dot(shotDirection, (ball.position - targetPoint).normalized);
                        float currentTargetBallToTargetDistance= Vector3.Distance(pocket.target, ball.position);
                        if (hitAngleCosine < currentHitAngleCosine || (currentHitAngleCosine > 0.5f && targetBallToTargetDistance > currentTargetBallToTargetDistance))
                        {
                            ball.isCast = false;
                            isCast = Physics.SphereCast(cueBallPosition, cueBall.radius, shotDirection, out targetHit, Vector3.Distance(cueBallPosition, targetPoint), ballsLayer);
                            ball.isCast = true;

                            if (isCast)
                            {
                                if (!changePosition)
                                {
                                    if (!haveHitBall || hitAngleCosine < 0.1f)
                                    {
                                        if(CheckBoardls(cueBall.radius, cueBallPosition, targetPoint, ball.listener, ref aimpoint, ref hitAngleCosine, ref targetBallToTargetDistance, ballsLayer, boardLayer))
                                        {
                                            hitAngleCosine = currentHitAngleCosine;
                                            targetBallToTargetDistance = currentTargetBallToTargetDistance;
                                            shotDirection = Vector3.ProjectOnPlane(aimpoint - cueBallPosition, Vector3.up).normalized;
                                            shotPoint = cueBallPosition - cueBall.radius * shotDirection;
                                            haveHitBall = true;
                                            targetBall = ball;
                                            pocketId = pocket.id;
                                        }
                                    }
                                }
                                else
                                {
                                    float displacement = 20.0f;
                                    Vector3 newPosition = targetPoint - displacement * cueBall.radius * targetBallShotDirection;
                                    isCast = true;

                                    while (isCast && displacement >= 4.0f)
                                    {
                                        displacement -= 2.0f;
                                        newPosition = targetPoint - displacement * cueBall.radius * targetBallShotDirection;
                                        //Check the presence of any shape from the 'new position' to the targetPoint
                                        ball.isCast = false;
                                        isCast = Physics.SphereCast(newPosition, cueBall.radius, targetBallShotDirection, out targetHit, Vector3.Distance(newPosition, targetPoint), ballsLayer) ||
                                            Physics.SphereCast(newPosition + 4.0f * cueBall.radius * Vector3.up, 1.5f * cueBall.radius, -Vector3.up, out targetHit, Vector3.Distance(newPosition, targetPoint), ballsLayer);
                                        ball.isCast = true;
                                        if (!isCast)
                                        {
                                            hitAngleCosine = currentHitAngleCosine;
                                            cueBallPosition = newPosition;
                                            targetPoint = ball.position - (cueBall.radius + ball.radius) * targetBallShotDirection;
                                            shotDirection = Vector3.ProjectOnPlane(targetPoint - cueBallPosition, Vector3.up).normalized;
                                            shotPoint = cueBallPosition - cueBall.radius * shotDirection;
                                            aimpoint = targetPoint;
                                            haveHitBall = true;
                                            targetBall = ball;
                                            cueBall.position = cueBallPosition;
                                            cueBall.OnState(BallState.SetState);
                                            pocketId = pocket.id;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                hitAngleCosine = currentHitAngleCosine;
                                targetBallToTargetDistance = currentTargetBallToTargetDistance;
                                shotDirection = Vector3.ProjectOnPlane(targetPoint - cueBallPosition, Vector3.up).normalized;
                                shotPoint = cueBallPosition - cueBall.radius * shotDirection;
                                aimpoint = targetPoint;
                                haveHitBall = true;
                                targetBall = ball;
                                pocketId = pocket.id;
                            }
                        }
                    }
                }
            }
            if (targetBall)
            {
                float displacement = 0.5f * Random.Range(-cueBall.radius, 0.0f);
                shotPoint = shotPoint + displacement * Vector3.up;
            }
            return targetBall;
        }
        private static bool CheckBoardls(float cueBallRadius, Vector3 cueBallPosition, Vector3 targetPoint, BallDetector targetBall, ref Vector3 aimpoint, ref float hitAngleCosine, ref float targetBallToTargetDistance, int ballsLayer, int boardLayer)
        {
            return
                CheckBoardl(Vector3.right, cueBallRadius, cueBallPosition, targetPoint, targetBall, ref aimpoint, ref hitAngleCosine, ref targetBallToTargetDistance, ballsLayer, boardLayer) |
                CheckBoardl(Vector3.left, cueBallRadius, cueBallPosition, targetPoint, targetBall, ref aimpoint, ref hitAngleCosine, ref targetBallToTargetDistance, ballsLayer, boardLayer) |
                CheckBoardl(Vector3.forward, cueBallRadius, cueBallPosition, targetPoint, targetBall, ref aimpoint, ref hitAngleCosine, ref targetBallToTargetDistance, ballsLayer, boardLayer) |
                CheckBoardl(Vector3.back, cueBallRadius, cueBallPosition, targetPoint, targetBall, ref aimpoint, ref hitAngleCosine, ref targetBallToTargetDistance, ballsLayer, boardLayer);
        }
        private static bool CheckBoardl(Vector3 direction, float cueBallRadius, Vector3 cueBallPosition, Vector3 targetPoint, BallDetector targetBall, ref Vector3 aimpoint, ref float hitAngleCosine, ref float targetBallToTargetDistance, int ballsLayer, int boardLayer)
        {
            Ray ray = new Ray(cueBallPosition, direction);
            RaycastHit hitBoard;
            if(Physics.SphereCast(ray, cueBallRadius, out hitBoard, 5.0f, boardLayer))
            {
                float height = Vector3.Distance(hitBoard.point + hitBoard.normal * cueBallRadius, cueBallPosition) - cueBallRadius;
                float deltaHeight = Vector3.Dot(targetPoint - cueBallPosition, direction);
                Vector3 orient = BallGeometry.getPerpendicularToVector(direction, targetPoint - cueBallPosition).normalized;
                float distance = Vector3.Project(targetPoint - cueBallPosition, orient).magnitude;

                Vector3 needPoint = cueBallPosition + height * direction + (distance * height / (2.0f * height - deltaHeight)) * orient;

                Vector3 checkDirection1 = (needPoint - cueBallPosition).normalized;
                Vector3 checkDirection2 = (targetPoint - needPoint).normalized;

                Ray checkPointRay = new Ray(cueBallPosition, checkDirection1);
                RaycastHit checkPointHit;
                if(Physics.SphereCast(checkPointRay, cueBallRadius, out checkPointHit, 5.0f, ballsLayer | boardLayer))
                {
                    if(checkPointHit.collider.gameObject.layer == LayerMask.NameToLayer("Board"))
                    {
                        if(Vector3.Dot(checkPointHit.normal, -direction) > 0.9f)
                        {
                            Ray targetRay = new Ray(needPoint, checkDirection2);
                            RaycastHit targetHit;
                            if(Physics.SphereCast(targetRay, cueBallRadius, out targetHit, 5.0f, ballsLayer))
                            {
                                BallDetector currentTargetBall = targetHit.collider.GetComponent<BallDetector>();
                                if(currentTargetBall && currentTargetBall == targetBall)
                                {
                                    float currentHitAngleCosine = Vector3.Dot(checkDirection2, (targetBall.transform.position - targetPoint).normalized);
                                    float currentTargetBallToTargetDistance = Vector3.Distance(cueBallPosition, needPoint) + Vector3.Distance(needPoint, targetPoint); 
                                    if(hitAngleCosine < currentHitAngleCosine || (currentHitAngleCosine > 0.5f && targetBallToTargetDistance > currentTargetBallToTargetDistance))
                                    {
                                        aimpoint = needPoint;
                                        hitAngleCosine = currentHitAngleCosine;
                                        targetBallToTargetDistance = currentTargetBallToTargetDistance;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}