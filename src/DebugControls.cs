﻿using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nerm.Colonization
{
    // LEARNINGS - OnGui is apparently defunct, https://forum.kerbalspaceprogram.com/index.php?/topic/151354-unity-ui-creation-tutorial/
    //   is the new and improved.

    [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
    public class DebugControls
        : MonoBehaviour
    {
        public const float WINDOW_WIDTH_DEFAULT = 100;
        public const float WINDOW_HEIGHT = 440;
        public const float HorizontalBuffer = 3;
        public const float VerticalBuffer = 3;
        public const float VerticalSpacing = 2;

        private static bool isToolbarUp = false;

        public static DebugControls Instance = null;

        public void Start()
        {
            Debug.Log("DebugControls - Start enter");
            if (!isToolbarUp)
            {
                Debug.Log("DebugControls - Initialized the toolbar");
                // TODO: Statics are bad - surely there's a way to know we've been here before?
                this.AttachToToolbar();

                // The tutorial:
                //  https://forum.kerbalspaceprogram.com/index.php?/topic/78231-application-launcher-and-mods/
                // Says we should listen to this and recreate the toolbar in reaction to it...  Doesn't
                // work (because our stuff is gone).  Maybe it'd be a good idea to, instead, set a flag
                // so that the next call to Start will restore it.

                //GameEvents.onGUIApplicationLauncherDestroyed.Add(() =>
                //{
                //    this.AttachToToolbar();
                //});
                isToolbarUp = true;
            }
            Debug.Log("DebugControls - Start exit");

            Instance = this;
        }

        private void AttachToToolbar()
        {
            Texture2D texture2D;
            if (GameDatabase.Instance.ExistsTexture("ColonizationByNerm/IFI_LS_GRN_38"))
            {
                Debug.Log("DebugControl - Using blank texture");
                texture2D = new Texture2D(38, 38, TextureFormat.ARGB32, false);
            }
            else
            {
                texture2D = GameDatabase.Instance.GetTexture("ColonizationByNerm/IFI_LS_GRN_38", true);
            }

            Debug.Assert(ApplicationLauncher.Ready, "ApplicationLauncher is not ready - can't add the toolbar button.  Is this possible, really?  If so maybe we could do it later?");
            ApplicationLauncher.Instance.AddModApplication(
                OnToggleOn, OnToggleOff, null, null, null, null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                texture2D );
        }

        private void OnToggleOn()
        {
            AddInSettings.DebugWindowIsVisible = true;
            Debug.Log("DebugControl - OnToggleOn");
        }

        private void OnToggleOff()
        {
            AddInSettings.DebugWindowIsVisible = false;
            Debug.Log("DebugControl - OnToggleOff");
        }

        private void OnHoverIn()
        {
            Debug.Log("DebugControl - OnHoverIn");
        }

        private void OnHoverOut()
        {
            Debug.Log("DebugControl - OnHoverOut");
        }

        private void OnEnable()
        {
            Debug.Log("DebugControl - OnEnable");
        }

        private void OnDisable()
        {
            Debug.Log("DebugControl - OnDisable");
        }


        //public void Awake()
        //{
        //    Debug.Log("DebugControls - Awake/Start");
        //    Debug.Log("DebugControls - Awake/Stop");
        //}

        public void OnGUI()
        {
            if (!AddInSettings.DebugWindowIsVisible)
            {
                return;
            }

            //windowPos = ClickThruBlocker.GUILayoutWindow(99977, windowPos, DebugModeDialog, "Debug modes");
            AddInSettings.DebugWindowExtent = GUI.Window(GetInstanceID(), AddInSettings.DebugWindowExtent, DebugModeDialog, "Debug modes");
        }

        private void DebugModeDialog(int windowId)
        {
            int i = 0;
            GUILayout.BeginVertical();
            foreach (string blob in new string[] { "Fiddly bits", "Naughty bits", "Crispy bits" })
            {
                GUILayout.BeginHorizontal();
                AddInSettings.DebugToggles[i] = GUILayout.Toggle(AddInSettings.DebugToggles[i], new GUIContent(blob));
                ++i;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        //private void DebugModeDialog(int windowId)
        //{
        //    Debug.Log($"DebugModeDialog {windowId}");
        //    int i = 0;
        //    float top = VerticalBuffer;
        //    foreach (string blob in new string[] { "Fiddly bits", "Naughty bits", "Crispy bits"})
        //    {
        //        var content = new GUIContent(blob);
        //        var width = this.windowPos.width - 2 * HorizontalBuffer;
        //        var height = GUI.skin.toggle.CalcHeight(content, width);
        //        var extent = new Rect(HorizontalBuffer, top, width, height);
        //        top = top + height + VerticalSpacing;

        //        if (GUI.Toggle(extent, this.settings[i], content))
        //        {
        //            this.settings[i] = !this.settings[i];
        //        }
        //        ++i;
        //    }

        //    GUI.DragWindow();
        //}
    }
}