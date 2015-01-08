using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Ultima
{
    public class Gumps
    {
        private static readonly FileIndex m_FileIndex = new FileIndex("Gumpidx.mul", "Gumpart.mul", 0x10000, 12);

        private static byte[] m_PixelBuffer;
        private static byte[] m_StreamBuffer;
        private static byte[] m_ColorTable;

        public static FileIndex FileIndex { get { return m_FileIndex; } }

        public unsafe static Bitmap GetGump(int index, Hue hue, bool onlyHueGrayPixels)
        {
            int length, extra;
            bool patched;
            Stream stream = m_FileIndex.Seek(index, out length, out extra, out patched);

            if (stream == null)
                return null;

            int width = (extra >> 16) & 0xFFFF;
            int height = extra & 0xFFFF;

            if (width <= 0 || height <= 0)
                return null;

            int bytesPerLine = width << 1;
            int bytesPerStride = (bytesPerLine + 3) & ~3;
            int bytesForImage = height * bytesPerStride;

            int pixelsPerStride = (width + 1) & ~1;
            int pixelsPerStrideDelta = pixelsPerStride - width;

            byte[] pixelBuffer = m_PixelBuffer;

            if (pixelBuffer == null || pixelBuffer.Length < bytesForImage)
                m_PixelBuffer = pixelBuffer = new byte[(bytesForImage + 2047) & ~2047];

            byte[] streamBuffer = m_StreamBuffer;

            if (streamBuffer == null || streamBuffer.Length < length)
                m_StreamBuffer = streamBuffer = new byte[(length + 2047) & ~2047];

            byte[] colorTable = m_ColorTable;

            if (colorTable == null)
                m_ColorTable = colorTable = new byte[128];

            stream.Read(streamBuffer, 0, length);

            fixed (short* psHueColors = hue.Colors)
            {
                fixed (byte* pbStream = streamBuffer)
                {
                    fixed (byte* pbPixels = pixelBuffer)
                    {
                        fixed (byte* pbColorTable = colorTable)
                        {
                            ushort* pHueColors = (ushort*)psHueColors;
                            ushort* pHueColorsEnd = pHueColors + 32;

                            ushort* pColorTable = (ushort*)pbColorTable;

                            ushort* pColorTableOpaque = pColorTable;

                            while (pHueColors < pHueColorsEnd)
                                *pColorTableOpaque++ = *pHueColors++;

                            ushort* pPixelDataStart = (ushort*)pbPixels;

                            int* pLookup = (int*)pbStream;
                            int* pLookupEnd = pLookup + height;
                            int* pPixelRleStart = pLookup;
                            int* pPixelRle;

                            ushort* pPixel = pPixelDataStart;
                            ushort* pRleEnd;
                            ushort* pPixelEnd = pPixel + width;

                            ushort color, count;

                            if (onlyHueGrayPixels)
                            {
                                while (pLookup < pLookupEnd)
                                {
                                    pPixelRle = pPixelRleStart + *pLookup++;
                                    pRleEnd = pPixel;

                                    while (pPixel < pPixelEnd)
                                    {
                                        color = *(ushort*)pPixelRle;
                                        count = *(1 + (ushort*)pPixelRle);
                                        ++pPixelRle;

                                        pRleEnd += count;

                                        if (color != 0 && (color & 0x1F) == ((color >> 5) & 0x1F) && (color & 0x1F) == ((color >> 10) & 0x1F))
                                            color = pColorTable[color >> 10];
                                        else if (color != 0)
                                            color ^= 0x8000;

                                        while (pPixel < pRleEnd)
                                            *pPixel++ = color;
                                    }

                                    pPixel += pixelsPerStrideDelta;
                                    pPixelEnd += pixelsPerStride;
                                }
                            }
                            else
                            {
                                while (pLookup < pLookupEnd)
                                {
                                    pPixelRle = pPixelRleStart + *pLookup++;
                                    pRleEnd = pPixel;

                                    while (pPixel < pPixelEnd)
                                    {
                                        color = *(ushort*)pPixelRle;
                                        count = *(1 + (ushort*)pPixelRle);
                                        ++pPixelRle;

                                        pRleEnd += count;

                                        if (color != 0)
                                            color = pColorTable[color >> 10];

                                        while (pPixel < pRleEnd)
                                            *pPixel++ = color;
                                    }

                                    pPixel += pixelsPerStrideDelta;
                                    pPixelEnd += pixelsPerStride;
                                }
                            }

                            return new Bitmap(width, height, bytesPerStride, PixelFormat.Format16bppArgb1555, (IntPtr)pPixelDataStart);
                        }
                    }
                }
            }
        }

        public unsafe static Bitmap GetGump(int index)
        {
            int length, extra;
            bool patched;
            Stream stream = m_FileIndex.Seek(index, out length, out extra, out patched);

            if (stream == null)
                return null;

            int width = (extra >> 16) & 0xFFFF;
            int height = extra & 0xFFFF;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format16bppArgb1555);
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
            BinaryReader bin = new BinaryReader(stream);

            int[] lookups = new int[height];
            int start = (int)bin.BaseStream.Position;

            for (int i = 0; i < height; ++i)
                lookups[i] = start + (bin.ReadInt32() * 4);

            ushort* line = (ushort*)bd.Scan0;
            int delta = bd.Stride >> 1;

            for (int y = 0; y < height; ++y, line += delta)
            {
                bin.BaseStream.Seek(lookups[y], SeekOrigin.Begin);

                ushort* cur = line;
                ushort* end = line + bd.Width;

                while (cur < end)
                {
                    ushort color = bin.ReadUInt16();
                    ushort* next = cur + bin.ReadUInt16();

                    if (color == 0)
                    {
                        cur = next;
                    }
                    else
                    {
                        color ^= 0x8000;

                        while (cur < next)
                            *cur++ = color;
                    }
                }
            }

            bmp.UnlockBits(bd);

            return bmp;
        }
    }
}