using System.Collections.Generic;
using UnityEngine;

namespace WalldoffStudios
{
    public static class MeshGenerator
    {
        //Logic for generating a cone mesh

        #region Cone Mesh

        public static void CreateConeMesh(int points, float fov, Transform player, bool is2D, MeshFilter meshFilter)
        {
            float aimAngleIncrements = fov / points;
            List<Vector3> hitPoints = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>() { Vector3.zero };
            List<Vector4> settings = new List<Vector4>() { Vector4.zero };

            Vector3 playerPos = player.position;
            for (int i = 0; i <= points; i++)
            {
                float angle = -fov * 0.5f + aimAngleIncrements * i;
                Vector3 normalizedDirection;
                if (is2D == true)
                {
                    normalizedDirection = DirectionFromAngle2D(angle).normalized;
                    normalizedDirection.z = 0;
                }
                else
                {
                    normalizedDirection = DirectionFromAngle3D(angle).normalized;
                    normalizedDirection.y = 0;
                }

                hitPoints.Add(playerPos + normalizedDirection);
                normals.Add(normalizedDirection);
                settings.Add(new Vector4(0, 0, 0, i));
            }

            int pointCount = hitPoints.Count + 1;
            List<Vector3> vertices = new List<Vector3>() { Vector3.zero };
            List<Vector2> uvs = new List<Vector2>() { new Vector2(0.5f, 0.0f) };
            int[] triangles = new int[(pointCount - 2) * 3];

            Vector2 uv;
            for (int i = 0; i < pointCount - 1; i++)
            {
                uv.x = GetUVOffsetX(i + 1, pointCount);
                uv.y = 1;
                uvs.Add(uv);

                Vector3 pos = playerPos - hitPoints[i];
                vertices.Add(pos);

                if (i < pointCount - 2)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            Mesh mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                uv = uvs.ToArray(),
                triangles = triangles,
                normals = normals.ToArray(),
                name = "Cone Indicator"
            };
            mesh.SetUVs(1, settings);

            meshFilter.mesh = mesh;
        }

        public static Vector3 DirectionFromAngle3D(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;

            Vector3 direction;
            direction.x = Mathf.Sin(radians);
            direction.y = 0.0f;
            direction.z = Mathf.Cos(radians);

            return direction;
        }

        public static Vector3 DirectionFromAngle2D(float angleDegrees)
        {
            angleDegrees = 90.0f - angleDegrees;
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0);
        }

        private static float GetUVOffsetX(int index, int points)
        {
            return ((float)index - 1) / (points - 2);
        }

        #endregion Cone Mesh

        //Logic for generating a line mesh

        #region Line Mesh

        public static void CreateLineMesh(float lineWidth, float range, bool is2D, MeshFilter meshFilter)
        {
            Mesh mesh = new Mesh
            {
                vertices = GetLineVertices(lineWidth, range, is2D),
                uv = GetLineUVs(),
                triangles = GetLineTriangles(),
                name = "Line Indicator"
            };
            mesh.SetUVs(1, GetMeshSettings(6));
            meshFilter.mesh = mesh;
        }

        private static Vector3[] GetLineVertices(float lineWidth, float range, bool is2D)
        {
            Vector3[] vertices;
            if (is2D == true)
            {
                vertices = new[]
                {
                    new Vector3(lineWidth * -0.5f, 0.0f, 0.0f),
                    new Vector3(lineWidth * 0.5f, 0.0f, 0.0f),
                    new Vector3(lineWidth * -0.5f, range - lineWidth, 0.0f),
                    new Vector3(lineWidth * 0.5f, range - lineWidth, 0.0f),
                    new Vector3(lineWidth * -0.5f, range, 0.0f),
                    new Vector3(lineWidth * 0.5f, range, 0.0f),
                };
            }
            else
            {
                vertices = new[]
                {
                    new Vector3(lineWidth * -0.5f, 0.0f, 0.0f),
                    new Vector3(lineWidth * 0.5f, 0.0f, 0.0f),
                    new Vector3(lineWidth * -0.5f, 0.0f, range - lineWidth),
                    new Vector3(lineWidth * 0.5f, 0.0f, range - lineWidth),
                    new Vector3(lineWidth * -0.5f, 0.0f, range),
                    new Vector3(lineWidth * 0.5f, 0.0f, range),
                };
            }


            return vertices;
        }

        private static Vector2[] GetLineUVs()
        {
            Vector2[] uvs =
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(0.0f, 0.75f),
                new Vector2(1.0f, 0.75f),
                new Vector2(0.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
            };

            return uvs;
        }

        private static int[] GetLineTriangles()
        {
            int[] triangles = { 0, 2, 1, 1, 2, 3, 4, 3, 2, 3, 4, 5 };
            return triangles;
        }

        #endregion Line Mesh

        //Logic for generating a parabolic mesh

        #region Parabolic Mesh

        public static void CreateParabolicMesh(float resolution, float meshWidth, float height, Transform player,
            Transform target, MeshFilter meshFilter)
        {
            Vector3 targetPos = CalculateTargetPos(player, target);

            CalculatePathWithHeight(targetPos, height, out var velocity, out var angle, out var time);
            int vertexCount = (int)(time / resolution);
            if (vertexCount % 2 != 0) vertexCount++; 

            List<Vector3> vertices = new List<Vector3>
                { new Vector3(meshWidth * -0.5f, 0, 0), new Vector3(meshWidth * 0.5f, 0, 0) };
            List<Vector2> uvs = new List<Vector2> { Vector2.zero, Vector2.right };

            int vertexIndex = 0;
            Vector2 pos;
            for (float i = 0; i < time; i += resolution)
            {
                pos = GetIndicatorPos(velocity, i, angle);

                if (vertexIndex > 1)
                {
                    vertices.Add(new Vector3(GetXPos(vertexIndex, meshWidth), pos.y, pos.x));
                    uvs.Add(GetUVs(vertexIndex));
                }

                vertexIndex++;
            }

            pos = GetIndicatorPos(velocity, time, angle);
            vertexIndex++;
            vertices.Add(new Vector3(GetXPos(vertexIndex - 1, meshWidth), pos.y, pos.x));
            vertices.Add(new Vector3(GetXPos(vertexIndex, meshWidth), pos.y, pos.x));

            bool even = (vertexIndex) % 2 == 0;
            uvs.Add(new Vector2(even ? 1.0f : 0.0f, vertexIndex * 0.05f));
            uvs.Add(new Vector2(even ? 0.0f : 1.0f, vertexIndex * 0.05f));

            Mesh mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = GetTriangles(vertexCount),
                uv = uvs.ToArray(),
                name = "Parabolic Indicator"
            };
            mesh.SetUVs(1, GetMeshSettings(vertexCount + 2, vertexIndex * 0.05f));
            meshFilter.mesh = mesh;
        }

        private static float GetXPos(int index, float width)
        {
            bool even = index % 2 == 0;
            return even ? width * -0.5f : width * 0.5f;
        }

        private static Vector2 GetUVs(int index)
        {
            Vector2 uv;
            bool even = index % 2 == 0;
            uv.x = even ? 0.0f : 1.0f;
            uv.y = index * 0.05f;

            return uv;
        }

        private static int[] GetTriangles(int vertexAmount)
        {
            int triangleIndex = 0;
            int[] triangles = new int[vertexAmount * 3];
            for (int i = 0; i < vertexAmount; i++)
            {
                bool even = i % 2 == 0;

                triangles[triangleIndex + 0] = i;
                triangles[triangleIndex + 1] = i + (even ? 2 : 1);
                triangles[triangleIndex + 2] = i + (even ? 1 : 2);
                triangleIndex += 3;
            }

            return triangles;
        }

        private static Vector2 GetIndicatorPos(float velocity, float time, float angle)
        {
            float gravity = Physics.gravity.y;

            Vector2 pos;
            pos.x = velocity * time * Mathf.Cos(angle);
            pos.y = velocity * time * Mathf.Sin(angle) - 0.5f * -gravity * Mathf.Pow(time, 2.0f);
            return pos;
        }

        private static Vector3 CalculateTargetPos(Transform player, Transform target)
        {
            Vector3 shootDir = target.position - player.position;

            Vector3 groundDir;
            groundDir.x = shootDir.x;
            groundDir.y = 0;
            groundDir.z = shootDir.z;

            Vector3 targetPos;
            targetPos.x = groundDir.magnitude;
            targetPos.y = shootDir.y;
            targetPos.z = 0;

            return targetPos;
        }

        public static void CalculatePathWithHeight(Vector3 targetPos, float height, out float velocity, out float angle,
            out float time)
        {
            float xTarget = targetPos.x;
            float yTarget = targetPos.y;
            float gravity = -Physics.gravity.y;

            float b = Mathf.Sqrt(2 * gravity * height);
            float a = (-0.5f * gravity);
            float c = -yTarget;

            float timePositive = QuadraticEquation(a, b, c, 1);
            float timeNegative = QuadraticEquation(a, b, c, -1);

            time = timePositive > timeNegative ? timePositive : timeNegative;
            angle = Mathf.Atan(b * time / xTarget);
            velocity = b / Mathf.Sin(angle);
        }

        private static float QuadraticEquation(float a, float b, float c, float sign)
        {
            return (-b + sign * Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
        }

        #endregion Parabolic Mesh

        private static Vector4[] GetMeshSettings(int amount = 0, float uvYSize = 0.0f)
        {
            List<Vector4> settings = new List<Vector4>();
            for (int i = 0; i < amount; i++)
            {
                settings.Add(new Vector4(0, 0, uvYSize, i));
            }

            return settings.ToArray();
        }

        //Logic for generating a target mesh

        #region Target Mesh

        public static Mesh CreateTargetMesh(float radialSize, bool is2D)
        {
            Vector3[] vertices;
            if (is2D == true)
            {
                vertices = new Vector3[4]
                {
                    new Vector3(-0.5f, 0.0f, -0.5f) * radialSize,
                    new Vector3(0.5f, 0.0f, -0.5f) * radialSize,
                    new Vector3(-0.5f, 0.0f, 0.5f) * radialSize,
                    new Vector3(0.5f, 0.0f, 0.5f) * radialSize
                };
            }
            else
            {
                vertices = new Vector3[4]
                {
                    new Vector3(-0.5f, -0.5f, 0.0f) * radialSize,
                    new Vector3(0.5f, -0.5f, 0.0f) * radialSize,
                    new Vector3(-0.5f, 0.5f, 0.0f) * radialSize,
                    new Vector3(0.5f, 0.5f, 0.0f) * radialSize
                };
            }


            int[] tris = new int[6] { 0, 2, 1, 1, 2, 3 };

            Vector2[] uvs = new Vector2[4] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            Vector4[] settings = new Vector4[4]
            {
                new Vector4(0, 0, 0, 0),
                new Vector4(0, 0, 0, 1),
                new Vector4(0, 0, 0, 2),
                new Vector4(0, 0, 0, 3)
            };

            Mesh targetMesh = new Mesh
            {
                name = "Target Indicator",
                vertices = vertices,
                triangles = tris,
                uv = uvs
            };
            targetMesh.SetUVs(1, settings);

            return targetMesh;
        }

        #endregion Target Mesh
    }
}