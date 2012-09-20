using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.CommonBot.Settings;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Profile.Common;

namespace ProfileSwitcher.Helpers
{
    public static class profileHandler
    {
        public static string[] profiles;
        public static string[] randomProfiles;
        public static string profilePath;
        private static float[] profilePriorities;
        private static Vector3 revivePosition { get; set; }
        public static List<Vector3> profileWaypoints { get; set; }
        private static string preDeathProfile { get; set; }
        public static bool tempProfileWasLoaded = false;

        public static void setPreDeathProfile(string profile)
        {
            preDeathProfile = profile;
        }

        public static string getPreDeathProfile()
        {
            return preDeathProfile;
        }

        public static void updateProfiles()
        {
            int startIndex = 0;
            ProfileSwitcher.Log("Updating profiles...");
            profilePath = ProfileSwitcher.profilesPath;
            profiles = Directory.GetFiles(profilePath, "*.xml");
            if (ProfileSwitcher.profileRandomization == true)
            {
                string randomFilePath = profilePath + "\\random.txt";
                Random randGenerator = new Random(DateTime.Now.Ticks.GetHashCode());
                profilePriorities = new float[profiles.Length];
                if (File.Exists(randomFilePath))
                {
                    using (StreamReader reader = new StreamReader(randomFilePath))
                    {
                        string line;
                        int i = 0;
                        while (((line = reader.ReadLine()) != null) && (i < profiles.Length))
                        {
                            int priority = Int32.Parse(line);
                            if ((priority < 1) || (priority > 99) && (priority != 0))
                            {
                                ProfileSwitcher.Log("Warning: Randomization priorities lower than 1 or higher than 99 can cause problems!!!");
                            }
                            profilePriorities[i] = (float)priority;
                            i++;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < profiles.Length; i++)
                    {
                        ProfileSwitcher.Log(randomFilePath + " not found, Randomizing all profiles!");
                        profilePriorities[i] = (float)1;
                    }
                }
                for (int i = 0; i < profilePriorities.Length; i++)
                {
                    float multiplier = (float)randGenerator.Next(10000, 10100) / (float)10000.0;
                    profilePriorities[i] = (float)profilePriorities[i] * multiplier;
                }
                randomProfiles = new string[profiles.Length];
                Array.Copy(profiles, randomProfiles, profiles.Length);
                Array.Sort(profilePriorities, randomProfiles);
                startIndex = 0;
                for (int i = 0; i < profilePriorities.Length; i++)
                {
                    if (profilePriorities[i] > 0.0)
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (ZetaDia.IsInGame && Zeta.CommonBot.BotMain.IsRunning)
                {
                    int counter = 0;
                    ProfileSwitcher.Log("Randomized profiles for next run:");
                    ProfileSwitcher.Logger("Randomization order for this run:");
                    for (int i = 0; i < profilePriorities.Length; i++)
                    {
                        if (i >= startIndex)
                        {
                            ProfileSwitcher.Log((++counter) + ": " + randomProfiles[i]);
                            ProfileSwitcher.Logger((counter) + ": " + randomProfiles[i]);
                        }
                    }
                    ProfileSwitcher.Logger("\n");
                }
                Array.Clear(profiles, 0, profiles.Length);
                Array.Copy(randomProfiles, startIndex, profiles, 0, randomProfiles.Length - startIndex);
            }
        }

        public static string getNextProfilePath(string currentPath = null)
        {
            if(currentPath == null)
            {
                currentPath = GlobalSettings.Instance.LastProfile;
            }
            if ((profiles.IndexOf(currentPath) + 1) < profiles.Length)
            {
                return profiles[profiles.IndexOf(currentPath) + 1];
            }
            else
            {
                return profiles[0];
            }
        }
        public static string getFirstProfilePath()
        {
            //updateProfiles();
            return profiles[0];
        }
        public static string getLastProfilePath()
        {
            return profiles[(profiles.Length - 1)];
        }
        public static void LoadProfile(string path)
        {
            DateTime currentDateTime = DateTime.Now;
            String dateStr = currentDateTime.ToString("dd.MM HH:mm:ss");
            ProfileSwitcher.Logger("" + dateStr + " loading profile: " + path);
            ProfileSwitcher.startRunTimer();
            ProfileSwitcher.deathRetries = 0;
            ProfileSwitcher.Log("Loading profile: " + path);
            ProfileManager.Load(path);
        }

        public static bool wasLastProfile(string profile = null)
        {
            if (getNextProfilePath(profile) == getFirstProfilePath())
            {
                ProfileSwitcher.Log("All profiles completed, logging out!.");
                if (!ZetaDia.Me.IsInTown)
                {
                    ZetaDia.Service.Games.LeaveGame();
                    BotMain.PauseFor(System.TimeSpan.FromSeconds(15));
                }
                else
                {
                    ZetaDia.Service.Games.LeaveGame();
                    BotMain.PauseFor(System.TimeSpan.FromSeconds(2));
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void startNewGame()
        {
                if (!ZetaDia.Me.IsInTown)
                {
                    ZetaDia.Service.Games.LeaveGame();
                    BotMain.PauseFor(System.TimeSpan.FromSeconds(15));
                }
                else
                {
                    ZetaDia.Service.Games.LeaveGame();
                    BotMain.PauseFor(System.TimeSpan.FromSeconds(2));
                }
        }

        public static void LoadNextProfile(string name = null, string path = null)
        {
            if (name != null)
            {
                int nextProfileIndex = profiles.IndexOf(profilePath + "\\" + name);
                if (nextProfileIndex == -1)
                {
                    ProfileSwitcher.Log("Profile not found - check your Profile XML!");
                }
                else
                {
                    if (wasLastProfile())
                    { }
                    else
                    {
                        LoadProfile(profiles[nextProfileIndex]);

                    }
                }
            }
            else if (path != null)
            {
                if (tempProfileWasLoaded)
                {
                    tempProfileWasLoaded = false;
                    if (wasLastProfile(preDeathProfile)) {}
                    else
                    {
                        LoadProfile(path);
                    }
                }
                else
                {
                    LoadProfile(path);
                }
            }
            else
            {
                if (tempProfileWasLoaded)
                {
                    tempProfileWasLoaded = false;
                    LoadProfile(getNextProfilePath(preDeathProfile));
                }else{
                    if (wasLastProfile())
                    {

                    }
                    else
                    {
                        LoadProfile(getNextProfilePath());
                    }
                }
            }
            ProfileSwitcher.deathRetries = 0;
        }

        public static void ReloadProfile()
        {
            ProfileSwitcher.startRunTimer();
            ProfileSwitcher.Log("Loading profile: " + GlobalSettings.Instance.LastProfile);
            ProfileManager.Load(GlobalSettings.Instance.LastProfile);
        }

        public static bool LoadTempProfile()
        {
            float min = 150;
            xmlHandler.readXml(new XmlTextReader(preDeathProfile));
            revivePosition = ZetaDia.Actors.Me.Position;
            Vector3 closestWP = new Vector3();
            foreach (Vector3 wp in profileWaypoints)
            {
                if (Vector3.Distance(wp, ZetaDia.Actors.Me.Position) < min)
                {
                    min = Vector3.Distance(wp, revivePosition);
                    closestWP = wp;
                }
            }
            if ((int)min != 150)
            {
                ProfileSwitcher.Log("Closest profile waypoint: " + closestWP.ToString());
                ProfileSwitcher.Log("Distance: " + min);
                ProfileSwitcher.Log("Generating temporary profile...");
                string tmpProfile = xmlHandler.generateTempProfile(closestWP, preDeathProfile, getNextProfilePath(preDeathProfile));
                ProfileSwitcher.Log("Loading temporary profile: " + tmpProfile);
                if (tmpProfile != null)
                {
                    ProfileManager.Load(tmpProfile);
                }
                else
                {
                    ProfileSwitcher.Log("Something went wrong while profile generation - reloading actual profile instead!");
                    ReloadProfile();
                }
                tempProfileWasLoaded = true;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}