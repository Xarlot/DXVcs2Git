using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.GitTools.ViewModels {
    public class Options {
        public string Token { get; set; }

        public static Options GenerateDefault() {
            return new Options();
        }
    }
}
