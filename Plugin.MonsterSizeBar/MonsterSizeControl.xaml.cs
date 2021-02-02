using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HunterPie.Core.Monsters;

namespace Plugin.MonsterSizeBar
{
    public class SizeSnapshot
    {
        public CrownInfo CrownInfo { get; set; }
        public float Size { get; set; }

        public SizeSnapshot(CrownInfo crownInfo, float size)
        {
            this.CrownInfo = crownInfo;
            this.Size = size;
        }
    }
    
    /// <summary>
    /// Interaction logic for MonsterSizeControl.xaml
    /// </summary>
    public partial class MonsterSizeControl : UserControl, INotifyPropertyChanged
    {
        private SizeSnapshot LastSnapshot { get; set; }
        private double lastWidth { get; set; }
        
        public MonsterSizeControl()
        {
            InitializeComponent();
            SetDefaultNotchValues();
            CompositionTarget.Rendering += CompositionTargetOnRendering;
        }

        ~MonsterSizeControl()
        {
            CompositionTarget.Rendering -= CompositionTargetOnRendering;
        }

        private void CompositionTargetOnRendering(object sender, EventArgs e)
        {
            if (LastSnapshot != null && lastWidth != ParentWidth)
            {
                Update(LastSnapshot.CrownInfo, LastSnapshot.Size);
                this.lastWidth = this.ParentWidth;
            }
        }
        
        private double ParentWidth
        {
            get
            {
                try
                {
                    return ((Grid) this.Parent).ActualWidth;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public Thickness MiniNotchShift { get; set; }
        public Thickness MiddleNotchShift { get; set; }
        public Thickness SilverNotchShift { get; set; }
        public Thickness GoldNotchShift { get; set; }
        public double BarWidth { get; set; }
        public float SizeModifier { get; private set; }

        public double TotalWidth => ((this.SizeControlWidget.Parent as Grid)?.FindName("MonsterHealthBar") as UserControl)?.ActualWidth ?? 230;
        
        public void Update(CrownInfo crowns, float size)
        {
            this.LastSnapshot = new SizeSnapshot(crowns, size);
            
            var maxSize = Math.Max(crowns.Gold, size) * 1.05f;
            var minSize = Math.Min(crowns.Mini, size) * 0.9f;
            var alocatedSize = maxSize - minSize;

            MiniNotchShift = CreateLeftThickness((crowns.Mini - minSize) / alocatedSize);
            MiddleNotchShift = CreateLeftThickness((1 - minSize) / alocatedSize);
            SilverNotchShift = CreateLeftThickness((crowns.Silver - minSize) / alocatedSize);
            GoldNotchShift = CreateLeftThickness((crowns.Gold - minSize) / alocatedSize);
            BarWidth = TotalWidth * ((size - minSize) / alocatedSize);
            this.SizeModifier = size;
            
            InvokePropertyChanged(nameof(MiniNotchShift));
            InvokePropertyChanged(nameof(MiddleNotchShift));
            InvokePropertyChanged(nameof(SilverNotchShift));
            InvokePropertyChanged(nameof(GoldNotchShift));
            InvokePropertyChanged(nameof(BarWidth));
            InvokePropertyChanged(nameof(TotalWidth));
            InvokePropertyChanged(nameof(this.SizeModifier));
        }

        private void SetDefaultNotchValues()
        {
            MiniNotchShift = CreateLeftThickness(TotalWidth * .25);
            MiddleNotchShift = CreateLeftThickness(TotalWidth * .5);
            SilverNotchShift = CreateLeftThickness(TotalWidth * .65);
            GoldNotchShift = CreateLeftThickness(TotalWidth * .95);
            BarWidth = .25;
            this.SizeModifier = 1;
        }
        
        private Thickness CreateLeftThickness(double left) => new Thickness(TotalWidth * left, 0, 0, 0);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void InvokePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
