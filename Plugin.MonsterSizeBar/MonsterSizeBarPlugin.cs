using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using HunterPie.Core;
using HunterPie.GUI;
using HunterPie.GUI.Widgets;
using HunterPie.Logger;
using HunterPie.Plugins;

namespace Plugin.MonsterSizeBar
{
    public class MonsterSizeBarPlugin : IPlugin
    {
        private bool controlsInjected;
        private MonsterSizeControl[] sizeControls = new MonsterSizeControl[0];
        
        public void Initialize(Game context)
        {
            this.Context = context;
            this.Context.Player.OnPeaceZoneEnter += PlayerOnOnPeaceZoneEnter;

            LoadDefaultTheme();
            
            foreach (var monster in this.Context.Monsters)
            {
                monster.OnMonsterSpawn += MonsterOnChange;
                monster.OnCrownChange += MonsterOnChange;
            }

            if (Context.Monsters.Any(m => m.IsAlive))
            {
                MonsterOnChange(null, null);
            }
        }

        private static void LoadDefaultTheme()
        {
            var themePath = Path.Combine(Path.GetDirectoryName(typeof(MonsterSizeBarPlugin).Assembly.Location), "DefaultTheme.xaml");
            if (!File.Exists(themePath))
            {
                Debugger.Error("[Monster Size Bar] DefaultTheme.xaml doesn't exist, bars will not work correctly!");
                return;
            }
            
            var reader = new XamlReader();
            using var stream = File.OpenRead(themePath);
            var themeDict = (ResourceDictionary)reader.LoadAsync(stream);
            Application.Current.Resources.MergedDictionaries.Insert(0, themeDict);
        }

        private void PlayerOnOnPeaceZoneEnter(object source, EventArgs args)
        {
            // existing controls will be collected by GC
            this.sizeControls = new MonsterSizeControl[0];
            this.controlsInjected = false;
        }

        private void MonsterOnChange(object source, EventArgs args)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!this.controlsInjected)
                {
                    CreateBars();
                }
                UpdateBars();
            });
        }

        public void Unload()
        {
            this.Context.Player.OnPeaceZoneEnter -= PlayerOnOnPeaceZoneEnter;
            foreach (var monster in this.Context.Monsters)
            {
                monster.OnMonsterSpawn -= MonsterOnChange;
                monster.OnCrownChange -= MonsterOnChange;
            }

            if (this.controlsInjected)
            {
                foreach (var control in this.sizeControls)
                {
                    ((Grid)control.Parent).Children.Remove(control);
                }

                this.sizeControls = new MonsterSizeControl[0];
            }
        }

        private void CreateBars()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var monstersWidget = (MonsterContainer) Overlay.Widgets.FirstOrDefault(w => w.GetType() == typeof(MonsterContainer));
                if (monstersWidget == null)
                {
                    Debugger.Error("Cannot add monster size bars!");
                    return;
                }

                try
                {
                    var monstersHealths = new[]
                    {
                        typeof(MonsterContainer)
                            .GetField("f_MonsterWidget", BindingFlags.NonPublic | BindingFlags.Instance)
                            .GetValue(monstersWidget) as MonsterHealth,
                        typeof(MonsterContainer)
                            .GetField("s_MonsterWidget", BindingFlags.NonPublic | BindingFlags.Instance)
                            .GetValue(monstersWidget) as MonsterHealth,
                        typeof(MonsterContainer)
                            .GetField("t_MonsterWidget", BindingFlags.NonPublic | BindingFlags.Instance)
                            .GetValue(monstersWidget) as MonsterHealth
                    };

                    var containers = monstersHealths.Select(h =>
                            typeof(MonsterHealth).GetField("MonsterHealthContainer",
                                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(h) as Grid)
                        .ToArray();

                    this.sizeControls = containers.Select(c =>
                    {
                        var sizeControl = new MonsterSizeControl();
                        // add sizes after HP control so it will be on top
                        c.Children.Insert(1, sizeControl);
                        return sizeControl;
                    }).ToArray();
                }
                catch (Exception ex)
                {
                    Debugger.Error("Cannot add size bars! " + ex);
                    return;
                }

                this.controlsInjected = true;
            });
        }

        private void UpdateBars()
        {
            if (!this.controlsInjected) return;
            for (var i = 0; i < this.Context.Monsters.Length; i++)
            {
                var m = this.Context.Monsters[i];
                var control = this.sizeControls[i];

                Application.Current.Dispatcher.Invoke(() =>
                    control.Update(MonsterData.MonstersInfo[m.GameId].Crowns, m.SizeMultiplier));
            }
        }

        public string Name { get; set; } = "Monster size bars plugin";
        public string Description { get; set; } = "Adds monster size bars to monster widget";
        public Game Context { get; set; }
    }
}