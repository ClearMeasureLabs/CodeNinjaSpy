﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using MufflonoSoft.CodeNinjaSpy.Keyboard;
using StatusBar = EnvDTE.StatusBar;

namespace MufflonoSoft.CodeNinjaSpy.ViewModels
{
    public class CodeNinjaSpyViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _currentShortcut;
        private string _currentCommand;
        private string _lastShortcut;
        private string _lastCommand;
        private string _nextToLastShortcut;
        private string _nextToLastCommand;
        private double _status;
        private string _statusText;
        private bool _isLoading;
        private readonly InterceptKeys _keyInterceptor;
        private readonly ShortcutToCommandConverter _shortcutToCommandConverter = new ShortcutToCommandConverter();

        public CodeNinjaSpyViewModel()
        {
            _shortcutToCommandConverter.CommandFetchingStatusUpdated += (s, e) =>
            {
                Status = e.Status;
                StatusText = e.StatusText;
                IsLoading = e.IsLoading;
            };

            _keyInterceptor = new InterceptKeys();
            _keyInterceptor.KeyIntercepted += (s, e) => TryGetCommand(e.PressedKeys);
        }

        private void TryGetCommand(ICollection<Keys> pressedKeys)
        {
            Command command;

            if (_shortcutToCommandConverter.TryGetCommand(pressedKeys, out command))
                UpdateShortcut(command);
        }

        private void UpdateShortcut(Command command)
        {
            var lastShortcut = command.Bindings.Aggregate("", (s, key) => s + key.ToString());
            if (lastShortcut.EndsWith("+"))
                lastShortcut = lastShortcut.Substring(0, lastShortcut.Length - 1);

            NextToLastShortcut = LastShortcut;
            NextToLastCommand = LastCommand;
            LastShortcut = CurrentShortcut;
            LastCommand = CurrentCommand;
            CurrentShortcut = lastShortcut;
            CurrentCommand = command.Name;
        }

        private void NotifyOfPropertyChange(string property)
        {
            var handler = PropertyChanged;

            if (handler != null)
                handler(this, new PropertyChangedEventArgs(property));
        }

        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }

            set
            {
                NotifyOfPropertyChange("IsLoading");
                _isLoading = value;
            }
        }

        public string StatusText
        {
            get
            {
                return _statusText;
            }

            set
            {
                NotifyOfPropertyChange("StatusText");
                _statusText = value;
            }
        }

        public string CurrentShortcut
        {
            get { return _currentShortcut; }

            set
            {
                _currentShortcut = value;
                NotifyOfPropertyChange("CurrentShortcut");
            }
        }

        public double Status
        {
            get
            {
                return _status;
            }

            set
            {
                NotifyOfPropertyChange("Status");
                _status = value;
            }
        }

        public string CurrentCommand
        {
            get { return _currentCommand; }

            set
            {
                _currentCommand = value;
                NotifyOfPropertyChange("CurrentCommand");
            }
        }

        public string LastShortcut
        {
            get { return _lastShortcut; }

            set
            {
                _lastShortcut = value;
                NotifyOfPropertyChange("LastShortcut");
            }
        }

        public string LastCommand
        {
            get { return _lastCommand; }

            set
            {
                _lastCommand = value;
                NotifyOfPropertyChange("LastCommand");
            }
        }

        public string NextToLastShortcut
        {
            get { return _nextToLastShortcut; }

            set
            {
                _nextToLastShortcut = value;
                NotifyOfPropertyChange("NextToLastShortcut");
            }
        }

        public string NextToLastCommand
        {
            get { return _nextToLastCommand; }

            set
            {
                _nextToLastCommand = value;
                NotifyOfPropertyChange("NextToLastCommand");
            }
        }
    }
}