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
        /// BatteryControl class, grabs all batteries on the grid and provides some control and configuration interface for them.
        /// </summary>
        public class BatteryControl
        {
            const string LCDStaticTitle = "Battery Status";
            const string LCDStaticText = "Hotkey 8\n--------------------\nBackup Power\n\nBattery Status\n";
            const string LCDStaticTextOff = "Offline";
            const string LCDStaticTextRecharge = "Recharge";
            const string LCDStaticTextDischarge = "Discharge";
            const string LCDStaticTextAuto = "Auto";
            const string LCDStaticTextSemiAuto = "Semi-Auto";
            const string cockpitLCDCustomDataTag = "BatteryCockpitLCD";

            protected Program _pgm; // this programmable block interface
            protected List<IMyBatteryBlock> batteryList; // list of reactors
            protected List<IMyTextPanel> cockpitLCDList; // list of cockpit LCD's
            protected int init; // initialization variable
            protected bool enabled; // battery enabled status
            protected bool recharge; // battery recharge status
            protected bool discharge; // battery discharge status
            protected bool semiauto; // battery semi-auto status
            protected string batteryMode; // battery mode (in text)

            /// <summary>
            /// BatteryControl constructor. Requires reference to the parent Program
            /// </summary>
            /// <param name="_program">The parent program (this)</param>
            public BatteryControl(Program _program)
            {
                _pgm = _program; // set the program wrapper
                init = 0;
            } // ReactorControl Constructor

            /// <summary>
            /// Method to turn off the batteries
            /// </summary>
            public void Off()
            {
                if (init == -1)
                {
                    enabled = false;
                    SetBatteryOff(); // set battery mode to off
                } // if init == -1
                else
                {
                    _pgm.Echo("BatteryControl: Class not initialized.\n");
                } // else init == -1
            } // Off ()

            /// <summary>
            ///  Method to set the batteries to Recharge
            /// </summary>
            public void Recharge()
            {
                if (init == -1)
                {
                    enabled = false;
                    SetBatteryRecharge();
                } // if init == -1
                else
                {
                    _pgm.Echo("BatteryControl: Class not initialized.\n");
                } // else init == -1
            } // Recharge ()

            /// <summary>
            /// Method to set the batteries to Discharge
            /// </summary>
            public void Discharge()
            {
                if (init == -1)
                {
                    enabled = false;
                    SetBatteryDischarge(); // set battery mode to Discharge
                } // if init == -1
                else
                {
                    _pgm.Echo("BatteryControl: Class not initialized.\n");
                } // else init == -1
            } // Discharge ()

            /// <summary>
            /// Method to set the batteries to Auto
            /// </summary>
            public void Auto()
            {
                if (init == -1)
                {
                    enabled = false;
                    SetBatteryAuto(); // Set Battery Mode to Auto
                } // if init == -1
                else
                {
                    _pgm.Echo("BatteryControl: Class not initialized.\n");
                } // else init == -1
            } // Auto ()

            /// <summary>
            /// Method to set the batteries to Semi-Auto
            /// </summary>
            public void SemiAuto ()
            {
                if (init == -1)
                {
                    enabled = false;
                    SetBatterySemiAuto(); // set mode to Semi-Auto
                } // if init == -1
                else
                {
                    _pgm.Echo("BatteryControl: Class not initialized.\n");
                } // else init == -1
            } // SemiAuto ()

            /// <summary>
            /// Method to toggle the batteries between Recharge and Discharge
            /// </summary>
            public void ToggleRD ()
            {
                if (init == -1)
                {
                    if (batteryMode == "Recharge")
                    {
                        SetBatteryDischarge(); // set battery mode to Discharge
                    } // if batteryMode == "Recharge"
                    else
                    {
                        SetBatteryRecharge(); // set battery mode to Recharge
                    } // else batteryMode == "Recharge"
                } // if init == -1
                else
                {
                    _pgm.Echo("BatteryControl: Class not initialized.\n");
                } // else init == -1
            } // ToggleRD ()

            /// <summary>
            /// Method to toggle the batteries between Auto and Off
            /// </summary>
            public void ToggleAO ()
            {
                if (init == -1)
                {
                    if (batteryMode == "Auto")
                    {
                        SetBatteryOff(); // set battery mode to off
                    } // if batteryMode == "Auto"
                    else
                    {
                        SetBatteryAuto(); // set battery mode to auto
                    } // else batteryMode == "Auto"
                } // if init == -1
                else
                {
                    _pgm.Echo("BatteryControl: Class not initialized.\n");
                } // else init == -1
            } // ToggleAO ()

            /// <summary>
            /// Returns the currently saved status
            /// </summary>
            /// <returns>A string with the current status of the batteries</returns>
            public string GetStatus()
            {
                return batteryMode; // return the battery mode
            } // GetStatus()

            /// <summary>
            /// Method to initialize the class. Should be run as part of main program initialization sequence
            /// </summary>
            /// <returns>Returns true once the process is completed.</returns>
            public bool Initialize()
            {
                switch (init)
                {
                    case 0:
                        Init0();
                        break;
                    case 1:
                        Init1();
                        break;
                    case 2:
                        Init2();
                        break;
                    case 3:
                        Init3();
                        break;
                    case 4:
                        init = -1;
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
            /// Init phase 0
            /// </summary>
            protected void Init0()
            {
                batteryList = new List<IMyBatteryBlock>(); // allocate memory
                cockpitLCDList = new List<IMyTextPanel>(); // allocate memory

                batteryList.Clear(); // clear the list
                cockpitLCDList.Clear(); // clear the list
                init++; // next init phase
            } // Init0

            /// <summary>
            /// Init phase 1
            /// </summary>
            protected void Init1()
            {
                List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>(); // list variable to grab blocks
                int i = 0; // loop counter

                blockList.Clear(); // clear the list

                _pgm.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(blockList, b => b.IsSameConstructAs(_pgm.Me)); // grab all batteries in construct

                for (i = 0; i < blockList.Count; i++) // loop the blocks
                {
                    batteryList.Add(blockList[i] as IMyBatteryBlock); // add reactors to the list
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
            /// Init phase 2
            /// </summary>
            protected void Init2()
            {
                // detect battery settings
                if (batteryList.Count > 0)
                {
                    enabled = batteryList[0].Enabled; // get enabled status of first battery
                    recharge = batteryList[0].OnlyRecharge; // get recharge status of first battery
                    discharge = batteryList[0].OnlyDischarge; // get discharge status of first battery
                    semiauto = batteryList[0].SemiautoEnabled; // get semiauto status of first battery

                    if (!enabled)
                    {
                        batteryMode = "Off";
                    } // if !enabled
                    else if (semiauto)
                    {
                        batteryMode = "SemiAuto";
                    } // else if semiauto
                    else if (recharge)
                    {
                        batteryMode = "Recharge";
                    } // else if recharge
                    else if (discharge)
                    {
                        batteryMode = "Discharge";
                    } // else if discharge
                    else
                    {
                        batteryMode = "Auto";
                    } // else
                } // if batteryList.Count > 0
                else
                {
                    _pgm.Echo("BatteryControl: No Batteries Found on Construct (Grid).\n");
                    batteryMode = "NotPresent";
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
                    _pgm.Echo("BatteryControl: No LCD panels found on Construct (Grid).\n");
                } // else cockpitLCDList.Count > 0

                init++; // next init phase
            } // Init2()

            /// <summary>
            /// Init phase 3
            /// </summary>
            protected void Init3()
            {
                SetBatteryMode(batteryMode); // configure reactors to be the same

                init++; // next init phase
            } // Init3()

            /// <summary>
            /// Configures the cockpit LCD with some standards (based on constants in the class)
            /// </summary>
            protected void ConfigCockpitLCD()
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
            /// Changes LCD text for "Auto" mode
            /// </summary>
            protected void SetCockpitLCDAuto()
            {
                int i = 0; // loop counter

                for (i = 0; i < cockpitLCDList.Count; i++)
                {
                    cockpitLCDList[i].BackgroundColor = Color.Black;
                    cockpitLCDList[i].FontColor = Color.Green;
                    cockpitLCDList[i].WritePublicText(LCDStaticText + LCDStaticTextAuto);
                } // for i
            } // SetCockpitLCDOn()

            /// <summary>
            /// Changes LCD text for "Off" mode
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
            /// Changes LCD text for "Recharge" mode
            /// </summary>
            protected void SetCockpitLCDRecharge()
            {
                int i = 0; // loop counter

                for (i = 0; i < cockpitLCDList.Count; i++)
                {
                    cockpitLCDList[i].BackgroundColor = Color.Black;
                    cockpitLCDList[i].FontColor = Color.Yellow;
                    cockpitLCDList[i].WritePublicText(LCDStaticText + LCDStaticTextRecharge);
                } // for i
            } // SetCockpitLCDRecharge ()

            /// <summary>
            /// Changes LCD text for "Discharge" mode
            /// </summary>
            protected void SetCockpitLCDDischarge()
            {
                int i = 0; // loop counter

                for (i = 0; i < cockpitLCDList.Count; i++)
                {
                    cockpitLCDList[i].BackgroundColor = Color.Black;
                    cockpitLCDList[i].FontColor = Color.Yellow;
                    cockpitLCDList[i].WritePublicText(LCDStaticText + LCDStaticTextDischarge);
                } // for i
            } // SetCockpitLCDDischarge ()

            /// <summary>
            /// Changes LCD text for "Semi-Auto" mode
            /// </summary>
            protected void SetCockpitLCDSemiAuto ()
            {
                int i = 0; // loop counter

                for (i = 0; i < cockpitLCDList.Count; i++)
                {
                    cockpitLCDList[i].BackgroundColor = Color.Black;
                    cockpitLCDList[i].FontColor = Color.Yellow;
                    cockpitLCDList[i].WritePublicText(LCDStaticText + LCDStaticTextSemiAuto);
                } // for i
            }

            /// <summary>
            /// Changes LCD text on to the appropriate text based on the current saved mode
            /// </summary>
            protected void SetCockpitLCD()
            {
                switch (batteryMode)
                {
                    case "Off": SetCockpitLCDOff();
                        break;
                    case "Auto": SetCockpitLCDAuto();
                        break;
                    case "SemiAuto": SetCockpitLCDSemiAuto();
                        break;
                    case "Recharge": SetCockpitLCDRecharge();
                        break;
                    case "Discharge": SetCockpitLCDDischarge();
                        break;
                } // switch batteryMode
            } // SetCockpitLCD ()

            /// <summary>
            /// Set batteries to Off, then update LCD's
            /// </summary>
            protected void SetBatteryOff ()
            {
                int i = 0; // loop counter

                for (i = 0; i < batteryList.Count; i++)
                {
                    batteryList[i].Enabled = false;
                } // for i
                batteryMode = "Off";
                SetCockpitLCD(); // update LCD
            } // SetBatteryOff ()

            /// <summary>
            /// Set batteries to Auto, then update LCD's
            /// </summary>
            protected void SetBatteryAuto ()
            {
                int i = 0; // loop counter

                for (i = 0; i < batteryList.Count; i++)
                {
                    batteryList[i].Enabled = true;
                    batteryList[i].SemiautoEnabled = false;
                    batteryList[i].OnlyDischarge = false;
                    batteryList[i].OnlyRecharge = false;
                } // for i
                batteryMode = "Auto";
                SetCockpitLCD(); // update LCD
            } // SetBatteryAuto ()

            /// <summary>
            /// Set batteries to Semi-Auto, then update LCD's
            /// </summary>
            protected void SetBatterySemiAuto ()
            {
                int i = 0; // loop counter

                for (i = 0; i < batteryList.Count; i++)
                {
                    batteryList[i].Enabled = true;
                    batteryList[i].OnlyDischarge = false;
                    batteryList[i].OnlyRecharge = false;
                    batteryList[i].SemiautoEnabled = true;
                } // for i
                batteryMode = "SemiAuto";
                SetCockpitLCD(); // update LCD
            } // SetBatterySemiAuto ()

            /// <summary>
            /// Set batteries to Discharge, then update LCD's
            /// </summary>
            protected void SetBatteryDischarge ()
            {
                int i = 0; // loop counter

                for (i = 0; i < batteryList.Count; i++)
                {
                    batteryList[i].Enabled = true;
                    batteryList[i].SemiautoEnabled = false;
                    batteryList[i].OnlyRecharge = false;
                    batteryList[i].OnlyDischarge = true;
                } // for i
                batteryMode = "Discharge";
                SetCockpitLCD(); // update LCD
            } // SetBatteryDischarge ()

            /// <summary>
            /// Set batteries to Recharge, then update LCD's
            /// </summary>
            protected void SetBatteryRecharge()
            {
                int i = 0; // loop counter

                for (i = 0; i < batteryList.Count; i++)
                {
                    batteryList[i].Enabled = true;
                    batteryList[i].SemiautoEnabled = false;
                    batteryList[i].OnlyDischarge = false;
                    batteryList[i].OnlyRecharge = true;
                } // for i
                batteryMode = "Recharge";
                SetCockpitLCD(); // update LCD
            } // SetBatteryRecharge()

            /// <summary>
            /// Set batteries to the provided mode, then update LCD's
            /// </summary>
            /// <param name="mode">String value for mode to set: "Off", "Auto", "SemiAuto", "Recharge", "Discharge"</param>
            protected void SetBatteryMode (string mode)
            {
                switch (mode)
                {
                    case "Off":
                        SetBatteryOff(); // set batteries to off
                        break;
                    case "Auto":
                        SetBatteryAuto(); // set batteries to auto
                        break;
                    case "SemiAuto":
                        SetBatterySemiAuto(); // set batteries to Semi-Auto
                        break;
                    case "Recharge":
                        SetBatteryRecharge(); // set batteries to recharge
                        break;
                    case "Discharge":
                        SetBatteryDischarge(); // set batteries to Discharge
                        break;
                } // switch mode
            } // SetBatteryMode()
        } // BatteryControl Class
    }
}
