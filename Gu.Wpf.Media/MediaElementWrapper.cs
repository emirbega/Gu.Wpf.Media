﻿namespace Gu.Wpf.Media
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Threading;

    public partial class MediaElementWrapper : Decorator
    {
        private readonly DispatcherTimer updatePositionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.1) };
        private readonly MediaElement mediaElement;

        public MediaElementWrapper()
        {
            this.mediaElement = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Source = this.Source,
            };
            this.mediaElement.MediaOpened += (o, e) =>
            {
                this.Pause();
                this.Length = this.mediaElement.NaturalDuration.TimeSpan;
                CommandManager.InvalidateRequerySuggested();
            };

            this.Bind(SourceProperty, MediaElement.SourceProperty);
            this.Bind(VolumeProperty, MediaElement.VolumeProperty);
            this.Bind(BalanceProperty, MediaElement.BalanceProperty);
            this.Bind(IsMutedProperty, MediaElement.IsMutedProperty);
            this.Bind(ScrubbingEnabledProperty, MediaElement.ScrubbingEnabledProperty);
            this.CommandBindings.Add(new CommandBinding(MediaCommands.Play, HandleExecute(this.Play), HandleCanExecute(this.CanPlay)));
            this.CommandBindings.Add(new CommandBinding(MediaCommands.Pause, HandleExecute(this.Pause), HandleCanExecute(this.CanPause)));
            this.CommandBindings.Add(new CommandBinding(MediaCommands.Stop, HandleExecute(this.Stop), HandleCanExecute(this.CanStop)));
            this.CommandBindings.Add(new CommandBinding(MediaCommands.TogglePlayPause, HandleExecute(this.TogglePlayPause), HandleCanExecute(() => this.CanPlay() || this.CanPause())));
            this.CommandBindings.Add(new CommandBinding(MediaCommands.Rewind, HandleExecute(this.Rewind), HandleCanExecute(this.CanRewind)));
            this.CommandBindings.Add(new CommandBinding(MediaCommands.IncreaseVolume, HandleExecute(() => this.IncreaseVolume(this.VolumeIncrement)), HandleCanExecute(this.CanIncreaseVolume)));
            this.CommandBindings.Add(new CommandBinding(MediaCommands.DecreaseVolume, HandleExecute(() => this.DecreaseVolume(this.VolumeIncrement)), HandleCanExecute(this.CanDecreaseVolume)));
            this.CommandBindings.Add(new CommandBinding(MediaCommands.MuteVolume, HandleExecute(this.Mute), HandleCanExecute(this.CanMute)));
            this.updatePositionTimer.Tick += (o, e) => this.Position = this.mediaElement?.Position;
        }

        public override void BeginInit()
        {
            this.Child = this.mediaElement;
            base.BeginInit();
        }

        public bool CanPlay()
        {
            return this.mediaElement.Source != null && this.State != MediaState.Play;
        }

        public void Play()
        {
            this.State = MediaState.Play;
        }

        public bool CanPause()
        {
            return this.mediaElement.Source != null && this.State == MediaState.Play;
        }

        public void Pause()
        {
            this.State = MediaState.Pause;
        }

        public void TogglePlayPause()
        {
            this.State = this.State != MediaState.Play
                             ? MediaState.Play
                             : MediaState.Pause;
        }

        public bool CanStop()
        {
            return this.mediaElement.Source != null && this.State == MediaState.Play;
        }

        public void Stop()
        {
            this.State = MediaState.Stop;
        }

        private bool CanRewind()
        {
            return this.mediaElement.Source != null && this.Position > TimeSpan.Zero;
        }

        private void Rewind()
        {
            this.Position = TimeSpan.Zero;
        }

        private bool CanDecreaseVolume()
        {
            return this.Volume > 0;
        }

        private void DecreaseVolume(double increment)
        {
            this.Volume = Math.Min(this.mediaElement.Volume - increment, 1);
        }

        private bool CanIncreaseVolume()
        {
            return this.Volume < 1;
        }

        private void IncreaseVolume(double increment)
        {
            this.Volume = Math.Min(this.mediaElement.Volume + increment, 1);
        }

        private bool CanMute()
        {
            return !this.IsMuted;
        }

        private void Mute()
        {
            this.IsMuted = true;
        }

        private void Bind(DependencyProperty sourceProperty, DependencyProperty targetProperty)
        {
            var binding = new Binding(sourceProperty.Name) { Mode = BindingMode.OneWay, Source = this };
            BindingOperations.SetBinding(this.mediaElement, targetProperty, binding);
        }

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var wrapper = (MediaElementWrapper)d;
            if (wrapper.mediaElement != null)
            {
                wrapper.mediaElement.Position = (TimeSpan)e.NewValue;
            }
        }

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var wrapper = (MediaElementWrapper)d;
            if (wrapper.mediaElement.Source == null)
            {
                return;
            }

            switch ((MediaState)e.NewValue)
            {
                case MediaState.Play:
                    wrapper.mediaElement.Play();
                    wrapper.updatePositionTimer.Start();
                    break;
                case MediaState.Close:
                    wrapper.mediaElement.Close();
                    wrapper.updatePositionTimer.Stop();
                    break;
                case MediaState.Pause:
                    wrapper.mediaElement.Pause();
                    wrapper.updatePositionTimer.Stop();
                    break;
                case MediaState.Stop:
                    wrapper.mediaElement.Stop();
                    wrapper.updatePositionTimer.Stop();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static ExecutedRoutedEventHandler HandleExecute(Action action)
        {
            return (o, e) =>
                {
                    action();
                    e.Handled = true;
                };
        }

        private static CanExecuteRoutedEventHandler HandleCanExecute(Func<bool> canExecute)
        {
            return (o, e) =>
            {
                e.CanExecute = canExecute();
                e.Handled = true;
            };
        }
    }
}
