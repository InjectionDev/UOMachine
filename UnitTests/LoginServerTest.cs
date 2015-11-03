using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UOMachine;
using UOMachine.Macros;
using UOMachine.Utility;
using UOMachine.Events;
using UOMachine.Tree;
using EasyHook;

namespace UnitTests
{
    public class LoginServerHook : EasyHook.IEntryPoint
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate int dConnect(uint s, ref NativeMethods.sockaddr_in sin, int len);

        private static LoginServerTest m_Interface;

        public static int Hook_connect(uint s, ref NativeMethods.sockaddr_in sin, int len)
        {
            m_Interface.Connect(String.Format("{0}.{1}.{2}.{3},{4}", sin.sin_addr.s_b1, sin.sin_addr.s_b2, sin.sin_addr.s_b3, sin.sin_addr.s_b4, NativeMethods.ntohs(sin.sin_port)));
            return -1;
        }

        public LoginServerHook(RemoteHooking.IContext context, System.String channel)
        {
            m_Interface = RemoteHooking.IpcConnectClient<LoginServerTest>(channel);
        }

        public void Run(RemoteHooking.IContext context, System.String channel)
        {
            try
            {
                LocalHook c = LocalHook.Create(LocalHook.GetProcAddress("WSOCK32.dll", "connect"), new dConnect(Hook_connect), null);
                c.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
                Console.WriteLine(c.ToString());
            }
            catch (Exception)
            {
                throw;
            }

            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [TestClass]
    public class LoginServerTest : MarshalByRefObject, EasyHook.IEntryPoint
    {
        private static string m_Address;
        public static object m_AddressLock;

        [TestCategory( "Network" ), TestCategory( "Network\\LoginServer" ), TestMethod]
        public void LoginServer70022()
        {
            string address = Start(@"D:\Clients\7.0.2.2");
            Assert.IsNotNull(address);
            Assert.AreEqual("127.0.0.1,2593", address);
        }

        [TestCategory( "Network" ), TestCategory( "Network\\LoginServer" ), TestMethod]
        public void LoginServer70200()
        {
            string address = Start(@"D:\Clients\7.0.20.0");
            Assert.IsNotNull(address);
            Assert.AreEqual("127.0.0.1,2593", address);
        }

        [TestCategory( "Network" ), TestCategory( "Network\\LoginServer" ), TestMethod]
        public void LoginServer70351()
        {
            string address = Start(@"D:\Clients\7.0.35.1");
            Assert.IsNotNull(address);
            Assert.AreEqual("127.0.0.1,2593", address);
        }

        [TestCategory( "Network" ), TestCategory( "Network\\LoginServer" ), TestMethod]
        public void LoginServer70450()
        {
            string address = Start(@"D:\Clients\7.0.45.0");
            Assert.IsNotNull(address);
            Assert.AreEqual("127.0.0.1,2593", address);
        }

        [TestCategory( "Network" ), TestCategory( "Network\\LoginServer" ), TestMethod]
        public void LoginServer70462()
        {
            string address = Start( @"D:\Clients\7.0.46.2" );
            Assert.IsNotNull( address );
            Assert.AreEqual( "127.0.0.1,2593", address );
        }

        public string Start(string path)
        {
            Process clientProcess = null;
            try {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WorkingDirectory = path;
                startInfo.FileName = Path.Combine(path, "client.exe");

                UOMachine.NativeMethods.SafeProcessHandle hProcess;
                UOMachine.NativeMethods.SafeThreadHandle hThread;
                uint pid, tid;

                if (UOMachine.NativeMethods.CreateProcess(startInfo, true, out hProcess, out hThread, out pid, out tid))
                {
                    ClientPatcher.MultiPatch(hProcess.DangerousGetHandle());
                    bool result = false;
                    m_AddressLock = new object();
                    clientProcess = Process.GetProcessById((int)pid);

                    UOMachine.IPC.Network.Initialize();
                    UOMachine.Utility.Log.Initialize("UnitTests" + DateTime.Now.ToString("[MM - dd - yyyy HH.mm.ss] ") + ".txt");
                    InternalEventHandler.IPCHandler.Initialize();

                    UOMachine.NativeMethods.ResumeThread(hThread.DangerousGetHandle());

                    ClientInfo ci = new ClientInfo(clientProcess);

                    int instance = 0;
                    ClientInfoCollection.ClientList[instance] = ci;
                    ClientInfoCollection.Count = 1;

                    ulong len = (ulong)ci.EntryPoint - (ulong)ci.BaseAddress;
                    UOMachine.NativeMethods.GainMemoryAccessEx(ci.Handle, ci.BaseAddress, len);

                    string channelName = null;
                    RemoteHooking.IpcCreateServer<LoginServerTest>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.SingleCall);
                    RemoteHooking.Inject(clientProcess.Id, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UnitTests.dll"), null, channelName);

                    ci.InstallMacroHook();
                    Macro.ChangeServer(instance, "127.0.0.1", 2593);

                    Thread.Sleep(10000);

                    lock (m_AddressLock)
                    {
                        GumpInfo[] gi = Macro.GetGumpList(instance);
                        foreach (GumpInfo g in gi)
                        {
                            if (g.Type == "MainMenu gump")
                            {
                                foreach (GumpInfo g2 in g.SubGumps)
                                {
                                    if (g2.Type == "AcctLogin gump")
                                    {
                                        if (ci.DateStamp > 0x43A06A35) g2.CallFunction(26);
                                        else g2.CallFunction(23);
                                    }
                                }
                            }
                        }
                        result = Monitor.Wait(m_AddressLock, 60000);
                    }

                    UOMachine.IPC.Network.Dispose();
                    Log.Dispose();
                    UOMachine.NativeMethods.TerminateProcess(Process.GetProcessById(clientProcess.Id).Handle, 0);

                    return m_Address;
                }
            } catch (Exception)
            {
                UOMachine.IPC.Network.Dispose();
                Log.Dispose();
                UOMachine.NativeMethods.TerminateProcess(Process.GetProcessById(clientProcess.Id).Handle, 0);
                throw;
            }
            return null;
        }

        public void Connect(string addr)
        {
            m_Address = addr;
            lock (m_AddressLock)
            {
                Monitor.Pulse(m_AddressLock);
            }
        }
    }
}
