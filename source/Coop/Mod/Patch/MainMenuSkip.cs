using HarmonyLib;
using SandBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;

namespace Coop.Mod.Patch
{
    class MainMenuSkip
    {
        //[HarmonyPatch(typeof(GameStateManager))]
        //[HarmonyPatch(nameof(GameStateManager.CleanAndPushState))]
        //class PatchCleanAndPushScreen
        //{
        //    static void Postfix(GameState gameState)
        //    {
        //        if(gameState is InitialState)
        //        {
        //            Module.CurrentModule.ExecuteInitialStateOptionWithId("CoOp Campaign");
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(CampaignGameManager))]
        //[HarmonyPatch(nameof(CampaignGameManager.OnLoadFinished))]
        //class PatchOnLoadFinished
        //{
        //    static void Postfix()
        //    {
        //        Module.CurrentModule.ExecuteInitialStateOptionWithId("CoOp Campaign");
        //    }
        //}

        //[HarmonyPatch(typeof(Module))]
        //[HarmonyPatch(nameof(Module.SetInitialModuleScreenAsRootScreen))]
        //class PatchSetInitialModuleScreenAsRootScreen
        //{
        //    static void Postfix()
        //    {
        //        string[] array = Utilities.GetFullCommandLineString().Split(' ');

        //        foreach (string argument in array)
        //        {
        //            if (argument.ToLower() == "/server")
        //            {
        //                Module.CurrentModule.ExecuteInitialStateOptionWithId("CoOp Campaign");
        //            }
        //            else if (argument.ToLower() == "/client")
        //            {
        //                Module.CurrentModule.ExecuteInitialStateOptionWithId("Join Coop Game");
        //            }
        //        }

        //    }
        //}

        //[HarmonyPatch(typeof(Module), "OnSplashScreenFinished")]
        //class PatchOnSplashScreenFinished
        //{
        //    static void Postfix()
        //    {
        //        string[] array = Utilities.GetFullCommandLineString().Split(' ');

        //        foreach (string argument in array)
        //        {
        //            if (argument.ToLower() == "/server")
        //            {
        //                Thread.Sleep(500);
        //                Module.CurrentModule.ExecuteInitialStateOptionWithId("CoOp Campaign");
        //            }
        //            else if (argument.ToLower() == "/client")
        //            {
        //                Module.CurrentModule.ExecuteInitialStateOptionWithId("Join Coop Game");
        //            }
        //        }

        //    }
        //}

        [HarmonyPatch(typeof(InitialGauntletScreen), "OnInitialize")]
        class PatchOnInitialize
        {
            static void Postfix()
            {
                string[] array = Utilities.GetFullCommandLineString().Split(' ');

                foreach (string argument in array)
                {
                    if (argument.ToLower() == "/server")
                    {
                        Thread.Sleep(500);
                        Module.CurrentModule.ExecuteInitialStateOptionWithId("CoOp Campaign");
                    }
                    else if (argument.ToLower() == "/client")
                    {
                        Module.CurrentModule.ExecuteInitialStateOptionWithId("Join Coop Game");
                    }
                }

            }
        }
    }
}
