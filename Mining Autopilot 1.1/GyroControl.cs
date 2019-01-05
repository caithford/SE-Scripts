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
        public class GyroControl
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

            // "Constant" Members ----------------------------------------------
            protected double rotAErr; // rotation "error"

            // Members ---------------------------------------------------------
            protected QuaternionD targetRotation; // Target rotation
            protected MatrixD tM; // Target rotation matrix
            protected MatrixD cM; // Current rotation matrix
            protected bool reset; // Has rotation been changed
            protected bool reverse; // Reverse?
            // "Constant" Properties -------------------------------------------
            public double RotationError
            {
                set { rotAErr = value; }
            } // RotationError

            // Properties ------------------------------------------------------

            // Constructor -----------------------------------------------------
            public GyroControl ()
            {
                targetRotation = new QuaternionD(0, 0, 0, 0); // set up memory
                tM = new MatrixD(); // set up memory
                cM = new MatrixD(); // set up memory

                rotAErr = 0.999999d; // set to default

                reset = false; // set to default
                reverse = false; // set to default
            } // GyroControl constructor

            // Public Methods --------------------------------------------------
            public double Align(IMyRemoteControl rc, List<IMyTerminalBlock> gyroList)
            {
                Vector3D rotAxis = new Vector3D(0, 0, 0); // rotation axis
                double angle = 0; // rotation angle
                double fdot = 0, udot = 0; // forward vector dot product result, up vector dot product result

                cM = rc.WorldMatrix; // get rotation matrix from RC block
                cM = MatrixD.Transpose(cM); // rotate matrix into local co-ords

                if (reset) // check if recently reset
                {
                    tM = MatrixD.CreateFromQuaternion(targetRotation); // get rotation matrix from saved rotation

                    if (reverse) // If we want to maintain up vector, but reverse heading 180 degrees
                    {
                        tM.M11 *= -1; // reverse x axis (right vector) before rotation
                        tM.M12 *= -1;
                        tM.M13 *= -1;

                        tM.M31 *= -1; // reverse z axis (forward vector) before rotation
                        tM.M32 *= -1;
                        tM.M33 *= -1;
                    } // if reverse

                    tM = MatrixD.Transpose(tM); // rotate matrix into local co-ords

                    reset = false; // we are now set, clear the reset flag
                } // if reset

                GetChangeInPose(cM.Forward, cM.Up, tM.Forward, tM.Up, out rotAxis, out angle); // get axis and angle of rotation

                fdot = Vector3D.Dot(cM.Forward, tM.Forward); // forward vector dot product (cos(theta) of angle between vectors)
                udot = Vector3D.Dot(cM.Up, tM.Up); // up vector dot product (cos(theta) of angle between vectors)

                if (fdot < rotAErr || udot < rotAErr) // check dot products against error
                {
                    EnableOverride(gyroList, true); //enable the gyros for override
                    TurnGyros(gyroList, rotAxis, angle); // set the gyros to turn
                }
                else
                {
                    EnableOverride(gyroList, false); // disable gyro override
                }

                // return the smaller dot product
                if (fdot > udot)
                    return udot; // up vector dot product is less
                else
                    return fdot; // forward vector dot product is less

            } // Align method

            public void SetRotation(QuaternionD newRotation, bool newReverse = false)
            {
                targetRotation = newRotation; // set the rotation
                reverse = newReverse; // set the reverse
                reset = true; // reset calculations
            }

            public void Stop(List<IMyTerminalBlock> gyroList)
            {
                EnableOverride(gyroList, false); // kill all rotation
            } // Stop method

            // Supporting Methods ----------------------------------------------
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

            protected void TurnGyros(List<IMyTerminalBlock> gyroList, Vector3D axis, double angle, float coEff = 0.5f)
            {
                float ctrlVel = 0.1f; // control velocity
                IMyGyro gyro = null; // for looping through gyros
                int i = 0; // counter for looping

                gyro = gyroList[0] as IMyGyro;

                ctrlVel = gyro.GetMaximum<float>("Yaw") * (float)(angle / Math.PI) * coEff;
                ctrlVel = Math.Min(gyro.GetMaximum<float>("Yaw"), ctrlVel);
                ctrlVel = (float)Math.Max(0.005, ctrlVel);
                axis.Normalize();
                axis *= ctrlVel;

                for (i = 0; i < gyroList.Count; i++)
                {
                    gyro = gyroList[i] as IMyGyro;
                    gyro.SetValue("Pitch", Convert.ToSingle(axis.GetDim(0)));
                    gyro.SetValue("Yaw", -Convert.ToSingle(axis.GetDim(1)));
                    gyro.SetValue("Roll", -Convert.ToSingle(axis.GetDim(2)));
                } // for i loop
            } //TurnGyros method

            protected void EnableOverride(List<IMyTerminalBlock> gyroList, bool onoff = true, float power = 1.0f)
            {
                int i;
                IMyGyro gyro;
                for (i = 0; i < gyroList.Count; i++)
                {
                    gyro = gyroList[i] as IMyGyro;
                    if (!gyro.GyroOverride && onoff)
                    {
                        gyro.ApplyAction("Override");
                        gyro.SetValue("Power", power);
                    } // if !override && onoff
                    if (gyro.GyroOverride && !onoff)
                    {
                        gyro.ApplyAction("Override");
                        gyro.SetValue("Power", 1.0f);
                        gyro.SetValue("Yaw", 0f);
                        gyro.SetValue("Pitch", 0f);
                        gyro.SetValue("Roll", 0f);
                    } // if override && !onoff
                } // for i loop
            } // EnableOverride method

        } // GyroControl class
    } // Partial class
} // Namespace
