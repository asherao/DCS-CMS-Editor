using System;
using System.Collections;
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
 * DCS aircraft like the F/A-18C, F-16C, and A-10C. This program are for people who
 * can't wait for the ED DCS implementation of the DTC features that you can find on the JF-17.
 * 
 * 
 * Tasks Competed:
 * -Made general GUI
 * -Did some other stuff
 * -F18 CMS import and export
 * -F16 CMS import and export
 * -A10C CMS import and export
 * 
 * TODO:
 * -Make video (maybe)
 * -Add instructions on how to add more aircraft
 * -Add F16 Harm Import and Export
 * -Consider adding support for A-10C II
 * 
 * 
 * Countermeasure Lua locations for DCS aircraft and other helpful paths
 * -\DCS World OpenBeta\Mods\aircraft\F-16C\Cockpit\Scripts\EWS\CMDS\device\CMDS_ALE47.lua
 * -\DCS World OpenBeta\Mods\aircraft\FA-18C\Cockpit\Scripts\TEWS\device\CMDS_ALE47.lua
 * -\DCS World OpenBeta\Mods\aircraft\A-10C\Cockpit\Scripts\AN_ALE40V\device\AN_ALE40V_params.lua
 * 
 * -\DCS World OpenBeta\Mods\aircraft\F-5E\Cockpit\Scripts\Systems\AN_ALE40V.lua //it looks like the cmds can only be programed on-game when the engine is off
 * -\DCS World OpenBeta\Mods\aircraft\M-2000C\Cockpit\Scripts\SPIRALE.lua
 * -\DCS World OpenBeta\Mods\aircraft\AV8BNA\Cockpit\Scripts\EWS\EW_Dispensers_init.lua
 * -\DCS World OpenBeta\bin\DCS.exe
 * 
 * 
 * Version Notes:
 * v1
 * -Added F-18C CMS editing
 * -Release
 * -About 1000 lines of code total
 * 
 * v2
 * -Added F-16C CMS editing
 * -CMS export is enabled even if default aircraft directory is not detected
 * -Added option to re-create orginal DCS CMS lua
 * -Adjusted numerical box max, min, and adjustment values to match aircraft
 * -Replaced sliders with numerical boxes
 * -Adjusted GUI
 * -About 2700 lines of code total
 * 
 * v2.1
 * -Uses en-US culture in C# to ensure periods are used for decimals instead of commas
 * 
 * v3
 * -Added A-10C CMS editing
 * -Added Preliminary F-16C HARM Table Export support. Please visit the 'Enable Editing Of Default DED HARM Tables Via A Lua File' thread on the ED forums to ask ED's help to implement the feature. https://forums.eagle.ru/showthread.php?t=286963
 * -About 5481 lines of code total
 * 
 * v4
 * -Added M2000 CMS editing
 * -Added A-10C2 Tank Killer CMS editing (inherited? see Bugs)
 * -Added AV8B CMS editing
 * -Fixed a bug with the export of an aircraft failing
 * -Fixed a bug were the program would crash if you tried to load a CMS profile from DCS, but the file was not there
 * -About 7202 lines of code total
 *  
 * 
 * vFuture
 * -Add F16c HARM maker. Have to wait for the release of the feature
 * -Add F5 (looks not possible)
 * 
 * 
 * Bugs:
 * -Due to the way ED has coded the A-10C and A-10C2, both aircraft may or may not share the same CMS file (feature?)
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

            //numericUpDown_A10C_programA_chaff.Controls[0].Hide();//this should hide the numeric updown arrows on spawn. nevermind, this method does weird stuff
            //numericUpDown_A10C_programA_chaff.Controls[0].Visible = false;//this should hide the numeric updown arrows on spawn. nevermind, this method does weird stuff
            //numericUpDown_A10C_programA_chaff.Controls.RemoveAt(0);//this should hide the numeric updown arrows on spawn. nevermind, this method does weird stuff


            //init the harm combo boxes
            initF16cHarmComboBoxes();
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
                cmdsLua_F18C_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\FA-18C\Cockpit\Scripts\TEWS\device";
                cmdsLua_F16C_fullPath = dcs_topFolderPath + @"\Mods\aircraft\F-16C\Cockpit\Scripts\EWS\CMDS\device\CMDS_ALE47.lua";
                cmdsLua_F16C_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\F-16C\Cockpit\Scripts\EWS\CMDS\device";
                cmdsLua_A10C_fullPath = dcs_topFolderPath + @"\Mods\aircraft\A-10C\Cockpit\Scripts\AN_ALE40V\device\AN_ALE40V_params.lua";
                cmdsLua_A10C_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\A-10C\Cockpit\Scripts\AN_ALE40V\device";
                cmdsLua_M2000C_fullPath = dcs_topFolderPath + @"\Mods\aircraft\M-2000C\Cockpit\Scripts\SPIRALE.lua";
                cmdsLua_M2000C_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\M-2000C\Cockpit\Scripts";
                cmdsLua_AV8B_fullPath = dcs_topFolderPath + @"\Mods\aircraft\AV8BNA\Cockpit\Scripts\EWS\EW_Dispensers_init.lua";
                cmdsLua_AV8B_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\AV8BNA\Cockpit\Scripts\EWS\";

                textBox_DcsPath.Text = dcs_topFolderPath;
                textBox_backupPath.Text = appPath + "\\DCS-CMS-Editor-Backup";
                exportPathMain = selectedPath_dcsExe;
                exportPathBackup = (appPath + "\\DCS-CMS-Editor-Backup");
                exportPathBackup_F18C = (exportPathBackup + "\\F18C Backup");
                exportPathBackup_F16C = (exportPathBackup + "\\F16C Backup");
                exportPathBackup_A10C = (exportPathBackup + "\\A10C Backup");
                exportPathBackup_AV8B = (exportPathBackup + "\\AV8B Backup");
                exportPathBackup_M2000C = (exportPathBackup + "\\M2000C Backup");

                //also, load the dcs CMS settings as default
                //loadCM_DCS_Click();
            }
        }



        string selectedFileName;//this is case sensitive. this means that if there is a different in capatialization in the file directory, it will fail and think its wrong
        string exportPathMain;
        string exportPathBackup;
        string exportPathBackup_F18C;
        string exportPathBackup_F16C;
        string exportPathBackup_A10C;
        string exportPathBackup_AV8B;
        string exportPathBackup_M2000C;
        bool isExportEnabled;
        bool isExportPathSelected;
        string selectedPath_dcsExe;
        string dcs_topFolderPath;
        string cmdsLua_F18C_fullPath;
        string cmdsLua_F16C_fullPath;
        string cmdsLua_F18C_FolderPath;
        string cmdsLua_F16C_FolderPath;
        string cmdsLua_A10C_fullPath;
        string cmdsLua_A10C_FolderPath;
        string cmdsLua_M2000C_fullPath;
        string cmdsLua_M2000C_FolderPath;
        string cmdsLua_AV8B_fullPath;
        string cmdsLua_AV8B_FolderPath;

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
                    cmdsLua_A10C_fullPath = dcs_topFolderPath + @"\Mods\aircraft\A-10C\Cockpit\Scripts\AN_ALE40V\device\AN_ALE40V_params.lua";
                    cmdsLua_A10C_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\A-10C\Cockpit\Scripts\AN_ALE40V\device";
                    cmdsLua_M2000C_fullPath = dcs_topFolderPath + @"\Mods\aircraft\M-2000C\Cockpit\Scripts\SPIRALE.lua";
                    cmdsLua_M2000C_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\M-2000C\Cockpit\Scripts";
                    cmdsLua_AV8B_fullPath = dcs_topFolderPath + @"\Mods\aircraft\AV8BNA\Cockpit\Scripts\EWS\EW_Dispensers_init.lua";
                    cmdsLua_AV8B_FolderPath = dcs_topFolderPath + @"\Mods\aircraft\AV8BNA\Cockpit\Scripts\EWS\";

                    textBox_DcsPath.Text = dcs_topFolderPath;
                    textBox_backupPath.Text = appPath + "\\DCS-CMS-Editor-Backup";
                    exportPathMain = selectedPath_dcsExe;
                    exportPathBackup = (appPath + "\\DCS-CMS-Editor-Backup");
                    exportPathBackup_F18C = (exportPathBackup + "\\F18C Backup");
                    exportPathBackup_F16C = (exportPathBackup + "\\F16C Backup");
                    exportPathBackup_A10C = (exportPathBackup + "\\A10C Backup");
                    exportPathBackup_AV8B = (exportPathBackup + "\\AV8B Backup");
                    exportPathBackup_M2000C = (exportPathBackup + "\\M2000C Backup");


                    MessageBox.Show("You selected " + selectedPath_dcsExe + "\r\n"
                       + "\r\n"
                       + "F-18C lua should be located here: " + cmdsLua_F18C_fullPath + "\r\n"
                       + "\r\n"
                       + "F-16C lua should be located here: " + cmdsLua_F16C_fullPath + "\r\n"
                       + "\r\n"
                       + "A-10C lua should be located here: " + cmdsLua_A10C_fullPath + "\r\n"
                       + "\r\n"
                       + "AV-8B lua should be located here: " + cmdsLua_AV8B_fullPath + "\r\n"
                       + "\r\n"
                       + "M2000C lua should be located here: " + cmdsLua_M2000C_fullPath + "\r\n"
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


        private void button4_Click(object sender, EventArgs e)//load from backup button
        {
            //MessageBox.Show(tabControl_mainTab.SelectedTab.Name.ToString());
            if(File.Exists(appPath + "\\DCS-CMS-Editor-Backup\\DCS-CMS-Editor-UserSettings.txt"))//if the user settings have been set continue
            {
                if (tabControl_mainTab.SelectedTab == tabPage1)//this is the f18c tab
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
                    
                }
                else if (tabControl_mainTab.SelectedTab == tabPage2)//f16c cms page
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
                else if (tabControl_mainTab.SelectedTab == tabPage3)//f16c harm page
                {
                    harmForF16cIsNotAvailableMessage();
                    /*
                    if (File.Exists(appPath + "\\DCS-CMS-Editor-Backup\\F16C Backup\\CMDS_ALE47.lua"))
                    {
                        loadLocation = (appPath + "\\DCS-CMS-Editor-Backup\\F16C Backup\\CMDS_ALE47.lua");
                        loadLua_F16C();
                    }
                    else
                    {
                        MessageBox.Show("Backup file not found. Please select your DCS.exe and export to generate a backup file.");
                    }
                    */
                }
                else if (tabControl_mainTab.SelectedTab == tabPage4)//a10c cms page
                {
                    if (File.Exists(appPath + "\\DCS-CMS-Editor-Backup\\A10C Backup\\AN_ALE40V_params.lua"))
                    {
                        loadLocation = (appPath + "\\DCS-CMS-Editor-Backup\\A10C Backup\\AN_ALE40V_params.lua");
                        loadLua_A10C_CMS();
                    }
                    else
                    {
                        MessageBox.Show("Backup file not found. Please select your DCS.exe and export to generate a backup file.");
                    }
                }
                else if (tabControl_mainTab.SelectedTab == tabPage7)//av8b cms page
                {
                    if (File.Exists(appPath + "\\DCS-CMS-Editor-Backup\\AV8B Backup\\EW_Dispensers_init.lua"))
                    {
                        loadLocation = (appPath + "\\DCS-CMS-Editor-Backup\\AV8B Backup\\EW_Dispensers_init.lua");
                        loadLua_AV8B_CMS();
                    }
                    else
                    {
                        MessageBox.Show("Backup file not found. Please select your DCS.exe and export to generate a backup file.");
                    }
                }
                else if (tabControl_mainTab.SelectedTab == tabPage8)//m2000c cms page
                {
                    if (File.Exists(appPath + "\\DCS-CMS-Editor-Backup\\M2000C Backup\\SPIRALE.lua"))
                    {
                        loadLocation = (appPath + "\\DCS-CMS-Editor-Backup\\M2000C Backup\\SPIRALE.lua");
                        loadLua_M2000C_CMS();
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
            //System.Windows.Forms.MessageBox.Show("Press \"OK\" to delete your entire harddrive. Just kidding.");


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



        private void loadLua_F16C()//do this like the loadLua_F18 method============================================
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

        private void loadLua_A10C_CMS()
        {
            //find the lua file
            string CountermeasureFileString_A10C = loadLocation;
            //load the text into a string
            string CountermeasureFileStringText_A10C = File.ReadAllText(CountermeasureFileString_A10C);
            //https://www.techiedelight.com/read-entire-file-to-string-csharp/

            //https://stackoverflow.com/questions/4776259/detecting-where-a-newline-is-located-in-a-string

            //consider making a prompt for programs that were not imported.

            string [] arrayOfLua_A10C_CMS = File.ReadAllLines(CountermeasureFileString_A10C);//put the file into an array https://stackoverflow.com/questions/31853731/read-a-preceding-line-of-a-text-file-if-current-line-contains-x
            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)//for as many files as there are in the array
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['N'] = {}"))//of you find the lead of a countermeasure program
                {
                    string programN_name = arrayOfLua_A10C_CMS[i - 1];//put the previous line in a string
                    if (programN_name.Length > 1 && programN_name.Substring(0,2).ToString() == "--")//this prevents empty lines from being grabbed and makes sure that the line that is grabbed is a lua comment line
                    {
                        programN_name = programN_name.Substring(2);//cut off the first two letters wich sould be '--' https://stackoverflow.com/questions/3222125/fastest-way-to-remove-first-char-in-a-string
                        textBox_programN.Text = programN_name;//put the result in the textbox
                        break;//breaks out of the for loop and prevents else statement below from running
                        //https://stackoverflow.com/questions/19524105/how-to-block-or-restrict-special-characters-from-textbox
                    }
                }
                else//the program does not exist
                {
                    textBox_programN.Text = "Custom Program N";//enter a default name
                }
            }

          
            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['O'] = {}"))
                {
                    string programO_name = arrayOfLua_A10C_CMS[i - 1];
                    //MessageBox.Show(programO_name.Substring(0, 2).ToString());
                    if (programO_name.Length > 1 && programO_name.Substring(0, 2).ToString() == "--")
                    {
                        programO_name = programO_name.Substring(2);
                        textBox_programO.Text = programO_name;
                        break;
                    }
                }
                else
                {
                    textBox_programO.Text = "Custom Program O";
                }
            }


            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['P'] = {}"))
                {
                    string programP_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programP_name.Length > 1 && programP_name.Substring(0, 2).ToString() == "--")
                    {
                        programP_name = programP_name.Substring(2);
                        textBox_programP.Text = programP_name;
                        break;
                    }
                }
                else
                {
                    textBox_programP.Text = "Custom Program P";
                }
            }



            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['Q'] = {}"))
                {
                    string programQ_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programQ_name.Length > 1 && programQ_name.Substring(0, 2).ToString() == "--")
                    {
                        programQ_name = programQ_name.Substring(2);
                        textBox_programQ.Text = programQ_name;
                        break;
                    }
                }
                else
                {
                    textBox_programQ.Text = "Custom Program Q";
                }
            }

            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['R'] = {}"))
                {
                    string programR_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programR_name.Length > 1 && programR_name.Substring(0, 2).ToString() == "--")
                    {
                        programR_name = programR_name.Substring(2);
                        textBox_programR.Text = programR_name;
                        break;
                    }
                }
                else
                {
                    textBox_programR.Text = "Custom Program R";
                }
            }

            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['S'] = {}"))
                {
                    string programS_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programS_name.Length > 1 && programS_name.Substring(0, 2).ToString() == "--")
                    {
                        programS_name = programS_name.Substring(2);
                        textBox_programS.Text = programS_name;
                        break;
                    }
                }
                else
                {
                    textBox_programS.Text = "Custom Program S";
                }
            }

            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['T'] = {}"))
                {
                    string programT_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programT_name.Length > 1 && programT_name.Substring(0, 2).ToString() == "--")
                    {
                        programT_name = programT_name.Substring(2);
                        textBox_programT.Text = programT_name;
                        break;
                    }
                }
                else
                {
                    textBox_programT.Text = "Custom Program T";
                }
            }

            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['U'] = {}"))
                {
                    string programU_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programU_name.Length > 1 && programU_name.Substring(0, 2).ToString() == "--")
                    {
                        programU_name = programU_name.Substring(2);
                        textBox_programU.Text = programU_name;
                        break;
                    }
                }
                else
                {
                    textBox_programU.Text = "Custom Program U";
                }
            }

            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['V'] = {}"))
                {
                    string programV_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programV_name.Length > 1 && programV_name.Substring(0, 2).ToString() == "--")
                    {
                        programV_name = programV_name.Substring(2);
                        textBox_programV.Text = programV_name;
                        break;
                    }
                }
                else
                {
                    textBox_programV.Text = "Custom Program V";
                }
            }

            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['W'] = {}"))
                {
                    string programW_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programW_name.Length > 1 && programW_name.Substring(0, 2).ToString() == "--")
                    {
                        programW_name = programW_name.Substring(2);
                        textBox_programW.Text = programW_name;
                        break;
                    }
                }
                else
                {
                    textBox_programW.Text = "Custom Program W";
                }
            }

            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['X'] = {}"))
                {
                    string programX_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programX_name.Length > 1 && programX_name.Substring(0, 2).ToString() == "--")
                    {
                        programX_name = programX_name.Substring(2);
                        textBox_programX.Text = programX_name;
                        break;
                    }
                }
                else
                {
                    textBox_programX.Text = "Custom Program X";
                }
            }

            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['Y'] = {}"))
                {
                    string programY_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programY_name.Length > 1 && programY_name.Substring(0, 2).ToString() == "--")
                    {
                        programY_name = programY_name.Substring(2);
                        textBox_programY.Text = programY_name;
                        break;
                    }
                }
                else
                {
                    textBox_programY.Text = "Custom Program Y";
                }
            }

            for (int i = 0; i < arrayOfLua_A10C_CMS.Length; i++)
            {
                if (arrayOfLua_A10C_CMS[i].Contains("programs['Z'] = {}"))
                {
                    string programZ_name = arrayOfLua_A10C_CMS[i - 1];
                    if (programZ_name.Length > 1 && programZ_name.Substring(0, 2).ToString() == "--")
                    {
                        programZ_name = programZ_name.Substring(2);
                        textBox_programZ.Text = programZ_name;
                        break;
                    }
                }
                else
                {
                    textBox_programZ.Text = "Custom Program Z";
                }
            }
            int A10C_programA_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['A'][\"chaff\"] =") + 24;
            int A10C_programA_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programA_chaff_getIndexStart);
            string A10C_programA_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programA_chaff_getIndexStart, A10C_programA_chaff_getIndexEnd - A10C_programA_chaff_getIndexStart);
            try { numericUpDown_A10C_programA_chaff.Value = int.Parse(A10C_programA_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programA_chaff.Value = numericUpDown_A10C_programA_chaff.Minimum; }

            int A10C_programA_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['A'][\"flare\"] =") + 24;
            int A10C_programA_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programA_flare_getIndexStart);
            string A10C_programA_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programA_flare_getIndexStart, A10C_programA_flare_getIndexEnd - A10C_programA_flare_getIndexStart);
            try { numericUpDown_A10C_programA_flare.Value = int.Parse(A10C_programA_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programA_flare.Value = numericUpDown_A10C_programA_flare.Minimum; }

            int A10C_programA_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['A'][\"intv\"]  =") + 24;
            int A10C_programA_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programA_interval_getIndexStart);
            string A10C_programA_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programA_interval_getIndexStart, A10C_programA_interval_getIndexEnd - A10C_programA_interval_getIndexStart);
            try { numericUpDown_A10C_programA_interval.Value = Decimal.Parse(A10C_programA_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programA_interval.Value = numericUpDown_A10C_programA_interval.Minimum; }

            int A10C_programA_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['A'][\"cycle\"] =") + 24;
            int A10C_programA_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programA_cycle_getIndexStart);
            string A10C_programA_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programA_cycle_getIndexStart, A10C_programA_cycle_getIndexEnd - A10C_programA_cycle_getIndexStart);
            try { numericUpDown_A10C_programA_cycle.Value = int.Parse(A10C_programA_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programA_cycle.Value = numericUpDown_A10C_programA_cycle.Minimum; }




            int A10C_programB_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['B'][\"chaff\"] =") + 24;
            int A10C_programB_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programB_chaff_getIndexStart);
            string A10C_programB_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programB_chaff_getIndexStart, A10C_programB_chaff_getIndexEnd - A10C_programB_chaff_getIndexStart);
            try { numericUpDown_A10C_programB_chaff.Value = int.Parse(A10C_programB_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programB_chaff.Value = numericUpDown_A10C_programB_chaff.Minimum; }

            int A10C_programB_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['B'][\"flare\"] =") + 24;
            int A10C_programB_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programB_flare_getIndexStart);
            string A10C_programB_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programB_flare_getIndexStart, A10C_programB_flare_getIndexEnd - A10C_programB_flare_getIndexStart);
            try { numericUpDown_A10C_programB_flare.Value = int.Parse(A10C_programB_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programB_flare.Value = numericUpDown_A10C_programB_flare.Minimum; }

            int A10C_programB_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['B'][\"intv\"]  =") + 24;
            int A10C_programB_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programB_interval_getIndexStart);
            string A10C_programB_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programB_interval_getIndexStart, A10C_programB_interval_getIndexEnd - A10C_programB_interval_getIndexStart);
            try { numericUpDown_A10C_programB_interval.Value = Decimal.Parse(A10C_programB_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programB_interval.Value = numericUpDown_A10C_programB_interval.Minimum; }

            int A10C_programB_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['B'][\"cycle\"] =") + 24;
            int A10C_programB_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programB_cycle_getIndexStart);
            string A10C_programB_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programB_cycle_getIndexStart, A10C_programB_cycle_getIndexEnd - A10C_programB_cycle_getIndexStart);
            try { numericUpDown_A10C_programB_cycle.Value = int.Parse(A10C_programB_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programB_cycle.Value = numericUpDown_A10C_programB_cycle.Minimum; }




            int A10C_programC_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['C'][\"chaff\"] =") + 24;
            int A10C_programC_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programC_chaff_getIndexStart);
            string A10C_programC_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programC_chaff_getIndexStart, A10C_programC_chaff_getIndexEnd - A10C_programC_chaff_getIndexStart);
            try { numericUpDown_A10C_programC_chaff.Value = int.Parse(A10C_programC_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programC_chaff.Value = numericUpDown_A10C_programC_chaff.Minimum; }

            int A10C_programC_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['C'][\"flare\"] =") + 24;
            int A10C_programC_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programC_flare_getIndexStart);
            string A10C_programC_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programC_flare_getIndexStart, A10C_programC_flare_getIndexEnd - A10C_programC_flare_getIndexStart);
            try { numericUpDown_A10C_programC_flare.Value = int.Parse(A10C_programC_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programC_flare.Value = numericUpDown_A10C_programC_flare.Minimum; }

            int A10C_programC_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['C'][\"intv\"]  =") + 24;
            int A10C_programC_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programC_interval_getIndexStart);
            string A10C_programC_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programC_interval_getIndexStart, A10C_programC_interval_getIndexEnd - A10C_programC_interval_getIndexStart);
            try { numericUpDown_A10C_programC_interval.Value = Decimal.Parse(A10C_programC_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programC_interval.Value = numericUpDown_A10C_programC_interval.Minimum; }

            int A10C_programC_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['C'][\"cycle\"] =") + 24;
            int A10C_programC_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programC_cycle_getIndexStart);
            string A10C_programC_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programC_cycle_getIndexStart, A10C_programC_cycle_getIndexEnd - A10C_programC_cycle_getIndexStart);
            try { numericUpDown_A10C_programC_cycle.Value = int.Parse(A10C_programC_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programC_cycle.Value = numericUpDown_A10C_programC_cycle.Minimum; }




            int A10C_programD_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['D'][\"chaff\"] =") + 24;
            int A10C_programD_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programD_chaff_getIndexStart);
            string A10C_programD_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programD_chaff_getIndexStart, A10C_programD_chaff_getIndexEnd - A10C_programD_chaff_getIndexStart);
            try { numericUpDown_A10C_programD_chaff.Value = int.Parse(A10C_programD_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programD_chaff.Value = numericUpDown_A10C_programD_chaff.Minimum; }

            int A10C_programD_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['D'][\"flare\"] =") + 24;
            int A10C_programD_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programD_flare_getIndexStart);
            string A10C_programD_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programD_flare_getIndexStart, A10C_programD_flare_getIndexEnd - A10C_programD_flare_getIndexStart);
            try { numericUpDown_A10C_programD_flare.Value = int.Parse(A10C_programD_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programD_flare.Value = numericUpDown_A10C_programD_flare.Minimum; }

            int A10C_programD_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['D'][\"intv\"]  =") + 24;
            int A10C_programD_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programD_interval_getIndexStart);
            string A10C_programD_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programD_interval_getIndexStart, A10C_programD_interval_getIndexEnd - A10C_programD_interval_getIndexStart);
            try { numericUpDown_A10C_programD_interval.Value = Decimal.Parse(A10C_programD_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programD_interval.Value = numericUpDown_A10C_programD_interval.Minimum; }

            int A10C_programD_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['D'][\"cycle\"] =") + 24;
            int A10C_programD_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programD_cycle_getIndexStart);
            string A10C_programD_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programD_cycle_getIndexStart, A10C_programD_cycle_getIndexEnd - A10C_programD_cycle_getIndexStart);
            try { numericUpDown_A10C_programD_cycle.Value = int.Parse(A10C_programD_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programD_cycle.Value = numericUpDown_A10C_programD_cycle.Minimum; }



            int A10C_programE_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['E'][\"chaff\"] =") + 24;
            int A10C_programE_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programE_chaff_getIndexStart);
            string A10C_programE_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programE_chaff_getIndexStart, A10C_programE_chaff_getIndexEnd - A10C_programE_chaff_getIndexStart);
            try { numericUpDown_A10C_programE_chaff.Value = int.Parse(A10C_programE_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programE_chaff.Value = numericUpDown_A10C_programE_chaff.Minimum; }

            int A10C_programE_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['E'][\"flare\"] =") + 24;
            int A10C_programE_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programE_flare_getIndexStart);
            string A10C_programE_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programE_flare_getIndexStart, A10C_programE_flare_getIndexEnd - A10C_programE_flare_getIndexStart);
            try { numericUpDown_A10C_programE_flare.Value = int.Parse(A10C_programE_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programE_flare.Value = numericUpDown_A10C_programE_flare.Minimum; }

            int A10C_programE_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['E'][\"intv\"]  =") + 24;
            int A10C_programE_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programE_interval_getIndexStart);
            string A10C_programE_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programE_interval_getIndexStart, A10C_programE_interval_getIndexEnd - A10C_programE_interval_getIndexStart);
            try { numericUpDown_A10C_programE_interval.Value = Decimal.Parse(A10C_programE_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programE_interval.Value = numericUpDown_A10C_programE_interval.Minimum; }

            int A10C_programE_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['E'][\"cycle\"] =") + 24;
            int A10C_programE_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programE_cycle_getIndexStart);
            string A10C_programE_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programE_cycle_getIndexStart, A10C_programE_cycle_getIndexEnd - A10C_programE_cycle_getIndexStart);
            try { numericUpDown_A10C_programE_cycle.Value = int.Parse(A10C_programE_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programE_cycle.Value = numericUpDown_A10C_programE_cycle.Minimum; }



            int A10C_programF_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['F'][\"chaff\"] =") + 24;
            int A10C_programF_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programF_chaff_getIndexStart);
            string A10C_programF_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programF_chaff_getIndexStart, A10C_programF_chaff_getIndexEnd - A10C_programF_chaff_getIndexStart);
            try { numericUpDown_A10C_programF_chaff.Value = int.Parse(A10C_programF_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programF_chaff.Value = numericUpDown_A10C_programF_chaff.Minimum; }

            int A10C_programF_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['F'][\"flare\"] =") + 24;
            int A10C_programF_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programF_flare_getIndexStart);
            string A10C_programF_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programF_flare_getIndexStart, A10C_programF_flare_getIndexEnd - A10C_programF_flare_getIndexStart);
            try { numericUpDown_A10C_programF_flare.Value = int.Parse(A10C_programF_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programF_flare.Value = numericUpDown_A10C_programF_flare.Minimum; }

            int A10C_programF_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['F'][\"intv\"]  =") + 24;
            int A10C_programF_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programF_interval_getIndexStart);
            string A10C_programF_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programF_interval_getIndexStart, A10C_programF_interval_getIndexEnd - A10C_programF_interval_getIndexStart);
            try { numericUpDown_A10C_programF_interval.Value = Decimal.Parse(A10C_programF_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programF_interval.Value = numericUpDown_A10C_programF_interval.Minimum; }

            int A10C_programF_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['F'][\"cycle\"] =") + 24;
            int A10C_programF_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programF_cycle_getIndexStart);
            string A10C_programF_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programF_cycle_getIndexStart, A10C_programF_cycle_getIndexEnd - A10C_programF_cycle_getIndexStart);
            try { numericUpDown_A10C_programF_cycle.Value = int.Parse(A10C_programF_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programF_cycle.Value = numericUpDown_A10C_programF_cycle.Minimum; }



            int A10C_programG_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['G'][\"chaff\"] =") + 24;
            int A10C_programG_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programG_chaff_getIndexStart);
            string A10C_programG_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programG_chaff_getIndexStart, A10C_programG_chaff_getIndexEnd - A10C_programG_chaff_getIndexStart);
            try { numericUpDown_A10C_programG_chaff.Value = int.Parse(A10C_programG_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programG_chaff.Value = numericUpDown_A10C_programG_chaff.Minimum; }

            int A10C_programG_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['G'][\"flare\"] =") + 24;
            int A10C_programG_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programG_flare_getIndexStart);
            string A10C_programG_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programG_flare_getIndexStart, A10C_programG_flare_getIndexEnd - A10C_programG_flare_getIndexStart);
            try { numericUpDown_A10C_programG_flare.Value = int.Parse(A10C_programG_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programG_flare.Value = numericUpDown_A10C_programG_flare.Minimum; }

            int A10C_programG_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['G'][\"intv\"]  =") + 24;
            int A10C_programG_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programG_interval_getIndexStart);
            string A10C_programG_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programG_interval_getIndexStart, A10C_programG_interval_getIndexEnd - A10C_programG_interval_getIndexStart);
            try { numericUpDown_A10C_programG_interval.Value = Decimal.Parse(A10C_programG_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programG_interval.Value = numericUpDown_A10C_programG_interval.Minimum; }

            int A10C_programG_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['G'][\"cycle\"] =") + 24;
            int A10C_programG_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programG_cycle_getIndexStart);
            string A10C_programG_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programG_cycle_getIndexStart, A10C_programG_cycle_getIndexEnd - A10C_programG_cycle_getIndexStart);
            try { numericUpDown_A10C_programG_cycle.Value = int.Parse(A10C_programG_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programG_cycle.Value = numericUpDown_A10C_programG_cycle.Minimum; }



            int A10C_programH_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['H'][\"chaff\"] =") + 24;
            int A10C_programH_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programH_chaff_getIndexStart);
            string A10C_programH_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programH_chaff_getIndexStart, A10C_programH_chaff_getIndexEnd - A10C_programH_chaff_getIndexStart);
            try { numericUpDown_A10C_programH_chaff.Value = int.Parse(A10C_programH_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programH_chaff.Value = numericUpDown_A10C_programH_chaff.Minimum; }

            int A10C_programH_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['H'][\"flare\"] =") + 24;
            int A10C_programH_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programH_flare_getIndexStart);
            string A10C_programH_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programH_flare_getIndexStart, A10C_programH_flare_getIndexEnd - A10C_programH_flare_getIndexStart);
            try { numericUpDown_A10C_programH_flare.Value = int.Parse(A10C_programH_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programH_flare.Value = numericUpDown_A10C_programH_flare.Minimum; }

            int A10C_programH_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['H'][\"intv\"]  =") + 24;
            int A10C_programH_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programH_interval_getIndexStart);
            string A10C_programH_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programH_interval_getIndexStart, A10C_programH_interval_getIndexEnd - A10C_programH_interval_getIndexStart);
            try { numericUpDown_A10C_programH_interval.Value = Decimal.Parse(A10C_programH_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programH_interval.Value = numericUpDown_A10C_programH_interval.Minimum; }

            int A10C_programH_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['H'][\"cycle\"] =") + 24;
            int A10C_programH_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programH_cycle_getIndexStart);
            string A10C_programH_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programH_cycle_getIndexStart, A10C_programH_cycle_getIndexEnd - A10C_programH_cycle_getIndexStart);
            try { numericUpDown_A10C_programH_cycle.Value = int.Parse(A10C_programH_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programH_cycle.Value = numericUpDown_A10C_programH_cycle.Minimum; }



            int A10C_programI_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['I'][\"chaff\"] =") + 24;
            int A10C_programI_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programI_chaff_getIndexStart);
            string A10C_programI_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programI_chaff_getIndexStart, A10C_programI_chaff_getIndexEnd - A10C_programI_chaff_getIndexStart);
            try { numericUpDown_A10C_programI_chaff.Value = int.Parse(A10C_programI_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programI_chaff.Value = numericUpDown_A10C_programI_chaff.Minimum; }

            int A10C_programI_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['I'][\"flare\"] =") + 24;
            int A10C_programI_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programI_flare_getIndexStart);
            string A10C_programI_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programI_flare_getIndexStart, A10C_programI_flare_getIndexEnd - A10C_programI_flare_getIndexStart);
            try { numericUpDown_A10C_programI_flare.Value = int.Parse(A10C_programI_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programI_flare.Value = numericUpDown_A10C_programI_flare.Minimum; }

            int A10C_programI_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['I'][\"intv\"]  =") + 24;
            int A10C_programI_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programI_interval_getIndexStart);
            string A10C_programI_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programI_interval_getIndexStart, A10C_programI_interval_getIndexEnd - A10C_programI_interval_getIndexStart);
            try { numericUpDown_A10C_programI_interval.Value = Decimal.Parse(A10C_programI_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programI_interval.Value = numericUpDown_A10C_programI_interval.Minimum; }

            int A10C_programI_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['I'][\"cycle\"] =") + 24;
            int A10C_programI_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programI_cycle_getIndexStart);
            string A10C_programI_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programI_cycle_getIndexStart, A10C_programI_cycle_getIndexEnd - A10C_programI_cycle_getIndexStart);
            try { numericUpDown_A10C_programI_cycle.Value = int.Parse(A10C_programI_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programI_cycle.Value = numericUpDown_A10C_programI_cycle.Minimum; }



            int A10C_programJ_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['J'][\"chaff\"] =") + 24;
            int A10C_programJ_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programJ_chaff_getIndexStart);
            string A10C_programJ_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programJ_chaff_getIndexStart, A10C_programJ_chaff_getIndexEnd - A10C_programJ_chaff_getIndexStart);
            try { numericUpDown_A10C_programJ_chaff.Value = int.Parse(A10C_programJ_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programJ_chaff.Value = numericUpDown_A10C_programJ_chaff.Minimum; }

            int A10C_programJ_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['J'][\"flare\"] =") + 24;
            int A10C_programJ_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programJ_flare_getIndexStart);
            string A10C_programJ_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programJ_flare_getIndexStart, A10C_programJ_flare_getIndexEnd - A10C_programJ_flare_getIndexStart);
            try { numericUpDown_A10C_programJ_flare.Value = int.Parse(A10C_programJ_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programJ_flare.Value = numericUpDown_A10C_programJ_flare.Minimum; }

            int A10C_programJ_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['J'][\"intv\"]  =") + 24;
            int A10C_programJ_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programJ_interval_getIndexStart);
            string A10C_programJ_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programJ_interval_getIndexStart, A10C_programJ_interval_getIndexEnd - A10C_programJ_interval_getIndexStart);
            try { numericUpDown_A10C_programJ_interval.Value = Decimal.Parse(A10C_programJ_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programJ_interval.Value = numericUpDown_A10C_programJ_interval.Minimum; }

            int A10C_programJ_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['J'][\"cycle\"] =") + 24;
            int A10C_programJ_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programJ_cycle_getIndexStart);
            string A10C_programJ_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programJ_cycle_getIndexStart, A10C_programJ_cycle_getIndexEnd - A10C_programJ_cycle_getIndexStart);
            try { numericUpDown_A10C_programJ_cycle.Value = int.Parse(A10C_programJ_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programJ_cycle.Value = numericUpDown_A10C_programJ_cycle.Minimum; }



            int A10C_programK_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['K'][\"chaff\"] =") + 24;
            int A10C_programK_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programK_chaff_getIndexStart);
            string A10C_programK_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programK_chaff_getIndexStart, A10C_programK_chaff_getIndexEnd - A10C_programK_chaff_getIndexStart);
            try { numericUpDown_A10C_programK_chaff.Value = int.Parse(A10C_programK_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programK_chaff.Value = numericUpDown_A10C_programK_chaff.Minimum; }

            int A10C_programK_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['K'][\"flare\"] =") + 24;
            int A10C_programK_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programK_flare_getIndexStart);
            string A10C_programK_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programK_flare_getIndexStart, A10C_programK_flare_getIndexEnd - A10C_programK_flare_getIndexStart);
            try { numericUpDown_A10C_programK_flare.Value = int.Parse(A10C_programK_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programK_flare.Value = numericUpDown_A10C_programK_flare.Minimum; }

            int A10C_programK_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['K'][\"intv\"]  =") + 24;
            int A10C_programK_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programK_interval_getIndexStart);
            string A10C_programK_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programK_interval_getIndexStart, A10C_programK_interval_getIndexEnd - A10C_programK_interval_getIndexStart);
            try { numericUpDown_A10C_programK_interval.Value = Decimal.Parse(A10C_programK_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programK_interval.Value = numericUpDown_A10C_programK_interval.Minimum; }

            int A10C_programK_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['K'][\"cycle\"] =") + 24;
            int A10C_programK_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programK_cycle_getIndexStart);
            string A10C_programK_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programK_cycle_getIndexStart, A10C_programK_cycle_getIndexEnd - A10C_programK_cycle_getIndexStart);
            try { numericUpDown_A10C_programK_cycle.Value = int.Parse(A10C_programK_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programK_cycle.Value = numericUpDown_A10C_programK_cycle.Minimum; }



            int A10C_programL_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['L'][\"chaff\"] =") + 24;
            int A10C_programL_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programL_chaff_getIndexStart);
            string A10C_programL_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programL_chaff_getIndexStart, A10C_programL_chaff_getIndexEnd - A10C_programL_chaff_getIndexStart);
            try { numericUpDown_A10C_programL_chaff.Value = int.Parse(A10C_programL_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programL_chaff.Value = numericUpDown_A10C_programL_chaff.Minimum; }

            int A10C_programL_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['L'][\"flare\"] =") + 24;
            int A10C_programL_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programL_flare_getIndexStart);
            string A10C_programL_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programL_flare_getIndexStart, A10C_programL_flare_getIndexEnd - A10C_programL_flare_getIndexStart);
            try { numericUpDown_A10C_programL_flare.Value = int.Parse(A10C_programL_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programL_flare.Value = numericUpDown_A10C_programL_flare.Minimum; }

            int A10C_programL_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['L'][\"intv\"]  =") + 24;
            int A10C_programL_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programL_interval_getIndexStart);
            string A10C_programL_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programL_interval_getIndexStart, A10C_programL_interval_getIndexEnd - A10C_programL_interval_getIndexStart);
            try { numericUpDown_A10C_programL_interval.Value = Decimal.Parse(A10C_programL_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programL_interval.Value = numericUpDown_A10C_programL_interval.Minimum; }

            int A10C_programL_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['L'][\"cycle\"] =") + 24;
            int A10C_programL_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programL_cycle_getIndexStart);
            string A10C_programL_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programL_cycle_getIndexStart, A10C_programL_cycle_getIndexEnd - A10C_programL_cycle_getIndexStart);
            try { numericUpDown_A10C_programL_cycle.Value = int.Parse(A10C_programL_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programL_cycle.Value = numericUpDown_A10C_programL_cycle.Minimum; }



            int A10C_programM_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['M'][\"chaff\"] =") + 24;
            int A10C_programM_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programM_chaff_getIndexStart);
            string A10C_programM_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programM_chaff_getIndexStart, A10C_programM_chaff_getIndexEnd - A10C_programM_chaff_getIndexStart);
            try { numericUpDown_A10C_programM_chaff.Value = int.Parse(A10C_programM_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programM_chaff.Value = numericUpDown_A10C_programM_chaff.Minimum; }

            int A10C_programM_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['M'][\"flare\"] =") + 24;
            int A10C_programM_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programM_flare_getIndexStart);
            string A10C_programM_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programM_flare_getIndexStart, A10C_programM_flare_getIndexEnd - A10C_programM_flare_getIndexStart);
            try { numericUpDown_A10C_programM_flare.Value = int.Parse(A10C_programM_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programM_flare.Value = numericUpDown_A10C_programM_flare.Minimum; }

            int A10C_programM_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['M'][\"intv\"]  =") + 24;
            int A10C_programM_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programM_interval_getIndexStart);
            string A10C_programM_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programM_interval_getIndexStart, A10C_programM_interval_getIndexEnd - A10C_programM_interval_getIndexStart);
            try { numericUpDown_A10C_programM_interval.Value = Decimal.Parse(A10C_programM_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programM_interval.Value = numericUpDown_A10C_programM_interval.Minimum; }

            int A10C_programM_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['M'][\"cycle\"] =") + 24;
            int A10C_programM_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programM_cycle_getIndexStart);
            string A10C_programM_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programM_cycle_getIndexStart, A10C_programM_cycle_getIndexEnd - A10C_programM_cycle_getIndexStart);
            try { numericUpDown_A10C_programM_cycle.Value = int.Parse(A10C_programM_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programM_cycle.Value = numericUpDown_A10C_programM_cycle.Minimum; }


            if (CountermeasureFileStringText_A10C.Contains("'N'"))//i fthe file does not contain the Program N, then it will skip the import, avoiding an error
            {
                //int A10C_programN_name_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['N'] = {}");
                //int A10C_programN_name_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programN_name_getIndexStart);
                //string A10C_programN_name_amount = CountermeasureFileStringText_A10C.Substring(A10C_programN_name_getIndexStart, A10C_programN_name_getIndexEnd - A10C_programN_name_getIndexStart);
                //try { textBox_programN.Text = A10C_programN_name_amount; }
                //catch (ArgumentOutOfRangeException) { textBox_programN.Text = "Custom Program N"; }


                int A10C_programN_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['N'][\"chaff\"] =") + 24;
                int A10C_programN_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programN_chaff_getIndexStart);
                string A10C_programN_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programN_chaff_getIndexStart, A10C_programN_chaff_getIndexEnd - A10C_programN_chaff_getIndexStart);
                try { numericUpDown_A10C_programN_chaff.Value = int.Parse(A10C_programN_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programN_chaff.Value = numericUpDown_A10C_programN_chaff.Minimum; }


                int A10C_programN_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['N'][\"flare\"] =") + 24;
                int A10C_programN_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programN_flare_getIndexStart);
                string A10C_programN_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programN_flare_getIndexStart, A10C_programN_flare_getIndexEnd - A10C_programN_flare_getIndexStart);
                try { numericUpDown_A10C_programN_flare.Value = int.Parse(A10C_programN_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programN_flare.Value = numericUpDown_A10C_programN_flare.Minimum; }

                int A10C_programN_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['N'][\"intv\"]  =") + 24;
                int A10C_programN_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programN_interval_getIndexStart);
                string A10C_programN_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programN_interval_getIndexStart, A10C_programN_interval_getIndexEnd - A10C_programN_interval_getIndexStart);
                try { numericUpDown_A10C_programN_interval.Value = Decimal.Parse(A10C_programN_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programN_interval.Value = numericUpDown_A10C_programN_interval.Minimum; }

                int A10C_programN_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['N'][\"cycle\"] =") + 24;
                int A10C_programN_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programN_cycle_getIndexStart);
                string A10C_programN_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programN_cycle_getIndexStart, A10C_programN_cycle_getIndexEnd - A10C_programN_cycle_getIndexStart);
                try { numericUpDown_A10C_programN_cycle.Value = int.Parse(A10C_programN_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programN_cycle.Value = numericUpDown_A10C_programN_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'O'"))
            {
                int A10C_programO_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['O'][\"chaff\"] =") + 24;
                int A10C_programO_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programO_chaff_getIndexStart);
                string A10C_programO_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programO_chaff_getIndexStart, A10C_programO_chaff_getIndexEnd - A10C_programO_chaff_getIndexStart);
                try { numericUpDown_A10C_programO_chaff.Value = int.Parse(A10C_programO_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programO_chaff.Value = numericUpDown_A10C_programO_chaff.Minimum; }

                int A10C_programO_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['O'][\"flare\"] =") + 24;
                int A10C_programO_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programO_flare_getIndexStart);
                string A10C_programO_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programO_flare_getIndexStart, A10C_programO_flare_getIndexEnd - A10C_programO_flare_getIndexStart);
                try { numericUpDown_A10C_programO_flare.Value = int.Parse(A10C_programO_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programO_flare.Value = numericUpDown_A10C_programO_flare.Minimum; }

                int A10C_programO_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['O'][\"intv\"]  =") + 24;
                int A10C_programO_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programO_interval_getIndexStart);
                string A10C_programO_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programO_interval_getIndexStart, A10C_programO_interval_getIndexEnd - A10C_programO_interval_getIndexStart);
                try { numericUpDown_A10C_programO_interval.Value = Decimal.Parse(A10C_programO_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programO_interval.Value = numericUpDown_A10C_programO_interval.Minimum; }

                int A10C_programO_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['O'][\"cycle\"] =") + 24;
                int A10C_programO_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programO_cycle_getIndexStart);
                string A10C_programO_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programO_cycle_getIndexStart, A10C_programO_cycle_getIndexEnd - A10C_programO_cycle_getIndexStart);
                try { numericUpDown_A10C_programO_cycle.Value = int.Parse(A10C_programO_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programO_cycle.Value = numericUpDown_A10C_programO_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'P'"))
            {
                int A10C_programP_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['P'][\"chaff\"] =") + 24;
                int A10C_programP_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programP_chaff_getIndexStart);
                string A10C_programP_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programP_chaff_getIndexStart, A10C_programP_chaff_getIndexEnd - A10C_programP_chaff_getIndexStart);
                try { numericUpDown_A10C_programP_chaff.Value = int.Parse(A10C_programP_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programP_chaff.Value = numericUpDown_A10C_programP_chaff.Minimum; }

                int A10C_programP_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['P'][\"flare\"] =") + 24;
                int A10C_programP_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programP_flare_getIndexStart);
                string A10C_programP_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programP_flare_getIndexStart, A10C_programP_flare_getIndexEnd - A10C_programP_flare_getIndexStart);
                try { numericUpDown_A10C_programP_flare.Value = int.Parse(A10C_programP_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programP_flare.Value = numericUpDown_A10C_programP_flare.Minimum; }

                int A10C_programP_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['P'][\"intv\"]  =") + 24;
                int A10C_programP_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programP_interval_getIndexStart);
                string A10C_programP_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programP_interval_getIndexStart, A10C_programP_interval_getIndexEnd - A10C_programP_interval_getIndexStart);
                try { numericUpDown_A10C_programP_interval.Value = Decimal.Parse(A10C_programP_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programP_interval.Value = numericUpDown_A10C_programP_interval.Minimum; }

                int A10C_programP_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['P'][\"cycle\"] =") + 24;
                int A10C_programP_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programP_cycle_getIndexStart);
                string A10C_programP_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programP_cycle_getIndexStart, A10C_programP_cycle_getIndexEnd - A10C_programP_cycle_getIndexStart);
                try { numericUpDown_A10C_programP_cycle.Value = int.Parse(A10C_programP_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programP_cycle.Value = numericUpDown_A10C_programP_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'Q'"))
            {
                int A10C_programQ_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Q'][\"chaff\"] =") + 24;
                int A10C_programQ_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programQ_chaff_getIndexStart);
                string A10C_programQ_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programQ_chaff_getIndexStart, A10C_programQ_chaff_getIndexEnd - A10C_programQ_chaff_getIndexStart);
                try { numericUpDown_A10C_programQ_chaff.Value = int.Parse(A10C_programQ_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programQ_chaff.Value = numericUpDown_A10C_programQ_chaff.Minimum; }

                int A10C_programQ_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Q'][\"flare\"] =") + 24;
                int A10C_programQ_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programQ_flare_getIndexStart);
                string A10C_programQ_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programQ_flare_getIndexStart, A10C_programQ_flare_getIndexEnd - A10C_programQ_flare_getIndexStart);
                try { numericUpDown_A10C_programQ_flare.Value = int.Parse(A10C_programQ_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programQ_flare.Value = numericUpDown_A10C_programQ_flare.Minimum; }

                int A10C_programQ_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Q'][\"intv\"]  =") + 24;
                int A10C_programQ_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programQ_interval_getIndexStart);
                string A10C_programQ_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programQ_interval_getIndexStart, A10C_programQ_interval_getIndexEnd - A10C_programQ_interval_getIndexStart);
                try { numericUpDown_A10C_programQ_interval.Value = Decimal.Parse(A10C_programQ_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programQ_interval.Value = numericUpDown_A10C_programQ_interval.Minimum; }

                int A10C_programQ_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Q'][\"cycle\"] =") + 24;
                int A10C_programQ_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programQ_cycle_getIndexStart);
                string A10C_programQ_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programQ_cycle_getIndexStart, A10C_programQ_cycle_getIndexEnd - A10C_programQ_cycle_getIndexStart);
                try { numericUpDown_A10C_programQ_cycle.Value = int.Parse(A10C_programQ_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programQ_cycle.Value = numericUpDown_A10C_programQ_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'R'"))
            {
                int A10C_programR_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['R'][\"chaff\"] =") + 24;
                int A10C_programR_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programR_chaff_getIndexStart);
                string A10C_programR_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programR_chaff_getIndexStart, A10C_programR_chaff_getIndexEnd - A10C_programR_chaff_getIndexStart);
                try { numericUpDown_A10C_programR_chaff.Value = int.Parse(A10C_programR_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programR_chaff.Value = numericUpDown_A10C_programR_chaff.Minimum; }

                int A10C_programR_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['R'][\"flare\"] =") + 24;
                int A10C_programR_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programR_flare_getIndexStart);
                string A10C_programR_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programR_flare_getIndexStart, A10C_programR_flare_getIndexEnd - A10C_programR_flare_getIndexStart);
                try { numericUpDown_A10C_programR_flare.Value = int.Parse(A10C_programR_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programR_flare.Value = numericUpDown_A10C_programR_flare.Minimum; }

                int A10C_programR_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['R'][\"intv\"]  =") + 24;
                int A10C_programR_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programR_interval_getIndexStart);
                string A10C_programR_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programR_interval_getIndexStart, A10C_programR_interval_getIndexEnd - A10C_programR_interval_getIndexStart);
                try { numericUpDown_A10C_programR_interval.Value = Decimal.Parse(A10C_programR_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programR_interval.Value = numericUpDown_A10C_programR_interval.Minimum; }

                int A10C_programR_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['R'][\"cycle\"] =") + 24;
                int A10C_programR_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programR_cycle_getIndexStart);
                string A10C_programR_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programR_cycle_getIndexStart, A10C_programR_cycle_getIndexEnd - A10C_programR_cycle_getIndexStart);
                try { numericUpDown_A10C_programR_cycle.Value = int.Parse(A10C_programR_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programR_cycle.Value = numericUpDown_A10C_programR_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'S'"))
            {
                int A10C_programS_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['S'][\"chaff\"] =") + 24;
                int A10C_programS_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programS_chaff_getIndexStart);
                string A10C_programS_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programS_chaff_getIndexStart, A10C_programS_chaff_getIndexEnd - A10C_programS_chaff_getIndexStart);
                try { numericUpDown_A10C_programS_chaff.Value = int.Parse(A10C_programS_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programS_chaff.Value = numericUpDown_A10C_programS_chaff.Minimum; }

                int A10C_programS_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['S'][\"flare\"] =") + 24;
                int A10C_programS_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programS_flare_getIndexStart);
                string A10C_programS_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programS_flare_getIndexStart, A10C_programS_flare_getIndexEnd - A10C_programS_flare_getIndexStart);
                try { numericUpDown_A10C_programS_flare.Value = int.Parse(A10C_programS_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programS_flare.Value = numericUpDown_A10C_programS_flare.Minimum; }

                int A10C_programS_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['S'][\"intv\"]  =") + 24;
                int A10C_programS_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programS_interval_getIndexStart);
                string A10C_programS_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programS_interval_getIndexStart, A10C_programS_interval_getIndexEnd - A10C_programS_interval_getIndexStart);
                try { numericUpDown_A10C_programS_interval.Value = Decimal.Parse(A10C_programS_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programS_interval.Value = numericUpDown_A10C_programS_interval.Minimum; }

                int A10C_programS_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['S'][\"cycle\"] =") + 24;
                int A10C_programS_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programS_cycle_getIndexStart);
                string A10C_programS_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programS_cycle_getIndexStart, A10C_programS_cycle_getIndexEnd - A10C_programS_cycle_getIndexStart);
                try { numericUpDown_A10C_programS_cycle.Value = int.Parse(A10C_programS_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programS_cycle.Value = numericUpDown_A10C_programS_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'T'"))
            {
                int A10C_programT_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['T'][\"chaff\"] =") + 24;
                int A10C_programT_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programT_chaff_getIndexStart);
                string A10C_programT_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programT_chaff_getIndexStart, A10C_programT_chaff_getIndexEnd - A10C_programT_chaff_getIndexStart);
                try { numericUpDown_A10C_programT_chaff.Value = int.Parse(A10C_programT_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programT_chaff.Value = numericUpDown_A10C_programT_chaff.Minimum; }

                int A10C_programT_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['T'][\"flare\"] =") + 24;
                int A10C_programT_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programT_flare_getIndexStart);
                string A10C_programT_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programT_flare_getIndexStart, A10C_programT_flare_getIndexEnd - A10C_programT_flare_getIndexStart);
                try { numericUpDown_A10C_programT_flare.Value = int.Parse(A10C_programT_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programT_flare.Value = numericUpDown_A10C_programT_flare.Minimum; }

                int A10C_programT_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['T'][\"intv\"]  =") + 24;
                int A10C_programT_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programT_interval_getIndexStart);
                string A10C_programT_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programT_interval_getIndexStart, A10C_programT_interval_getIndexEnd - A10C_programT_interval_getIndexStart);
                try { numericUpDown_A10C_programT_interval.Value = Decimal.Parse(A10C_programT_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programT_interval.Value = numericUpDown_A10C_programT_interval.Minimum; }

                int A10C_programT_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['T'][\"cycle\"] =") + 24;
                int A10C_programT_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programT_cycle_getIndexStart);
                string A10C_programT_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programT_cycle_getIndexStart, A10C_programT_cycle_getIndexEnd - A10C_programT_cycle_getIndexStart);
                try { numericUpDown_A10C_programT_cycle.Value = int.Parse(A10C_programT_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programT_cycle.Value = numericUpDown_A10C_programT_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'U'"))
            {
                int A10C_programU_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['U'][\"chaff\"] =") + 24;
                int A10C_programU_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programU_chaff_getIndexStart);
                string A10C_programU_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programU_chaff_getIndexStart, A10C_programU_chaff_getIndexEnd - A10C_programU_chaff_getIndexStart);
                try { numericUpDown_A10C_programU_chaff.Value = int.Parse(A10C_programU_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programU_chaff.Value = numericUpDown_A10C_programU_chaff.Minimum; }

                int A10C_programU_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['U'][\"flare\"] =") + 24;
                int A10C_programU_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programU_flare_getIndexStart);
                string A10C_programU_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programU_flare_getIndexStart, A10C_programU_flare_getIndexEnd - A10C_programU_flare_getIndexStart);
                try { numericUpDown_A10C_programU_flare.Value = int.Parse(A10C_programU_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programU_flare.Value = numericUpDown_A10C_programU_flare.Minimum; }

                int A10C_programU_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['U'][\"intv\"]  =") + 24;
                int A10C_programU_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programU_interval_getIndexStart);
                string A10C_programU_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programU_interval_getIndexStart, A10C_programU_interval_getIndexEnd - A10C_programU_interval_getIndexStart);
                try { numericUpDown_A10C_programU_interval.Value = Decimal.Parse(A10C_programU_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programU_interval.Value = numericUpDown_A10C_programU_interval.Minimum; }

                int A10C_programU_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['U'][\"cycle\"] =") + 24;
                int A10C_programU_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programU_cycle_getIndexStart);
                string A10C_programU_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programU_cycle_getIndexStart, A10C_programU_cycle_getIndexEnd - A10C_programU_cycle_getIndexStart);
                try { numericUpDown_A10C_programU_cycle.Value = int.Parse(A10C_programU_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programU_cycle.Value = numericUpDown_A10C_programU_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'V'"))
            {
                int A10C_programV_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['V'][\"chaff\"] =") + 24;
                int A10C_programV_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programV_chaff_getIndexStart);
                string A10C_programV_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programV_chaff_getIndexStart, A10C_programV_chaff_getIndexEnd - A10C_programV_chaff_getIndexStart);
                try { numericUpDown_A10C_programV_chaff.Value = int.Parse(A10C_programV_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programV_chaff.Value = numericUpDown_A10C_programV_chaff.Minimum; }

                int A10C_programV_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['V'][\"flare\"] =") + 24;
                int A10C_programV_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programV_flare_getIndexStart);
                string A10C_programV_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programV_flare_getIndexStart, A10C_programV_flare_getIndexEnd - A10C_programV_flare_getIndexStart);
                try { numericUpDown_A10C_programV_flare.Value = int.Parse(A10C_programV_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programV_flare.Value = numericUpDown_A10C_programV_flare.Minimum; }

                int A10C_programV_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['V'][\"intv\"]  =") + 24;
                int A10C_programV_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programV_interval_getIndexStart);
                string A10C_programV_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programV_interval_getIndexStart, A10C_programV_interval_getIndexEnd - A10C_programV_interval_getIndexStart);
                try { numericUpDown_A10C_programV_interval.Value = Decimal.Parse(A10C_programV_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programV_interval.Value = numericUpDown_A10C_programV_interval.Minimum; }

                int A10C_programV_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['V'][\"cycle\"] =") + 24;
                int A10C_programV_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programV_cycle_getIndexStart);
                string A10C_programV_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programV_cycle_getIndexStart, A10C_programV_cycle_getIndexEnd - A10C_programV_cycle_getIndexStart);
                try { numericUpDown_A10C_programV_cycle.Value = int.Parse(A10C_programV_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programV_cycle.Value = numericUpDown_A10C_programV_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'W'"))
            {
                int A10C_programW_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['W'][\"chaff\"] =") + 24;
                int A10C_programW_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programW_chaff_getIndexStart);
                string A10C_programW_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programW_chaff_getIndexStart, A10C_programW_chaff_getIndexEnd - A10C_programW_chaff_getIndexStart);
                try { numericUpDown_A10C_programW_chaff.Value = int.Parse(A10C_programW_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programW_chaff.Value = numericUpDown_A10C_programW_chaff.Minimum; }

                int A10C_programW_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['W'][\"flare\"] =") + 24;
                int A10C_programW_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programW_flare_getIndexStart);
                string A10C_programW_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programW_flare_getIndexStart, A10C_programW_flare_getIndexEnd - A10C_programW_flare_getIndexStart);
                try { numericUpDown_A10C_programW_flare.Value = int.Parse(A10C_programW_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programW_flare.Value = numericUpDown_A10C_programW_flare.Minimum; }

                int A10C_programW_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['W'][\"intv\"]  =") + 24;
                int A10C_programW_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programW_interval_getIndexStart);
                string A10C_programW_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programW_interval_getIndexStart, A10C_programW_interval_getIndexEnd - A10C_programW_interval_getIndexStart);
                try { numericUpDown_A10C_programW_interval.Value = Decimal.Parse(A10C_programW_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programW_interval.Value = numericUpDown_A10C_programW_interval.Minimum; }

                int A10C_programW_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['W'][\"cycle\"] =") + 24;
                int A10C_programW_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programW_cycle_getIndexStart);
                string A10C_programW_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programW_cycle_getIndexStart, A10C_programW_cycle_getIndexEnd - A10C_programW_cycle_getIndexStart);
                try { numericUpDown_A10C_programW_cycle.Value = int.Parse(A10C_programW_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programW_cycle.Value = numericUpDown_A10C_programW_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'X'"))
            {
                int A10C_programX_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['X'][\"chaff\"] =") + 24;
                int A10C_programX_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programX_chaff_getIndexStart);
                string A10C_programX_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programX_chaff_getIndexStart, A10C_programX_chaff_getIndexEnd - A10C_programX_chaff_getIndexStart);
                try { numericUpDown_A10C_programX_chaff.Value = int.Parse(A10C_programX_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programX_chaff.Value = numericUpDown_A10C_programX_chaff.Minimum; }

                int A10C_programX_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['X'][\"flare\"] =") + 24;
                int A10C_programX_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programX_flare_getIndexStart);
                string A10C_programX_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programX_flare_getIndexStart, A10C_programX_flare_getIndexEnd - A10C_programX_flare_getIndexStart);
                try { numericUpDown_A10C_programX_flare.Value = int.Parse(A10C_programX_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programX_flare.Value = numericUpDown_A10C_programX_flare.Minimum; }

                int A10C_programX_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['X'][\"intv\"]  =") + 24;
                int A10C_programX_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programX_interval_getIndexStart);
                string A10C_programX_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programX_interval_getIndexStart, A10C_programX_interval_getIndexEnd - A10C_programX_interval_getIndexStart);
                try { numericUpDown_A10C_programX_interval.Value = Decimal.Parse(A10C_programX_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programX_interval.Value = numericUpDown_A10C_programX_interval.Minimum; }

                int A10C_programX_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['X'][\"cycle\"] =") + 24;
                int A10C_programX_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programX_cycle_getIndexStart);
                string A10C_programX_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programX_cycle_getIndexStart, A10C_programX_cycle_getIndexEnd - A10C_programX_cycle_getIndexStart);
                try { numericUpDown_A10C_programX_cycle.Value = int.Parse(A10C_programX_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programX_cycle.Value = numericUpDown_A10C_programX_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'Y'"))
            {
                int A10C_programY_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Y'][\"chaff\"] =") + 24;
                int A10C_programY_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programY_chaff_getIndexStart);
                string A10C_programY_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programY_chaff_getIndexStart, A10C_programY_chaff_getIndexEnd - A10C_programY_chaff_getIndexStart);
                try { numericUpDown_A10C_programY_chaff.Value = int.Parse(A10C_programY_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programY_chaff.Value = numericUpDown_A10C_programY_chaff.Minimum; }

                int A10C_programY_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Y'][\"flare\"] =") + 24;
                int A10C_programY_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programY_flare_getIndexStart);
                string A10C_programY_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programY_flare_getIndexStart, A10C_programY_flare_getIndexEnd - A10C_programY_flare_getIndexStart);
                try { numericUpDown_A10C_programY_flare.Value = int.Parse(A10C_programY_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programY_flare.Value = numericUpDown_A10C_programY_flare.Minimum; }

                int A10C_programY_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Y'][\"intv\"]  =") + 24;
                int A10C_programY_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programY_interval_getIndexStart);
                string A10C_programY_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programY_interval_getIndexStart, A10C_programY_interval_getIndexEnd - A10C_programY_interval_getIndexStart);
                try { numericUpDown_A10C_programY_interval.Value = Decimal.Parse(A10C_programY_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programY_interval.Value = numericUpDown_A10C_programY_interval.Minimum; }

                int A10C_programY_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Y'][\"cycle\"] =") + 24;
                int A10C_programY_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programY_cycle_getIndexStart);
                string A10C_programY_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programY_cycle_getIndexStart, A10C_programY_cycle_getIndexEnd - A10C_programY_cycle_getIndexStart);
                try { numericUpDown_A10C_programY_cycle.Value = int.Parse(A10C_programY_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programY_cycle.Value = numericUpDown_A10C_programY_cycle.Minimum; }
            }

            if (CountermeasureFileStringText_A10C.Contains("'Z'"))
            {
                int A10C_programZ_chaff_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Z'][\"chaff\"] =") + 24;
                int A10C_programZ_chaff_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programZ_chaff_getIndexStart);
                string A10C_programZ_chaff_amount = CountermeasureFileStringText_A10C.Substring(A10C_programZ_chaff_getIndexStart, A10C_programZ_chaff_getIndexEnd - A10C_programZ_chaff_getIndexStart);
                try { numericUpDown_A10C_programZ_chaff.Value = int.Parse(A10C_programZ_chaff_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programZ_chaff.Value = numericUpDown_A10C_programZ_chaff.Minimum; }

                int A10C_programZ_flare_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Z'][\"flare\"] =") + 24;
                int A10C_programZ_flare_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programZ_flare_getIndexStart);
                string A10C_programZ_flare_amount = CountermeasureFileStringText_A10C.Substring(A10C_programZ_flare_getIndexStart, A10C_programZ_flare_getIndexEnd - A10C_programZ_flare_getIndexStart);
                try { numericUpDown_A10C_programZ_flare.Value = int.Parse(A10C_programZ_flare_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programZ_flare.Value = numericUpDown_A10C_programZ_flare.Minimum; }

                int A10C_programZ_interval_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Z'][\"intv\"]  =") + 24;
                int A10C_programZ_interval_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programZ_interval_getIndexStart);
                string A10C_programZ_interval_amount = CountermeasureFileStringText_A10C.Substring(A10C_programZ_interval_getIndexStart, A10C_programZ_interval_getIndexEnd - A10C_programZ_interval_getIndexStart);
                try { numericUpDown_A10C_programZ_interval.Value = Decimal.Parse(A10C_programZ_interval_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programZ_interval.Value = numericUpDown_A10C_programZ_interval.Minimum; }

                int A10C_programZ_cycle_getIndexStart = CountermeasureFileStringText_A10C.IndexOf("programs['Z'][\"cycle\"] =") + 24;
                int A10C_programZ_cycle_getIndexEnd = CountermeasureFileStringText_A10C.IndexOf("\n", A10C_programZ_cycle_getIndexStart);
                string A10C_programZ_cycle_amount = CountermeasureFileStringText_A10C.Substring(A10C_programZ_cycle_getIndexStart, A10C_programZ_cycle_getIndexEnd - A10C_programZ_cycle_getIndexStart);
                try { numericUpDown_A10C_programZ_cycle.Value = int.Parse(A10C_programZ_cycle_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_A10C_programZ_cycle.Value = numericUpDown_A10C_programZ_cycle.Minimum; }
            }

            //MessageBox.Show("Index Start:" + A10C_programB_cycle_getIndexStart + " Index End:" + A10C_programB_cycle_getIndexEnd + " Result: |" + A10C_programB_cycle_amount + "|");

        }


        private void loadLua_AV8B_CMS()
        {
            //TODO: Code this like the others

            //find the lua file
            string CountermeasureFileString_AV8B = loadLocation;
            //load the text into a string
            string CountermeasureFileStringText_AV8B = File.ReadAllText(CountermeasureFileString_AV8B);

            //Get all of the values
            
            int AV8B_ALL_CHAFF_BQTY_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_ALL_CHAFF_BQTY =") + 19;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_ALL_CHAFF_BQTY_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_ALL_CHAFF_BQTY_getIndexStart);
            string AV8B_ALL_CHAFF_BQTY_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_ALL_CHAFF_BQTY_getIndexStart, AV8B_ALL_CHAFF_BQTY_getIndexEnd - AV8B_ALL_CHAFF_BQTY_getIndexStart);

            //MessageBox.Show("|" + AV8B_ALL_CHAFF_BQTY_amount + "|");
            
            int AV8B_ALL_CHAFF_BINT_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_ALL_CHAFF_BINT =") + 19;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_ALL_CHAFF_BINT_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_ALL_CHAFF_BINT_getIndexStart);
            string AV8B_ALL_CHAFF_BINT_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_ALL_CHAFF_BINT_getIndexStart, AV8B_ALL_CHAFF_BINT_getIndexEnd - AV8B_ALL_CHAFF_BINT_getIndexStart);

            int AV8B_ALL_CHAFF_SQTY_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_ALL_CHAFF_SQTY =") + 19;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_ALL_CHAFF_SQTY_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_ALL_CHAFF_SQTY_getIndexStart);
            string AV8B_ALL_CHAFF_SQTY_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_ALL_CHAFF_SQTY_getIndexStart, AV8B_ALL_CHAFF_SQTY_getIndexEnd - AV8B_ALL_CHAFF_SQTY_getIndexStart);


            int AV8B_ALL_CHAFF_SINT_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_ALL_CHAFF_SINT =") + 19;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_ALL_CHAFF_SINT_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_ALL_CHAFF_SINT_getIndexStart);
            string AV8B_ALL_CHAFF_SINT_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_ALL_CHAFF_SINT_getIndexStart, AV8B_ALL_CHAFF_SINT_getIndexEnd - AV8B_ALL_CHAFF_SINT_getIndexStart);


            int AV8B_ALL_FLARES_SQTY_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_ALL_FLARES_SQTY =") + 20;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_ALL_FLARES_SQTY_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_ALL_FLARES_SQTY_getIndexStart);
            string AV8B_ALL_FLARES_SQTY_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_ALL_FLARES_SQTY_getIndexStart, AV8B_ALL_FLARES_SQTY_getIndexEnd - AV8B_ALL_FLARES_SQTY_getIndexStart);


            int AV8B_ALL_FLARES_SINT_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_ALL_FLARES_SINT =") + 20;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_ALL_FLARES_SINT_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_ALL_FLARES_SINT_getIndexStart);
            string AV8B_ALL_FLARES_SINT_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_ALL_FLARES_SINT_getIndexStart, AV8B_ALL_FLARES_SINT_getIndexEnd - AV8B_ALL_FLARES_SINT_getIndexStart);


            int AV8B_CHAFF_BQTY_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_CHAFF_BQTY =") + 15;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_CHAFF_BQTY_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_CHAFF_BQTY_getIndexStart);
            string AV8B_CHAFF_BQTY_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_CHAFF_BQTY_getIndexStart, AV8B_CHAFF_BQTY_getIndexEnd - AV8B_CHAFF_BQTY_getIndexStart);


            int AV8B_CHAFF_BINT_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_CHAFF_BINT =") + 15;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_CHAFF_BINT_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_CHAFF_BINT_getIndexStart);
            string AV8B_CHAFF_BINT_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_CHAFF_BINT_getIndexStart, AV8B_CHAFF_BINT_getIndexEnd - AV8B_CHAFF_BINT_getIndexStart);


            int AV8B_CHAFF_SQTY_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_CHAFF_SQTY =") + 15;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_CHAFF_SQTY_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_CHAFF_SQTY_getIndexStart);
            string AV8B_CHAFF_SQTY_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_CHAFF_SQTY_getIndexStart, AV8B_CHAFF_SQTY_getIndexEnd - AV8B_CHAFF_SQTY_getIndexStart);


            int AV8B_CHAFF_SINT_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_CHAFF_SINT =") + 15;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_CHAFF_SINT_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_CHAFF_SINT_getIndexStart);
            string AV8B_CHAFF_SINT_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_CHAFF_SINT_getIndexStart, AV8B_CHAFF_SINT_getIndexEnd - AV8B_CHAFF_SINT_getIndexStart);


            int AV8B_FLARES_SQTY_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_FLARES_SQTY =") + 16;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_FLARES_SQTY_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_FLARES_SQTY_getIndexStart);
            string AV8B_FLARES_SQTY_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_FLARES_SQTY_getIndexStart, AV8B_FLARES_SQTY_getIndexEnd - AV8B_FLARES_SQTY_getIndexStart);

            int AV8B_FLARES_SINT_getIndexStart = CountermeasureFileStringText_AV8B.IndexOf("EW_FLARES_SINT =") + 16;//'19' because that is actually when the start of the number we want is located compared to the start of the index
            int AV8B_FLARES_SINT_getIndexEnd = CountermeasureFileStringText_AV8B.IndexOf(";", AV8B_FLARES_SINT_getIndexStart);
            string AV8B_FLARES_SINT_amount = CountermeasureFileStringText_AV8B.Substring(AV8B_FLARES_SINT_getIndexStart, AV8B_FLARES_SINT_getIndexEnd - AV8B_FLARES_SINT_getIndexStart);
            
            
            //entering the information into the winForm
            //first intry
            if (AV8B_ALL_CHAFF_BQTY_amount.Contains("-1"))//if the value in the file is -1
            {
                //numericUpDown_AV8B_ALL_CHAFF_BQTY.Value = numericUpDown_AV8B_ALL_CHAFF_BQTY.Minimum;//make the number the lowest it can go
                numericUpDown_AV8B_ALL_CHAFF_BQTY.Enabled = false;//disable the use of the numbers
                checkBox_AV8B_ALL_chaffBquantity_continuous.Checked = true;//check the box
                checkBox_AV8B_ALL_chaffBquantity_random.Checked = false;//uncheck this box
                //MessageBox.Show("Value detected as -1");
            }
            else if (AV8B_ALL_CHAFF_BQTY_amount.Contains("-2"))
            {
                //numericUpDown_AV8B_ALL_CHAFF_BQTY.Value = numericUpDown_AV8B_ALL_CHAFF_BQTY.Minimum;
                numericUpDown_AV8B_ALL_CHAFF_BQTY.Enabled = false;
                checkBox_AV8B_ALL_chaffBquantity_continuous.Checked = false;
                checkBox_AV8B_ALL_chaffBquantity_random.Checked = true;
                //MessageBox.Show("Value detected as -2");
            }
            else
            {
                checkBox_AV8B_ALL_chaffBquantity_continuous.Checked = false;
                checkBox_AV8B_ALL_chaffBquantity_random.Checked = false;
                numericUpDown_AV8B_ALL_CHAFF_BQTY.Enabled = true;
                try { numericUpDown_AV8B_ALL_CHAFF_BQTY.Value = int.Parse(AV8B_ALL_CHAFF_BQTY_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_ALL_CHAFF_BQTY.Value = numericUpDown_AV8B_ALL_CHAFF_BQTY.Minimum; }
                //MessageBox.Show("Value detected as an actual number");
            }



            //second entry
            if (AV8B_ALL_CHAFF_BINT_amount.Contains("-2"))
            {
                //numericUpDown_AV8B_ALL_CHAFF_BINT.Value = numericUpDown_AV8B_ALL_CHAFF_BINT.Minimum;
                numericUpDown_AV8B_ALL_CHAFF_BINT.Enabled = false;
                checkBox_AV8B_ALL_chaffBinterval_random.Checked = true;
            }
            else
            {
                numericUpDown_AV8B_ALL_CHAFF_BINT.Enabled = true;
                checkBox_AV8B_ALL_chaffBinterval_random.Checked = false;
                try { numericUpDown_AV8B_ALL_CHAFF_BINT.Value = Decimal.Parse(AV8B_ALL_CHAFF_BINT_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_ALL_CHAFF_BINT.Value = numericUpDown_AV8B_ALL_CHAFF_BINT.Minimum; }
            }

            try { numericUpDown_AV8B_ALL_CHAFF_SQTY.Value = int.Parse(AV8B_ALL_CHAFF_SQTY_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_ALL_CHAFF_SQTY.Value = numericUpDown_AV8B_ALL_CHAFF_SQTY.Minimum; }

            try { numericUpDown_AV8B_ALL_CHAFF_SINT.Value = int.Parse(AV8B_ALL_CHAFF_SINT_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_ALL_CHAFF_SINT.Value = numericUpDown_AV8B_ALL_CHAFF_SINT.Minimum; }

            try { numericUpDown_AV8B_ALL_FLARES_SQTY.Value = int.Parse(AV8B_ALL_FLARES_SQTY_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_ALL_FLARES_SQTY.Value = numericUpDown_AV8B_ALL_FLARES_SQTY.Minimum; }

            try { numericUpDown_AV8B_ALL_FLARES_SINT.Value = int.Parse(AV8B_ALL_FLARES_SINT_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_ALL_FLARES_SINT.Value = numericUpDown_AV8B_ALL_FLARES_SINT.Minimum; }

            //chaff only

            if (AV8B_CHAFF_BQTY_amount.Contains("-1"))//if the value in the file is -1
            {
                //numericUpDown_AV8B_CHAFF_BQTY.Value = numericUpDown_AV8B_CHAFF_BQTY.Minimum;//make the number the lowest it can go
                numericUpDown_AV8B_CHAFF_BQTY.Enabled = false;//disable the use of the numbers
                checkBox_AV8B_chaffBquantity_continuous.Checked = true;//check the box
                checkBox_AV8B_chaffBquantity_random.Checked = false;//uncheck this box

            }
            else if (AV8B_CHAFF_BQTY_amount.Contains("-2"))
            {
                //numericUpDown_AV8B_CHAFF_BQTY.Value = numericUpDown_AV8B_CHAFF_BQTY.Minimum;
                numericUpDown_AV8B_CHAFF_BQTY.Enabled = false;
                checkBox_AV8B_chaffBquantity_continuous.Checked = false;
                checkBox_AV8B_chaffBquantity_random.Checked = true;

            }
            else
            {
                checkBox_AV8B_chaffBquantity_continuous.Checked = false;
                checkBox_AV8B_chaffBquantity_random.Checked = false;
                numericUpDown_AV8B_CHAFF_BQTY.Enabled = true;
                try { numericUpDown_AV8B_CHAFF_BQTY.Value = int.Parse(AV8B_CHAFF_BQTY_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_CHAFF_BQTY.Value = numericUpDown_AV8B_CHAFF_BQTY.Minimum; }
            }



            //second entry
            if (AV8B_CHAFF_BQTY_amount.Contains("-2"))
            {
                //numericUpDown_AV8B_CHAFF_BINT.Value = numericUpDown_AV8B_CHAFF_BINT.Minimum;
                numericUpDown_AV8B_CHAFF_BINT.Enabled = false;
                checkBox_AV8B_chaffBinterval_random.Checked = true;
            }
            else
            {
                numericUpDown_AV8B_CHAFF_BINT.Enabled = true;
              
                checkBox_AV8B_chaffBinterval_random.Checked = false;
                try { numericUpDown_AV8B_CHAFF_BINT.Value = Decimal.Parse(AV8B_CHAFF_BINT_amount); }
                catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_CHAFF_BINT.Value = numericUpDown_AV8B_CHAFF_BINT.Minimum; }
            }


            try { numericUpDown_AV8B_CHAFF_SQTY.Value = int.Parse(AV8B_CHAFF_SQTY_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_CHAFF_SQTY.Value = numericUpDown_AV8B_CHAFF_SQTY.Minimum; }

            try { numericUpDown_AV8B_CHAFF_SINT.Value = int.Parse(AV8B_CHAFF_SINT_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_CHAFF_SINT.Value = numericUpDown_AV8B_CHAFF_SINT.Minimum; }


            //flares only
            try { numericUpDown_AV8B_FLARES_SQTY.Value = int.Parse(AV8B_FLARES_SQTY_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_FLARES_SQTY.Value = numericUpDown_AV8B_FLARES_SQTY.Minimum; }

            try { numericUpDown_AV8B_FLARES_SINT.Value = int.Parse(AV8B_FLARES_SINT_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_AV8B_FLARES_SINT.Value = numericUpDown_AV8B_FLARES_SINT.Minimum; }
        }

        private void loadLua_M2000C_CMS()
        {
        

            //find the lua file
            string CountermeasureFileString_M2000C = loadLocation;
            //load the text into a string
            string CountermeasureFileStringText_M2000C = File.ReadAllText(CountermeasureFileString_M2000C);


            //program1
            int M2000C_program1_chaff_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[1][\"chaff\"]  =") + 23;
            int M2000C_program1_chaff_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program1_chaff_getIndexStart);
            string M2000C_program1_chaff_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program1_chaff_getIndexStart, M2000C_program1_chaff_getIndexEnd - M2000C_program1_chaff_getIndexStart);
            try { numericUpDown_M2000C_program1_chaff.Value = int.Parse(M2000C_program1_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program1_chaff.Value = numericUpDown_M2000C_program1_chaff.Minimum; }

            int M2000C_program1_flare_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[1][\"flare\"]  =") + 23;
            int M2000C_program1_flare_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program1_flare_getIndexStart);
            string M2000C_program1_flare_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program1_flare_getIndexStart, M2000C_program1_flare_getIndexEnd - M2000C_program1_flare_getIndexStart);
            try { numericUpDown_M2000C_program1_flare.Value = int.Parse(M2000C_program1_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program1_flare.Value = numericUpDown_M2000C_program1_flare.Minimum; }

            //this needs parcing to show the correct data
            //https://docs.microsoft.com/en-us/dotnet/api/system.string.insert?view=netcore-3.1
            int M2000C_program1_interval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[1][\"intv\"]   =") + 23;
            int M2000C_program1_interval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program1_interval_getIndexStart);//there is an error that inserts an extra return line in the result of the string
            string M2000C_program1_interval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program1_interval_getIndexStart, M2000C_program1_interval_getIndexEnd - M2000C_program1_interval_getIndexStart);
            M2000C_program1_interval_amount = Decimal.Parse(M2000C_program1_interval_amount).ToString();//takes out some junk
            M2000C_program1_interval_amount = M2000C_program1_interval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program1_interval_amount = M2000C_program1_interval_amount.Insert(M2000C_program1_interval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program1_interval_amount = Decimal.Parse(M2000C_program1_interval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show("|" + M2000C_program1_interval_amount + "|");//debugging
            //MessageBox.Show(M2000C_program1_interval_amount);//debugging
            try { numericUpDown_M2000C_program1_interval.Value = Decimal.Parse(M2000C_program1_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program1_interval.Value = numericUpDown_M2000C_program1_interval.Minimum; }

            int M2000C_program1_cycle_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[1][\"cycle\"]  =") + 23;
            int M2000C_program1_cycle_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program1_cycle_getIndexStart);
            string M2000C_program1_cycle_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program1_cycle_getIndexStart, M2000C_program1_cycle_getIndexEnd - M2000C_program1_cycle_getIndexStart);
            try { numericUpDown_M2000C_program1_cycle.Value = int.Parse(M2000C_program1_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program1_cycle.Value = numericUpDown_M2000C_program1_cycle.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program1_cycleInterval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[1][\"c_intv\"] =") + 23;
            int M2000C_program1_cycleInterval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program1_cycleInterval_getIndexStart);
            string M2000C_program1_cycleInterval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program1_cycleInterval_getIndexStart, M2000C_program1_cycleInterval_getIndexEnd - M2000C_program1_cycleInterval_getIndexStart);
            //M2000C_program1_cycleInterval_amount = M2000C_program1_cycleInterval_amount.Insert(M2000C_program1_cycleInterval_amount.Length - 3, ".");
            //MessageBox.Show(M2000C_program1_cycleInterval_amount);//debugging
            M2000C_program1_cycleInterval_amount = Decimal.Parse(M2000C_program1_cycleInterval_amount).ToString();//takes out some junk
            M2000C_program1_cycleInterval_amount = M2000C_program1_cycleInterval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program1_cycleInterval_amount = M2000C_program1_cycleInterval_amount.Insert(M2000C_program1_cycleInterval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program1_cycleInterval_amount = Decimal.Parse(M2000C_program1_cycleInterval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show(M2000C_program1_cycleInterval_amount);//debugging
            try { numericUpDown_M2000C_program1_cycleInterval.Value = Decimal.Parse(M2000C_program1_cycleInterval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program1_cycleInterval.Value = numericUpDown_M2000C_program1_cycleInterval.Minimum; }



            //program2
            int M2000C_program2_chaff_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[2][\"chaff\"]  =") + 23;
            int M2000C_program2_chaff_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program2_chaff_getIndexStart);
            string M2000C_program2_chaff_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program2_chaff_getIndexStart, M2000C_program2_chaff_getIndexEnd - M2000C_program2_chaff_getIndexStart);
            try { numericUpDown_M2000C_program2_chaff.Value = int.Parse(M2000C_program2_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program2_chaff.Value = numericUpDown_M2000C_program2_chaff.Minimum; }

            int M2000C_program2_flare_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[2][\"flare\"]  =") + 23;
            int M2000C_program2_flare_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program2_flare_getIndexStart);
            string M2000C_program2_flare_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program2_flare_getIndexStart, M2000C_program2_flare_getIndexEnd - M2000C_program2_flare_getIndexStart);
            try { numericUpDown_M2000C_program2_flare.Value = int.Parse(M2000C_program2_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program2_flare.Value = numericUpDown_M2000C_program2_flare.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program2_interval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[2][\"intv\"]   =") + 23;
            int M2000C_program2_interval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program2_interval_getIndexStart);//there is an error that inserts an extra return line in the result of the string
            string M2000C_program2_interval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program2_interval_getIndexStart, M2000C_program2_interval_getIndexEnd - M2000C_program2_interval_getIndexStart);
            M2000C_program2_interval_amount = Decimal.Parse(M2000C_program2_interval_amount).ToString();//takes out some junk
            M2000C_program2_interval_amount = M2000C_program2_interval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program2_interval_amount = M2000C_program2_interval_amount.Insert(M2000C_program2_interval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program2_interval_amount = Decimal.Parse(M2000C_program2_interval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show("|" + M2000C_program2_interval_amount + "|");//debugging
            //MessageBox.Show(M2000C_program2_interval_amount);//debugging
            try { numericUpDown_M2000C_program2_interval.Value = Decimal.Parse(M2000C_program2_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program2_interval.Value = numericUpDown_M2000C_program2_interval.Minimum; }

            int M2000C_program2_cycle_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[2][\"cycle\"]  =") + 23;
            int M2000C_program2_cycle_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program2_cycle_getIndexStart);
            string M2000C_program2_cycle_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program2_cycle_getIndexStart, M2000C_program2_cycle_getIndexEnd - M2000C_program2_cycle_getIndexStart);
            try { numericUpDown_M2000C_program2_cycle.Value = int.Parse(M2000C_program2_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program2_cycle.Value = numericUpDown_M2000C_program2_cycle.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program2_cycleInterval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[2][\"c_intv\"] =") + 23;
            int M2000C_program2_cycleInterval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program2_cycleInterval_getIndexStart);
            string M2000C_program2_cycleInterval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program2_cycleInterval_getIndexStart, M2000C_program2_cycleInterval_getIndexEnd - M2000C_program2_cycleInterval_getIndexStart);
            //M2000C_program2_cycleInterval_amount = M2000C_program2_cycleInterval_amount.Insert(M2000C_program2_cycleInterval_amount.Length - 3, ".");
            //MessageBox.Show(M2000C_program2_cycleInterval_amount);//debugging
            M2000C_program2_cycleInterval_amount = Decimal.Parse(M2000C_program2_cycleInterval_amount).ToString();//takes out some junk
            M2000C_program2_cycleInterval_amount = M2000C_program2_cycleInterval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program2_cycleInterval_amount = M2000C_program2_cycleInterval_amount.Insert(M2000C_program2_cycleInterval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program2_cycleInterval_amount = Decimal.Parse(M2000C_program2_cycleInterval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show(M2000C_program2_cycleInterval_amount);//debugging
            try { numericUpDown_M2000C_program2_cycleInterval.Value = Decimal.Parse(M2000C_program2_cycleInterval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program2_cycleInterval.Value = numericUpDown_M2000C_program2_cycleInterval.Minimum; }


            //program3
            int M2000C_program3_chaff_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[3][\"chaff\"]  =") + 23;
            int M2000C_program3_chaff_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program3_chaff_getIndexStart);
            string M2000C_program3_chaff_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program3_chaff_getIndexStart, M2000C_program3_chaff_getIndexEnd - M2000C_program3_chaff_getIndexStart);
            try { numericUpDown_M2000C_program3_chaff.Value = int.Parse(M2000C_program3_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program3_chaff.Value = numericUpDown_M2000C_program3_chaff.Minimum; }

            int M2000C_program3_flare_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[3][\"flare\"]  =") + 23;
            int M2000C_program3_flare_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program3_flare_getIndexStart);
            string M2000C_program3_flare_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program3_flare_getIndexStart, M2000C_program3_flare_getIndexEnd - M2000C_program3_flare_getIndexStart);
            try { numericUpDown_M2000C_program3_flare.Value = int.Parse(M2000C_program3_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program3_flare.Value = numericUpDown_M2000C_program3_flare.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program3_interval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[3][\"intv\"]   =") + 23;
            int M2000C_program3_interval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program3_interval_getIndexStart);//there is an error that inserts an extra return line in the result of the string
            string M2000C_program3_interval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program3_interval_getIndexStart, M2000C_program3_interval_getIndexEnd - M2000C_program3_interval_getIndexStart);
            M2000C_program3_interval_amount = Decimal.Parse(M2000C_program3_interval_amount).ToString();//takes out some junk
            M2000C_program3_interval_amount = M2000C_program3_interval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program3_interval_amount = M2000C_program3_interval_amount.Insert(M2000C_program3_interval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program3_interval_amount = Decimal.Parse(M2000C_program3_interval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show("|" + M2000C_program3_interval_amount + "|");//debugging
            //MessageBox.Show(M2000C_program3_interval_amount);//debugging
            try { numericUpDown_M2000C_program3_interval.Value = Decimal.Parse(M2000C_program3_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program3_interval.Value = numericUpDown_M2000C_program3_interval.Minimum; }

            int M2000C_program3_cycle_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[3][\"cycle\"]  =") + 23;
            int M2000C_program3_cycle_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program3_cycle_getIndexStart);
            string M2000C_program3_cycle_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program3_cycle_getIndexStart, M2000C_program3_cycle_getIndexEnd - M2000C_program3_cycle_getIndexStart);
            try { numericUpDown_M2000C_program3_cycle.Value = int.Parse(M2000C_program3_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program3_cycle.Value = numericUpDown_M2000C_program3_cycle.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program3_cycleInterval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[3][\"c_intv\"] =") + 23;
            int M2000C_program3_cycleInterval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program3_cycleInterval_getIndexStart);
            string M2000C_program3_cycleInterval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program3_cycleInterval_getIndexStart, M2000C_program3_cycleInterval_getIndexEnd - M2000C_program3_cycleInterval_getIndexStart);
            //M2000C_program3_cycleInterval_amount = M2000C_program3_cycleInterval_amount.Insert(M2000C_program3_cycleInterval_amount.Length - 3, ".");
            //MessageBox.Show(M2000C_program3_cycleInterval_amount);//debugging
            M2000C_program3_cycleInterval_amount = Decimal.Parse(M2000C_program3_cycleInterval_amount).ToString();//takes out some junk
            M2000C_program3_cycleInterval_amount = M2000C_program3_cycleInterval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program3_cycleInterval_amount = M2000C_program3_cycleInterval_amount.Insert(M2000C_program3_cycleInterval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program3_cycleInterval_amount = Decimal.Parse(M2000C_program3_cycleInterval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show(M2000C_program3_cycleInterval_amount);//debugging
            try { numericUpDown_M2000C_program3_cycleInterval.Value = Decimal.Parse(M2000C_program3_cycleInterval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program3_cycleInterval.Value = numericUpDown_M2000C_program3_cycleInterval.Minimum; }


            //program4
            int M2000C_program4_chaff_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[4][\"chaff\"]  =") + 23;
            int M2000C_program4_chaff_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program4_chaff_getIndexStart);
            string M2000C_program4_chaff_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program4_chaff_getIndexStart, M2000C_program4_chaff_getIndexEnd - M2000C_program4_chaff_getIndexStart);
            try { numericUpDown_M2000C_program4_chaff.Value = int.Parse(M2000C_program4_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program4_chaff.Value = numericUpDown_M2000C_program4_chaff.Minimum; }

            int M2000C_program4_flare_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[4][\"flare\"]  =") + 23;
            int M2000C_program4_flare_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program4_flare_getIndexStart);
            string M2000C_program4_flare_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program4_flare_getIndexStart, M2000C_program4_flare_getIndexEnd - M2000C_program4_flare_getIndexStart);
            try { numericUpDown_M2000C_program4_flare.Value = int.Parse(M2000C_program4_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program4_flare.Value = numericUpDown_M2000C_program4_flare.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program4_interval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[4][\"intv\"]   =") + 23;
            int M2000C_program4_interval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program4_interval_getIndexStart);//there is an error that inserts an extra return line in the result of the string
            string M2000C_program4_interval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program4_interval_getIndexStart, M2000C_program4_interval_getIndexEnd - M2000C_program4_interval_getIndexStart);
            M2000C_program4_interval_amount = Decimal.Parse(M2000C_program4_interval_amount).ToString();//takes out some junk
            M2000C_program4_interval_amount = M2000C_program4_interval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program4_interval_amount = M2000C_program4_interval_amount.Insert(M2000C_program4_interval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program4_interval_amount = Decimal.Parse(M2000C_program4_interval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show("|" + M2000C_program4_interval_amount + "|");//debugging
            //MessageBox.Show(M2000C_program4_interval_amount);//debugging
            try { numericUpDown_M2000C_program4_interval.Value = Decimal.Parse(M2000C_program4_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program4_interval.Value = numericUpDown_M2000C_program4_interval.Minimum; }

            int M2000C_program4_cycle_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[4][\"cycle\"]  =") + 23;
            int M2000C_program4_cycle_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program4_cycle_getIndexStart);
            string M2000C_program4_cycle_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program4_cycle_getIndexStart, M2000C_program4_cycle_getIndexEnd - M2000C_program4_cycle_getIndexStart);
            try { numericUpDown_M2000C_program4_cycle.Value = int.Parse(M2000C_program4_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program4_cycle.Value = numericUpDown_M2000C_program4_cycle.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program4_cycleInterval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[4][\"c_intv\"] =") + 23;
            int M2000C_program4_cycleInterval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program4_cycleInterval_getIndexStart);
            string M2000C_program4_cycleInterval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program4_cycleInterval_getIndexStart, M2000C_program4_cycleInterval_getIndexEnd - M2000C_program4_cycleInterval_getIndexStart);
            //M2000C_program4_cycleInterval_amount = M2000C_program4_cycleInterval_amount.Insert(M2000C_program4_cycleInterval_amount.Length - 3, ".");
            //MessageBox.Show(M2000C_program4_cycleInterval_amount);//debugging
            M2000C_program4_cycleInterval_amount = Decimal.Parse(M2000C_program4_cycleInterval_amount).ToString();//takes out some junk
            M2000C_program4_cycleInterval_amount = M2000C_program4_cycleInterval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program4_cycleInterval_amount = M2000C_program4_cycleInterval_amount.Insert(M2000C_program4_cycleInterval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program4_cycleInterval_amount = Decimal.Parse(M2000C_program4_cycleInterval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show(M2000C_program4_cycleInterval_amount);//debugging
            try { numericUpDown_M2000C_program4_cycleInterval.Value = Decimal.Parse(M2000C_program4_cycleInterval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program4_cycleInterval.Value = numericUpDown_M2000C_program4_cycleInterval.Minimum; }


            //program5
            int M2000C_program5_chaff_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[5][\"chaff\"]  =") + 23;
            int M2000C_program5_chaff_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program5_chaff_getIndexStart);
            string M2000C_program5_chaff_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program5_chaff_getIndexStart, M2000C_program5_chaff_getIndexEnd - M2000C_program5_chaff_getIndexStart);
            try { numericUpDown_M2000C_program5_chaff.Value = int.Parse(M2000C_program5_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program5_chaff.Value = numericUpDown_M2000C_program5_chaff.Minimum; }

            int M2000C_program5_flare_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[5][\"flare\"]  =") + 23;
            int M2000C_program5_flare_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program5_flare_getIndexStart);
            string M2000C_program5_flare_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program5_flare_getIndexStart, M2000C_program5_flare_getIndexEnd - M2000C_program5_flare_getIndexStart);
            try { numericUpDown_M2000C_program5_flare.Value = int.Parse(M2000C_program5_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program5_flare.Value = numericUpDown_M2000C_program5_flare.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program5_interval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[5][\"intv\"]   =") + 23;
            int M2000C_program5_interval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program5_interval_getIndexStart);//there is an error that inserts an extra return line in the result of the string
            string M2000C_program5_interval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program5_interval_getIndexStart, M2000C_program5_interval_getIndexEnd - M2000C_program5_interval_getIndexStart);
            M2000C_program5_interval_amount = Decimal.Parse(M2000C_program5_interval_amount).ToString();//takes out some junk
            M2000C_program5_interval_amount = M2000C_program5_interval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program5_interval_amount = M2000C_program5_interval_amount.Insert(M2000C_program5_interval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program5_interval_amount = Decimal.Parse(M2000C_program5_interval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show("|" + M2000C_program5_interval_amount + "|");//debugging
            //MessageBox.Show(M2000C_program5_interval_amount);//debugging
            try { numericUpDown_M2000C_program5_interval.Value = Decimal.Parse(M2000C_program5_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program5_interval.Value = numericUpDown_M2000C_program5_interval.Minimum; }

            int M2000C_program5_cycle_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[5][\"cycle\"]  =") + 23;
            int M2000C_program5_cycle_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program5_cycle_getIndexStart);
            string M2000C_program5_cycle_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program5_cycle_getIndexStart, M2000C_program5_cycle_getIndexEnd - M2000C_program5_cycle_getIndexStart);
            try { numericUpDown_M2000C_program5_cycle.Value = int.Parse(M2000C_program5_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program5_cycle.Value = numericUpDown_M2000C_program5_cycle.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program5_cycleInterval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[5][\"c_intv\"] =") + 23;
            int M2000C_program5_cycleInterval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program5_cycleInterval_getIndexStart);
            string M2000C_program5_cycleInterval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program5_cycleInterval_getIndexStart, M2000C_program5_cycleInterval_getIndexEnd - M2000C_program5_cycleInterval_getIndexStart);
            //M2000C_program5_cycleInterval_amount = M2000C_program5_cycleInterval_amount.Insert(M2000C_program5_cycleInterval_amount.Length - 3, ".");
            //MessageBox.Show(M2000C_program5_cycleInterval_amount);//debugging
            M2000C_program5_cycleInterval_amount = Decimal.Parse(M2000C_program5_cycleInterval_amount).ToString();//takes out some junk
            M2000C_program5_cycleInterval_amount = M2000C_program5_cycleInterval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program5_cycleInterval_amount = M2000C_program5_cycleInterval_amount.Insert(M2000C_program5_cycleInterval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program5_cycleInterval_amount = Decimal.Parse(M2000C_program5_cycleInterval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show(M2000C_program5_cycleInterval_amount);//debugging
            try { numericUpDown_M2000C_program5_cycleInterval.Value = Decimal.Parse(M2000C_program5_cycleInterval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program5_cycleInterval.Value = numericUpDown_M2000C_program5_cycleInterval.Minimum; }




            //program6
            int M2000C_program6_chaff_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[6][\"chaff\"]  =") + 23;
            int M2000C_program6_chaff_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program6_chaff_getIndexStart);
            string M2000C_program6_chaff_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program6_chaff_getIndexStart, M2000C_program6_chaff_getIndexEnd - M2000C_program6_chaff_getIndexStart);
            try { numericUpDown_M2000C_program6_chaff.Value = int.Parse(M2000C_program6_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program6_chaff.Value = numericUpDown_M2000C_program6_chaff.Minimum; }

            int M2000C_program6_flare_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[6][\"flare\"]  =") + 23;
            int M2000C_program6_flare_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program6_flare_getIndexStart);
            string M2000C_program6_flare_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program6_flare_getIndexStart, M2000C_program6_flare_getIndexEnd - M2000C_program6_flare_getIndexStart);
            try { numericUpDown_M2000C_program6_flare.Value = int.Parse(M2000C_program6_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program6_flare.Value = numericUpDown_M2000C_program6_flare.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program6_interval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[6][\"intv\"]   =") + 23;
            int M2000C_program6_interval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program6_interval_getIndexStart);//there is an error that inserts an extra return line in the result of the string
            string M2000C_program6_interval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program6_interval_getIndexStart, M2000C_program6_interval_getIndexEnd - M2000C_program6_interval_getIndexStart);
            M2000C_program6_interval_amount = Decimal.Parse(M2000C_program6_interval_amount).ToString();//takes out some junk
            M2000C_program6_interval_amount = M2000C_program6_interval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program6_interval_amount = M2000C_program6_interval_amount.Insert(M2000C_program6_interval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program6_interval_amount = Decimal.Parse(M2000C_program6_interval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show("|" + M2000C_program6_interval_amount + "|");//debugging
            //MessageBox.Show(M2000C_program6_interval_amount);//debugging
            try { numericUpDown_M2000C_program6_interval.Value = Decimal.Parse(M2000C_program6_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program6_interval.Value = numericUpDown_M2000C_program6_interval.Minimum; }

            int M2000C_program6_cycle_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[6][\"cycle\"]  =") + 23;
            int M2000C_program6_cycle_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program6_cycle_getIndexStart);
            string M2000C_program6_cycle_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program6_cycle_getIndexStart, M2000C_program6_cycle_getIndexEnd - M2000C_program6_cycle_getIndexStart);
            try { numericUpDown_M2000C_program6_cycle.Value = int.Parse(M2000C_program6_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program6_cycle.Value = numericUpDown_M2000C_program6_cycle.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program6_cycleInterval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[6][\"c_intv\"] =") + 23;
            int M2000C_program6_cycleInterval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program6_cycleInterval_getIndexStart);
            string M2000C_program6_cycleInterval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program6_cycleInterval_getIndexStart, M2000C_program6_cycleInterval_getIndexEnd - M2000C_program6_cycleInterval_getIndexStart);
            //M2000C_program6_cycleInterval_amount = M2000C_program6_cycleInterval_amount.Insert(M2000C_program6_cycleInterval_amount.Length - 3, ".");
            //MessageBox.Show(M2000C_program6_cycleInterval_amount);//debugging
            M2000C_program6_cycleInterval_amount = Decimal.Parse(M2000C_program6_cycleInterval_amount).ToString();//takes out some junk
            M2000C_program6_cycleInterval_amount = M2000C_program6_cycleInterval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program6_cycleInterval_amount = M2000C_program6_cycleInterval_amount.Insert(M2000C_program6_cycleInterval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program6_cycleInterval_amount = Decimal.Parse(M2000C_program6_cycleInterval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show(M2000C_program6_cycleInterval_amount);//debugging
            try { numericUpDown_M2000C_program6_cycleInterval.Value = Decimal.Parse(M2000C_program6_cycleInterval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program6_cycleInterval.Value = numericUpDown_M2000C_program6_cycleInterval.Minimum; }




            //program7
            int M2000C_program7_chaff_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[7][\"chaff\"]  =") + 23;
            int M2000C_program7_chaff_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program7_chaff_getIndexStart);
            string M2000C_program7_chaff_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program7_chaff_getIndexStart, M2000C_program7_chaff_getIndexEnd - M2000C_program7_chaff_getIndexStart);
            try { numericUpDown_M2000C_program7_chaff.Value = int.Parse(M2000C_program7_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program7_chaff.Value = numericUpDown_M2000C_program7_chaff.Minimum; }

            int M2000C_program7_flare_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[7][\"flare\"]  =") + 23;
            int M2000C_program7_flare_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program7_flare_getIndexStart);
            string M2000C_program7_flare_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program7_flare_getIndexStart, M2000C_program7_flare_getIndexEnd - M2000C_program7_flare_getIndexStart);
            try { numericUpDown_M2000C_program7_flare.Value = int.Parse(M2000C_program7_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program7_flare.Value = numericUpDown_M2000C_program7_flare.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program7_interval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[7][\"intv\"]   =") + 23;
            int M2000C_program7_interval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program7_interval_getIndexStart);//there is an error that inserts an extra return line in the result of the string
            string M2000C_program7_interval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program7_interval_getIndexStart, M2000C_program7_interval_getIndexEnd - M2000C_program7_interval_getIndexStart);
            M2000C_program7_interval_amount = Decimal.Parse(M2000C_program7_interval_amount).ToString();//takes out some junk
            M2000C_program7_interval_amount = M2000C_program7_interval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program7_interval_amount = M2000C_program7_interval_amount.Insert(M2000C_program7_interval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program7_interval_amount = Decimal.Parse(M2000C_program7_interval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show("|" + M2000C_program7_interval_amount + "|");//debugging
            //MessageBox.Show(M2000C_program7_interval_amount);//debugging
            try { numericUpDown_M2000C_program7_interval.Value = Decimal.Parse(M2000C_program7_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program7_interval.Value = numericUpDown_M2000C_program7_interval.Minimum; }

            int M2000C_program7_cycle_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[7][\"cycle\"]  =") + 23;
            int M2000C_program7_cycle_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program7_cycle_getIndexStart);
            string M2000C_program7_cycle_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program7_cycle_getIndexStart, M2000C_program7_cycle_getIndexEnd - M2000C_program7_cycle_getIndexStart);
            try { numericUpDown_M2000C_program7_cycle.Value = int.Parse(M2000C_program7_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program7_cycle.Value = numericUpDown_M2000C_program7_cycle.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program7_cycleInterval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[7][\"c_intv\"] =") + 23;
            int M2000C_program7_cycleInterval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program7_cycleInterval_getIndexStart);
            string M2000C_program7_cycleInterval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program7_cycleInterval_getIndexStart, M2000C_program7_cycleInterval_getIndexEnd - M2000C_program7_cycleInterval_getIndexStart);
            //M2000C_program7_cycleInterval_amount = M2000C_program7_cycleInterval_amount.Insert(M2000C_program7_cycleInterval_amount.Length - 3, ".");
            //MessageBox.Show(M2000C_program7_cycleInterval_amount);//debugging
            M2000C_program7_cycleInterval_amount = Decimal.Parse(M2000C_program7_cycleInterval_amount).ToString();//takes out some junk
            M2000C_program7_cycleInterval_amount = M2000C_program7_cycleInterval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program7_cycleInterval_amount = M2000C_program7_cycleInterval_amount.Insert(M2000C_program7_cycleInterval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program7_cycleInterval_amount = Decimal.Parse(M2000C_program7_cycleInterval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show(M2000C_program7_cycleInterval_amount);//debugging
            try { numericUpDown_M2000C_program7_cycleInterval.Value = Decimal.Parse(M2000C_program7_cycleInterval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program7_cycleInterval.Value = numericUpDown_M2000C_program7_cycleInterval.Minimum; }



            //program8
            int M2000C_program8_chaff_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[8][\"chaff\"]  =") + 23;
            int M2000C_program8_chaff_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program8_chaff_getIndexStart);
            string M2000C_program8_chaff_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program8_chaff_getIndexStart, M2000C_program8_chaff_getIndexEnd - M2000C_program8_chaff_getIndexStart);
            try { numericUpDown_M2000C_program8_chaff.Value = int.Parse(M2000C_program8_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program8_chaff.Value = numericUpDown_M2000C_program8_chaff.Minimum; }

            int M2000C_program8_flare_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[8][\"flare\"]  =") + 23;
            int M2000C_program8_flare_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program8_flare_getIndexStart);
            string M2000C_program8_flare_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program8_flare_getIndexStart, M2000C_program8_flare_getIndexEnd - M2000C_program8_flare_getIndexStart);
            try { numericUpDown_M2000C_program8_flare.Value = int.Parse(M2000C_program8_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program8_flare.Value = numericUpDown_M2000C_program8_flare.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program8_interval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[8][\"intv\"]   =") + 23;
            int M2000C_program8_interval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program8_interval_getIndexStart);//there is an error that inserts an extra return line in the result of the string
            string M2000C_program8_interval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program8_interval_getIndexStart, M2000C_program8_interval_getIndexEnd - M2000C_program8_interval_getIndexStart);
            M2000C_program8_interval_amount = Decimal.Parse(M2000C_program8_interval_amount).ToString();//takes out some junk
            M2000C_program8_interval_amount = M2000C_program8_interval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program8_interval_amount = M2000C_program8_interval_amount.Insert(M2000C_program8_interval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program8_interval_amount = Decimal.Parse(M2000C_program8_interval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show("|" + M2000C_program8_interval_amount + "|");//debugging
            //MessageBox.Show(M2000C_program8_interval_amount);//debugging
            try { numericUpDown_M2000C_program8_interval.Value = Decimal.Parse(M2000C_program8_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program8_interval.Value = numericUpDown_M2000C_program8_interval.Minimum; }

            int M2000C_program8_cycle_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[8][\"cycle\"]  =") + 23;
            int M2000C_program8_cycle_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program8_cycle_getIndexStart);
            string M2000C_program8_cycle_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program8_cycle_getIndexStart, M2000C_program8_cycle_getIndexEnd - M2000C_program8_cycle_getIndexStart);
            try { numericUpDown_M2000C_program8_cycle.Value = int.Parse(M2000C_program8_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program8_cycle.Value = numericUpDown_M2000C_program8_cycle.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program8_cycleInterval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[8][\"c_intv\"] =") + 23;
            int M2000C_program8_cycleInterval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program8_cycleInterval_getIndexStart);
            string M2000C_program8_cycleInterval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program8_cycleInterval_getIndexStart, M2000C_program8_cycleInterval_getIndexEnd - M2000C_program8_cycleInterval_getIndexStart);
            //M2000C_program8_cycleInterval_amount = M2000C_program8_cycleInterval_amount.Insert(M2000C_program8_cycleInterval_amount.Length - 3, ".");
            //MessageBox.Show(M2000C_program8_cycleInterval_amount);//debugging
            M2000C_program8_cycleInterval_amount = Decimal.Parse(M2000C_program8_cycleInterval_amount).ToString();//takes out some junk
            M2000C_program8_cycleInterval_amount = M2000C_program8_cycleInterval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program8_cycleInterval_amount = M2000C_program8_cycleInterval_amount.Insert(M2000C_program8_cycleInterval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program8_cycleInterval_amount = Decimal.Parse(M2000C_program8_cycleInterval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show(M2000C_program8_cycleInterval_amount);//debugging
            try { numericUpDown_M2000C_program8_cycleInterval.Value = Decimal.Parse(M2000C_program8_cycleInterval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program8_cycleInterval.Value = numericUpDown_M2000C_program8_cycleInterval.Minimum; }



            //program9
            int M2000C_program9_chaff_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[9][\"chaff\"]  =") + 23;
            int M2000C_program9_chaff_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program9_chaff_getIndexStart);
            string M2000C_program9_chaff_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program9_chaff_getIndexStart, M2000C_program9_chaff_getIndexEnd - M2000C_program9_chaff_getIndexStart);
            try { numericUpDown_M2000C_program9_chaff.Value = int.Parse(M2000C_program9_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program9_chaff.Value = numericUpDown_M2000C_program9_chaff.Minimum; }

            int M2000C_program9_flare_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[9][\"flare\"]  =") + 23;
            int M2000C_program9_flare_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program9_flare_getIndexStart);
            string M2000C_program9_flare_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program9_flare_getIndexStart, M2000C_program9_flare_getIndexEnd - M2000C_program9_flare_getIndexStart);
            try { numericUpDown_M2000C_program9_flare.Value = int.Parse(M2000C_program9_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program9_flare.Value = numericUpDown_M2000C_program9_flare.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program9_interval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[9][\"intv\"]   =") + 23;
            int M2000C_program9_interval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program9_interval_getIndexStart);//there is an error that inserts an extra return line in the result of the string
            string M2000C_program9_interval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program9_interval_getIndexStart, M2000C_program9_interval_getIndexEnd - M2000C_program9_interval_getIndexStart);
            M2000C_program9_interval_amount = Decimal.Parse(M2000C_program9_interval_amount).ToString();//takes out some junk
            M2000C_program9_interval_amount = M2000C_program9_interval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program9_interval_amount = M2000C_program9_interval_amount.Insert(M2000C_program9_interval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program9_interval_amount = Decimal.Parse(M2000C_program9_interval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show("|" + M2000C_program9_interval_amount + "|");//debugging
            //MessageBox.Show(M2000C_program9_interval_amount);//debugging
            try { numericUpDown_M2000C_program9_interval.Value = Decimal.Parse(M2000C_program9_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program9_interval.Value = numericUpDown_M2000C_program9_interval.Minimum; }

            int M2000C_program9_cycle_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[9][\"cycle\"]  =") + 23;
            int M2000C_program9_cycle_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program9_cycle_getIndexStart);
            string M2000C_program9_cycle_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program9_cycle_getIndexStart, M2000C_program9_cycle_getIndexEnd - M2000C_program9_cycle_getIndexStart);
            try { numericUpDown_M2000C_program9_cycle.Value = int.Parse(M2000C_program9_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program9_cycle.Value = numericUpDown_M2000C_program9_cycle.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program9_cycleInterval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[9][\"c_intv\"] =") + 23;
            int M2000C_program9_cycleInterval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program9_cycleInterval_getIndexStart);
            string M2000C_program9_cycleInterval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program9_cycleInterval_getIndexStart, M2000C_program9_cycleInterval_getIndexEnd - M2000C_program9_cycleInterval_getIndexStart);
            //M2000C_program9_cycleInterval_amount = M2000C_program9_cycleInterval_amount.Insert(M2000C_program9_cycleInterval_amount.Length - 3, ".");
            //MessageBox.Show(M2000C_program9_cycleInterval_amount);//debugging
            M2000C_program9_cycleInterval_amount = Decimal.Parse(M2000C_program9_cycleInterval_amount).ToString();//takes out some junk
            M2000C_program9_cycleInterval_amount = M2000C_program9_cycleInterval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program9_cycleInterval_amount = M2000C_program9_cycleInterval_amount.Insert(M2000C_program9_cycleInterval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program9_cycleInterval_amount = Decimal.Parse(M2000C_program9_cycleInterval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show(M2000C_program9_cycleInterval_amount);//debugging
            try { numericUpDown_M2000C_program9_cycleInterval.Value = Decimal.Parse(M2000C_program9_cycleInterval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program9_cycleInterval.Value = numericUpDown_M2000C_program9_cycleInterval.Minimum; }



            //program10
            int M2000C_program10_chaff_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[10][\"chaff\"]  =") + 24;//added 1 digit because the number '10' has 1 more digit than the number '9'
            int M2000C_program10_chaff_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program10_chaff_getIndexStart);
            string M2000C_program10_chaff_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program10_chaff_getIndexStart, M2000C_program10_chaff_getIndexEnd - M2000C_program10_chaff_getIndexStart);
            try { numericUpDown_M2000C_program10_chaff.Value = int.Parse(M2000C_program10_chaff_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program10_chaff.Value = numericUpDown_M2000C_program10_chaff.Minimum; }

            int M2000C_program10_flare_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[10][\"flare\"]  =") + 24;
            int M2000C_program10_flare_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program10_flare_getIndexStart);
            string M2000C_program10_flare_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program10_flare_getIndexStart, M2000C_program10_flare_getIndexEnd - M2000C_program10_flare_getIndexStart);
            try { numericUpDown_M2000C_program10_flare.Value = int.Parse(M2000C_program10_flare_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program10_flare.Value = numericUpDown_M2000C_program10_flare.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program10_interval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[10][\"intv\"]   =") + 24;
            int M2000C_program10_interval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program10_interval_getIndexStart);//there is an error that inserts an extra return line in the result of the string
            string M2000C_program10_interval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program10_interval_getIndexStart, M2000C_program10_interval_getIndexEnd - M2000C_program10_interval_getIndexStart);
            M2000C_program10_interval_amount = Decimal.Parse(M2000C_program10_interval_amount).ToString();//takes out some junk
            M2000C_program10_interval_amount = M2000C_program10_interval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program10_interval_amount = M2000C_program10_interval_amount.Insert(M2000C_program10_interval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program10_interval_amount = Decimal.Parse(M2000C_program10_interval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show("|" + M2000C_program10_interval_amount + "|");//debugging
            //MessageBox.Show(M2000C_program10_interval_amount);//debugging
            try { numericUpDown_M2000C_program10_interval.Value = Decimal.Parse(M2000C_program10_interval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program10_interval.Value = numericUpDown_M2000C_program10_interval.Minimum; }

            int M2000C_program10_cycle_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[10][\"cycle\"]  =") + 24;
            int M2000C_program10_cycle_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program10_cycle_getIndexStart);
            string M2000C_program10_cycle_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program10_cycle_getIndexStart, M2000C_program10_cycle_getIndexEnd - M2000C_program10_cycle_getIndexStart);
            try { numericUpDown_M2000C_program10_cycle.Value = int.Parse(M2000C_program10_cycle_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program10_cycle.Value = numericUpDown_M2000C_program10_cycle.Minimum; }

            //this needs parcing to show the correct data
            int M2000C_program10_cycleInterval_getIndexStart = CountermeasureFileStringText_M2000C.IndexOf("programs[10][\"c_intv\"] =") + 24;
            int M2000C_program10_cycleInterval_getIndexEnd = CountermeasureFileStringText_M2000C.IndexOf("\n", M2000C_program10_cycleInterval_getIndexStart);
            string M2000C_program10_cycleInterval_amount = CountermeasureFileStringText_M2000C.Substring(M2000C_program10_cycleInterval_getIndexStart, M2000C_program10_cycleInterval_getIndexEnd - M2000C_program10_cycleInterval_getIndexStart);
            //M2000C_program10_cycleInterval_amount = M2000C_program10_cycleInterval_amount.Insert(M2000C_program10_cycleInterval_amount.Length - 3, ".");
            //MessageBox.Show(M2000C_program10_cycleInterval_amount);//debugging
            M2000C_program10_cycleInterval_amount = Decimal.Parse(M2000C_program10_cycleInterval_amount).ToString();//takes out some junk
            M2000C_program10_cycleInterval_amount = M2000C_program10_cycleInterval_amount.Insert(0, "00");//puts two zeros in the front to prevent an error when we try to add the decimal in the next step
            M2000C_program10_cycleInterval_amount = M2000C_program10_cycleInterval_amount.Insert(M2000C_program10_cycleInterval_amount.Length - 2, ".");//this converts the number from Razbam format to proper decimal format
            M2000C_program10_cycleInterval_amount = Decimal.Parse(M2000C_program10_cycleInterval_amount).ToString();//takes away any more extra stuff
            //MessageBox.Show(M2000C_program10_cycleInterval_amount);//debugging
            try { numericUpDown_M2000C_program10_cycleInterval.Value = Decimal.Parse(M2000C_program10_cycleInterval_amount); }
            catch (ArgumentOutOfRangeException) { numericUpDown_M2000C_program10_cycleInterval.Value = numericUpDown_M2000C_program10_cycleInterval.Minimum; }

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
            else if (tabControl_mainTab.SelectedTab == tabPage3)
            {
                //MessageBox.Show("You were on tab 3");
                harmForF16cIsNotAvailableMessage();
                //must wait till you know what the lua looks like

                /*
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
                */
            }
            else if (tabControl_mainTab.SelectedTab == tabPage4)//a10c page
            {
                button_export_Click_A10C();
            }
            else if (tabControl_mainTab.SelectedTab == tabPage7)//m2kpage
            {
                button_export_Click_AV8B();
            }
            else if (tabControl_mainTab.SelectedTab == tabPage8)//m2kpage
            {
                button_export_Click_M2000C();
            }
        }

        private void button_export_Click_AV8B()
        {
            //TODO: Write export code here
            //make sure that the special values are accounted for

            string AV8B_ALL_chaffBquantity_exportValue;//have to do this because of the special conditions it can be exported as
            if (checkBox_AV8B_ALL_chaffBquantity_continuous.Checked == true)
            {
                AV8B_ALL_chaffBquantity_exportValue = "-1";
            }
            else if (checkBox_AV8B_ALL_chaffBquantity_random.Checked == true)
            {
                AV8B_ALL_chaffBquantity_exportValue = "-2";
            }
            else
            {
                AV8B_ALL_chaffBquantity_exportValue = numericUpDown_AV8B_ALL_CHAFF_BQTY.Value.ToString();
            }

            string AV8B_ALL_chaffBinterval_exportValue;//have to do this because of the special conditions it can be exported as
            if (checkBox_AV8B_ALL_chaffBinterval_random.Checked == true)
            {
                AV8B_ALL_chaffBinterval_exportValue = "-2";
            }
            else
            {
                AV8B_ALL_chaffBinterval_exportValue = numericUpDown_AV8B_ALL_CHAFF_BINT.Value.ToString();
            }

            string AV8B_chaffBquantity_exportValue;//have to do this because of the special conditions it can be exported as
            if (checkBox_AV8B_chaffBquantity_continuous.Checked == true)
            {
                AV8B_chaffBquantity_exportValue = "-1";
            }
            else if (checkBox_AV8B_chaffBquantity_random.Checked == true)
            {
                AV8B_chaffBquantity_exportValue = "-2";
            }
            else
            {
                AV8B_chaffBquantity_exportValue = numericUpDown_AV8B_CHAFF_BQTY.Value.ToString();
            }

            string AV8B_chaffBinterval_exportValue;//have to do this because of the special conditions it can be exported as
            if (checkBox_AV8B_chaffBinterval_random.Checked == true)
            {
                AV8B_chaffBinterval_exportValue = "-2";
            }
            else
            {
                AV8B_chaffBinterval_exportValue = numericUpDown_AV8B_CHAFF_BINT.Value.ToString();
            }


            string[] luaExportString = {
                "local gettext = require(\"i_18n\")",
                "_ = gettext.translate",
                "",
                "-- Chaff Burst Values",
                "-- BQTY: 1 to 15. Special values: -1 = Continuous (will use ALL chaff); -2 = Random (will dispense between 1 to 6 chaff)",
                "-- BINT: 0.1 to 1.5 seconds. Special values: -2 = Random (will set an interval between 0.1 and 0.4 seconds)",
                "",
                "-- Chaff Salvo Values",
                "-- SQTY: 1 to 15.",
                "-- SINT: 1 to 15 seconds.",
                "",
                "-- Flare Salvo Values",
                "-- SQTY: 1 to 15.",
                "-- SINT: 1 to 15 seconds.",
                "",
                "--All Expendables",
                "EW_ALL_CHAFF_BQTY = " + AV8B_ALL_chaffBquantity_exportValue + ";",
                "EW_ALL_CHAFF_BINT = " + AV8B_ALL_chaffBinterval_exportValue + ";",
                "EW_ALL_CHAFF_SQTY = " + numericUpDown_AV8B_ALL_CHAFF_SQTY.Value + ";",
                "EW_ALL_CHAFF_SINT = " + numericUpDown_AV8B_ALL_CHAFF_SINT.Value + ";",
                "EW_ALL_FLARES_SQTY = " + numericUpDown_AV8B_ALL_FLARES_SQTY.Value + ";",
                "EW_ALL_FLARES_SINT = " + numericUpDown_AV8B_ALL_FLARES_SINT.Value + ";",
                "",
                "--Chaff Only",
                "EW_CHAFF_BQTY = " + AV8B_chaffBquantity_exportValue + ";",
                "EW_CHAFF_BINT = " + AV8B_chaffBinterval_exportValue + ";",
                "EW_CHAFF_SQTY = " + numericUpDown_AV8B_CHAFF_SQTY.Value + ";",
                "EW_CHAFF_SINT = " + numericUpDown_AV8B_CHAFF_SINT.Value + ";",
                "",
                "--Flares Only",
                "EW_FLARES_SQTY = " + numericUpDown_AV8B_FLARES_SQTY.Value + ";",
                "EW_FLARES_SINT = " + numericUpDown_AV8B_FLARES_SINT.Value + ";",
                "",
                "need_to_be_closed = true",
                "",

                 "--Exported via Bailey's CMS Editor on " + System.DateTime.Now};

            if (isExportEnabled == true)
            {
                System.IO.Directory.CreateDirectory(cmdsLua_AV8B_FolderPath);
                System.IO.File.WriteAllLines(cmdsLua_AV8B_fullPath, luaExportString);

                //https://stackoverflow.com/questions/5920882/file-move-does-not-work-file-already-exists
                System.IO.Directory.CreateDirectory(exportPathBackup_AV8B);
                if (File.Exists(exportPathBackup_AV8B + "\\EW_Dispensers_init.lua"))
                {
                    File.Delete(exportPathBackup_AV8B + "\\EW_Dispensers_init.lua");
                }
                System.IO.File.WriteAllLines(exportPathBackup_AV8B + "\\EW_Dispensers_init.lua", luaExportString);
                File.Move(exportPathBackup_AV8B + "\\EW_Dispensers_init.lua", Path.ChangeExtension(exportPathBackup_AV8B + "\\EW_Dispensers_init.lua", ".lua"));


                MessageBox.Show("Your AV8B CMDS file was exported to \r\n" + cmdsLua_AV8B_fullPath + "\r\n\r\n"
                    + "Your AV8B CMDS backup file was exported to \r\n" + exportPathBackup_AV8B + "\\SPIRALE.lua");
            }
            else
            {
                MessageBox.Show("Please select your DCS.exe Location");
            }
        }

        private void button_export_Click_M2000C()
        {
            //https://www.geeksforgeeks.org/c-sharp-how-to-use-strings-in-switch-statement/
            //https://stackoverflow.com/questions/848472/how-add-or-in-switch-statements
            //switch statements are necessarry because what the user sees on the GUI is not what we can put into the .lua export
            /*
            string M2000C_program1_interval = numericUpDown_M2000C_program1_interval.Value.ToString();
            switch (M2000C_program1_interval)
            {
                case "0.00":
                    M2000C_program1_interval = "0";
                    break;
                case "0.25":
                    M2000C_program1_interval = "25";
                    break;
                case "0.50":
                    M2000C_program1_interval = "50";
                    break;
                case "0.75":
                    M2000C_program1_interval = "75";
                    break;
                default:
                    M2000C_program1_interval = "0";
                    break;
            }

            string M2000C_program1_cycleInterval = numericUpDown_M2000C_program1_cycleInterval.Value.ToString();
            switch (M2000C_program1_cycleInterval)
            {
                case "0.00":
                    M2000C_program1_cycleInterval = "0";
                    break;
                case "1.00":
                case "1":
                    M2000C_program1_cycleInterval = "100";
                    break;
                case "2.00":
                case "2":
                    M2000C_program1_cycleInterval = "200";
                    break;
                default:
                    M2000C_program1_cycleInterval = "0";
                    break;
            }


            string M2000C_program2_interval = numericUpDown_M2000C_program2_interval.Value.ToString();
            switch (M2000C_program2_interval)
            {
                case "0.00":
                    M2000C_program2_interval = "0";
                    break;
                case "0.25":
                    M2000C_program2_interval = "25";
                    break;
                case "0.50":
                    M2000C_program2_interval = "50";
                    break;
                case "0.75":
                    M2000C_program2_interval = "75";
                    break;
                default:
                    M2000C_program2_interval = "0";
                    break;
            }

            string M2000C_program2_cycleInterval = numericUpDown_M2000C_program2_cycleInterval.Value.ToString();
            switch (M2000C_program2_cycleInterval)
            {
                case "0.00":
                    M2000C_program2_cycleInterval = "0";
                    break;
                case "1.00":
                case "1":
                    M2000C_program2_cycleInterval = "100";
                    break;
                case "2.00":
                case "2":
                    M2000C_program2_cycleInterval = "200";
                    break;               
                default:
                    M2000C_program2_cycleInterval = "0";
                    break;
            }
            string M2000C_program3_interval = numericUpDown_M2000C_program3_interval.Value.ToString();
            switch (M2000C_program3_interval)
            {
                case "0.00":
                    M2000C_program3_interval = "0";
                    break;
                case "0.25":
                    M2000C_program3_interval = "25";
                    break;
                case "0.50":
                    M2000C_program3_interval = "50";
                    break;
                case "0.75":
                    M2000C_program3_interval = "75";
                    break;
                default:
                    M2000C_program3_interval = "0";
                    break;
            }

            string M2000C_program3_cycleInterval = numericUpDown_M2000C_program3_cycleInterval.Value.ToString();
            switch (M2000C_program3_cycleInterval)
            {
                case "0.00":
                    M2000C_program3_cycleInterval = "0";
                    break;
                case "1.00":
                case "1":
                    M2000C_program3_cycleInterval = "100";
                    break;
                case "2.00":
                case "2":
                    M2000C_program3_cycleInterval = "200";
                    break;
                default:
                    M2000C_program3_cycleInterval = "0";
                    break;
            }



            string M2000C_program4_interval = numericUpDown_M2000C_program4_interval.Value.ToString();
            switch (M2000C_program4_interval)
            {
                case "0.00":
                    M2000C_program4_interval = "0";
                    break;
                case "0.25":
                    M2000C_program4_interval = "25";
                    break;
                case "0.50":
                    M2000C_program4_interval = "50";
                    break;
                case "0.75":
                    M2000C_program4_interval = "75";
                    break;
                default:
                    M2000C_program4_interval = "0";
                    break;
            }

            string M2000C_program4_cycleInterval = numericUpDown_M2000C_program4_cycleInterval.Value.ToString();
            switch (M2000C_program4_cycleInterval)
            {
                case "0.00":
                    M2000C_program4_cycleInterval = "0";
                    break;
                case "1.00":
                case "1":
                    M2000C_program4_cycleInterval = "100";
                    break;
                case "2.00":
                case "2":
                    M2000C_program4_cycleInterval = "200";
                    break;
                default:
                    M2000C_program4_cycleInterval = "0";
                    break;
            }



            string M2000C_program5_interval = numericUpDown_M2000C_program5_interval.Value.ToString();
            switch (M2000C_program5_interval)
            {
                case "0.00":
                    M2000C_program5_interval = "0";
                    break;
                case "0.25":
                    M2000C_program5_interval = "25";
                    break;
                case "0.50":
                    M2000C_program5_interval = "50";
                    break;
                case "0.75":
                    M2000C_program5_interval = "75";
                    break;
                default:
                    M2000C_program5_interval = "0";
                    break;
            }

            string M2000C_program5_cycleInterval = numericUpDown_M2000C_program5_cycleInterval.Value.ToString();
            switch (M2000C_program5_cycleInterval)
            {
                case "0.00":
                    M2000C_program5_cycleInterval = "0";
                    break;
                case "1.00":
                case "1":
                    M2000C_program5_cycleInterval = "100";
                    break;
                case "2.00":
                case "2":
                    M2000C_program5_cycleInterval = "200";
                    break;
                default:
                    M2000C_program5_cycleInterval = "0";
                    break;
            }



            string M2000C_program6_interval = numericUpDown_M2000C_program6_interval.Value.ToString();
            switch (M2000C_program6_interval)
            {
                case "0.00":
                    M2000C_program6_interval = "0";
                    break;
                case "0.25":
                    M2000C_program6_interval = "25";
                    break;
                case "0.50":
                    M2000C_program6_interval = "50";
                    break;
                case "0.75":
                    M2000C_program6_interval = "75";
                    break;
                default:
                    M2000C_program6_interval = "0";
                    break;
            }

            string M2000C_program6_cycleInterval = numericUpDown_M2000C_program6_cycleInterval.Value.ToString();
            switch (M2000C_program6_cycleInterval)
            {
                case "0.00":
                    M2000C_program6_cycleInterval = "0";
                    break;
                case "1.00":
                case "1":
                    M2000C_program6_cycleInterval = "100";
                    break;
                case "2.00":
                case "2":
                    M2000C_program6_cycleInterval = "200";
                    break;
                default:
                    M2000C_program6_cycleInterval = "0";
                    break;
            }
            string M2000C_program7_interval = numericUpDown_M2000C_program7_interval.Value.ToString();
            switch (M2000C_program7_interval)
            {
                case "0.00":
                    M2000C_program7_interval = "0";
                    break;
                case "0.25":
                    M2000C_program7_interval = "25";
                    break;
                case "0.50":
                    M2000C_program7_interval = "50";
                    break;
                case "0.75":
                    M2000C_program7_interval = "75";
                    break;
                default:
                    M2000C_program7_interval = "0";
                    break;
            }

            string M2000C_program7_cycleInterval = numericUpDown_M2000C_program7_cycleInterval.Value.ToString();
            switch (M2000C_program7_cycleInterval)
            {
                case "0.00":
                    M2000C_program7_cycleInterval = "0";
                    break;
                case "1.00":
                case "1":
                    M2000C_program7_cycleInterval = "100";
                    break;
                case "2.00":
                case "2":
                    M2000C_program7_cycleInterval = "200";
                    break;
                default:
                    M2000C_program7_cycleInterval = "0";
                    break;
            }



            string M2000C_program8_interval = numericUpDown_M2000C_program8_interval.Value.ToString();
            switch (M2000C_program8_interval)
            {
                case "0.00":
                    M2000C_program8_interval = "0";
                    break;
                case "0.25":
                    M2000C_program8_interval = "25";
                    break;
                case "0.50":
                    M2000C_program8_interval = "50";
                    break;
                case "0.75":
                    M2000C_program8_interval = "75";
                    break;
                default:
                    M2000C_program8_interval = "0";
                    break;
            }

            string M2000C_program8_cycleInterval = numericUpDown_M2000C_program8_cycleInterval.Value.ToString();
            switch (M2000C_program8_cycleInterval)
            {
                case "0.00":
                    M2000C_program8_cycleInterval = "0";
                    break;
                case "1.00":
                case "1":
                    M2000C_program8_cycleInterval = "100";
                    break;
                case "2.00":
                case "2":
                    M2000C_program8_cycleInterval = "200";
                    break;
                default:
                    M2000C_program8_cycleInterval = "0";
                    break;
            }



            string M2000C_program9_interval = numericUpDown_M2000C_program9_interval.Value.ToString();
            switch (M2000C_program9_interval)
            {
                case "0.00":
                    M2000C_program9_interval = "0";
                    break;
                case "0.25":
                    M2000C_program9_interval = "25";
                    break;
                case "0.50":
                    M2000C_program9_interval = "50";
                    break;
                case "0.75":
                    M2000C_program9_interval = "75";
                    break;
                default:
                    M2000C_program9_interval = "0";
                    break;
            }

            string M2000C_program9_cycleInterval = numericUpDown_M2000C_program9_cycleInterval.Value.ToString();
            switch (M2000C_program9_cycleInterval)
            {
                case "0.00":
                    M2000C_program9_cycleInterval = "0";
                    break;
                case "1.00":
                case "1":
                    M2000C_program9_cycleInterval = "100";
                    break;
                case "2.00":
                case "2":
                    M2000C_program9_cycleInterval = "200";
                    break;
                default:
                    M2000C_program9_cycleInterval = "0";
                    break;
            }


            string M2000C_program10_interval = numericUpDown_M2000C_program10_interval.Value.ToString();
            switch (M2000C_program10_interval)
            {
                case "0.00":
                    M2000C_program10_interval = "0";
                    break;
                case "0.25":
                    M2000C_program10_interval = "25";
                    break;
                case "0.50":
                    M2000C_program10_interval = "50";
                    break;
                case "0.75":
                    M2000C_program10_interval = "75";
                    break;
                default:
                    M2000C_program10_interval = "0";
                    break;
            }

            string M2000C_program10_cycleInterval = numericUpDown_M2000C_program10_cycleInterval.Value.ToString();
            switch (M2000C_program10_cycleInterval)
            {
                case "0.00":
                    M2000C_program10_cycleInterval = "0";
                    break;
                case "1.00":
                case "1":
                    M2000C_program10_cycleInterval = "100";
                    break;
                case "2.00":
                case "2":
                    M2000C_program10_cycleInterval = "200";
                    break;
                default:
                    M2000C_program10_cycleInterval = "0";
                    break;
            }
            */
            //https://stackoverflow.com/questions/13769938/decimal-data-type-is-stripping-trailing-zeros-when-they-are-needed-to-display
            //https://stackoverflow.com/questions/10298940/remove-dot-character-from-a-string-c-sharp
            //BUG: trivial. exports 0.00 as '00' in the countermeasure .lua. still works though
            string M2000C_program1_interval = numericUpDown_M2000C_program1_interval.Value.ToString(".00");
            M2000C_program1_interval = M2000C_program1_interval.Replace(".", string.Empty);
            string M2000C_program1_cycleInterval = numericUpDown_M2000C_program1_cycleInterval.Value.ToString("0.00");
            M2000C_program1_cycleInterval = M2000C_program1_cycleInterval.Replace(".", string.Empty);
            //MessageBox.Show(M2000C_program1_cycleInterval);

            //decimal M2000C_program1_interval_dec = numericUpDown_M2000C_program1_interval.Value;
            //decimal M2000C_program1_cycleInterval_dec = numericUpDown_M2000C_program1_cycleInterval.Value;
            //MessageBox.Show(M2000C_program1_cycleInterval_dec.ToString());

            
           
            //M2000C_program1_interval = M2000C_program1_interval.Substring(2);
            //M2000C_program1_cycleInterval = M2000C_program1_cycleInterval.Substring(2);
            //MessageBox.Show(M2000C_program1_cycleInterval);

            //TODO: this is broken
            //MessageBox.Show(M2000C_program1_cycleInterval);





            string M2000C_program2_interval = numericUpDown_M2000C_program2_interval.Value.ToString(".00");
            M2000C_program2_interval = M2000C_program2_interval.Replace(".", string.Empty);
            string M2000C_program2_cycleInterval = numericUpDown_M2000C_program2_cycleInterval.Value.ToString("0.00");
            M2000C_program2_cycleInterval = M2000C_program2_cycleInterval.Replace(".", string.Empty);



            string M2000C_program3_interval = numericUpDown_M2000C_program3_interval.Value.ToString(".00");
            M2000C_program3_interval = M2000C_program3_interval.Replace(".", string.Empty);
            string M2000C_program3_cycleInterval = numericUpDown_M2000C_program3_cycleInterval.Value.ToString("0.00");
            M2000C_program3_cycleInterval = M2000C_program3_cycleInterval.Replace(".", string.Empty);



            string M2000C_program4_interval = numericUpDown_M2000C_program4_interval.Value.ToString(".00");
            M2000C_program4_interval = M2000C_program4_interval.Replace(".", string.Empty);
            string M2000C_program4_cycleInterval = numericUpDown_M2000C_program4_cycleInterval.Value.ToString("0.00");
            M2000C_program4_cycleInterval = M2000C_program4_cycleInterval.Replace(".", string.Empty);

            string M2000C_program5_interval = numericUpDown_M2000C_program5_interval.Value.ToString(".00");
            M2000C_program5_interval = M2000C_program5_interval.Replace(".", string.Empty);
            string M2000C_program5_cycleInterval = numericUpDown_M2000C_program5_cycleInterval.Value.ToString("0.00");
            M2000C_program5_cycleInterval = M2000C_program5_cycleInterval.Replace(".", string.Empty);

            string M2000C_program6_interval = numericUpDown_M2000C_program6_interval.Value.ToString(".00");
            M2000C_program6_interval = M2000C_program6_interval.Replace(".", string.Empty);
            string M2000C_program6_cycleInterval = numericUpDown_M2000C_program6_cycleInterval.Value.ToString("0.00");
            M2000C_program6_cycleInterval = M2000C_program6_cycleInterval.Replace(".", string.Empty);

            string M2000C_program7_interval = numericUpDown_M2000C_program7_interval.Value.ToString(".00");
            M2000C_program7_interval = M2000C_program7_interval.Replace(".", string.Empty);
            string M2000C_program7_cycleInterval = numericUpDown_M2000C_program7_cycleInterval.Value.ToString("0.00");
            M2000C_program7_cycleInterval = M2000C_program7_cycleInterval.Replace(".", string.Empty);

            string M2000C_program8_interval = numericUpDown_M2000C_program8_interval.Value.ToString(".00");
            M2000C_program8_interval = M2000C_program8_interval.Replace(".", string.Empty);
            string M2000C_program8_cycleInterval = numericUpDown_M2000C_program8_cycleInterval.Value.ToString("0.00");
            M2000C_program8_cycleInterval = M2000C_program8_cycleInterval.Replace(".", string.Empty);

            string M2000C_program9_interval = numericUpDown_M2000C_program9_interval.Value.ToString(".00");
            M2000C_program9_interval = M2000C_program9_interval.Replace(".", string.Empty);
            string M2000C_program9_cycleInterval = numericUpDown_M2000C_program9_cycleInterval.Value.ToString("0.00");
            M2000C_program9_cycleInterval = M2000C_program9_cycleInterval.Replace(".", string.Empty);

            string M2000C_program10_interval = numericUpDown_M2000C_program10_interval.Value.ToString(".00");
            M2000C_program10_interval = M2000C_program10_interval.Replace(".", string.Empty);
            string M2000C_program10_cycleInterval = numericUpDown_M2000C_program10_cycleInterval.Value.ToString("0.00");
            M2000C_program10_cycleInterval = M2000C_program10_cycleInterval.Replace(".", string.Empty);

         


            string[] luaExportString = {
                "local gettext = require(\"i_18n\")",
                "_ = gettext.translate",
                "",
                "programs = {}",
                "",
                "-- User Modifiable program",
                "programs[1] = {}",
                "programs[1][\"chaff\"]  = " + numericUpDown_M2000C_program1_chaff.Value,
                "programs[1][\"flare\"]  = " + numericUpDown_M2000C_program1_flare.Value,
                "programs[1][\"intv\"]   = " + M2000C_program1_interval,
                "programs[1][\"cycle\"]  = " + numericUpDown_M2000C_program1_cycle.Value,
                "programs[1][\"c_intv\"] = " + M2000C_program1_cycleInterval,
                "programs[1][\"panic\"]  = 0",//dont change panic bc idk what it actually does yet
                "",
                "programs[2] = {}",
                "programs[2][\"chaff\"]  = " + numericUpDown_M2000C_program2_chaff.Value,
                "programs[2][\"flare\"]  = " + numericUpDown_M2000C_program2_flare.Value,
                "programs[2][\"intv\"]   = " + M2000C_program2_interval,
                "programs[2][\"cycle\"]  = " + numericUpDown_M2000C_program2_cycle.Value,
                "programs[2][\"c_intv\"] = " + M2000C_program2_cycleInterval,
                "programs[2][\"panic\"]  = 0",
                "",
                "programs[3] = {}",
                "programs[3][\"chaff\"]  = " + numericUpDown_M2000C_program3_chaff.Value,
                "programs[3][\"flare\"]  = " + numericUpDown_M2000C_program3_flare.Value,
                "programs[3][\"intv\"]   = " + M2000C_program3_interval,
                "programs[3][\"cycle\"]  = " + numericUpDown_M2000C_program3_cycle.Value,
                "programs[3][\"c_intv\"] = " + M2000C_program3_cycleInterval,
                "programs[3][\"panic\"]  = 0",
                "",
                "programs[4] = {}",
                "programs[4][\"chaff\"]  = " + numericUpDown_M2000C_program4_chaff.Value,
                "programs[4][\"flare\"]  = " + numericUpDown_M2000C_program4_flare.Value,
                "programs[4][\"intv\"]   = " + M2000C_program4_interval,
                "programs[4][\"cycle\"]  = " + numericUpDown_M2000C_program4_cycle.Value,
                "programs[4][\"c_intv\"] = " + M2000C_program4_cycleInterval,
                "programs[4][\"panic\"]  = 0",
                "",
                "programs[5] = {}",
                "programs[5][\"chaff\"]  = " + numericUpDown_M2000C_program5_chaff.Value,
                "programs[5][\"flare\"]  = " + numericUpDown_M2000C_program5_flare.Value,
                "programs[5][\"intv\"]   = " + M2000C_program5_interval,
                "programs[5][\"cycle\"]  = " + numericUpDown_M2000C_program5_cycle.Value,
                "programs[5][\"c_intv\"] = " + M2000C_program5_cycleInterval,
                "programs[5][\"panic\"]  = 0",
                "",
                "programs[6] = {}",
                "programs[6][\"chaff\"]  = " + numericUpDown_M2000C_program6_chaff.Value,
                "programs[6][\"flare\"]  = " + numericUpDown_M2000C_program6_flare.Value,
                "programs[6][\"intv\"]   = " + M2000C_program6_interval,
                "programs[6][\"cycle\"]  = " + numericUpDown_M2000C_program6_cycle.Value,
                "programs[6][\"c_intv\"] = " + M2000C_program6_cycleInterval,
                "programs[6][\"panic\"]  = 0",
                "",
                "programs[7] = {}",
                "programs[7][\"chaff\"]  = " + numericUpDown_M2000C_program7_chaff.Value,
                "programs[7][\"flare\"]  = " + numericUpDown_M2000C_program7_flare.Value,
                "programs[7][\"intv\"]   = " + M2000C_program7_interval,
                "programs[7][\"cycle\"]  = " + numericUpDown_M2000C_program7_cycle.Value,
                "programs[7][\"c_intv\"] = " + M2000C_program7_cycleInterval,
                "programs[7][\"panic\"]  = 0",
                "",
                "programs[8] = {}",
                "programs[8][\"chaff\"]  = " + numericUpDown_M2000C_program8_chaff.Value,
                "programs[8][\"flare\"]  = " + numericUpDown_M2000C_program8_flare.Value,
                "programs[8][\"intv\"]   = " + M2000C_program8_interval,
                "programs[8][\"cycle\"]  = " + numericUpDown_M2000C_program8_cycle.Value,
                "programs[8][\"c_intv\"] = " + M2000C_program8_cycleInterval,
                "programs[8][\"panic\"]  = 0",
                "",
                "programs[9] = {}",
                "programs[9][\"chaff\"]  = " + numericUpDown_M2000C_program9_chaff.Value,
                "programs[9][\"flare\"]  = " + numericUpDown_M2000C_program9_flare.Value,
                "programs[9][\"intv\"]   = " + M2000C_program9_interval,
                "programs[9][\"cycle\"]  = " + numericUpDown_M2000C_program9_cycle.Value,
                "programs[9][\"c_intv\"] = " + M2000C_program9_cycleInterval,
                "programs[9][\"panic\"]  = 0",
                "",
                "programs[10] = {}",
                "programs[10][\"chaff\"]  = " + numericUpDown_M2000C_program10_chaff.Value,
                "programs[10][\"flare\"]  = " + numericUpDown_M2000C_program10_flare.Value,
                "programs[10][\"intv\"]   = " + M2000C_program10_interval,
                "programs[10][\"cycle\"]  = " + numericUpDown_M2000C_program10_cycle.Value,
                "programs[10][\"c_intv\"] = " + M2000C_program10_cycleInterval,
                "programs[10][\"panic\"]  = 0",
                "",
                "need_to_be_closed = true",
                "",
                "--Exported via Bailey's CMS Editor on " + System.DateTime.Now};

           
            if (isExportEnabled == true)
            {
                System.IO.Directory.CreateDirectory(cmdsLua_M2000C_FolderPath);
                System.IO.File.WriteAllLines(cmdsLua_M2000C_fullPath, luaExportString);

                //https://stackoverflow.com/questions/5920882/file-move-does-not-work-file-already-exists
                System.IO.Directory.CreateDirectory(exportPathBackup_M2000C);
                if (File.Exists(exportPathBackup_M2000C + "\\SPIRALE.lua"))
                {
                    File.Delete(exportPathBackup_M2000C + "\\SPIRALE.lua");
                }
                System.IO.File.WriteAllLines(exportPathBackup_M2000C + "\\SPIRALE.lua", luaExportString);
                File.Move(exportPathBackup_M2000C + "\\SPIRALE.lua", Path.ChangeExtension(exportPathBackup_M2000C + "\\SPIRALE.lua", ".lua"));


                MessageBox.Show("Your M2000C CMDS file was exported to \r\n" + cmdsLua_M2000C_fullPath + "\r\n\r\n"
                    + "Your M2000C CMDS backup file was exported to \r\n" + exportPathBackup_M2000C + "\\SPIRALE.lua");
            }
            else
            {
                MessageBox.Show("Please select your DCS.exe Location");
            }

        }

        private void button_export_Click_A10C()//this contains the code for the formated lua that the program will spit out for DCS
        {
           
            string[] luaExportString = {
            "local gettext = require(\"i_18n\")",
            "_ = gettext.translate",
            "",
            "programs = {}",
            "",
            "-- Old generation radar SAM",
            "programs['A'] = {}",
            "programs['A'][\"chaff\"] = " + numericUpDown_A10C_programA_chaff.Value ,
            "programs['A'][\"flare\"] = " + numericUpDown_A10C_programA_flare.Value ,
            "programs['A'][\"intv\"]  = " + numericUpDown_A10C_programA_interval.Value ,
            "programs['A'][\"cycle\"] = " + numericUpDown_A10C_programA_cycle.Value ,
            "",
            "-- Current generation radar SAM",
            "programs['B'] = {}",
            "programs['B'][\"chaff\"] = " + numericUpDown_A10C_programB_chaff.Value ,
            "programs['B'][\"flare\"] = " + numericUpDown_A10C_programB_flare.Value ,
            "programs['B'][\"intv\"]  = " + numericUpDown_A10C_programB_interval.Value ,
            "programs['B'][\"cycle\"] = " + numericUpDown_A10C_programB_cycle.Value ,
            "",
            "-- IR SAM",
            "programs['C'] = {}",
            "programs['C'][\"chaff\"] = " + numericUpDown_A10C_programC_chaff.Value ,
            "programs['C'][\"flare\"] = " + numericUpDown_A10C_programC_flare.Value ,
            "programs['C'][\"intv\"]  = " + numericUpDown_A10C_programC_interval.Value ,
            "programs['C'][\"cycle\"] = " + numericUpDown_A10C_programC_cycle.Value ,
            "",
            "-- Default manual presets",
            "-- Mix 1",
            "programs['D'] = {}",
            "programs['D'][\"chaff\"] = " + numericUpDown_A10C_programD_chaff.Value ,
            "programs['D'][\"flare\"] = " + numericUpDown_A10C_programD_flare.Value ,
            "programs['D'][\"intv\"]  = " + numericUpDown_A10C_programD_interval.Value ,
            "programs['D'][\"cycle\"] = " + numericUpDown_A10C_programD_cycle.Value ,
            "",
            "-- Mix 2",
            "programs['E'] = {}",
            "programs['E'][\"chaff\"] = " + numericUpDown_A10C_programE_chaff.Value ,
            "programs['E'][\"flare\"] = " + numericUpDown_A10C_programE_flare.Value ,
            "programs['E'][\"intv\"]  = " + numericUpDown_A10C_programE_interval.Value ,
            "programs['E'][\"cycle\"] = " + numericUpDown_A10C_programE_cycle.Value ,
            "",
            "-- Mix 3",
            "programs['F'] = {}",
            "programs['F'][\"chaff\"] = " + numericUpDown_A10C_programF_chaff.Value ,
            "programs['F'][\"flare\"] = " + numericUpDown_A10C_programF_flare.Value ,
            "programs['F'][\"intv\"]  = " + numericUpDown_A10C_programF_interval.Value ,
            "programs['F'][\"cycle\"] = " + numericUpDown_A10C_programF_cycle.Value ,
            "",
            "-- Mix 4",
            "programs['G'] = {}",
            "programs['G'][\"chaff\"] = " + numericUpDown_A10C_programG_chaff.Value ,
            "programs['G'][\"flare\"] = " + numericUpDown_A10C_programG_flare.Value ,
            "programs['G'][\"intv\"]  = " + numericUpDown_A10C_programG_interval.Value ,
            "programs['G'][\"cycle\"] = " + numericUpDown_A10C_programG_cycle.Value ,
            "",
            "-- Chaff single",
            "programs['H'] = {}",
            "programs['H'][\"chaff\"] = " + numericUpDown_A10C_programH_chaff.Value ,
            "programs['H'][\"flare\"] = " + numericUpDown_A10C_programH_flare.Value ,
            "programs['H'][\"intv\"]  = " + numericUpDown_A10C_programH_interval.Value ,
            "programs['H'][\"cycle\"] = " + numericUpDown_A10C_programH_cycle.Value ,
            "",
            "-- Chaff pair",
            "programs['I'] = {}",
            "programs['I'][\"chaff\"] = " + numericUpDown_A10C_programI_chaff.Value ,
            "programs['I'][\"flare\"] = " + numericUpDown_A10C_programI_flare.Value ,
            "programs['I'][\"intv\"]  = " + numericUpDown_A10C_programI_interval.Value ,
            "programs['I'][\"cycle\"] = " + numericUpDown_A10C_programI_cycle.Value ,
            "",
            "-- Flare single",
            "programs['J'] = {}",
            "programs['J'][\"chaff\"] = " + numericUpDown_A10C_programJ_chaff.Value ,
            "programs['J'][\"flare\"] = " + numericUpDown_A10C_programJ_flare.Value ,
            "programs['J'][\"intv\"]  = " + numericUpDown_A10C_programJ_interval.Value ,
            "programs['J'][\"cycle\"] = " + numericUpDown_A10C_programJ_cycle.Value ,
            "",
            "-- Flare pair",
            "programs['K'] = {}",
            "programs['K'][\"chaff\"] = " + numericUpDown_A10C_programK_chaff.Value ,
            "programs['K'][\"flare\"] = " + numericUpDown_A10C_programK_flare.Value ,
            "programs['K'][\"intv\"]  = " + numericUpDown_A10C_programK_interval.Value ,
            "programs['K'][\"cycle\"] = " + numericUpDown_A10C_programK_cycle.Value ,
            "",
            "-- Chaff pre-empt",
            "programs['L'] = {}",
            "programs['L'][\"chaff\"] = " + numericUpDown_A10C_programL_chaff.Value ,
            "programs['L'][\"flare\"] = " + numericUpDown_A10C_programL_flare.Value ,
            "programs['L'][\"intv\"]  = " + numericUpDown_A10C_programL_interval.Value ,
            "programs['L'][\"cycle\"] = " + numericUpDown_A10C_programL_cycle.Value ,
            "",
            "-- Flare pre-empt",
            "programs['M'] = {}",
            "programs['M'][\"chaff\"] = " + numericUpDown_A10C_programM_chaff.Value ,
            "programs['M'][\"flare\"] = " + numericUpDown_A10C_programM_flare.Value ,
            "programs['M'][\"intv\"]  = " + numericUpDown_A10C_programM_interval.Value ,
            "programs['M'][\"cycle\"] = " + numericUpDown_A10C_programM_cycle.Value ,
            "",
            "--" + textBox_programN.Text,
            "programs['N'] = {}",
            "programs['N'][\"chaff\"] = " + numericUpDown_A10C_programN_chaff.Value ,
            "programs['N'][\"flare\"] = " + numericUpDown_A10C_programN_flare.Value ,
            "programs['N'][\"intv\"]  = " + numericUpDown_A10C_programN_interval.Value ,
            "programs['N'][\"cycle\"] = " + numericUpDown_A10C_programN_cycle.Value ,
            "",
            "--" + textBox_programO.Text,
            "programs['O'] = {}",
            "programs['O'][\"chaff\"] = " + numericUpDown_A10C_programO_chaff.Value ,
            "programs['O'][\"flare\"] = " + numericUpDown_A10C_programO_flare.Value ,
            "programs['O'][\"intv\"]  = " + numericUpDown_A10C_programO_interval.Value ,
            "programs['O'][\"cycle\"] = " + numericUpDown_A10C_programO_cycle.Value ,
            "",
            "--" + textBox_programP.Text,
            "programs['P'] = {}",
            "programs['P'][\"chaff\"] = " + numericUpDown_A10C_programP_chaff.Value ,
            "programs['P'][\"flare\"] = " + numericUpDown_A10C_programP_flare.Value ,
            "programs['P'][\"intv\"]  = " + numericUpDown_A10C_programP_interval.Value ,
            "programs['P'][\"cycle\"] = " + numericUpDown_A10C_programP_cycle.Value ,
            "",
            "--" + textBox_programQ.Text,
            "programs['Q'] = {}",
            "programs['Q'][\"chaff\"] = " + numericUpDown_A10C_programQ_chaff.Value ,
            "programs['Q'][\"flare\"] = " + numericUpDown_A10C_programQ_flare.Value ,
            "programs['Q'][\"intv\"]  = " + numericUpDown_A10C_programQ_interval.Value ,
            "programs['Q'][\"cycle\"] = " + numericUpDown_A10C_programQ_cycle.Value ,
            "",
            "--" + textBox_programR.Text,
            "programs['R'] = {}",
            "programs['R'][\"chaff\"] = " + numericUpDown_A10C_programR_chaff.Value ,
            "programs['R'][\"flare\"] = " + numericUpDown_A10C_programR_flare.Value ,
            "programs['R'][\"intv\"]  = " + numericUpDown_A10C_programR_interval.Value ,
            "programs['R'][\"cycle\"] = " + numericUpDown_A10C_programR_cycle.Value ,
            "",
            "--" + textBox_programS.Text,
            "programs['S'] = {}",
            "programs['S'][\"chaff\"] = " + numericUpDown_A10C_programS_chaff.Value ,
            "programs['S'][\"flare\"] = " + numericUpDown_A10C_programS_flare.Value ,
            "programs['S'][\"intv\"]  = " + numericUpDown_A10C_programS_interval.Value ,
            "programs['S'][\"cycle\"] = " + numericUpDown_A10C_programS_cycle.Value ,
            "",
            "--" + textBox_programT.Text,
            "programs['T'] = {}",
            "programs['T'][\"chaff\"] = " + numericUpDown_A10C_programT_chaff.Value ,
            "programs['T'][\"flare\"] = " + numericUpDown_A10C_programT_flare.Value ,
            "programs['T'][\"intv\"]  = " + numericUpDown_A10C_programT_interval.Value ,
            "programs['T'][\"cycle\"] = " + numericUpDown_A10C_programT_cycle.Value ,
            "",
            "--" + textBox_programU.Text,
            "programs['U'] = {}",
            "programs['U'][\"chaff\"] = " + numericUpDown_A10C_programU_chaff.Value ,
            "programs['U'][\"flare\"] = " + numericUpDown_A10C_programU_flare.Value ,
            "programs['U'][\"intv\"]  = " + numericUpDown_A10C_programU_interval.Value ,
            "programs['U'][\"cycle\"] = " + numericUpDown_A10C_programU_cycle.Value ,
            "",
            "--" + textBox_programV.Text,
            "programs['V'] = {}",
            "programs['V'][\"chaff\"] = " + numericUpDown_A10C_programV_chaff.Value ,
            "programs['V'][\"flare\"] = " + numericUpDown_A10C_programV_flare.Value ,
            "programs['V'][\"intv\"]  = " + numericUpDown_A10C_programV_interval.Value ,
            "programs['V'][\"cycle\"] = " + numericUpDown_A10C_programV_cycle.Value ,
            "",
            "--" + textBox_programW.Text,
            "programs['W'] = {}",
            "programs['W'][\"chaff\"] = " + numericUpDown_A10C_programW_chaff.Value ,
            "programs['W'][\"flare\"] = " + numericUpDown_A10C_programW_flare.Value ,
            "programs['W'][\"intv\"]  = " + numericUpDown_A10C_programW_interval.Value ,
            "programs['W'][\"cycle\"] = " + numericUpDown_A10C_programW_cycle.Value ,
            "",
            "--" + textBox_programX.Text,
            "programs['X'] = {}",
            "programs['X'][\"chaff\"] = " + numericUpDown_A10C_programX_chaff.Value ,
            "programs['X'][\"flare\"] = " + numericUpDown_A10C_programX_flare.Value ,
            "programs['X'][\"intv\"]  = " + numericUpDown_A10C_programX_interval.Value ,
            "programs['X'][\"cycle\"] = " + numericUpDown_A10C_programX_cycle.Value ,
            "",
            "--" + textBox_programY.Text,
            "programs['Y'] = {}",
            "programs['Y'][\"chaff\"] = " + numericUpDown_A10C_programY_chaff.Value ,
            "programs['Y'][\"flare\"] = " + numericUpDown_A10C_programY_flare.Value ,
            "programs['Y'][\"intv\"]  = " + numericUpDown_A10C_programY_interval.Value ,
            "programs['Y'][\"cycle\"] = " + numericUpDown_A10C_programY_cycle.Value ,
            "",
            "--" + textBox_programZ.Text,
            "programs['Z'] = {}",
            "programs['Z'][\"chaff\"] = " + numericUpDown_A10C_programZ_chaff.Value ,
            "programs['Z'][\"flare\"] = " + numericUpDown_A10C_programZ_flare.Value ,
            "programs['Z'][\"intv\"]  = " + numericUpDown_A10C_programZ_interval.Value ,
            "programs['Z'][\"cycle\"] = " + numericUpDown_A10C_programZ_cycle.Value ,
            "",
            "",
            "ContainerChaffCapacity = 120",
            "",
            "ContainerFlareCapacity = 60",
            "",
            "NumberOfContiners      = 4",
            "",
            "AN_ALE_40V_FAILURE_TOTAL = 0",
            "AN_ALE_40V_FAILURE_CONTAINER_LEFT_WING	= 1",
            "AN_ALE_40V_FAILURE_CONTAINER_LEFT_GEAR	= 2",
            "AN_ALE_40V_FAILURE_CONTAINER_RIGHT_GEAR	= 3",
            "AN_ALE_40V_FAILURE_CONTAINER_RIGHT_WING	= 4",
            "",
            "Damage = {	{Failure = AN_ALE_40V_FAILURE_TOTAL, Failure_name = \"AN_ALE_40V_FAILURE_TOTAL\", Failure_editor_name = _(\"AN/ALE-40(V) total failure\"),  Element = 10, Integrity_Treshold = 0.5, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
            "		{Failure = AN_ALE_40V_FAILURE_CONTAINER_LEFT_WING, Failure_name = \"AN_ALE_40V_FAILURE_CONTAINER_LEFT_WING\", Failure_editor_name = _(\"AN/ALE-40(V) left wing container failure\"),  Element = 23, Integrity_Treshold = 0.75, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
            "		{Failure = AN_ALE_40V_FAILURE_CONTAINER_LEFT_GEAR, Failure_name = \"AN_ALE_40V_FAILURE_CONTAINER_LEFT_GEAR\", Failure_editor_name = _(\"AN/ALE-40(V) left gear container failure\"),  Element = 15, Integrity_Treshold = 0.75, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
            "		{Failure = AN_ALE_40V_FAILURE_CONTAINER_RIGHT_GEAR, Failure_name = \"AN_ALE_40V_FAILURE_CONTAINER_RIGHT_GEAR\", Failure_editor_name = _(\"AN/ALE-40(V) right gear container failure\"),  Element = 16, Integrity_Treshold = 0.75, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
            "		{Failure = AN_ALE_40V_FAILURE_CONTAINER_RIGHT_WING, Failure_name = \"AN_ALE_40V_FAILURE_CONTAINER_RIGHT_WING\", Failure_editor_name = _(\"AN/ALE-40(V) right wing container failure\"),  Element = 24, Integrity_Treshold = 0.75, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
            "}",
            "",
            "need_to_be_closed = true -- lua_state  will be closed in post_initialize()",
            "--Exported via Bailey's CMS Editor on " + System.DateTime.Now};

       
            if (isExportEnabled == true)
            {
                System.IO.Directory.CreateDirectory(cmdsLua_A10C_FolderPath);
                System.IO.File.WriteAllLines(cmdsLua_A10C_fullPath, luaExportString);

                //https://stackoverflow.com/questions/5920882/file-move-does-not-work-file-already-exists
                System.IO.Directory.CreateDirectory(exportPathBackup_A10C);
                if (File.Exists(exportPathBackup_A10C + "\\AN_ALE40V_params.lua"))
                {
                    File.Delete(exportPathBackup_A10C + "\\AN_ALE40V_params.lua");
                }
                System.IO.File.WriteAllLines(exportPathBackup_A10C + "\\AN_ALE40V_params.lua", luaExportString);
                File.Move(exportPathBackup_A10C + "\\AN_ALE40V_params.lua", Path.ChangeExtension(exportPathBackup_A10C + "\\AN_ALE40V_params.lua", ".lua"));


                MessageBox.Show("Your A-10C CMDS file was exported to \r\n" + cmdsLua_A10C_fullPath + "\r\n\r\n"
                    + "Your A-10C CMDS backup file was exported to \r\n" + exportPathBackup_A10C + "\\AN_ALE40V_params.lua");
            }
            else
            {
                MessageBox.Show("Please select your DCS.exe Location");
            }
        }

        public void A10C_makeDefaultLua()
        {
            string[] luaExportString = {
            "local gettext = require(\"i_18n\")",
            "_ = gettext.translate",
            "",
            "programs = {}",
            "",
            "-- Old generation radar SAM",
            "programs['A'] = {}",
            "programs['A'][\"chaff\"] = 2",
            "programs['A'][\"flare\"] = 0",
            "programs['A'][\"intv\"]  = 1.0",
            "programs['A'][\"cycle\"] = 10",
            "",
            "-- Current generation radar SAM",
            "programs['B'] = {}",
            "programs['B'][\"chaff\"] = 4",
            "programs['B'][\"flare\"] = 0",
            "programs['B'][\"intv\"]  = 0.5",
            "programs['B'][\"cycle\"] = 10",
            "",
            "-- IR SAM",
            "programs['C'] = {}",
            "programs['C'][\"chaff\"] = 0",
            "programs['C'][\"flare\"] = 4",
            "programs['C'][\"intv\"]  = 0.2",
            "programs['C'][\"cycle\"] = 5",
            "",
            "-- Default manual presets",
            "-- Mix 1",
            "programs['D'] = {}",
            "programs['D'][\"chaff\"] = 2",
            "programs['D'][\"flare\"] = 2",
            "programs['D'][\"intv\"]  = 1.0",
            "programs['D'][\"cycle\"] = 10",
            "",
            "-- Mix 2",
            "programs['E'] = {}",
            "programs['E'][\"chaff\"] = 2",
            "programs['E'][\"flare\"] = 2",
            "programs['E'][\"intv\"]  = 0.5",
            "programs['E'][\"cycle\"] = 10",
            "",
            "-- Mix 3",
            "programs['F'] = {}",
            "programs['F'][\"chaff\"] = 4",
            "programs['F'][\"flare\"] = 4",
            "programs['F'][\"intv\"]  = 1.0",
            "programs['F'][\"cycle\"] = 10",
            "",
            "-- Mix 4",
            "programs['G'] = {}",
            "programs['G'][\"chaff\"] = 4",
            "programs['G'][\"flare\"] = 4",
            "programs['G'][\"intv\"]  = 0.5",
            "programs['G'][\"cycle\"] = 10",
            "",
            "-- Chaff single",
            "programs['H'] = {}",
            "programs['H'][\"chaff\"] = 1",
            "programs['H'][\"flare\"] = 0",
            "programs['H'][\"intv\"]  = 1.0",
            "programs['H'][\"cycle\"] = 1",
            "",
            "-- Chaff pair",
            "programs['I'] = {}",
            "programs['I'][\"chaff\"] = 2",
            "programs['I'][\"flare\"] = 0",
            "programs['I'][\"intv\"]  = 1.0",
            "programs['I'][\"cycle\"] = 1",
            "",
            "-- Flare single",
            "programs['J'] = {}",
            "programs['J'][\"chaff\"] = 0",
            "programs['J'][\"flare\"] = 1",
            "programs['J'][\"intv\"]  = 1.0",
            "programs['J'][\"cycle\"] = 1",
            "",
            "-- Flare pair",
            "programs['K'] = {}",
            "programs['K'][\"chaff\"] = 0",
            "programs['K'][\"flare\"] = 2",
            "programs['K'][\"intv\"]  = 1.0",
            "programs['K'][\"cycle\"] = 1",
            "",
            "-- Chaff pre-empt",
            "programs['L'] = {}",
            "programs['L'][\"chaff\"] = 1",
            "programs['L'][\"flare\"] = 0",
            "programs['L'][\"intv\"]  = 1.0",
            "programs['L'][\"cycle\"] = 20",
            "",
            "-- Flare pre-empt",
            "programs['M'] = {}",
            "programs['M'][\"chaff\"] = 0",
            "programs['M'][\"flare\"] = 1",
            "programs['M'][\"intv\"]  = 1.0",
            "programs['M'][\"cycle\"] = 20",
            "",
            "--Program N",
            "programs['N'] = {}",
            "programs['N'][\"chaff\"] = 0",
            "programs['N'][\"flare\"] = 1",
            "programs['N'][\"intv\"]  = 1.0",
            "programs['N'][\"cycle\"] = 20",
            "",
            "--Program O",
            "programs['O'] = {}",
            "programs['O'][\"chaff\"] = 0",
            "programs['O'][\"flare\"] = 1",
            "programs['O'][\"intv\"]  = 1.0",
            "programs['O'][\"cycle\"] = 20",
            "",
            "--Program P",
            "programs['P'] = {}",
            "programs['P'][\"chaff\"] = 0",
            "programs['P'][\"flare\"] = 1",
            "programs['P'][\"intv\"]  = 1.0",
            "programs['P'][\"cycle\"] = 20",
            "",
            "--Program Q",
            "programs['Q'] = {}",
            "programs['Q'][\"chaff\"] = 0",
            "programs['Q'][\"flare\"] = 1",
            "programs['Q'][\"intv\"]  = 1.0",
            "programs['Q'][\"cycle\"] = 20",
            "",
            "--Program R",
            "programs['R'] = {}",
            "programs['R'][\"chaff\"] = 0",
            "programs['R'][\"flare\"] = 1",
            "programs['R'][\"intv\"]  = 1.0",
            "programs['R'][\"cycle\"] = 20",
            "",
            "--Program S",
            "programs['S'] = {}",
            "programs['S'][\"chaff\"] = 0",
            "programs['S'][\"flare\"] = 1",
            "programs['S'][\"intv\"]  = 1.0",
            "programs['S'][\"cycle\"] = 20",
            "",
            "--Program T",
            "programs['T'] = {}",
            "programs['T'][\"chaff\"] = 0",
            "programs['T'][\"flare\"] = 1",
            "programs['T'][\"intv\"]  = 1.0",
            "programs['T'][\"cycle\"] = 20",
            "",
            "--Program U",
            "programs['U'] = {}",
            "programs['U'][\"chaff\"] = 0",
            "programs['U'][\"flare\"] = 1",
            "programs['U'][\"intv\"]  = 1.0",
            "programs['U'][\"cycle\"] = 20",
            "",
            "--Program V",
            "programs['V'] = {}",
            "programs['V'][\"chaff\"] = 0",
            "programs['V'][\"flare\"] = 1",
            "programs['V'][\"intv\"]  = 1.0",
            "programs['V'][\"cycle\"] = 20",
            "",
            "--Program W",
            "programs['W'] = {}",
            "programs['W'][\"chaff\"] = 0",
            "programs['W'][\"flare\"] = 1",
            "programs['W'][\"intv\"]  = 1.0",
            "programs['W'][\"cycle\"] = 20",
            "",
            "--Program X",
            "programs['X'] = {}",
            "programs['X'][\"chaff\"] = 0",
            "programs['X'][\"flare\"] = 1",
            "programs['X'][\"intv\"]  = 1.0",
            "programs['X'][\"cycle\"] = 20",
            "",
            "--Program Y",
            "programs['Y'] = {}",
            "programs['Y'][\"chaff\"] = 0",
            "programs['Y'][\"flare\"] = 1",
            "programs['Y'][\"intv\"]  = 1.0",
            "programs['Y'][\"cycle\"] = 20",
            "",
            "--Program Z",
            "programs['Z'] = {}",
            "programs['Z'][\"chaff\"] = 0",
            "programs['Z'][\"flare\"] = 1",
            "programs['Z'][\"intv\"]  = 1.0",
            "programs['Z'][\"cycle\"] = 20",
            "",
            "",
            "ContainerChaffCapacity = 120",
            "",
            "ContainerFlareCapacity = 60",
            "",
            "NumberOfContiners      = 4",
            "",
            "AN_ALE_40V_FAILURE_TOTAL = 0",
            "AN_ALE_40V_FAILURE_CONTAINER_LEFT_WING	= 1",
            "AN_ALE_40V_FAILURE_CONTAINER_LEFT_GEAR	= 2",
            "AN_ALE_40V_FAILURE_CONTAINER_RIGHT_GEAR	= 3",
            "AN_ALE_40V_FAILURE_CONTAINER_RIGHT_WING	= 4",
            "",
            "Damage = {	{Failure = AN_ALE_40V_FAILURE_TOTAL, Failure_name = \"AN_ALE_40V_FAILURE_TOTAL\", Failure_editor_name = _(\"AN/ALE-40(V) total failure\"),  Element = 10, Integrity_Treshold = 0.5, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
            "		{Failure = AN_ALE_40V_FAILURE_CONTAINER_LEFT_WING, Failure_name = \"AN_ALE_40V_FAILURE_CONTAINER_LEFT_WING\", Failure_editor_name = _(\"AN/ALE-40(V) left wing container failure\"),  Element = 23, Integrity_Treshold = 0.75, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
            "		{Failure = AN_ALE_40V_FAILURE_CONTAINER_LEFT_GEAR, Failure_name = \"AN_ALE_40V_FAILURE_CONTAINER_LEFT_GEAR\", Failure_editor_name = _(\"AN/ALE-40(V) left gear container failure\"),  Element = 15, Integrity_Treshold = 0.75, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
            "		{Failure = AN_ALE_40V_FAILURE_CONTAINER_RIGHT_GEAR, Failure_name = \"AN_ALE_40V_FAILURE_CONTAINER_RIGHT_GEAR\", Failure_editor_name = _(\"AN/ALE-40(V) right gear container failure\"),  Element = 16, Integrity_Treshold = 0.75, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
            "		{Failure = AN_ALE_40V_FAILURE_CONTAINER_RIGHT_WING, Failure_name = \"AN_ALE_40V_FAILURE_CONTAINER_RIGHT_WING\", Failure_editor_name = _(\"AN/ALE-40(V) right wing container failure\"),  Element = 24, Integrity_Treshold = 0.75, work_time_to_fail_probability = 0.5, work_time_to_fail = 3600*300},",
            "}",
            "",
            "need_to_be_closed = true -- lua_state  will be closed in post_initialize()",
            "--Exported via Bailey's CMS Editor on " + System.DateTime.Now};

            if (isExportEnabled == true)
            {
                System.IO.Directory.CreateDirectory(cmdsLua_A10C_FolderPath);
                System.IO.File.WriteAllLines(cmdsLua_A10C_fullPath, luaExportString);
                //https://stackoverflow.com/questions/5920882/file-move-does-not-work-file-already-exists
            }
            else
            {
                MessageBox.Show("Please select your DCS.exe Location");
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

        public void F16C_makeDefaultHarmLua()
        {
            MessageBox.Show("Re-Creation of F-16C Harm Tables is not currently supported.");
            //change the below to harm specific stuff.
            /*
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
            }*/
        }

        public void AV8B_makeDefaultLua()
        {
            string[] luaExportString = {
                "local gettext = require(\"i_18n\")",
                "_ = gettext.translate",
                "",
                "-- Chaff Burst Values",
                "-- BQTY: 1 to 15. Special values: -1 = Continuous (will use ALL chaff); -2 = Random (will dispense between 1 to 6 chaff)",
                "-- BINT: 0.1 to 1.5 seconds. Special values: -2 = Random (will set an interval between 0.1 and 0.4 seconds)",
                "",
                "-- Chaff Salvo Values",
                "-- SQTY: 1 to 15.",
                "-- SINT: 1 to 15 seconds.",
                "",
                "-- Flare Salvo Values",
                "-- SQTY: 1 to 15.",
                "-- SINT: 1 to 15 seconds.",
                "",
                "--All Expendables",
                "EW_ALL_CHAFF_BQTY = 1;",
                "EW_ALL_CHAFF_BINT = 0.1;",
                "EW_ALL_CHAFF_SQTY = 1;",
                "EW_ALL_CHAFF_SINT = 1;",
                "EW_ALL_FLARES_SQTY = 1;",
                "EW_ALL_FLARES_SINT = 1;",
                "",
                "--Chaff Only",
                "EW_CHAFF_BQTY = 1;",
                "EW_CHAFF_BINT = 0.1;",
                "EW_CHAFF_SQTY = 1;",
                "EW_CHAFF_SINT = 1;",
                "",
                "--Flares Only",
                "EW_FLARES_SQTY = 1;",
                "EW_FLARES_SINT = 1;",
                "",
                "need_to_be_closed = true",
                "",

                 "--Exported via Bailey's CMS Editor on " + System.DateTime.Now};
            if (isExportEnabled == true)
            {
                System.IO.Directory.CreateDirectory(cmdsLua_AV8B_FolderPath);
                System.IO.File.WriteAllLines(cmdsLua_AV8B_fullPath, luaExportString);
                //https://stackoverflow.com/questions/5920882/file-move-does-not-work-file-already-exists

            }
            else
            {
                MessageBox.Show("Please select your DCS.exe location.");
            }
        }

        public void M2000C_makeDefaultLua()
        {
            //MessageBox.Show("You were on tab 1");
            string[] luaExportString = {
                "local gettext = require(\"i_18n\")",
                "_ = gettext.translate",
                "",
                "programs = {}",
                "",
                "-- User Modifiable program",
                "programs[1] = {}",
                "programs[1][\"chaff\"]  = 6",
                "programs[1][\"flare\"]  = 0",
                "programs[1][\"intv\"]   = 50",
                "programs[1][\"cycle\"]  = 1",
                "programs[1][\"c_intv\"] = 0",
                "programs[1][\"panic\"]  = 0",
                "",
                "programs[2] = {}",
                "programs[2][\"chaff\"]  = 6",
                "programs[2][\"flare\"]  = 0",
                "programs[2][\"intv\"]   = 50",
                "programs[2][\"cycle\"]  = 2",
                "programs[2][\"c_intv\"] = 200",
                "programs[2][\"panic\"]  = 0",
                "",
                "programs[3] = {}",
                "programs[3][\"chaff\"]  = 6",
                "programs[3][\"flare\"]  = 0",
                "programs[3][\"intv\"]   = 50",
                "programs[3][\"cycle\"]  = 3",
                "programs[3][\"c_intv\"] = 200",
                "programs[3][\"panic\"]  = 0",
                "",
                "programs[4] = {}",
                "programs[4][\"chaff\"]  = 0",
                "programs[4][\"flare\"]  = 2",
                "programs[4][\"intv\"]   = 0",
                "programs[4][\"cycle\"]  = 1",
                "programs[4][\"c_intv\"] = 0",
                "programs[4][\"panic\"]  = 0",
                "",
                "programs[5] = {}",
                "programs[5][\"chaff\"]  = 1",
                "programs[5][\"flare\"]  = 1",
                "programs[5][\"intv\"]   = 0",
                "programs[5][\"cycle\"]  = 1",
                "programs[5][\"c_intv\"] = 0",
                "programs[5][\"panic\"]  = 0",
                "",
                "programs[6] = {}",
                "programs[6][\"chaff\"]  = 12",
                "programs[6][\"flare\"]  = 0",
                "programs[6][\"intv\"]   = 75",
                "programs[6][\"cycle\"]  = 1",
                "programs[6][\"c_intv\"] = 0",
                "programs[6][\"panic\"]  = 0",
                "",
                "programs[7] = {}",
                "programs[7][\"chaff\"]  = 20",
                "programs[7][\"flare\"]  = 0",
                "programs[7][\"intv\"]   = 25",
                "programs[7][\"cycle\"]  = 1",
                "programs[7][\"c_intv\"] = 0",
                "programs[7][\"panic\"]  = 0",
                "",
                "programs[8] = {}",
                "programs[8][\"chaff\"]  = 0",
                "programs[8][\"flare\"]  = 6",
                "programs[8][\"intv\"]   = 25",
                "programs[8][\"cycle\"]  = 1",
                "programs[8][\"c_intv\"] = 0",
                "programs[8][\"panic\"]  = 0",
                "",
                "programs[9] = {}",
                "programs[9][\"chaff\"]  = 20",
                "programs[9][\"flare\"]  = 6",
                "programs[9][\"intv\"]   = 25",
                "programs[9][\"cycle\"]  = 1",
                "programs[9][\"c_intv\"] = 0",
                "programs[9][\"panic\"]  = 0",
                "",
                "programs[10] = {}",
                "programs[10][\"chaff\"]  = 0",
                "programs[10][\"flare\"]  = 32",
                "programs[10][\"intv\"]   = 25",
                "programs[10][\"cycle\"]  = 1",
                "programs[10][\"c_intv\"] = 0",
                "programs[10][\"panic\"]  = 0",
                "",
                "need_to_be_closed = true",
                "",
                "--Exported via Bailey's CMS Editor on " + System.DateTime.Now};
            // WriteAllLines creates a file, writes a collection of strings to the file,
            // and then closes the file.  You do NOT need to call Flush() or Close().
            //System.IO.File.WriteAllLines(@"C:\TestFolder\WriteLines.txt", lines);

            if (isExportEnabled == true)
            {
                System.IO.Directory.CreateDirectory(cmdsLua_M2000C_FolderPath);
                System.IO.File.WriteAllLines(cmdsLua_M2000C_fullPath, luaExportString);
                //https://stackoverflow.com/questions/5920882/file-move-does-not-work-file-already-exists

            }
            else
            {
                MessageBox.Show("Please select your DCS.exe location.");
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
                if (tabControl_mainTab.SelectedTab == tabPage1 && File.Exists(cmdsLua_F18C_fullPath))//this is the f18c tab
                {
                    loadLocation = cmdsLua_F18C_fullPath;
                    loadLua_F18C();
                }
                else if (tabControl_mainTab.SelectedTab == tabPage2 && File.Exists(cmdsLua_F16C_fullPath))//this is the f16c tab
                {
                    loadLocation = cmdsLua_F16C_fullPath;
                    loadLua_F16C();
                }
                else if (tabControl_mainTab.SelectedTab == tabPage3 && File.Exists(cmdsLua_F16C_fullPath))//this is the f16c tab for harms
                {
                    //loadLocation = cmdsLua_F16C_fullPath;
                    //loadLua_F16C();
                    harmForF16cIsNotAvailableMessage();
                }
                else if (tabControl_mainTab.SelectedTab == tabPage4 && File.Exists(cmdsLua_A10C_fullPath))//this is the a10c cms tab
                {
                    loadLocation = cmdsLua_A10C_fullPath;
                    loadLua_A10C_CMS();
                }
                else if (tabControl_mainTab.SelectedTab == tabPage7 && File.Exists(cmdsLua_AV8B_fullPath))//this is the AV8B cms tab
                {
                    loadLocation = cmdsLua_AV8B_fullPath;
                    loadLua_AV8B_CMS();
                }
                else if (tabControl_mainTab.SelectedTab == tabPage8 && File.Exists(cmdsLua_M2000C_fullPath))//this is the m2000c cms tab
                {
                    loadLocation = cmdsLua_M2000C_fullPath;
                    loadLua_M2000C_CMS();
                }
                else {MessageBox.Show("DCS Countermeasure files not found. Please select your DCS.exe location or try a different aircraft.");}
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
            "3.Click the ‘Export’ button. This will export a Countermeasure .lua file to DCS, and it will export a copy to a backup folder that the utility creates. The backup can be used incase the DCS location lua is overwritten by a DCS update, for example." +
            "\r\n" + "\r\n" +
            "4.Load the DCS mission and have fun!" +
            "\r\n" + "\r\n" +
            "A lot of time has been spent making this utility. I do not do this for a living. I have Google, Visual Studio, and some time on my hands. There may be bugs. Please use the utility as intended. If you have any questions, comments, improvements, bugs, concerns, input, or just want to say thanks, please contact me via Discord: Bailey#6230" +
            "\r\n" + "\r\n" +
            "Join us in the Discord Server! https://discord.gg/PbYgC5e" +
            "\r\n" + "\r\n" +
            "You can also look for help and tips for this utility on the Eagle Dynamics Forums here: 'DCS CMS Editor by Bailey' https://forums.eagle.ru/showthread.php?t=281284" +
            "\r\n" + "\r\n" +
            "Please feel free to donate here: https://www.paypal.me/asherao." +
            "\r\n" + "All donations go to making more free Utilities for DCS, like this one!" +
            "\r\n" + "\r\n" +
            "Thank you to Arctic Fox for the idea and collaboration. Thank you to multiple people on Discord for sanity checks." +
            "\r\n" + "\r\n" +
            "~Bailey" + "\r\n" +
            "October 2020" + "\r\n" +
            "v4", "DCS CMS Editor by Bailey READMEE");
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
            if (tabControl_mainTab.SelectedTab == tabPage1)//this is the f18 cms tab
            {
                DialogResult dialogResult = MessageBox.Show("Are you sure you want to clear and reset the F-18C CMS Lua? This cannot be undone.", "Are you sure?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    if (cmdsLua_F18C_FolderPath == null)
                    {
                        MessageBox.Show("DCS.exe has not been set. Please select your DCS.exe location or try a different aircraft.");
                    }
                    else
                    {
                        F18C_makeDefaultLua();
                        MessageBox.Show("Your F-18C CMDS file was exported to \r\n" + cmdsLua_F18C_fullPath + ".");
                        loadCmsFromDcs();
                    }

                }
                else if (dialogResult == DialogResult.No)
                { 
                    //do nothing
                }
            }


            else if (tabControl_mainTab.SelectedTab == tabPage2)//this is the f16 cms tab
            {
                    DialogResult dialogResult = MessageBox.Show("Are you sure you want to clear and reset the F-16C CMS Lua? This cannot be undone.", "Are you sure?", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        if (cmdsLua_F16C_FolderPath == null)
                        {
                            MessageBox.Show("DCS.exe has not been set. Please select your DCS.exe location or try a different aircraft.");
                        }
                        else
                        {
                            F16C_makeDefaultLua();
                            MessageBox.Show("Your F-16C CMDS file was exported to \r\n" + cmdsLua_F16C_fullPath + ".");
                            loadCmsFromDcs();
                        }
                    }
                    else if (dialogResult == DialogResult.No)
                    { 
                        //do nothing
                    }
            }
                else if (tabControl_mainTab.SelectedTab == tabPage3)//this is the f16 harm tab
                {
                harmForF16cIsNotAvailableMessage();
                /*
                DialogResult dialogResult = MessageBox.Show("Are you sure you want to clear and reset the F-16C HARM Lua? This cannot be undone.", "Are you sure?", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        if (cmdsLua_F16C_FolderPath == null)
                        {
                            MessageBox.Show("DCS.exe has not been set. Please select your DCS.exe location or try a different aircraft.");
                        }
                        else
                        {
                            F16C_makeDefaultHarmLua();
                            //enable the below when the feature is supported.
                            //MessageBox.Show("Your F-16C HARM file was exported to \r\n" + cmdsLua_F16C_fullPath + ".");//this should be the harm path
                            loadCmsFromDcs();
                        }
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                        //do nothing
                    }
                */
            }
            else if (tabControl_mainTab.SelectedTab == tabPage4)//this is the a10c tab
            {
                DialogResult dialogResult = MessageBox.Show("Are you sure you want to clear and reset the A-10C CMS Lua? This cannot be undone.", "Are you sure?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    if (cmdsLua_A10C_FolderPath == null)
                    {
                        MessageBox.Show("DCS.exe has not been set. Please select your DCS.exe location or try a different aircraft.");
                    }
                    else
                    {
                        A10C_makeDefaultLua();
                        MessageBox.Show("Your A-10C CMDS file was exported to \r\n" + cmdsLua_A10C_fullPath + ".");
                        loadCmsFromDcs();
                    }
                }
                else if (dialogResult == DialogResult.No)
                {
                    //do nothing
                }
            }

            else if (tabControl_mainTab.SelectedTab == tabPage7)//this is the AV8B tab
            {
                DialogResult dialogResult = MessageBox.Show("Are you sure you want to clear and reset the AV-8B CMS Lua? This cannot be undone.", "Are you sure?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    if (cmdsLua_AV8B_FolderPath == null)
                    {
                        MessageBox.Show("DCS.exe has not been set. Please select your DCS.exe location or try a different aircraft.");
                    }
                    else
                    {
                        AV8B_makeDefaultLua();
                        MessageBox.Show("Your AV-8B CMS file was exported to \r\n" + cmdsLua_AV8B_fullPath + ".");
                        loadCmsFromDcs();
                    }
                }
                else if (dialogResult == DialogResult.No)
                {
                    //do nothing
                }
            }

            else if (tabControl_mainTab.SelectedTab == tabPage8)//this is the m2000c tab
            {
                DialogResult dialogResult = MessageBox.Show("Are you sure you want to clear and reset the M2000C CMS Lua? This cannot be undone.", "Are you sure?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    if (cmdsLua_M2000C_FolderPath == null)
                    {
                        MessageBox.Show("DCS.exe has not been set. Please select your DCS.exe location or try a different aircraft.");
                    }
                    else
                    {
                        M2000C_makeDefaultLua();
                        MessageBox.Show("Your M2000C CMDS file was exported to \r\n" + cmdsLua_M2000C_fullPath + ".");
                        loadCmsFromDcs();
                    }
                }
                else if (dialogResult == DialogResult.No)
                {
                    //do nothing
                }
            }
        }

        private void initF16cHarmComboBoxes()
        {
            //https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.listcontrol.displaymember?view=netcore-3.1
            //i have no idea how this works

            //make the array of SAMs. Add new sams here. Future Source: https://docs.google.com/spreadsheets/d/1p77yaLQJaUMbAIKJDKX5iMN5N5qm_Qqt_dJNyJjLQtw/edit#gid=0
            ArrayList arrayOfSamsForF16cHarm2 = new ArrayList();



            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("Empty", "000"));//0

            /*
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-2_TR (SNR-75V; ID 126)", "126"));//1
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-3_SR (S125_SR_P_19; ID 122)", "122"));//2
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-3_TR (S125_TR_SNR; ID 123)", "123"));//3
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-6 (Kub_STR_9S91; ID 108)", "108"));//4
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-8 (Osa_9A33; ID 117)", "117"));//5
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-10_SR (S300PS_SR_5N66M; ID 103)", "103"));//6
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-10_SR (S300PS_SR_64H6E; ID 104)", "104"));//7
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-10_TR (S300PS_TR_30N6; ID 110)", "110"));//8
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-11_SR (Buk_SR_9S18M1; ID 107)", "107"));//9
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-11_TR (Buk_LN_9A310M1; ID 115)", "115"));//10
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-13 (Strela_9A35M3; ID 118)", "118"));//11
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-15 (Tor_9A331; ID 119)", "119"));//12
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SA-19 (Tunguska_2S6; ID 120)", "120"));//13
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("ZSU-23-4 (ZSU_23_4_Shilka; ID 121)", "121"));//14
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("Dog Ear Radar (Dog Ear; ID 109)", "109"));//15
            */

            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("AAA Gepard - A - ID 207", "207"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("AAA Vulcan M163 - A - ID 208", "208"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("AAA ZSU-23-4 Shilka - A - ID 121", "121"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("CG 1164 Moskva - T2 - ID 303", "303"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("CGN 1144.2 Piotr Velikiy - HN - ID 313", "313"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("CP 9S80M1 Sborka - DE - ID 109", "109"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("CV 1143.5 Admiral Kuznetsov - SW - ID 301", "301"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("CV 1143.5 Admiral Kuznetsov(2017) - SW - ID 320", "320"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("CVN-70 Carl Vinson - SS - ID 402", "402"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("CVN-71 Theodore Roosevelt - SS - ID 403", "403"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("CVN-72 Abraham Lincoln - SS - ID 404", "404"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("CVN-73 George Washington - SS - ID 405", "405"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("CVN-74 John C. Stennis - SS - ID 406", "406"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("EWR 1L13 - S - ID 101", "101"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("EWR 55G6 - S - ID 102", "102"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("FF 1135M Rezky - TP - ID 309", "309"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("FFG 11540 Neustrashimy - TP - ID 319", "319"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("FFL 1124.4 Grisha - HP - ID 306", "306"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("FSG 1241.1MP Molniya - PS - ID 312", "312"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("LHA-1 Tarawa - 40 - ID 407", "407"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("Oliver Hazzard Perry class - 49 - ID 401", "401"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("Rapier FSA Blindfire Tracker - RP - ID 124", "124"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("Rapier FSA Launcher - RT - ID 125", "125"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM Hawk CWAR AN/MPQ-55 - ID 206 HK", "206"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM Hawk SR AN/MPQ-50 - HK - ID 203", "203"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM Hawk TR AN/MPQ-46 - HK - ID 204", "204"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM Patriot STR AN/MPQ-53 - P - ID 2", "202"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM Roland ADS - RO - ID 201", "201"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM Roland SR - RO - ID 205", "205"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-10 S-300PS SR 5N66M - CS - ID 103", "103"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-10 S-300PS SR 64H6E - BB - ID 104", "104"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-10 S-300PS TR 30N6 - 10 - ID 110", "110"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-11 Buk LN 9A310M1 - 11 - ID 115", "115"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-11 Buk SR 9S18M1 - SD - ID 107", "107"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-13 Strela-10M3 9A35M3 - 13 - ID 118", "118"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-15 Tor 9A331 - 15 - ID 119", "119"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-19 Tunguska 2S6 - 19 - ID 120", "120"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-2 TR SNR-75 Fan Song - 2 - ID 126", "126"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-3 S-125 TR SNR - 3 - ID 123", "123"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-6 Kub STR 1S91 - 6 - ID 108", "108"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SA-8 Osa 9A33 - 8 - ID 117", "117"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("SAM SR P-19 - S - ID 122", "122"));
            arrayOfSamsForF16cHarm2.Add(new samForF16Harm("Ticonderoga class - AE - ID 315", "315"));

            //making each combo box do something (shrug)
            //https://stackoverflow.com/questions/20024907/multiple-combobox-controls-from-the-same-dataset
            comboBox_f16cHarm1.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm1.BindingContext = new BindingContext();
            comboBox_f16cHarm1.DisplayMember = "LongName";
            comboBox_f16cHarm1.ValueMember = "ShortName";

            comboBox_f16cHarm2.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm2.BindingContext = new BindingContext();
            comboBox_f16cHarm2.DisplayMember = "LongName";
            comboBox_f16cHarm2.ValueMember = "ShortName";

            comboBox_f16cHarm3.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm3.BindingContext = new BindingContext();
            comboBox_f16cHarm3.DisplayMember = "LongName";
            comboBox_f16cHarm3.ValueMember = "ShortName";

            comboBox_f16cHarm4.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm4.BindingContext = new BindingContext();
            comboBox_f16cHarm4.DisplayMember = "LongName";
            comboBox_f16cHarm4.ValueMember = "ShortName";

            comboBox_f16cHarm5.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm5.BindingContext = new BindingContext();
            comboBox_f16cHarm5.DisplayMember = "LongName";
            comboBox_f16cHarm5.ValueMember = "ShortName";

            comboBox_f16cHarm6.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm6.BindingContext = new BindingContext();
            comboBox_f16cHarm6.DisplayMember = "LongName";
            comboBox_f16cHarm6.ValueMember = "ShortName";

            comboBox_f16cHarm7.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm7.BindingContext = new BindingContext();
            comboBox_f16cHarm7.DisplayMember = "LongName";
            comboBox_f16cHarm7.ValueMember = "ShortName";

            comboBox_f16cHarm8.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm8.BindingContext = new BindingContext();
            comboBox_f16cHarm8.DisplayMember = "LongName";
            comboBox_f16cHarm8.ValueMember = "ShortName";

            comboBox_f16cHarm9.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm9.BindingContext = new BindingContext();
            comboBox_f16cHarm9.DisplayMember = "LongName";
            comboBox_f16cHarm9.ValueMember = "ShortName";

            comboBox_f16cHarm10.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm10.BindingContext = new BindingContext();
            comboBox_f16cHarm10.DisplayMember = "LongName";
            comboBox_f16cHarm10.ValueMember = "ShortName";

            comboBox_f16cHarm11.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm11.BindingContext = new BindingContext();
            comboBox_f16cHarm11.DisplayMember = "LongName";
            comboBox_f16cHarm11.ValueMember = "ShortName";

            comboBox_f16cHarm12.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm12.BindingContext = new BindingContext();
            comboBox_f16cHarm12.DisplayMember = "LongName";
            comboBox_f16cHarm12.ValueMember = "ShortName";

            comboBox_f16cHarm13.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm13.BindingContext = new BindingContext();
            comboBox_f16cHarm13.DisplayMember = "LongName";
            comboBox_f16cHarm13.ValueMember = "ShortName";

            comboBox_f16cHarm14.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm14.BindingContext = new BindingContext();
            comboBox_f16cHarm14.DisplayMember = "LongName";
            comboBox_f16cHarm14.ValueMember = "ShortName";

            comboBox_f16cHarm15.DataSource = arrayOfSamsForF16cHarm2;
            comboBox_f16cHarm15.BindingContext = new BindingContext();
            comboBox_f16cHarm15.DisplayMember = "LongName";
            comboBox_f16cHarm15.ValueMember = "ShortName";

            //indexes for the defaults as of 20sep2020
            //8,7,6,10,9
            //13,12,5,14,15
            //3,2,4,1,11

            //setting the default selected values for the text boxes
            comboBox_f16cHarm1.SelectedIndex = 8;
            comboBox_f16cHarm2.SelectedIndex = 7;
            comboBox_f16cHarm3.SelectedIndex = 4;
            comboBox_f16cHarm4.SelectedIndex = 10;
            comboBox_f16cHarm5.SelectedIndex = 9;

            comboBox_f16cHarm6.SelectedIndex = 13;
            comboBox_f16cHarm7.SelectedIndex = 12;
            comboBox_f16cHarm8.SelectedIndex = 5;
            comboBox_f16cHarm9.SelectedIndex = 14;
            comboBox_f16cHarm10.SelectedIndex = 15;

            comboBox_f16cHarm11.SelectedIndex = 3;
            comboBox_f16cHarm12.SelectedIndex = 2;
            comboBox_f16cHarm13.SelectedIndex = 4;
            comboBox_f16cHarm14.SelectedIndex = 1;
            comboBox_f16cHarm15.SelectedIndex = 11;

            updateHarmIdLabels();//update the label called ID

        }

        public class samForF16Harm
            //https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.listcontrol.displaymember?view=netcore-3.1
            //i have no idea how this works
        {
            private string myShortName;
            private string myLongName;

            public samForF16Harm(string strLongName, string strShortName)
            {

                this.myShortName = strShortName;
                this.myLongName = strLongName;
            }

            public string ShortName
            {
                get
                {
                    return myShortName;
                }
            }

            public string LongName
            {

                get
                {
                    return myLongName;
                }
            }
        }

        private void updateHarmIdLabels()
        {
            //https://stackoverflow.com/questions/15982929/how-to-get-valuemember-value-from-combobox-c-sharp-winforms
            //displays the ID in a text label
            //TODO: Rename the 'comboBox_f16cHarm1' to 'comboBox_f16CHarm_table1_1'
            //apply the above convention to all harm combo boxes

            //this method is not directly in use on lead becuase the combobox changes from null to the defaults and 
            //causes and error because the string requested below starts as null
            label_F16CHarm_selectedID_Table1_T1.Text = comboBox_f16cHarm1.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table1_T2.Text = comboBox_f16cHarm2.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table1_T3.Text = comboBox_f16cHarm3.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table1_T4.Text = comboBox_f16cHarm4.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table1_T5.Text = comboBox_f16cHarm5.SelectedValue.ToString();

            label_F16CHarm_selectedID_Table2_T1.Text = comboBox_f16cHarm6.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table2_T2.Text = comboBox_f16cHarm7.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table2_T3.Text = comboBox_f16cHarm8.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table2_T4.Text = comboBox_f16cHarm9.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table2_T5.Text = comboBox_f16cHarm10.SelectedValue.ToString();

            label_F16CHarm_selectedID_Table3_T1.Text = comboBox_f16cHarm11.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table3_T2.Text = comboBox_f16cHarm12.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table3_T3.Text = comboBox_f16cHarm13.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table3_T4.Text = comboBox_f16cHarm14.SelectedValue.ToString();
            label_F16CHarm_selectedID_Table3_T5.Text = comboBox_f16cHarm15.SelectedValue.ToString();
        }


        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void comboBox_f16cHarm1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm1.SelectedValue.ToString() == null)//if tje box in null, dont do anything
            {
                return;
            }
            else//if the comboBox has something selected, change the ID number label
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table1_T1.Text = comboBox_f16cHarm1.SelectedValue.ToString();
            }
           
        }

        private void comboBox_f16cHarm2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm2.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table1_T2.Text = comboBox_f16cHarm2.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm3.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table1_T3.Text = comboBox_f16cHarm3.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm4.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table1_T4.Text = comboBox_f16cHarm4.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm5.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table1_T5.Text = comboBox_f16cHarm5.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm6.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table2_T1.Text = comboBox_f16cHarm6.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm7_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm7.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table2_T2.Text = comboBox_f16cHarm7.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm8_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm8.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table2_T3.Text = comboBox_f16cHarm8.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm9_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm9.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table2_T4.Text = comboBox_f16cHarm9.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm10_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm10.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table2_T5.Text = comboBox_f16cHarm10.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm11_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm11.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table3_T1.Text = comboBox_f16cHarm11.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm12_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm12.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table3_T2.Text = comboBox_f16cHarm12.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm13_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm13.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table3_T3.Text = comboBox_f16cHarm13.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm14_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm14.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table3_T4.Text = comboBox_f16cHarm14.SelectedValue.ToString();
            }
        }

        private void comboBox_f16cHarm15_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_f16cHarm15.SelectedValue.ToString() == null)
            {
                return;
            }
            else
            {
                //updateHarmIdLabels();
                label_F16CHarm_selectedID_Table3_T5.Text = comboBox_f16cHarm15.SelectedValue.ToString();
            }
        }

        private void label_F16CHarm_samName_table1_Click(object sender, EventArgs e)
        {

        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

        private void label47_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown_A10C_programA_chaff_Enter(object sender, EventArgs e)
        {
            //MessageBox.Show("This is a message");
            numericUpDown_A10C_programA_chaff.Controls[0].Show();//this should show the numeric updown arrows on focus
        }

        private void numericUpDown_A10C_programA_chaff_Leave(object sender, EventArgs e)
        {
            numericUpDown_A10C_programA_chaff.Controls[0].Hide();//this should hide the numeric updown arrows on unfocus
        }

        private void textBox_programN_KeyPress(object sender, KeyPressEventArgs e)
        {
            //https://stackoverflow.com/questions/19524105/how-to-block-or-restrict-special-characters-from-textbox
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programO_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programP_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programQ_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programR_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programS_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programT_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void textBox_programT_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programU_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programV_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programW_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programX_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programY_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void textBox_programZ_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) || e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Space || char.IsDigit(e.KeyChar))//enables the use of letters, backspace, space, and numbers https://www.codeproject.com/Questions/382891/Restrict-textbox-from-Special-Characters-and-Numbe 
            {
                // These characters may pass
                e.Handled = false;
            }
            else
            {
                // Everything that is not a letter, nor a backspace nor a space nor a number will be blocked
                e.Handled = true;
            }
        }

        private void harmForF16cIsNotAvailableMessage()
        {
            
            DialogResult dialogResult = MessageBox.Show("This feature is not yet available. Would you like to visit the 'Enable Editing Of Default DED HARM Tables Via A Lua File' thread on the ED forums to ask ED's help to implement the feature? https://forums.eagle.ru/showthread.php?t=286963", "Feature Not Available", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                //https://stackoverflow.com/questions/502199/how-to-open-a-web-page-from-my-application
                Process.Start("https://forums.eagle.ru/showthread.php?t=286963");
            }
            else if (dialogResult == DialogResult.No)
            {
                //do something else
            }
        }

        private void numericUpDown_A10C_programS_cycle_ValueChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckStateChanged(object sender, EventArgs e)
        {
           
        }

        private void checkBox_AV8B_chaffBquantity_continuous_CheckStateChanged(object sender, EventArgs e)
        {
            
        }

        private void checkBox_AV8B_chaffBquantity_random_CheckStateChanged(object sender, EventArgs e)
        {
           
        }

        private void checkBox_AV8B_chaffBinterval_random_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_AV8B_ALL_chaffBinterval_random.Checked == true)
            {
                numericUpDown_AV8B_ALL_CHAFF_BINT.Enabled = false;
            }
            else
            {
                numericUpDown_AV8B_ALL_CHAFF_BINT.Enabled = true;
            }
        }

        private void checkBox_AV8B_chaffBquantity_continuous_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_AV8B_ALL_chaffBquantity_continuous.Checked == true)
            {
                numericUpDown_AV8B_ALL_CHAFF_BQTY.Enabled = false;
                checkBox_AV8B_ALL_chaffBquantity_random.Checked = false;
                checkBox_AV8B_ALL_chaffBquantity_random.Enabled = false;
            }
            else
            {
                numericUpDown_AV8B_ALL_CHAFF_BQTY.Enabled = true;
                checkBox_AV8B_ALL_chaffBquantity_random.Checked = false;
                checkBox_AV8B_ALL_chaffBquantity_random.Enabled = true;
            }
        }

        private void checkBox_AV8B_chaffBquantity_random_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_AV8B_ALL_chaffBquantity_random.Checked == true)
            {
                numericUpDown_AV8B_ALL_CHAFF_BQTY.Enabled = false;
                checkBox_AV8B_ALL_chaffBquantity_continuous.Checked = false;
                checkBox_AV8B_ALL_chaffBquantity_continuous.Enabled = false;
            }
            else
            {
                numericUpDown_AV8B_ALL_CHAFF_BQTY.Enabled = true;
                checkBox_AV8B_ALL_chaffBquantity_continuous.Checked = false;
                checkBox_AV8B_ALL_chaffBquantity_continuous.Enabled = true;
            }
        }

        private void numericUpDown_A10C_programA_chaff_ValueChanged(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_AV8B_chaffBquantity_continuous.Checked == true)
            {
                numericUpDown_AV8B_CHAFF_BQTY.Enabled = false;
                checkBox_AV8B_chaffBquantity_random.Checked = false;
                checkBox_AV8B_chaffBquantity_random.Enabled = false;
            }
            else
            {
                numericUpDown_AV8B_CHAFF_BQTY.Enabled = true;
                checkBox_AV8B_chaffBquantity_random.Checked = false;
                checkBox_AV8B_chaffBquantity_random.Enabled = true;
            }
        }

        private void checkBox_AV8B_chaffBquantity_random_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBox_AV8B_chaffBquantity_random.Checked == true)
            {
                numericUpDown_AV8B_CHAFF_BQTY.Enabled = false;
                checkBox_AV8B_chaffBquantity_continuous.Checked = false;
                checkBox_AV8B_chaffBquantity_continuous.Enabled = false;
            }
            else
            {
                numericUpDown_AV8B_CHAFF_BQTY.Enabled = true;
                checkBox_AV8B_chaffBquantity_continuous.Checked = false;
                checkBox_AV8B_chaffBquantity_continuous.Enabled = true;
            }
        }

        private void checkBox_AV8B_chaffBinterval_random_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBox_AV8B_chaffBinterval_random.Checked == true)
            {
                numericUpDown_AV8B_CHAFF_BINT.Enabled = false;
            }
            else
            {
                numericUpDown_AV8B_CHAFF_BINT.Enabled = true;
            }
        }
    }
}
