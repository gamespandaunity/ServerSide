using UnityEngine;

namespace Mechanics
{
    /// <summary>
    /// The curve on bases in the Bezier curve.
    /// </summary>
    public class BezierCurve
    {
        public static float CalculateLength(int smoothDivisions, Vector3[] nodes)
        {
            if (nodes == null || nodes.Length < 2)
            {
                return 0.0f;
            }
            float _smoothDivisions = 0.5f / ((float)(nodes.Length - 1) * (float)Mathf.Clamp(smoothDivisions, 1, 100));

            float length = 0.0f;
            for (float f = 0.0f; f < 1.0f; f += _smoothDivisions)
            {
                Vector3 position1 = CalculateValue(f, nodes);
                Vector3 position2 = CalculateValue(f + _smoothDivisions, nodes);
                #if UNITY_EDITOR
                Debug.DrawLine(position1, position2);
                #endif
                length += Vector3.Distance(position1, position2);
            }

            return length;
        }

        public static Vector3 CalculateValue(float time01, Vector3[] nodes)
        {
            if (nodes == null || nodes.Length == 0)
            {
                return Vector3.zero;
            }
            else if (nodes.Length == 1)
            {
                return nodes[0];
            }
            Vector3[] newNodes = new Vector3[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                newNodes[i] = nodes[i];
            }
            int j = nodes.Length - 1;
            while (j > 0)
            {
                for (int i = 0; i < j; i++)
                {
                    newNodes[i] = Vector3.Lerp(newNodes[i], newNodes[i + 1], time01);
                }
                j--;
            }
            return newNodes[0];
        }

        public static Vector3 CalculateTangent(float time01, Vector3[] nodes)
        { 
            return ((1.0f / 0.0001f) * (CalculateValue(time01 + 0.0001f, nodes) - CalculateValue(time01, nodes))).normalized;
        }

        public static Vector3 CalculateNormal(float time01, Vector3[] nodes)
        { 
            Vector3 tangent0 = CalculateTangent(time01, nodes);
            Vector3 tangent = CalculateTangent(time01 + 0.0001f, nodes);
            return ((1.0f / 0.0001f) * (tangent - tangent0)).normalized;
        }

        public static Vector3 CalculateBinormal(float time01, Vector3[] nodes)
        {
            return (-Vector3.Cross(CalculateTangent(time01, nodes), CalculateNormal(time01, nodes))).normalized;
        }
    }
    // https://ru.wikipedia.org/wiki/%D0%9F%D0%B0%D1%80%D0%B0%D0%B1%D0%BE%D0%BB%D0%B0
    public class BezierCurve2
    {
        private static Vector3[] GetNewNodes(float magnit, Vector3[] nodes)
        {
            magnit = Mathf.Lerp(-3, 1, magnit);
            if (nodes == null || nodes.Length < 2)
            {
                return nodes;
            }
            else if (nodes.Length == 2)
            {
                return new Vector3[2]{ nodes[0], nodes[1] };
            }
            Vector3[] newNodes = new Vector3[nodes.Length];

            for (int i = 0; i < nodes.Length; i++)
            {
                if (i == 0 || i == nodes.Length - 1)
                {
                    newNodes[i] = nodes[i];
                }
                else
                {
                    Vector3 midlePoint = 0.5f * (nodes[i] + 0.5f * Vector3.Distance(nodes[i - 1], nodes[i]) * (nodes[i] - nodes[i + 1]).normalized +
                                         nodes[i] + 0.5f * Vector3.Distance(nodes[i], nodes[i + 1]) * (nodes[i] - nodes[i - 1]).normalized);
                    newNodes[i] = nodes[i] + (1.0f + magnit) * (midlePoint - nodes[i]);
                }
            }
            return newNodes;
        }

        public static Vector3 CalculateValue(float magnit, float time01, Vector3[] nodes)
        {
            return BezierCurve.CalculateValue(time01, GetNewNodes(magnit, nodes));
        }

        public static float CalculateLength(float magnit, int smoothDivisions, Vector3[] nodes)
        {
            return BezierCurve.CalculateLength(smoothDivisions, GetNewNodes(magnit, nodes));
        }
    }

    public class QuadraticCurve
    {
        public static float CalculateLength(int smoothDivisions, Vector3[] nodes)
        {
            if (nodes == null || nodes.Length < 2)
            {
                return 0.0f;
            }
            float _smoothDivisions = 0.5f / ((float)(nodes.Length - 1) * (float)Mathf.Clamp(smoothDivisions, 1, 100));

            float length = 0.0f;
            for (float f = 0.0f; f < 1.0f - _smoothDivisions; f += _smoothDivisions)
            {
                Vector3 position1 = CalculateValue(f, nodes);
                Vector3 position2 = CalculateValue(f + _smoothDivisions, nodes);
                #if UNITY_EDITOR
                Debug.DrawLine(position1, position2);
                #endif
                length += Vector3.Distance(position1, position2);
            }

            return length;
        }
        public static float CalculateLength(Vector3[] nodes)
        {
            float length = 0.0f;
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                length += Vector3.Distance(nodes[i], nodes[i + 1]);
            }
            return length;
        }

        public static Vector3 CalculateValue(float time01, Vector3[] nodes)
        {
            if (nodes == null || nodes.Length == 0)
            {
                return Vector3.zero;
            }
            else if (nodes.Length == 1)
            {
                return nodes[0];
            }
            else if (nodes.Length == 2)
            {
                return Vector3.Lerp(nodes[0], nodes[1], time01);
            }
            float length = 0.0f;
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                length += Vector3.Distance(nodes[i], nodes[i + 1]);
            }
            float time = time01 * length;
            if (nodes.Length == 3)
            {
                return CalculateValue(0.0f, time, nodes[0], nodes[1], nodes[2]);
            }
            else
            {
                if (time <= Vector3.Distance(nodes[0], nodes[1]))
                {
                    return CalculateValue(0.0f, time, nodes[0], nodes[1], nodes[2]);
                }
                else if (time >= length - Vector3.Distance(nodes[nodes.Length - 2], nodes[nodes.Length - 1]))
                {
                    return CalculateValue(length - Vector3.Distance(nodes[nodes.Length - 2], nodes[nodes.Length - 1]) - Vector3.Distance(nodes[nodes.Length - 3], nodes[nodes.Length - 2]), time, nodes[nodes.Length - 3], nodes[nodes.Length - 2], nodes[nodes.Length - 1]);
                }
                else
                {
                    float currentTime = 0.0f;
                    Vector3 sum = Vector3.zero;
                    for (int i = 0; i < nodes.Length - 2; i++)
                    {
                        sum += CalculateValue(currentTime, time, nodes[i], nodes[i + 1], nodes[i + 2]);
                        currentTime += Vector3.Distance(nodes[i], nodes[i + 1]);
                    }
                    return 0.5f * sum;
                }
            }
        }

        private static Vector3 CalculateValue(float time0, float time, Vector3 node1, Vector3 node2, Vector3 node3)
        {
            float maxTime = time0 + Vector3.Distance(node1, node2) + Vector3.Distance(node2, node3);
            if (time < time0 ||  time > maxTime)
            {
                return Vector3.zero;
            }
            Vector3 a, b, c;
            GetABC(time0, node1, node2, node3, out a, out b, out c);
            return a * time * time + b * time + c;
        }

        private static void GetABC(float time0, Vector3 node1, Vector3 node2, Vector3 node3, out Vector3 a, out Vector3 b, out Vector3 c)
        {
            float time1 = time0;
            float time2 = time1 + Vector3.Distance(node1, node2);
            float time3 = time2 + Vector3.Distance(node2, node3);

            a = (node3 - ((time3 * (node2 - node1) + time2 * node1 - time1 * node2) / (time2 - time1))) / (time3 * (time3 - time1 - time2) + time1 * time2);
            b = (node2 - node1) / (time2 - time1) - a * (time1 + time2);
            c = ((time2 * node1 - time1 * node2) / (time2 - time1)) + a * time1 * time2;
        }
    }
}
