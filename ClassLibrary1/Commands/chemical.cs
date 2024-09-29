using CommandSystem;
using Exiled.API.Features;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NorthwoodLib;
using UnityEngine;
using Exiled.API.Features.Items;
using ClassLibrary1;

namespace ClassLibrary1
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class ChemicalCommandfr : ICommand
    {
        public string Command { get; } = "chemical";
        public string[] Aliases { get; } = { "chem" };
        public string Description { get; } = "Manages chemicals, allowing creation, modification, and deletion of chemical files.";

        // Path to the plugins directory
        private readonly string logFilePath = Path.Combine(Exiled.API.Features.Paths.Plugins, "ChemicalLog.txt");

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);

            if (!Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out RaycastHit raycastHitt,
                   5, ~(1 << 1 | 1 << 13 | 1 << 16 | 1 << 28)))
            {
                if (raycastHitt.collider.gameObject != null)
                {


                    //ev.Player.Broadcast(new($"1 This {raycastHitt.collider}, {builder.ToString()}", 5));
                }
            }

            else
            {
                Player raycastedPlayer = Player.Get(raycastHitt.collider);
                if (raycastHitt.collider.gameObject != null)
                {
                    if (raycastHitt.collider.gameObject.name.Contains("drawerCabinet_hanger"))
                    {
                        string myarguments = string.Join(" ", arguments);

                        if (IsChemicalInFile($"{myarguments}"))
                        {
                            player.ShowHint($"You picked {myarguments}", 5);

                            // Add the item and capture the returned reference
                            var newItem = player.AddItem(ItemType.Adrenaline);

                            if (newItem != null)
                            {
                                uint itemSerial = newItem.Serial;
                                Log.Info($"New Adrenaline item added to {player.Nickname}, Serial: {itemSerial}");

                                // Add the chemical to the item using its serial number
                                bool added = Class1.Instance.AddChemicalToItem(itemSerial, $"{myarguments}");
                                if (added)
                                {
                                    Log.Info($"Chemical {myarguments} added to item with serial {itemSerial}.");
                                }
                                else
                                {
                                    Log.Warn($"Failed to add chemical {myarguments} to item with serial {itemSerial}.");
                                }
                            }
                            else
                            {
                                Log.Error("Failed to add Adrenaline item to the player.");
                            }

                            response = $"You picked {myarguments}";
                            return true;
                        }
                    }
                }
            }
            response = "Epic";
            return false;
        }

        private bool IsChemicalInFile(string chemicalName)
        {
            try
            {
                // Ensure the file exists, if not, create it
                if (!File.Exists(logFilePath))
                {
                    // Create the file with an optional header (or leave it empty)
                    File.WriteAllText(logFilePath, ""); // Add a header if needed
                    Log.Info($"Chemical log file created at {logFilePath}.");
                }

                // Read all lines from the file
                string[] lines = File.ReadAllLines(logFilePath);

                // Check if any line contains the chemical name
                foreach (string line in lines)
                {
                    if (line.Trim().Equals(chemicalName.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Found the chemical
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error reading or creating chemical log file: {ex.Message}");
            }

            return false; // Chemical not found or error occurred
        }
    }
}