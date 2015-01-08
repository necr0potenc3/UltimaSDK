using System;

namespace Ultima
{
	public class HuedTileList
	{
		private HuedTile[] m_Tiles;
		private int m_Count;

		public HuedTileList()
		{
			m_Tiles = new HuedTile[8];
			m_Count = 0;
		}

		public int Count
		{
			get
			{
				return m_Count;
			}
		}

		public void Add( short id, short hue, sbyte z )
		{
			if ( (m_Count + 1) > m_Tiles.Length )
			{
				HuedTile[] old = m_Tiles;
				m_Tiles = new HuedTile[old.Length * 2];

				for ( int i = 0; i < old.Length; ++i )
					m_Tiles[i] = old[i];
			}

			m_Tiles[m_Count++].Set( id, hue, z );
		}

		public HuedTile[] ToArray()
		{
			HuedTile[] tiles = new HuedTile[m_Count];

			for ( int i = 0; i < m_Count; ++i )
				tiles[i] = m_Tiles[i];

			m_Count = 0;

			return tiles;
		}
	}

	public class TileList
	{
		private Tile[] m_Tiles;
		private int m_Count;

		public TileList()
		{
			m_Tiles = new Tile[8];
			m_Count = 0;
		}

		public int Count
		{
			get
			{
				return m_Count;
			}
		}

		public void Add( short id, sbyte z )
		{
			if ( (m_Count + 1) > m_Tiles.Length )
			{
				Tile[] old = m_Tiles;
				m_Tiles = new Tile[old.Length * 2];

				for ( int i = 0; i < old.Length; ++i )
					m_Tiles[i] = old[i];
			}

			m_Tiles[m_Count++].Set( id, z );
		}

		public Tile[] ToArray()
		{
			Tile[] tiles = new Tile[m_Count];

			for ( int i = 0; i < m_Count; ++i )
				tiles[i] = m_Tiles[i];

			m_Count = 0;

			return tiles;
		}
	}
}