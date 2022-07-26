using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace opchda
{
  [CompilerGenerated]
  [TypeIdentifier("1F1217BA-DEE0-11D2-A5E5-000086339399", "opchda.tagOPCHDA_TIME")]
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  public struct tagOPCHDA_TIME
  {
    public int bString;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string szTime;
    public _FILETIME ftTime;
  }
}
