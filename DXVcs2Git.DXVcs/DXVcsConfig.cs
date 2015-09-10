using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.DXVcs {
    public class DXVcsConfig {
        public string AuxPath { get; set; }
    }

    public static class DefaultConfig {
        static DXVcsConfig config;
        public static DXVcsConfig Config { get { return config ?? (config = CreateDefaultConfig()); } }
        static DXVcsConfig CreateDefaultConfig() {
            return new DXVcsConfig() { AuxPath = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService" };
        }
    }
}
