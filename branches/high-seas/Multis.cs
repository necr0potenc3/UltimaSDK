using System;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace Ultima
{
	public class Multis
	{
		private static MultiComponentList[] m_Components = new MultiComponentList[0x4000];
		public static MultiComponentList[] Cache{ get{ return m_Components; } }

		private static FileIndex m_FileIndex = new FileIndex( "Multi.idx", "Multi.mul", 0x4000, 14 );
		public static FileIndex FileIndex{ get{ return m_FileIndex; } }

		public static MultiComponentList GetComponents( int index )
		{
			MultiComponentList mcl;

			index &= 0x3FFF;

			if ( index >= 0 && index < m_Components.Length )
			{
				mcl = m_Components[index];

				if ( mcl == null )
					m_Components[index] = mcl = Load( index );
			}
			else
			{
				mcl = MultiComponentList.Empty;
			}

			return mcl;
		}

		public static MultiComponentList Load( int index )
		{
			try
			{
				int length, extra;
				bool patched;
				Stream stream = m_FileIndex.Seek( index, out length, out extra, out patched );

				if ( stream == null )
					return MultiComponentList.Empty;

				return new MultiComponentList( new BinaryReader( stream ), length / 12 );
			}
			catch
			{
				return MultiComponentList.Empty;
			}
		}
	}

	public sealed class MultiComponentList
	{
		private Point m_Min, m_Max, m_Center;
		private int m_Width, m_Height;
		private Tile[][][] m_Tiles;

		public static readonly MultiComponentList Empty = new MultiComponentList();

		public Point Min{ get{ return m_Min; } }
		public Point Max{ get{ return m_Max; } }
		public Point Center{ get{ return m_Center; } }
		public int Width{ get{ return m_Width; } }
		public int Height{ get{ return m_Height; } }
		public Tile[][][] Tiles{ get{ return m_Tiles; } }

		private struct MultiTileEntry
		{
			public short m_ItemID;
			public short m_OffsetX, m_OffsetY, m_OffsetZ;
			public int m_Flags;
            public int m_Unknown;
		}

		public Bitmap GetImage()
		{
			if ( m_Width == 0 || m_Height == 0 )
				return null;

			int xMin = 1000, yMin = 1000;
			int xMax =-1000, yMax =-1000;

			for ( int x = 0; x < m_Width; ++x )
			{
				for ( int y = 0; y < m_Height; ++y )
				{
					Tile[] tiles = m_Tiles[x][y];

					for ( int i = 0; i < tiles.Length; ++i )
					{
						Bitmap bmp = Art.GetStatic( tiles[i].ID - 0x4000 );

						if ( bmp == null )
							continue;

						int px = (x - y) * 22;
						int py = (x + y) * 22;

						px -= (bmp.Width / 2);
						py -= tiles[i].Z * 4;
						py -= bmp.Height;

						if ( px < xMin )
							xMin = px;

						if ( py < yMin )
							yMin = py;

						px += bmp.Width;
						py += bmp.Height;

						if ( px > xMax )
							xMax = px;

						if ( py > yMax )
							yMax = py;
					}
				}
			}

			Bitmap canvas = new Bitmap( xMax - xMin, yMax - yMin );
			Graphics gfx = Graphics.FromImage( canvas );

			for ( int x = 0; x < m_Width; ++x )
			{
				for ( int y = 0; y < m_Height; ++y )
				{
					Tile[] tiles = m_Tiles[x][y];

					for ( int i = 0; i < tiles.Length; ++i )
					{
						Bitmap bmp = Art.GetStatic( tiles[i].ID - 0x4000 );

						if ( bmp == null )
							continue;

						int px = (x - y) * 22;
						int py = (x + y) * 22;

						px -= (bmp.Width / 2);
						py -= tiles[i].Z * 4;
						py -= bmp.Height;
						px -= xMin;
						py -= yMin;

						gfx.DrawImageUnscaled( bmp, px, py, bmp.Width, bmp.Height );
					}

					int tx = (x - y) * 22;
					int ty = (x + y) * 22;
					tx -= xMin;
					ty -= yMin;
				}
			}

			gfx.Dispose();

			return canvas;
		}

		public MultiComponentList( BinaryReader reader, int count )
		{
			m_Min = m_Max = Point.Empty;

			MultiTileEntry[] allTiles = new MultiTileEntry[count];

			for ( int i = 0; i < count; ++i )
			{
				allTiles[i].m_ItemID = reader.ReadInt16();
				allTiles[i].m_OffsetX = reader.ReadInt16();
				allTiles[i].m_OffsetY = reader.ReadInt16();
				allTiles[i].m_OffsetZ = reader.ReadInt16();
                allTiles[i].m_Flags = reader.ReadInt32();
                allTiles[i].m_Unknown = reader.ReadInt32();

				MultiTileEntry e = allTiles[i];

				if ( e.m_OffsetX < m_Min.X )
					m_Min.X = e.m_OffsetX;

				if ( e.m_OffsetY < m_Min.Y )
					m_Min.Y = e.m_OffsetY;

				if ( e.m_OffsetX > m_Max.X )
					m_Max.X = e.m_OffsetX;

				if ( e.m_OffsetY > m_Max.Y )
					m_Max.Y = e.m_OffsetY;
			}

			m_Center = new Point( -m_Min.X, -m_Min.Y );
			m_Width = (m_Max.X - m_Min.X) + 1;
			m_Height = (m_Max.Y - m_Min.Y) + 1;

			TileList[][] tiles = new TileList[m_Width][];
			m_Tiles = new Tile[m_Width][][];

			for ( int x = 0; x < m_Width; ++x )
			{
				tiles[x] = new TileList[m_Height];
				m_Tiles[x] = new Tile[m_Height][];

				for ( int y = 0; y < m_Height; ++y )
					tiles[x][y] = new TileList();
			}

			for ( int i = 0; i < allTiles.Length; ++i )
			{
				int xOffset = allTiles[i].m_OffsetX + m_Center.X;
				int yOffset = allTiles[i].m_OffsetY + m_Center.Y;

				tiles[xOffset][yOffset].Add( (short)((allTiles[i].m_ItemID & 0x3FFF) + 0x4000), (sbyte)allTiles[i].m_OffsetZ );
			}

			for ( int x = 0; x < m_Width; ++x )
			{
				for ( int y = 0; y < m_Height; ++y )
				{
					m_Tiles[x][y] = tiles[x][y].ToArray();

					if ( m_Tiles[x][y].Length > 1 )
						Array.Sort( m_Tiles[x][y] );
				}
			}
		}

		private MultiComponentList()
		{
			m_Tiles = new Tile[0][][];
		}
	}
}