using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Player = Exiled.API.Features.Player;
using Pickup = Exiled.API.Features.Pickups;
using UnityEngine;
using System.IO;
using InventorySystem.Items.Pickups; // For ItemPickupBase
using Exiled.Events.EventArgs.Player;
using Exiled.API.Interfaces;
using Exiled.Loader;
using CommandSystem.Commands.RemoteAdmin.Cleanup;
using Exiled.API.Features.Items;
using CommandSystem.Commands.Shared;
using System.Linq;

namespace ClassLibrary1
{
    public class Class1 : Plugin<Config>
    {
        public static Class1 Instance;

        // Dictionary to store chemicals and their ingredient lists globally
        public Dictionary<string, List<string>> Chemicals = new Dictionary<string, List<string>>();

        // Dictionary to track player's active adrenalin chemicals, using the item serial as the key
        public Dictionary<uint, List<string>> ItemChemicals = new Dictionary<uint, List<string>>();


        public override void OnEnabled()
        {
            Instance = this;
            // Load chemicals from file and initialize
            LoadChemicals();

            // Subscribe to player events
            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;
            Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoclip;
            Exiled.Events.Handlers.Player.UsingItem += OnUsingItem;
            Exiled.Events.Handlers.Player.ChangingItem += OnChangingItem;
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            // Unsubscribe from events
            Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;
            Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoclip;
            Exiled.Events.Handlers.Player.UsingItem -= OnUsingItem;
            Exiled.Events.Handlers.Player.ChangingItem -= OnChangingItem;

            ItemChemicals.Clear();
            base.OnDisabled();
        }

        private void OnPlayerVerified(VerifiedEventArgs ev)
        {
            // When player is verified, we can initialize other things if needed.
        }

        private void OnUsingItem(UsingItemEventArgs ev)
        {
            // Check if the player is using an Adrenaline item
            if (ev.Item.Type == ItemType.Adrenaline)
            {
                ev.IsAllowed = false;
                uint itemSerial = ev.Item.Serial;

                // Check if the item has any chemicals
                if (ItemChemicals.ContainsKey(itemSerial) && ItemChemicals[itemSerial].Count > 0)
                {
                    // Find the last chemical added to the item
                    string lastChemical = GetLastChemicalBySerial(itemSerial);

                    if (!string.IsNullOrEmpty(lastChemical))
                    {
                        // Try to apply effects from the Effects folder
                        ApplyChemicalEffects(ev.Player, lastChemical);
                    }
                    else
                    {
                        ev.Player.ShowHint("No chemicals found in the Adrenaline.", 3);
                    }
                }
                else
                {
                    ev.Player.ShowHint("No chemicals found in the Adrenaline.", 3);
                }
            }
        }

        // Method to apply effects based on the chemical name
        private void ApplyChemicalEffects(Player player, string chemical)
        {
            // Path to the Effects folder
            string effectsFolderPath = Path.Combine(Exiled.API.Features.Paths.Plugins, "Effects");

            // Path to the specific effect file for the chemical
            string effectFilePath = Path.Combine(effectsFolderPath, $"{chemical}.txt");

            // Check if the effect file exists
            if (!File.Exists(effectFilePath))
            {
                player.ShowHint($"You drank {chemical}", 5);
                Class1.Instance.RemoveChemicalFromItem(player.CurrentItem.Serial, $"{chemical}");
                return; // No file, no effects
            }

            try
            {
                // Read all lines (effects) from the effect file
                var effects = File.ReadAllLines(effectFilePath).ToList();

                // Apply each effect if it's a valid Exiled effect type
                foreach (string effect in effects)
                {
                    ApplyDynamicEffectToPlayer(player, effect); // Dynamically apply the effect
                }

                // Inform the player that the effects have been applied
                player.ShowHint($"You drank {chemical}", 5);
                Class1.Instance.RemoveChemicalFromItem(player.CurrentItem.Serial, $"{chemical}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error reading effect file for {chemical}: {ex.Message}");
                player.ShowHint($"You drank {chemical}", 5);
                Class1.Instance.RemoveChemicalFromItem(player.CurrentItem.Serial, $"{chemical}");
            }
        }

        // Method to apply a dynamic Exiled effect to the player based on the effect name
        private void ApplyDynamicEffectToPlayer(Player player, string effectName)
        {
            // Try to parse the effect name as an Exiled EffectType
            if (Enum.TryParse(effectName, true, out Exiled.API.Enums.EffectType effectType))
            {
                // Enable the effect on the player for a default duration (e.g., 10 seconds)
                player.EnableEffect(effectType, 10); // You can customize the duration as needed
                Log.Info($"Applied effect: {effectType} to player {player.Nickname}");
            }
            else
            {
                Log.Warn($"Invalid effect type: {effectName} for player {player.Nickname}");
            }
        }


        private void OnTogglingNoclip(TogglingNoClipEventArgs ev)
        {
            if (ev.Player.CurrentItem == null) 
            {
                return;
            }


            if (ev.Player.CurrentItem.Type == ItemType.Adrenaline) 
            {
                if (!Physics.Raycast(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward, out RaycastHit raycastHitt,
                   5, ~(1 << 1 | 1 << 13 | 1 << 16 | 1 << 28)))
                {
                    if (raycastHitt.collider.gameObject != null)
                    {


                        //ev.Player.Broadcast(new($"1 This {raycastHitt.collider}, {builder.ToString()}", 5));
                    }
                    ev.Player.ShowHint("Nuh uh", 5);
                }

                else
                {
                    Player raycastedPlayer = Player.Get(raycastHitt.collider);
                    if (raycastHitt.collider.gameObject != null)
                    {
                        if (raycastHitt.collider.gameObject.name.Contains("centrifuge")) 
                        {
                            uint itemSerial = ev.Player.CurrentItem.Serial;

                            // Check if the item has any chemicals
                            if (ItemChemicals.ContainsKey(itemSerial))
                            {
                                List<string> itemChemicals = ItemChemicals[itemSerial];

                                // Get the chemical name associated with this centrifuge interaction
                                string createdChemical = MatchChemicals(itemChemicals);

                                if (!string.IsNullOrEmpty(createdChemical))
                                {
                                    // Successfully created a new chemical, now clear the old ones
                                    ItemChemicals[itemSerial].Clear();

                                    // Add the new combined chemical to the item
                                    ItemChemicals[itemSerial].Add(createdChemical);

                                    // Optionally update the player's display/UI to reflect the new chemical
                                    ev.Player.ShowHint($"Successfully created: {createdChemical}", 5);
                                }
                                else
                                {
                                    ev.Player.ShowHint("Invalid chemical combination.", 5);
                                }
                            }
                            else
                            {
                                ev.Player.ShowHint("No chemicals found in the adrenaline.", 5);
                            }
                            return;
                        }
                    }
                }

                
                // Raycast to detect what the player is looking at (e.g., a wall)
                if (Physics.Raycast(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward, out RaycastHit raycastHit, 5f))
                {
                    // Get the position where the raycast hit (e.g., the wall)
                    Vector3 hitPosition = raycastHit.point;

                    // Define a search radius (e.g., 1.5 units)
                    float searchRadius = 1.5f;

                    // Find the closest pickup to the hit point
                    ItemPickupBase closestPickup = FindClosestPickup(hitPosition, searchRadius);

                    if (closestPickup != null || !string.IsNullOrEmpty($"{closestPickup}"))
                    {
                        // Get details about the pickup (serial, item type, etc.)
                        uint serialNumber = closestPickup.NetworkInfo.Serial;
                        ItemType itemType = closestPickup.NetworkInfo.ItemId;

                        
                        bool Hrozno = Class1.Instance.AddChemicalToItem(serialNumber, $"{GetLastChemicalBySerial(ev.Player.CurrentItem.Serial)}");
                        if (Hrozno == true) 
                        {
                            ev.Player.ShowHint($"You added {GetLastChemicalBySerial(ev.Player.CurrentItem.Serial)}", 5);
                        }

                        if (Hrozno == false)
                        {
                            ev.Player.ShowHint($"Chemical {GetLastChemicalBySerial(ev.Player.CurrentItem.Serial)} was already inside", 5);
                        }
                        Class1.Instance.RemoveChemicalFromItem(ev.Player.CurrentItem.Serial, $"{GetLastChemicalBySerial(ev.Player.CurrentItem.Serial)}");
                        //ev.Player.ShowHint($"Found closest pickup: {closestPickup.name}, Serial: {serialNumber}, ItemType: {itemType}", 5);
                    }
                    else
                    {
                        //ev.Player.ShowHint("No pickup found near the hit point.", 5);
                    }
                }
                else
                {
                    //ev.Player.ShowHint("No object hit by the raycast.", 5);
                }
            }
            else 
            {
                return;
            }
        }

        // Function to check if the chemicals match any predefined chemical in the dictionary
        // Path to the folder where the chemical files are stored
        private readonly string chemicalsFolderPath = Path.Combine(Exiled.API.Features.Paths.Plugins, "Chemicals");

        // Modified MatchChemicals method that checks against file contents
        private string MatchChemicals(List<string> currentChemicals)
        {
            // Ensure the directory exists
            if (!Directory.Exists(chemicalsFolderPath))
            {
                Log.Warn($"No chemicals directory found at {chemicalsFolderPath}");
                return null;
            }

            // Go through each file in the chemicals folder
            foreach (var filePath in Directory.GetFiles(chemicalsFolderPath, "*.txt"))
            {
                // Read the chemicals from the file
                var fileChemicals = File.ReadAllLines(filePath).Select(c => c.Trim()).ToList();

                // Check if the list of chemicals from the item exactly matches the ones in the file
                if (fileChemicals.Count == currentChemicals.Count && !currentChemicals.Except(fileChemicals).Any())
                {
                    // Return the name of the chemical (file name without extension)
                    return Path.GetFileNameWithoutExtension(filePath);
                }
            }

            // No match found
            return null;
        }

        // Method to find the closest pickup to a given position within a radius
        private ItemPickupBase FindClosestPickup(Vector3 hitPosition, float radius)
        {
            // Get all pickups in the game (iterate through active pickups)
            var pickups = UnityEngine.Object.FindObjectsOfType<ItemPickupBase>();

            ItemPickupBase closestPickup = null;
            float closestDistance = radius; // We want to find pickups within this radius

            // Iterate through all pickups and find the closest one within the radius
            foreach (var pickup in pickups)
            {
                float distanceToPickup = Vector3.Distance(hitPosition, pickup.transform.position);

                if (distanceToPickup <= closestDistance)
                {
                    closestPickup = pickup;
                    closestDistance = distanceToPickup; // Update the closest distance
                }
            }

            return closestPickup; // Return the closest pickup (or null if none found)
        }

        // Load the chemicals from the chemical.txt file
        private void LoadChemicals()
        {
            string chemicalFilePath = Path.Combine(Exiled.API.Features.Paths.Plugins, "Chemicals", "chemical.txt");

            if (!File.Exists(chemicalFilePath))
            {
                Log.Warn($"No chemical.txt file found in Chemicals folder.");
                return;
            }

            // Read the file line by line, assuming each line is formatted like: ChemicalName = Ingredient1 + Ingredient2 + ...
            foreach (var line in File.ReadAllLines(chemicalFilePath))
            {
                var parts = line.Split('=');
                if (parts.Length != 2)
                    continue; // Skip invalid lines

                string chemicalName = parts[0].Trim();
                List<string> ingredients = new List<string>(parts[1].Trim().Split('+'));

                if (!Chemicals.ContainsKey(chemicalName))
                    Chemicals.Add(chemicalName, ingredients);
            }

            Log.Info($"Loaded {Chemicals.Count} chemicals.");
        }

        // Helper method to add a chemical to an item's chemical list
        public bool AddChemicalToItem(uint itemSerial, string chemical)
        {
            if (!ItemChemicals.ContainsKey(itemSerial))
            {
                // If no entry exists for this serial, create a new list
                ItemChemicals[itemSerial] = new List<string>();
            }

            // Check if the chemical is already in the list
            if (ItemChemicals[itemSerial].Contains(chemical))
            {
                return false; // Chemical is already present
            }

            // Add the chemical to the list
            ItemChemicals[itemSerial].Add(chemical);
            return true;
        }

        // Helper method to remove a chemical from an item's chemical list
        public bool RemoveChemicalFromItem(uint itemSerial, string chemical)
        {
            if (!ItemChemicals.ContainsKey(itemSerial))
            {
                return false; // No chemicals to remove for this item
            }

            // Try to remove the chemical
            return ItemChemicals[itemSerial].Remove(chemical);
        }

        private void OnChangingItem(ChangingItemEventArgs ev)
        {
            // Ensure that the new item is not null before proceeding
            if (ev.Item == null)
            {
                ev.Player.ShowHint("No item selected.", 3);
                return;
            }

            // Check if the new item is Adrenaline
            if (ev.Item.Type == ItemType.Adrenaline)
            {
                uint itemSerial = ev.Item.Serial; // Use ev.NewItem instead of ev.Player.CurrentItem

                // Check if this item already has a list of chemicals
                if (!ItemChemicals.ContainsKey(itemSerial))
                {
                    // Initialize an empty list if the item doesn't have any chemicals yet
                    ItemChemicals[itemSerial] = new List<string>();
                }

                // Get the list of chemicals for this item
                List<string> chemicals = ItemChemicals[itemSerial];

                // Prepare the message to show the player
                string chemicalList = chemicals.Count > 0 ? string.Join(", ", chemicals) : "No chemicals";

                // Show the player the chemicals in a hint
                ev.Player.ShowHint($"<b>Chemicals:</b>\n{chemicalList}", 5);
            }
        }

        // Helper method to get the last chemical added to an item's chemical list by serial number
        public string GetLastChemicalBySerial(uint itemSerial)
        {
            // Check if the item has any chemicals associated with it
            if (ItemChemicals.ContainsKey(itemSerial) && ItemChemicals[itemSerial].Count > 0)
            {
                // Get the last chemical from the list
                return ItemChemicals[itemSerial][ItemChemicals[itemSerial].Count - 1];
            }

            // If no chemicals are found, return null or an appropriate message
            return null; // Or return a placeholder value like "No chemicals"
        }
        private string CheckForValidChemical(List<string> currentChemicals)
        {
            // Loop through the predefined chemicals and check if the current chemicals match exactly
            foreach (var chemical in Chemicals)
            {
                // Check if the current item has the exact chemical combination (no extra or missing)
                if (chemical.Value.Count == currentChemicals.Count && !currentChemicals.Except(chemical.Value).Any())
                {
                    return chemical.Key; // Return the name of the chemical that matches
                }
            }

            return null; // No valid chemical combination found
        }

    }
}
