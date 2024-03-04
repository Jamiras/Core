using System;

namespace Jamiras.Components
{
    /// <summary>
    /// Defines a structure that can be used to represent a semantic software version.
    /// </summary>
    public readonly struct SoftwareVersion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareVersion"/> struct.
        /// </summary>
        /// <param name="major">The major part of the version.</param>
        /// <param name="minor">The minor part of the version.</param>
        public SoftwareVersion(uint major, uint minor)
        {
            _version = (major << MajorShift) | (minor << MinorShift);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareVersion"/> struct.
        /// </summary>
        /// <param name="major">The major part of the version.</param>
        /// <param name="minor">The minor part of the version.</param>
        /// <param name="patch">The patch part of the version.</param>
        public SoftwareVersion(uint major, uint minor, uint patch)
        {
            _version = (major << MajorShift) | (minor << MinorShift) | (patch << PatchShift);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareVersion"/> struct.
        /// </summary>
        /// <param name="major">The major part of the version.</param>
        /// <param name="minor">The minor part of the version.</param>
        /// <param name="patch">The patch part of the version.</param>
        /// <param name="revision">The revision part of the version.</param>
        public SoftwareVersion(uint major, uint minor, uint patch, uint revision)
        {
            _version = (major << MajorShift) | (minor << MinorShift) | (patch << PatchShift) | revision;
        }

        private readonly uint _version;

        private static readonly int MajorBits = 4;
        private static readonly int MinorBits = 8;
        private static readonly int PatchBits = 5;
        private static readonly int RevisionBits = 32 - MajorBits - MinorBits - PatchBits;

        private static readonly int PatchShift = RevisionBits;
        private static readonly int MinorShift = PatchShift + PatchBits;
        private static readonly int MajorShift = MinorShift + MinorBits;

        private static readonly int MajorMask = ((1 << MajorBits) - 1);
        private static readonly int MinorMask = ((1 << MinorBits) - 1);
        private static readonly int PatchMask = ((1 << PatchBits) - 1);
        private static readonly int RevisionMask = ((1 << RevisionBits) - 1);

        /// <summary>
        /// Gets the major part of the version.
        /// </summary>
        public uint Major
        {
            get { return (uint)((_version >> MajorShift) & MajorMask); }
        }

        /// <summary>
        /// Gets the minor part of the version.
        /// </summary>
        public uint Minor
        {
            get { return (uint)((_version >> MinorShift) & MinorMask); }
        }

        /// <summary>
        /// Gets the patch part of the version.
        /// </summary>
        public uint Patch
        {
            get { return (uint)((_version >> PatchShift) & PatchMask); }
        }

        /// <summary>
        /// Gets the revision part of the version.
        /// </summary>
        public uint Revision
        {
            get { return (uint)(_version & RevisionMask); }
        }

        /// <summary>
        /// Gets the value of the <see cref="SoftwareVersion"/> in the standard format "{major}.{minor}",
        /// "{major}.{minor}.{patch}" or "{major}.{minor}.{patch}.{revision}"
        /// </summary>
        public override string ToString()
        {
            var major = Major;
            var minor = Minor;
            var patch = Patch;
            var revision = Revision;

            if (revision != 0)
                return String.Format("{0}.{1}.{2}.{3}", major, minor, patch, revision);

            if (patch != 0)
                return String.Format("{0}.{1}.{2}", major, minor, patch);

            return String.Format("{0}.{1}", major, minor);
        }

        /// <summary>
        /// Attempts to convert a string into a <see cref="SoftwareVersion"/>. Supported formats are 
        /// "{major}.{minor}", "{major}.{minor}.{patch}" and "{major}.{minor}.{patch}.{revision}"
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <param name="version">The version to populate.</param>
        /// <returns><c>true</c> if the version was populated, <c>false</c> if not.</returns>
        public static bool TryParse(string input, out SoftwareVersion version)
        {
            uint major;
            uint minor = 0;
            uint patch = 0;
            uint revision = 0;
            bool success = true;

            var parts = input.Split('.');
            success &= uint.TryParse(parts[0], out major);
            success &= (major <= MajorMask);

            if (parts.Length < 2 || parts.Length > 4)
            {
                success = false;
            }
            else
            {
                success &= uint.TryParse(parts[1], out minor);
                success &= (minor <= MinorMask);

                if (parts.Length > 2)
                {
                    success &= uint.TryParse(parts[2], out patch);
                    success &= (patch <= PatchMask);

                    if (parts.Length > 3)
                    {
                        success &= uint.TryParse(parts[3], out revision);
                        success &= (revision <= RevisionMask);
                    }
                }
            }

            version = new SoftwareVersion(major, minor, patch, revision);
            return success;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>
        /// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name="obj">Another object to compare to.</param>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (obj is not SoftwareVersion)
                return false;

            return ((SoftwareVersion)obj)._version == _version;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return (int)_version;
        }

        /// <summary>
        /// Determines if one <see cref="SoftwareVersion"/> is semantically the same as another.
        /// </summary>
        public static bool operator ==(SoftwareVersion left, SoftwareVersion right)
        {
            return left._version == right._version;
        }

        /// <summary>
        /// Determines if one <see cref="SoftwareVersion"/> is semantically different from another.
        /// </summary>
        public static bool operator !=(SoftwareVersion left, SoftwareVersion right)
        {
            return left._version != right._version;
        }

        /// <summary>
        /// Determines if one <see cref="SoftwareVersion"/> is semantically before another.
        /// </summary>
        public static bool operator<(SoftwareVersion left, SoftwareVersion right)
        {
            return left._version < right._version;
        }

        /// <summary>
        /// Determines if one <see cref="SoftwareVersion"/> is semantically after another.
        /// </summary>
        public static bool operator >(SoftwareVersion left, SoftwareVersion right)
        {
            return left._version > right._version;
        }

        /// <summary>
        /// Determines if one <see cref="SoftwareVersion"/> is semantically before or the same as another.
        /// </summary>
        public static bool operator <=(SoftwareVersion left, SoftwareVersion right)
        {
            return left._version <= right._version;
        }

        /// <summary>
        /// Determines if one <see cref="SoftwareVersion"/> is semantically after or the same as another.
        /// </summary>
        public static bool operator >=(SoftwareVersion left, SoftwareVersion right)
        {
            return left._version >= right._version;
        }
    }
}
