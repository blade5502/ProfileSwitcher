using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.CommonBot.Settings;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Profile.Common;
using System.IO;

namespace ProfileSwitcher.Helpers
{
    public static class checkpointHandler
    {
        public static string CurrentCheckpoint { get; set; }

        public static void SetCheckpoint(string checkpointProfile, Vector3 checkpointPosition)
        {
            if (!string.IsNullOrEmpty(checkpointProfile) && checkpointPosition != null)
            {
                if (File.Exists(checkpointProfile))
                {
                    CurrentCheckpoint = checkpointProfile;
                    ProfileSwitcher.Log("Reached Checkpoint! Death profile will be: " + checkpointProfile);
                }
                else
                {
                    ProfileSwitcher.Log("Checkpoint file: " + checkpointProfile + " does not exist!");
                }

            }
            else
            {
                ProfileSwitcher.Log("Error: Checkpoint reached but no profile set");
            }
        }
    }
}
