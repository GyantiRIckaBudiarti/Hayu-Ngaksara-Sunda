using System.Collections.Generic;
using UnityEngine;

namespace HayuNgaksara
{
    public static class TraceEvaluator
    {
        private const float PassThreshold = 0.5f;

        public struct EvaluationResult
        {
            public float score;
            public bool  passed;
        }

        public static EvaluationResult Evaluate(List<Vector3> userPoints, List<Vector3> guidePoints)
        {
            if (userPoints == null || userPoints.Count == 0 || guidePoints == null || guidePoints.Count == 0)
                return new EvaluationResult { score = 0f, passed = false };

            float totalDist = 0f;
            foreach (var pt in userPoints)
                totalDist += DistanceToPath(pt, guidePoints);

            float avgDist = totalDist / userPoints.Count;
            float score   = Mathf.Clamp01(1f - avgDist / PassThreshold);
            bool  passed  = avgDist < PassThreshold;

            return new EvaluationResult { score = score, passed = passed };
        }

        private static float DistanceToPath(Vector3 point, List<Vector3> path)
        {
            float minDist = float.MaxValue;
            for (int i = 0; i < path.Count - 1; i++)
            {
                float d = DistanceToSegment(point, path[i], path[i + 1]);
                if (d < minDist) minDist = d;
            }
            return minDist;
        }

        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float   t  = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector3.Distance(p, a + t * ab);
        }
    }
}
