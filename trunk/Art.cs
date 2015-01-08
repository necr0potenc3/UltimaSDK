using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Ultima
{
	public sealed class Art
	{
		private static FileIndex m_FileIndex = new FileIndex( "Artidx.mul", "Art.mul", 0x10000, 4 );
		public static FileIndex FileIndex{ get{ return m_FileIndex; } }

		private static Bitmap[] m_Cache = new Bitmap[0x10000];

        //QUERY: Does this really need to be exposed?
		public static Bitmap[] Cache{ get{ return m_Cache; } }

		private Art()
		{
		}

		public static Bitmap GetLand( int index )
		{
            return GetLand(index, true);
        }

        public static Bitmap GetLand(int index, bool cache)
        {
            index &= 0x3FFF;

            if (m_Cache[index] != null)
                return m_Cache[index];

            int length, extra;
            bool patched;
            Stream stream = m_FileIndex.Seek(index, out length, out extra, out patched);

            if (stream == null)
                return null;

            if (cache)
                return m_Cache[index] = LoadLand(stream);
            else
                return LoadLand(stream);
        }

        public static Bitmap GetStatic(int index)
        {
            return GetStatic(index, true);
        }

        public static Bitmap GetStatic(int index, bool cache)
		{
            if (index + 0x4000 > int.MaxValue)
            {
                throw new ArithmeticException("The index must not excede (int.MaxValue - 0x4000)");
            }

            index += 0x4000;
            index &= 0xFFFF;

			if ( m_Cache[index] != null )
				return m_Cache[index];

			int length, extra;
			bool patched;
			Stream stream = m_FileIndex.Seek( index, out length, out extra, out patched );

			if ( stream == null )
				return null;

            if (cache)
                return m_Cache[index] = LoadStatic(stream);
            else
                return LoadStatic(stream);
		}

		public unsafe static void Measure( Bitmap bmp, out int xMin, out int yMin, out int xMax, out int yMax )
		{
			xMin = yMin = 0;
			xMax = yMax = -1;

			if ( bmp == null || bmp.Width <= 0 || bmp.Height <= 0 )
				return;

			BitmapData bd = bmp.LockBits( new Rectangle( 0, 0, bmp.Width, bmp.Height ), ImageLockMode.ReadOnly, PixelFormat.Format16bppArgb1555 );

			int delta = ((bd.Stride) >> 1) - bd.Width;
			int lineDelta = bd.Stride >> 1;

			ushort *pBuffer = (ushort *)bd.Scan0;
			ushort *pLineEnd = pBuffer + bd.Width;
			ushort *pEnd = pBuffer + (bd.Height * lineDelta);

			bool foundPixel = false;

			int x = 0, y = 0;

			while ( pBuffer < pEnd )
			{
				while ( pBuffer < pLineEnd )
				{
					ushort c = *pBuffer++;

					if ( (c & 0x8000) != 0 )
					{
						if ( !foundPixel )
						{
							foundPixel = true;
							xMin = xMax = x;
							yMin = yMax = y;
						}
						else
						{
							if ( x < xMin )
								xMin = x;

							if ( y < yMin )
								yMin = y;

							if ( x > xMax )
								xMax = x;

							if ( y > yMax )
								yMax = y;
						}
					}

					++x;
				}

				pBuffer += delta;
				pLineEnd += lineDelta;
				++y;
				x = 0;
			}

			bmp.UnlockBits( bd );
		}

		private static unsafe Bitmap LoadStatic( Stream stream )
		{
			BinaryReader bin = new BinaryReader( stream );

			bin.ReadInt32();
			int width = bin.ReadInt16();
			int height = bin.ReadInt16();

			if ( width <= 0 || height <= 0 )
				return null;

			int[] lookups = new int[height];

			int start = (int)bin.BaseStream.Position + (height * 2);

			for ( int i = 0; i < height; ++i )
				lookups[i] = (int)(start + (bin.ReadUInt16() * 2));

			Bitmap bmp = new Bitmap( width, height, PixelFormat.Format16bppArgb1555 );
			BitmapData bd = bmp.LockBits( new Rectangle( 0, 0, width, height ), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555 );

			ushort *line = (ushort *)bd.Scan0;
			int delta = bd.Stride >> 1;

			for ( int y = 0; y < height; ++y, line += delta )
			{
				bin.BaseStream.Seek( lookups[y], SeekOrigin.Begin );

				ushort *cur = line;
				ushort *end;

				int xOffset, xRun;

				while ( ((xOffset = bin.ReadUInt16()) + (xRun = bin.ReadUInt16())) != 0 )
				{
					cur += xOffset;
					end = cur + xRun;

					while ( cur < end )
						*cur++ = (ushort)(bin.ReadUInt16() ^ 0x8000);
				}
			}

			bmp.UnlockBits( bd );

			return bmp;
		}

		private static unsafe Bitmap LoadLand( Stream stream )
		{
			Bitmap bmp = new Bitmap( 44, 44, PixelFormat.Format16bppArgb1555 );
			BitmapData bd = bmp.LockBits( new Rectangle( 0, 0, 44, 44 ), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555 );
			BinaryReader bin = new BinaryReader( stream );

			int xOffset = 21;
			int xRun = 2;

			ushort *line = (ushort *)bd.Scan0;
			int delta = bd.Stride >> 1;

			for ( int y = 0; y < 22; ++y, --xOffset, xRun += 2, line += delta )
			{
				ushort *cur = line + xOffset;
				ushort *end = cur + xRun;

				while ( cur < end )
					*cur++ = (ushort)(bin.ReadUInt16() | 0x8000);
			}

			xOffset = 0;
			xRun = 44;

			for ( int y = 0; y < 22; ++y, ++xOffset, xRun -= 2, line += delta )
			{
				ushort *cur = line + xOffset;
				ushort *end = cur + xRun;

				while ( cur < end )
					*cur++ = (ushort)(bin.ReadUInt16() | 0x8000);
			}

			bmp.UnlockBits( bd );

			return bmp;
		}
	}
}