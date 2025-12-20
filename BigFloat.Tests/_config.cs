using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigFloatLibrary.Tests;

public static class _config
{
    /// <summary>
    /// For long running methods only, This specifies the target time in milliseconds.
    /// </summary>
    public const int TestTargetInMilliseconds = 100;
    public const int RAND_SEED = 0x51C0_F00D;
    public static readonly Random _rand = new(RAND_SEED);
}
