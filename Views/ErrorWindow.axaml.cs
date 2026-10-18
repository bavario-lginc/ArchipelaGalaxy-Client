using System;
using ArchipelaGalaxy.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ArchipelaGalaxy.Helpers;
using Avalonia.Platform.Storage;
using System.Collections.Generic;

namespace ArchipelaGalaxy.Views
{
    public partial class ErrorWindow : Window
    {
        public ErrorWindow ()
        {
            
        }
        public ErrorWindow(string title, string message)
        {
            InitializeComponent();
            Title = title;
            this.message.Text = message;
        }
    }
}