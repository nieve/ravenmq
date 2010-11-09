﻿using System;
using System.Configuration;

namespace RavenMQ.Config
{
    public class RavenConfiguration : InMemroyRavenConfiguration
    {
        public RavenConfiguration()
        {
            foreach (string setting in ConfigurationManager.AppSettings)
            {
                if (setting.StartsWith("Raven/", StringComparison.InvariantCultureIgnoreCase))
                    Settings[setting] = ConfigurationManager.AppSettings[setting];
            }

            Initialize();
        }

        
    }
}