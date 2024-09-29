using CommandSystem;
using Exiled.API.Features;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace ClassLibrary1
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ChemicalCommand : ICommand
    {
        public string Command { get; } = "chem";
        public string[] Aliases { get; } = { "ch" };
        public string Description { get; } = "Manages chemicals, allowing creation, modification, and deletion of chemical files.";

        private readonly string chemicalsFolderPath = Path.Combine(Exiled.API.Features.Paths.Plugins, "Chemicals");

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            if (player == null)
            {
                response = "Command must be run by a player.";
                return false;
            }

            if (arguments.Count == 0)
            {
                response = "Usage: .chemical <create/list/view/add/remove/delete> [arguments]";
                return false;
            }

            string action = arguments.At(0).ToLower();

            switch (action)
            {
                case "create":
                    if (arguments.Count != 2)
                    {
                        response = "Usage: .chemical create <name>";
                        return false;
                    }
                    CreateChemicalFile(arguments.At(1), out response);
                    break;

                case "list":
                    ListChemicals(out response);
                    break;

                case "view":
                    if (arguments.Count != 2)
                    {
                        response = "Usage: .chemical view <name>";
                        return false;
                    }
                    ViewChemicalFile(arguments.At(1), out response);
                    break;

                case "add":
                    if (arguments.Count != 3)
                    {
                        response = "Usage: .chemical add <name> <chemical>";
                        return false;
                    }
                    AddChemical(arguments.At(1), arguments.At(2), out response);
                    break;

                case "remove":
                    if (arguments.Count != 3)
                    {
                        response = "Usage: .chemical remove <name> <chemical>";
                        return false;
                    }
                    RemoveChemical(arguments.At(1), arguments.At(2), out response);
                    break;

                case "delete":
                    if (arguments.Count != 2)
                    {
                        response = "Usage: .chemical delete <name>";
                        return false;
                    }
                    DeleteChemicalFile(arguments.At(1), out response);
                    break;

                default:
                    response = "Unknown command. Usage: .chemical <create/list/view/add/remove/delete>";
                    return false;
            }

            return true;
        }

        private void CreateChemicalFile(string name, out string response)
        {
            string filePath = Path.Combine(chemicalsFolderPath, $"{name}.txt");

            if (File.Exists(filePath))
            {
                response = $"Chemical file '{name}' already exists.";
                return;
            }

            try
            {
                File.WriteAllText(filePath, ""); // Create an empty file
                response = $"Chemical file '{name}' created successfully.";
            }
            catch (Exception ex)
            {
                response = $"Error creating file: {ex.Message}";
            }
        }

        private void ListChemicals(out string response)
        {
            try
            {
                if (!Directory.Exists(chemicalsFolderPath))
                {
                    Directory.CreateDirectory(chemicalsFolderPath);
                }

                var files = Directory.GetFiles(chemicalsFolderPath, "*.txt")
                    .Select(Path.GetFileNameWithoutExtension).ToArray();

                if (files.Length == 0)
                {
                    response = "No chemicals found.";
                }
                else
                {
                    response = "Available chemicals: " + string.Join(", ", files);
                }
            }
            catch (Exception ex)
            {
                response = $"Error listing chemicals: {ex.Message}";
            }
        }

        private void ViewChemicalFile(string name, out string response)
        {
            string filePath = Path.Combine(chemicalsFolderPath, $"{name}.txt");

            if (!File.Exists(filePath))
            {
                response = $"Chemical file '{name}' does not exist.";
                return;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                response = string.IsNullOrEmpty(content) ? "File is empty." : content;
            }
            catch (Exception ex)
            {
                response = $"Error reading file: {ex.Message}";
            }
        }

        private void AddChemical(string name, string chemical, out string response)
        {
            string filePath = Path.Combine(chemicalsFolderPath, $"{name}.txt");

            if (!File.Exists(filePath))
            {
                response = $"Chemical file '{name}' does not exist.";
                return;
            }

            try
            {
                var chemicals = File.ReadAllLines(filePath).ToList();
                if (chemicals.Contains(chemical))
                {
                    response = $"{chemical} is already in the file '{name}'.";
                    return;
                }

                // Append chemical with a new line
                File.AppendAllText(filePath, chemical + Environment.NewLine);
                response = $"{chemical} added to the file '{name}'.";
            }
            catch (Exception ex)
            {
                response = $"Error adding chemical: {ex.Message}";
            }
        }

        private void RemoveChemical(string name, string chemical, out string response)
        {
            string filePath = Path.Combine(chemicalsFolderPath, $"{name}.txt");

            if (!File.Exists(filePath))
            {
                response = $"Chemical file '{name}' does not exist.";
                return;
            }

            try
            {
                var chemicals = File.ReadAllLines(filePath).ToList();
                if (!chemicals.Contains(chemical))
                {
                    response = $"{chemical} not found in the file '{name}'.";
                    return;
                }

                chemicals.Remove(chemical);
                File.WriteAllLines(filePath, chemicals);
                response = $"{chemical} removed from the file '{name}'.";
            }
            catch (Exception ex)
            {
                response = $"Error removing chemical: {ex.Message}";
            }
        }

        private void DeleteChemicalFile(string name, out string response)
        {
            string filePath = Path.Combine(chemicalsFolderPath, $"{name}.txt");

            if (!File.Exists(filePath))
            {
                response = $"Chemical file '{name}' does not exist.";
                return;
            }

            try
            {
                File.Delete(filePath);
                response = $"Chemical file '{name}' deleted.";
            }
            catch (Exception ex)
            {
                response = $"Error deleting file: {ex.Message}";
            }
        }
    }
}
