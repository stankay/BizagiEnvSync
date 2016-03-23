using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BizAgi.Commons.Collections;
using BizAgi.Commons.ProgressStatus;
using BizAgi.MD.MetadataManagement.ExportMetadata;
using BizAgi.MD.MetadataManagement.ImportMetadata;
using BizAgi.PL.Entities.Validations.Advise;
using BizAgi.Publication.Support;
using BizAgi.Publication.Util;
using BizAgi.Publication.Util.Dependencies;
using BizAgi.Publication.Util.AdvancedOptions.ExportMetadata;
using Vision.DA;

namespace BizagiEnvSync
{
    /// <summary>
    /// Mode of functioning
    /// </summary>
    enum Mode {
        EXPORT, IMPORT, CLEANUP, NONE
    }

    /// <summary>
    /// CLI-based Utility used for synchronization of the BizAgi 9.1.x environments.
    /// The synchronization consists of two parts
    /// 1. Export of metadata from source environment
    /// 2. Comparison of source and destination metadata and generation of diff SQL scripts
    /// 
    /// Additionally there is a CLEANUP action that removes old generated files
    /// </summary>
    class BizagiEnvSync
    {
        private const int DAYS_TO_KEEP = 20;
        static CConfiguration dbConfig;
        static string config;
        static string bexFile;
        static string destinationDir;
        static Mode mode = Mode.NONE;
        static string destinationPrefix;
        static string cleanupDir;

        static void Main(string[] args)
        {

            var vole = "BizAgi.PL.Entities.Implementations.dll";
            string text = Environment.CommandLine.Remove(Environment.CommandLine.Length - 1, 1).Remove(0, 1);
            int num = text.LastIndexOf("\\");
            text = text.Remove(num + 1) + vole;
            Log(text);

            ValidateArgs(args);

            Log("START");

            switch (mode) {
                case Mode.EXPORT:
                    RunExport();
                    break;
                case Mode.IMPORT:
                    RunImport();
                    break;
                case Mode.CLEANUP:
                    RunCleanup();
                    break;
                default:
                    Console.WriteLine("No functionality to be run, exiting");
                    Environment.Exit(0);
                    break;
            }
            Log("END");
        }

        private static void RunImport()
        {
            Log("Input BEX is " + bexFile);
            Log("Target database is " + dbConfig.Server + "/" + dbConfig.Database);
            Log("SQL scripts will be saved to " + destinationDir + "\\" + destinationPrefix + "\\*.sql");
            Log("---");

            Log("Loading BEX");
            var MtdSubSetSource = CMetadataSubSet.ReadFromFile(bexFile, null);
            Log("Loading BEX done");

            //niekam ho narvi
            Log("Running analysis");
            ProcessOverwriteImporter_EditableDiff poi = new ProcessOverwriteImporter_EditableDiff(null, null, null, new BizAgi.Publication.Support.CRowExclusionSet(), new BizAgi.Publication.Support.CRowExclusionSet(), null);
            IDataProvider DataProvider = CUtil.CreateNewDataProvider(CUtil.DefaultConfiguration);
            poi.Progress = new MyProgressStatus();
            poi.ImportWfOverWrite(MtdSubSetSource, DataProvider, true);

            Log("Fetch differences");
            var MtdDifferencesEditable = new CMetadataDifferencesEditable(poi.Differences);

            Log("Saving SQL scripts");
            MtdDifferencesEditable.MetadataDifferences.SqlScriptSet.SaveScriptsToDirectory(destinationDir + "\\" + destinationPrefix);
        }

        private static void RunExport() {

            Log("Target BEX is " + bexFile);
            Log("Target database is " + dbConfig.Server + "/" + dbConfig.Database);
            Log("---");

            //string connectString = System.Configuration.ConfigurationSettings.AppSettings.Get("DSNDB");

            Log("Prepare configuration");
            var ec = new ProcessExportConfiguration();
            ec.DatabaseConfiguration = dbConfig;
            ec.ExportFilePath = new FileInfo(bexFile).FullName;
            ec.AdvancedOptions = new ExportImportDeploymentAdvancedOptions();
            ec.AdvancedOptions.AllOptions.SelectDefaults();

            //AdvancedOptionsFactory.GetInstanceByClassName("BizAgi.Publication.Util.CDeploymentAdvancedOptions");
            ec.WorkflowsIdsToExport = GetWorkflowIds();
            ExportedProcesses eps = new ExportedProcesses();

            //vykonaj export
            Log("Export Starts");
            ExportProcesses(ec, new MyProgressStatus(), out eps);
            Log("Export End");
        }

        /// <summary>
        /// Validate passed command-line arguments
        /// </summary>
        /// <param name="args"></param>
        private static void ValidateArgs(string[] args)
        {
            try
            {
                mode = (Mode)Enum.Parse(typeof(Mode), args[0]);
            }
            catch (Exception e) {
                PrintUsage();
                Environment.Exit(-3);
            }

            switch (mode) {
                case Mode.EXPORT:
                    ValidateArgsEXPORT(args);
                    ValidateArgsConfig(args);
                    break;
                case Mode.IMPORT:
                    ValidateArgsIMPORT(args);
                    ValidateArgsConfig(args);
                    break;
                case Mode.CLEANUP:
                    ValidateArgsCLEANUP(args);
                    break;    
            }
        }

        private static void ValidateArgsConfig(string[] args)
        {
            config = args[1];

            if (File.Exists(config))
            {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", config);
                dbConfig = CUtil.DefaultConfiguration;
            }
            else
            {
                Console.Error.WriteLine("Config file does not exist: " + config);
                Environment.Exit(-1);
            }

        }

        private static void ValidateArgsIMPORT(string[] args)
        {
            if (args.Length != 5)
            {
                PrintUsage();
                Environment.Exit(0);
            }

            bexFile = args[2];
            var bexFileInfo = new FileInfo(bexFile);

            if (!File.Exists(bexFileInfo.FullName))
            {
                Console.Error.WriteLine("BEX input file does not exist: " + bexFile);
                Environment.Exit(-4);
            }

            destinationDir = new DirectoryInfo(args[3]).FullName;

            if (!Directory.Exists(destinationDir))
            {
                Console.Error.WriteLine("Destination directory for SQL scripts does not exist, create one: " + destinationDir);
                Environment.Exit(-2);
            }

            destinationPrefix = args[4];
            string desiredPattern = @"^[a-zA-Z0-9_\-\.]+$";
            if (!Regex.IsMatch(destinationPrefix, desiredPattern))
            {
                Console.Error.WriteLine("SQL filename prefix must match " + desiredPattern + ": " + destinationPrefix);
                Environment.Exit(-5);
            }
        }

        private static void ValidateArgsEXPORT(string[] args)
        {
            if (args.Length != 3)
            {
                PrintUsage();
                Environment.Exit(0);
            }

            bexFile = args[2].Replace("/", "\\");

            var bexFileInfo = new FileInfo(bexFile);

            if (!Directory.Exists(bexFileInfo.Directory.FullName))
            {
                Console.Error.WriteLine("Directory for BEX file does not exist, create one: " + bexFileInfo.Directory.FullName);
                Environment.Exit(-2);
            }
        }

        private static void ValidateArgsCLEANUP(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
                Environment.Exit(0);
            }

            cleanupDir = args[1];

            if (!Directory.Exists(cleanupDir))
            {
                Console.Error.WriteLine("Directory to be cleaned up does not exist: " + cleanupDir);
                Environment.Exit(-6);
            }
        }

        private static CList<int> GetWorkflowIds() {

            //int[] ids = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 27, 38, 49, 60, 71, 82, 93, 104, 115, 126, 137, 148, 149, 150, 161, 172, 183, 184, 195, 206, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 239, 250, 261, 262, 273, 284, 295, 306, 317, 328, 339, 350, 361, 372, 383, 384, 385, 386, 397, 408, 419, 430, 441, 452, 453, 464, 475, 476, 477, 488, 499, 510, 521, 532, 533, 534, 535, 546, 557, 568, 579, 590, 591, 592, 603, 614, 615, 626, 627, 628, 629, 640, 641, 642, 643, 644, 645, 656, 657, 658, 659, 660, 661 };
            //var output = new BizAgi.Commons.Collections.CList<int>(ids);

            var output = new BizAgi.Commons.Collections.CList<int>();
            var provider = CUtil.DefaultDataProviderValidated;
            var dataset = provider.RunQuery("SELECT idWorkflow FROM WORKFLOW", "WORKFLOW");
            var dr = dataset.Tables[0];

            foreach (DataRow row in dr.Rows) {
                output.Add((int)row["idWorkflow"]);
            }

            return output;
        }

        private static void ExportProcesses(ProcessExportConfiguration exportConfiguration, IProgressStatus progressStatus, out ExportedProcesses exportResults)
        {
            exportConfiguration.WorkflowsIdsToExport = CSubWorkFlowFinder.GetRecursiveSubWorkFlows(exportConfiguration.WorkflowsIdsToExport, exportConfiguration.AdvancedOptions);
            List<Guid> list = new List<Guid>();
            foreach (int current in exportConfiguration.WorkflowsIdsToExport)
            {
                Guid objectID = CDependenciesUtil.GetObjectID(BizagiEnvSync.dbConfig, "WORKFLOW", current);
                list.Add(objectID);
            }
            CMetadataSubSet cMetadataSubSet = CMetadataSubSet.BuildMetadataSubset(exportConfiguration.DatabaseConfiguration, 
                list, exportConfiguration.AdvancedOptions, 
                progressStatus);
            exportResults = new ExportedProcesses();
            exportResults.AdviseErrors = new AdviseDictionary();
            exportResults.AdviseErrors.AddRange(cMetadataSubSet.Advises.Values);
            exportResults.AdviseErrors.AddRange(cMetadataSubSet.BizagiCatalog.Advises.Values);
            exportResults.AdviseErrors.Add(new Error("error"));
            exportResults.ExportedMetadataSubSet = cMetadataSubSet;
            cMetadataSubSet.SaveToFile(exportConfiguration.ExportFilePath);
        }

        /// <summary>
        /// Output the message accompanied with date, time to the Console
        /// </summary>
        /// <param name="message">text message to be logged</param>
        private static void Log(Object message) {
            DateTime dt = DateTime.Now;
            Console.WriteLine("{0} INFO {1}", dt.ToString("HH:mm:ss.fff"), message);
        }

        /// <summary>
        /// Print usage options to stdout
        /// </summary>
        private static void PrintUsage() {
            Console.Error.WriteLine("Usage: BizagEnvSync EXPORT [config file] [BEX file name, use forward slashes]");
            Console.Error.WriteLine("   or: BizagEnvSync IMPORT [config file] [input BEX] [SQL scripts output dir] [SQL scripts filename prefix]");
            Console.Error.WriteLine("   or: BizagEnvSync CLEANUP [path to cleanup]");
        }

        /// <summary>
        /// Remove all files from passed directory that are older than DAYS_TO_KEEP days.
        /// </summary>
        private static void RunCleanup()
        {
            var tenDaysAgo = DateTime.Now.AddDays(-DAYS_TO_KEEP);

            foreach (string fileName in Directory.EnumerateFiles(cleanupDir)) {
                var ctime = new FileInfo(fileName).LastWriteTime;// .CreationTime;

                if (ctime < tenDaysAgo) {
                    Log("Deleting file older than 10 days (ctime "+ ctime + "): " + fileName);
                    File.Delete(fileName);
                }
            }
        }
    }
}
