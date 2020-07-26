using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DCSF18ALE47Programmer.Properties;


/* Hello and welcome to the DCS CMS Editor (Countermeasures Editor) (name not yet finalized)
 * This utility will be able to load, modify, and export various countermeasure programs for
 * DCS aircraft like the F/A-18C and F-16C (hopefully). This program are for prople who
 * can't wait for the ED DCS implementation of the DTC features that you can find on the JF-17.
 * 
 * 
 * Tasks Competed:
 * -Made general GUI
 * -Did some other stuff
 * -F18 import and export
 * -F16 import and export
 * 
 * 
 * TODO:
 * -Make video (maybe)
 * -Add instructions on how to add more aircraft
 *
 *
 * Bugs:
 * -When the dots in the .lua are replaced by commas (eu standard?) the program bugs out (maybe)
 * 
 * 
 * Countermeasure Lua locations for DCS aircraft and other helpful paths
 * -\DCS World OpenBeta\Mods\aircraft\F-16C\Cockpit\Scripts\EWS\CMDS\device\CMDS_ALE47.lua
 * -\DCS World OpenBeta\Mods\aircraft\FA-18C\Cockpit\Scripts\TEWS\device\CMDS_ALE47.lua
 * -\DCS World OpenBeta\Mods\aircraft\F-5E\Cockpit\Scripts\Systems\AN_ALE40V.lua
 * -\DCS World OpenBeta\Mods\aircraft\M-2000C\Cockpit\Scripts\SPIRALE.lua
 * -\DCS World OpenBeta\Mods\aircraft\A-10C\Cockpit\Scripts\AN_ALE40V\device\AN_ALE40V_params.lua
 * -\DCS World OpenBeta\Mods\aircraft\AV8BNA\Cockpit\Scripts\EWS\EW_Dispensers_init.lua
 * -\DCS World OpenBeta\bin\DCS.exe
 * 
 * 
 * Version Notes:
 * v1
 * -Added DCS F-18C
 * -Release
 * -About 1000 lines of code 
 * 
 * v2
 * -Added DCS F-16C
 * -CMS export is enabled even if default aircraft directory is not detected
 * -Added option to re-create orginal DCS CMS lua
 * -Adjusted numerical box max, min, and adjustment values to match aircraft
 * -Replaced sliders with numerical boxes
 * -Adjusted GUI
 * -About 2700 lines of code
 * 
 * 
 * Research:
 * https://stackoverflow.com/questions/17483563/c-sharp-reset-textbox-to-default-value
 */

namespace DCSF18ALE47Programmer
{
    public partial class Form1 : Form
    {
        string appPath = Application.StartupPath;//path of this program exe
        int posOfCMDSIndex;//whats this for?


        public Form1()
        {
            InitializeComponent();

            //sets the cultire to Englush United States so that the decimals are exported as "." instead of ","
            //https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentculture?view=netcore-3.1
            //https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo?view=netcore-3.1
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            CultureInfo.CurrentUICulture = new CultureInfo("en-US", false);

            //CultureInfo.CurrentCulture = new CultureInfo("fr-FR", false);
            //CultureInfo.CurrentUICulture = new CultureInfo("fr-FR", false);

            warningMessage();//displays disclamer/warning message
            //these set the labels and labelBoxes to empty strings to be filled later
            label_DCS_Path2.Text = "";
            label_backupPath2.Text = "";
            textBox_DcsPath.Text = "";
            textBox_backupPath.Text = "";

            isExportEnabled = false;//default state of the export button because the program was just loaded

            if (File.Exists(appPath + "\\DCS-CMS-Editor-Backup\\DCS-CMS-Editor-UserSettings.txt"))//if there is already a backup file
            {
                selectedPath_dcsExe = System.IO.File.ReadAllText(appPath + "\\DCS-CMS-Editor-Backup\\DCS-CMS-Editor-UserSettings.txt");//set the location of the DCS.exe as a string
                dcs_topFolderPath = selectedPath_dcsExe.Remove(selectedPath_dcsExe.Length - 12);//remove the bin and exe so that you get to the top folder
                exportPathBackup = (appPath + "\\DCS-CMS-Editor-Backup\\");//string the location for the backup
                isExportEnabled = true;//enable export

                //prints the location of the DCS path from the backup file and the location of the current backup file
                cmdsLua_F18C_fullPath = dcs_topFolderPath + @"\Mods\aircraft\FA-18C\Cockpit\Scripts\TEWS\device\CMDS_ALE47.lua";
                cmdsLua_F16C_fullPath = dcs_topFolderPath + @"\Mods\aircraft\F-16C\Cockpit\Scripts\EWS\CMDS\device\CMDS_ALE47.lua";
                cmdsLua_F18C_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\FA-18C\Cockpit\Scripts\TEWS\device";
                cmdsLua_F16C_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\F-16C\Cockpit\Scripts\EWS\CMDS\device";


                textBox_DcsPath.Text = dcs_topFolderPath;
                textBox_backupPath.Text = appPath + "\\DCS-CMS-Editor-Backup";
                exportPathMain = selectedPath_dcsExe;
                exportPathBackup = (appPath + "\\DCS-CMS-Editor-Backup");
                exportPathBackup_F18C = (exportPathBackup + "\\F18C Backup");
                exportPathBackup_F16C = (exportPathBackup + "\\F16C Backup");

                //also, load the dcs CMS settings as default
                //loadCM_DCS_Click();
            }
        }

        string selectedFileName;//this is case sensitive. this means that if there is a different in capatialization in the file directory, it will fail and think its wrong
        string exportPathMain;
        string exportPathBackup;
        string exportPathBackup_F18C;
        string exportPathBackup_F16C;
        bool isExportEnabled;
        bool isExportPathSelected;
        string selectedPath_dcsExe;
        string cmdsLua_F18C_fullPath;
        string cmdsLua_F16C_fullPath;
        string cmdsLua_F18C_FolderPath;
        string cmdsLua_F16C_FolderPath;
        string dcs_topFolderPath;

        private void button2_Click(object sender, EventArgs e)
        {
            //setLuaLocation();
            //https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/how-to-open-files-using-the-openfiledialog-component

            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            //openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.InitialDirectory = @"C:\Program Files\Eagle Dynamics\DCS World OpenBeta\bin\DCS.exe";
            openFileDialog1.Filter = "DCS .exe|*.exe";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Title = "Select DCS.exe (Example: C:\\Program Files\\Eagle Dynamics\\DCS World OpenBeta\\bin\\DCS.exe)";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                selectedPath_dcsExe = openFileDialog1.FileName;
                //...

                //http://csharp.net-informations.com/string/csharp-string-contains.htm
                if (selectedPath_dcsExe.Contains(@"bin\DCS.exe") == true)
                {
                    button_export.Enabled = true;
                    dcs_topFolderPath = selectedPath_dcsExe.Remove(selectedPath_dcsExe.Length - 12);//https://stackoverflow.com/questions/15564944/remove-the-last-three-characters-from-a-string/15564958
                    cmdsLua_F18C_fullPath = dcs_topFolderPath + @"\Mods\aircraft\FA-18C\Cockpit\Scripts\TEWS\device\CMDS_ALE47.lua";
                    cmdsLua_F16C_fullPath = dcs_topFolderPath + @"\Mods\aircraft\F-16C\Cockpit\Scripts\EWS\CMDS\device\CMDS_ALE47.lua";
                    cmdsLua_F18C_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\FA-18C\Cockpit\Scripts\TEWS\device";
                    cmdsLua_F16C_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\F-16C\Cockpit\Scripts\EWS\CMDS\device";

                    textBox_DcsPath.Text = dcs_topFolderPath;
                    textBox_backupPath.Text = appPath + "\\DCS-CMS-Editor-Backup";
                    exportPathMain = selectedPath_dcsExe;
                    exportPathBackup = (appPath + "\\DCS-CMS-Editor-Backup");
                    exportPathBackup_F18C = (exportPathBackup + "\\F18C Backup");
                    exportPathBackup_F16C = (exportPathBackup + "\\F16C Backup");

                    MessageBox.Show("You selected " + selectedPath_dcsExe + "\r\n"
                       + "\r\n"
                       + "F-18C lua should be located here: " + cmdsLua_F18C_fullPath + "\r\n"
                       + "\r\n"
                       + "F-16C lua should be located here: " + cmdsLua_F16C_fullPath + "\r\n"
                       + "\r\n"
                       + "Export is enabled. Backup folder has been created.");

                    isExportEnabled = true;
                    isExportPathSelected = true;

                    //https://www.c-sharpcorner.com/UploadFile/mahesh/add-remove-replace-strings-in-C-Sharp/
                    // this first part of the IF is kinda useless.
                    posOfCMDSIndex = selectedPath_dcsExe.IndexOf("bin\\DCS.exe");

                    loadAllCmsAfterUserSelectedExe();//loads the countermeasure files that the user has and the program supports

                    if (posOfCMDSIndex >= 0)
                    {
                        //loadCM_DCS_Click();//loads the cms file the user just selected
                        try
                        {
                            // Determine whether the directory exists.
                            if (Directory.Exists(appPath + "\\DCS-CMS-Editor-Backup"))
                            {
                                System.IO.File.WriteAllText(appPath + "\\DCS-CMS-Editor-Backup\\DCS-CMS-Editor-UserSettings.txt", selectedPath_dcsExe);//this saves the DCS location in the backup location
                                return;
                            }
                            // Try to create the directory.
                            DirectoryInfo di = Directory.CreateDirectory(appPath + "\\DCS-CMS-Editor-Backup");
                            System.IO.File.WriteAllText(appPath + "\\DCS-CMS-Editor-Backup\\DCS-CMS-Editor-UserSettings.txt", selectedPath_dcsExe);//this saves the DCS location in the backup location
                        }
                        catch (Exception)
                        {
                        }
                        finally { }
                    }
                }
                
                else
                {
                    MessageBox.Show("You have not selected the correct file. Please try again." + "\r\n" + "Export is disabled.");
                    selectedPath_dcsExe = "";
                    button_export.Enabled = false;
                    isExportEnabled = false;
                    label_DCS_Path2.Text = "";
                    label_backupPath2.Text = "";
                    textBox_DcsPath.Text = "";
                    textBox_backupPath.Text = "";
                }
            }
        }

        private void setLuaLocation()
        {
        }


        private void button4_Click(object sender, EventArgs e)//load from backup
        {
            //LoadCountermeasure file button TODO: fix this to accomidate multiple aircraft luas.
            if(File.Exists(appPath + "\\DCS-CMS-Editor-Backup\\DCS-CMS-Editor-UserSettings.txt"))//if the user settings have been set continue
            {
                if (tabControl_mainTab.SelectedTab == tabPage1)//this is the f18 tab
                {
                    if (File.Exists(appPath + "\\DCS-CMS-Editor-Backup\\F18C Backup\\CMDS_ALE47.lua"))
                    {
                        loadLocation = (appPath + "\\DCS-CMS-Editor-Backup\\F18C Backup\\CMDS_ALE47.lua");
                        loadLua_F18C();
                    }
                    else
                    {
                        MessageBox.Show("Backup file not found. Please select your DCS.exe and export to generate a backup file.");
                    }
                    
                }else if (tabControl_mainTab.SelectedTab == tabPage2)
                {
                    if (File.Exists(appPath + "\\DCS-CMS-Editor-Backup\\F16C Backup\\CMDS_ALE47.lua"))
                    {
                        loadLocation = (appPath + "\\DCS-CMS-Editor-Backup\\F16C Backup\\CMDS_ALE47.lua");
                        loadLua_F16C();
                    }
                    else
                    {
                        MessageBox.Show("Backup file not found. Please select your DCS.exe and export to generate a backup file.");
                    }
                }
            }
            else
            {
                MessageBox.Show("Backup file not found. Please select your DCS.exe and export to generate a backup file.");
            }
        }
        
        string loadLocation;
        private void loadLua_F18C()
        {
            //find the lua file
            //string F18CountermeasureFileString = @"G:\Games\DCS World OpenBeta\Mods\aircraft\FA-18C\Cockpit\Scripts\TEWS\device\CMDS_ALE47.lua";//hardcoded for now
            string F18CountermeasureFileString = loadLocation;
            //load the text into a string
            string F18CountermeasureFileStringText = File.ReadAllText(F18CountermeasureFileString);
            //https://www.techiedelight.com/read-entire-file-to-string-csharp/

            //MANUAL 1 Values get
            //find the number of chaff
            int F18C_manual1ChaffIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_1][\"chaff\"] = ");
            int F18C_manual1ChaffIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_1][\"flare\"] = ") - 40;//gets the index of the next
            string F18C_manual1ChaffAmount = F18CountermeasureFileStringText.Substring(F18C_manual1ChaffIndex + 40, F18C_manual1ChaffIndexEnd - F18C_manual1ChaffIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual1ChaffAmount);

            int F18C_manual1FlareIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_1][\"flare\"] = ");
            int F18C_manual1FlareIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_1][\"intv\"]  = ") - 40;//gets the index of the next
            string F18C_manual1FlareAmount = F18CountermeasureFileStringText.Substring(F18C_manual1FlareIndex + 40, F18C_manual1FlareIndexEnd - F18C_manual1FlareIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual1FlareAmount);

            int F18C_manual1IntervalIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_1][\"intv\"]  = ");
            int F18C_manual1IntervalIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_1][\"cycle\"] = ") - 40;//gets the index of the next
            string F18C_manual1IntervalAmount = F18CountermeasureFileStringText.Substring(F18C_manual1IntervalIndex + 40, F18C_manual1IntervalIndexEnd - F18C_manual1IntervalIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual1IntervalAmount);

            int F18C_manual1cycleIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_1][\"cycle\"] = ");
            int F18C_manual1cycleIndexEnd = F18CountermeasureFileStringText.IndexOf("-- MAN 2") - 40;//gets the index of the next. remember to change to MAN 3, 4 etc.
            string F18C_manual1cycleAmount = F18CountermeasureFileStringText.Substring(F18C_manual1cycleIndex + 40, F18C_manual1cycleIndexEnd - F18C_manual1cycleIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual1cycleAmount);
            //be carefull with cycle because it can either be 1 length or two length
            //System.Windows.Forms.MessageBox.Show("Press \"OK\" to delete your entire harddrive");


            //MANUAL 2 Values get
            //find the number of chaff
            int F18C_manual2ChaffIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_2][\"chaff\"] = ");
            int F18C_manual2ChaffIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_2][\"flare\"] = ") - 40;//gets the index of the next
            string F18C_manual2ChaffAmount = F18CountermeasureFileStringText.Substring(F18C_manual2ChaffIndex + 40, F18C_manual2ChaffIndexEnd - F18C_manual2ChaffIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual2ChaffAmount);

            int F18C_manual2FlareIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_2][\"flare\"] = ");
            int F18C_manual2FlareIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_2][\"intv\"]  = ") - 40;//gets the index of the next
            string F18C_manual2FlareAmount = F18CountermeasureFileStringText.Substring(F18C_manual2FlareIndex + 40, F18C_manual2FlareIndexEnd - F18C_manual2FlareIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual2FlareAmount);

            int F18C_manual2IntervalIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_2][\"intv\"]  = ");
            int F18C_manual2IntervalIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_2][\"cycle\"] = ") - 40;//gets the index of the next
            string F18C_manual2IntervalAmount = F18CountermeasureFileStringText.Substring(F18C_manual2IntervalIndex + 40, F18C_manual2IntervalIndexEnd - F18C_manual2IntervalIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual2IntervalAmount);

            int F18C_manual2cycleIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_2][\"cycle\"] = ");
            int F18C_manual2cycleIndexEnd = F18CountermeasureFileStringText.IndexOf("-- MAN 3") - 40;//gets the index of the next. remember to change to MAN 3, 4 etc.
            string F18C_manual2cycleAmount = F18CountermeasureFileStringText.Substring(F18C_manual2cycleIndex + 40, F18C_manual2cycleIndexEnd - F18C_manual2cycleIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual2cycleAmount);
            //be carefull with cycle because it can either be 1 length or two length


            //MANUAL 3 Values get
            //find the number of chaff
            int F18C_manual3ChaffIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_3][\"chaff\"] = ");
            int F18C_manual3ChaffIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_3][\"flare\"] = ") - 40;//gets the index of the next
            string F18C_manual3ChaffAmount = F18CountermeasureFileStringText.Substring(F18C_manual3ChaffIndex + 40, F18C_manual3ChaffIndexEnd - F18C_manual3ChaffIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual3ChaffAmount);

            int F18C_manual3FlareIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_3][\"flare\"] = ");
            int F18C_manual3FlareIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_3][\"intv\"]  = ") - 40;//gets the index of the next
            string F18C_manual3FlareAmount = F18CountermeasureFileStringText.Substring(F18C_manual3FlareIndex + 40, F18C_manual3FlareIndexEnd - F18C_manual3FlareIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual3FlareAmount);

            int F18C_manual3IntervalIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_3][\"intv\"]  = ");
            int F18C_manual3IntervalIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_3][\"cycle\"] = ") - 40;//gets the index of the next
            string F18C_manual3IntervalAmount = F18CountermeasureFileStringText.Substring(F18C_manual3IntervalIndex + 40, F18C_manual3IntervalIndexEnd - F18C_manual3IntervalIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual3IntervalAmount);

            int F18C_manual3cycleIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_3][\"cycle\"] = ");
            int F18C_manual3cycleIndexEnd = F18CountermeasureFileStringText.IndexOf("-- MAN 4") - 40;//gets the index of the next. remember to change to MAN 3, 4 etc.
            string F18C_manual3cycleAmount = F18CountermeasureFileStringText.Substring(F18C_manual3cycleIndex + 40, F18C_manual3cycleIndexEnd - F18C_manual3cycleIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual3cycleAmount);
            //be carefull with cycle because it can either be 1 length or two length


            //MANUAL 4 Values get
            //find the number of chaff
            int F18C_manual4ChaffIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_4][\"chaff\"] = ");
            int F18C_manual4ChaffIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_4][\"flare\"] = ") - 40;//gets the index of the next
            string F18C_manual4ChaffAmount = F18CountermeasureFileStringText.Substring(F18C_manual4ChaffIndex + 40, F18C_manual4ChaffIndexEnd - F18C_manual4ChaffIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual4ChaffAmount);

            int F18C_manual4FlareIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_4][\"flare\"] = ");
            int F18C_manual4FlareIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_4][\"intv\"]  = ") - 40;//gets the index of the next
            string F18C_manual4FlareAmount = F18CountermeasureFileStringText.Substring(F18C_manual4FlareIndex + 40, F18C_manual4FlareIndexEnd - F18C_manual4FlareIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual4FlareAmount);

            int F18C_manual4IntervalIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_4][\"intv\"]  = ");
            int F18C_manual4IntervalIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_4][\"cycle\"] = ") - 40;//gets the index of the next
            string F18C_manual4IntervalAmount = F18CountermeasureFileStringText.Substring(F18C_manual4IntervalIndex + 40, F18C_manual4IntervalIndexEnd - F18C_manual4IntervalIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual4IntervalAmount);

            int F18C_manual4cycleIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_4][\"cycle\"] = ");
            int F18C_manual4cycleIndexEnd = F18CountermeasureFileStringText.IndexOf("-- MAN 5") - 40;//gets the index of the next. remember to change to MAN 3, 4 etc.
            string F18C_manual4cycleAmount = F18CountermeasureFileStringText.Substring(F18C_manual4cycleIndex + 40, F18C_manual4cycleIndexEnd - F18C_manual4cycleIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual4cycleAmount);
            //be carefull with cycle because it can either be 1 length or two length


            //MANUAL 5 Values get
            //find the number of chaff
            int F18C_manual5ChaffIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_5][\"chaff\"] = ");
            int F18C_manual5ChaffIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_5][\"flare\"] = ") - 40;//gets the index of the next
            string F18C_manual5ChaffAmount = F18CountermeasureFileStringText.Substring(F18C_manual5ChaffIndex + 40, F18C_manual5ChaffIndexEnd - F18C_manual5ChaffIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual5ChaffAmount);

            int F18C_manual5FlareIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_5][\"flare\"] = ");
            int F18C_manual5FlareIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_5][\"intv\"]  = ") - 40;//gets the index of the next
            string F18C_manual5FlareAmount = F18CountermeasureFileStringText.Substring(F18C_manual5FlareIndex + 40, F18C_manual5FlareIndexEnd - F18C_manual5FlareIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual5FlareAmount);

            int F18C_manual5IntervalIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_5][\"intv\"]  = ");
            int F18C_manual5IntervalIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_5][\"cycle\"] = ") - 40;//gets the index of the next
            string F18C_manual5IntervalAmount = F18CountermeasureFileStringText.Substring(F18C_manual5IntervalIndex + 40, F18C_manual5IntervalIndexEnd - F18C_manual5IntervalIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual5IntervalAmount);

            int F18C_manual5cycleIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_5][\"cycle\"] = ");
            int F18C_manual5cycleIndexEnd = F18CountermeasureFileStringText.IndexOf("-- MAN 6") - 40;//gets the index of the next. remember to change to MAN 3, 4 etc.
            string F18C_manual5cycleAmount = F18CountermeasureFileStringText.Substring(F18C_manual5cycleIndex + 40, F18C_manual5cycleIndexEnd - F18C_manual5cycleIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual5cycleAmount);
            //be carefull with cycle because it can either be 1 length or two length


            //MANUAL 6 Values get
            //find the number of chaff
            int F18C_manual6ChaffIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_6][\"chaff\"] = ");
            int F18C_manual6ChaffIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_6][\"flare\"] = ") - 40;//gets the index of the next
            string F18C_manual6ChaffAmount = F18CountermeasureFileStringText.Substring(F18C_manual6ChaffIndex + 40, F18C_manual6ChaffIndexEnd - F18C_manual6ChaffIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual6ChaffAmount);

            int F18C_manual6FlareIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_6][\"flare\"] = ");
            int F18C_manual6FlareIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_6][\"intv\"]  = ") - 40;//gets the index of the next
            string F18C_manual6FlareAmount = F18CountermeasureFileStringText.Substring(F18C_manual6FlareIndex + 40, F18C_manual6FlareIndexEnd - F18C_manual6FlareIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual6FlareAmount);

            int F18C_manual6IntervalIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_6][\"intv\"]  = ");
            int F18C_manual6IntervalIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_6][\"cycle\"] = ") - 40;//gets the index of the next
            string F18C_manual6IntervalAmount = F18CountermeasureFileStringText.Substring(F18C_manual6IntervalIndex + 40, F18C_manual6IntervalIndexEnd - F18C_manual6IntervalIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual6IntervalAmount);

            int F18C_manual6cycleIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.MAN_6][\"cycle\"] = ");
            int F18C_manual6cycleIndexEnd = F18CountermeasureFileStringText.IndexOf("-- Auto presets") - 40;//gets the index of the next. remember to change to MAN 3, 4 etc.
            string F18C_manual6cycleAmount = F18CountermeasureFileStringText.Substring(F18C_manual6cycleIndex + 40, F18C_manual6cycleIndexEnd - F18C_manual6cycleIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_manual6cycleAmount);
            //be carefull with cycle because it can either be 1 length or two length


            //MANUAL F18C_OldSam Values get
            //find the number of chaff
            int F18C_OldSamChaffIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_1][\"chaff\"] = ");
            int F18C_OldSamChaffIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_1][\"flare\"] = ") - 40;//gets the index of the next
            string F18C_OldSamChaffAmount = F18CountermeasureFileStringText.Substring(F18C_OldSamChaffIndex + 40, F18C_OldSamChaffIndexEnd - F18C_OldSamChaffIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_OldSamChaffAmount);

            int F18C_OldSamFlareIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_1][\"flare\"] = ");
            int F18C_OldSamFlareIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_1][\"intv\"]  = ") - 40;//gets the index of the next
            string F18C_OldSamFlareAmount = F18CountermeasureFileStringText.Substring(F18C_OldSamFlareIndex + 40, F18C_OldSamFlareIndexEnd - F18C_OldSamFlareIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_OldSamFlareAmount);

            int F18C_OldSamIntervalIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_1][\"intv\"]  = ");
            int F18C_OldSamIntervalIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_1][\"cycle\"] = ") - 40;//gets the index of the next
            string F18C_OldSamIntervalAmount = F18CountermeasureFileStringText.Substring(F18C_OldSamIntervalIndex + 40, F18C_OldSamIntervalIndexEnd - F18C_OldSamIntervalIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_OldSamIntervalAmount);

            int F18C_OldSamcycleIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_1][\"cycle\"] = ");
            int F18C_OldSamcycleIndexEnd = F18CountermeasureFileStringText.IndexOf("-- Current generation radar SAM") - 40;//gets the index of the next. remember to change to MAN 3, 4 etc.
            string F18C_OldSamcycleAmount = F18CountermeasureFileStringText.Substring(F18C_OldSamcycleIndex + 40, F18C_OldSamcycleIndexEnd - F18C_OldSamcycleIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_OldSamcycleAmount);
            //be carefull with cycle because it can either be 1 length or two length


            //MANUAL F18C_CurrentSam Values get
            //find the number of chaff
            int F18C_CurrentSamChaffIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_2][\"chaff\"] = ");
            int F18C_CurrentSamChaffIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_2][\"flare\"] = ") - 40;//gets the index of the next
            string F18C_CurrentSamChaffAmount = F18CountermeasureFileStringText.Substring(F18C_CurrentSamChaffIndex + 40, F18C_CurrentSamChaffIndexEnd - F18C_CurrentSamChaffIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_CurrentSamChaffAmount);

            int F18C_CurrentSamFlareIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_2][\"flare\"] = ");
            int F18C_CurrentSamFlareIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_2][\"intv\"]  = ") - 40;//gets the index of the next
            string F18C_CurrentSamFlareAmount = F18CountermeasureFileStringText.Substring(F18C_CurrentSamFlareIndex + 40, F18C_CurrentSamFlareIndexEnd - F18C_CurrentSamFlareIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_CurrentSamFlareAmount);

            int F18C_CurrentSamIntervalIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_2][\"intv\"]  = ");
            int F18C_CurrentSamIntervalIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_2][\"cycle\"] = ") - 40;//gets the index of the next
            string F18C_CurrentSamIntervalAmount = F18CountermeasureFileStringText.Substring(F18C_CurrentSamIntervalIndex + 40, F18C_CurrentSamIntervalIndexEnd - F18C_CurrentSamIntervalIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_CurrentSamIntervalAmount);

            int F18C_CurrentSamcycleIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_2][\"cycle\"] = ");
            int F18C_CurrentSamcycleIndexEnd = F18CountermeasureFileStringText.IndexOf("-- IR SAM") - 40;//gets the index of the next. remember to change to MAN 3, 4 etc.
            string F18C_CurrentSamcycleAmount = F18CountermeasureFileStringText.Substring(F18C_CurrentSamcycleIndex + 40, F18C_CurrentSamcycleIndexEnd - F18C_CurrentSamcycleIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_CurrentSamcycleAmount);
            //be carefull with cycle because it can either be 1 length or two length


            //MANUAL F18C_IRSam Values get
            //find the number of chaff
            int F18C_IRSamChaffIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_3][\"chaff\"] = ");
            int F18C_IRSamChaffIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_3][\"flare\"] = ") - 40;//gets the index of the next
            string F18C_IRSamChaffAmount = F18CountermeasureFileStringText.Substring(F18C_IRSamChaffIndex + 40, F18C_IRSamChaffIndexEnd - F18C_IRSamChaffIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_IRSamChaffAmount);

            int F18C_IRSamFlareIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_3][\"flare\"] = ");
            int F18C_IRSamFlareIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_3][\"intv\"]  = ") - 40;//gets the index of the next
            string F18C_IRSamFlareAmount = F18CountermeasureFileStringText.Substring(F18C_IRSamFlareIndex + 40, F18C_IRSamFlareIndexEnd - F18C_IRSamFlareIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_IRSamFlareAmount);

            int F18C_IRSamIntervalIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_3][\"intv\"]  = ");
            int F18C_IRSamIntervalIndexEnd = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_3][\"cycle\"] = ") - 40;//gets the index of the next
            string F18C_IRSamIntervalAmount = F18CountermeasureFileStringText.Substring(F18C_IRSamIntervalIndex + 40, F18C_IRSamIntervalIndexEnd - F18C_IRSamIntervalIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_IRSamIntervalAmount);

            int F18C_IRSamcycleIndex = F18CountermeasureFileStringText.IndexOf("programs[ProgramNames.AUTO_3][\"cycle\"] = ");
            int F18C_IRSamcycleIndexEnd = F18CountermeasureFileStringText.IndexOf("need_to_be_closed") - 40;//gets the index of the next. remember to change to MAN 3, 4 etc.
            string F18C_IRSamcycleAmount = F18CountermeasureFileStringText.Substring(F18C_IRSamcycleIndex + 40, F18C_IRSamcycleIndexEnd - F18C_IRSamcycleIndex);//40 is the length of the Index request
            //System.Windows.Forms.MessageBox.Show("First value Index of 'How' is " + F18C_IRSamcycleAmount);
            //be carefull with cycle because it can either be 1 length or two length


            //change the GUI to match what was imported
            //set Man1 GUI elements
            //make trys and catches for the value bar for weird imports====================================
            //https://docs.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception?view=netcore-3.1
            //try to import the value. If the imported value is out of the set range, make the value the minimum acceptable value
            try{numericUpDown_F18C_manual1Chaff.Value = int.Parse(F18C_manual1ChaffAmount);}
            catch(ArgumentOutOfRangeException){numericUpDown_F18C_manual1Chaff.Value = numericUpDown_F18C_manual1Chaff.Minimum;}

            try
            {
                numericUpDown_F18C_manual1Flare.Value = int.Parse(F18C_manual1FlareAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual1Flare.Value = numericUpDown_F18C_manual1Flare.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual1Interval.Value = decimal.Parse(F18C_manual1IntervalAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual1Interval.Value = numericUpDown_F18C_manual1Interval.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual1Cycle.Value = decimal.Parse(F18C_manual1cycleAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual1Cycle.Value = numericUpDown_F18C_manual1Cycle.Minimum;
            }
            //https://stackoverflow.com/questions/4264736/convert-string-to-decimal-keeping-fractions


            //set Man2 GUI elements
            try
            {
                numericUpDown_F18C_manual2Chaff.Value = int.Parse(F18C_manual2ChaffAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual2Chaff.Value = numericUpDown_F18C_manual2Chaff.Minimum;
            }
            //label_manual2Chaff.Text = numericUpDown_manual2Chaff.Value.ToString();
            try
            {
                numericUpDown_F18C_manual2Flare.Value = int.Parse(F18C_manual2FlareAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual2Flare.Value = numericUpDown_F18C_manual2Flare.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual2Interval.Value = decimal.Parse(F18C_manual2IntervalAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual2Interval.Value = numericUpDown_F18C_manual2Interval.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual2Cycle.Value = decimal.Parse(F18C_manual2cycleAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual2Cycle.Value = numericUpDown_F18C_manual2Cycle.Minimum;
            }

            //set Man3 GUI elements
            try
            {
                numericUpDown_F18C_manual3Chaff.Value = int.Parse(F18C_manual3ChaffAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual3Chaff.Value = numericUpDown_F18C_manual3Chaff.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual3Flare.Value = int.Parse(F18C_manual3FlareAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual3Flare.Value = numericUpDown_F18C_manual3Flare.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual3Interval.Value = decimal.Parse(F18C_manual3IntervalAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual3Interval.Value = numericUpDown_F18C_manual3Interval.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual3Cycle.Value = decimal.Parse(F18C_manual3cycleAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual3Cycle.Value = numericUpDown_F18C_manual3Cycle.Minimum;
            }
            


            //set Man4 GUI elements
            try
            {
                numericUpDown_F18C_manual4Chaff.Value = int.Parse(F18C_manual4ChaffAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual4Chaff.Value = numericUpDown_F18C_manual4Chaff.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual4Flare.Value = int.Parse(F18C_manual4FlareAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual4Flare.Value = numericUpDown_F18C_manual4Flare.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual4Interval.Value = decimal.Parse(F18C_manual4IntervalAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual4Interval.Value = numericUpDown_F18C_manual4Interval.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual4Cycle.Value = decimal.Parse(F18C_manual4cycleAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual4Cycle.Value = numericUpDown_F18C_manual4Cycle.Minimum;
            }
           


            //set Man5 GUI elements
            try
            {
                numericUpDown_F18C_manual5Chaff.Value = int.Parse(F18C_manual5ChaffAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual5Chaff.Value = numericUpDown_F18C_manual5Chaff.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual5Flare.Value = int.Parse(F18C_manual5FlareAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual5Flare.Value = numericUpDown_F18C_manual5Flare.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual5Interval.Value = decimal.Parse(F18C_manual5IntervalAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual5Interval.Value = numericUpDown_F18C_manual5Interval.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual5Cycle.Value = decimal.Parse(F18C_manual5cycleAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual5Cycle.Value = numericUpDown_F18C_manual5Cycle.Minimum;
            }

           
            //set Man6 GUI elements
            try
            {
                numericUpDown_F18C_manual6Chaff.Value = int.Parse(F18C_manual6ChaffAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual6Chaff.Value = numericUpDown_F18C_manual6Chaff.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual6Flare.Value = int.Parse(F18C_manual6FlareAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual6Flare.Value = numericUpDown_F18C_manual6Flare.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual6Interval.Value = decimal.Parse(F18C_manual6IntervalAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual6Interval.Value = numericUpDown_F18C_manual6Interval.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual6Cycle.Value = decimal.Parse(F18C_manual6cycleAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual6Cycle.Value = numericUpDown_F18C_manual6Cycle.Minimum;
            }
            


            //set OldSam GUI elements
            try
            {
                numericUpDown_F18C_manualOldSamChaff.Value = int.Parse(F18C_OldSamChaffAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manualOldSamChaff.Value = numericUpDown_F18C_manualOldSamChaff.Minimum;
            }

            try
            {
                numericUpDown_F18C_manualOldSamFlare.Value = int.Parse(F18C_OldSamFlareAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manualOldSamFlare.Value = numericUpDown_F18C_manualOldSamFlare.Minimum;
            }

            try
            {
                numericUpDown_F18C_OldSamInterval.Value = decimal.Parse(F18C_OldSamIntervalAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_OldSamInterval.Value = numericUpDown_F18C_OldSamInterval.Minimum;
            }

            try
            {
                numericUpDown_F18C_OldSamCycle.Value = decimal.Parse(F18C_OldSamcycleAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_OldSamCycle.Value = numericUpDown_F18C_OldSamCycle.Minimum;
            }
            

            //set CurrentSam GUI elements
            try
            {
                numericUpDown_F18C_manualCurrentSamChaff.Value = int.Parse(F18C_CurrentSamChaffAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manualCurrentSamChaff.Value = numericUpDown_F18C_manualCurrentSamChaff.Minimum;
            }

            try
            {
                numericUpDown_F18C_manualCurrentSamFlare.Value = int.Parse(F18C_CurrentSamFlareAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manualCurrentSamFlare.Value = numericUpDown_F18C_manualCurrentSamFlare.Minimum;
            }

            try{numericUpDown_F18C_CurrentSamInterval.Value = decimal.Parse(F18C_CurrentSamIntervalAmount);}
            catch (ArgumentOutOfRangeException){numericUpDown_F18C_CurrentSamInterval.Value = numericUpDown_F18C_CurrentSamInterval.Minimum;}

            try
            {
                numericUpDown_F18C_CurrentSamCycle.Value = decimal.Parse(F18C_CurrentSamcycleAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_CurrentSamCycle.Value = numericUpDown_F18C_CurrentSamCycle.Minimum;
            }


            //set IRSam GUI elements
            try
            {
                numericUpDown_F18C_manual_IRSamChaff.Value = int.Parse(F18C_IRSamChaffAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual_IRSamChaff.Value = numericUpDown_F18C_manual_IRSamChaff.Minimum;
            }

            try
            {
                numericUpDown_F18C_manual_IRSamFlare.Value = int.Parse(F18C_IRSamFlareAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_manual_IRSamFlare.Value = numericUpDown_F18C_manual_IRSamFlare.Minimum;
            }

            try
            {
                numericUpDown_F18C_IRSamInterval.Value = decimal.Parse(F18C_IRSamIntervalAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_IRSamInterval.Value = numericUpDown_F18C_IRSamInterval.Minimum;
            }

            try
            {
                numericUpDown_F18C_IRSamCycle.Value = decimal.Parse(F18C_IRSamcycleAmount);
            }
            catch (ArgumentOutOfRangeException)
            {
                numericUpDown_F18C_IRSamCycle.Value = numericUpDown_F18C_IRSamCycle.Minimum;
            }
        }



        private void loadLua_F16C()//TODO: do this like the loadLua_F18 method============================================
        {
            //find the lua file
           
            string CountermeasureFileString_F16C = loadLocation;
            //load the text into a string
            string CountermeasureFileStringText_F16C = File.ReadAllText(CountermeasureFileString_F16C);
            //https://www.techiedelight.com/read-entire-file-to-string-csharp/



            //Change MANUAL X --> MANUAL Y
            //Change manualx --> manualy
            //change MAN_X --> MAN_Y

            //MANUAL 1 ChaffBQ Values get
            //find the number of ChaffBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual1ChaffBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty 	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_1]") );
            //MessageBox.Show("First value Index is " + F16C_manual1ChaffBQIndex);//528
            int F16C_manual1ChaffBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual1ChaffBQIndex ) -12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual1ChaffBQIndexEnd);//589
            //MessageBox.Show("Starting at" + (F16C_manual1ChaffBQIndex + 12) + " with a length of " + (F16C_manual1ChaffBQIndexEnd - F16C_manual1ChaffBQIndex));
            string F16C_manual1ChaffBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual1ChaffBQIndex + 12, F16C_manual1ChaffBQIndexEnd - F16C_manual1ChaffBQIndex);//12 is the length of the Index request...or something.
            //MessageBox.Show("Actual String is |" + F16C_manual1ChaffBQAmount + "|");
            //MessageBox.Show("Parsed String is " + F16C_manual1ChaffBQAmount);
            
            //MANUAL 1 ChaffBI Values get
            //find the number of ChaffBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual1ChaffBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_1]") );
            //MessageBox.Show("First value Index is " + F16C_manual1ChaffBIIndex);
            int F16C_manual1ChaffBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual1ChaffBIIndex ) -12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual1ChaffBIIndexEnd);
            string F16C_manual1ChaffBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual1ChaffBIIndex + 12, F16C_manual1ChaffBIIndexEnd - F16C_manual1ChaffBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual1ChaffBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual1ChaffBIAmount));

            //MANUAL 1 ChaffSQ Values get
            //find the number of ChaffSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual1ChaffSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_1]"));
            //MessageBox.Show("First value Index is " + F16C_manual1ChaffSQIndex);
            int F16C_manual1ChaffSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual1ChaffSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual1ChaffSQIndexEnd);
            string F16C_manual1ChaffSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual1ChaffSQIndex + 11, F16C_manual1ChaffSQIndexEnd - F16C_manual1ChaffSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual1ChaffSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual1ChaffSQAmount));

            //MANUAL 1 ChaffSI Values get
            //find the number of ChaffSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual1ChaffSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_1]"));
            //MessageBox.Show("First value Index is " + F16C_manual1ChaffSIIndex);
            int F16C_manual1ChaffSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual1ChaffSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual1ChaffSIIndexEnd);
            string F16C_manual1ChaffSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual1ChaffSIIndex + 12, F16C_manual1ChaffSIIndexEnd - F16C_manual1ChaffSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual1ChaffSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual1ChaffSIAmount));


            //MANUAL 1 FlareBQ Values get
            //find the number of FlareBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual1FlareBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty	= ", F16C_manual1ChaffSIIndexEnd);//i use F16C_manual1ChaffSIIndexEnd because we already know it!
            //MessageBox.Show("First value Index is " + F16C_manual1FlareBQIndex);//
            int F16C_manual1FlareBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual1FlareBQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual1FlareBQIndexEnd);//
            //MessageBox.Show("Starting at" + (F16C_manual1FlareBQIndex + 11) + " with a length of " + (F16C_manual1FlareBQIndexEnd - F16C_manual1FlareBQIndex));
            string F16C_manual1FlareBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual1FlareBQIndex + 11, F16C_manual1FlareBQIndexEnd - F16C_manual1FlareBQIndex);//11 is the length of the Index request...or something.
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual1FlareBQAmount));                                                                                                                                                                      
            //MessageBox.Show("Actual String is |" + F16C_manual1FlareBQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual1FlareBQAmount));

            //MANUAL 1 FlareBI Values get
            //find the number of FlareBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual1FlareBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", F16C_manual1ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual1FlareBIIndex);
            int F16C_manual1FlareBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual1FlareBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual1FlareBIIndexEnd);
            string F16C_manual1FlareBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual1FlareBIIndex + 12, F16C_manual1FlareBIIndexEnd - F16C_manual1FlareBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual1FlareBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual1FlareBIAmount));

            //MANUAL 1 FlareSQ Values get
            //find the number of FlareSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual1FlareSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", F16C_manual1ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual1FlareSQIndex);
            int F16C_manual1FlareSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual1FlareSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual1FlareSQIndexEnd);
            string F16C_manual1FlareSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual1FlareSQIndex + 11, F16C_manual1FlareSQIndexEnd - F16C_manual1FlareSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual1FlareSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual1FlareSQAmount));

            //MANUAL 1 FlareSI Values get
            //find the number of FlareSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual1FlareSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", F16C_manual1ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual1FlareSIIndex);
            int F16C_manual1FlareSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual1FlareSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual1FlareSIIndexEnd);
            string F16C_manual1FlareSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual1FlareSIIndex + 12, F16C_manual1FlareSIIndexEnd - F16C_manual1FlareSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual1FlareSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual1FlareSIAmount));


            //MANUAL 2 ChaffBQ Values get
            //find the number of ChaffBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual2ChaffBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty 	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_2]"));
            //MessageBox.Show("First value Index is " + F16C_manual2ChaffBQIndex);//528
            int F16C_manual2ChaffBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual2ChaffBQIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual2ChaffBQIndexEnd);//534
            //MessageBox.Show("Starting at" + (F16C_manual2ChaffBQIndex + 12) + " with a length of " + (F16C_manual2ChaffBQIndexEnd - F16C_manual2ChaffBQIndex));
            string F16C_manual2ChaffBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual2ChaffBQIndex + 12, F16C_manual2ChaffBQIndexEnd - F16C_manual2ChaffBQIndex);//12 is the length of the Index request...or something.
                     
            //MANUAL 2 ChaffBI Values get
            //find the number of ChaffBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual2ChaffBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_2]"));
            //MessageBox.Show("First value Index is " + F16C_manual2ChaffBIIndex);
            int F16C_manual2ChaffBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual2ChaffBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual2ChaffBIIndexEnd);
            string F16C_manual2ChaffBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual2ChaffBIIndex + 12, F16C_manual2ChaffBIIndexEnd - F16C_manual2ChaffBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual2ChaffBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual2ChaffBIAmount));

            //MANUAL 2 ChaffSQ Values get
            //find the number of ChaffSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual2ChaffSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_2]"));
            //MessageBox.Show("First value Index is " + F16C_manual2ChaffSQIndex);
            int F16C_manual2ChaffSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual2ChaffSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual2ChaffSQIndexEnd);
            string F16C_manual2ChaffSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual2ChaffSQIndex + 11, F16C_manual2ChaffSQIndexEnd - F16C_manual2ChaffSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual2ChaffSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual2ChaffSQAmount));

            //MANUAL 2 ChaffSI Values get
            //find the number of ChaffSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual2ChaffSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_2]"));
            //MessageBox.Show("First value Index is " + F16C_manual2ChaffSIIndex);
            int F16C_manual2ChaffSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual2ChaffSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual2ChaffSIIndexEnd);
            string F16C_manual2ChaffSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual2ChaffSIIndex + 12, F16C_manual2ChaffSIIndexEnd - F16C_manual2ChaffSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual2ChaffSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual2ChaffSIAmount));


            //MANUAL 2 FlareBQ Values get
            //find the number of FlareBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual2FlareBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty	= ", F16C_manual2ChaffSIIndexEnd);//i use F16C_manual2ChaffSIIndexEnd because we already know it!
            //MessageBox.Show("First value Index is " + F16C_manual2FlareBQIndex);//528
            int F16C_manual2FlareBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual2FlareBQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual2FlareBQIndexEnd);//534
            string F16C_manual2FlareBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual2FlareBQIndex + 11, F16C_manual2FlareBQIndexEnd - F16C_manual2FlareBQIndex);//11 is the length of the Index request...or something.
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual2FlareBQAmount));                                                                                                                                                                      //MessageBox.Show("Actual String is |" + F16C_manual2FlareBQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual2FlareBQAmount));

            //MANUAL 2 FlareBI Values get
            //find the number of FlareBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual2FlareBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", F16C_manual2ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual2FlareBIIndex);
            int F16C_manual2FlareBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual2FlareBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual2FlareBIIndexEnd);
            string F16C_manual2FlareBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual2FlareBIIndex + 12, F16C_manual2FlareBIIndexEnd - F16C_manual2FlareBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual2FlareBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual2FlareBIAmount));

            //MANUAL 2 FlareSQ Values get
            //find the number of FlareSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual2FlareSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", F16C_manual2ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual2FlareSQIndex);
            int F16C_manual2FlareSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual2FlareSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual2FlareSQIndexEnd);
            string F16C_manual2FlareSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual2FlareSQIndex + 11, F16C_manual2FlareSQIndexEnd - F16C_manual2FlareSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual2FlareSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual2FlareSQAmount));

            //MANUAL 2 FlareSI Values get
            //find the number of FlareSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual2FlareSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", F16C_manual2ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual2FlareSIIndex);
            int F16C_manual2FlareSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual2FlareSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual2FlareSIIndexEnd);
            string F16C_manual2FlareSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual2FlareSIIndex + 12, F16C_manual2FlareSIIndexEnd - F16C_manual2FlareSIIndex);//12 is the length of the Index request
                         

            //MANUAL 3 ChaffBQ Values get
            //find the number of ChaffBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual3ChaffBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty 	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_3]"));
            //MessageBox.Show("First value Index is " + F16C_manual3ChaffBQIndex);//528
            int F16C_manual3ChaffBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual3ChaffBQIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual3ChaffBQIndexEnd);//534
            string F16C_manual3ChaffBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual3ChaffBQIndex + 12, F16C_manual3ChaffBQIndexEnd - F16C_manual3ChaffBQIndex);//12 is the length of the Index request...or something.

            //MANUAL 3 ChaffBI Values get
            //find the number of ChaffBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual3ChaffBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_3]"));
            //MessageBox.Show("First value Index is " + F16C_manual3ChaffBIIndex);
            int F16C_manual3ChaffBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual3ChaffBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual3ChaffBIIndexEnd);
            string F16C_manual3ChaffBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual3ChaffBIIndex + 12, F16C_manual3ChaffBIIndexEnd - F16C_manual3ChaffBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual3ChaffBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual3ChaffBIAmount));

            //MANUAL 3 ChaffSQ Values get
            //find the number of ChaffSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual3ChaffSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_3]"));
            //MessageBox.Show("First value Index is " + F16C_manual3ChaffSQIndex);
            int F16C_manual3ChaffSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual3ChaffSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual3ChaffSQIndexEnd);
            string F16C_manual3ChaffSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual3ChaffSQIndex + 11, F16C_manual3ChaffSQIndexEnd - F16C_manual3ChaffSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual3ChaffSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual3ChaffSQAmount));

            //MANUAL 3 ChaffSI Values get
            //find the number of ChaffSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual3ChaffSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_3]"));
            //MessageBox.Show("First value Index is " + F16C_manual3ChaffSIIndex);
            int F16C_manual3ChaffSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual3ChaffSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual3ChaffSIIndexEnd);
            string F16C_manual3ChaffSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual3ChaffSIIndex + 12, F16C_manual3ChaffSIIndexEnd - F16C_manual3ChaffSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual3ChaffSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual3ChaffSIAmount));


            //MANUAL 3 FlareBQ Values get
            //find the number of FlareBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual3FlareBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty	= ", F16C_manual3ChaffSIIndexEnd);//i use F16C_manual3ChaffSIIndexEnd because we already know it!
            //MessageBox.Show("First value Index is " + F16C_manual3FlareBQIndex);//528
            int F16C_manual3FlareBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual3FlareBQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual3FlareBQIndexEnd);//534
            string F16C_manual3FlareBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual3FlareBQIndex + 11, F16C_manual3FlareBQIndexEnd - F16C_manual3FlareBQIndex);//11 is the length of the Index request...or something.
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual3FlareBQAmount));                                                                                                                                                                      //MessageBox.Show("Actual String is |" + F16C_manual3FlareBQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual3FlareBQAmount));

            //MANUAL 3 FlareBI Values get
            //find the number of FlareBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual3FlareBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", F16C_manual3ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual3FlareBIIndex);
            int F16C_manual3FlareBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual3FlareBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual3FlareBIIndexEnd);
            string F16C_manual3FlareBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual3FlareBIIndex + 12, F16C_manual3FlareBIIndexEnd - F16C_manual3FlareBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual3FlareBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual3FlareBIAmount));

            //MANUAL 3 FlareSQ Values get
            //find the number of FlareSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual3FlareSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", F16C_manual3ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual3FlareSQIndex);
            int F16C_manual3FlareSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual3FlareSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual3FlareSQIndexEnd);
            string F16C_manual3FlareSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual3FlareSQIndex + 11, F16C_manual3FlareSQIndexEnd - F16C_manual3FlareSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual3FlareSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual3FlareSQAmount));

            //MANUAL 3 FlareSI Values get
            //find the number of FlareSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual3FlareSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", F16C_manual3ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual3FlareSIIndex);
            int F16C_manual3FlareSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual3FlareSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual3FlareSIIndexEnd);
            string F16C_manual3FlareSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual3FlareSIIndex + 12, F16C_manual3FlareSIIndexEnd - F16C_manual3FlareSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual3FlareSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual3FlareSIAmount));


            //MANUAL 4 ChaffBQ Values get
            //find the number of ChaffBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual4ChaffBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty 	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_4]"));
            //MessageBox.Show("First value Index is " + F16C_manual4ChaffBQIndex);//528
            int F16C_manual4ChaffBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual4ChaffBQIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual4ChaffBQIndexEnd);//534
            string F16C_manual4ChaffBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual4ChaffBQIndex + 12, F16C_manual4ChaffBQIndexEnd - F16C_manual4ChaffBQIndex);//12 is the length of the Index request...or something.

            //MANUAL 4 ChaffBI Values get
            //find the number of ChaffBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual4ChaffBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_4]"));
            //MessageBox.Show("First value Index is " + F16C_manual4ChaffBIIndex);
            int F16C_manual4ChaffBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual4ChaffBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual4ChaffBIIndexEnd);
            string F16C_manual4ChaffBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual4ChaffBIIndex + 12, F16C_manual4ChaffBIIndexEnd - F16C_manual4ChaffBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual4ChaffBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual4ChaffBIAmount));

            //MANUAL 4 ChaffSQ Values get
            //find the number of ChaffSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual4ChaffSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_4]"));
            //MessageBox.Show("First value Index is " + F16C_manual4ChaffSQIndex);
            int F16C_manual4ChaffSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual4ChaffSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual4ChaffSQIndexEnd);
            string F16C_manual4ChaffSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual4ChaffSQIndex + 11, F16C_manual4ChaffSQIndexEnd - F16C_manual4ChaffSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual4ChaffSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual4ChaffSQAmount));

            //MANUAL 4 ChaffSI Values get
            //find the number of ChaffSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual4ChaffSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_4]"));
            //MessageBox.Show("First value Index is " + F16C_manual4ChaffSIIndex);
            int F16C_manual4ChaffSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual4ChaffSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual4ChaffSIIndexEnd);
            string F16C_manual4ChaffSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual4ChaffSIIndex + 12, F16C_manual4ChaffSIIndexEnd - F16C_manual4ChaffSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual4ChaffSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual4ChaffSIAmount));


            //MANUAL 4 FlareBQ Values get
            //find the number of FlareBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual4FlareBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty	= ", F16C_manual4ChaffSIIndexEnd);//i use F16C_manual4ChaffSIIndexEnd because we already know it!
            //MessageBox.Show("First value Index is " + F16C_manual4FlareBQIndex);//528
            int F16C_manual4FlareBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual4FlareBQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual4FlareBQIndexEnd);//534
            string F16C_manual4FlareBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual4FlareBQIndex + 11, F16C_manual4FlareBQIndexEnd - F16C_manual4FlareBQIndex);//11 is the length of the Index request...or something.
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual4FlareBQAmount));                                                                                                                                                                      //MessageBox.Show("Actual String is |" + F16C_manual4FlareBQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual4FlareBQAmount));

            //MANUAL 4 FlareBI Values get
            //find the number of FlareBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual4FlareBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", F16C_manual4ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual4FlareBIIndex);
            int F16C_manual4FlareBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual4FlareBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual4FlareBIIndexEnd);
            string F16C_manual4FlareBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual4FlareBIIndex + 12, F16C_manual4FlareBIIndexEnd - F16C_manual4FlareBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual4FlareBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual4FlareBIAmount));

            //MANUAL 4 FlareSQ Values get
            //find the number of FlareSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual4FlareSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", F16C_manual4ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual4FlareSQIndex);
            int F16C_manual4FlareSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual4FlareSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual4FlareSQIndexEnd);
            string F16C_manual4FlareSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual4FlareSQIndex + 11, F16C_manual4FlareSQIndexEnd - F16C_manual4FlareSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual4FlareSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual4FlareSQAmount));

            //MANUAL 4 FlareSI Values get
            //find the number of FlareSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual4FlareSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", F16C_manual4ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual4FlareSIIndex);
            int F16C_manual4FlareSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual4FlareSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual4FlareSIIndexEnd);
            string F16C_manual4FlareSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual4FlareSIIndex + 12, F16C_manual4FlareSIIndexEnd - F16C_manual4FlareSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual4FlareSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual4FlareSIAmount));


            //MANUAL 5 ChaffBQ Values get
            //find the number of ChaffBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual5ChaffBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty 	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_5]"));
            //MessageBox.Show("First value Index is " + F16C_manual5ChaffBQIndex);//528
            int F16C_manual5ChaffBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual5ChaffBQIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual5ChaffBQIndexEnd);//534
            string F16C_manual5ChaffBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual5ChaffBQIndex + 12, F16C_manual5ChaffBQIndexEnd - F16C_manual5ChaffBQIndex);//12 is the length of the Index request...or something.

            //MANUAL 5 ChaffBI Values get
            //find the number of ChaffBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual5ChaffBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_5]"));
            //MessageBox.Show("First value Index is " + F16C_manual5ChaffBIIndex);
            int F16C_manual5ChaffBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual5ChaffBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual5ChaffBIIndexEnd);
            string F16C_manual5ChaffBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual5ChaffBIIndex + 12, F16C_manual5ChaffBIIndexEnd - F16C_manual5ChaffBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual5ChaffBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual5ChaffBIAmount));

            //MANUAL 5 ChaffSQ Values get
            //find the number of ChaffSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual5ChaffSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_5]"));
            //MessageBox.Show("First value Index is " + F16C_manual5ChaffSQIndex);
            int F16C_manual5ChaffSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual5ChaffSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual5ChaffSQIndexEnd);
            string F16C_manual5ChaffSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual5ChaffSQIndex + 11, F16C_manual5ChaffSQIndexEnd - F16C_manual5ChaffSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual5ChaffSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual5ChaffSQAmount));

            //MANUAL 5 ChaffSI Values get
            //find the number of ChaffSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual5ChaffSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_5]"));
            //MessageBox.Show("First value Index is " + F16C_manual5ChaffSIIndex);
            int F16C_manual5ChaffSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual5ChaffSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual5ChaffSIIndexEnd);
            string F16C_manual5ChaffSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual5ChaffSIIndex + 12, F16C_manual5ChaffSIIndexEnd - F16C_manual5ChaffSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual5ChaffSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual5ChaffSIAmount));


            //MANUAL 5 FlareBQ Values get
            //find the number of FlareBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual5FlareBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty	= ", F16C_manual5ChaffSIIndexEnd);//i use F16C_manual5ChaffSIIndexEnd because we already know it!
            //MessageBox.Show("First value Index is " + F16C_manual5FlareBQIndex);//528
            int F16C_manual5FlareBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual5FlareBQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual5FlareBQIndexEnd);//534
            string F16C_manual5FlareBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual5FlareBQIndex + 11, F16C_manual5FlareBQIndexEnd - F16C_manual5FlareBQIndex);//11 is the length of the Index request...or something.
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual5FlareBQAmount));                                                                                                                                                                      //MessageBox.Show("Actual String is |" + F16C_manual5FlareBQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual5FlareBQAmount));

            //MANUAL 5 FlareBI Values get
            //find the number of FlareBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual5FlareBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", F16C_manual5ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual5FlareBIIndex);
            int F16C_manual5FlareBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual5FlareBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual5FlareBIIndexEnd);
            string F16C_manual5FlareBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual5FlareBIIndex + 12, F16C_manual5FlareBIIndexEnd - F16C_manual5FlareBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual5FlareBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual5FlareBIAmount));

            //MANUAL 5 FlareSQ Values get
            //find the number of FlareSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual5FlareSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", F16C_manual5ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual5FlareSQIndex);
            int F16C_manual5FlareSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual5FlareSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual5FlareSQIndexEnd);
            string F16C_manual5FlareSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual5FlareSQIndex + 11, F16C_manual5FlareSQIndexEnd - F16C_manual5FlareSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual5FlareSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual5FlareSQAmount));

            //MANUAL 5 FlareSI Values get
            //find the number of FlareSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual5FlareSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", F16C_manual5ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual5FlareSIIndex);
            int F16C_manual5FlareSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual5FlareSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual5FlareSIIndexEnd);
            string F16C_manual5FlareSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual5FlareSIIndex + 12, F16C_manual5FlareSIIndexEnd - F16C_manual5FlareSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual5FlareSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual5FlareSIAmount));


            //MANUAL 6 ChaffBQ Values get
            //find the number of ChaffBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual6ChaffBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty 	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_6]"));
            //MessageBox.Show("First value Index is " + F16C_manual6ChaffBQIndex);//528
            int F16C_manual6ChaffBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual6ChaffBQIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual6ChaffBQIndexEnd);//534
            string F16C_manual6ChaffBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual6ChaffBQIndex + 12, F16C_manual6ChaffBQIndexEnd - F16C_manual6ChaffBQIndex);//12 is the length of the Index request...or something.

            //MANUAL 6 ChaffBI Values get
            //find the number of ChaffBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual6ChaffBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_6]"));
            //MessageBox.Show("First value Index is " + F16C_manual6ChaffBIIndex);
            int F16C_manual6ChaffBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual6ChaffBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual6ChaffBIIndexEnd);
            string F16C_manual6ChaffBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual6ChaffBIIndex + 12, F16C_manual6ChaffBIIndexEnd - F16C_manual6ChaffBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual6ChaffBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual6ChaffBIAmount));

            //MANUAL 6 ChaffSQ Values get
            //find the number of ChaffSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual6ChaffSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_6]"));
            //MessageBox.Show("First value Index is " + F16C_manual6ChaffSQIndex);
            int F16C_manual6ChaffSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual6ChaffSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual6ChaffSQIndexEnd);
            string F16C_manual6ChaffSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual6ChaffSQIndex + 11, F16C_manual6ChaffSQIndexEnd - F16C_manual6ChaffSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual6ChaffSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual6ChaffSQAmount));

            //MANUAL 6 ChaffSI Values get
            //find the number of ChaffSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual6ChaffSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.MAN_6]"));
            //MessageBox.Show("First value Index is " + F16C_manual6ChaffSIIndex);
            int F16C_manual6ChaffSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual6ChaffSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual6ChaffSIIndexEnd);
            string F16C_manual6ChaffSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual6ChaffSIIndex + 12, F16C_manual6ChaffSIIndexEnd - F16C_manual6ChaffSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual6ChaffSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual6ChaffSIAmount));


            //MANUAL 6 FlareBQ Values get
            //find the number of FlareBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual6FlareBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty	= ", F16C_manual6ChaffSIIndexEnd);//i use F16C_manual6ChaffSIIndexEnd because we already know it!
            //MessageBox.Show("First value Index is " + F16C_manual6FlareBQIndex);//528
            int F16C_manual6FlareBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual6FlareBQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual6FlareBQIndexEnd);//534
            string F16C_manual6FlareBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual6FlareBQIndex + 11, F16C_manual6FlareBQIndexEnd - F16C_manual6FlareBQIndex);//11 is the length of the Index request...or something.
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual6FlareBQAmount));                                                                                                                                                                      //MessageBox.Show("Actual String is |" + F16C_manual6FlareBQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual6FlareBQAmount));

            //MANUAL 6 FlareBI Values get
            //find the number of FlareBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual6FlareBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", F16C_manual6ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual6FlareBIIndex);
            int F16C_manual6FlareBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual6FlareBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual6FlareBIIndexEnd);
            string F16C_manual6FlareBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual6FlareBIIndex + 12, F16C_manual6FlareBIIndexEnd - F16C_manual6FlareBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual6FlareBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual6FlareBIAmount));

            //MANUAL 6 FlareSQ Values get
            //find the number of FlareSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual6FlareSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", F16C_manual6ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual6FlareSQIndex);
            int F16C_manual6FlareSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual6FlareSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual6FlareSQIndexEnd);
            string F16C_manual6FlareSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual6FlareSQIndex + 11, F16C_manual6FlareSQIndexEnd - F16C_manual6FlareSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual6FlareSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_manual6FlareSQAmount));

            //MANUAL 6 FlareSI Values get
            //find the number of FlareSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_manual6FlareSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", F16C_manual6ChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_manual6FlareSIIndex);
            int F16C_manual6FlareSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_manual6FlareSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_manual6FlareSIIndexEnd);
            string F16C_manual6FlareSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_manual6FlareSIIndex + 12, F16C_manual6FlareSIIndexEnd - F16C_manual6FlareSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_manual6FlareSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_manual6FlareSIAmount));


            //Old Sam ChaffBQ Values get
            //find the number of ChaffBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_oldSamChaffBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty 	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_1]"));
            //MessageBox.Show("First value Index is " + F16C_oldSamChaffBQIndex);//528
            int F16C_oldSamChaffBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_oldSamChaffBQIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_oldSamChaffBQIndexEnd);//534
            string F16C_oldSamChaffBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_oldSamChaffBQIndex + 12, F16C_oldSamChaffBQIndexEnd - F16C_oldSamChaffBQIndex);//12 is the length of the Index request...or something.

            //Old Sam ChaffBI Values get
            //find the number of ChaffBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_oldSamChaffBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_1]"));
            //MessageBox.Show("First value Index is " + F16C_oldSamChaffBIIndex);
            int F16C_oldSamChaffBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_oldSamChaffBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_oldSamChaffBIIndexEnd);
            string F16C_oldSamChaffBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_oldSamChaffBIIndex + 12, F16C_oldSamChaffBIIndexEnd - F16C_oldSamChaffBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_oldSamChaffBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_oldSamChaffBIAmount));

            //Old Sam ChaffSQ Values get
            //find the number of ChaffSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_oldSamChaffSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_1]"));
            //MessageBox.Show("First value Index is " + F16C_oldSamChaffSQIndex);
            int F16C_oldSamChaffSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_oldSamChaffSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_oldSamChaffSQIndexEnd);
            string F16C_oldSamChaffSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_oldSamChaffSQIndex + 11, F16C_oldSamChaffSQIndexEnd - F16C_oldSamChaffSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_oldSamChaffSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_oldSamChaffSQAmount));

            //Old Sam ChaffSI Values get
            //find the number of ChaffSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_oldSamChaffSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_1]"));
            //MessageBox.Show("First value Index is " + F16C_oldSamChaffSIIndex);
            int F16C_oldSamChaffSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_oldSamChaffSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_oldSamChaffSIIndexEnd);
            string F16C_oldSamChaffSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_oldSamChaffSIIndex + 12, F16C_oldSamChaffSIIndexEnd - F16C_oldSamChaffSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_oldSamChaffSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_oldSamChaffSIAmount));


            //Old Sam FlareBQ Values get
            //find the number of FlareBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_oldSamFlareBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty	= ", F16C_oldSamChaffSIIndexEnd);//i use F16C_oldSamChaffSIIndexEnd because we already know it!
            //MessageBox.Show("First value Index is " + F16C_oldSamFlareBQIndex);//528
            int F16C_oldSamFlareBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_oldSamFlareBQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_oldSamFlareBQIndexEnd);//534
            string F16C_oldSamFlareBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_oldSamFlareBQIndex + 11, F16C_oldSamFlareBQIndexEnd - F16C_oldSamFlareBQIndex);//11 is the length of the Index request...or something.
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_oldSamFlareBQAmount));                                                                                                                                                                      //MessageBox.Show("Actual String is |" + F16C_oldSamFlareBQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_oldSamFlareBQAmount));

            //Old Sam FlareBI Values get
            //find the number of FlareBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_oldSamFlareBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", F16C_oldSamChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_oldSamFlareBIIndex);
            int F16C_oldSamFlareBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_oldSamFlareBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_oldSamFlareBIIndexEnd);
            string F16C_oldSamFlareBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_oldSamFlareBIIndex + 12, F16C_oldSamFlareBIIndexEnd - F16C_oldSamFlareBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_oldSamFlareBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_oldSamFlareBIAmount));

            //Old Sam FlareSQ Values get
            //find the number of FlareSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_oldSamFlareSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", F16C_oldSamChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_oldSamFlareSQIndex);
            int F16C_oldSamFlareSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_oldSamFlareSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_oldSamFlareSQIndexEnd);
            string F16C_oldSamFlareSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_oldSamFlareSQIndex + 11, F16C_oldSamFlareSQIndexEnd - F16C_oldSamFlareSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_oldSamFlareSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_oldSamFlareSQAmount));

            //Old Sam FlareSI Values get
            //find the number of FlareSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_oldSamFlareSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", F16C_oldSamChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_oldSamFlareSIIndex);
            int F16C_oldSamFlareSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_oldSamFlareSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_oldSamFlareSIIndexEnd);
            string F16C_oldSamFlareSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_oldSamFlareSIIndex + 12, F16C_oldSamFlareSIIndexEnd - F16C_oldSamFlareSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_oldSamFlareSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_oldSamFlareSIAmount));


            //Current Sam ChaffBQ Values get
            //find the number of ChaffBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_currentSamChaffBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty 	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_2]"));
            //MessageBox.Show("First value Index is " + F16C_currentSamChaffBQIndex);//528
            int F16C_currentSamChaffBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_currentSamChaffBQIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_currentSamChaffBQIndexEnd);//534
            string F16C_currentSamChaffBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_currentSamChaffBQIndex + 12, F16C_currentSamChaffBQIndexEnd - F16C_currentSamChaffBQIndex);//12 is the length of the Index request...or something.

            //Current Sam ChaffBI Values get
            //find the number of ChaffBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_currentSamChaffBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_2]"));
            //MessageBox.Show("First value Index is " + F16C_currentSamChaffBIIndex);
            int F16C_currentSamChaffBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_currentSamChaffBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_currentSamChaffBIIndexEnd);
            string F16C_currentSamChaffBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_currentSamChaffBIIndex + 12, F16C_currentSamChaffBIIndexEnd - F16C_currentSamChaffBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_currentSamChaffBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_currentSamChaffBIAmount));

            //Current Sam ChaffSQ Values get
            //find the number of ChaffSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_currentSamChaffSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_2]"));
            //MessageBox.Show("First value Index is " + F16C_currentSamChaffSQIndex);
            int F16C_currentSamChaffSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_currentSamChaffSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_currentSamChaffSQIndexEnd);
            string F16C_currentSamChaffSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_currentSamChaffSQIndex + 11, F16C_currentSamChaffSQIndexEnd - F16C_currentSamChaffSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_currentSamChaffSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_currentSamChaffSQAmount));

            //Current Sam ChaffSI Values get
            //find the number of ChaffSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_currentSamChaffSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_2]"));
            //MessageBox.Show("First value Index is " + F16C_currentSamChaffSIIndex);
            int F16C_currentSamChaffSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_currentSamChaffSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_currentSamChaffSIIndexEnd);
            string F16C_currentSamChaffSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_currentSamChaffSIIndex + 12, F16C_currentSamChaffSIIndexEnd - F16C_currentSamChaffSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_currentSamChaffSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_currentSamChaffSIAmount));


            //Current Sam FlareBQ Values get
            //find the number of FlareBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_currentSamFlareBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty	= ", F16C_currentSamChaffSIIndexEnd);//i use F16C_currentSamChaffSIIndexEnd because we already know it!
            //MessageBox.Show("First value Index is " + F16C_currentSamFlareBQIndex);//528
            int F16C_currentSamFlareBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_currentSamFlareBQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_currentSamFlareBQIndexEnd);//534
            string F16C_currentSamFlareBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_currentSamFlareBQIndex + 11, F16C_currentSamFlareBQIndexEnd - F16C_currentSamFlareBQIndex);//11 is the length of the Index request...or something.
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_currentSamFlareBQAmount));                                                                                                                                                                      //MessageBox.Show("Actual String is |" + F16C_currentSamFlareBQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_currentSamFlareBQAmount));

            //Current Sam FlareBI Values get
            //find the number of FlareBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_currentSamFlareBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", F16C_currentSamChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_currentSamFlareBIIndex);
            int F16C_currentSamFlareBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_currentSamFlareBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_currentSamFlareBIIndexEnd);
            string F16C_currentSamFlareBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_currentSamFlareBIIndex + 12, F16C_currentSamFlareBIIndexEnd - F16C_currentSamFlareBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_currentSamFlareBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_currentSamFlareBIAmount));

            //Current Sam FlareSQ Values get
            //find the number of FlareSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_currentSamFlareSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", F16C_currentSamChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_currentSamFlareSQIndex);
            int F16C_currentSamFlareSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_currentSamFlareSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_currentSamFlareSQIndexEnd);
            string F16C_currentSamFlareSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_currentSamFlareSQIndex + 11, F16C_currentSamFlareSQIndexEnd - F16C_currentSamFlareSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_currentSamFlareSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_currentSamFlareSQAmount));

            //Current Sam FlareSI Values get
            //find the number of FlareSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_currentSamFlareSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", F16C_currentSamChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_currentSamFlareSIIndex);
            int F16C_currentSamFlareSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_currentSamFlareSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_currentSamFlareSIIndexEnd);
            string F16C_currentSamFlareSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_currentSamFlareSIIndex + 12, F16C_currentSamFlareSIIndexEnd - F16C_currentSamFlareSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_currentSamFlareSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_currentSamFlareSIAmount));


            //IR Sam ChaffBQ Values get
            //find the number of ChaffBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_irSamChaffBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty 	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_3]"));
            //MessageBox.Show("First value Index is " + F16C_irSamChaffBQIndex);//528
            int F16C_irSamChaffBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_irSamChaffBQIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_irSamChaffBQIndexEnd);//534
            string F16C_irSamChaffBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_irSamChaffBQIndex + 12, F16C_irSamChaffBQIndexEnd - F16C_irSamChaffBQIndex);//12 is the length of the Index request...or something.

            //IR Sam ChaffBI Values get
            //find the number of ChaffBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_irSamChaffBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_3]"));
            //MessageBox.Show("First value Index is " + F16C_irSamChaffBIIndex);
            int F16C_irSamChaffBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_irSamChaffBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_irSamChaffBIIndexEnd);
            string F16C_irSamChaffBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_irSamChaffBIIndex + 12, F16C_irSamChaffBIIndexEnd - F16C_irSamChaffBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_irSamChaffBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_irSamChaffBIAmount));

            //IR Sam ChaffSQ Values get
            //find the number of ChaffSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_irSamChaffSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_3]"));
            //MessageBox.Show("First value Index is " + F16C_irSamChaffSQIndex);
            int F16C_irSamChaffSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_irSamChaffSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_irSamChaffSQIndexEnd);
            string F16C_irSamChaffSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_irSamChaffSQIndex + 11, F16C_irSamChaffSQIndexEnd - F16C_irSamChaffSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_irSamChaffSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_irSamChaffSQAmount));

            //IR Sam ChaffSI Values get
            //find the number of ChaffSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_irSamChaffSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", CountermeasureFileStringText_F16C.IndexOf("programs[ProgramNames.AUTO_3]"));
            //MessageBox.Show("First value Index is " + F16C_irSamChaffSIIndex);
            int F16C_irSamChaffSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_irSamChaffSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_irSamChaffSIIndexEnd);
            string F16C_irSamChaffSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_irSamChaffSIIndex + 12, F16C_irSamChaffSIIndexEnd - F16C_irSamChaffSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_irSamChaffSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_irSamChaffSIAmount));


            //IR Sam FlareBQ Values get
            //find the number of FlareBQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_irSamFlareBQIndex = CountermeasureFileStringText_F16C.IndexOf("burstQty	= ", F16C_irSamChaffSIIndexEnd);//i use F16C_irSamChaffSIIndexEnd because we already know it!
            //MessageBox.Show("First value Index is " + F16C_irSamFlareBQIndex);//528
            int F16C_irSamFlareBQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_irSamFlareBQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_irSamFlareBQIndexEnd);//534
            string F16C_irSamFlareBQAmount = CountermeasureFileStringText_F16C.Substring(F16C_irSamFlareBQIndex + 11, F16C_irSamFlareBQIndexEnd - F16C_irSamFlareBQIndex);//11 is the length of the Index request...or something.
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_irSamFlareBQAmount));                                                                                                                                                                      //MessageBox.Show("Actual String is |" + F16C_irSamFlareBQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_irSamFlareBQAmount));

            //IR Sam FlareBI Values get
            //find the number of FlareBI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_irSamFlareBIIndex = CountermeasureFileStringText_F16C.IndexOf("burstIntv	= ", F16C_irSamChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_irSamFlareBIIndex);
            int F16C_irSamFlareBIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_irSamFlareBIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_irSamFlareBIIndexEnd);
            string F16C_irSamFlareBIAmount = CountermeasureFileStringText_F16C.Substring(F16C_irSamFlareBIIndex + 12, F16C_irSamFlareBIIndexEnd - F16C_irSamFlareBIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_irSamFlareBIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_irSamFlareBIAmount));

            //IR Sam FlareSQ Values get
            //find the number of FlareSQ burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_irSamFlareSQIndex = CountermeasureFileStringText_F16C.IndexOf("salvoQty	= ", F16C_irSamChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_irSamFlareSQIndex);
            int F16C_irSamFlareSQIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_irSamFlareSQIndex) - 11;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_irSamFlareSQIndexEnd);
            string F16C_irSamFlareSQAmount = CountermeasureFileStringText_F16C.Substring(F16C_irSamFlareSQIndex + 11, F16C_irSamFlareSQIndexEnd - F16C_irSamFlareSQIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_irSamFlareSQAmount + "|");
            //MessageBox.Show("Parsed String is " + int.Parse(F16C_irSamFlareSQAmount));

            //IR Sam FlareSI Values get
            //find the number of FlareSI burst quantity after program 1 is read
            //MessageBox.Show("String is " + CountermeasureFileStringText_F16C);
            int F16C_irSamFlareSIIndex = CountermeasureFileStringText_F16C.IndexOf("salvoIntv	= ", F16C_irSamChaffSIIndexEnd);
            //MessageBox.Show("First value Index is " + F16C_irSamFlareSIIndex);
            int F16C_irSamFlareSIIndexEnd = CountermeasureFileStringText_F16C.IndexOf(",", F16C_irSamFlareSIIndex) - 12;//gets the index of the next part to signal the end
            //MessageBox.Show("Second value Index is " + F16C_irSamFlareSIIndexEnd);
            string F16C_irSamFlareSIAmount = CountermeasureFileStringText_F16C.Substring(F16C_irSamFlareSIIndex + 12, F16C_irSamFlareSIIndexEnd - F16C_irSamFlareSIIndex);//12 is the length of the Index request
            //MessageBox.Show("Actual String is |" + F16C_irSamFlareSIAmount + "|");
            //MessageBox.Show("Parsed String is " + Decimal.Parse(F16C_irSamFlareSIAmount));



            //set Man1 GUI elements TODO: This try stuff
            //https://docs.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception?view=netcore-3.1
            //https://stackoverflow.com/questions/4264736/convert-string-to-decimal-keeping-fractions
            //try to import the value. If the imported value is out of the set range, make the value the minimum acceptable value

            //manual1
            try { numericUpDown_F16C_manual1Chaff_BQ.Value = int.Parse(F16C_manual1ChaffBQAmount);}
            catch (ArgumentOutOfRangeException){numericUpDown_F16C_manual1Chaff_BQ.Value = numericUpDown_F16C_manual1Chaff_BQ.Minimum;}

            try { numericUpDown_F16C_manual1Chaff_BI.Value = Decimal.Parse(F16C_manual1ChaffBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual1Chaff_BI.Value = numericUpDown_F16C_manual1Chaff_BI.Minimum; }

            try { numericUpDown_F16C_manual1Chaff_SQ.Value = int.Parse(F16C_manual1ChaffSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual1Chaff_SQ.Value = numericUpDown_F16C_manual1Chaff_SQ.Minimum; }

            try { numericUpDown_F16C_manual1Chaff_SI.Value = Decimal.Parse(F16C_manual1ChaffSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual1Chaff_SI.Value = numericUpDown_F16C_manual1Chaff_SI.Minimum; }


            try { numericUpDown_F16C_manual1Flare_BQ.Value = int.Parse(F16C_manual1FlareBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual1Flare_BQ.Value = numericUpDown_F16C_manual1Flare_BQ.Minimum; }

            try { numericUpDown_F16C_manual1Flare_BI.Value = Decimal.Parse(F16C_manual1FlareBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual1Flare_BI.Value = numericUpDown_F16C_manual1Flare_BI.Minimum; }

            try { numericUpDown_F16C_manual1Flare_SQ.Value = int.Parse(F16C_manual1FlareSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual1Flare_SQ.Value = numericUpDown_F16C_manual1Flare_SQ.Minimum; }

            try { numericUpDown_F16C_manual1Flare_SI.Value = Decimal.Parse(F16C_manual1FlareSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual1Flare_SI.Value = numericUpDown_F16C_manual1Flare_SI.Minimum; }

            //manual2
            try { numericUpDown_F16C_manual2Chaff_BQ.Value = int.Parse(F16C_manual2ChaffBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual2Chaff_BQ.Value = numericUpDown_F16C_manual2Chaff_BQ.Minimum; }

            try { numericUpDown_F16C_manual2Chaff_BI.Value = Decimal.Parse(F16C_manual2ChaffBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual2Chaff_BI.Value = numericUpDown_F16C_manual2Chaff_BI.Minimum; }

            try { numericUpDown_F16C_manual2Chaff_SQ.Value = int.Parse(F16C_manual2ChaffSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual2Chaff_SQ.Value = numericUpDown_F16C_manual2Chaff_SQ.Minimum; }

            try { numericUpDown_F16C_manual2Chaff_SI.Value = Decimal.Parse(F16C_manual2ChaffSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual2Chaff_SI.Value = numericUpDown_F16C_manual2Chaff_SI.Minimum; }


            try { numericUpDown_F16C_manual2Flare_BQ.Value = int.Parse(F16C_manual2FlareBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual2Flare_BQ.Value = numericUpDown_F16C_manual2Flare_BQ.Minimum; }

            try { numericUpDown_F16C_manual2Flare_BI.Value = Decimal.Parse(F16C_manual2FlareBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual2Flare_BI.Value = numericUpDown_F16C_manual2Flare_BI.Minimum; }

            try { numericUpDown_F16C_manual2Flare_SQ.Value = int.Parse(F16C_manual2FlareSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual2Flare_SQ.Value = numericUpDown_F16C_manual2Flare_SQ.Minimum; }

            try { numericUpDown_F16C_manual2Flare_SI.Value = Decimal.Parse(F16C_manual2FlareSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual2Flare_SI.Value = numericUpDown_F16C_manual2Flare_SI.Minimum; }

            //manual3
            try { numericUpDown_F16C_manual3Chaff_BQ.Value = int.Parse(F16C_manual3ChaffBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual3Chaff_BQ.Value = numericUpDown_F16C_manual3Chaff_BQ.Minimum; }

            try { numericUpDown_F16C_manual3Chaff_BI.Value = Decimal.Parse(F16C_manual3ChaffBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual3Chaff_BI.Value = numericUpDown_F16C_manual3Chaff_BI.Minimum; }

            try { numericUpDown_F16C_manual3Chaff_SQ.Value = int.Parse(F16C_manual3ChaffSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual3Chaff_SQ.Value = numericUpDown_F16C_manual3Chaff_SQ.Minimum; }

            try { numericUpDown_F16C_manual3Chaff_SI.Value = Decimal.Parse(F16C_manual3ChaffSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual3Chaff_SI.Value = numericUpDown_F16C_manual3Chaff_SI.Minimum; }


            try { numericUpDown_F16C_manual3Flare_BQ.Value = int.Parse(F16C_manual3FlareBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual3Flare_BQ.Value = numericUpDown_F16C_manual3Flare_BQ.Minimum; }

            try { numericUpDown_F16C_manual3Flare_BI.Value = Decimal.Parse(F16C_manual3FlareBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual3Flare_BI.Value = numericUpDown_F16C_manual3Flare_BI.Minimum; }

            try { numericUpDown_F16C_manual3Flare_SQ.Value = int.Parse(F16C_manual3FlareSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual3Flare_SQ.Value = numericUpDown_F16C_manual3Flare_SQ.Minimum; }

            try { numericUpDown_F16C_manual3Flare_SI.Value = Decimal.Parse(F16C_manual3FlareSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual3Flare_SI.Value = numericUpDown_F16C_manual3Flare_SI.Minimum; }

            //manual4
            try { numericUpDown_F16C_manual4Chaff_BQ.Value = int.Parse(F16C_manual4ChaffBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual4Chaff_BQ.Value = numericUpDown_F16C_manual4Chaff_BQ.Minimum; }

            try { numericUpDown_F16C_manual4Chaff_BI.Value = Decimal.Parse(F16C_manual4ChaffBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual4Chaff_BI.Value = numericUpDown_F16C_manual4Chaff_BI.Minimum; }

            try { numericUpDown_F16C_manual4Chaff_SQ.Value = int.Parse(F16C_manual4ChaffSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual4Chaff_SQ.Value = numericUpDown_F16C_manual4Chaff_SQ.Minimum; }

            try { numericUpDown_F16C_manual4Chaff_SI.Value = Decimal.Parse(F16C_manual4ChaffSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual4Chaff_SI.Value = numericUpDown_F16C_manual4Chaff_SI.Minimum; }


            try { numericUpDown_F16C_manual4Flare_BQ.Value = int.Parse(F16C_manual4FlareBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual4Flare_BQ.Value = numericUpDown_F16C_manual4Flare_BQ.Minimum; }

            try { numericUpDown_F16C_manual4Flare_BI.Value = Decimal.Parse(F16C_manual4FlareBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual4Flare_BI.Value = numericUpDown_F16C_manual4Flare_BI.Minimum; }

            try { numericUpDown_F16C_manual4Flare_SQ.Value = int.Parse(F16C_manual4FlareSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual4Flare_SQ.Value = numericUpDown_F16C_manual4Flare_SQ.Minimum; }

            try { numericUpDown_F16C_manual4Flare_SI.Value = Decimal.Parse(F16C_manual4FlareSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual4Flare_SI.Value = numericUpDown_F16C_manual4Flare_SI.Minimum; }


            //manual5
            try { numericUpDown_F16C_manual5Chaff_BQ.Value = int.Parse(F16C_manual5ChaffBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual5Chaff_BQ.Value = numericUpDown_F16C_manual5Chaff_BQ.Minimum; }

            try { numericUpDown_F16C_manual5Chaff_BI.Value = Decimal.Parse(F16C_manual5ChaffBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual5Chaff_BI.Value = numericUpDown_F16C_manual5Chaff_BI.Minimum; }

            try { numericUpDown_F16C_manual5Chaff_SQ.Value = int.Parse(F16C_manual5ChaffSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual5Chaff_SQ.Value = numericUpDown_F16C_manual5Chaff_SQ.Minimum; }

            try { numericUpDown_F16C_manual5Chaff_SI.Value = Decimal.Parse(F16C_manual5ChaffSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual5Chaff_SI.Value = numericUpDown_F16C_manual5Chaff_SI.Minimum; }


            try { numericUpDown_F16C_manual5Flare_BQ.Value = int.Parse(F16C_manual5FlareBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual5Flare_BQ.Value = numericUpDown_F16C_manual5Flare_BQ.Minimum; }

            try { numericUpDown_F16C_manual5Flare_BI.Value = Decimal.Parse(F16C_manual5FlareBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual5Flare_BI.Value = numericUpDown_F16C_manual5Flare_BI.Minimum; }

            try { numericUpDown_F16C_manual5Flare_SQ.Value = int.Parse(F16C_manual5FlareSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual5Flare_SQ.Value = numericUpDown_F16C_manual5Flare_SQ.Minimum; }

            try { numericUpDown_F16C_manual5Flare_SI.Value = Decimal.Parse(F16C_manual5FlareSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual5Flare_SI.Value = numericUpDown_F16C_manual5Flare_SI.Minimum; }


            //manual6
            try { numericUpDown_F16C_manual6Chaff_BQ.Value = int.Parse(F16C_manual6ChaffBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual6Chaff_BQ.Value = numericUpDown_F16C_manual6Chaff_BQ.Minimum; }

            try { numericUpDown_F16C_manual6Chaff_BI.Value = Decimal.Parse(F16C_manual6ChaffBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual6Chaff_BI.Value = numericUpDown_F16C_manual6Chaff_BI.Minimum; }

            try { numericUpDown_F16C_manual6Chaff_SQ.Value = int.Parse(F16C_manual6ChaffSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual6Chaff_SQ.Value = numericUpDown_F16C_manual6Chaff_SQ.Minimum; }

            try { numericUpDown_F16C_manual6Chaff_SI.Value = Decimal.Parse(F16C_manual6ChaffSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual6Chaff_SI.Value = numericUpDown_F16C_manual6Chaff_SI.Minimum; }


            try { numericUpDown_F16C_manual6Flare_BQ.Value = int.Parse(F16C_manual6FlareBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual6Flare_BQ.Value = numericUpDown_F16C_manual6Flare_BQ.Minimum; }

            try { numericUpDown_F16C_manual6Flare_BI.Value = Decimal.Parse(F16C_manual6FlareBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual6Flare_BI.Value = numericUpDown_F16C_manual6Flare_BI.Minimum; }

            try { numericUpDown_F16C_manual6Flare_SQ.Value = int.Parse(F16C_manual6FlareSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual6Flare_SQ.Value = numericUpDown_F16C_manual6Flare_SQ.Minimum; }

            try { numericUpDown_F16C_manual6Flare_SI.Value = Decimal.Parse(F16C_manual6FlareSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_manual6Flare_SI.Value = numericUpDown_F16C_manual6Flare_SI.Minimum; }


            //oldSam
            try { numericUpDown_F16C_oldSamChaff_BQ.Value = int.Parse(F16C_oldSamChaffBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_oldSamChaff_BQ.Value = numericUpDown_F16C_oldSamChaff_BQ.Minimum; }

            try { numericUpDown_F16C_oldSamChaff_BI.Value = Decimal.Parse(F16C_oldSamChaffBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_oldSamChaff_BI.Value = numericUpDown_F16C_oldSamChaff_BI.Minimum; }

            try { numericUpDown_F16C_oldSamChaff_SQ.Value = int.Parse(F16C_oldSamChaffSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_oldSamChaff_SQ.Value = numericUpDown_F16C_oldSamChaff_SQ.Minimum; }

            try { numericUpDown_F16C_oldSamChaff_SI.Value = Decimal.Parse(F16C_oldSamChaffSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_oldSamChaff_SI.Value = numericUpDown_F16C_oldSamChaff_SI.Minimum; }


            try { numericUpDown_F16C_oldSamFlare_BQ.Value = int.Parse(F16C_oldSamFlareBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_oldSamFlare_BQ.Value = numericUpDown_F16C_oldSamFlare_BQ.Minimum; }

            try { numericUpDown_F16C_oldSamFlare_BI.Value = Decimal.Parse(F16C_oldSamFlareBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_oldSamFlare_BI.Value = numericUpDown_F16C_oldSamFlare_BI.Minimum; }

            try { numericUpDown_F16C_oldSamFlare_SQ.Value = int.Parse(F16C_oldSamFlareSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_oldSamFlare_SQ.Value = numericUpDown_F16C_oldSamFlare_SQ.Minimum; }

            try { numericUpDown_F16C_oldSamFlare_SI.Value = Decimal.Parse(F16C_oldSamFlareSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_oldSamFlare_SI.Value = numericUpDown_F16C_oldSamFlare_SI.Minimum; }


            //currentSam
            try { numericUpDown_F16C_currentSamChaff_BQ.Value = int.Parse(F16C_currentSamChaffBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_currentSamChaff_BQ.Value = numericUpDown_F16C_currentSamChaff_BQ.Minimum; }

            try { numericUpDown_F16C_currentSamChaff_BI.Value = Decimal.Parse(F16C_currentSamChaffBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_currentSamChaff_BI.Value = numericUpDown_F16C_currentSamChaff_BI.Minimum; }

            try { numericUpDown_F16C_currentSamChaff_SQ.Value = int.Parse(F16C_currentSamChaffSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_currentSamChaff_SQ.Value = numericUpDown_F16C_currentSamChaff_SQ.Minimum; }

            try { numericUpDown_F16C_currentSamChaff_SI.Value = Decimal.Parse(F16C_currentSamChaffSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_currentSamChaff_SI.Value = numericUpDown_F16C_currentSamChaff_SI.Minimum; }


            try { numericUpDown_F16C_currentSamFlare_BQ.Value = int.Parse(F16C_currentSamFlareBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_currentSamFlare_BQ.Value = numericUpDown_F16C_currentSamFlare_BQ.Minimum; }

            try { numericUpDown_F16C_currentSamFlare_BI.Value = Decimal.Parse(F16C_currentSamFlareBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_currentSamFlare_BI.Value = numericUpDown_F16C_currentSamFlare_BI.Minimum; }

            try { numericUpDown_F16C_currentSamFlare_SQ.Value = int.Parse(F16C_currentSamFlareSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_currentSamFlare_SQ.Value = numericUpDown_F16C_currentSamFlare_SQ.Minimum; }

            try { numericUpDown_F16C_currentSamFlare_SI.Value = Decimal.Parse(F16C_currentSamFlareSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_currentSamFlare_SI.Value = numericUpDown_F16C_currentSamFlare_SI.Minimum; }


            //irSam
            try { numericUpDown_F16C_irSamChaff_BQ.Value = int.Parse(F16C_irSamChaffBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_irSamChaff_BQ.Value = numericUpDown_F16C_irSamChaff_BQ.Minimum; }

            try { numericUpDown_F16C_irSamChaff_BI.Value = Decimal.Parse(F16C_irSamChaffBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_irSamChaff_BI.Value = numericUpDown_F16C_irSamChaff_BI.Minimum; }

            try { numericUpDown_F16C_irSamChaff_SQ.Value = int.Parse(F16C_irSamChaffSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_irSamChaff_SQ.Value = numericUpDown_F16C_irSamChaff_SQ.Minimum; }

            try { numericUpDown_F16C_irSamChaff_SI.Value = Decimal.Parse(F16C_irSamChaffSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_irSamChaff_SI.Value = numericUpDown_F16C_irSamChaff_SI.Minimum; }


            try { numericUpDown_F16C_irSamFlare_BQ.Value = int.Parse(F16C_irSamFlareBQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_irSamFlare_BQ.Value = numericUpDown_F16C_irSamFlare_BQ.Minimum; }

            try { numericUpDown_F16C_irSamFlare_BI.Value = Decimal.Parse(F16C_irSamFlareBIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_irSamFlare_BI.Value = numericUpDown_F16C_irSamFlare_BI.Minimum; }

            try { numericUpDown_F16C_irSamFlare_SQ.Value = int.Parse(F16C_irSamFlareSQAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_irSamFlare_SQ.Value = numericUpDown_F16C_irSamFlare_SQ.Minimum; }

            try { numericUpDown_F16C_irSamFlare_SI.Value = Decimal.Parse(F16C_irSamFlareSIAmount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_F16C_irSamFlare_SI.Value = numericUpDown_F16C_irSamFlare_SI.Minimum; }
        }

        private void printLua()
        {

        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button_export_Click(object sender, EventArgs e)
        {
            //determine which tab the user is on to export the correct .lua
            if (tabControl_mainTab.SelectedTab == tabPage1)
            {
                //MessageBox.Show("You were on tab 1");
                string[] luaExportString = { "local count = 0",
                "local function counter()",
                "    count = count + 1",
                "    return count",
                "end",
                "",
                "ProgramNames =",
                "{",
                "    MAN_1 = counter(),",
                "    MAN_2 = counter(),",
                "    MAN_3 = counter(),",
                "    MAN_4 = counter(),",
                "    MAN_5 = counter(),",
                "    MAN_6 = counter(),",
                "    AUTO_1 = counter(),",
                "    AUTO_2 = counter(),",
                "    AUTO_3 = counter(),",
                "    AUTO_4 = counter(),",
                "    AUTO_5 = counter(),",
                "    AUTO_6 = counter()",
                "}",
                "",
                "",
                "programs = {}",
                "",
                "-- Default manual presets",
                "-- MAN 1",
                "programs[ProgramNames.MAN_1] = {}",
                "programs[ProgramNames.MAN_1][\"chaff\"] = " + numericUpDown_F18C_manual1Chaff.Value,
                "programs[ProgramNames.MAN_1][\"flare\"] = " + numericUpDown_F18C_manual1Flare.Value,
                "programs[ProgramNames.MAN_1][\"intv\"]  = " + numericUpDown_F18C_manual1Interval.Value,
                "programs[ProgramNames.MAN_1][\"cycle\"] = " + numericUpDown_F18C_manual1Cycle.Value,
                "",
                "-- MAN 2",
                "programs[ProgramNames.MAN_2] = {}",
                "programs[ProgramNames.MAN_2][\"chaff\"] = " + numericUpDown_F18C_manual2Chaff.Value,
                "programs[ProgramNames.MAN_2][\"flare\"] = " + numericUpDown_F18C_manual2Flare.Value,
                "programs[ProgramNames.MAN_2][\"intv\"]  = " + numericUpDown_F18C_manual2Interval.Value,
                "programs[ProgramNames.MAN_2][\"cycle\"] = " + numericUpDown_F18C_manual2Cycle.Value,
                "",
                "-- MAN 3",
                "programs[ProgramNames.MAN_3] = {}",
                "programs[ProgramNames.MAN_3][\"chaff\"] = " + numericUpDown_F18C_manual3Chaff.Value,
                "programs[ProgramNames.MAN_3][\"flare\"] = " + numericUpDown_F18C_manual3Flare.Value,
                "programs[ProgramNames.MAN_3][\"intv\"]  = " + numericUpDown_F18C_manual3Interval.Value,
                "programs[ProgramNames.MAN_3][\"cycle\"] = " + numericUpDown_F18C_manual3Cycle.Value,
                "",
                "-- MAN 4",
                "programs[ProgramNames.MAN_4] = {}",
                "programs[ProgramNames.MAN_4][\"chaff\"] = " + numericUpDown_F18C_manual4Chaff.Value,
                "programs[ProgramNames.MAN_4][\"flare\"] = " + numericUpDown_F18C_manual4Flare.Value,
                "programs[ProgramNames.MAN_4][\"intv\"]  = " + numericUpDown_F18C_manual4Interval.Value,
                "programs[ProgramNames.MAN_4][\"cycle\"] = " + numericUpDown_F18C_manual4Cycle.Value,
                "",
                "-- MAN 5 - Chaff single",
                "programs[ProgramNames.MAN_5] = {}",
                "programs[ProgramNames.MAN_5][\"chaff\"] = " + numericUpDown_F18C_manual5Chaff.Value,
                "programs[ProgramNames.MAN_5][\"flare\"] = " + numericUpDown_F18C_manual5Flare.Value,
                "programs[ProgramNames.MAN_5][\"intv\"]  = " + numericUpDown_F18C_manual5Interval.Value,
                "programs[ProgramNames.MAN_5][\"cycle\"] = " + numericUpDown_F18C_manual5Cycle.Value,
                "",
                "-- MAN 6 - Wall Dispense button, Panic",
                "programs[ProgramNames.MAN_6] = {}",
                "programs[ProgramNames.MAN_6][\"chaff\"] = " + numericUpDown_F18C_manual6Chaff.Value,
                "programs[ProgramNames.MAN_6][\"flare\"] = " + numericUpDown_F18C_manual6Flare.Value,
                "programs[ProgramNames.MAN_6][\"intv\"]  = " + numericUpDown_F18C_manual6Interval.Value,
                "programs[ProgramNames.MAN_6][\"cycle\"] = " + numericUpDown_F18C_manual6Cycle.Value,
                "",
                "-- Auto presets",
                "-- Old generation radar SAM",
                "programs[ProgramNames.AUTO_1] = {}",
                "programs[ProgramNames.AUTO_1][\"chaff\"] = " + numericUpDown_F18C_manualOldSamChaff.Value,
                "programs[ProgramNames.AUTO_1][\"flare\"] = " + numericUpDown_F18C_manualOldSamFlare.Value,
                "programs[ProgramNames.AUTO_1][\"intv\"]  = " + numericUpDown_F18C_OldSamInterval.Value,
                "programs[ProgramNames.AUTO_1][\"cycle\"] = " + numericUpDown_F18C_OldSamCycle.Value,
                "",
                "-- Current generation radar SAM",
                "programs[ProgramNames.AUTO_2] = {}",
                "programs[ProgramNames.AUTO_2][\"chaff\"] = " + numericUpDown_F18C_manualCurrentSamChaff.Value,
                "programs[ProgramNames.AUTO_2][\"flare\"] = " + numericUpDown_F18C_manualCurrentSamFlare.Value,
                "programs[ProgramNames.AUTO_2][\"intv\"]  = " + numericUpDown_F18C_CurrentSamInterval.Value,
                "programs[ProgramNames.AUTO_2][\"cycle\"] = " + numericUpDown_F18C_CurrentSamCycle.Value,
                "",
                "-- IR SAM",
                "programs[ProgramNames.AUTO_3] = {}",
                "programs[ProgramNames.AUTO_3][\"chaff\"] = " + numericUpDown_F18C_manual_IRSamChaff.Value,
                "programs[ProgramNames.AUTO_3][\"flare\"] = " + numericUpDown_F18C_manual_IRSamFlare.Value,
                "programs[ProgramNames.AUTO_3][\"intv\"]  = " + numericUpDown_F18C_IRSamInterval.Value,
                "programs[ProgramNames.AUTO_3][\"cycle\"] = " + numericUpDown_F18C_IRSamCycle.Value,
                "",
                "",
                "need_to_be_closed = true -- lua_state  will be closed in post_initialize()",
                "--Exported via Bailey's CMS Editor on " + System.DateTime.Now};
                // WriteAllLines creates a file, writes a collection of strings to the file,
                // and then closes the file.  You do NOT need to call Flush() or Close().
                //System.IO.File.WriteAllLines(@"C:\TestFolder\WriteLines.txt", lines);

                if (isExportEnabled == true)
                {
                    System.IO.Directory.CreateDirectory(cmdsLua_F18C_FolderPath);
                    System.IO.File.WriteAllLines(cmdsLua_F18C_fullPath, luaExportString);

                    //https://stackoverflow.com/questions/5920882/file-move-does-not-work-file-already-exists
                    System.IO.Directory.CreateDirectory(exportPathBackup_F18C);
                    if (File.Exists(exportPathBackup_F18C + "\\CMDS_ALE47.lua"))
                    {
                        File.Delete(exportPathBackup_F18C + "\\CMDS_ALE47.lua");
                    }
                    System.IO.File.WriteAllLines(exportPathBackup_F18C + "\\CMDS_ALE47.txt", luaExportString);
                    File.Move(exportPathBackup_F18C + "\\CMDS_ALE47.txt", Path.ChangeExtension(exportPathBackup_F18C + "\\CMDS_ALE47.txt", ".lua"));


                    MessageBox.Show("Your F-18C CMDS file was exported to \r\n" + cmdsLua_F18C_fullPath + "\r\n\r\n" 
                        + "Your F-18C CMDS backup file was exported to \r\n" + exportPathBackup_F18C + "\\CMDS_ALE47.lua");
                }
                else
                {
                    MessageBox.Show("Please select your DCS.exe location.");
                }
            }
            else if (tabControl_mainTab.SelectedTab == tabPage2)
            {
                //MessageBox.Show("You were on tab 2");
                //readjust the spacing. low priority.
                string[] luaExportString = {"local gettext = require(\"i_18n\")",
                "_ = gettext.translate",
                "",
                "local count = 0",
                "local function counter()",
                "	count = count + 1",
                "	return count",
                "end",
                "",
                "ProgramNames =",
                "{",
                "	MAN_1 = counter(),",
                "	MAN_2 = counter(),",
                "	MAN_3 = counter(),",
                "	MAN_4 = counter(),",
                "	MAN_5 = counter(),",
                "	MAN_6 = counter(),",
                "	AUTO_1 = counter(),",
                "	AUTO_2 = counter(),",
                "	AUTO_3 = counter(),",
                "	AUTO_4 = counter(),",
                "	AUTO_5 = counter(),",
                "	AUTO_6 = counter(),",
                "}",
                "",
                "programs = {}",
                "",
                "-- Default manual presets",
                "-- MAN 1",
                "programs[ProgramNames.MAN_1] = {",
                "	chaff = {",
                "		burstQty 	= " + numericUpDown_F16C_manual1Chaff_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual1Chaff_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual1Chaff_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual1Chaff_SI.Value + ",",
                "	},",
                "	flare = {",
                "		burstQty	= " + numericUpDown_F16C_manual1Flare_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual1Flare_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual1Flare_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual1Flare_SI.Value + ",",
                "	},",
                "}",
                "",
                "-- MAN 2",
                "programs[ProgramNames.MAN_2] = {",
                "	chaff = {",
                "		burstQty 	= " + numericUpDown_F16C_manual2Chaff_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual2Chaff_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual2Chaff_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual2Chaff_SI.Value + ",",
                "	},",
                "	flare = {",
                "		burstQty	= " + numericUpDown_F16C_manual2Flare_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual2Flare_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual2Flare_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual2Flare_SI.Value + ",",
                "	},",
                "}",
                "",
                "-- MAN 3",
                "programs[ProgramNames.MAN_3] = {",
                "	chaff = {",
                "		burstQty 	= " + numericUpDown_F16C_manual3Chaff_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual3Chaff_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual3Chaff_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual3Chaff_SI.Value + ",",
                "	},",
                "	flare = {",
                "		burstQty	= " + numericUpDown_F16C_manual3Flare_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual3Flare_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual3Flare_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual3Flare_SI.Value + ",",
                "	},",
                "}",
                "",
                "-- MAN 4",
                "programs[ProgramNames.MAN_4] = {",
                "	chaff = {",
                "		burstQty 	= " + numericUpDown_F16C_manual4Chaff_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual4Chaff_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual4Chaff_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual4Chaff_SI.Value + ",",
                "	},",
                "	flare = {",
                "		burstQty	= " + numericUpDown_F16C_manual4Flare_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual4Flare_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual4Flare_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual4Flare_SI.Value + ",",
                "	},",
                "}",
                "",
                "-- MAN 5 - Wall Dispense button, Panic",
                "programs[ProgramNames.MAN_5] = {",
                "	chaff = {",
                "		burstQty 	= " + numericUpDown_F16C_manual5Chaff_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual5Chaff_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual5Chaff_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual5Chaff_SI.Value + ",",
                "	},",
                "	flare = {",
                "		burstQty	= " + numericUpDown_F16C_manual5Flare_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual5Flare_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual5Flare_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual5Flare_SI.Value + ",",
                "	},",
                "}",
                "",
                "-- MAN 6 - BYPASS mode",
                "programs[ProgramNames.MAN_6] = {",
                "	chaff = {",
                "		burstQty 	= " + numericUpDown_F16C_manual6Chaff_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual6Chaff_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual6Chaff_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual6Chaff_SI.Value + ",",
                "	},",
                "	flare = {",
                "		burstQty	= " + numericUpDown_F16C_manual6Flare_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_manual6Flare_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_manual6Flare_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_manual6Flare_SI.Value + ",",
                "	},",
                "}",
                "",
                "-- Auto presets",
                "-- Old generation radar SAM",
                "programs[ProgramNames.AUTO_1] = {",
                "	chaff = {",
                "		burstQty 	= " + numericUpDown_F16C_oldSamChaff_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_oldSamChaff_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_oldSamChaff_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_oldSamChaff_SI.Value + ",",
                "	},",
                "	flare = {",
                "		burstQty	= " + numericUpDown_F16C_oldSamFlare_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_oldSamFlare_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_oldSamFlare_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_oldSamFlare_SI.Value + ",",
                "	},",
                "}",
                "",
                "-- Current generation radar SAM",
                "programs[ProgramNames.AUTO_2] = {",
                "	chaff = {",
                "		burstQty 	= " + numericUpDown_F16C_currentSamChaff_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_currentSamChaff_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_currentSamChaff_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_currentSamChaff_SI.Value + ",",
                "	},",
                "	flare = {",
                "		burstQty	= " + numericUpDown_F16C_currentSamFlare_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_currentSamFlare_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_currentSamFlare_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_currentSamFlare_SI.Value + ",",
                "	},",
                "}",
                "",
                "-- IR SAM",
                "programs[ProgramNames.AUTO_3] = {",
                "	chaff = {",
                "		burstQty 	= " + numericUpDown_F16C_irSamChaff_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_irSamChaff_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_irSamChaff_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_irSamChaff_SI.Value + ",",
                "	},",
                "	flare = {",
                "		burstQty	= " + numericUpDown_F16C_irSamFlare_BQ.Value + ",",
                "		burstIntv	= " + numericUpDown_F16C_irSamFlare_BI.Value + ",",
                "		salvoQty	= " + numericUpDown_F16C_irSamFlare_SQ.Value + ",",
                "		salvoIntv	= " + numericUpDown_F16C_irSamFlare_SI.Value + ",",
                "	},",
                "}",
                "",
                "AN_ALE_47_FAILURE_TOTAL = 0",
                "AN_ALE_47_FAILURE_CONTAINER	= 1",
                "",
                "Damage = {	{Failure = AN_ALE_47_FAILURE_TOTAL, Failure_name = \"AN_ALE_47_FAILURE_TOTAL\", Failure_editor_name = _(\"AN/ALE-47 total failure\"),  Element = 10, Integrity_Treshold = 0.5, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
                "			{Failure = AN_ALE_47_FAILURE_CONTAINER, Failure_name = \"AN_ALE_47_FAILURE_CONTAINER\", Failure_editor_name = _(\"AN/ALE-47 container failure\"),  Element = 23, Integrity_Treshold = 0.75, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
                "}",
                "",
                "need_to_be_closed = true -- lua_state  will be closed in post_initialize()",
                "--Exported via Bailey's CMS Editor on " + System.DateTime.Now};

                if (isExportEnabled == true)
                {
                    System.IO.Directory.CreateDirectory(cmdsLua_F16C_FolderPath);
                    System.IO.File.WriteAllLines(cmdsLua_F16C_fullPath, luaExportString);

                    //https://stackoverflow.com/questions/5920882/file-move-does-not-work-file-already-exists
                    System.IO.Directory.CreateDirectory(exportPathBackup_F16C);
                    if (File.Exists(exportPathBackup_F16C + "\\CMDS_ALE47.lua"))
                    {
                        File.Delete(exportPathBackup_F16C + "\\CMDS_ALE47.lua");
                    }
                    System.IO.File.WriteAllLines(exportPathBackup_F16C + "\\CMDS_ALE47.txt", luaExportString);
                    File.Move(exportPathBackup_F16C + "\\CMDS_ALE47.txt", Path.ChangeExtension(exportPathBackup_F16C + "\\CMDS_ALE47.txt", ".lua"));


                    MessageBox.Show("Your F-16C CMDS file was exported to \r\n" + cmdsLua_F16C_fullPath + "\r\n\r\n" 
                        + "Your F-16C CMDS backup file was exported to \r\n" + exportPathBackup_F16C + "\\CMDS_ALE47.lua");
                }
                else
                {
                    MessageBox.Show("Please select your DCS.exe Location");
                }
            }
         }


        public void F18C_makeDefaultLua()
        {
                //MessageBox.Show("You were on tab 1");
                string[] luaExportString = { "local count = 0",
                "local function counter()",
                "    count = count + 1",
                "    return count",
                "end",
                "",
                "ProgramNames =",
                "{",
                "    MAN_1 = counter(),",
                "    MAN_2 = counter(),",
                "    MAN_3 = counter(),",
                "    MAN_4 = counter(),",
                "    MAN_5 = counter(),",
                "    MAN_6 = counter(),",
                "    AUTO_1 = counter(),",
                "    AUTO_2 = counter(),",
                "    AUTO_3 = counter(),",
                "    AUTO_4 = counter(),",
                "    AUTO_5 = counter(),",
                "    AUTO_6 = counter()",
                "}",
                "",
                "",
                "programs = {}",
                "",
                "-- Default manual presets",
                "-- MAN 1",
                "programs[ProgramNames.MAN_1] = {}",
                "programs[ProgramNames.MAN_1][\"chaff\"] = 1",
                "programs[ProgramNames.MAN_1][\"flare\"] = 1",
                "programs[ProgramNames.MAN_1][\"intv\"]  = 1.00",
                "programs[ProgramNames.MAN_1][\"cycle\"] = 10",
                "",
                "-- MAN 2",
                "programs[ProgramNames.MAN_2] = {}",
                "programs[ProgramNames.MAN_2][\"chaff\"] = 1",
                "programs[ProgramNames.MAN_2][\"flare\"] = 1",
                "programs[ProgramNames.MAN_2][\"intv\"]  = 0.5",
                "programs[ProgramNames.MAN_2][\"cycle\"] = 10",
                "",
                "-- MAN 3",
                "programs[ProgramNames.MAN_3] = {}",
                "programs[ProgramNames.MAN_3][\"chaff\"] = 2",
                "programs[ProgramNames.MAN_3][\"flare\"] = 2",
                "programs[ProgramNames.MAN_3][\"intv\"]  = 1.00",
                "programs[ProgramNames.MAN_3][\"cycle\"] = 5",
                "",
                "-- MAN 4",
                "programs[ProgramNames.MAN_4] = {}",
                "programs[ProgramNames.MAN_4][\"chaff\"] = 2",
                "programs[ProgramNames.MAN_4][\"flare\"] = 2",
                "programs[ProgramNames.MAN_4][\"intv\"]  = 0.5",
                "programs[ProgramNames.MAN_4][\"cycle\"] = 10",
                "",
                "-- MAN 5 - Chaff single",
                "programs[ProgramNames.MAN_5] = {}",
                "programs[ProgramNames.MAN_5][\"chaff\"] = 1",
                "programs[ProgramNames.MAN_5][\"flare\"] = 1",
                "programs[ProgramNames.MAN_5][\"intv\"]  = 1.00",
                "programs[ProgramNames.MAN_5][\"cycle\"] = 2",
                "",
                "-- MAN 6 - Wall Dispense button, Panic",
                "programs[ProgramNames.MAN_6] = {}",
                "programs[ProgramNames.MAN_6][\"chaff\"] = 2",
                "programs[ProgramNames.MAN_6][\"flare\"] = 2",
                "programs[ProgramNames.MAN_6][\"intv\"]  = 0.75",
                "programs[ProgramNames.MAN_6][\"cycle\"] = 20",
                "",
                "-- Auto presets",
                "-- Old generation radar SAM",
                "programs[ProgramNames.AUTO_1] = {}",
                "programs[ProgramNames.AUTO_1][\"chaff\"] = 1",
                "programs[ProgramNames.AUTO_1][\"flare\"] = 0",
                "programs[ProgramNames.AUTO_1][\"intv\"]  = 1",
                "programs[ProgramNames.AUTO_1][\"cycle\"] = 10",
                "",
                "-- Current generation radar SAM",
                "programs[ProgramNames.AUTO_2] = {}",
                "programs[ProgramNames.AUTO_2][\"chaff\"] = 2",
                "programs[ProgramNames.AUTO_2][\"flare\"] = 0",
                "programs[ProgramNames.AUTO_2][\"intv\"]  = 0.50",
                "programs[ProgramNames.AUTO_2][\"cycle\"] = 10",
                "",
                "-- IR SAM",
                "programs[ProgramNames.AUTO_3] = {}",
                "programs[ProgramNames.AUTO_3][\"chaff\"] = 0",
                "programs[ProgramNames.AUTO_3][\"flare\"] = 2",
                "programs[ProgramNames.AUTO_3][\"intv\"]  = 0.25",
                "programs[ProgramNames.AUTO_3][\"cycle\"] = 5",
                "",
                "",
                "need_to_be_closed = true -- lua_state  will be closed in post_initialize()",
                "--Exported via Bailey's CMS Editor on " + System.DateTime.Now};
                // WriteAllLines creates a file, writes a collection of strings to the file,
                // and then closes the file.  You do NOT need to call Flush() or Close().
                //System.IO.File.WriteAllLines(@"C:\TestFolder\WriteLines.txt", lines);

                if (isExportEnabled == true)
                {
                    System.IO.Directory.CreateDirectory(cmdsLua_F18C_FolderPath);
                    System.IO.File.WriteAllLines(cmdsLua_F18C_fullPath, luaExportString);
                    //https://stackoverflow.com/questions/5920882/file-move-does-not-work-file-already-exists
               
                }
                else
                {
                    MessageBox.Show("Please select your DCS.exe location.");
                }
            }

        public void F16C_makeDefaultLua()
        {
            string[] luaExportString = {"local gettext = require(\"i_18n\")",
                "_ = gettext.translate",
                "",
                "local count = 0",
                "local function counter()",
                "	count = count + 1",
                "	return count",
                "end",
                "",
                "ProgramNames =",
                "{",
                "	MAN_1 = counter(),",
                "	MAN_2 = counter(),",
                "	MAN_3 = counter(),",
                "	MAN_4 = counter(),",
                "	MAN_5 = counter(),",
                "	MAN_6 = counter(),",
                "	AUTO_1 = counter(),",
                "	AUTO_2 = counter(),",
                "	AUTO_3 = counter(),",
                "	AUTO_4 = counter(),",
                "	AUTO_5 = counter(),",
                "	AUTO_6 = counter(),",
                "}",
                "",
                "programs = {}",
                "",
                "-- Default manual presets",
                "-- MAN 1",
                "programs[ProgramNames.MAN_1] = {",
                "	chaff = {",
                "		burstQty 	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 10,",
                "		salvoIntv	= 1.0,",
                "	},",
                "	flare = {",
                "		burstQty	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 10,",
                "		salvoIntv	= 1.0,",
                "	},",
                "}",
                "",
                "-- MAN 2",
                "programs[ProgramNames.MAN_2] = {",
                "	chaff = {",
                "		burstQty 	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 10,",
                "		salvoIntv	= 0.5,",
                "	},",
                "	flare = {",
                "		burstQty	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 10,",
                "		salvoIntv	= 0.5,",
                "	},",
                "}",
                "",
                "-- MAN 3",
                "programs[ProgramNames.MAN_3] = {",
                "	chaff = {",
                "		burstQty 	= 2,",
                "		burstIntv	= 0.1,",
                "		salvoQty	= 5,",
                "		salvoIntv	= 1.0,",
                "	},",
                "	flare = {",
                "		burstQty	= 2,",
                "		burstIntv	= 0.1,",
                "		salvoQty	= 5,",
                "		salvoIntv	= 1.0,",
                "	},",
                "}",
                "",
                "-- MAN 4",
                "programs[ProgramNames.MAN_4] = {",
                "	chaff = {",
                "		burstQty 	= 2,",
                "		burstIntv	= 0.1,",
                "		salvoQty	= 5,",
                "		salvoIntv	= 0.5,",
                "	},",
                "	flare = {",
                "		burstQty	= 2,",
                "		burstIntv	= 0.1,",
                "		salvoQty	= 5,",
                "		salvoIntv	= 0.5,",
                "	},",
                "}",
                "",
                "-- MAN 5 - Wall Dispense button, Panic",
                "programs[ProgramNames.MAN_5] = {",
                "	chaff = {",
                "		burstQty 	= 2,",
                "		burstIntv	= 0.05,",
                "		salvoQty	= 20,",
                "		salvoIntv	= 0.75,",
                "	},",
                "	flare = {",
                "		burstQty	= 2,",
                "		burstIntv	= 0.05,",
                "		salvoQty	= 20,",
                "		salvoIntv	= 0.75,",
                "	},",
                "}",
                "",
                "-- MAN 6 - BYPASS mode",
                "programs[ProgramNames.MAN_6] = {",
                "	chaff = {",
                "		burstQty 	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 1,",
                "		salvoIntv	= 0.5,",
                "	},",
                "	flare = {",
                "		burstQty	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 1,",
                "		salvoIntv	= 0.5,",
                "	},",
                "}",
                "",
                "-- Auto presets",
                "-- Old generation radar SAM",
                "programs[ProgramNames.AUTO_1] = {",
                "	chaff = {",
                "		burstQty 	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 1,",
                "		salvoIntv	= 0.5,",
                "	},",
                "	flare = {",
                "		burstQty	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 1,",
                "		salvoIntv	= 0.5,",
                "	},",
                "}",
                "",
                "-- Current generation radar SAM",
                "programs[ProgramNames.AUTO_2] = {",
                "	chaff = {",
                "		burstQty 	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 1,",
                "		salvoIntv	= 0.5,",
                "	},",
                "	flare = {",
                "		burstQty	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 1,",
                "		salvoIntv	= 0.5,",
                "	},",
                "}",
                "",
                "-- IR SAM",
                "programs[ProgramNames.AUTO_3] = {",
                "	chaff = {",
                "		burstQty 	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 1,",
                "		salvoIntv	= 0.5,",
                "	},",
                "	flare = {",
                "		burstQty	= 1,",
                "		burstIntv	= 0.02,",
                "		salvoQty	= 1,",
                "		salvoIntv	= 0.5,",
                "	},",
                "}",
                "",
                "AN_ALE_47_FAILURE_TOTAL = 0",
                "AN_ALE_47_FAILURE_CONTAINER	= 1",
                "",
                "Damage = {	{Failure = AN_ALE_47_FAILURE_TOTAL, Failure_name = \"AN_ALE_47_FAILURE_TOTAL\", Failure_editor_name = _(\"AN/ALE-47 total failure\"),  Element = 10, Integrity_Treshold = 0.5, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
                "			{Failure = AN_ALE_47_FAILURE_CONTAINER, Failure_name = \"AN_ALE_47_FAILURE_CONTAINER\", Failure_editor_name = _(\"AN/ALE-47 container failure\"),  Element = 23, Integrity_Treshold = 0.75, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
                "}",
                "",
                "need_to_be_closed = true -- lua_state  will be closed in post_initialize()",
                "--Exported via Bailey's CMS Editor on " + System.DateTime.Now};

            if (isExportEnabled == true)
            {
                System.IO.Directory.CreateDirectory(cmdsLua_F16C_FolderPath);
                System.IO.File.WriteAllLines(cmdsLua_F16C_fullPath, luaExportString);
                //https://stackoverflow.com/questions/5920882/file-move-does-not-work-file-already-exists
            }
            else
            {
                MessageBox.Show("Please select your DCS.exe Location");
            }
        }


        public void loadAllCmsAfterUserSelectedExe()//loads the countermeasure files that the user has and the program supports
        {
            if (File.Exists(cmdsLua_F18C_fullPath))//this is the f18
            {
                loadLocation = cmdsLua_F18C_fullPath;
                loadLua_F18C();
            }
            if (File.Exists(cmdsLua_F16C_fullPath))//this is the f16
            {
                loadLocation = cmdsLua_F16C_fullPath;
                loadLua_F16C();
            }
        }



        private void button_loadCM_DCS_Click(object sender, EventArgs e)
        {
            //LoadCountermeasure file button
            loadCmsFromDcs();
        }

        public void loadCmsFromDcs()
        {
            //we want ot load the countermeasure file from dcs
            //click dcs cms load
            //if the files are there, load them
            //you know if the files are there ifthe location was set on load or set via the user
            //if they are not there, tell the user you could not find them
           
            if (File.Exists(appPath + "\\DCS-CMS-Editor-Backup\\DCS-CMS-Editor-UserSettings.txt"))//if the user settings have been set continue
            {
                if (tabControl_mainTab.SelectedTab == tabPage1 && File.Exists(cmdsLua_F18C_fullPath))//this is the f18 tab
                {
                    loadLocation = cmdsLua_F18C_fullPath;
                    loadLua_F18C();
                }
                else if (tabControl_mainTab.SelectedTab == tabPage2 && File.Exists(cmdsLua_F16C_fullPath))//this is the f16 tab
                {
                    loadLocation = cmdsLua_F16C_fullPath;
                    loadLua_F16C();
                }
                else{MessageBox.Show("DCS Countermeasure files not found. Please select your DCS.exe location or try a different aircraft.");}
            }

            else
            {
                MessageBox.Show("DCS Countermeasure files not found. Please select your DCS.exe location or try a different aircraft.");
            }
        }

        private void button_help_Click(object sender, EventArgs e)
        {
            warningMessage();
        }

        public void warningMessage()
        {
            MessageBox.Show("WARNING!!! " +
                "\r\n" +
                "\r\n" +
                "This utility creates, deletes, and modifies its own files and files that you may specify. If you are not comfortable with this, please do not use this utility. You may have to run this utility in Admin Mode.\r\n" +
            "\r\n" + "\r\n" +
            "1.Set the 'DCS.exe' location. This file is most likely located at C:\\Program Files\\Eagle Dynamics\\DCS World OpenBeta\\bin\\DCS.exe. The location you chose will be saved in a '.txt' file in a backup folder that this utility creates." +
            "\r\n" + "\r\n" +
            "2.Select your aircraft at the top and modify the countermeasure values using the controls in the utility." +
            "\r\n" + "\r\n" +
            "3.Click the ‘Export’ button. This will export a ‘CMDS_ALE47.lua’ to DCS, and it will export a copy to a backup folder that the utility creates. The backup can be used incase the DCS location lua is overwritten by a DCS update, for example." +
            "\r\n" + "\r\n" +
            "4.Load the DCS mission and have fun!" +
            "\r\n" + "\r\n" +
            "A lot of time has been spent making this utility. I do not do this for a living. I have Google, Visual Studio, and some time on my hands. There may be bugs. Please use the utility as intended. If you have any questions, comments, improvements, bugs, concerns, input, or just want to say thanks, please contact me via Discord: Bailey#6230" +
            "\r\n" + "\r\n" +
            "Please feel free to donate here: https://www.paypal.me/asherao." +
            "\r\n" + "All donations go to making more free Utilities for DCS, like this one!" +
            "\r\n" + "\r\n" +
            "Thank you to Arctic Fox for the idea and collaboration." +
            "\r\n" + "\r\n" +
            "~Bailey" + "\r\n" +
            "24JUL2020" + "\r\n" +
            "v2.0", "DCS CMS Editor by Bailey READMEE");
        }

       

        private void button_openBackupPath_Click(object sender, EventArgs e)//when the Open Folder button is clicked for backup Location
        {
            if (Directory.Exists(textBox_backupPath.Text))//if the directory in the box exists
            {
                string Backup_DCS_Path = textBox_backupPath.Text;
                Process.Start("explorer.exe", Backup_DCS_Path);//open explorer to the directory
            }
        }

        private void button_openDCS_Path_Click(object sender, EventArgs e)//when the Open Folder button is clicked for DCS Location
        {
            if (Directory.Exists(textBox_DcsPath.Text))//if the directory in the box exists
            {
                string User_DCS_Path = textBox_DcsPath.Text;
                Process.Start("explorer.exe", User_DCS_Path);//open explorer to the directory
            }
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void label_ManualPresetIRSam_Click(object sender, EventArgs e)
        {

        }

        private void label43_Click(object sender, EventArgs e)
        {

        }

        private void label_DCS_Path2_Click(object sender, EventArgs e)
        {

        }
        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
        private void numericUpDown_OldSamInterval_ValueChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown52_ValueChanged(object sender, EventArgs e)
        {

        }

        private void button_recreateOrginalLua_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to clear and reset the DCS CMDS Lua for this aircraft? This cannot be undone.", "Are you sure?", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                //do something
                if (tabControl_mainTab.SelectedTab == tabPage1)//this is the f18 tab
                {
                    //insert method here
                    if (cmdsLua_F18C_FolderPath == null)
                    {
                       MessageBox.Show("DCS.exe has not been set. Please select your DCS.exe location or try a different aircraft.");
                    }
                    else
                    {
                        F18C_makeDefaultLua();
                        MessageBox.Show("Your F-18C CMDS file was exported to \r\n" + cmdsLua_F18C_fullPath + ".");
                    }
                }
                else if(tabControl_mainTab.SelectedTab == tabPage2)//this is the f16 tab
                {
                    //insert method here
                    if (cmdsLua_F16C_FolderPath == null)
                    {
                        MessageBox.Show("DCS.exe has not been set. Please select your DCS.exe location or try a different aircraft.");
                    }
                    else
                    {
                        F16C_makeDefaultLua();
                        MessageBox.Show("Your F-16C CMDS file was exported to \r\n" + cmdsLua_F16C_fullPath + ".");
                    }
                }
            }
            else if (dialogResult == DialogResult.No)
            {
                //do something else
            }
        }
    }
}
