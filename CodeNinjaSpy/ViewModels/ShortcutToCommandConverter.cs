﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MufflonoSoft.CodeNinjaSpy.ViewModels
{
    internal class ShortcutToCommandConverter
    {
        public event EventHandler<CommandFetchingStatusUpdatedEventArgs> CommandFetchingStatusUpdated;

        private readonly List<Command> _commands = new List<Command>();

        public ShortcutToCommandConverter()
        {
            System.Threading.Tasks.Task.Factory.StartNew(FetchCommandsWithBindings);
        }

        private void FetchCommandsWithBindings()
        {
            OnCommandFetchingStatusUpdatedEventArgs(0, "", true);

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

            if (dte != null)
            {
                var numberOfCommands = (double)dte.Commands.Count;
                var commandCounter = 0;

                try
                {
                    foreach (EnvDTE.Command command in dte.Commands)
                    {
                        commandCounter++;
                        var status = commandCounter / numberOfCommands * 100.0;
                        var statusText = string.Format("Command {0} of {1}", commandCounter, numberOfCommands);
                        OnCommandFetchingStatusUpdatedEventArgs(status, statusText, true);

                        var bindings = command.Bindings as object[];

                        if (bindings != null && bindings.Length > 0)
                            _commands.Add(new Command(command.Name, GetBindings(bindings)));
                    }
                }
                catch (InvalidComObjectException)
                {
                    // user closed the window or closed Visual Studio.
                }

                OnCommandFetchingStatusUpdatedEventArgs(100, "", false);
            }
        }

        private static List<string> GetBindings(IEnumerable<object> bindings)
        {
            var result = bindings.Select(binding => binding.ToString().IndexOf("::") >= 0
                ? binding.ToString().Substring(binding.ToString().IndexOf("::") + 2)
                : binding.ToString()).ToList();

            return result;
        }

        private void OnCommandFetchingStatusUpdatedEventArgs(double status, string statusText, bool isLoading)
        {
            var handler = CommandFetchingStatusUpdated;

            if (handler != null)
                handler(this, new CommandFetchingStatusUpdatedEventArgs(status, statusText, isLoading));
        }

        public bool TryGetCommand(ICollection<Keys> pressedKeys, out Command calledCommand)
        {
            calledCommand = null;

            if (pressedKeys.Count > 1)
            {
                var pressedKeyBinding = ConvertToKeyBinding(pressedKeys);

                foreach (var command in _commands)
                {
                    if (WasThisTheCommand(command, pressedKeyBinding, ref calledCommand))
                        return true;
                }
            }

            return false;
        }

        private static bool WasThisTheCommand(Command command, string pressedKeyBinding, ref Command calledCommand)
        {
            foreach (var binding in command.Bindings)
            {
                if (binding.ToLower() == pressedKeyBinding)
                {
                    var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

                    if (dte.Commands.Item(command.Name).IsAvailable)
                    {
                        calledCommand = new Command(command.Name, new List<string>{pressedKeyBinding});
                        return true;
                    }
                }
            }

            return false;
        }

        private static string ConvertToKeyBinding(IEnumerable<Keys> pressedKeysParameter)
        {
            var keyBinding = "";
            var pressedKeys = pressedKeysParameter.Select(k => k); // just copy

            // first ctrl
            if (pressedKeys.Any(IsControlKey))
            {
                keyBinding = "ctrl+";
                pressedKeys = pressedKeys.Where(k => !IsControlKey(k));
            }

            // then shift
            if (pressedKeys.Any(IsShiftKey))
            {
                keyBinding += "shift+";
                pressedKeys = pressedKeys.Where(k => !IsShiftKey(k));
            }

            // then alt
            if (pressedKeys.Any(IsAltKey))
            {
                keyBinding += "alt+";
                pressedKeys = pressedKeys.Where(k => !IsAltKey(k));
            }

            keyBinding = pressedKeys.Aggregate(keyBinding, (current, pressedKey) => current + (pressedKey.ToString().ToLower() + "+"));

            return keyBinding.Substring(0, keyBinding.Length - 1);
        }

        private static bool IsControlKey(Keys key)
        {
            return (key == Keys.Control ||
                    key == Keys.ControlKey ||
                    key == Keys.LControlKey ||
                    key == Keys.RControlKey);
        }

        private static bool IsShiftKey(Keys key)
        {
            return (key == Keys.Shift ||
                    key == Keys.ShiftKey ||
                    key == Keys.LShiftKey ||
                    key == Keys.RShiftKey);
        }

        private static bool IsAltKey(Keys key)
        {
            return (key == Keys.Alt ||
                    key == Keys.LMenu);
        }
    }
}