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
        public class ThrusterControl
        {
            // "Constant" Members ----------------------------------------------
            private const int directionForward = 1; // Direction constant
            private const int directionAft = 2; // Direction constant
            private const int directionPort = 4; // Direction constant
            private const int directionStarboard = 8; // Direction constant
            private const int directionDorsal = 16; // Direction constant
            private const int directionVentral = 32; // Direction constant
            private const int directionAll = 255; // Direction constant

            // Members ---------------------------------------------------------
            List<IMyTerminalBlock> fwdThrList; // list of forward facing thruster
            List<IMyTerminalBlock> aftThrList; // list of aft facing thruster
            List<IMyTerminalBlock> portThrList; // list of port facing thruster
            List<IMyTerminalBlock> stbdThrList; // list of stbd facing thruster
            List<IMyTerminalBlock> ventThrList; // list of ventral facing thruster
            List<IMyTerminalBlock> dorsThrList; // list of dorsal facing thruster
            float maxFwdThr; // max thrust forward
            float maxAftThr; // max thrust aft
            float maxPortThr; // max thrust port
            float maxStbdThr; // max thrust stbd
            float maxVentThr; // max thrust ventral
            float maxDorsThr; // max thrust dorsal

            // "Constant" Properties -------------------------------------------
            public int FORWARD
            { get { return directionForward; } }

            public int AFT
            { get { return directionAft; } }

            public int PORT
            { get { return directionPort; } }

            public int STARBOARD
            { get { return directionStarboard; } }

            public int DORSAL
            { get { return directionDorsal; } }

            public int VENTRAL
            { get { return directionVentral; } }

            public int ALL
            { get { return directionAll; } } 

            // Properties ------------------------------------------------------

            // Constructor -----------------------------------------------------
            public ThrusterControl ()
            {
                fwdThrList = new List<IMyTerminalBlock>(); // allocate memory
                aftThrList = new List<IMyTerminalBlock>(); // allocate memory
                portThrList = new List<IMyTerminalBlock>(); // allocate memory
                stbdThrList = new List<IMyTerminalBlock>(); // allocate memory
                ventThrList = new List<IMyTerminalBlock>(); // allocate memory
                dorsThrList = new List<IMyTerminalBlock>(); // allocate memory

                maxFwdThr = 0; // reset value to 0
                maxAftThr = 0; // reset value to 0
                maxPortThr = 0; // reset value to 0
                maxStbdThr = 0; // reset value to 0
                maxVentThr = 0; // reset value to 0
                maxDorsThr = 0; // reset value to 0

            } // ThrusterControl constructor

            // Public Methods --------------------------------------------------
            public void Initialize (List<IMyTerminalBlock> thrusterList, IMyRemoteControl rc)
            {
                /* Most of the logic for this function came from code that Wicorel posted on
                 * the Space Engineers released codes page.
                 * https://forum.keenswh.com/threads/thruster-code.7392871/ */
                int i = 0; // loop counter
                Vector3 accDir; // acceleration direction vector
                Matrix gridRef; // grid reference matrix
                Matrix thrRef; // thruster reference matrix
                Matrix identity = new Matrix(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1); // identity matrix
                IMyThrust thruster; // thruster variable

                fwdThrList.Clear(); // clear the list to be safe
                aftThrList.Clear(); // clear the list to be safe
                portThrList.Clear(); // clear the list to be safe
                stbdThrList.Clear(); // clear the list to be safe
                ventThrList.Clear(); // clear the list to be safe
                dorsThrList.Clear(); // clear the list to be safe

                maxFwdThr = 0; // reset value to 0
                maxAftThr = 0; // reset value to 0
                maxPortThr = 0; // reset value to 0
                maxStbdThr = 0; // reset value to 0
                maxVentThr = 0; // reset value to 0
                maxDorsThr = 0; // reset value to 0

                rc.Orientation.GetMatrix(out gridRef); // get the RC orientation matrix
                Matrix.Transpose(ref gridRef, out gridRef); // transpose the matrix (orthoganal, convert global to local)

                for (i = 0; i < thrusterList.Count; i++)
                {
                    thruster = thrusterList[i] as IMyThrust; // get the thruster
                    thruster.Orientation.GetMatrix(out thrRef); // get thruster reference matrix
                    accDir = Vector3.Transform(thrRef.Backward, gridRef); // get acceleration direction

                    if (accDir == identity.Left)
                    {
                        portThrList.Add(thrusterList[i]); // add thruster
                        maxPortThr += thruster.MaxEffectiveThrust; // add thrust
                    } // if dir left
                    else if (accDir == identity.Right)
                    {
                        stbdThrList.Add(thrusterList[i]); // add thruster
                        maxStbdThr += thruster.MaxEffectiveThrust; // add thrust
                    } // if dir right
                    else if (accDir == identity.Backward)
                    {
                        aftThrList.Add(thrusterList[i]); // add thruster
                        maxAftThr += thruster.MaxEffectiveThrust; // add thrust
                    } // if dir backward
                    else if (accDir == identity.Forward)
                    {
                        fwdThrList.Add(thrusterList[i]); // add thruster
                        maxFwdThr += thruster.MaxEffectiveThrust; // add thrust
                    } // if dir forward
                    else if (accDir == identity.Up)
                    {
                        dorsThrList.Add(thrusterList[i]); // add thruster
                        maxDorsThr += thruster.MaxEffectiveThrust; // add thrust
                    } // if dir up
                    else if (accDir == identity.Down)
                    {
                        ventThrList.Add(thrusterList[i]); // add thruster
                        maxVentThr += thruster.MaxEffectiveThrust; // add thrust
                    } // if dir down
                } // for thruster loop
            } // Initialize method

            public void Recalculate ()
            {
                int i = 0; // loop counter
                IMyThrust thruster; // thruster pointer

                maxFwdThr = 0; // reset value to 0
                maxAftThr = 0; // reset value to 0
                maxPortThr = 0; // reset value to 0
                maxStbdThr = 0; // reset value to 0
                maxVentThr = 0; // reset value to 0
                maxDorsThr = 0; // reset value to 0

                for (i = 0; i < fwdThrList.Count; i++)
                {
                    thruster = fwdThrList[i] as IMyThrust; // grab the thruster
                    maxFwdThr += thruster.MaxEffectiveThrust; // update the thrust
                }

                for (i = 0; i < aftThrList.Count; i++)
                {
                    thruster = aftThrList[i] as IMyThrust; // grab the thruster
                    maxFwdThr += thruster.MaxEffectiveThrust; // update the thrust
                }

                for (i = 0; i < portThrList.Count; i++)
                {
                    thruster = portThrList[i] as IMyThrust; // grab the thruster
                    maxFwdThr += thruster.MaxEffectiveThrust; // update the thrust
                }

                for (i = 0; i < stbdThrList.Count; i++)
                {
                    thruster = stbdThrList[i] as IMyThrust; // grab the thruster
                    maxFwdThr += thruster.MaxEffectiveThrust; // update the thrust
                }

                for (i = 0; i < ventThrList.Count; i++)
                {
                    thruster = ventThrList[i] as IMyThrust; // grab the thruster
                    maxFwdThr += thruster.MaxEffectiveThrust; // update the thrust
                }

                for (i = 0; i < dorsThrList.Count; i++)
                {
                    thruster = dorsThrList[i] as IMyThrust; // grab the thruster
                    maxFwdThr += thruster.MaxEffectiveThrust; // update the thrust
                }
            } // Recalculate method

            public void SetThrust (IMyRemoteControl rc, int direction, float accel)
            {
                float power = 0; // thruster power level

                if ((direction & FORWARD) > 0)
                {
                    // calculate thrust percentage
                    //f=ma
                    //percent = (req force) / (total force)
                    if (accel == 0) // check if no acceleration
                    { power = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        power = rc.CalculateShipMass().TotalMass * accel / maxFwdThr;
                        power *= 99;
                        power += 1;
                        //power *= 100;
                    }
                    

                    // set thrusters
                    PowerThruster(fwdThrList, power); // Set Thruster Power Level
                } // if FORWARD

                if ((direction & AFT) > 0)
                {
                    if (accel == 0) // check if no acceleration
                    { power = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        power = rc.CalculateShipMass().TotalMass * accel / maxFwdThr;
                        power *= 99;
                        power += 1;
                        //power *= 100;
                    }

                    PowerThruster(aftThrList, power); // Set Thruster Power Level
                } // if AFT

                if ((direction & STARBOARD) > 0)
                {
                    if (accel == 0) // check if no acceleration
                    { power = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        power = rc.CalculateShipMass().TotalMass * accel / maxFwdThr;
                        power *= 99;
                        power += 1;
                        //power *= 100;
                    }

                    PowerThruster(stbdThrList, power); // Set Thruster Power Level
                } // if STARBOARD

                if ((direction & PORT) > 0)
                {
                    if (accel == 0) // check if no acceleration
                    { power = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        power = rc.CalculateShipMass().TotalMass * accel / maxFwdThr;
                        power *= 99;
                        power += 1;
                        //power *= 100;
                    }

                    PowerThruster(portThrList, power); // Set Thruster Power Level
                } // if PORT

                if ((direction & VENTRAL) > 0)
                {
                    if (accel == 0) // check if no acceleration
                    { power = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        power = rc.CalculateShipMass().TotalMass * accel / maxFwdThr;
                        power *= 99;
                        power += 1;
                        //power *= 100;
                    }

                    PowerThruster(ventThrList, power); // Set Thruster Power Level
                } // if VENTRAL

                if ((direction & DORSAL) > 0)
                {
                    if (accel == 0) // check if no acceleration
                    { power = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        power = rc.CalculateShipMass().TotalMass * accel / maxFwdThr;
                        power *= 99;
                        power += 1;
                        //power *= 100;
                    }

                    PowerThruster(dorsThrList, power); // Set Thruster Power Level
                } // if DORSAL
            } // SetThrust method

            public void Stop ()
            {
                SetThrust(null, directionAll, 0);
            } // Stop method

            public void EnableThrusters ()
            {

            } // EnableThrusters method

            public void DisableThrusters()
            {

            } // DisableThrusters method

            // Supporting Methods ----------------------------------------------
            protected void PowerThruster (List<IMyTerminalBlock> thrusterList, float power)
            {
                int i = 0; // loop counter
                IMyThrust thruster; // thruster access variable

                if ((power < 1.0000001f) && (power != 0))
                {
                    power = 1.0000001f; // minimum value
                }

                if (power > 100.0f)
                {
                    power = 100.0f; // maximum value
                }

                for (i = 0; i < thrusterList.Count; i++)
                {
                    thruster = thrusterList[i] as IMyThrust; // grab the thruster
                    thruster.SetValue("Override", power); // set the override level
                } // for loop
            } // PowerThrust method
        } // ThrusterControl class
    } // Program partial class
} // Ingamescript namespace
