﻿using System;
using System.Threading;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using Forms.Logging;
using Utilities;

namespace ABB_Comunication
{
    public partial class ControlUnit
    {
        public bool TryOffsetMove(DrawPlane plane, decimal x, decimal y, decimal z, bool doLog = true)
        {
            return TryOffsetMove(plane, (double)x, (double)y, (double)z, doLog);
        }
        public bool TryOffsetMove(DrawPlane plane, double x, double y, double z, bool doLog = true)
        {
            var task = _controller.Rapid.GetTask(RapidNames.TaskName);

            var executeFlag = task.GetRapidData(RapidNames.ModuleName, RapidNames.Variables.ExecuteFlag);
            if (executeFlag.GetBool())
            {
                if (HasTimedOut())
                {
                    StopTimeout();
                    Logger.InvokeLog($"Operation has timed out.");
                    return false;
                }
                return TryOffsetMove(plane, x, y, z, doLog);
            }
            StopTimeout();

            var homeFlag = task.GetRapidData(RapidNames.ModuleName, RapidNames.Variables.HomeFlag);
            var xData = task.GetRapidData(RapidNames.ModuleName, RapidNames.Variables.XOffset);
            var yData = task.GetRapidData(RapidNames.ModuleName, RapidNames.Variables.YOffset);
            var zData = task.GetRapidData(RapidNames.ModuleName, RapidNames.Variables.ZOffset);

            try
            {
                if (_controller.OperatingMode == ControllerOperatingMode.Auto)
                {
                    using (var m = Mastership.Request(_controller.Rapid))
                    {
                        xData.Value = new Num(x);
                        yData.Value = new Num(y);
                        zData.Value = new Num(z);
                        homeFlag.Value = new Bool(false);
                        executeFlag.Value = new Bool(true);
                    }

                    if (doLog)
                    {
                        Logger.InvokeLog(
                            $"Moving by offset: " +
                            $"{x.GetFixedString(3, 2)}; " +
                            $"{y.GetFixedString(3, 2)}; " +
                            $"{z.GetFixedString(3, 2)}");
                    }

                    return true;
                }
                else
                {
                    Logger.InvokeLog("Failed to move to target position: Automatic mode is required to start execution from a remote client.");
                }
            }
            catch (InvalidOperationException)
            {
                Logger.InvokeLog("Failed to move to target position: Mastership is held by another client.");
            }
            catch (Exception ex)
            {
                Logger.InvokeLog("Failed to move to target position: " + ex.Message);
            }

            return false;
        }
    }
}
