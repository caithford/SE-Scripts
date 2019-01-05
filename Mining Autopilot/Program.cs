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
    partial class Program : MyGridProgram
    {
        // ----------------------------------------------------------------------
        // Global Constants
        const double shipWidth = 8; // Ship "radius" for grid
        const int shaftPerRun = 1; // # of shafts to drill per run
        const int gridSide = 20; // length of side of square of drilling grid (number of shafts)
        const bool returnIfFull = false; // Returns drone to dock point before shaft complete if cargo full
        VRage.MyFixedPoint uraniumMinimum = 60; // Amount of uranium to keep in the reactor


        // ----------------------------------------------------------------------
        // Global Variables

        StorageControl stg = new StorageControl(); // storage device (autopilot)
        CargoControl cargo = new CargoControl(); // cargo storage device
        GyroControl gyro = new GyroControl(); // gyro control device
        ThrusterControl thrusters = new ThrusterControl(); // thruster control device
        DrillControl drills = new DrillControl(); // drill control device
        LogControl log = new LogControl(); // logging device (debugging)
        Vector3D curPos = new Vector3D(0, 0, 0); // current position
        QuaternionD curRot = new QuaternionD(0, 0, 0, 0); // current rotation (heading)

        List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>(); // list for grabbing blocks on init

        // ----------------------------------------------------------------------
        // Methods
        public Program()
        {
            
        } // Program()

        public void Save()
        {
            
        } // Save()

        public void Main(string argument)
        {
            
            
            if (argument != "") // check for an argument
            {
                // parse the argument, get control + result
                // call the right function
                // set drill grid - here
                // set drill grid - x,y,z
                // set dock grid - here
                // set dock grid - x,y,z
                // pause
                // reset
                // return to base
                // launch
            } // if argument
            else // no argument
            {
                stg.Update(); // Update current position and rotation

                // update status LCD's

                switch (stg.Mode) // check the current mode
                {
                    case "Docked":
                        //targPos = stg.GetVecKey("cs"); // Connector Stage Point
                        RunMode_Docked();
                        break;
                    case "Undock":
                        //targPos = stg.GetVecKey("cs"); // Connector Stage Point
                        RunMode_Undock(targPos, curPos, rc);
                        break;
                    case "ToGrid":
                        //targPos = stg.GetVecKey("gs"); // Grid Stage Point
                        RunMode_ToGrid(targPos, curPos, rc);
                        break;
                    case "GridAlign":
                        //targPos = stg.GetVecKey("g"); // Grid Origin Point
                        //targRot = stg.GetQuatKey("gr"); // Grid Rotation
                        RunMode_GridAlign(targPos, curPos, targRot, curRot, gyroList, rc);
                        break;
                    case "Drill":
                        //targPos = stg.GetVecKey("g"); // Grid Origin Point
                        //targRot = stg.GetQuatKey("gr"); // Grid Rotation
                        RunMode_Drill(targPos, curPos, targRot, curRot, gyroList, rc, cargo);
                        break;
                    case "ClearShaft":
                        //targPos = stg.GetVecKey("g"); // Grid Origin Point
                        //targRot = stg.GetQuatKey("gr"); // Grid Rotation
                        RunMode_ClearShaft(targPos, curPos, targRot, curRot, gyroList, rc, cargo);
                        break;
                    case "ReturnToBase":
                        //targPos = stg.GetVecKey("cs"); // Connector Stage Point
                        RunMode_ReturnToBase(targPos, curPos, rc);
                        break;
                    case "Dock":
                        //targPos = stg.GetVecKey("c"); // Connector Point
                        //targRot = stg.GetQuatKey("cr"); // Connector Rotation
                        RunMode_Dock(targPos, curPos, targRot, curRot, gyroList, rc);
                        break;
                    default:
                        Echo("Main: Invalid Status.");
                        break;
                }

                // check mode/status
                // undock
                // clear docking area
                // travel to asteroid
                // align on drilling grid
                // drilling mode
                // back out of shaft
                // return to base
                // dock
                // maintenance
                // idle
            } // else argument
        } // Main()

        public void RunMode_Docked ()
        {
            if (stg.SubMode == 0)
            {
                stg.UnloadCargo(GridTerminalSystem); // Unload the Cargo Module
                stg.ReactorRefuel(GridTerminalSystem, uraniumMinimum); // Refuel the Reactor
                stg.SubMode = 1; // Move to next SubMode
            } // if SubMode 0

            if (stg.Cont && !stg.Pause)
            {
                stg.Mode = "Undock"; // Set Mode to Undock
                stg.SubMode = 0; // Reset SubMode
                stg.Delay(1); // Delay the Program 1 s
            } // if Cont && !Pause

        } // RunMode_Docked method

        public void RunMode_Undock ()
        {
            if (rc.IsAutoPilotEnabled == false)
            {
                UnlockConnector(shipName + connName); // Unlock Connector
                // Configure Autopilot
                rc.ClearWaypoints(); // clear waypoints
                rc.ApplyAction("DockingMode_Off").Apply(rc); // configure docking mode
                rc.ApplyAction("CollisionAvoidance_Off").Apply(rc); // configure collision avoidance
                rc.ApplyAction("Forward").Apply(rc); // configure forward direction
                rc.AddWaypoint(target, "Undock Staging Location"); // set the waypoint
                rc.GetActionWithName("AutoPilot_On").Apply(rc); // turn on the auto pilot
            }

            // Check distance to Destination
            if (Vector3D.Distance(target, current) < apDErr)
            {
                rc.GetActionWithName("AutoPilot_Off").Apply(rc); // kill the autopilot
                rc.GetActionWithName("DockingMode_Off").Apply(rc); // turn off docking mode
                rc.ClearWaypoints(); // clear the waypoints
                stg.UpdateKey("Status", "ToGrid"); // upgrade our status
            }
            debug.WritePublicText("Distance: " + Vector3D.Distance(target, current).ToString() + "\n");
            // Delay
            Delay(timerClock, 1);

        } //RunMode_Undock method
    }
}