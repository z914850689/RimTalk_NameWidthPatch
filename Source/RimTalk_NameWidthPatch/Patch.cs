using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimTalk_NameWidthPatch
{
    public enum PatchMode
    {
        WidthLimit = 0,   // 模式1: 限制名字列最大宽度
        NoWrapName = 1    // 模式2: 名字不换行+宽度自适应
    }

    public class NameWidthSettings : ModSettings
    {
        public PatchMode mode = PatchMode.WidthLimit;
        public float maxNameWidth = 120f;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref mode, "mode", PatchMode.WidthLimit);
            Scribe_Values.Look(ref maxNameWidth, "maxNameWidth", 120f);
        }
    }

    public class NameWidthMod : Mod
    {
        public static NameWidthSettings settings;
        public static float runtimeMaxWidth = 120f;
        public static PatchMode runtimeMode = PatchMode.WidthLimit;
        [ThreadStatic] private static bool _isInDrawMessageLog = false;

        public NameWidthMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<NameWidthSettings>();
            runtimeMaxWidth = settings.maxNameWidth;
            runtimeMode = settings.mode;
            ApplyPatch();
        }

        public override string SettingsCategory() => "RimTalk 名字列设置";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            // 模式选择
            if (list.ButtonText(runtimeMode == PatchMode.WidthLimit ? "当前: 限制宽度模式" : "当前: 不限制宽度模式"))
            {
                var floatMenu = new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("模式1: 限制名字列最大宽度", () => {
                        settings.mode = PatchMode.WidthLimit;
                        runtimeMode = PatchMode.WidthLimit;
                    }),
                    new FloatMenuOption("模式2: 不限制宽度（RimTalk默认）", () => {
                        settings.mode = PatchMode.NoWrapName;
                        runtimeMode = PatchMode.NoWrapName;
                    })
                });
                Find.WindowStack.Add(floatMenu);
            }

            list.Gap(10f);

            // 模式1 的宽度滑块
            if (runtimeMode == PatchMode.WidthLimit)
            {
                settings.maxNameWidth = Mathf.RoundToInt(list.SliderLabeled(
                    "名字列最大宽度: " + Mathf.RoundToInt(settings.maxNameWidth) + "px",
                    settings.maxNameWidth, 40f, 800f));
                runtimeMaxWidth = settings.maxNameWidth;
            }

            list.Gap(10f);
            list.Label(runtimeMode == PatchMode.WidthLimit
                ? "超过设定宽度的名字会被截断，对话区获得更多空间"
                : "不限制名字宽度，保持 RimTalk 默认行为");

            list.End();
            settings.Write();
        }

        private void ApplyPatch()
        {
            Harmony harmony = new Harmony("Custom.RimTalkNameWidthPatch");

            Assembly rimTalkAsm = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "RimTalk")
                {
                    rimTalkAsm = asm;
                    Log.Message("[NameWidthPatch] 找到 RimTalk 程序集");
                    break;
                }
            }
            if (rimTalkAsm == null)
            {
                Log.Error("[NameWidthPatch] 找不到 RimTalk 程序集");
                return;
            }

            MethodInfo drawMsgLog = null;
            foreach (var type in rimTalkAsm.GetTypes())
            {
                drawMsgLog = AccessTools.Method(type, "DrawMessageLog");
                if (drawMsgLog != null)
                {
                    Log.Message("[NameWidthPatch] 找到 DrawMessageLog 在: " + type.FullName);
                    break;
                }
            }
            if (drawMsgLog == null)
            {
                Log.Error("[NameWidthPatch] 找不到 DrawMessageLog");
                return;
            }

            harmony.Patch(drawMsgLog,
                prefix: new HarmonyMethod(typeof(NameWidthMod), nameof(Prefix_DrawMessageLog)),
                postfix: new HarmonyMethod(typeof(NameWidthMod), nameof(Postfix_DrawMessageLog)));

            var calcSize = AccessTools.Method(typeof(Verse.Text), "CalcSize", new[] { typeof(string) });
            if (calcSize != null)
            {
                harmony.Patch(calcSize, postfix: new HarmonyMethod(typeof(NameWidthMod), nameof(Postfix_CalcSize)));
                Log.Message("[NameWidthPatch] Verse.Text.CalcSize 已打补丁");
            }

            Log.Message("[NameWidthPatch] 补丁完成! 当前模式: " + runtimeMode);
        }

        public static void Prefix_DrawMessageLog()
        {
            if (runtimeMode == PatchMode.WidthLimit)
            {
                _isInDrawMessageLog = true;
            }
            // 模式2: 不做任何干预，保持 RimTalk 默认行为
        }

        public static void Postfix_DrawMessageLog()
        {
            if (runtimeMode == PatchMode.WidthLimit)
            {
                _isInDrawMessageLog = false;
            }
        }

        public static void Postfix_CalcSize(ref Vector2 __result)
        {
            if (runtimeMode == PatchMode.WidthLimit && _isInDrawMessageLog && __result.x > runtimeMaxWidth)
            {
                __result.x = runtimeMaxWidth;
            }
            // 模式2: 宽度自适应，不做限制
        }
    }
}
