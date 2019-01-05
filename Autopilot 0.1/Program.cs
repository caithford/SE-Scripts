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
        // constants
        private const int directionForward = 1; // Direction constant
        private const int directionAft = 2; // Direction constant
        private const int directionPort = 4; // Direction constant
        private const int directionStarboard = 8; // Direction constant
        private const int directionDorsal = 16; // Direction constant
        private const int directionVentral = 32; // Direction constant
        private const int directionAll = 255; // Direction constant
        private const double rotationError = 0.999999d; // How close to 1.0 do we need to  be. Law of Cosines

        INIHolder _ini; // INI holder
        AutoPilot_Class ship; // The ship
        bool initialized = false; // initialization

        // ini holder class ----------------------------------------------------------------

        public class INIHolder
        {

            /// <summary>
            /// section names start with this character
            /// </summary>
            char _sectionStart = '[';
            /// <summary>
            /// section names end with this character
            /// </summary>
            char _sectionEnd = ']';
            /// <summary>
            /// comment lines start with this char
            /// </summary>
            string _CommentStart = ";";

            /// <summary>
            /// the text BEFORE any sections.  Will be saved as-is and regenerated
            /// </summary>
            string BeginningContent = "";

            /// <summary>
            /// flag to support content before any sections <see cref="BeginningContent"/>
            /// </summary>
            public bool bSupportBeginning = false;

            /// <summary>
            /// The text after the end of parsing designator
            /// </summary>
            public string EndContent = "";

            /// <summary>
            ///  line containing just this designates end of INI parsing.  All other text goes into <see cref="EndContent"/>
            /// </summary>
            string EndDesignator = "---";

            char MultLineStart = '|';

            private MyGridProgram _pg;

            private Dictionary<string, string> _Sections;
            private Dictionary<string, string[]> _Lines;
            private Dictionary<string, Dictionary<string, string>> _Keys;

            private string _sLastINI = "";

            // From Malware:
            static readonly string[] TrueValues = { "true", "yes", "on", "1" };
            const StringComparison Cmp = StringComparison.OrdinalIgnoreCase;
            const char SeparatorChar = '=';


            /// <summary>
            /// Have the sections been modified?  If so, they should be written back out/saved
            /// </summary>
            public bool IsDirty { get; private set; } = false;


            /// <summary>
            /// Constructor,  Pass MyGridProgram so it can access things like Echo()
            /// pass String to parse.
            /// </summary>
            /// <param name="pg">Allow access to things like Echo()</param>
            /// <param name="sINI">String to parse</param>
            public INIHolder(MyGridProgram pg, string sINI)
            {
                _pg = pg;
                _Sections = new Dictionary<string, string>();
                _Lines = new Dictionary<string, string[]>();
                _Keys = new Dictionary<string, Dictionary<string, string>>();

                ParseINI(sINI);
            }

            /// <summary>
            /// Re-parse string after construction
            /// </summary>
            /// <param name="sINI">String to parse</param>
            /// <returns>number of sections found</returns>
            public int ParseINI(string sINI)
            {
                // optimize if it is the same as last time..
                sINI.TrimEnd();

                if (_sLastINI == sINI)
                {
                    //                    _pg.Echo("INI:Same"); // DEBUG
                    return _Sections.Count;
                }
                //                else _pg.Echo("INI: NOT SAME"); // DEBUG


                _Sections.Clear();
                _Lines.Clear();
                _Keys.Clear();
                BeginningContent = "";
                EndContent = "";
                IsDirty = false;
                _sLastINI = sINI;

                // get an array of the all of lines
                string[] aLines = sINI.Split('\n');

                //                               _pg.Echo("INI: " + aLines.Count() + " Lines to process"); // DEBUG

                // walk through all of the lines
                for (int iLine = 0; iLine < aLines.Count(); iLine++)
                {
                    string sSection = "";
                    aLines[iLine].Trim();
                    if (aLines[iLine].StartsWith(_sectionStart.ToString()))
                    {
                        //                        _pg.Echo(iLine + ":" + aLines[iLine]); // DEBUG
                        string sName = "";
                        for (int iChar = 1; iChar < aLines[iLine].Length; iChar++)
                            if (aLines[iLine][iChar] == _sectionEnd)
                                break;
                            else
                                sName += aLines[iLine][iChar];
                        if (sName != "")
                        {
                            sSection = sName.ToUpper();
                        }
                        else continue; // malformed line?

                        iLine++;
                        string sText = "";
                        var asLines = new string[aLines.Count() - iLine]; // maximum size.
                        int iSectionLine = 0;
                        var dKeyValue = new Dictionary<string, string>();

                        for (; iLine < aLines.Count(); iLine++)
                        {
                            aLines[iLine].Trim();

                            //                       _pg.Echo(iLine+":"+aLines[iLine]); // DEBUG

                            if (
                                aLines[iLine].StartsWith(_sectionStart.ToString())
                                || aLines[iLine].StartsWith(EndDesignator)
                                )
                            {
                                iLine--;
                                break;
                            }

                            // TODO: Support Mult-line strings?
                            // TODO: Support comments

                            sText += aLines[iLine] + "\n";
                            asLines[iSectionLine++] = aLines[iLine];

                            if (aLines[iLine].Contains(SeparatorChar))
                            {
                                string[] aKeyValue = aLines[iLine].Split('=');
                                if (aKeyValue.Count() > 1)
                                {
                                    string key = aKeyValue[0];
                                    string value = "";
                                    for (int i1 = 1; i1 < aKeyValue.Count(); i1++)
                                    {
                                        value += aKeyValue[i1];
                                        if (i1 + 1 < aKeyValue.Count()) value += SeparatorChar; // untested: add back together values with multiple seperatorChar
                                    }
                                    if (value == "") // blank line
                                    {
                                        // support malware style multi-line (needs testing)
                                        /*
                                        *
                                        ;The following line is a special format which allows for multiline text in a single key:
                                        MultiLine=
                                        |The first line of the value
                                        |The second line of the value
                                        |And so on
                                        */
                                        int iMulti = iLine + 1;
                                        for (; iMulti < aKeyValue.Count(); iMulti++)
                                        {
                                            aLines[iMulti].Trim();

                                            if (aLines[iMulti].Length > 1 && aLines[iMulti][0] == MultLineStart)
                                            {
                                                value += aLines[iMulti].Substring(1).Trim() + "\n";
                                                break;
                                            }
                                        }
                                        iLine = iMulti;
                                    }
                                    dKeyValue.Add(key, value);
                                }
                            }
                            else if (aLines[iLine].StartsWith(_CommentStart))
                            {
                                // comment line... ignore for now
                            }
                        }
                        if (!_Keys.ContainsKey(sSection))
                        {
                            _Keys.Add(sSection, dKeyValue);
                            if (!_Lines.ContainsKey(sSection)) _Lines.Add(sSection, asLines);
                        }
                        else
                        {
                            // duplicate section..  Add each keyvalue to existing

                        }
                        if (!_Sections.ContainsKey(sSection))
                        {
                            _Sections.Add(sSection, sText);
                        }
                        else
                        {
                            IsDirty = true; // we have combined two sections from the source.
                        }
                    }
                    else if (aLines[iLine].StartsWith(EndDesignator))
                    {
                        iLine++;

                        // end of INI section.  Save rest of text into EndContent
                        for (; iLine < aLines.Count(); iLine++)
                        {
                            EndContent += aLines[iLine];
                        }
                    }
                    else
                    {
                        // we are before any sections. Save the text as-is
                        BeginningContent += aLines[iLine] + "\n";
                    }
                }
                return _Sections.Count;
            }

            /// <summary>
            /// Get the raw text of a specified section
            /// </summary>
            /// <param name="section">The name of the section</param>
            /// <returns>The text of the section</returns>
            public string GetSection(string section)
            {
                string sText = "";
                if (_Sections.ContainsKey(section))
                    sText = _Sections[section];
                return sText;
            }

            /// <summary>
            /// Get the parsed lines of a specified section
            /// </summary>
            /// <param name="section">The name of the section</param>
            /// <returns>string array of the lines in the section</returns>
            public string[] GetLines(string section)
            {
                string[] as1 = { "" };
                if (_Lines.ContainsKey(section))
                    as1 = _Lines[section];
                //                _pg.Echo("GetLines(" + section + ") : " + as1.Count() + " Lines");
                return as1;
            }

            /// <summary>
            /// Gets the string value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="sValue">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref string sValue, bool bSetDefault = false)
            {
                //                sValue = null;
                //                _pg.Echo(".GetValue(" + section+", " + key + ")");
                section = section.ToUpper();
                if (_Keys.ContainsKey(section)) // case sensitive
                {
                    var dValue = _Keys[section];
                    if (dValue.ContainsKey(key)) // case sensitive
                    {
                        sValue = dValue[key];
                        //                        _pg.Echo(" value=" + sValue);
                        return true;
                    }
                }
                if (bSetDefault)
                    SetValue(section, key, sValue);

                return false;
            }

            /// <summary>
            /// gets the long value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="lValue">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref long lValue, bool bSetDefault = false)
            {
                string sVal = "";

                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, lValue);
                    }
                    return false;
                }

                lValue = Convert.ToInt64(sVal);
                return true;
            }

            /// <summary>
            /// gets the long value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="iValue">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref int iValue, bool bSetDefault = false)
            {
                string sVal = "";

                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, iValue);
                    }
                    return false;
                }

                iValue = Convert.ToInt32(sVal);
                return true;
            }

            /// <summary>
            /// gets the double value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="dVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref double dVal, bool bSetDefault = false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, dVal);
                    }
                    return false;
                }

                bool pOK = double.TryParse(sVal, out dVal);
                return true;
            }

            /// <summary>
            /// gets the float value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="fVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref float fVal, bool bSetDefault = false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, fVal.ToString());
                    }
                    return false;
                }

                bool pOK = float.TryParse(sVal, out fVal);
                return true;
            }

            /// <summary>
            /// gets the DateTime value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="dtVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref DateTime dtVal, bool bSetDefault = false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, dtVal);
                    }
                    return false;
                }


                dtVal = DateTime.Parse(sVal);
                return true;
            }

            /// <summary>
            /// gets the Vector3D value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="vVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref Vector3D vVal, bool bSetDefault = false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, vVal);
                    }
                    return false;
                }

                double x1, y1, z1;
                ParseVector3d(sVal, out x1, out y1, out z1);
                vVal.X = x1;
                vVal.Y = y1;
                vVal.Z = z1;
                return true;
            }

            /// <summary>
            /// Gets the QuaternionD value of the key in the section
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case sensitive</param>
            /// <param name="qVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default if the key is not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref QuaternionD qVal, bool bSetDefault = false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, qVal);
                    }
                    return false;
                }

                double x1, y1, z1, w1;
                ParseQuaterniond(sVal, out x1, out y1, out z1, out w1);
                qVal.X = x1;
                qVal.Y = y1;
                qVal.Z = z1;
                qVal.W = w1;
                return true;
            }

            /// <summary>
            /// gets the Bool value of the key in the sectin
            /// </summary>
            /// <param name="section">the section to check. Case sensitive</param>
            /// <param name="key">the key to look for. Case Sensitive</param>
            /// <param name="bVal">the value in the key</param>
            /// <param name="bSetDefault">Optional. Set to true to set the current value as default is key not found.</param>
            /// <returns>true if the key was found. The value is unmodified if unfound</returns>
            public bool GetValue(string section, string key, ref bool bVal, bool bSetDefault = false)
            {
                string sVal = "";
                if (!GetValue(section, key, ref sVal))
                {
                    if (bSetDefault)
                    {
                        SetValue(section, key, bVal);
                    }
                    return false;
                }

                bVal = TrueValues.Any(c => string.Equals(sVal, c, Cmp)); // From Malware
                return true;
            }

            public bool SetValue(string section, string key, string sVal)
            {
                //                _pg.Echo("SetValue(" + section + "," + key + "," + sVal+")");

                // we are no longer caching direct text
                if (_Sections.ContainsKey(section))
                {
                    //                   _pg.Echo("ContainsKey(" + section + ")");
                    _Sections[section] = "";
                }
                else
                {
                    //                    _pg.Echo("addsection(" + section + ")");
                    _Sections.Add(section, "");// no cached text for now.
                    IsDirty = true;
                }
                // if there is a set of keys for the section
                if (_Keys.ContainsKey(section))
                {
                    //                                        _pg.Echo("keysContain");
                    var dKeyValue = new Dictionary<string, string>();

                    var dValue = _Keys[section];
                    if (dValue.ContainsKey(key))
                    {
                        //                        _pg.Echo("valueContains");
                        if (dValue[key] == sVal) return false;

                        dValue[key] = sVal;
                    }
                    else
                    {
                        //                        _pg.Echo("addkey");
                        dValue.Add(key, sVal);
                    }
                    IsDirty = true;
                }
                else
                { // no keys for the section
                  //               _pg.Echo("keysNoContain");
                  // add the key value dictionary and the new section
                    var dKeyValue = new Dictionary<string, string>();
                    dKeyValue.Add(key, sVal);

                    //                    _pg.Echo("keyvalueadd");
                    _Keys.Add(section, dKeyValue);


                    IsDirty = true;
                }
                //                _pg.Echo("SetValue:X");
                return true;
            }

            public bool SetValue(string section, string key, Vector3D vVal)
            {
                SetValue(section, key, Vector3DToString(vVal));
                return true;
            }
            public bool SetValue(string section, string key, bool bVal)
            {
                SetValue(section, key, bVal.ToString());
                return true;
            }
            public bool SetValue(string section, string key, int iVal)
            {
                SetValue(section, key, iVal.ToString());
                return true;
            }
            public bool SetValue(string section, string key, long lVal)
            {
                SetValue(section, key, lVal.ToString());
                return true;
            }
            public bool SetValue(string section, string key, DateTime dtVal)
            {
                SetValue(section, key, dtVal.ToString());
                return true;
            }
            public bool SetValue(string section, string key, float fVal)
            {
                SetValue(section, key, fVal.ToString());
                return true;
            }
            public bool SetValue(string section, string key, double dVal)
            {
                SetValue(section, key, dVal.ToString());
                return true;
            }

            public bool SetValue(string section, string key, QuaternionD qVal)
            {
                SetValue(section, key, QuaternionDToString(qVal));
                return true;
            }
            /// <summary>
            /// Modify the section to have the specified text.. NOTE: This will overwrite any keys.  Use either full text and lines interfaces or keys interface
            /// </summary>
            /// <param name="section">the name of the section to modify</param>
            /// <param name="sText">the text to set as the new text</param>
            public void WriteSection(string section, string sText)
            {
                sText.TrimEnd();
                section = section.ToUpper();
                if (_Sections.ContainsKey(section))
                {
                    if (_Sections[section] != sText)
                    {
                        //                        _pg.Echo("INI WriteSection: Now Dirty:"+section);
                        _Sections[section] = sText;
                        IsDirty = true;
                    }
                }
                else
                {
                    //                    _pg.Echo("INI WriteSection: Adding new Section:" + section);
                    IsDirty = true;
                    _Sections.Add(section, sText);
                }
            }

            /// <summary>
            /// Generate the full text again. This includes any modifications that have been made
            /// </summary>
            /// <param name="bClearDirty">clear the dirty flag. Use if you are writing the text back to the original location</param>
            /// <returns>full text including ALL sections and header information</returns>
            public string GenerateINI(bool bClearDirty = true)
            {
                string sIni = "";
                string s1 = BeginningContent.Trim();
                if (bSupportBeginning && s1 != "") sIni = s1 + "\n";

                //_pg.Echo("INI Generate: " + _Sections.Count() + "sections");
                foreach (var kv in _Sections)
                {
                    // TODO: if key values set, regenerate ini text from keys
                    //_pg.Echo("Section:" + kv.Key);
                    sIni += _sectionStart + kv.Key.Trim() + _sectionEnd + "\n";
                    if (kv.Value.TrimEnd() == "")
                    {
                        //_pg.Echo("Generate from keys");
                        string sSectionText = "";
                        // if raw text is cleared, regenerate from keys
                        if (_Keys.ContainsKey(kv.Key))
                        {
                            foreach (var dk in _Keys[kv.Key])
                            {
                                //_pg.Echo(" Key:" + dk.Key);
                                sSectionText += dk.Key + SeparatorChar + dk.Value + "\n";
                            }
                        }
                        sSectionText += "\n"; // add empty line at end
                        sIni += sSectionText;
                        //_pg.Echo("Set Cached Vavlue");
                        //                        _Sections[kv.Key] = sSectionText; // set cached value -- CANNOT because we are in enumeration loop
                        //_pg.Echo("Set");
                    }
                    else
                    {
                        sIni += kv.Value.Trim() + "\n\n"; // close last line + add empty line at end
                    }
                }
                if (EndContent != "")
                {
                    sIni += "\n" + EndDesignator + "\n";
                    sIni += EndContent + "\n";
                }
                if (bClearDirty)
                {
                    IsDirty = false;
                    _sLastINI = sIni;
                }
                return sIni;
            }

            /// <summary>
            /// Parses a string version of a Vector3D into double components
            /// </summary>
            /// <param name="sVector">The string to parse</param>
            /// <param name="x">The X component</param>
            /// <param name="y">The Y component</param>
            /// <param name="z">The Z component</param>
            /// <returns>True if successful, false if not</returns>
            bool ParseVector3d(string sVector, out double x, out double y, out double z)
            {
                string[] coordinates = sVector.Trim().Split(',');
                if (coordinates.Length < 3)
                {
                    coordinates = sVector.Trim().Split(':');
                }
                x = 0;
                y = 0;
                z = 0;
                if (coordinates.Length < 3) return false;

                bool xOk = double.TryParse(coordinates[0].Trim(), out x);
                bool yOk = double.TryParse(coordinates[1].Trim(), out y);
                bool zOk = double.TryParse(coordinates[2].Trim(), out z);
                if (!xOk || !yOk || !zOk)
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Converts a Vector3D to a String, rounding to two decimal places
            /// </summary>
            /// <param name="v">A vector to convert</param>
            /// <returns>The string version of the vector</returns>
            string Vector3DToString(Vector3D v)
            {
                string s;
                s = v.X.ToString("0.00") + ":" + v.Y.ToString("0.00") + ":" + v.Z.ToString("0.00");
                return s;
            }

            /// <summary>
            /// Parses a string version of a QuaternionD in to four doubles.
            /// </summary>
            /// <param name="sQuaternion">The String version of a QuaternionD</param>
            /// <param name="x">The X value of the Quaternion</param>
            /// <param name="y">The Y value of the Quaternion</param>
            /// <param name="z">The Z value of the Quaternion</param>
            /// <param name="w">The W value of the Quaternion</param>
            /// <returns>True if successful conversion, false if unsuccessful</returns>
            bool ParseQuaterniond(string sQuaternion, out double x, out double y, out double z, out double w)
            {
                string[] coordinates = sQuaternion.Trim().Split(',');
                if (coordinates.Length < 4)
                {
                    coordinates = sQuaternion.Trim().Split(':');
                }
                x = 0;
                y = 0;
                z = 0;
                w = 0;
                if (coordinates.Length < 4) return false;

                bool xOk = double.TryParse(coordinates[0].Trim(), out x);
                bool yOk = double.TryParse(coordinates[1].Trim(), out y);
                bool zOk = double.TryParse(coordinates[2].Trim(), out z);
                bool wOk = double.TryParse(coordinates[3].Trim(), out w);
                if (!xOk || !yOk || !zOk || !wOk)
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Writes a QuaternionD to a string more cleanly than the default ToString() function provided by Keen. The numbers remain unchanged.
            /// </summary>
            /// <param name="q">The Quaternion to convert to a string</param>
            /// <returns>The string version of the Quaternion</returns>
            string QuaternionDToString(QuaternionD q)
            {
                string s;
                s = q.X.ToString() + ":" + q.Y.ToString() + ":" + q.Z.ToString() + ":" + q.W.ToString();
                return s;
            }

        }


        // gyro class ----------------------------------------------------------------------

        public class Gyro_Class
        {
            /* All of the core logic in this class is based on code taken from JoeTheDestroyer
             * on the Keen forums.
             * 
             * https://forums.keenswh.com/threads/gravity-aware-rotation-solved.7376549/#post-1286908533
             * https://forums.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461
             * 
             * I have modified the code to meet some of my naming standards, but the core of it is most
             * definitely his.
             * 
             * I will take credit for the code which allows a ship to "reverse" where it keeps the same
             * up vector, but the left/right and fwd/reverse vectors are reversed.
             */

            protected QuaternionD targetRotation; // Target rotation
            protected Vector3D targetDirection; // Target Direction
            protected MatrixD tM; // Target rotation matrix
            protected MatrixD cM; // Current rotation matrix
            protected bool reset; // Has rotation been changed
            protected bool reverse; // Reverse?
            protected List<IMyTerminalBlock> gyroList; // List of gyros
            protected string mode; // what type of facing mode?
            protected INIHolder ini; // storage

            public Gyro_Class (INIHolder newIni)
            {
                targetRotation = new QuaternionD(0, 0, 0, 0); // set up memory
                targetDirection = new Vector3D(0, 0, 0); // set up memory
                tM = new MatrixD(); // set up memory
                cM = new MatrixD(); // set up memory

                reset = true; // set to default
                reverse = false; // set to default

                mode = ""; // clear the mode

                gyroList.Clear(); // clear the list to be safe

                ini = newIni; // set the ini
            } // Gyro_Class (Constructor)

            public void Initialize (List<IMyTerminalBlock> blocklist)
            {
                gyroList = blocklist;
            }

            public void EnableGyros (bool enable = true)
            {
                IMyGyro gyro = null;
                int i = 0;

                for (i = 0; i < gyroList.Count; i++)
                {
                    gyro = gyroList[i] as IMyGyro; // grab the i gyro
                    gyro.Enabled = enable; // enable the gyro per value provided
                } // for gyroList
            } // EnableGyros

            public void OverrideGyros (bool enable = true, float power = 1.0f)
            {
                IMyGyro gyro = null;
                int i = 0;

                for (i = 0; i < gyroList.Count; i++)
                {
                    gyro = gyroList[i] as IMyGyro;
                    gyro.GyroOverride = enable; // set override value
                    gyro.GyroPower = power; // set power value

                    if (!enable)
                    {
                        gyro.Yaw = 0;
                        gyro.Pitch = 0;
                        gyro.Roll = 0;
                    } // if !enable
                }
            } // OverrideGyros

            public void AllStop ()
            {

            } // AllStop

            public void SetRotation (QuaternionD newRotation, bool newReverse = false)
            {
                mode = "rotation"; // set the mode to rotation
                targetRotation = newRotation; // set the new rotation value
                reverse = newReverse; // set the new reverse value
                reset = true; // reset the calculations
            } // SetRotation (Quaternion)

            public void SetRotation (Vector3D newRotation, bool newReverse = false)
            {
                mode = "direction"; // set the mode to direction
                targetDirection = newRotation; // set the new direction value
                reverse = newReverse; // set the new reverse value
                reset = true; // reset the calculations
            } // SetRotation (Vector)

            public double Align (MatrixD currentRotation)
            {
                double dot = 0; // dot product

                cM = currentRotation; // get rotation matrix from that provided
                cM = MatrixD.Transpose(cM); // rotate the matrix into local co-ordinates

                switch (mode)
                {
                    case "rotation":
                        dot = AlignRotation();
                        break;
                    case "direction":
                        dot = AlignDirection();
                        break;
                    default:
                        dot = -1;
                        break;
                }
                return dot;
            } //Align

            public void Save ()
            {
                // YES protected QuaternionD targetRotation; // Target rotation
                // YES protected Vector3D targetDirection; // Target Direction
                // NO protected MatrixD tM; // Target rotation matrix
                // NO protected MatrixD cM; // Current rotation matrix
                // NO protected bool reset; // Has rotation been changed
                // YES protected bool reverse; // Reverse?
                // NO protected List<IMyTerminalBlock> gyroList; // List of gyros
                // YES protected string mode; // what type of facing mode?
                // NO protected INIHolder ini; // storage
                ini.SetValue("GyroControl", "targetRotation", targetRotation); // Save target rotation
                ini.SetValue("GyroControl", "targetDirection", targetDirection); // Save target direction
                ini.SetValue("GyroControl", "reverse", reverse); // Save reverse value
                ini.SetValue("GyroControl", "mode", mode); // Save mode value
            } // Save

            public void Load ()
            {
                ini.GetValue("GyroControl", "targetRotation", ref targetRotation); // Get target rotation
                ini.GetValue("GyroControl", "targetdirection", ref targetDirection); // Get target direction
                ini.GetValue("GyroControl", "reverse", ref reverse); // Get the reverse value
                ini.GetValue("GyroControl", "mode", ref mode); // Get the mode value
            } // Load

            protected double AlignRotation ()
            {
                Vector3D rotAxis = new Vector3D(0, 0, 0); // rotation axis
                double angle = 0; // rotation angle
                double fdot = 0, udot = 0; // forward vector dot product result, up vector dot product result

                if (reset) // check if recently reset
                {
                    tM = MatrixD.CreateFromQuaternion(targetRotation); // get rotation matrix from saved rotation

                    if (reverse) // if we want to maintain the up vector, but reverse heading 180 degrees
                    {
                        tM.M11 *= -1; // reverse x axis (right vector) before transforming the matrix
                        tM.M12 *= -1;
                        tM.M13 *= -1;

                        tM.M31 *= -1; // reverse z axis (forward vector) before transforming the matrix
                        tM.M32 *= -1;
                        tM.M33 *= -1;
                    } // if reverse

                    tM = MatrixD.Transpose(tM); // rotate matrix into local co-ords

                    reset = false;
                } // if reset

                GetChangeInPose(cM.Forward, cM.Up, tM.Forward, tM.Up, out rotAxis, out angle); // get axis and angle of rotation

                fdot = Vector3D.Dot(cM.Forward, tM.Forward); // forward vector dot product (cos(theta) of angle between vectors)
                udot = Vector3D.Dot(cM.Up, tM.Up); // up vector dot product (cos(theta) of angle between vectors)

                if (fdot < rotationError || udot < rotationError) // check dot products against error
                {
                    OverrideGyros(); // Enable Gyro Override mode
                    SetGyroOverrideRotation(rotAxis, angle); // Set the gyros to rotate
                } // if fdot, udot < rotationError
                else
                {
                    OverrideGyros(false); // Disable Gyro Override mode (and 0 out the manual rotation)
                } // else fdot, udot >= rotationError

                if (fdot > udot)
                    return udot; // up vector dot product is less (greater distance)
                else
                    return fdot; // forward vector dot product is less (greater distance)
            } // AlignRotation

            protected double AlignDirection ()
            {
                Vector3D rotAxis = new Vector3D(0, 0, 0); // rotation axis
                double angle = 0; // rotation angle
                double fdot = 0; // forward vector dot product result

                if (reset)
                {
                    reset = false; // reset the flag
                } // if reset

                GetChangeInDirection(cM.Forward, targetDirection, out rotAxis, out angle); // get axis and angle of rotation

                fdot = Vector3D.Dot(cM.Forward, targetDirection); // forward vector dot product (cos(theta) of angle between vectors)

                if (fdot < rotationError)
                {
                    OverrideGyros(); // Enable Gyro Override mode
                    SetGyroOverrideRotation(rotAxis, angle); // Set the gyros to rotate
                } // if fdot < rotationError
                else
                {
                    OverrideGyros(false); // Disable Gyro Override mode (and 0 out the manual rotation)
                } // else fdot >= rotationError

                return fdot; // return dot product
            } // AlignDirection

            protected void GetChangeInDirection(Vector3D cur, Vector3D targ, out Vector3D axis, out double angle)
            {
                axis = Vector3D.Cross(targ, cur); // get cross product (axis) between vectors
                angle = axis.Normalize();
                angle = Math.Atan2(angle, Math.Sqrt(Math.Max(0.0, 1.0 - angle * angle)));
            } //GetChangeInDirection()

            protected void GetChangeInPose(Vector3D cFwdV, Vector3D cUpV, Vector3D tFwdV, Vector3D tUpV, out Vector3D axis, out double angle)
            {
                Quaternion r1 = new Quaternion(0, 0, 0, 0);
                Quaternion r2 = new Quaternion(0, 0, 0, 0);
                Quaternion r3 = new Quaternion(0, 0, 0, 0);
                Vector3D rotAxis1 = new Vector3D(0, 0, 0);
                Vector3D rotAxis2 = new Vector3D(0, 0, 0);
                Vector3D v2a = new Vector3D(0, 0, 0);
                Vector3 v = new Vector3(0, 0, 0);
                float f = 0f;
                double rotAng1 = 0d;
                double rotAng2 = 0d;

                GetChangeInDirection(cFwdV, tFwdV, out rotAxis1, out rotAng1);
                r1 = Quaternion.CreateFromAxisAngle(rotAxis1, (float)rotAng1);
                v2a = Vector3D.Transform(cUpV, r1);

                GetChangeInDirection(v2a, tUpV, out rotAxis2, out rotAng2);
                r2 = Quaternion.CreateFromAxisAngle(rotAxis2, (float)rotAng2);
                r3 = r2 * r1;
                r3.GetAxisAngle(out v, out f);

                axis.X = v.X;
                axis.Y = v.Y;
                axis.Z = v.Z;
                angle = f;
            } //GetChangeInPose()

            protected void SetGyroOverrideRotation (Vector3D axis, double angle)
            {
                IMyGyro gyro = null; // used to loop through gyros
                int i = 0; // counter for looping
                float turnVelocity = 0; // used to control the rotation velocity

                gyro = gyroList[0] as IMyGyro; // grab first gyro

                turnVelocity = gyro.GetMaximum<float>("Pitch") * (float)(angle / Math.PI / 2); // convert angle to percentage of rotation
                turnVelocity = Math.Min(gyro.GetMaximum<float>("Pitch"), turnVelocity); // just in case math fails, and we attempt to turn faster than possible
                turnVelocity = (float)Math.Max(0.005f, turnVelocity);

                axis.Normalize();
                axis *= turnVelocity;

                for (i = 0; i < gyroList.Count; i++)
                {
                    gyro = gyroList[i] as IMyGyro; // cast the gyro
                    gyro.Pitch = -(float)axis.X; // set the pitch
                    gyro.Yaw = -(float)axis.Y; // set the yaw
                    gyro.Roll = -(float)axis.Z; // set the roll
                } // for i loop
            } // SetGyroOverrideRotation
        } // Gyro_Class

        // thruster class ------------------------------------------------------------------

        public class Thruster_Class
        {
            // Members ---------------------------------------------------------
            protected List<IMyTerminalBlock> fwdThrList; // list of forward facing thruster
            protected List<IMyTerminalBlock> aftThrList; // list of aft facing thruster
            protected List<IMyTerminalBlock> portThrList; // list of port facing thruster
            protected List<IMyTerminalBlock> stbdThrList; // list of stbd facing thruster
            protected List<IMyTerminalBlock> ventThrList; // list of ventral facing thruster
            protected List<IMyTerminalBlock> dorsThrList; // list of dorsal facing thruster
            public float maxFwdThr { get; protected set; } // max thrust forward
            public float maxAftThr { get; protected set; } // max thrust aft
            public float maxPortThr { get; protected set; } // max thrust port
            public float maxStbdThr { get; protected set; } // max thrust stbd
            public float maxVentThr { get; protected set; } // max thrust ventral
            public float maxDorsThr { get; protected set; } // max thrust dorsal

            public Thruster_Class ()
            {
                fwdThrList.Clear();
                aftThrList.Clear();
                portThrList.Clear();
                stbdThrList.Clear();
                ventThrList.Clear();
                dorsThrList.Clear();

                maxFwdThr = 0;
                maxAftThr = 0;
                maxPortThr = 0;
                maxStbdThr = 0;
                maxVentThr = 0;
                maxDorsThr = 0;
            } // Thruster_Class (Constructor)

            public void ConfigureThrusters (List<IMyTerminalBlock> thrusterList, IMyRemoteControl rc)
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
                    } // if dir left
                    else if (accDir == identity.Right)
                    {
                        stbdThrList.Add(thrusterList[i]); // add thruster
                    } // if dir right
                    else if (accDir == identity.Backward)
                    {
                        aftThrList.Add(thrusterList[i]); // add thruster
                    } // if dir backward
                    else if (accDir == identity.Forward)
                    {
                        fwdThrList.Add(thrusterList[i]); // add thruster
                    } // if dir forward
                    else if (accDir == identity.Up)
                    {
                        dorsThrList.Add(thrusterList[i]); // add thruster
                    } // if dir up
                    else if (accDir == identity.Down)
                    {
                        ventThrList.Add(thrusterList[i]); // add thruster
                    } // if dir down
                } // for thruster loop

                CalculateMaxThrust(); // Calculate the Maximum Thrust

            } // ConfigureThrusters

            protected void CalculateMaxThrust ()
            {
                /* Most of the logic for this function came from code that Wicorel posted on
                 * the Space Engineers released codes page.
                 * https://forum.keenswh.com/threads/thruster-code.7392871/ */
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

            } // CalculateMaxThrust

            protected void SetThrustLevel (List<IMyTerminalBlock> thrusterList, float thrustPct)
            {
                int i = 0; // loop counter
                IMyThrust thruster; // thruster access variable

                if (thrustPct < 0)
                {
                    thrustPct = 0; // minimum value
                }

                if (thrustPct > 1.0f)
                {
                    thrustPct = 1.0f; // maximum value
                }

                for (i = 0; i < thrusterList.Count; i++)
                {
                    thruster = thrusterList[i] as IMyThrust; // grab the thruster
                    //thruster.SetValue("Override", thrustPct); // set the override level
                    thruster.ThrustOverridePercentage = thrustPct; // set the override percentage
                } // for loop
            } // SetThrustLevel

            public void SetThrustAccel (float currentMass, float acceleration, int direction = 2)
            {
                float thrustPct = 0; // percentage of thrust

                if ((direction & directionForward) > 0)
                {
                    // calculate thrust percentage
                    //f=ma
                    //percent = (req force) / (total force)
                    if (acceleration == 0) // check if no acceleration
                    { thrustPct = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        thrustPct = currentMass * acceleration / maxFwdThr;
                    }

                    SetThrustLevel(fwdThrList, thrustPct); // Set Thruster Power Level
                } // if FORWARD

                if ((direction & directionAft) > 0)
                {
                    if (acceleration == 0) // check if no acceleration
                    { thrustPct = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        thrustPct = currentMass * acceleration / maxAftThr;
                    }

                    SetThrustLevel(aftThrList, thrustPct); // Set Thruster Power Level
                } // if AFT

                if ((direction & directionStarboard) > 0)
                {
                    if (acceleration == 0) // check if no acceleration
                    { thrustPct = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        thrustPct = currentMass * acceleration / maxStbdThr;
                    }

                    SetThrustLevel(stbdThrList, thrustPct); // Set Thruster Power Level
                } // if STARBOARD

                if ((direction & directionPort) > 0)
                {
                    if (acceleration == 0) // check if no acceleration
                    { thrustPct = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        thrustPct = currentMass * acceleration / maxPortThr;
                    }

                    SetThrustLevel(portThrList, thrustPct); // Set Thruster Power Level
                } // if PORT

                if ((direction & directionVentral) > 0)
                {
                    if (acceleration == 0) // check if no acceleration
                    { thrustPct = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        thrustPct = currentMass * acceleration / maxVentThr;
                    }

                    SetThrustLevel(ventThrList, thrustPct); // Set Thruster Power Level
                } // if VENTRAL

                if ((direction & directionDorsal) > 0)
                {
                    if (acceleration == 0) // check if no acceleration
                    { thrustPct = 0; } // set power to 0 (simplify calculations)
                    else
                    {
                        thrustPct = currentMass * acceleration / maxDorsThr;
                    }

                    SetThrustLevel(dorsThrList, thrustPct); // Set Thruster Power Level
                } // if DORSAL

            } // SetThrustAccel

            public void SetThrustFull (int direction = 2)
            {
                if ((direction & directionForward) > 0)
                {
                    SetThrustLevel(fwdThrList, 1.0f); // Set Thruster Power Level
                } // if FORWARD

                if ((direction & directionAft) > 0)
                {
                    SetThrustLevel(aftThrList, 1.0f); // Set Thruster Power Level
                } // if AFT

                if ((direction & directionStarboard) > 0)
                {
                    SetThrustLevel(stbdThrList, 1.0f); // Set Thruster Power Level
                } // if STARBOARD

                if ((direction & directionPort) > 0)
                {
                    SetThrustLevel(portThrList, 1.0f); // Set Thruster Power Level
                } // if PORT

                if ((direction & directionVentral) > 0)
                {
                    SetThrustLevel(ventThrList, 1.0f); // Set Thruster Power Level
                } // if VENTRAL

                if ((direction & directionDorsal) > 0)
                {
                    SetThrustLevel(dorsThrList, 1.0f); // Set Thruster Power Level
                } // if DORSAL

            }

            public void EnableThrusters (int direction, bool enable = true)
            {
                int i = 0; // counter
                IMyThrust thruster = null; // thruster variable

                if ((direction & directionForward) > 0)
                {
                    for (i = 0; i < fwdThrList.Count; i++)
                    {
                        thruster = fwdThrList[i] as IMyThrust;
                        thruster.Enabled = enable;
                    }
                } // if forward

                if ((direction & directionAft) > 0)
                {
                    for (i = 0; i < aftThrList.Count; i++)
                    {
                        thruster = aftThrList[i] as IMyThrust;
                        thruster.Enabled = enable;
                    }
                } // if aft

                if ((direction & directionPort) > 0)
                {
                    for (i = 0; i < portThrList.Count; i++)
                    {
                        thruster = portThrList[i] as IMyThrust;
                        thruster.Enabled = enable;
                    }
                } // if port

                if ((direction & directionStarboard) > 0)
                {
                    for (i = 0; i < stbdThrList.Count; i++)
                    {
                        thruster = stbdThrList[i] as IMyThrust;
                        thruster.Enabled = enable;
                    }
                } // if starboard

                if ((direction & directionDorsal) > 0)
                {
                    for (i = 0; i < dorsThrList.Count; i++)
                    {
                        thruster = dorsThrList[i] as IMyThrust;
                        thruster.Enabled = enable;
                    }
                } // if dorsal

                if ((direction & directionVentral) > 0)
                {
                    for (i = 0; i < ventThrList.Count; i++)
                    {
                        thruster = ventThrList[i] as IMyThrust;
                        thruster.Enabled = enable;
                    }
                } // if ventral
            } // EnableThrusters

            public void AllStop ()
            {
                EnableThrusters(directionAll); // ensure all thrusters are enabled
                SetThrustAccel(1, 0, directionAll); // stop all thrusters
            } // AllStop
        } // Thruster_Class

        // auto pilot class ----------------------------------------------------------------

        public class AutoPilot_Class
        {
            protected double maxSpeed; // Maximum Autopilot Speed

            protected MyGridProgram _pgm; // reference to grid program
            protected INIHolder _ini; // refrence to the INI
            protected Gyro_Class _gyroControl; // Gyro Control
            protected Thruster_Class _thrusterControl; // Thruster Control
            protected IMyRemoteControl _rc; // Remote Control

            protected Vector3D tPos; // Target Position
            protected Vector3D tDir; // Target Direction (Facing)
            protected QuaternionD tRot; // Target Rotation (Alignment)
            protected string mode1; // mode1 - primary mode
            protected string mode2; // mode2 - sub mode
            protected double decelTime; // time to deceleration
            protected double coastTime; // time to coast (stop accel)
            protected double clock; // clock variable... keeping time
            
            // mining behavior
            // grinding behavior?
            // welding behavior?
            // docking behavior
            // travel behavior - accelerate, coast, deccelerate, avoidance


            // constructor ----------------------------------------------------------------

            public AutoPilot_Class (MyGridProgram pgm, INIHolder ini)
            {
                _pgm = pgm; // grab the program reference
                _ini = ini; // grab the ini reference
                _gyroControl = new Gyro_Class(_ini); // configure Gyro memory
                _thrusterControl = new Thruster_Class(); // configure thruster memory
                tPos = new Vector3D(0, 0, 0); // configure target position memory

                maxSpeed = 50.0d; // set to 50 m/s
            } // AutoPilot_Class (Constructor)

            // methods --------------------------------------------------------------------

            public void Initialize()
            {
                List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();

                _pgm.GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(blockList);
                // need a null check and loop here
                _rc = blockList[0] as IMyRemoteControl;
                _pgm.GridTerminalSystem.GetBlocksOfType<IMyGyro>(blockList);
                // need a null check
                _gyroControl.Initialize(blockList);
                _pgm.GridTerminalSystem.GetBlocksOfType<IMyThrust>(blockList);
                // need a null check
                _thrusterControl.ConfigureThrusters(blockList, _rc);
            }

            public void SetTravelDestination (Vector3D d)
            {
                mode1 = "Travel"; // set primary mode
                mode2 = "Prereq"; // set secondary mode
                tPos = d; // set destination
            } // SetTravelDestination

            public void Travel ()
            {
                double a = 0; // acceleration
                double t = 0; // time
                double d1 = 0; // distance1
                double d2 = 0; // distance2
                // loop logic here
                switch (mode2)
                {
                    case "Prereq":
                        if (_rc.GetShipSpeed() > 0)
                        {
                            _thrusterControl.AllStop(); // Stop all thruster overrides
                            _rc.DampenersOverride = true; // Enable inertial dampeners
                            mode2 = "Prereq_Stop"; // Go to Prereq_Stop mode (monitor)
                        } // if speed > 0
                        else
                        {
                            mode2 = "Configure"; // Go to configure mode
                        } // else speed == 0
                        break;
                    case "Prereq_Stop":
                        if (_rc.GetShipSpeed() == 0)
                        {
                            mode2 = "Configure"; // Go to configure mode
                        }
                        break;
                    case "Configure":
                        tDir = tPos - _rc.GetPosition(); // Get facing vector
                        _gyroControl.SetRotation(tDir); // Set facing direction in Gyro's
                        d1 = Vector3D.Distance(tPos, _rc.GetPosition()); // calculate distance
                        a = _thrusterControl.maxAftThr / _rc.CalculateShipMass().TotalMass; // calculate maximum accel f=ma a=f/m
                        t = maxSpeed / a; // calculate time to accel to max speed t=v/a
                        d2 = /*0 + 0 * t +*/ a * t * t / 2; // calculate accel distance x = x0 + vx0*t+1/2at^2
                        // check, is total distance >= 2 * accel distance
                        if (d1 > d2 * 2)
                        {
                            // set coast time = t
                            coastTime = t; // set the coast time point to the time required to accelerate to the max speed
                            // calculate coast distance (d1 - (d2 * 2))
                            // calculate total coast time d = r/t dt = r t = r/d  t = maxspeed / (d1 - (d2 * 2)
                            decelTime = t + (maxSpeed) / (d1 - (d2 * 2)); // calculate the time to set deceleration
                            // set decel time = t + coast time
                        }
                        else
                        {
                            coastTime = 0; // no coasting time
                            // distance accel and decel is d1 / 2  so d1/2 = 1/2 * a * t^2
                            // d1 / 2 * 2 = 2 * 1/2 * a * t^2
                            // d1 = a * t^2
                            // d1/a = t^2
                            // sqrt(d1/a) = t
                            // set decel time
                            decelTime = Math.Sqrt(d1 / a);
                        }
                        // calculate coast distance 
                        // calculate coast time
                        mode2 = "Face"; // next mode
                        break;
                    case "Face":
                        if (_gyroControl.Align(_rc.WorldMatrix) >= rotationError)
                        {
                            _rc.DampenersOverride = false; // Disable inertial dampeners
                            mode2 = "Accel_Start"; // go to next mode
                        } // gyro alignment
                        break;
                    case "Accel_Start":
                        _thrusterControl.SetThrustFull(directionAft); // enable full acceleration of thruster
                        clock = 0; // set the clock to 0
                        mode2 = "Accel_Monitor"; // switch to monitor mode
                        break;
                    case "Accel_Monitor":
                        clock += _pgm.Runtime.TimeSinceLastRun.TotalSeconds;
                        if (clock >= coastTime) // convert seconds to milliseconds
                        {
                            _rc.DampenersOverride = false; // Disable inertial dampeners
                            _thrusterControl.AllStop(); // stop thrusters
                            mode2 = "Coast"; // go to next mode
                        }
                        break;
                    case "Accel_Stop":
                        break;
                    case "Coast":
                        clock += _pgm.Runtime.TimeSinceLastRun.TotalSeconds;
                        if (clock >= decelTime)
                        {
                            _rc.DampenersOverride = true; // Enable inertial dampeners
                            mode2 = "Brake"; // go to next mode
                        }
                        break;
                    case "Brake":
                        if (_rc.GetShipSpeed() == 0)
                        {
                            mode1 = "Idle";
                            mode2 = "Done";
                        }
                        break;
                    default:
                        break;
                } // switch (mode2)
            } // Travel

            public void Run ()
            {
                if (mode1 == "Travel")
                {
                    Travel();
                }
            } // Run
        } // AutoPilot_Class


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        } // Program() constructor

        public void Save()
        {
            Storage = _ini.GenerateINI(); // save the INI
        } // Save() save routine

        public void Main(string argument, UpdateType updateSource)
        {
            Vector3D pointA = new Vector3D(-609, -1117, 840);
            Vector3D pointB = new Vector3D(-1650, 1955, 2472);

            if (!initialized)
            {
                _ini = new INIHolder(this, Storage);
                ship = new AutoPilot_Class(this, _ini);
                ship.Initialize();
                initialized = true;
            }
            else
            {

                _ini.ParseINI(Storage); // get storage variable

                if ((updateSource & UpdateType.Terminal) != 0)
                {
                    if (argument.Contains("starta"))
                    {
                        ship.SetTravelDestination(pointA);
                    }
                    if (argument.Contains("startb"))
                    {
                        ship.SetTravelDestination(pointB);
                    }
                }

                if ((updateSource & UpdateType.Update10) > 0)
                {
                    ship.Run();
                }
            }
        } // MAIN
    }
}