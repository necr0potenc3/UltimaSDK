using System.IO;

namespace Ultima
{
    public class FileIndex
    {
        private readonly Entry3D[] m_Index;
        private readonly string m_IndexPath;
        private readonly string m_MulPath;

        private Stream m_Stream;

        public Entry3D[] Index { get { return m_Index; } }
        public Stream Stream { get { return m_Stream; } }
        public string IndexPath { get { return m_IndexPath; } }
        public string MulPath { get { return m_MulPath; } }

        public Stream Seek(int index, out int length, out int extra, out bool patched)
        {
            if (index < 0 || index >= m_Index.Length)
            {
                length = extra = 0;
                patched = false;
                return null;
            }

            Entry3D e = m_Index[index];

            if (e.lookup < 0)
            {
                length = extra = 0;
                patched = false;
                return null;
            }

            length = e.length & 0x7FFFFFFF;
            extra = e.extra;

            if ((e.length & (1 << 31)) != 0)
            {
                patched = true;

                Verdata.Stream.Seek(e.lookup, SeekOrigin.Begin);
                return Verdata.Stream;
            }
            
            if (m_Stream == null)
            {
                length = extra = 0;
                patched = false;
                return null;
            }

            patched = false;

            InvalidateFileStream();

            m_Stream.Seek(e.lookup, SeekOrigin.Begin);
            return m_Stream;
        }

        private void InvalidateFileStream()
        {
            if (m_Stream == null || !m_Stream.CanRead || !m_Stream.CanSeek)
                m_Stream = new FileStream(m_MulPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public FileIndex(string idxFile, string mulFile, int length, int file)
        {
            m_Index = new Entry3D[length];

            m_IndexPath = Client.GetFilePath(idxFile);
            m_MulPath = Client.GetFilePath(mulFile);

            if (m_IndexPath != null && m_MulPath != null)
            {
                using (FileStream index = new FileStream(m_IndexPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BinaryReader bin = new BinaryReader(index);
                    m_Stream = new FileStream(m_MulPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                    int count = (int)(index.Length / 12);

                    for (int i = 0; i < count && i < length; ++i)
                    {
                        m_Index[i].lookup = bin.ReadInt32();
                        m_Index[i].length = bin.ReadInt32();
                        m_Index[i].extra = bin.ReadInt32();
                    }

                    for (int i = count; i < length; ++i)
                    {
                        m_Index[i].lookup = -1;
                        m_Index[i].length = -1;
                        m_Index[i].extra = -1;
                    }
                }
            }

            Entry5D[] patches = Verdata.Patches;

            for (int i = 0; i < patches.Length; ++i)
            {
                Entry5D patch = patches[i];

                if (patch.file == file && patch.index >= 0 && patch.index < length)
                {
                    m_Index[patch.index].lookup = patch.lookup;
                    m_Index[patch.index].length = patch.length | (1 << 31);
                    m_Index[patch.index].extra = patch.extra;
                }
            }
        }
    }

    public struct Entry3D
    {
        public int lookup;
        public int length;
        public int extra;
    }
}