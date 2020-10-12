using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System;

namespace Synthesis
{
    /// <summary>
    /// Synthesis is a patching pipeline that strings small patchers together to make one final patch
    /// 
    /// The basic project setup can automatically be generated for you by Synthesis.
    /// 
    /// As such, this section showcases a few of the advanced features when hooking into the Synthesis system.
    /// </summary>
    class Program
    {
        public static int Main(string[] args)
        {
            // This call parses the arguments given to your program for you, and runs a patch when appropriate
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                // Pass in what function we want to run when we patch
                patcher: RunPatch,
                // Optional user parameters
                userPreferences: new UserPreferences()
                {
                    // Here we can specify default behavior for when no arguments are passed.
                    // Starting normally with no arguments will then create a standalone patch within the 
                    // default Data folder for the target game.  This is helpful during development when
                    // you want to run directly from your IDE, without starting it through Synthesis
                    ActionsForEmptyArgs = new RunDefaultPatcher()
                    {
                        IdentifyingModKey = ModKey.FromNameAndExtension("YourModName.esp"),
                        TargetRelease = GameRelease.SkyrimSE,
                    }
                });
        }

        /// <summary>
        /// This is our actual function to do our patch
        /// </summary>
        /// <param name="state">An object containing all the parameters for our patch</param>
        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // Add 2 magicka offset to every NPC. Oooh boy
            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                var patchNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                patchNpc.Configuration.MagickaOffset += 2;
            }

            // We will not go over the more advanced Mutagen usage here, as this area is focused on how
            // to take advantage of specific Synthesis features.  To get more examples of Mutagen usage,
            // that will still apply here, check out the Freeform project.
        }
    }
}
