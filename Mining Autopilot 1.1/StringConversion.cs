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
        static public class StringConversion
        {
            static public Vector3D? Vector3DFromString (string s)
            {
                Vector3D v = new Vector3D(); // value to return
                bool conv = false; // error value
                string[] components; // components array
                int i = 0; // counter

                if (!s.Contains("{") && !s.Contains("}"))
                {
                    return null; // bail out, not right format
                }

                s = s.Remove(s.IndexOf("{"), 1); // remove the { from the string
                s = s.Remove(s.IndexOf("}"), 1); // remove the } from the string

                components = s.Split(' '); // split on the space

                for (i = 0; i < 3; i++)
                {
                    components[i] = components[i].Remove(0, 2); // remove the letter and : from the string
                } // for loop

                conv = double.TryParse(components[0], out v.X); // try to convert the X component
                conv = (conv && double.TryParse(components[1], out v.Y)); // try to convert the Y component
                conv = (conv && double.TryParse(components[2], out v.Z)); // try to convert the Z component

                if (conv)
                {
                    return v; // conversion succeeded
                }
                else
                {
                    return null; // conversion failed (somewhere)
                }

            } // Vector3DFromString

            static public QuaternionD? QuaternionDFromString (string s)
            {
                QuaternionD q = new QuaternionD(); // value to return
                bool conv = false; // error value
                string[] components; // components array
                int i = 0; // counter

                if (!s.Contains("{") && !s.Contains("}"))
                {
                    return null; // bail out, format is wrong
                }

                s = s.Remove(s.IndexOf("{"), 1); // remove the { from the string
                s = s.Remove(s.IndexOf("}"), 1); // remove the } from the string

                components = s.Split(' '); // split on the space

                for (i = 0; i < 4; i++)
                {
                    components[i] = components[i].Remove(0, 2); // remove the letter and :
                } // for loop

                conv = double.TryParse(components[0], out q.X); // try to convert the X component
                conv = (conv && double.TryParse(components[1], out q.Y)); // try to convert the Y component
                conv = (conv && double.TryParse(components[2], out q.Z)); // try to convert the Z component
                conv = (conv && double.TryParse(components[3], out q.W)); // try to convert the Z component

                if (conv)
                {
                    return q; // conversion succeeded
                }
                else
                {
                    return null; // conversion failed
                }
            } // QuaternionDFromString
        } // StringConversion class
    } // Program partial class
} // IngameScript namespace
