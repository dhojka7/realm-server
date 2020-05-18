using System;
using System.Collections.Generic;
using System.Text;

namespace RotMG.Common
{
    public enum RegisterStatus
    {
        Success,
        UsernameTaken,
        InvalidUsername,
        InvalidPassword,
        TooManyRegisters
    }
}
