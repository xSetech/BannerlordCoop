using System;
using Autofac;
using Common;
using Coop.Mod.Binding;
using Coop.Mod.Patch;
using NLog;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Missions;
using Logger = NLog.Logger;

namespace Coop.Mod
{
    internal class Main
    {
        // Debug symbols
        public static readonly bool DEBUG = true;

        public static readonly string LOAD_GAME = "MP";

        // -------------
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Main()
        {
            Debug.DebugManager = Debugging.DebugManager;
            MBDebug.DisableLogging = false;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception) e.ExceptionObject;
            Logger.Fatal(ex, "Unhandled exception");
        }


        internal static bool DisableIntroVideo = true;

        internal static bool EnableTalkToOtherLordsInAnArmy = true;

        internal static bool RecordFirstChanceExceptions = true;

        internal static bool DontGroupThirdPartyMenuOptions = true;

        internal static bool QuartermasterIsClanWide = true;

    }
}
