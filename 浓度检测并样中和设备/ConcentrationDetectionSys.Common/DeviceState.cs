using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Common
{
    public enum RunMode
    {
        Manual = 0,
        Auto = 1,
        Maintain = 2
    }
    public enum MES_Mode
    {
        Off_Line = 0,
        On_Line = 1
    }
    public enum DeviceState
    {
        Stop = 1,
        Running = 2,
        Alarm = 3,
    }
    
}
