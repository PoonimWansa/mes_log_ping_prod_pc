using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Data;

namespace mes_log_ping_prod_pc
{
    public class MES_LOG_PING_TXN
    {
        public string ID { get; set; }
        public string CDATE { get; set; }
        public string CREATE_BY { get; set; }
        public string PING_STATUS { get; set; }
        public string PING_TIME { get; set; }
        public string TEST_RESULT { get; set; }
        public string PC_HOST_NAME { get; set; }
        public string LINE { get; set; }
        public string FACTORY { get; set; }
        public string PC_IP { get; set; }
        public string STATION { get; set; }
    }

    public static class MyHttp
    {
        public static bool Get(string token_name, string token_key, string url, out string ReturnResult, out string ErrorMsg)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            if (token_name != "")
            {
                request.Headers[token_name] = token_key;
            }

            try
            {
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                    ReturnResult = reader.ReadToEnd();
                }

                ErrorMsg = "";
                return true;
            }
            catch (Exception ex)
            {
                ErrorMsg = ex.Message;
                ReturnResult = "";
                return false;
            }
        }

        public static bool Post(string token_name, string token_key, string url, string data, out string result, out string msg)
        {
            try
            {
                byte[] arrdata = Encoding.ASCII.GetBytes($"{data}");

                WebRequest request = WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = arrdata.Length;
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(arrdata, 0, arrdata.Length);
                }


                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader sr99 = new StreamReader(stream))
                        {
                            result = sr99.ReadToEnd();
                        }
                    }
                }

                msg = "";
                return true;
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                result = ex.Message;
                return false;
            }
        }

        public static string GetHostName(string ipAddress)
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                if (entry != null)
                {
                    return entry.HostName;
                }
            }
            catch (Exception ex)
            {
                //unknown host or
                //not every IP has a name
                //log exception (manage it)
            }

            return null;
        }

        public static bool PingHost(string nameOrAddress, out string Status, out string pingTime, out string pingTTL, out string pingBytes, out string pingStatus)
        {
            Ping pingSender = new Ping();
            try
            {
                PingOptions options = new PingOptions();

                options.DontFragment = true;
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 1000;
                PingReply reply = pingSender.Send(nameOrAddress, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                {

                    Status = $"Status: {reply.Status}" +
                        $"\n - RoundTrip time (time): {reply.RoundtripTime}ms" +
                        $"\n - Time to live (TTL): {reply.Options.Ttl}" +
                        $"\n - Buffer size (bytes): {reply.Buffer.Length}";

                    pingTime = reply.RoundtripTime.ToString();
                    pingTTL = reply.Options.Ttl.ToString();
                    pingBytes = reply.Buffer.Length.ToString();
                    pingStatus = $"{reply.Status}";
                    return true;
                }
                else
                {
                    Status = $"Status: {reply.Status}";
                    pingTime = "";
                    pingTTL = "";
                    pingBytes = "";
                    pingStatus = $"{reply.Status}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                pingTime = "";
                pingTTL = "";
                pingBytes = "";
                Status = ex.Message;
                pingStatus = "";
                return false;
            }
            finally
            {
                if (pingSender != null)
                {
                    pingSender.Dispose();
                }
            }
        }
    }

    public static class MyComputer
    {
        public static string GetComputerNameAndIP()
        {
            try
            {
                string PCName = Environment.MachineName;
                IPAddress[] arrIP = Dns.GetHostAddresses(Environment.MachineName);
                string IP = "";
                for (int i = 0; i < arrIP.Length; i++)
                {
                    if (i == 0)
                    {
                        //IP = arrIP[i].ToString();
                    }
                    else
                    {
                        IP = IP + ", " + arrIP[i].ToString();
                    }

                }

                return $"{PCName} {IP}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }

    public static class MESLogPingAction
    {
        public static DataTable GetComputerInLine()
        {
            DataTable result = new DataTable();
            using (MyOracleManagedDataAccess DB = new MyOracleManagedDataAccess("THPUBMES-SCAN", "1521"
                , "THPUBMES", "MESAP03", "Delta12345", out string ErrorMessage))
            {
                string SQL = $@"SELECT
	                            MAX(CARETE_TIME),
	                            SCHEMANAME,
	                            LINENAME,
	                            GROUPNAME,
	                            CLIENTHOSTNAME,
	                            CLIENTIP
                            FROM
	                            DET_AM.c_mes_user_info_t
                            WHERE
	                            LINENAME IS NOT NULL
	                            AND CARETE_TIME >= sysdate-3
                            GROUP BY
	                            SCHEMANAME,
	                            LINENAME,
	                            GROUPNAME,
	                            CLIENTHOSTNAME,
	                            CLIENTIP
                            ORDER BY
	                            LINENAME DESC";
                DB.ExecQuery(SQL, out result, out string message);
            }
            return result;
        }

        public static void RecordTransaction(MES_LOG_PING_TXN data)
        {
            string ID = "null";
            if (data.ID != "")
            {
                ID = $"'{data.ID}'";
            }

            string CREATE_BY = "null";
            if (data.CREATE_BY != "")
            {
                CREATE_BY = $"'{data.CREATE_BY}'";
            }

            string PING_STATUS = "null";
            if (data.PING_STATUS != "")
            {
                PING_STATUS = $"'{data.PING_STATUS}'";
            }

            string PING_TIME = "null";
            if (data.PING_TIME != "")
            {
                PING_TIME = $"'{data.PING_TIME}'";
            }

            string PC_HOST_NAME = "null";
            if (data.PC_HOST_NAME != "")
            {
                PC_HOST_NAME = $"'{data.PC_HOST_NAME}'";
            }

            string LINE = "null";
            if (data.LINE != "")
            {
                LINE = $"'{data.LINE}'";
            }

            string FACTORY = "null";
            if (data.FACTORY != "")
            {
                FACTORY = $"'{data.FACTORY}'";
            }

            string PC_IP = "null";
            if (data.PC_IP != "")
            {
                PC_IP = $"'{data.PC_IP}'";
            }

            string STATION = "null";
            if (data.STATION != "")
            {
                STATION = $"'{data.STATION}'";
            }

            string TEST_RESULT = "null";
            if (data.TEST_RESULT != "")
            {
                TEST_RESULT = $"'{data.TEST_RESULT}'";
            }

            using (MyOleDb DB = new MyOleDb("Provider=sqloledb; Data Source=THBPOCIMDB; User Id=MESDB;Password=MES12345;"))
            {
                string SQL = $@"INSERT INTO MESPRDDB.dbo.MES_LOG_PING_TXN (ID
                            ,CDATE
                            ,CREATE_BY
                            ,PING_STATUS
                            ,PING_TIME
                            ,PC_HOST_NAME
                            ,LINE
                            ,FACTORY
                            ,PC_IP
                            ,STATION
                            ,TEST_RESULT)VALUES(
                            '{ID}'
                            ,getdate()
                            ,'{CREATE_BY}'
                            ,'{PING_TIME}'
                            ,'{PC_HOST_NAME}'
                            ,'{LINE}'
                            ,'{FACTORY}'
                            ,'{PC_IP}'
                            ,'{STATION}'
                            ,'{TEST_RESULT}') ";
                DB.ExecNonQuery(SQL, out string Message);
            }
        }

        public static void RecordMaster(MES_LOG_PING_TXN data)
        {
            string ID = "null";
            if (data.ID != "")
            {
                ID = $"'{data.ID}'";
            }

            string CREATE_BY = "null";
            if (data.CREATE_BY != "")
            {
                CREATE_BY = $"'{data.CREATE_BY}'";
            }

            string PING_STATUS = "null";
            if (data.PING_STATUS != "")
            {
                PING_STATUS = $"'{data.PING_STATUS}'";
            }

            string PING_TIME = "null";
            if (data.PING_TIME != "")
            {
                PING_TIME = $"'{data.PING_TIME}'";
            }

            string PC_HOST_NAME = "null";
            if (data.PC_HOST_NAME != "")
            {
                PC_HOST_NAME = $"'{data.PC_HOST_NAME}'";
            }

            string LINE = "null";
            if (data.LINE != "")
            {
                LINE = $"'{data.LINE}'";
            }

            string FACTORY = "null";
            if (data.FACTORY != "")
            {
                FACTORY = $"'{data.FACTORY}'";
            }

            string PC_IP = "null";
            if (data.PC_IP != "")
            {
                PC_IP = $"'{data.PC_IP}'";
            }

            string STATION = "null";
            if (data.STATION != "")
            {
                STATION = $"'{data.STATION}'";
            }

            string TEST_RESULT = "null";
            if (data.TEST_RESULT != "")
            {
                TEST_RESULT = $"'{data.TEST_RESULT}'";
            }

            using (MyOleDb DB = new MyOleDb("Provider=sqloledb; Data Source=THBPOCIMDB; User Id=MESDB;Password=MES12345;"))
            {
                string SQL = $@"SELECT * FROM MESPRDDB.dbo.MES_PROD_PC_MT WHERE  PC_HOST_NAME = '{data.PC_HOST_NAME}' ";
                DB.ExecQuery(SQL, out DataTable DT, out string Status);
                if (DT.Rows.Count > 0)
                {
                    SQL = $@"UPDATE MESPRDDB.dbo.MES_LOG_PING_MT SET 
                          CDATE = GETDATE()
                          ,CREATE_BY = {CREATE_BY}
                          ,PING_STATUS = {PING_STATUS}
                          ,PING_TIME = {PING_TIME}
                          ,PC_HOST_NAME = {CREATE_BY}
                          ,LINE = {PING_STATUS}
                          ,FACTORY = {PING_TIME}
                          ,PC_IP = {PC_IP}
                          ,STATION = {STATION}
                          ,TEST_RESULT = {TEST_RESULT}                           
                          WHERE PC_HOST_NAME = {PC_HOST_NAME}
                          ";
                }
                else
                {
                    SQL = $@"INSERT INTO MESPRDDB.dbo.MES_LOG_PING_MT (
                            CDATE
                            ,CREATE_BY
                            ,PING_STATUS
                            ,PING_TIME
                            ,PC_HOST_NAME
                            ,LINE
                            ,FACTORY
                            ,PC_IP
                            ,STATION
                            ,TEST_RESULT)VALUES(
                            getdate()
                            ,{CREATE_BY}
                            ,{PING_STATUS}
                            ,{PING_TIME}
                            ,{PC_HOST_NAME}
                            ,{LINE}
                            ,{FACTORY}
                            ,{PC_IP}
                            ,{STATION}
                            ,{TEST_RESULT}) ";
                }

                DB.ExecNonQuery(SQL, out string Message);
            }
        }
    }
}
