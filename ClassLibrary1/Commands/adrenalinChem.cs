using CommandSystem;
using Exiled.API.Features;
using System;

namespace ClassLibrary1
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class AdrenalinChemCommand : ICommand
    {
        public string Command { get; } = "adrenalinchem";
        public string[] Aliases { get; } = { "adchem" };
        public string Description { get; } = "Add or remove chemicals from the adrenalin item you're holding.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);

            if (player == null)
            {
                response = "This command can only be run by a player.";
                return false;
            }

            if (arguments.Count < 2)
            {
                response = "Usage: .adrenalinchem <add/remove> <chemical>";
                return false;
            }

            // Parse the command type (add/remove)
            string commandType = arguments.At(0).ToLower();
            string chemical = arguments.At(1);

            // Check if the player is holding an item, and that the item is Adrenaline
            if (player.CurrentItem == null || player.CurrentItem.Type != ItemType.Adrenaline)
            {
                response = "You are not holding an Adrenaline item.";
                return false;
            }

            // Get the item's serial number (unique identifier)
            uint itemSerial = player.CurrentItem.Serial;

            bool success = false;

            // Add or remove chemical based on command type
            if (commandType == "add")
            {
                success = Class1.Instance.AddChemicalToItem(itemSerial, chemical);
                response = success ? $"Successfully added {chemical} to Adrenaline item {itemSerial}." : $"{chemical} is already on this Adrenaline.";
            }
            else if (commandType == "remove")
            {
                success = Class1.Instance.RemoveChemicalFromItem(itemSerial, chemical);
                response = success ? $"Successfully removed {chemical} from Adrenaline item {itemSerial}." : $"{chemical} was not found on this Adrenaline.";
            }
            else
            {
                response = "Invalid command type. Use 'add' or 'remove'.";
                return false;
            }

            return success;
        }
    }
}
