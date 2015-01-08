using System;
using System.IO;
using System.Collections;
using Microsoft.Win32.SafeHandles;

namespace Ultima
{
	public class TileMatrixPatch
	{
		private int m_LandBlocks, m_StaticBlocks;

		public int LandBlocks
		{
			get
			{
				return m_LandBlocks;
			}
		}

		public int StaticBlocks
		{
			get
			{
				return m_StaticBlocks;
			}
		}

		public TileMatrixPatch( TileMatrix matrix, int index )
		{
			string mapDataPath = Client.GetFilePath( "mapdif{0}.mul", index );
			string mapIndexPath = Client.GetFilePath( "mapdifl{0}.mul", index );

			if( mapDataPath != null && mapIndexPath != null )
				m_LandBlocks = PatchLand( matrix, mapDataPath, mapIndexPath );

			string staDataPath = Client.GetFilePath( "stadif{0}.mul", index );
			string staIndexPath = Client.GetFilePath( "stadifl{0}.mul", index );
			string staLookupPath = Client.GetFilePath( "stadifi{0}.mul", index );

			if( staDataPath != null && staIndexPath != null && staLookupPath != null )
				m_StaticBlocks = PatchStatics( matrix, staDataPath, staIndexPath, staLookupPath );
		}

		private unsafe int PatchLand( TileMatrix matrix, string dataPath, string indexPath )
		{
			using ( FileStream fsData = new FileStream( dataPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			{
				using ( FileStream fsIndex = new FileStream( indexPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryReader indexReader = new BinaryReader( fsIndex );

					int count = (int)(indexReader.BaseStream.Length / 4);

					for ( int i = 0; i < count; ++i )
					{
						int blockID = indexReader.ReadInt32();
						int x = blockID / matrix.BlockHeight;
						int y = blockID % matrix.BlockHeight;

						fsData.Seek( 4, SeekOrigin.Current );

						Tile[] tiles = new Tile[64];

						fixed ( Tile *pTiles = tiles )
						{
                            NativeMethods._lread(fsData.SafeFileHandle, pTiles, 192);
						}

						matrix.SetLandBlock( x, y, tiles );
					}

					return count;
				}
			}
		}

		private unsafe int PatchStatics( TileMatrix matrix, string dataPath, string indexPath, string lookupPath )
		{
			using ( FileStream fsData = new FileStream( dataPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			{
				using ( FileStream fsIndex = new FileStream( indexPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					using ( FileStream fsLookup = new FileStream( lookupPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					{
						BinaryReader indexReader = new BinaryReader( fsIndex );
						BinaryReader lookupReader = new BinaryReader( fsLookup );

						int count = (int)(indexReader.BaseStream.Length / 4);

						HuedTileList[][] lists = new HuedTileList[8][];

						for ( int x = 0; x < 8; ++x )
						{
							lists[x] = new HuedTileList[8];

							for ( int y = 0; y < 8; ++y )
								lists[x][y] = new HuedTileList();
						}

						for ( int i = 0; i < count; ++i )
						{
							int blockID = indexReader.ReadInt32();
							int blockX = blockID / matrix.BlockHeight;
							int blockY = blockID % matrix.BlockHeight;

							int offset = lookupReader.ReadInt32();
							int length = lookupReader.ReadInt32();
							lookupReader.ReadInt32(); // Extra

							if ( offset < 0 || length <= 0 )
							{
								matrix.SetStaticBlock( blockX, blockY, matrix.EmptyStaticBlock );
								continue;
							}

							fsData.Seek( offset, SeekOrigin.Begin );

							int tileCount = length / 7;

							StaticTile[] staTiles = new StaticTile[tileCount];

							fixed ( StaticTile *pTiles = staTiles )
							{
                                NativeMethods._lread(fsData.SafeFileHandle, pTiles, length);

								StaticTile *pCur = pTiles, pEnd = pTiles + tileCount;

								while ( pCur < pEnd )
								{
									lists[pCur->m_X & 0x7][pCur->m_Y & 0x7].Add( (short)((pCur->m_ID & 0x3FFF) + 0x4000), pCur->m_Hue, pCur->m_Z );
									++pCur;
								}

								HuedTile[][][] tiles = new HuedTile[8][][];

								for ( int x = 0; x < 8; ++x )
								{
									tiles[x] = new HuedTile[8][];

									for ( int y = 0; y < 8; ++y )
										tiles[x][y] = lists[x][y].ToArray();
								}

								matrix.SetStaticBlock( blockX, blockY, tiles );
							}
						}

						return count;
					}
				}
			}
		}
	}
}