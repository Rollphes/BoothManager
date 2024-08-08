using System;

namespace io.github.rollphes.epmanager.library {
    internal class Package {
        internal long Id { get; private set; }
        internal string Name { get; private set; }
        internal Uri DownloadUrl { get; private set; }
    }
}
