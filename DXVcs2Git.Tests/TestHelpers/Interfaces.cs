using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Tests.TestHelpers {
    public interface IPostTestDirectoryRemover {
        void Register(string directoryPath);
    }
}
