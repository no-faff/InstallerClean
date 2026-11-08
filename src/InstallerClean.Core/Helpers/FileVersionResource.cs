using System.Buffers.Binary;

namespace InstallerClean.Helpers;

/// <summary>
/// Reads the file version a program carries out of its own bytes: the
/// VS_FIXEDFILEINFO block of its RT_VERSION resource, the numbers Explorer shows as
/// "File version".
/// </summary>
/// <remarks>
/// The file at the path is opened through the file system and read, so the answer is
/// always about whatever file is there now. The version read Windows itself provides,
/// given the path a running program was started from, answers with that program's own
/// version, even once its file has been renamed and another put at the path.
///
/// It reads the PE headers, the resource directory and VS_VERSIONINFO as Microsoft's
/// PE format and VS_VERSIONINFO pages lay them out, and uses nothing outside CoreLib.
/// </remarks>
internal static class FileVersionResource
{
    private const ushort RtVersion = 16;
    private const uint PeSignature = 0x00004550;
    private const uint FixedFileInfoSignature = 0xFEEF04BD;

    // VS_VERSIONINFO's three WORDs and its key, "VS_VERSION_INFO" with its terminator
    // in UTF-16, padded to a 32-bit boundary, come before the fixed block.
    private const int FixedFileInfoOffset = 40;
    private const int FixedFileInfoLength = 52;
    private const string VersionInfoKey = "VS_VERSION_INFO\0";

    /// <summary>
    /// The file version <paramref name="path"/> carries, or null where the file cannot
    /// be read, is not a program or library, or carries no version resource. Never
    /// throws for what the file holds.
    /// </summary>
    public static Version? Read(string path)
    {
        try
        {
            return ReadIfThere(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// The file version <paramref name="path"/> carries, or null where no file is there, it
    /// is not a program or library, or it carries no version resource. A file that is there
    /// and cannot be opened or read throws <see cref="IOException"/> or
    /// <see cref="UnauthorizedAccessException"/>, so a caller can tell a file another process
    /// holds from one that carries no version. The file is opened sharing read, write and
    /// delete, so only a handle that shares nothing for reading refuses it.
    /// </summary>
    public static Version? ReadIfThere(string path)
    {
        FileStream file;
        try
        {
            file = FrameworkFileStream.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
        using (file) return Read(file);
    }

    /// <summary>
    /// Whether <paramref name="carried"/> is the release <paramref name="release"/> names: the
    /// first three parts compared, a part left out counting as 0, as the update check compares
    /// versions. False where <paramref name="release"/> is not a version.
    /// </summary>
    public static bool IsRelease(Version carried, string release) =>
        Version.TryParse(release, out var expected)
        && carried.Major == expected.Major && carried.Minor == expected.Minor
        && Math.Max(carried.Build, 0) == Math.Max(expected.Build, 0);

    /// <summary>
    /// The file version the open <paramref name="file"/> carries, or null where it is not a
    /// program or library or carries no version resource, read through that handle, so the
    /// answer is about the file the caller holds. Throws <see cref="IOException"/> where a
    /// read fails.
    /// </summary>
    internal static Version? Read(FileStream file)
    {
        byte[]? At(long offset, int count)
        {
            if (offset < 0 || count < 0 || offset > file.Length - count) return null;
            var buffer = new byte[count];
            file.Position = offset;
            file.ReadExactly(buffer);
            return buffer;
        }

        // The DOS header names where the PE header starts; the COFF header after its
        // signature gives the section count and the optional header's size.
        if (At(0, 64) is not { } dos || dos[0] != 'M' || dos[1] != 'Z') return null;
        long pe = BinaryPrimitives.ReadUInt32LittleEndian(dos.AsSpan(0x3C));
        if (At(pe, 24) is not { } coff || BinaryPrimitives.ReadUInt32LittleEndian(coff) != PeSignature) return null;
        int sectionCount = BinaryPrimitives.ReadUInt16LittleEndian(coff.AsSpan(6));
        int optionalSize = BinaryPrimitives.ReadUInt16LittleEndian(coff.AsSpan(20));

        // The resource table is the third data directory, whose place depends on
        // whether the optional header is PE32 or PE32+.
        if (optionalSize < 2 || At(pe + 24, optionalSize) is not { } optional) return null;
        var (countAt, directoriesAt) = BinaryPrimitives.ReadUInt16LittleEndian(optional) switch
        {
            0x10B => (92, 96),
            0x20B => (108, 112),
            _ => (-1, -1),
        };
        if (countAt < 0 || optionalSize < directoriesAt + 3 * 8
            || BinaryPrimitives.ReadUInt32LittleEndian(optional.AsSpan(countAt)) < 3) return null;
        var resourceRva = BinaryPrimitives.ReadUInt32LittleEndian(optional.AsSpan(directoriesAt + 2 * 8));
        if (resourceRva == 0) return null;
        if (At(pe + 24 + optionalSize, sectionCount * 40) is not { } sections) return null;

        long ToOffset(uint rva)
        {
            for (var i = 0; i < sectionCount; i++)
            {
                var section = sections.AsSpan(i * 40);
                var start = BinaryPrimitives.ReadUInt32LittleEndian(section[12..]);
                var rawSize = BinaryPrimitives.ReadUInt32LittleEndian(section[16..]);
                if (rva >= start && rva - start < rawSize)
                    return rva - start + (long)BinaryPrimitives.ReadUInt32LittleEndian(section[20..]);
            }
            return -1;
        }

        var root = ToOffset(resourceRva);
        if (root < 0) return null;

        // Each level of the resource directory holds named entries and then numbered
        // ones. An entry points at the next level with its top bit set, and at a data
        // entry without it. Answers the offset it points at within the resource
        // section, and whether that is a further level.
        (long Offset, bool IsDirectory)? Entry(long directory, ushort? id)
        {
            if (At(root + directory, 16) is not { } header) return null;
            int count = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(12))
                + BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(14));
            if (At(root + directory + 16, count * 8) is not { } entries) return null;
            for (var i = 0; i < count; i++)
            {
                var name = BinaryPrimitives.ReadUInt32LittleEndian(entries.AsSpan(i * 8));
                var target = BinaryPrimitives.ReadUInt32LittleEndian(entries.AsSpan(i * 8 + 4));
                if (id is null || name == id)
                    return (target & 0x7FFFFFFF, (target & 0x80000000) != 0);
            }
            return null;
        }

        // The type, then the resource's first name, then its first language.
        if (Entry(0, RtVersion) is not { IsDirectory: true } type) return null;
        if (Entry(type.Offset, null) is not { IsDirectory: true } name) return null;
        if (Entry(name.Offset, null) is not { IsDirectory: false } language) return null;
        if (At(root + language.Offset, 16) is not { } data) return null;
        var infoAt = ToOffset(BinaryPrimitives.ReadUInt32LittleEndian(data));
        var infoLength = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4));
        if (infoAt < 0 || infoLength < FixedFileInfoOffset + FixedFileInfoLength) return null;
        if (At(infoAt, FixedFileInfoOffset + FixedFileInfoLength) is not { } info) return null;

        if (BinaryPrimitives.ReadUInt16LittleEndian(info.AsSpan(2)) < FixedFileInfoLength) return null;
        for (var i = 0; i < VersionInfoKey.Length; i++)
            if (BinaryPrimitives.ReadUInt16LittleEndian(info.AsSpan(6 + i * 2)) != VersionInfoKey[i]) return null;
        var fixedInfo = info.AsSpan(FixedFileInfoOffset);
        if (BinaryPrimitives.ReadUInt32LittleEndian(fixedInfo) != FixedFileInfoSignature) return null;

        var high = BinaryPrimitives.ReadUInt32LittleEndian(fixedInfo[8..]);
        var low = BinaryPrimitives.ReadUInt32LittleEndian(fixedInfo[12..]);
        return new Version((int)(high >> 16), (int)(high & 0xFFFF), (int)(low >> 16), (int)(low & 0xFFFF));
    }
}
