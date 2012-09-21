using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Zeta;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Settings;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace ProfileSwitcher.Helpers
{
    [XmlElement("Checkpoint")]
    public class CheckpointXmlTag : ProfileBehavior
    {
        #region ProfileBehavior
        private bool m_IsDone = false;
        public override bool IsDone
        {
            get { return m_IsDone; }
        }
        #endregion

        [XmlAttribute("profile")]
        public string ProfileName { get; set; }

        protected override Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action((ret) =>
            {
                checkpointHandler.SetCheckpoint(ProfileName, ZetaDia.Me.Position);
                m_IsDone = true;
            });
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }

           
    }
}
