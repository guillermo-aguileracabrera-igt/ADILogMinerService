using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data;

namespace LoggerService
{
    public class Logger
    {
        private string connectionString = ConfigurationManager.AppSettings["ConnectionStrings"];
        private string path;

        #region _CONST
        private const string reduceMultiSpace = @"[ ]{2,}";
        #endregion _CONST

        public Logger(string _path) {
            path = _path;
        }

        public void GetFiles(string fileName = "")
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                Dictionary<FileInfo, List<List<string>>> results = new Dictionary<FileInfo, List<List<string>>>();
                StreamReader reader = null;

                if (fileName != "")
                {
                    long fileNumber = Int64.Parse(fileName.Replace(".log", "").Substring(Regex.Match(fileName, @"\d+").Index));
                    string file = fileName.Substring(0, Regex.Match(fileName, @"\d+").Index);
                    string number = "";
                    string finalName = "";
                    FileInfo fileInfo = null;

                    string prevFileName = "";

                    if (fileNumber > 1)
                    {
                        fileNumber--;
                        number = fileNumber.ToString("D11");
                        finalName = path + Path.DirectorySeparatorChar + file + number + ".log";
                        prevFileName = file + number + ".log";

                        fileInfo = new FileInfo(finalName);

                        List<List<string>> queries = new List<List<string>>();
                        reader = new StreamReader(fileInfo.FullName);
                        queries.AddRange(CreateQueries(reader, fileInfo.Name));
                        results.Add(fileInfo, queries);

                        if (CheckPreviousInsert(prevFileName) == false)
                        {
                            foreach (KeyValuePair<FileInfo, List<List<string>>> d in results)
                            {
                                foreach (List<string> s in d.Value)
                                {
                                    Console.WriteLine($"Insert!");
                                    ExecQueries(s, d.Key.Name);
                                }
                            }
                        }
                        else
                        {
                            WriteToFile($"{prevFileName} already inserted.");
                            Console.WriteLine($"{prevFileName} already inserted.");
                        }
                    }
                }
                else
                {
                    List<FileInfo> files = new List<FileInfo>();
                    files = directory.GetFiles("*.log").ToList();

                    foreach (FileInfo f in files)
                    {
                        List<List<string>> queries = new List<List<string>>();
                        reader = new StreamReader(f.FullName);

                        queries.AddRange(CreateQueries(reader, f.Name));

                        if (queries.Count > 0)
                        {
                            results.Add(f, queries);
                        }
                    }

                    foreach(KeyValuePair<FileInfo, List<List<string>>> r in results)
                    {
                        if (CheckPreviousInsert(r.Key.Name) == false)
                        {
                            foreach (List<string> s in r.Value)
                            {
                                ExecQueries(s, r.Key.Name);
                            }
                        }
                        else
                        {
                            WriteToFile($"{r.Key.Name} already inserted.");
                            Console.WriteLine($"{r.Key.Name} already inserted.");
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToFile(ex.Message);
                Console.WriteLine(ex.Message);
            }
        }

        private List<List<string>> CreateQueries(StreamReader reader, string fileName, string lineFilter = "", DateTime? date1 = null, DateTime? date2 = null)
        {
            List<List<string>> queries = new List<List<string>>();
            String line = reader.ReadLine();
            DateTime? startDate = DateTime.MinValue;
            DateTime? endDate = DateTime.MinValue;
            StringBuilder sb = new StringBuilder();
            bool isMsg = false;
            bool checkDate = false;
            int lineCounter = 0;
            string appName = "";
            string host = "";

            if (date1 == null)
            {
                date1 = DateTime.MinValue;
            }

            if (date2 == null)
            {
                date2 = DateTime.MinValue;
            }

            try
            {
                while (line != null)
                {
                    lineCounter++;
                    line = line.Replace("/*", String.Empty).Replace("*/", String.Empty);
                    string[] spl = line.Replace("\t", "  ").Split(' ').Where(x => x != "").ToArray();
                    string logMsg = "";

                    if (line == "")
                    {
                        line = reader.ReadLine();
                        continue;
                    }

                    if (!String.IsNullOrEmpty(lineFilter) && !String.IsNullOrWhiteSpace(lineFilter))
                    {
                        if (!line.Contains(lineFilter))
                        {
                            line = reader.ReadLine();
                            continue;
                        }
                    }

                    if (date1 > DateTime.MinValue && date2 > DateTime.MinValue && date2 >= date1 && date2 <= DateTime.Today)
                    {
                        startDate = date1;
                        endDate = date2;
                        checkDate = true;
                    }

                    if (!DateTime.TryParseExact(spl[0], "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d3))
                    {
                        if (lineCounter == 1)
                        {
                            line = reader.ReadLine();
                            continue;
                        }
                        isMsg = true;
                        sb.Append(Regex.Replace(line.Replace("\t", "  "), reduceMultiSpace, String.Empty));
                    }
                    else
                    {
                        if (isMsg)
                        {
                            if (lineCounter == 1)
                            {
                                line = reader.ReadLine();
                                continue;
                            }
                            logMsg = sb.ToString();
                            sb.Clear();
                            isMsg = false;
                        }

                        if (spl.Length < 7)
                        {
                            if (!isMsg && logMsg == "")
                            {
                                DateTime sDateTime = DateTime.MinValue;
                                if (spl.Length > 2 && DateTime.TryParseExact(spl[0] + " " + spl[1], "MM/dd/yy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dd))
                                {
                                    sDateTime = dd;
                                }
                                else
                                {
                                    line = reader.ReadLine();
                                    continue;
                                }
                                string severityID = spl[2];
                                if (spl.Length == 4)
                                {
                                    logMsg = spl[3];
                                }
                                else if (spl.Length == 5)
                                {
                                    appName = spl[3];
                                    logMsg = spl[4];
                                }
                                else if (spl.Length == 6)
                                {
                                    appName = spl[3];
                                    host = spl[4] + " " + spl[5];
                                    logMsg = "";
                                }

                                List<string> query = new List<string>() { sDateTime.ToString(), appName, host, severityID, logMsg, " -- file: " + fileName };

                                queries.Add(query);
                            }
                        }
                        else
                        {
                            if (!isMsg && logMsg == "")
                            {
                                int j = 6;

                                if (spl[4].Contains("Service"))
                                {
                                    j++;
                                }

                                for (int i = j; i < spl.Length; i++)
                                {
                                    sb.Append(spl[i] + " ");
                                }

                                logMsg = sb.ToString();
                                sb.Clear();
                            }

                            DateTime sDateTime = DateTime.Parse(spl[0] + " " + spl[1]);
                            if (checkDate && ((sDateTime < startDate) || (sDateTime > endDate)))
                            {
                                line = reader.ReadLine();
                                continue;
                            }
                            string severityID = spl[2];
                            if (spl[4].Contains("Service"))
                            {
                                appName = spl[3] + " " + spl[4];
                                host = spl[5] + " " + spl[6];
                            }
                            else
                            {
                                appName = spl[3];
                                host = spl[4] + " " + spl[5];
                            }

                            List<string> query = new List<string>() { sDateTime.ToString(), appName, host, severityID, logMsg, " -- file: " + fileName };

                            queries.Add(query);
                        }
                    }

                    line = reader.ReadLine();
                }

                return queries;
            }
            catch (Exception ex)
            {
                WriteToFile($"Exception -> CreateQueries -> { ex.Message }");
                Console.WriteLine($"Exception -> CreateQueries -> { ex.Message }");

                return queries;
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }
        }
        
        private void ExecQueries(List<string> query, string prevFileName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("Proc_Insert_ADI_Logs", conn))
                {
                    try
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Date", DateTime.Parse(query[0]));
                        cmd.Parameters.AddWithValue("@Thread", query[1]);
                        cmd.Parameters.AddWithValue("@Logger", query[2]);
                        cmd.Parameters.AddWithValue("@LevelID", query[3]);
                        cmd.Parameters.AddWithValue("@Message", query[4]);
                        cmd.Parameters.AddWithValue("@FileName", query[5].Replace(" -- file: ", ""));
                        cmd.Parameters.AddWithValue("@FromService", 1);
                        conn.Open();

                        cmd.ExecuteNonQuery();

                        WriteToFile($"{prevFileName} Inserted at {DateTime.Now}.");
                        Console.WriteLine($"{prevFileName} Inserted at {DateTime.Now}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        WriteToFile($"Exception -> ExecQueries -> {ex.Message}");
                        Console.WriteLine($"Exception -> ExecQueries -> {ex.Message}");
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
        }

        internal void WriteToFile(string Message)
        {
            string path = ConfigurationManager.AppSettings["Path"] + "\\writeLogs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filepath = path + "\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            StreamWriter sw = null;

            try
            {
                if (!File.Exists(filepath))
                {
                    // Create a file to write to.
                    using (sw = new StreamWriter(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
                else
                {
                    using (sw = File.AppendText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
            }
            catch (Exception ex)
            {
                sw = File.AppendText(filepath);
                sw.WriteLine(ex.Message);
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        private bool CheckPreviousInsert(string fileName)
        {
            bool resp = false;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("IF EXISTS(SELECT TOP 1 [Id] FROM [dbo].[ADILogData] WHERE ([FileName] = @FileName)) SELECT 1 ELSE SELECT 0", conn))
                {
                    try
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@FileName", fileName);
                        conn.Open();
                        int UserExist = (int)(cmd.ExecuteScalar());

                        if (UserExist > 0)
                        {
                            resp = true;
                        }
                        else
                        {
                            resp = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteToFile(ex.Message);
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }

            return resp;
        }
    }
}
