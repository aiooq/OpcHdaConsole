using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace opchda
{
  [CompilerGenerated]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [Guid("1F1217B0-DEE0-11D2-A5E5-000086339399")]
  [TypeIdentifier]
  [ComImport]
  public interface IOPCHDA_Server
  {
    [SpecialName]
    [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
    void _VtblGap1_3();

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetItemHandles([In] uint dwCount, [MarshalAs(UnmanagedType.LPWStr), In] ref string pszItemID, [In] ref uint phClient, out IntPtr pphServer, out IntPtr ppErrors);
  }
}
