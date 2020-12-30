using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using HunterPie.Core.Monsters;

namespace Plugin.MonsterSizeBar
{
    /// <summary>
    /// Interaction logic for MonsterSizeControl.xaml
    /// </summary>
    public partial class MonsterSizeControl : UserControl, INotifyPropertyChanged
    {
        public MonsterSizeControl()
        {
            SetDefaultNotchValues();
            InitializeComponent();
        }

        public Thickness MiniNotchShift { get; set; }
        public Thickness MiddleNotchShift { get; set; }
        public Thickness SilverNotchShift { get; set; }
        public Thickness GoldNotchShift { get; set; }
        public double BarWidth { get; set; }

        public double TotalWidth = 230;

        public void Update(CrownInfo crowns, float size)
        {
            var maxSize = Math.Max(crowns.Gold, size) * 1.05f;
            var minSize = Math.Min(crowns.Mini, size) * 0.9f;
            var alocatedSize = maxSize - minSize;

            MiniNotchShift = CreateLeftThickness((crowns.Mini - minSize) / alocatedSize);
            MiddleNotchShift = CreateLeftThickness((1 - minSize) / alocatedSize);
            SilverNotchShift = CreateLeftThickness((crowns.Silver - minSize) / alocatedSize);
            GoldNotchShift = CreateLeftThickness((crowns.Gold - minSize) / alocatedSize);
            BarWidth = TotalWidth * ((size - minSize) / alocatedSize);
            
            InvokePropertyChanged(nameof(MiniNotchShift));
            InvokePropertyChanged(nameof(MiddleNotchShift));
            InvokePropertyChanged(nameof(SilverNotchShift));
            InvokePropertyChanged(nameof(GoldNotchShift));
            InvokePropertyChanged(nameof(BarWidth));
        }

        private void SetDefaultNotchValues()
        {
            MiniNotchShift = CreateLeftThickness(TotalWidth * .25);
            MiddleNotchShift = CreateLeftThickness(TotalWidth * .5);
            SilverNotchShift = CreateLeftThickness(TotalWidth * .65);
            GoldNotchShift = CreateLeftThickness(TotalWidth * .95);
            BarWidth = .25;
        }
        
        private Thickness CreateLeftThickness(double left) => new Thickness(TotalWidth * left, 0, 0, 0);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void InvokePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
