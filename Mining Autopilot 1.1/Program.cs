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
        public MiningAutoPilotShip ship;// = new MiningAutoPilotShip();

        public Program()
        {
            ship = new MiningAutoPilotShip(this); // configure the ship
            ship.Initialize(GridTerminalSystem, Me); // initialize the platform
            ship.Load(Storage); // Get the storage data

            ship.DockDistErr = 0.25f; // set docking distance to .25m
            ship.DockStageDist = 100f; // set the docking staging distance to 100m
            ship.GridStageDist = 100f; // set the grid staging distance to 100m
            ship.ApDErr = 5.0f; // set waypoint distance to 5m
            ship.RotAErr = 0.999999d; // set rotation angle (the cos of the angle) to .999999
            ship.RotPct = 0.75f; // set the percentage of distance to rotate when docking to 75%
            ship.GOPwr = 1.0f; // set gyro power override to 1
            ship.ShipWidth = 8; // set ship width to 8
            ship.ShaftsPerRun = 1; // set # of shafts per run to 1
            ship.GridSideLength = 20; // set grid side length to 20 (ship widths)
            ship.ReturnOnFull = true; // return to base on full inventory
            ship.MinUrReactor = 60; // minimum kg of uranium in reactors during refuel
            ship.EjectStone = true; // Eject Stone
        }

        public void Save()
        {
            string s = ""; // temp string

            s = Storage;
            ship.Save(ref s); // save the data
            Storage = s;
            Echo("save\n");
        }

        public void Main(string argument)
        {
            string s = ""; // temp string
            
            if (argument == "")
            {
                ship.Run(GridTerminalSystem); // run ship program
            }
            else
            {
                ship.Argument(argument); // deal with the argument
            }

            s = Storage;
            ship.Save(ref s);
            Storage = s;
        }
    }
}