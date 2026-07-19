using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimTalk_NameWidthPatch
{
    public class NameWidthSettings : ModSettings
    {
        public float maxNameWidth = 120f;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxNameWidth, "maxNameWidth", 120f);
        }
    }

    public class NameWidthMod : Mod
    {
        public static NameWidthSettings settings;
        public static float runtimeMaxWidth = 120f;
        [ThreadStatic] private static bool _isInDrawMessageLog = false;

        public NameWidthMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<NameWidthSettings>();
            runtimeMaxWidth = settings.maxNameWidth;
            ApplyPatch();
        }

        public override string SettingsCategory() => "RimTalk 名字列设置";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            settings.maxNameWidth = Mathf.RoundToInt(list.SliderLabeled(
                "名字列最大宽度: " + Mathf.RoundToInt(settings.maxNameWidth) + "px",
                settings.maxNameWidth, 40f, 800f));
            runtimeMaxWidth = settings.maxNameWidth;

            list.Gap(10f);
            list.Label("超过设定宽度的名字会被截断，防止字体变化时换行显示不全");

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

            Log.Message("[NameWidthPatch] 补丁完成!");
        }

        public static void Prefix_DrawMessageLog()
        {
            _isInDrawMessageLog = true;
        }

        public static void Postfix_DrawMessageLog()
        {
            _isInDrawMessageLog = false;
        }

        public static void Postfix_CalcSize(ref Vector2 __result)
        {
            if (_isInDrawMessageLog && __result.x > runtimeMaxWidth)
            {
                __result.x = runtimeMaxWidth;
            }
        }
    }
}
