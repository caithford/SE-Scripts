using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        /// <summary>
        /// ReactorControl Class - Grabs all reactor blocks and reactor status cockpit LCD panels and encapsulates them in
        /// and easy control interface.
        /// </summary>
        public class ReactorControl
        {
            const string LCDStaticTitle = "Reactor Status";
            const string LCDStaticText = "Hotkey 7\n--------------------\nMain Power\n\nReactor Status\n";
            const string LCDStaticTextOn = "Online";
            const string LCDStaticTextOff = "Offline";
            const string cockpitLCDCustomDataTag = "ReactorCockpitLCD";

            protected Program _pgm; // this programmable block interface
            protected List<IMyReactor> reactorList; // list of reactors
            protected List<IMyTextPanel> cockpitLCDList; // list of cockpit LCD's
            protected int init; // initialization variable
            protected bool enabled; // reactor enabled status

            /// <summary>
            /// ReactorControl Constructor
            /// </summary>
            /// <param name="_program">Parent Program (this)</param>
            public ReactorControl (Program _program)
            {
                _pgm = _program; // set the program wrapper
                init = 0;
            } // ReactorControl Constructor

            /// <summary>
            /// On - Turns on the Reactors, and sets LCD panels appropriately
            /// </summary>
            public void On ()
            {
                if (init == -1)
                {
                    enabled = true;
                    SetCockpitLCDOn();
                    SetReactorEnabled(enabled);
                } // if init == -1
                else
                {
                    _pgm.Echo("ReactorControl: Class not initialized.\n");
                } // else init == -1
            } // On ()

            /// <summary>
            /// Off - Turns off the Reactors, and sets LCD panels appropriately
            /// </summary>
            public void Off ()
            {
                if (init == -1)
                {
                    enabled = false;
                    SetCockpitLCDOff();
                    SetReactorEnabled(enabled);
                } // if init == -1
                else
                {
                    _pgm.Echo("ReactorControl: Class not initialized.\n");
                } // else init == -1
            } // Off ()

            /// <summary>
            /// Toggles - Toggles the Reactors based on current status, and updates LCD panels appropriately
            /// </summary>
            public void Toggle ()
            {
                if (init == -1)
                {
                    enabled = !enabled;
                    SetCockpitLCD(enabled);
                    SetReactorEnabled(enabled);
                } // if init == -1
                else
                {
                    _pgm.Echo("ReactorControl: Class not initialized.\n");
                } // else init == -1
            } // Toggle ()

            /// <summary>
            /// GetStatus - Returns the power on/off status of reactors
            /// </summary>
            /// <returns>True if Reactors are On, False if Reactors are Off</returns>
            public bool GetStatus ()
            {
                return enabled;
            } // GetStatus()

            /// <summary>
            /// Initialize - Grabs the appropriate blocks from the construct, and sets up the class for operation.
            /// </summary>
            /// <returns>Returns TRUE when complete.</returns>
            public bool Initialize ()
            {
                switch (init)
                {
                    case 0: Init0();
                        break;
                    case 1: Init1();
                        break;
                    case 2: Init2();
                        break;
                    case 3: Init3();
                        break;
                    case 4: init = -1;
                        break;
                } // switch init

                if (init == -1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            } // Initialize

            /// <summary>
            /// Internal phase 0 initialization - allocate memory
            /// </summary>
            protected void Init0 ()
            {
                reactorList = new List<IMyReactor>(); // allocate memory
                cockpitLCDList = new List<IMyTextPanel>(); // allocate memory

                reactorList.Clear(); // clear the list
                cockpitLCDList.Clear(); // clear the list
                init++; // next init phase
            } // Init0

            /// <summary>
            /// Internal phase 1 initialization - get blocks from construct
            /// </summary>
            protected void Init1 ()
            {
                List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>(); // list variable to grab blocks
                int i = 0; // loop counter

                blockList.Clear(); // clear the list

                _pgm.GridTerminalSystem.GetBlocksOfType<IMyReactor>(blockList, b => b.IsSameConstructAs(_pgm.Me)); // grab all reactors in construct

                for (i = 0; i < blockList.Count; i++) // loop the blocks
                {
                    reactorList.Add(blockList[i] as IMyReactor); // add reactors to the list
                } // for i

                _pgm.GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(blockList, b => b.IsSameConstructAs(_pgm.Me)); // grab all LCD panels in construct

                for (i = 0; i < blockList.Count; i++) // loop the blocks
                {
                    if (blockList[i].CustomData.Contains(cockpitLCDCustomDataTag)) // check if custom data tag is present
                    {
                        cockpitLCDList.Add(blockList[i] as IMyTextPanel); // add to the LCD list
                    } // if custom data matches
                } // for i

                init++; // next init phase
            } // Init1()

            /// <summary>
            /// Internal phase 2 initialization - detect reactor status, configure LCD's
            /// </summary>
            protected void Init2 ()
            {
                // detect reactor settings
                if (reactorList.Count > 0)
                {
                    enabled = reactorList[0].Enabled; // get enabled status of first reactor
                } // if reactorList.Count > 0
                else
                {
                    _pgm.Echo("ReactorControl: No Reactors Found on Construct (Grid).\n");
                    init = -1;
                    return;
                } // else reactorList.Count > 0
                // configure panel

                if (cockpitLCDList.Count > 0)
                {
                    ConfigCockpitLCD(); // configure LCD's
                } // if cockpitLCDList.Count > 0
                else
                {
                    _pgm.Echo("ReactorControl: No LCD panels found on Construct (Grid).\n");
                } // else cockpitLCDList.Count > 0

                init++; // next init phase
            } // Init2()

            /// <summary>
            /// Internal phase 3 initialization - normalize reactors and LCD's
            /// </summary>
            protected void Init3()
            {
                SetReactorEnabled(enabled); // configure reactors to be the same
                SetCockpitLCD(enabled); // configure LCD's to be the same

                init++; // next init phase
            } // Init3()

            /// <summary>
            /// Base cockpit LCD configuration
            /// </summary>
            protected void ConfigCockpitLCD ()
            {
                int i = 0; // loop counter

                for (i = 0; i < cockpitLCDList.Count; i++)
                {
                    cockpitLCDList[i].Enabled = true;
                    //cockpitLCDList[i].Font = "";
                    cockpitLCDList[i].BackgroundColor = Color.Black;
                    cockpitLCDList[i].FontColor = Color.Green;
                    cockpitLCDList[i].FontSize = 2.9f;
                    cockpitLCDList[i].ShowPublicTextOnScreen();
                    cockpitLCDList[i].WritePublicTitle(LCDStaticTitle);
                } // for i
            } // ConfigCockpitLCD()

            /// <summary>
            /// Change LCD text to "On" status
            /// </summary>
            protected void SetCockpitLCDOn()
            {
                int i = 0; // loop counter

                for (i = 0; i < cockpitLCDList.Count; i++)
                {
                    cockpitLCDList[i].BackgroundColor = Color.Black;
                    cockpitLCDList[i].FontColor = Color.Green;
                    cockpitLCDList[i].WritePublicText(LCDStaticText + LCDStaticTextOn);
                } // for i
            } // SetCockpitLCDOn()

            /// <summary>
            /// Change LCD text to "Off" status
            /// </summary>
            protected void SetCockpitLCDOff()
            {
                int i = 0; // loop counter

                for (i = 0; i < cockpitLCDList.Count; i++)
                {
                    cockpitLCDList[i].BackgroundColor = Color.Black;
                    cockpitLCDList[i].FontColor = Color.Red;
                    cockpitLCDList[i].WritePublicText(LCDStaticText + LCDStaticTextOff);
                } // for i
            } // SetCockpitLCDOff()

            /// <summary>
            /// Set Cockpit LCD's to a specified status
            /// </summary>
            /// <param name="onoff">True if Reactors are On, False if Reactors are Off</param>
            protected void SetCockpitLCD (bool onoff)
            {
                if (onoff)
                {
                    SetCockpitLCDOn(); // Set LCD's to display reactor on
                } // if onoff
                else
                {
                    SetCockpitLCDOff(); // Set LCD's to display reactor off
                } // else onoff
            } // SetCockpitLCD ()

            /// <summary>
            /// Set Reactors to a specified status
            /// </summary>
            /// <param name="onoff">True if Reactors are On, False if Reactors are Off</param>
            protected void SetReactorEnabled (bool onoff)
            {
                int i = 0; // loop counter

                for (i = 0; i < reactorList.Count; i++)
                {
                    reactorList[i].Enabled = onoff;
                } // for i
            } // SetReactorEnabled
        } // ReactorControl
    }
}
