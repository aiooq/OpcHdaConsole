using opchda;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpcHdaConsole
{
  public class Server
  {
    public string name;
    private IOPCHDA_Server m_pIOPCHDAServer;
    private IOPCHDA_SyncRead m_pIOPCHDASyncRead;

    public Server(string name)
    {
      this.name = name;
    }

    ~Server()
    {
        Detach();
    }

    public void Attach()
    {
      try
      {
        Type typeFromProgId = Type.GetTypeFromProgID(this.name);
        if (typeFromProgId == (Type) null)
          throw new SystemException("Не найден сервер OPC HDA:" + this.name);
        this.m_pIOPCHDAServer = (IOPCHDA_Server) Activator.CreateInstance(typeFromProgId);
        if (this.m_pIOPCHDAServer == null)
          throw new SystemException("Не удалось подключиться к серверу OPC HDA:" + this.name);
      }
      catch (SystemException ex)
      {
        throw ex;
      }
    }

    public void Detach()
    {
      if (this.m_pIOPCHDASyncRead != null)
        Marshal.ReleaseComObject((object) this.m_pIOPCHDASyncRead);

      if (this.m_pIOPCHDAServer == null)
        return;
      
        Marshal.ReleaseComObject((object) this.m_pIOPCHDAServer);
    }

    public uint GetItemHandle(string strItem)
    {
      try
      {
        string pszItemID = strItem.ToString();
        uint phClient = 0;
        IntPtr pphServer = IntPtr.Zero;
        IntPtr ppErrors = IntPtr.Zero;
        // ISSUE: reference to a compiler-generated method
        this.m_pIOPCHDAServer.GetItemHandles(1U, ref pszItemID, ref phClient, out pphServer, out ppErrors);
        int num = Marshal.ReadInt32(pphServer);
        Marshal.ReadInt32(ppErrors);
        Marshal.FreeCoTaskMem(pphServer);
        Marshal.FreeCoTaskMem(ppErrors);
        return (uint) num;
      }
      catch (SystemException ex)
      {
          throw new SystemException("Не удалось получить описатель OPC HDA сигнала: " + strItem + " | ошибка: " + ex.Message);
      }
    }

    public List<Item> ReadItem(string strItem, DateTime dtStart, DateTime dtEnd, uint uNumValues)
    {
      uint hResult;
      return this.ReadItem(strItem, dtStart, dtEnd, uNumValues, out hResult);
    }

    public List<Item> ReadItem(string strItem, DateTime dtStart, DateTime dtEnd, uint uNumValues, out uint hResult)
    {
        List<Item> opcHdaItemList = new List<Item>();
      try
      {
        this.m_pIOPCHDASyncRead = (IOPCHDA_SyncRead) this.m_pIOPCHDAServer;

        tagOPCHDA_TIME htStartTime = new tagOPCHDA_TIME();
        if (dtStart.Ticks == 0L)
        {
          htStartTime.ftTime.dwLowDateTime = 0U;
          htStartTime.ftTime.dwHighDateTime = 0U;
        }
        else
        {
          htStartTime.ftTime.dwLowDateTime = (uint) dtStart.ToFileTime();
          htStartTime.ftTime.dwHighDateTime = (uint) (dtStart.ToFileTime() >> 32);
        }
        tagOPCHDA_TIME htEndTime = new tagOPCHDA_TIME();
        if (dtEnd.Ticks == 0L)
        {
          htEndTime.ftTime.dwLowDateTime = 0U;
          htEndTime.ftTime.dwHighDateTime = 0U;
        }
        else
        {
          htEndTime.ftTime.dwLowDateTime = (uint) dtEnd.ToFileTime();
          htEndTime.ftTime.dwHighDateTime = (uint) (dtEnd.ToFileTime() >> 32);
        }
        uint itemHandle = this.GetItemHandle(strItem);
        IntPtr ppItemValues = IntPtr.Zero;
        IntPtr ppErrors = IntPtr.Zero;
        this.m_pIOPCHDASyncRead.ReadRaw(ref htStartTime, ref htEndTime, uNumValues, 0, 1U, ref itemHandle, out ppItemValues, out ppErrors);
        tagOPCHDA_ITEM structure = (tagOPCHDA_ITEM) Marshal.PtrToStructure(ppItemValues, typeof (tagOPCHDA_ITEM));
        hResult = (uint) Marshal.ReadInt32(ppErrors);
        if (hResult == 0U)
        {
            for (int index = 0; (long)index < (long)structure.dwCount; ++index)
            {
                Item opcHdaItem = new Item();
                opcHdaItem.value = Marshal.GetObjectForNativeVariant(IntPtr.Add(structure.pvDataValues, index * 16));
                long fileTime = Marshal.ReadInt64(IntPtr.Add(structure.pftTimeStamps, index * 8));
                opcHdaItem.time = DateTime.FromFileTime(fileTime);
                int num = Marshal.ReadInt32(IntPtr.Add(structure.pdwQualities, index * 4));
                opcHdaItem.quality = (uint)num;
                opcHdaItemList.Add(opcHdaItem);
            }
        }
        Marshal.FreeCoTaskMem(ppItemValues);
        Marshal.FreeCoTaskMem(ppErrors);
      }
      catch (SystemException ex)
      {
          throw new SystemException("Не удалось получить данные сигнала: " + strItem + " | ошибка: " + ex.Message);
      }
      return opcHdaItemList;
    }
  }
}
