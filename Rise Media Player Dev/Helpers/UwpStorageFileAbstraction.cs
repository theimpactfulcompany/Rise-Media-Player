using System;
using System.IO;
using Windows.Storage;
using static TagLib.File;

namespace Rise.App.Helpers
{
    /// <summary>
    /// TagLib file abstraction for <see cref="StorageFile"/>s.
    /// </summary>
    public sealed class UwpStorageFileAbstraction : IFileAbstraction
    {
        private readonly StorageFile _file;

        public string Name => _file.Name;

        public Stream ReadStream
            => _file.OpenStreamForReadAsync().Result;

        public Stream WriteStream
            => _file.OpenStreamForWriteAsync().Result;

        public UwpStorageFileAbstraction(StorageFile file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        public void CloseStream(Stream stream)
        {
            stream?.Dispose();
        }
    }
}
