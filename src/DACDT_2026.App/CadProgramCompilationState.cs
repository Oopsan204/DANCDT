using System.Threading;

namespace DACDT_2026
{
    internal sealed class CadProgramCompilationState
    {
        private long state;

        public int RequestedVersion
        {
            get
            {
                return UnpackRequested(Volatile.Read(ref state));
            }
        }

        public int PublishedVersion
        {
            get
            {
                return UnpackPublished(Volatile.Read(ref state));
            }
        }

        public int MarkDirty()
        {
            while (true)
            {
                long observed = Volatile.Read(ref state);
                int requestedVersion = UnpackRequested(observed);
                int publishedVersion = UnpackPublished(observed);
                int nextVersion = unchecked(requestedVersion + 1);
                long updated = Pack(nextVersion, publishedVersion);

                if (Interlocked.CompareExchange(ref state, updated, observed) == observed)
                    return nextVersion;
            }
        }

        public bool IsCurrent(int version)
        {
            long snapshot = Volatile.Read(ref state);
            return UnpackRequested(snapshot) == version
                && UnpackPublished(snapshot) == version;
        }

        public bool TryPublish(int version)
        {
            while (true)
            {
                long observed = Volatile.Read(ref state);
                int requestedVersion = UnpackRequested(observed);
                int publishedVersion = UnpackPublished(observed);

                if (requestedVersion != version)
                    return false;

                if (publishedVersion == version)
                    return true;

                long updated = Pack(version, version);
                if (Interlocked.CompareExchange(ref state, updated, observed) == observed)
                    return true;
            }
        }

        private static long Pack(int requestedVersion, int publishedVersion)
        {
            return ((long)requestedVersion << 32) | (uint)publishedVersion;
        }

        private static int UnpackRequested(long packedState)
        {
            return (int)(packedState >> 32);
        }

        private static int UnpackPublished(long packedState)
        {
            return (int)packedState;
        }
    }
}
