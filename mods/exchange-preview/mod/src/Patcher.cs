using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2CppInterop.Common;
using MelonLoader;

namespace ProbablyStolenExchangePreview
{
    /// <summary>
    /// 掛 Harmony 攔截的共用入口。**同一個原生位址只能掛一次**：這個遊戲有些方法被編譯器合併成同一段機器碼
    /// （例如公開的 Input.GetKey(KeyCode) 和內部的 GetKeyInt），兩個都掛的話，呼叫原本的方法會繞回攔截自己，
    /// 無限遞迴當掉（2026-10-04 實機 StackOverflowException）。所以所有攔截都經過這裡，用同一張「已掛的位址」表。
    /// </summary>
    static class Patcher
    {
        /// <summary>已經掛了攔截的原生位址 → 方法名稱。</summary>
        static readonly Dictionary<IntPtr, string> _patchedAt = new Dictionary<IntPtr, string>();

        /// <summary>實際掛上的攔截數（位址重複而跳過的不算）。</summary>
        internal static int Count;

        internal static string Name(MethodBase m) =>
            $"{m.DeclaringType?.Name}.{m.Name}({string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name))})";

        /// <summary>
        /// 方法的原生程式碼位址：Il2CppInterop 代理類別的 NativeMethodInfoPtr_… 欄位指向 IL2CPP 的 MethodInfo，
        /// 它的第一個欄位就是 methodPointer。
        /// </summary>
        static IntPtr NativeCode(MethodBase m)
        {
            var field = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(m);
            if (field == null) return IntPtr.Zero;
            var info = (IntPtr)field.GetValue(null);
            return info == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(info);
        }

        /// <summary>
        /// 掛上攔截；prefix／postfix 是 owner 類別裡的 static 方法名稱（可以是 private）。
        /// 位址和已掛的方法重複時不再掛、回傳 true（等於已經攔到了）。失敗寫進 log、回傳 false。
        /// </summary>
        internal static bool Patch(HarmonyLib.Harmony h, MelonLogger.Instance log, MethodBase target, Type owner, string prefix, string postfix = null)
        {
            if (target == null) { log.Warning($"找不到要攔截的方法（{owner.Name}.{prefix ?? postfix}）"); return false; }
            try
            {
                var code = NativeCode(target);
                if (code == IntPtr.Zero) { log.Warning($"讀不到 {Name(target)} 的原生程式碼位址（可能被裁掉了），不攔"); return false; }
                if (_patchedAt.TryGetValue(code, out var other))
                {
                    log.Msg($"{Name(target)} 和 {other} 是同一段原生程式碼，只攔一次");
                    return true;
                }
                h.Patch(target, prefix: Method(owner, prefix), postfix: Method(owner, postfix));
                _patchedAt[code] = Name(target);
                Count++;
                return true;
            }
            catch (Exception e)
            {
                log.Warning($"攔截 {Name(target)} 失敗：{e.Message}");
                return false;
            }
        }

        static HarmonyMethod Method(Type owner, string name) =>
            name == null ? null : new HarmonyMethod(owner.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
    }
}
