using System;

namespace ProbablyStolenTodo
{
    /// <summary>
    /// 小數學工具：mod 是 net48（遊戲的 Mono 是 .NET 4.x，沒有 Math.Clamp、MathF），離線測試 mod/verify 是 net8.0，
    /// 兩邊都會編譯的檔（TodoStore、IconShapes）用這裡的，不碰 Unity 的 Mathf（離線測試沒有 Unity）。
    /// </summary>
    static class MathUtil
    {
        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;

        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;

        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Cos(float v) => (float)Math.Cos(v);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public const float PI = (float)Math.PI;
    }
}
