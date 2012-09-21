using System;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Xml;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Reflection;
using Zeta;
using Zeta.Internals.Actors;
using Zeta.XmlEngine;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Settings;
using Zeta.Common.Plugins;
using System.Threading;
using System.Diagnostics;
using System.IO;
using ProfileSwitcher.Helpers;

namespace ProfileSwitcher
{
    public class ProfileSwitcher : IPlugin
    {
        private static string cfgFile = "";
        private static string switcherPath = "";
        public Version Version { get { return new Version(1, 1, 7); } }
        public string Author { get { return "Haley/sfj"; } }
        public string Description { get { return "Loads next profile if finished or died"; } }
        public string Name { get { return "ProfileSwitcher"; } }
        public bool initialized { get; set; }
        private static Stopwatch runTimer = new Stopwatch();
        private DateTime currentDeath;
        private DateTime lastDeath;
        private Stopwatch deathTimer = new Stopwatch();
        private Stopwatch deathVendorTimer = new Stopwatch();
        private bool wasVendoring { get; set; }
        private bool wasDead { get; set; }
        private int maxDeathRetries = 2;
        private int maxRetryRunTime = 5;
        private static int maxReviveTime = 20;
        public static int deathRetries = 0;
        private static long reviveTime = 0;
        public static string profilesPath { get; set; }
        public static bool advancedDeathHandling = true;
        public static int advancedDrathHandlingMethod = 0;
        public static bool profileRandomization = false;
        public static bool pathOverride = false;
        public static string configProfilesPath { get; set; }


        public static void Log(string message)
        {
            Logging.Write("[ProfileSwitcher] " + message);
        }

        public static void Logger(string line)
        {
            using (StreamWriter writer = new StreamWriter("" + switcherPath + "running_log.txt", true))
            {
                writer.WriteLine(line);
                writer.Close();
            }
        }

        public static void startRunTimer()
        {
            runTimer.Stop();
            runTimer.Reset();
            runTimer.Start();
        }

        public void OnInitialize()
        {
            switcherPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\Plugins\ProfileSwitcher\";
            cfgFile = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\Settings\ProfileSwitcher.cfg";
            Zeta.Common.Plugins.PluginManager.SetEnabledPlugins(new string[] { "ProfileSwitcher" });
            profileHandler.profileWaypoints = new List<Vector3>();
            Log("Initialized.");
        }

        public void OnPulse()
        {
            if (initialized)
            {
                if (!deathTimer.IsRunning && ZetaDia.Me.IsDead)
                {
                    deathTimer.Start();
                }
                if (wasDead && !ZetaDia.Me.IsDead)
                {
                    wasDead = false;
                    DateTime currentDateTime = DateTime.Now;
                    String dateStr = currentDateTime.ToString("dd.MM HH:mm:ss");
                    Logger("" + dateStr + " I died - Profile was " + GlobalSettings.Instance.LastProfile);
                    reviveTime = deathTimer.ElapsedMilliseconds - 5000;
                    deathVendorTimer.Start();
                    //Log("ReviveTime: " + reviveTime);
                    if (reviveTime > (maxReviveTime * 1000))
                    {
                        Log("Revive Time to high! Maybe stuck on bad pack at revive point - logging out!");
                        profileHandler.startNewGame();
                    }
                    else
                    {
                        if (deathRetries >= maxDeathRetries)
                        {
                            Log("I died, loading next profile.");
                            if (profileHandler.wasLastProfile(profileHandler.getPreDeathProfile())) {
                            }
                            else
                            {
                                deathRetries = 0;
                                profileHandler.tempProfileWasLoaded = false;
                                profileHandler.LoadProfile(profileHandler.getNextProfilePath(profileHandler.getPreDeathProfile()));
                            }
                        }
                        else
                        {
                            if (runTimer.ElapsedMilliseconds <= (maxRetryRunTime * 60 * 1000))
                            {
                                Log(string.Format("Retry after death, attempt #{0} of {1}", ++deathRetries, maxDeathRetries));
                                startRunTimer();
                                if (advancedDeathHandling == true)
                                {
                                    switch (advancedDrathHandlingMethod){
                                        case 0: //Find nearest Waypoint
                                            if (!profileHandler.LoadTempProfile())
                                            {
                                                Log("No close waypoint found, restarting profile");
                                                profileHandler.ReloadProfile();
                                            }
                                            break;
                                        case 1: //Checkpoint System
                                            //Checkpoint logic atm not implemented
                                            break;
                                    }
                                }
                                else
                                {
                                    profileHandler.ReloadProfile();
                                }
                            }
                            else
                            {
                                Log(string.Format("I died; Profile is runnning for more than {0} minutes. Loading next!", maxRetryRunTime));
                                profileHandler.LoadNextProfile();
                            }
                        }
                    }
                    deathTimer.Stop();
                    deathTimer.Reset();
                }
                if (deathVendorTimer.ElapsedMilliseconds > 10000)
                {
                    if (Zeta.CommonBot.Logic.BrainBehavior.IsVendoring)
                    {
                        //Debug: Log("Waiting to Complete Vendor run");
                        wasVendoring = true;
                    }
                    else
                    {
                        if (wasVendoring)
                        {
                            ProfileManager.Load(GlobalSettings.Instance.LastProfile);
                            wasVendoring = false;
                        }
                        deathVendorTimer.Stop();
                        deathVendorTimer.Reset();

                    }
                }
            }
        }

        public void OnPlayerDied(object sender, EventArgs e)
        {
            currentDeath = DateTime.Now;
            TimeSpan ts = new TimeSpan(currentDeath.Ticks - lastDeath.Ticks);
            //Filter OnPlayerDied event spamming from DB
            if (ts.TotalSeconds < 5)
            {
                Log("Ignoring Death");
            }
            else
            {
                wasDead = true;
            }
            if (Path.GetDirectoryName(Path.GetTempPath()) != Path.GetDirectoryName(GlobalSettings.Instance.LastProfile))
            {
                Log("Updating preDeathProfile: " + GlobalSettings.Instance.LastProfile);
                profileHandler.setPreDeathProfile(GlobalSettings.Instance.LastProfile);
            }
            lastDeath = DateTime.Now;
        }

        public void init()
        {
            deathRetries = 0;
            deathTimer.Stop();
            deathTimer.Reset();
            deathVendorTimer.Stop();
            deathVendorTimer.Reset();
            wasDead = false;
            initialized = true;
            startRunTimer();
        }

        public void OnGameJoined(object sender, EventArgs e)
        {
            init();
        }

        public void OnGameChanged(object sender, EventArgs e)
        {
            if (pathOverride)
            {
                profilesPath = configProfilesPath;
                Log("Upadating profilesPath: " + profilesPath);
            }
            else
            {
                if (Path.GetDirectoryName(Path.GetTempPath()) != Path.GetDirectoryName(GlobalSettings.Instance.LastProfile))
                {
                    profilesPath = Path.GetDirectoryName(GlobalSettings.Instance.LastProfile);
                    Log("Upadating profilesPath: " + profilesPath);
                }
            }
            Logger("--------------------------NEW RUN--------------------------");
            profileHandler.updateProfiles();
            profileHandler.LoadProfile(profileHandler.getFirstProfilePath());
            init();
        }

        public void OnGameLeft(object sender, EventArgs e)
        {
            //profileHandler.updateProfiles();
            ProfileManager.Load(profileHandler.getFirstProfilePath());
            initialized = false;
        }

        public void OnShutdown()
        {
        }

        public void OnEnabled()
        {
            LoadConfiguration();
            if (!Directory.Exists(switcherPath))
            {
                Log("Invalid path: " + switcherPath);
                Log(@"Plugin must be installed to \<DemonBuddyFolder>\Plugins\ProfileSwitcher\");
            }
            else
            {
                GameEvents.OnPlayerDied += new EventHandler<EventArgs>(OnPlayerDied);
                GameEvents.OnGameJoined += new EventHandler<EventArgs>(OnGameJoined);
                GameEvents.OnGameLeft += new EventHandler<EventArgs>(OnGameLeft);
                GameEvents.OnGameChanged += new EventHandler<EventArgs>(OnGameChanged);
                Log("Enabled.");
            }
        }

        public void OnDisabled()
        {
            GameEvents.OnPlayerDied -= new EventHandler<EventArgs>(OnPlayerDied);
            GameEvents.OnGameJoined -= new EventHandler<EventArgs>(OnGameJoined);
            GameEvents.OnGameLeft -= new EventHandler<EventArgs>(OnGameLeft);
            GameEvents.OnGameChanged -= new EventHandler<EventArgs>(OnGameChanged);
            Log("Disabled");
        }

        public bool Equals(IPlugin other)
        {
            return (other.Name == Name) && (other.Version == Version);
        }



        // ***************************************************
        // Config window region. Thanks to GilesSmith for allowing me to modify and share his code! -secretbear 
        // Thx to secretbear 4 adding settings menu - sfj
        // ***************************************************
        #region configWindow
        private Button buttonSave, buttonDefault;
        private TextBox textDeathRetries, textRetryRunTime, textMaxReviveTime, textProfilesPath;
        private CheckBox checkboxEnableAdvancedDeathhandling, checkboxEnableProfileRandomization, checkboxPathOverride;
        private ComboBox comboBoxAndvancedDeathHandling;
        private Window configWindow;
        public Window DisplayWindow
        {
            get
            {
                if (!File.Exists(switcherPath + "ProfileSwitcher.xaml"))
                    Log("Can't find \"" + switcherPath + "ProfileSwitcher.xaml\"");
                try
                {
                    if (configWindow == null)
                    {
                        configWindow = new Window();
                    }
                    // Loads the .xaml
                    StreamReader xamlStream = new StreamReader(switcherPath + "ProfileSwitcher.xaml");
                    DependencyObject xamlContent = XamlReader.Load(xamlStream.BaseStream) as DependencyObject;
                    configWindow.Content = xamlContent;

                    // Fetches and sets properties and actions of the GUI components and loads them
                    buttonSave = LogicalTreeHelper.FindLogicalNode(xamlContent, "buttonSave") as Button;
                    buttonSave.Click += new RoutedEventHandler(buttonSave_Click);
                    buttonDefault = LogicalTreeHelper.FindLogicalNode(xamlContent, "buttonDefault") as Button;
                    buttonDefault.Click += new RoutedEventHandler(buttonDefault_Click);
                    checkboxEnableAdvancedDeathhandling = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkboxEnableAdvancedDeathhandling") as CheckBox;
                    checkboxEnableAdvancedDeathhandling.Checked += new RoutedEventHandler(checkboxAdvancedDeathHandling_check);
                    checkboxEnableAdvancedDeathhandling.Unchecked += new RoutedEventHandler(checkboxAdvancedDeathHandling_uncheck);
                    comboBoxAndvancedDeathHandling = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkBoxAdvancedDeathHandling") as ComboBox;
                    comboBoxAndvancedDeathHandling.SelectionChanged += new SelectionChangedEventHandler(comboBoxAdvancedDeathhandling_selectionChanged);
                    checkboxEnableProfileRandomization = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkboxEnableProfileRandomization") as CheckBox;
                    checkboxEnableProfileRandomization.Checked += new RoutedEventHandler(checkboxProfileRandomization_check);
                    checkboxEnableProfileRandomization.Unchecked += new RoutedEventHandler(checkboxProfileRandomization_uncheck);
                    textDeathRetries = LogicalTreeHelper.FindLogicalNode(xamlContent, "textDeathRetries") as TextBox;
                    textRetryRunTime = LogicalTreeHelper.FindLogicalNode(xamlContent, "textRetryRunTime") as TextBox;
                    textMaxReviveTime = LogicalTreeHelper.FindLogicalNode(xamlContent, "textMaxReviveTime") as TextBox;
                    checkboxPathOverride = LogicalTreeHelper.FindLogicalNode(xamlContent, "checkboxPathOverride") as CheckBox;
                    checkboxPathOverride.Checked += new RoutedEventHandler(checkboxPathOverride_check);
                    checkboxPathOverride.Unchecked += new RoutedEventHandler(checkboxPathOverride_uncheck);
                    textProfilesPath = LogicalTreeHelper.FindLogicalNode(xamlContent, "textProfilesPath") as TextBox;
                    UserControl mainControl = LogicalTreeHelper.FindLogicalNode(xamlContent, "mainControl") as UserControl;
                    configWindow.Height = mainControl.Height + 50;
                    configWindow.Width = mainControl.Width + 50;
                    configWindow.Title = "ProfileSwitcher";
                    configWindow.Loaded += new RoutedEventHandler(configWindow_Loaded);
                    configWindow.Closed += configWindow_Closed;
                    configWindow.Content = xamlContent;

                    // Fetches the values of the text fields
                    checkboxEnableAdvancedDeathhandling.IsChecked = advancedDeathHandling;
                    checkboxEnableProfileRandomization.IsChecked = profileRandomization;
                    textDeathRetries.Text = maxDeathRetries.ToString();
                    textRetryRunTime.Text = maxRetryRunTime.ToString();
                    textMaxReviveTime.Text = maxReviveTime.ToString();
                    checkboxPathOverride.IsChecked = pathOverride;
                    textProfilesPath.Text = configProfilesPath.ToString();
                }
                catch (XamlParseException ex)
                {
                    Log(ex.ToString());
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                }
                return configWindow;
            }
        }

        private void checkboxAdvancedDeathHandling_check(object sender, RoutedEventArgs e)
        {
            advancedDeathHandling = true;
        }

        private void checkboxAdvancedDeathHandling_uncheck(object sender, RoutedEventArgs e)
        {
            advancedDeathHandling = false;
        }

        private void comboBoxAdvancedDeathhandling_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            advancedDrathHandlingMethod = comboBoxAndvancedDeathHandling.SelectedIndex;
        }

        private void checkboxProfileRandomization_check(object sender, RoutedEventArgs e)
        {
            profileRandomization = true;
        }

        private void checkboxProfileRandomization_uncheck(object sender, RoutedEventArgs e)
        {
            profileRandomization = false;
        }

        private void checkboxPathOverride_check(object sender, RoutedEventArgs e)
        {
            pathOverride = true;
            textProfilesPath.Visibility = Visibility.Visible;
        }

        private void checkboxPathOverride_uncheck(object sender, RoutedEventArgs e)
        {
            pathOverride = false;
            textProfilesPath.Visibility = Visibility.Hidden;
        }

        private void configWindow_Closed(object sender, EventArgs e)
        {
            configWindow = null;
        }

        private void configWindow_Loaded(object sender, RoutedEventArgs e)
        {
            checkboxEnableAdvancedDeathhandling.IsChecked = advancedDeathHandling;
            comboBoxAndvancedDeathHandling.SelectedIndex = advancedDrathHandlingMethod;
            checkboxEnableProfileRandomization.IsChecked = profileRandomization;
            textDeathRetries.Text = maxDeathRetries.ToString();
            textRetryRunTime.Text = maxRetryRunTime.ToString();
            textMaxReviveTime.Text = maxReviveTime.ToString();
            checkboxPathOverride.IsChecked = pathOverride;
            textProfilesPath.Text = configProfilesPath.ToString();
        }
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool saved = true;
                try { advancedDeathHandling = Convert.ToBoolean(checkboxEnableAdvancedDeathhandling.IsChecked); }
                catch { saved = false; }
                try { profileRandomization = Convert.ToBoolean(checkboxEnableProfileRandomization.IsChecked); }
                catch { saved = false; }
                try { maxDeathRetries = Convert.ToInt32(textDeathRetries.Text); }
                catch { saved = false; }
                try { maxRetryRunTime = Convert.ToInt32(textRetryRunTime.Text); }
                catch { saved = false; }
                try { maxReviveTime = Convert.ToInt32(textMaxReviveTime.Text); }
                catch { saved = false; }
                try { pathOverride = Convert.ToBoolean(checkboxPathOverride.IsChecked); }
                catch { saved = false; }
                try { configProfilesPath = Convert.ToString(textProfilesPath.Text); }
                catch { saved = false; }
                if (!saved)
                    Log("Couldn't save settings!");
                SaveConfiguration();
                configWindow.Close();
            }
            catch (Exception er)
            {
                Log("Couldn't save settings: " + er.ToString());
            }
            if (!ZetaDia.IsInGame)
            {
                profileHandler.updateProfiles();
            }
        }

        private void buttonDefault_Click(object sender, RoutedEventArgs e)
        {
            advancedDeathHandling = true;
            advancedDrathHandlingMethod = 0;
            checkboxEnableAdvancedDeathhandling.IsChecked = true;
            profileRandomization = false;
            checkboxEnableProfileRandomization.IsChecked = false;
            textDeathRetries.Text = "2";
            maxDeathRetries = 2;
            textRetryRunTime.Text = "5";
            maxRetryRunTime = 5;
            textMaxReviveTime.Text = "20";
            maxReviveTime = 20;
            pathOverride = false;
            checkboxPathOverride.IsChecked = false;
            textProfilesPath.Text = @"C:\";
            configProfilesPath = @"C:\";
        }

        #endregion

        private void SaveConfiguration()
        {
            FileStream configStream = File.Open(cfgFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            using (StreamWriter configWriter = new StreamWriter(configStream))
            {
                configWriter.WriteLine("advancedDeathHandling=" + advancedDeathHandling.ToString());
                configWriter.WriteLine("advancedDrathHandlingMethod=" + advancedDrathHandlingMethod.ToString());
                configWriter.WriteLine("profileRandomization=" + profileRandomization.ToString());
                configWriter.WriteLine("maxDeathRetries=" + maxDeathRetries.ToString());
                configWriter.WriteLine("maxRetryRunTime=" + maxRetryRunTime.ToString());
                configWriter.WriteLine("maxReviveTime=" + maxReviveTime.ToString());
                configWriter.WriteLine("pathOverride=" + pathOverride.ToString());
                configWriter.WriteLine("profilesPath=" + configProfilesPath.ToString());
                Log("Settings saved to: " + cfgFile);
            }
            configStream.Close();
        }

        private void LoadConfiguration()
        {
            //Check if the cfg file exists, if not - create it
            if (!File.Exists(cfgFile))
            {
                Log("No .cfg file in Settings folder, creating default: " + cfgFile);
                SaveConfiguration();
                return;
            }
            //Load file if it does exist and declare variables
            using (StreamReader configReader = new StreamReader(cfgFile))
            {
                while (!configReader.EndOfStream)
                {
                    string[] config = configReader.ReadLine().Split('=');
                    if (config != null)
                    {
                        switch (config[0])
                        {
                            case "advancedDeathHandling":
                                advancedDeathHandling = Convert.ToBoolean(config[1]);
                                break;
                            case "advancedDrathHandlingMethod":
                                advancedDrathHandlingMethod = Convert.ToInt32(config[1]);
                                break;
                            case "profileRandomization":
                                profileRandomization = Convert.ToBoolean(config[1]);
                                break;
                            case "maxDeathRetries":
                                maxDeathRetries = Convert.ToInt32(config[1]);
                                break;
                            case "maxRetryRunTime":
                                maxRetryRunTime = Convert.ToInt32(config[1]);
                                break;
                            case "maxReviveTime":
                                maxReviveTime = Convert.ToInt32(config[1]);
                                break;
                            case "pathOverride":
                                pathOverride = Convert.ToBoolean(config[1]);
                                break;
                            case "profilesPath":
                                configProfilesPath = config[1];
                                break;
                        }
                    }
                }
                configReader.Close();
            }
        }
    }
}