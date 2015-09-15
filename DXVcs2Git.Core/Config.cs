using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.DXVcs {
    public class Config {
        public string AuxPath { get; set; }
        public string TrackConfigPath { get; set; }
    }

    public static class DefaultConfig {
        static Config config;
        public static Config Config { get { return config ?? (config = CreateDefaultConfig()); } }
        static Config CreateDefaultConfig() {
            return new Config() {
                AuxPath = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService",
                TrackConfigPath = "trackconfig_common.config",
            };
        }
    }
}
