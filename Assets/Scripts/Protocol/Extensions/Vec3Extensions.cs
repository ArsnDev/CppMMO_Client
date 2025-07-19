using Google.FlatBuffers;
using CppMMO.Protocol;
using UnityEngine;

namespace SimpleMMO.Protocol.Extensions
{
    public static class Vec3Extensions
    {
        public static Vector3 ToUnityVector3(this Vec3 vec3)
        {
            return new Vector3(vec3.X, vec3.Y, vec3.Z);
        }

        public static Offset<Vec3> ToFlatBufferVec3(this Vector3 vector, FlatBufferBuilder builder)
        {
            return Vec3.CreateVec3(builder, vector.x, vector.y, vector.z);
        }

        public static Offset<Vec3> ToFlatBufferVec3(this Vector2 vector2, FlatBufferBuilder builder)
        {
            return Vec3.CreateVec3(builder, vector2.x, vector2.y, 0f);
        }

        public static Vector2 ToUnityVector2(this Vec3 vec3)
        {
            return new Vector2(vec3.X, vec3.Y);
        }
    }
}