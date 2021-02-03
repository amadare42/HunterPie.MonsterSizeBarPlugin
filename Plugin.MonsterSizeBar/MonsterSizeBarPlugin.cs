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
        private MonsterSizeControl[] sizeControls = new MonsterSizeControl[0];
        private bool ControlsInjected => this.sizeControls.Any();
        
        public void Initialize(Game context)
        {
            this.Context = context;
            this.Context.Player.OnPeaceZoneEnter += PlayerOnOnPeaceZoneEnter;
            this.Context.Player.OnPeaceZoneLeave += OnMonsterUpdate;
            
            LoadDefaultTheme();

            Action change = UpdateMonsters;
            this.debouncedOnMonsterUpdate = change.Debounce(200);
            foreach (var monster in this.Context.Monsters)
            {
                monster.OnMonsterSpawn += OnMonsterUpdate;
                monster.OnCrownChange += OnMonsterUpdate;
                
                monster.OnMonsterDespawn += OnMonsterUpdate;
                monster.OnMonsterDeath += OnMonsterUpdate;
                monster.OnMonsterCapture += OnMonsterUpdate;
            }

            if (Context.Monsters.Any(m => m.IsAlive))
            {
                UpdateMonsters();
            }
        }

        public void Unload()
        {
            // unsub
            this.Context.Player.OnPeaceZoneEnter -= PlayerOnOnPeaceZoneEnter;
            this.Context.Player.OnPeaceZoneLeave -= OnMonsterUpdate;
            
            foreach (var monster in this.Context.Monsters)
            {
                monster.OnMonsterSpawn -= OnMonsterUpdate;
                monster.OnCrownChange -= OnMonsterUpdate;
                
                monster.OnMonsterDespawn -= OnMonsterUpdate;
                monster.OnMonsterDeath -= OnMonsterUpdate;
                monster.OnMonsterCapture -= OnMonsterUpdate;
            }

            // remove injected controls
            RemoveInjectedControls();
            
            // remove theme
            var theme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(dict =>
                dict.Contains("OVERLAY_MONSTER_SIZE_BAR"));
            if (theme != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(theme);
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
            RemoveInjectedControls();
        }

        private void RemoveInjectedControls()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var control in this.sizeControls)
                {
                    try
                    {
                        ((Grid) control.Parent).Children.Remove(control);
                    }
                    catch (Exception ex)
                    {
                        Debugger.Warn("Error on removing size bar: " + ex);
                    }
                }

                // existing controls will be collected by GC
                this.sizeControls = new MonsterSizeControl[0];
            });
        }
        
        private Action debouncedOnMonsterUpdate;
        private void OnMonsterUpdate(object sender, EventArgs args) => this.debouncedOnMonsterUpdate?.Invoke();

        private void UpdateMonsters()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // not very elegant, but will work - recreate controls on every update
                if (ControlsInjected)
                {
                    RemoveInjectedControls();
                }

                CreateBars();
                UpdateBars();
            });
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
                }
            });
        }

        private void UpdateBars()
        {
            if (!this.ControlsInjected) return;
            for (var i = 0; i < this.Context.Monsters.Length; i++)
            {
                var m = this.Context.Monsters[i];
                var control = this.sizeControls[i];
                
                if (string.IsNullOrEmpty(m.Id) || !m.IsAlive)
                {
                    continue;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (MonsterData.MonstersInfo.TryGetValue(m.GameId, out var monsterInfo))
                    {
                        control.Update(monsterInfo.Crowns, m.SizeMultiplier);
                    }
                });
            }
        }

        public string Name { get; set; } = "Monster size bars plugin";
        public string Description { get; set; } = "Adds monster size bars to monster widget";
        public Game Context { get; set; }
    }
}