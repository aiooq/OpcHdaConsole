using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace opchda
{
  [CompilerGenerated]
  [Guid("1F1217B2-DEE0-11D2-A5E5-000086339399")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [TypeIdentifier]
  [ComImport]
  public interface IOPCHDA_SyncRead
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void ReadRaw([In, Out] ref tagOPCHDA_TIME htStartTime, [In, Out] ref tagOPCHDA_TIME htEndTime, [In] uint dwNumValues, [In] int bBounds, [In] uint dwNumItems, [In] ref uint phServer, out IntPtr ppItemValues, out IntPtr ppErrors);
  }
}
