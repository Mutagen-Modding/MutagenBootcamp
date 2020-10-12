using Loqui;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MutagenBootcamp.Freeform
{
    class Program
    {
        static void Main(string[] args)
        {
            // ToDo
            // Set your SE Skyrim.esm path here
            var pathToSESkyrimESM = @"D:\Games\steamapps\common\Skyrim Special Edition\Data\Skyrim.esm";

            // ToDo
            // Set your desired output location here
            var outputPath = @"output.esp";

            // Just checking you have set your data path
            if (string.IsNullOrWhiteSpace(pathToSESkyrimESM) 
                || !File.Exists(pathToSESkyrimESM))
            {
                System.Console.WriteLine("Set your Skyrim path in the main function!  Exiting.");
                return;
            }

            // Not necessary, but slightly speeds up initial bootstrapping if done explicitly
            WarmupAll.Init();

            // Run some examples
            try
            {
                PrintAllWeapons(pathToSESkyrimESM);
                DoSomeLinking(pathToSESkyrimESM);
                MakeAMod(pathToSESkyrimESM, outputPath);
            }
            catch (Exception ex)
            {
                // Print errors if things go wrong
                System.Console.WriteLine();
                System.Console.WriteLine("Oh no:");
                System.Console.WriteLine(ex.ToString());
            }

            // Print that we are done, and wait for user
            System.Console.WriteLine("Done!  Press enter to exit.");
            System.Console.ReadLine();
        }

        /// <summary>
        /// Loops over all weapons, and prints their EditorIDs and FormKeys
        /// </summary>
        static void PrintAllWeapons(string path)
        {
            // Create a readonly mod object from the file path, using the overlay pattern
            using var mod = SkyrimMod.CreateFromBinaryOverlay(path, SkyrimRelease.SkyrimSE);

            // Loop and print
            System.Console.WriteLine($"Printing all weapons:");
            foreach (var weap in mod.Weapons)
            {
                System.Console.WriteLine($"  {weap.EditorID}: {weap.FormKey}");
            }
            PrintSeparator();
        }

        /// <summary>
        /// Shows how to follow a FormID reference to find another record
        /// </summary>
        static void DoSomeLinking(string path)
        {
            // Create a readonly mod object from the file path, using the overlay pattern
            using var mod = SkyrimMod.CreateFromBinaryOverlay(path, SkyrimRelease.SkyrimSE);

            // Create a link cache to store info about all the linking queries we do
            var cache = mod.ToImmutableLinkCache();

            System.Console.WriteLine($"Printing all worn armors that Npcs have:");
            foreach (var npc in mod.Npcs)
            {
                // Attempt to find the armor, using the cache
                if (npc.WornArmor.TryResolve(cache, out var armor))
                {
                    System.Console.WriteLine($"  {npc.EditorID}: {armor.EditorID}.");
                }
                else
                {
                    // Had no armor
                }
            }
            PrintSeparator();
        }

        /// <summary>
        /// Some record copying and new record construction.  Writes to an esp
        /// </summary>
        public static void MakeAMod(string inputPath, string outputPath)
        {
            // Create a readonly mod object from the file path, using the overlay pattern
            using var inputMod = SkyrimMod.CreateFromBinaryOverlay(inputPath, SkyrimRelease.SkyrimSE);

            // Create our mod to eventually export
            var outputMod = new SkyrimMod(ModKey.FromNameAndExtension(Path.GetFileName(outputPath)), SkyrimRelease.SkyrimSE);

            // Copy over all existing weapons, while changing their damage
            foreach (var weap in inputMod.Weapons)
            {
                // Make a copy of the readonly record so we can modify it
                var copy = weap.DeepCopy();

                if (copy.BasicStats == null)
                {
                    // Skip any weapon that doesn't have the stats subrecord
                    continue;
                }

                // Bump up the damage!
                copy.BasicStats.Damage += 100;

                // Add to our output mod
                // The record is implicitly an override, as its FormKey originates from Skyrim.esm, not our mod.
                outputMod.Weapons.RecordCache.Set(copy);
                System.Console.WriteLine($"Overriding {copy.EditorID}");
            }

            // Add our own brand new weapon
            // This convenience function creates a new weapon already added to the mod, with a new FormID
            var overpoweredSword = outputMod.Weapons.AddNew();
            overpoweredSword.EditorID = "MutagenBlade";
            overpoweredSword.Name = "Mutagen Blade";
            overpoweredSword.BasicStats = new WeaponBasicStats()
            {
                Damage = 9000,
                Weight = 1,
                Value = 9000,
            };
            // Actually, scrap this.  Too many things to add by hand.  Let's try by using an existing record as a template

            // Let's base our weapon off Skyrim's DraugrSword
            if (inputMod.Weapons.RecordCache.TryGetValue(FormKey.Factory("02C66F:Skyrim.esm"), out var templateSword))
            {
                // Now we can copy in the values we like from our template.  Let's skip the items we already set earlier
                overpoweredSword.DeepCopyIn(
                    templateSword,
                    // We can optionally specify a mask that copies over everything but our earlier items
                    new Weapon.TranslationMask(true)
                    {
                        Name = false,
                        EditorID = false,
                        BasicStats = false
                    });

                // Now we are good to go, as our weapon is already in our mod, and has the values we want
            }
            else
            {
                System.Console.WriteLine("Couldn't create our sword!  The template could not be found.");
                // I guess let's remove it
                outputMod.Weapons.RecordCache.Remove(overpoweredSword);
            }

            // Write out our mod
            outputMod.WriteToBinaryParallel(outputPath);
            System.Console.WriteLine($"Wrote out mod to: {new FilePath(outputPath).Path}");

            PrintSeparator();
        }

        static void PrintSeparator()
        {
            System.Console.WriteLine("---------------");
            System.Console.WriteLine();
        }
    }
}
