using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace mes_log_ping_prod_pc
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write($">>>>> MES PC Production Network Checking Ver. {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}\n");
            Console.Write(">>>>> By MES Team\n");
            Console.Write(">>>>> Date 2022/03/15\n");
            Console.Write($">>>>> --------- Start ({DateTime.Now}) ---------\n");
            MES_LOG_PING_TXN mES_LOG_PING = new MES_LOG_PING_TXN();

            DataTable DT = MESLogPingAction.GetComputerInLine();

            DT = MyDataTable.AddColumn(DT, "PING");
            double Total = DT.Rows.Count;
            for (int idx = 0; idx < DT.Rows.Count; idx++)
            {
                double progress = ((double.Parse((idx + 1).ToString("0")) / Total) * 100);

                Console.Write($"\r>>>>> Progress : {idx + 1}/{DT.Rows.Count}, {progress.ToString("0.00")}%");

                string CLIENTIP = MyDataTable.GetCell(DT, "CLIENTIP", "", idx);
                string[] separatingStrings = { "\r\n" };
                string[] arrIP = CLIENTIP.Split(separatingStrings, StringSplitOptions.RemoveEmptyEntries);

                DataTable dtIP = new DataTable();
                dtIP = MyDataTable.AddColumn(dtIP, "IP");
                for (int i = 0; i < arrIP.Length; i++)
                {
                    DataRow DR = dtIP.NewRow();
                    DR["IP"] = arrIP[i];
                    dtIP.Rows.Add(DR);
                }

                if (arrIP.Length > 0)
                {
                    dtIP = MyDataTable.GetTableBySelect(dtIP, "IP LIKE '%10.193.%'");
                    string IP = MyDataTable.GetCell(dtIP, "IP", arrIP[arrIP.Length - 1]);

                    DT = MyDataTable.SetCell(DT, idx, "CLIENTIP", IP);

                    mES_LOG_PING.PC_HOST_NAME = MyDataTable.GetCell(DT, "CLIENTHOSTNAME", "", idx);
                    mES_LOG_PING.PC_IP = IP;
                    bool pingResult = MyHttp.PingHost(IP, out string msgTestResult, out string time, out string TTL, out string bytes, out string status);

                    string PingResult = "";
                    if (pingResult)
                    {
                        PingResult = "Normal";
                    }
                    else
                    {
                        PingResult = "Abnormal";
                    }

                    mES_LOG_PING.TEST_RESULT = PingResult;
                    mES_LOG_PING.PING_TIME = time;
                    mES_LOG_PING.PING_STATUS = status;
                    mES_LOG_PING.CREATE_BY = MyComputer.GetComputerNameAndIP();
                    mES_LOG_PING.FACTORY = MyDataTable.GetCell(DT, "SCHEMANAME", "", idx);
                    mES_LOG_PING.ID = DateTime.Now.ToString("yyyyMMddHHmmss") + mES_LOG_PING.PC_HOST_NAME;
                    mES_LOG_PING.LINE = MyDataTable.GetCell(DT, "LINENAME", "", idx);
                    mES_LOG_PING.STATION = MyDataTable.GetCell(DT, "GROUPNAME", "", idx);

                    if (!pingResult)
                    {
                        MESLogPingAction.RecordTransaction(mES_LOG_PING);
                    }

                    MESLogPingAction.RecordMaster(mES_LOG_PING);

                    DT = MyDataTable.SetCell(DT, idx, "PING", PingResult);
                }
            }

            Console.Write("\n>>>>> --------- End ---------\n");
        }
    }
}
