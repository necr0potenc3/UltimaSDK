using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Win32.SafeHandles;

namespace Ultima
{
	public class Map
	{
		private TileMatrix m_Tiles;
		private int m_FileIndex, m_MapID;
		private int m_Width, m_Height;

		private static short[] m_Colors;

		public static short[] Colors{ get{ return m_Colors; } set{ m_Colors = value; } }

		public static readonly Map Felucca = new Map( 0, 0, 6144, 4096 );
		public static readonly Map Trammel = new Map( 0, 1, 6144, 4096 );
		public static readonly Map Ilshenar = new Map( 2, 2, 2304, 1600 );
		public static readonly Map Malas = new Map( 3, 3, 2560, 2048 );
		public static readonly Map Tokuno = new Map( 4, 4, 1448, 1448 );

		private Map( int fileIndex, int mapID, int width, int height )
		{
			m_FileIndex = fileIndex;
			m_MapID = mapID;
			m_Width = width;
			m_Height = height;
		}

		public bool LoadedMatrix
		{
			get
			{
				return ( m_Tiles != null );
			}
		}

		public TileMatrix Tiles
		{
			get
			{
				if ( m_Tiles == null )
					m_Tiles = new TileMatrix( m_FileIndex, m_MapID, m_Width, m_Height );

				return m_Tiles;
			}
		}

		public int Width
		{
			get{ return m_Width; }
		}

		public int Height
		{
			get{ return m_Height; }
		}

		public Bitmap GetImage( int x, int y, int width, int height )
		{
			return GetImage( x, y, width, height, true );
		}

		public Bitmap GetImage( int x, int y, int width, int height, bool statics )
		{
			Bitmap bmp = new Bitmap( width << 3, height << 3, PixelFormat.Format16bppRgb555 );

			GetImage( x, y, width, height, bmp, statics );

			return bmp;
		}

		private short[][][] m_Cache;
		private short[][][] m_Cache_NoStatics;
		private short[] m_Black;

		private short[] GetRenderedBlock( int x, int y, bool statics )
		{
			TileMatrix matrix = this.Tiles;

			int bw = matrix.BlockWidth;
			int bh = matrix.BlockHeight;

			if ( x < 0 || y < 0 || x >= bw || y >= bh )
			{
				if ( m_Black == null )
					m_Black = new short[64];

				return m_Black;
			}

			short[][][] cache = ( statics ? m_Cache : m_Cache_NoStatics );

			if ( cache == null )
			{
				if ( statics )
					m_Cache = cache = new short[m_Tiles.BlockHeight][][];
				else
					m_Cache_NoStatics = new short[m_Tiles.BlockHeight][][];
			}

			if ( cache[y] == null )
				cache[y] = new short[m_Tiles.BlockWidth][];

			short[] data = cache[y][x];

			if ( data == null )
				cache[y][x] = data = RenderBlock( x, y, statics );

			return data;
		}

		private unsafe short[] RenderBlock( int x, int y, bool drawStatics )
		{
			short[] data = new short[64];

			Tile[] tiles = m_Tiles.GetLandBlock( x, y );

			fixed ( short *pColors = m_Colors )
			{
				fixed ( int *pHeight = TileData.HeightTable )
				{
					fixed ( Tile *ptTiles = tiles )
					{
						Tile *pTiles = ptTiles;

						fixed ( short *pData = data )
						{
							short *pvData = pData;

							if ( drawStatics )
							{
								HuedTile[][][] statics = drawStatics ? m_Tiles.GetStaticBlock( x, y ) : null;

								for ( int k = 0, v = 0; k < 8; ++k, v += 8 )
								{
									for ( int p = 0; p < 8; ++p )
									{
										int highTop = -255;
										int highZ = -255;
										int highID = 0;
										int highHue = 0;
										int z, top;

										HuedTile[] curStatics = statics[p][k];

										if ( curStatics.Length > 0 )
										{
											fixed ( HuedTile *phtStatics = curStatics )
											{
												HuedTile *pStatics = phtStatics;
												HuedTile *pStaticsEnd = pStatics + curStatics.Length;

												while ( pStatics < pStaticsEnd )
												{
													z = pStatics->m_Z;
													top = z + pHeight[pStatics->m_ID & 0x3FFF];

													if ( top > highTop || (z > highZ && top >= highTop) )
													{
														highTop = top;
														highZ = z;
														highID = pStatics->m_ID;
														highHue = pStatics->m_Hue;
													}

													++pStatics;
												}
											}
										}

										top = pTiles->m_Z;

										if ( top > highTop )
										{
											highID = pTiles->m_ID;
											highHue = 0;
										}

										if ( highHue == 0 )
											*pvData++ = pColors[highID];
										else
											*pvData++ = Hues.GetHue( highHue - 1 ).Colors[(pColors[highID] >> 10) & 0x1F];

										++pTiles;
									}
								}
							}
							else
							{
								Tile *pEnd = pTiles + 64;

								while ( pTiles < pEnd )
									*pvData++ = pColors[(pTiles++)->m_ID];
							}
						}
					}
				}
			}

			return data;
		}

		public unsafe void GetImage( int x, int y, int width, int height, Bitmap bmp )
		{
			GetImage( x, y, width, height, bmp, true );
		}

		public unsafe void GetImage( int x, int y, int width, int height, Bitmap bmp, bool statics )
		{
			if ( m_Colors == null )
				LoadColors();

			BitmapData bd = bmp.LockBits( new Rectangle( 0, 0, width<<3, height<<3 ), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb555 );

			int stride = bd.Stride;
			int blockStride = stride << 3;

			byte *pStart = (byte *)bd.Scan0;

			for ( int oy = 0, by = y; oy < height; ++oy, ++by, pStart += blockStride )
			{
				int *pRow0 = (int *)(pStart + (0 * stride));
				int *pRow1 = (int *)(pStart + (1 * stride));
				int *pRow2 = (int *)(pStart + (2 * stride));
				int *pRow3 = (int *)(pStart + (3 * stride));
				int *pRow4 = (int *)(pStart + (4 * stride));
				int *pRow5 = (int *)(pStart + (5 * stride));
				int *pRow6 = (int *)(pStart + (6 * stride));
				int *pRow7 = (int *)(pStart + (7 * stride));

				for ( int ox = 0, bx = x; ox < width; ++ox, ++bx )
				{
					short[] data = GetRenderedBlock( bx, by, statics );

					fixed ( short *pData = data )
					{
						int *pvData = (int *)pData;

						*pRow0++ = *pvData++;
						*pRow0++ = *pvData++;
						*pRow0++ = *pvData++;
						*pRow0++ = *pvData++;

						*pRow1++ = *pvData++;
						*pRow1++ = *pvData++;
						*pRow1++ = *pvData++;
						*pRow1++ = *pvData++;

						*pRow2++ = *pvData++;
						*pRow2++ = *pvData++;
						*pRow2++ = *pvData++;
						*pRow2++ = *pvData++;

						*pRow3++ = *pvData++;
						*pRow3++ = *pvData++;
						*pRow3++ = *pvData++;
						*pRow3++ = *pvData++;

						*pRow4++ = *pvData++;
						*pRow4++ = *pvData++;
						*pRow4++ = *pvData++;
						*pRow4++ = *pvData++;

						*pRow5++ = *pvData++;
						*pRow5++ = *pvData++;
						*pRow5++ = *pvData++;
						*pRow5++ = *pvData++;

						*pRow6++ = *pvData++;
						*pRow6++ = *pvData++;
						*pRow6++ = *pvData++;
						*pRow6++ = *pvData++;

						*pRow7++ = *pvData++;
						*pRow7++ = *pvData++;
						*pRow7++ = *pvData++;
						*pRow7++ = *pvData++;
					}
				}
			}

			bmp.UnlockBits( bd );
		}

		/*public unsafe void GetImage( int x, int y, int width, int height, Bitmap bmp )
		{
			if ( m_Colors == null )
				LoadColors();

			TileMatrix matrix = Tiles;

			BitmapData bd = bmp.LockBits( new Rectangle( 0, 0, width<<3, height<<3 ), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb555 );

			int scanDelta = bd.Stride >> 1;

			short *pvDest = (short *)bd.Scan0;

			fixed ( short *pColors = m_Colors )
			{
				fixed ( int *pHeight = TileData.HeightTable )
				{
					for ( int i = 0; i < width; ++i )
					{
						pvDest = ((short *)bd.Scan0) + (i << 3);

						for ( int j = 0; j < height; ++j )
						{
							Tile[] tiles = matrix.GetLandBlock( x + i, y + j );
							HuedTile[][][] statics = matrix.GetStaticBlock( x + i, y + j );

							for ( int k = 0, v = 0; k < 8; ++k, v += 8 )
							{
								for ( int p = 0; p < 8; ++p )
								{
									int highTop = -255;
									int highZ = -255;
									int highID = 0;
									int highHue = 0;
									int z, top;

									HuedTile[] curStatics = statics[p][k];

									if ( curStatics.Length > 0 )
									{
										fixed ( HuedTile *phtStatics = curStatics )
										{
											HuedTile *pStatics = phtStatics;
											HuedTile *pStaticsEnd = pStatics + curStatics.Length;

											while ( pStatics < pStaticsEnd )
											{
												z = pStatics->m_Z;
												top = z + pHeight[pStatics->m_ID & 0x3FFF];

												if ( top > highTop || (z > highZ && top >= highTop) )
												{
													highTop = top;
													highZ = z;
													highID = pStatics->m_ID;
													highHue = pStatics->m_Hue;
												}

												++pStatics;
											}
										}
									}

									top = tiles[v + p].Z;

									if ( top > highTop )
									{
										highID = tiles[v + p].ID;
										highHue = 0;
									}

									if ( highHue == 0 )
										pvDest[p] = pColors[highID];
									else
										pvDest[p] = Hues.GetHue( highHue - 1 ).Colors[(pColors[highID] >> 10) & 0x1F];
								}

								pvDest += scanDelta;
							}
						}
					}
				}
			}

			bmp.UnlockBits(bd);
		}*/

		private unsafe static void LoadColors()
		{
			m_Colors = new short[0x8000];

			string path = Client.GetFilePath( "radarcol.mul" );

			if ( path == null )
				return;

			fixed ( short *pColors = m_Colors )
			{
				using ( FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.Read ) )
                    NativeMethods._lread(fs.SafeFileHandle, pColors, 0x10000);
			}
		}
	}
}