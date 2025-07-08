using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grey.Enums
{
    public enum DeviceType
    {
        Unknown,
        CPAP,
        OxygenTank,
        WheelChair
    }

    public enum MaskType
    {
        FullFace
    }

    public enum AddOnType
    {
        Humidifier
    }

    public enum OxygenTankUseType
    {
        Sleep,
        Exertion,
        SleepAndExertion
    }
    
}
