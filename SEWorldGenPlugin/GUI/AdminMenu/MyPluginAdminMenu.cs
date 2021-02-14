﻿using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SEWorldGenPlugin.Generator.AsteroidObjects;
using SEWorldGenPlugin.Session;
using SEWorldGenPlugin.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRageMath;

namespace SEWorldGenPlugin.GUI
{
    public partial class MyPluginAdminMenu : MyGuiScreenAdminMenu
    {
        private static readonly float MARGIN_VERT = 25f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y;

        private float m_usableWidth;

        private bool m_pluginInstalled = false;

        private int m_selectedMenuIndex = 0;

        private List<MyAbstractAsteroidObjectProvider> m_asteroidProviders;

        private bool m_isRecreating = false;

        private bool m_shouldRecreate = false;

        /// <summary>
        /// Set to true, to trigger a recreate after the current recreate has finished.
        /// Cant be set to false, if it is already true.
        /// </summary>
        public bool ShouldRecreate
        {
            get 
            {
                return m_shouldRecreate;
            }
            set
            {
                m_shouldRecreate = m_shouldRecreate || value;
            }
        }

        public MyPluginAdminMenu() : base()
        {
            m_asteroidProviders = new List<MyAbstractAsteroidObjectProvider>();

            foreach(var provider in MyAsteroidObjectsManager.Static.AsteroidObjectProviders)
            {
                if (provider.Value.GetAdminMenuCreator() != null)
                    m_asteroidProviders.Add(provider.Value);
            }

            m_isRecreating = false;
            m_shouldRecreate = false;
        }

        public override bool Draw()
        {
            bool ret = base.Draw();
            if (!m_isRecreating && m_shouldRecreate)
            {
                m_shouldRecreate = false;
                RecreateControls(false);
            }
            return ret;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_usableWidth = Size.Value.X * 0.75f;

            if (!m_pluginInstalled)
            {
                CheckPluginInstalledOnServer();
                return;
            }

            if (m_isRecreating) return;
            m_isRecreating = true;

            var comboBoxTop = GetCombo();
            int oldCount = comboBoxTop.GetItemsCount();

            if (MySession.Static.IsUserSpaceMaster(Sync.MyId) && MySession.Static.IsUserAdmin(Sync.MyId))
            {
                comboBoxTop.AddItem(oldCount, "SEWorldGenPlugin - Spawning");
                comboBoxTop.AddItem(oldCount + 1, "SEWorldGenPlugin - Editing");
            }

            MyGuiControlCombobox newCombo = AddCombo();

            for(int i = 0; i < comboBoxTop.GetItemsCount(); i++)
            {
                newCombo.AddItem(comboBoxTop.GetItemByIndex(i).Key, comboBoxTop.GetItemByIndex(i).Value);
            }
            
            newCombo.Position = comboBoxTop.Position;
            newCombo.Size = comboBoxTop.Size;
            newCombo.OriginAlign = comboBoxTop.OriginAlign;
            newCombo.SelectItemByIndex(m_selectedMenuIndex);

            Controls[Controls.IndexOf(comboBoxTop)] = newCombo;
            Controls.Remove(comboBoxTop);

            newCombo.ItemSelected += delegate
            {
                m_selectedMenuIndex = newCombo.GetSelectedIndex();
                if (newCombo.GetSelectedIndex() >= oldCount)
                {
                    RecreateControls(false);
                }
                else
                {
                    comboBoxTop.SelectItemByIndex(m_selectedMenuIndex);
                    RecreateControls(false);
                }
            };

            if (m_selectedMenuIndex == oldCount)
            {
                ClearControls();
                BuildSpawnMenu();
            }
            if (m_selectedMenuIndex == oldCount + 1)
            {
                ClearControls();
                BuildEditMenu();
            }
            m_isRecreating = false;
        }

        protected override void OnClosed()
        {
            base.OnClosed();

            foreach(var provider in m_asteroidProviders)
            {
                provider.GetAdminMenuCreator().OnAdminMenuClose();
            }
        }

        /// <summary>
        /// Clears all controls, except those before the combo box to select the current menu from.
        /// Used to rebuild the whole admin menu
        /// </summary>
        private void ClearControls()
        {
            List<MyGuiControlBase> keep = new List<MyGuiControlBase>();
            foreach (var c in Controls)
            {
                keep.Add(c);
                if (c is MyGuiControlCombobox) break;
            }
            Controls.Clear();
            foreach (var c in keep)
            {
                Controls.Add(c);
            }

        }

        /// <summary>
        /// Checks if the plugin is installed on the server
        /// </summary>
        private void CheckPluginInstalledOnServer()
        {
            if (!m_pluginInstalled)
            {
                MyNetUtil.PingServer(delegate
                {
                    m_pluginInstalled = true;
                    RecreateControls(false);
                });
            }
        }

        /// <summary>
        /// Gets the admin menu combo box from the list of controls
        /// </summary>
        /// <returns>The combo box or null if it does not exist</returns>
        private MyGuiControlCombobox GetCombo()
        {
            foreach (var c in Controls)
            {
                if (c is MyGuiControlCombobox)
                {
                    return (MyGuiControlCombobox)c;
                }
            }
            return null;
        }
    }
}
