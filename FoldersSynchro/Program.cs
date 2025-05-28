
using CommandLine;

namespace FoldersSynchro
{
    using System;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml.Linq;
    using static System.Runtime.InteropServices.JavaScript.JSType;
    using System.Timers;

    internal class Program
    {
        public class Options
        {
            [Option('s', "source", Required = true, HelpText = "Enter the path to the SOURCE folder.")]
            public string Source { get; set; }

            [Option('r', "replica", Required = false, HelpText = "Enter the path to the REPLICA folder.")]
            public string Replica { get; set; }

            [Option('i', "interval", Required = false, HelpText = "Enter the synchronization INTERVAL in seconds.")]
            public int Interval { get; set; }

            [Option('l', "logfile", Required = false, HelpText = "Enter the path to the LOG_FILE file.")]
            public string LogFile { get; set; }
        }

        static Timer timer;

        private static Options userdata;


        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                  .WithParsed(o =>
                  {
                      if (PathCheck(o.Source, "Source", o.LogFile, false) == "")
                        return;

                      if (!string.IsNullOrEmpty(o.Interval.ToString()) && !string.IsNullOrWhiteSpace(o.Interval.ToString()))
                      {
                          if (o.Interval <= 0)
                          {
                              Console.WriteLine($"INFO - Enter the synchronization INTERVAL in seconds (integer greater than 0).");
                              return;
                          }
                      }
                      else 
                      {
                          Console.WriteLine("INFO - Enter the synchronization INTERVAL in seconds.");
                          return;
                      }

                      if (!string.IsNullOrEmpty(o.LogFile) && !string.IsNullOrWhiteSpace(o.LogFile))
                      {
                          o.LogFile = PathCheck(o.LogFile, "LogFile.txt", o.LogFile, true);
                      }
                      else
                      {
                          if (File.Exists(Directory.GetCurrentDirectory() + "\\" + "LogFile.txt"))
                          {
                              o.LogFile = Directory.GetCurrentDirectory() + "\\" + "LogFile.txt";
                              WriteToLogFile(o.LogFile, "INFO - No path given to LOG_FILE file.");
                              WriteToLogFile(o.LogFile, $"REUSE - {o.LogFile}");
                          }
                          else
                          {
                              o.LogFile = NewFile("LogFile.txt", "");
                              WriteToLogFile(o.LogFile, "INFO - No path given to LOG_FILE file.");
                              WriteToLogFile(o.LogFile, $"CREATE - {o.LogFile}");
                          }
                      }

                      if (!string.IsNullOrEmpty(o.Replica) && !string.IsNullOrWhiteSpace(o.Replica))
                      {
                          o.Replica = PathCheck(o.Replica, "Replica", o.LogFile, false);
                      }
                      else
                      {
                          WriteToLogFile(o.LogFile, "INFO - No path given to REPLICA folder.");
                          o.Replica = NewFolder("Replica", o.LogFile);
                      }

                      Program.userdata = o;

                      configTimer();

                      Console.WriteLine("\r\n");
                      WriteToLogFile(o.LogFile, "START Synchronization");

                      timer.Enabled = true;   
                      
                      Console.WriteLine($"\r\nSynchronization started. Operation performed every {o.Interval} seconds.");
                      Console.WriteLine("\r\nPress Enter to exit the program.\r\n");
                      Console.ReadLine();   

                      WriteToLogFile(o.LogFile, "END Synchronization");

                  })
                  .WithNotParsed(errors =>
                  {

                      Console.WriteLine("Invalid arguments. Use --help to see available options.");
                  });
        }

        private static void configTimer()
        {
            try
            {
                double intervalMiliseconds = Program.userdata.Interval * 1000;

                timer = new Timer(intervalMiliseconds);
                timer.Elapsed += TimerTasks;
                timer.AutoReset = true; 

            }
            catch (Exception ex)
            {
                Console.WriteLine($"configTimer ERROR: {ex.Message}");
            }
        }

        private static void WriteToLogFile(string logFile, string logText)
        {
            try
            {
                string logDirectory = Path.GetDirectoryName(logFile);
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                using (StreamWriter writer = new StreamWriter(logFile, append: true))
                { 
                    string logTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine($"{logTime} - {logText}");
                    writer.WriteLine($"{logTime} - {logText}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WriteToLogFile ERROR: {ex.Message}");
            }
        }

        private static void ReplicaCleaning(string source, string replica, string logFile)
        {
            /* Deleting folders and files which no longer exist in Source */

            string initialTextR = Path.GetFileName(replica);   
            var elementsR = Directory.EnumerateFileSystemEntries(replica);
            foreach (string elementR in elementsR)
            {
                string cutoutFragmentR = "";
                int indexR = elementR.IndexOf(initialTextR);            // index of the end of the REPLICA folder

                if (indexR != -1)                                       // text was found
                {
                    cutoutFragmentR = elementR.Substring(indexR + initialTextR.Length);    // cut a fragment of the path after the found text
                }
                else
                {
                    Console.WriteLine($"Tekxt '{initialTextR}' was not found in the path.");
                }

                if (Directory.Exists(elementR)) 
                {
                    if (!Directory.Exists(source + cutoutFragmentR))    // there is no directory "elementR" in SOURCE directory
                    {
                        Directory.Delete(elementR, true);               // remove it from replica
                        WriteToLogFile(logFile, $"DELETE - {elementR}");
                    }
                    else                                               
                        ReplicaCleaning(source + cutoutFragmentR, elementR, logFile);      
                }
                else if (File.Exists(elementR))                       
                {
                    if (!File.Exists(source + cutoutFragmentR))         // there is no file "elementR" in SOURCE directory
                    {
                        DeleteFile(elementR, cutoutFragmentR, logFile); // remove it from replica
                    }
                }
            }
        }

        public static string PathCheck(string source, string sourceName, string logFile, bool ifCreateFile)
        {
            if (!string.IsNullOrEmpty(source))
            {
                if (File.Exists(source))
                {
                    return source;
                }
                else if (Directory.Exists(source))
                {
                    return source;               
                }
                else
                {
                    if (ifCreateFile)
                    {
                        if (File.Exists(Directory.GetCurrentDirectory() + "\\" + "LogFile.txt"))
                            {
                            logFile = Directory.GetCurrentDirectory() + "\\" + "LogFile.txt";
                                WriteToLogFile(logFile, $"ERROR - The specified file '{source}' does not exist.");
                                WriteToLogFile(logFile, $"REUSE - {logFile}");
                            return logFile;
                            }

                            string newFile = NewFile(sourceName, "");  
                            WriteToLogFile(newFile, $"ERROR - The specified file '{source}' does not exist.");
                            WriteToLogFile(newFile, $"CREATE - {newFile}");
                            return newFile; 
                    }
                    else
                    {
                        if (sourceName == "Source")
                        {
                            Console.WriteLine($"ERROR - The SOURCE directory '{source}' does not exist. Please, give the correct path.");
                            return "";
                        }
                        WriteToLogFile(logFile, $"ERROR - The specified directory '{source}' does not exist.");
                        return NewFolder(sourceName, logFile);
                    }

                }
            }
            return source;
        }

        private static string NewFile(string sourceName, string logFile)
        {
            try
            {
                string katalog = Directory.GetCurrentDirectory();
                if (!Directory.Exists(katalog))
                {
                    Directory.CreateDirectory(katalog);
                }

                using (FileStream fs = File.Create(sourceName))
                {
                    byte[] dane = System.Text.Encoding.UTF8.GetBytes("\n");
                    fs.Write(dane, 0, dane.Length);
                    if (logFile != "")
                        WriteToLogFile(logFile, $"CREATE - {katalog + "\\" + sourceName}");
                    return katalog + "\\" + sourceName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An ERROR occurred: {ex.Message}");
                WriteToLogFile(logFile, $"An ERROR occurred: {ex.Message}");    //usun?
                return ex.Message;
            }
        }

        private static string NewFolder(string sourceName, string logFile)
        {           
            string newFolder = Path.Combine(Directory.GetCurrentDirectory(), sourceName);
            try
            {
                Directory.CreateDirectory(newFolder);
                WriteToLogFile(logFile, "CREATE - " + newFolder);

                return newFolder;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An ERROR occurred: {ex.Message}");
                WriteToLogFile(logFile, $"An ERROR occurred: {ex.Message}");    //usun?
                return ex.Message;
            }
        }

        private static void TimerTasks(object sender, ElapsedEventArgs e) {
           
            FoldersSynchro(Program.userdata.Source, Program.userdata.Replica, Program.userdata.LogFile);
            ReplicaCleaning(Program.userdata.Source, Program.userdata.Replica, Program.userdata.LogFile);

        }

        private static void FoldersSynchro(string source, string replica, string logFile)
        {
            var elements = Directory.EnumerateFileSystemEntries(source);
            string initialText = Path.GetFileName(source);    
            foreach (string element in elements)
            {
                if (Directory.Exists(element))
                {
                    initialText = Path.GetFileName(element);
                    FoldersSynchro(element, replica + "\\" + initialText, logFile);
                }
                else if (File.Exists(element))
                {
                    string cutoutFragment = "";
                    initialText = Path.GetFileName(element);
                    cutoutFragment = initialText;

                    if (File.Exists(replica + "\\" + cutoutFragment)) 
                    {
                        if (!getMD5(element).Equals(getMD5(replica + "\\" + cutoutFragment)))  
                            CopyFile(element, replica + "\\" + cutoutFragment, logFile);
                    }
                    else
                    {
                        CreateFile(element, replica + "\\" + cutoutFragment, logFile);
                    }
                }
            }
        }

        private static void DeleteFile(string sourcePath, string fileName, string logFile)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    File.Delete(sourcePath);
                    WriteToLogFile(logFile, $"DELETE - {sourcePath}");
                }
                else
                {
                    WriteToLogFile(logFile, $"DELETE-ERROR - {sourcePath}");
                }
            }
            catch (Exception ex)
            {
                WriteToLogFile(logFile, $"DELETE-ERROR - {ex.Message}");
            }
        }

        private static void CopyFile(string sourcePath, string targetPath, string logFile)
        {
            try
            {
                File.Copy(sourcePath, targetPath, overwrite: true);
                Console.WriteLine($"COPY - {targetPath}");
                WriteToLogFile(logFile, $"COPY - {targetPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"COPY-ERROR - {ex.Message}");
                WriteToLogFile(logFile, $"COPY-ERROR - {ex.Message}");
            }
        }

        private static void CreateFile(string sourcePath, string targetPath, string logFile)
        {
            try
            {
                string katalogDocelowy = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(katalogDocelowy))
                {
                    Directory.CreateDirectory(katalogDocelowy);
                }

                File.Copy(sourcePath, targetPath, overwrite: true);
                WriteToLogFile(logFile, $"CREATE - {targetPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"COPY-ERROR - {ex.Message}");
                WriteToLogFile(logFile, $"COPY-ERROR - {ex.Message}");
            }
        }

        private static string getMD5(string filePath)
        {
            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    using (MD5 md5 = MD5.Create())
                    {
                        byte[] hash = md5.ComputeHash(stream);

                        StringBuilder wynik = new StringBuilder();
                        foreach (byte b in hash)
                        {
                            wynik.Append(b.ToString("x2"));
                        }
                        return wynik.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MD5-ERROR - {ex.Message}");
                return ex.Message; 
            }
        }
    }
}
