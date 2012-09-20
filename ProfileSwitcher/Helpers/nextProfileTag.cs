using System;
using Zeta;
using Zeta.XmlEngine;
using Zeta.TreeSharp;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using ProfileSwitcher.Helpers;

namespace ProfileSwitcher.Helpers
{
    [XmlElement("nextProfile")]
		public class nextProfileTag : ProfileBehavior
		{
			private bool m_IsDone = false;
			public override bool IsDone
			{
				get { return m_IsDone; }
			}

			[XmlAttribute("name")]
			public string name
			{
				get;
				set;
			}

            [XmlAttribute("path")]
            public string path
            {
                get;
                set;
            }

			protected override Zeta.TreeSharp.Composite CreateBehavior()
			{
				return new Zeta.TreeSharp.Action((ret) =>
				{
                    if (name != null)
                    {
                        profileHandler.LoadNextProfile(name);
                        m_IsDone = true;
                    }
                    if (path != null)
                    {
                        profileHandler.LoadNextProfile(null, path);
                        m_IsDone = true;
                    }
                    if (name == null && path == null)
                    {
                        profileHandler.LoadNextProfile();
                        m_IsDone = true;
                    }
				});
			}

			public override void ResetCachedDone()
			{
				m_IsDone = false;
				base.ResetCachedDone();
			}
		}

    [XmlElement("Continue")]
    public class ContinueTag : ProfileBehavior
    {
        private bool m_IsDone = false;

        public override bool IsDone
        {
            get { return m_IsDone; }
        }

        [XmlAttribute("profile")]
        public string ProfileName { get; set; }

        [XmlAttribute("exitgame")]
        public string ExitGame { get; set; }


        protected override Zeta.TreeSharp.Composite CreateBehavior()
        {
            return new Zeta.TreeSharp.Action((ret) =>
            {
                if (ProfileName != null)
                {
                    if (ProfileSwitcher.profileRandomization == false)
                    {
                        profileHandler.LoadNextProfile(ProfileName);
                    }
                    else
                    {
                        profileHandler.LoadNextProfile();
                    }
                    m_IsDone = true;
                }
                if (ProfileName == null)
                {
                    profileHandler.LoadNextProfile();
                    m_IsDone = true;
                }
            });
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }
    }
}