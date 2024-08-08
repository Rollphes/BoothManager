using System;
using System.IO;

namespace io.github.rollphes.epmanager {
    internal static class Core {
        internal static readonly string RoamingDirectoryPath = "\\\\?\\" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EPManager");
    }
}
