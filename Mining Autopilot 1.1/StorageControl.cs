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
        public class StorageControl
        {
            // "Constant" Members ----------------------------------------------
            // Members ---------------------------------------------------------
            protected string data; // Data String

            // "Constant" Properties -------------------------------------------
            // Properties ------------------------------------------------------
            
            // Constructor -----------------------------------------------------
            // Public Methods --------------------------------------------------
            public void SetData (string str)
            {
                data = str; // set the string
            }

            public string GetData ()
            {
                return data; // return the string
            }

            public void ResetKeys()
            {
                data = ""; // wipe it all
            } //ResetKeys()

            // Supporting Methods ----------------------------------------------
            protected bool KeyExists(string key)
            {
                return (data.Contains(key));
            } //KeyExists()

            protected bool AddKey(string key, string value)
            {
                if (!KeyExists(key))
                {
                    data += key + ":" + value + ";";
                    return true;
                } // if !KeyExists
                else { return false; }
            } //AddKey()

            protected string GetKey(string key)
            {
                int index = 0;
                string keystring = data;

                if (KeyExists(key))
                {
                    index = keystring.IndexOf(key) + key.Length + 1;
                    return keystring.Substring(index, keystring.IndexOf(";", index) - index);
                }
                else { return ""; }
            } //GetKey()

            protected bool RemoveKey(string key)
            {
                string temp = "";
                string substr = "";
                string keystring = data;

                if (!KeyExists(key))
                { return false; }

                substr = key + ":" + GetKey(key) + ";";

                temp = keystring.Remove(keystring.IndexOf(substr), substr.Length);

                data = temp;

                return (!KeyExists(key));
            } //RemoveKey()

            protected bool UpdateKey(string key, string value)
            {
                RemoveKey(key);
                return AddKey(key, value);
            } //UpdateKey()

            protected Vector3D? GetVecKey(string key)
            {
                string val = ""; // value string

                val = GetKey(key); // get the key

                return StringConversion.Vector3DFromString(val); // return the value
            } //GetVecKey()

            protected QuaternionD? GetQuatKey(string key)
            {
                string val = ""; // value string

                val = GetKey(key); // get the key

                return StringConversion.QuaternionDFromString(val); // return the value
            } //GetQuatKey()

            protected bool UpdateVecKey(string key, Vector3D v)
            {
                bool err = false; // error return

                RemoveKey(key); // remove the key (if it exists)
                err = AddKey(key, v.ToString()); // output as a string
                return err;
            } // UpdateVecKey()

            protected bool UpdateQuatKey(string key, QuaternionD q)
            {
                bool err = false; // error return

                RemoveKey(key); // remove the key (if it exists)
                err = AddKey(key, q.ToString()); // output as a string
                return err;
            } // UpdateQuatKey()

        } // StorageControl class
    } // Program partial class
} // IngameScript namespace
