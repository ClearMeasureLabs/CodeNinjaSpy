﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace MufflonoSoft.CodeNinjaSpy
{
    public partial class CodeNinjaSpyShell : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly InterceptKeys _keyInterceptor;
        private readonly Dispatcher _dispatcher;
        private readonly List<Command> _commands = new List<Command>();
        private string _currentShortcut;
        private string _currentCommand;
        private string _lastShortcut;
        private string _lastCommand;
        private string _nextToLastShortcut;
        private string _nextToLastCommand;

        public CodeNinjaSpyShell()
        {
            InitializeComponent();
            PaintBackground();
            DataContext = this;

            System.Threading.Tasks.Task.Factory.StartNew(FetchCommandsWithBindings);

            _dispatcher = Dispatcher;

            _keyInterceptor = new InterceptKeys();
            _keyInterceptor.KeyIntercepted += (s, e) => TryGetShortcut(e.PressedKeys);
        }

        private void PaintBackground()
        {
            var backgroundSquare = new GeometryDrawing(Brushes.Black, null, new RectangleGeometry(new Rect(0, 0, 100, 100)));

            var aGeometryGroup = new GeometryGroup();
            aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 50, 50)));
            aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(50, 50, 50, 50)));

            var checkerBrush = new LinearGradientBrush();
            checkerBrush.GradientStops.Add(new GradientStop(Colors.Black, 0.0));
            checkerBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0,22,0), 1.0));

            var checkers = new GeometryDrawing(checkerBrush, null, aGeometryGroup);

            var checkersDrawingGroup = new DrawingGroup();
            checkersDrawingGroup.Children.Add(backgroundSquare);
            checkersDrawingGroup.Children.Add(checkers);

            var myBrush = new DrawingBrush
            {
                Drawing = checkersDrawingGroup,
                Viewport = new Rect(0, 0, 0.02, 0.02),
                TileMode = TileMode.Tile,
                Opacity = 0.5
            };

            LayoutRoot.Background = myBrush;
        }

        private void FetchCommandsWithBindings()
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

            if (dte != null)
            {
                foreach (Command command in dte.Commands)
                {
                    var bindings = command.Bindings as object[];

                    if (bindings != null && bindings.Length > 0)
                        _commands.Add(command);
                }
            }
        }

        private void TryGetShortcut(ICollection<Keys> pressedKeys)
        {
            if (pressedKeys.Count > 1)
            {
                var keyBinding = ConvertToKeyBinding(pressedKeys);

                foreach (var command in _commands)
                {
                    var bindings = command.Bindings as object[];

                    foreach (string binding in bindings)
                    {
                        var modifiedClosureProtection = binding;

                        if (!keyBinding.All(pressedKey => modifiedClosureProtection.ToLower().Contains(pressedKey)))
                            continue;

                        UpdateShortcut(keyBinding, command);
                        return;
                    }
                }
            }
        }

        private static List<string> ConvertToKeyBinding(IEnumerable<Keys> pressedKeys)
        {
            var keyBinding = new List<string>();

            foreach (var pressedKey in pressedKeys)
            {
                switch (pressedKey)
                {
                    case Keys.Control:
                    case Keys.ControlKey:
                    case Keys.LControlKey:
                    case Keys.RControlKey:
                        keyBinding.Add("ctrl+");
                        break;

                    case Keys.Shift:
                    case Keys.ShiftKey:
                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                        keyBinding.Add("shift+");
                        break;

                    case Keys.Alt:
                    case Keys.LMenu:
                        keyBinding.Add("alt+");
                        break;

                    default:
                        keyBinding.Add(pressedKey.ToString().ToLower());
                        break;
                }
            }

            return keyBinding;
        }

        private void UpdateShortcut(IEnumerable<string> keyBinding, Command command)
        {
            Action action = () =>
            {
                var lastShortcut = keyBinding.Aggregate("", (s, key) => s + key.ToString());
                if (lastShortcut.EndsWith("+"))
                    lastShortcut = lastShortcut.Substring(0, lastShortcut.Length - 1);

                NextToLastShortcut = LastShortcut;
                NextToLastCommand = LastCommand;
                LastShortcut = CurrentShortcut;
                LastCommand = CurrentCommand;
                CurrentShortcut = lastShortcut;
                CurrentCommand = command.Name;
            };

            _dispatcher.Invoke(action);
        }

        public string CurrentShortcut
        {
            get { return _currentShortcut; }

            set
            {
                _currentShortcut = value;
                OnPropertyChanged("CurrentShortcut");
            }
        }

        public string CurrentCommand
        {
            get { return _currentCommand; }

            set
            {
                _currentCommand = value;
                OnPropertyChanged("CurrentCommand");
            }
        }

        public string LastShortcut
        {
            get { return _lastShortcut; }

            set
            {
                _lastShortcut = value;
                OnPropertyChanged("LastShortcut");
            }
        }

        public string LastCommand
        {
            get { return _lastCommand; }

            set
            {
                _lastCommand = value;
                OnPropertyChanged("LastCommand");
            }
        }

        public string NextToLastShortcut
        {
            get { return _nextToLastShortcut; }

            set
            {
                _nextToLastShortcut = value;
                OnPropertyChanged("NextToLastShortcut");
            }
        }

        public string NextToLastCommand
        {
            get { return _nextToLastCommand; }

            set
            {
                _nextToLastCommand = value;
                OnPropertyChanged("NextToLastCommand");
            }
        }

        private void OnPropertyChanged(string property)
        {
            var handler = PropertyChanged;

            if (handler != null)
                handler(this, new PropertyChangedEventArgs(property));
        }
    }
}