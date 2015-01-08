using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
// ascii text support written by arul
namespace Ultima
{
    //QUERY: Does this really need to be exposed? Shouldnt this be a child class of ASCIIText?
	public sealed class ASCIIFont
	{
		private int m_Height;
		private Bitmap[] m_Characters;

		public int Height { get { return m_Height; } set { m_Height = value; } }
		public Bitmap[] Characters { get { return m_Characters; } set { m_Characters = value; } }

		public ASCIIFont()
		{
			Height		= 0;
			Characters	= new Bitmap[ 224 ];
		}

		public Bitmap GetBitmap( char character )
		{
			return m_Characters[ ( ( ( ( (int)character ) - 0x20 ) & 0x7FFFFFFF ) % 224 ) ];
		}

		public int GetWidth( string text )
		{
			if( text == null || text.Length == 0 ) { return 0; }

			int width = 0;

			for( int i = 0; i < text.Length; ++i )
			{
				width += GetBitmap( text[ i ] ).Width;
			}

			return width;
		}

		public static ASCIIFont GetFixed( int font )
		{
			if( font < 0 || font > 9 )
			{
				return ASCIIText.Fonts[ 3 ];
			}

			return ASCIIText.Fonts[ font ];
		}
	}

	public static class ASCIIText
    {
        private static ASCIIFont[] m_Fonts = new ASCIIFont[10];

        //QUERY: Does this really need to be exposed?
        public static ASCIIFont[] Fonts { get { return ASCIIText.m_Fonts; } set { ASCIIText.m_Fonts = value; } }

		static ASCIIText()
		{
			string path = Client.GetFilePath( "fonts.mul" );

			if( path != null )
			{
				using( BinaryReader reader = new BinaryReader( new FileStream( path, FileMode.Open ) ) )
				{
					for( int i = 0; i < 10; ++i )
					{
						m_Fonts[ i ] = new ASCIIFont();

						byte header = reader.ReadByte();

						for( int k = 0; k < 224; ++k )
						{
							byte width = reader.ReadByte();
							byte height = reader.ReadByte();
							reader.ReadByte(); // delimeter?

							if( width > 0 && height > 0 )
							{
								if( height > m_Fonts[ i ].Height && k < 96 )
								{
									m_Fonts[ i ].Height = height;
								}

								Bitmap bmp = new Bitmap( width, height );

								for( int y = 0; y < height; ++y )
								{
									for( int x = 0; x < width; ++x )
									{
										short pixel = (short)( reader.ReadByte() | ( reader.ReadByte() << 8 ) );

										if( pixel != 0 )
										{
											bmp.SetPixel( x, y, Color.FromArgb( ( pixel & 0x7C00 ) >> 7, ( pixel & 0x3E0 ) >> 2, ( pixel & 0x1F ) << 3 ) );
										}
									}
								}

								m_Fonts[ i ].Characters[ k ] = bmp;
							}
						}
					}
				}
			}
		}

		public unsafe static Bitmap DrawText( int fontId, string text, short hueId )
		{
			ASCIIFont font = ASCIIFont.GetFixed( fontId );

			Bitmap result		= 
				new Bitmap( font.GetWidth( text ), font.Height );
			BitmapData surface	= 
				result.LockBits( new Rectangle( 0, 0, result.Width, result.Height ), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb );

			int dx				= 0;

			for( int i = 0; i < text.Length; ++i )
			{
				Bitmap bmp		=
					font.GetBitmap( text[ i ] );				
				BitmapData chr	= 
					bmp.LockBits( new Rectangle( 0, 0, bmp.Width, bmp.Height ), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb );

				for( int dy = 0; dy < chr.Height; ++dy )
				{
					int* src	= 
						( (int*)chr.Scan0 ) + ( chr.Stride * dy );
					int* dest  = 
						( ( (int*)surface.Scan0 ) + ( surface.Stride * ( dy + ( font.Height - chr.Height ) ) ) ) + ( dx << 2 );

					for( int k = 0; k < chr.Width; ++k )
						*dest++ = *src++;
					
				}

				dx += chr.Width;
				bmp.UnlockBits( chr );
			}

			result.UnlockBits( surface );

			hueId = (short)(( hueId & 0x3FFF ) - 1);
			if( hueId >= 0 && hueId < Hues.List.Length )
			{
				Hue hueObject = Hues.List[ hueId ];

				if( hueObject != null )
				{
					hueObject.ApplyTo( result, ( ( hueId & 0x8000 ) == 0 ) );
				}
			}

			return result;
		}
	}
}
