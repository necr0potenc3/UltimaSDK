using System;

namespace Ultima
{
	public class LocationPointer
	{
		private int m_PointerX, m_SizeX;
		private int m_PointerY, m_SizeY;
		private int m_PointerZ, m_SizeZ;
		private int m_PointerF, m_SizeF;

		public int PointerX
		{
			get{ return m_PointerX; }
			set{ m_PointerX = value; }
		}

		public int PointerY
		{
			get{ return m_PointerY; }
			set{ m_PointerY = value; }
		}

		public int PointerZ
		{
			get{ return m_PointerZ; }
			set{ m_PointerZ = value; }
		}

		public int PointerF
		{
			get{ return m_PointerF; }
			set{ m_PointerF = value; }
		}

		public int SizeX
		{
			get{ return m_SizeX; }
			set{ m_SizeX = value; }
		}

		public int SizeY
		{
			get{ return m_SizeY; }
			set{ m_SizeY = value; }
		}

		public int SizeZ
		{
			get{ return m_SizeZ; }
			set{ m_SizeZ = value; }
		}

		public int SizeF
		{
			get{ return m_SizeF; }
			set{ m_SizeF = value; }
		}

		public LocationPointer( int ptrX, int ptrY, int ptrZ, int ptrF, int sizeX, int sizeY, int sizeZ, int sizeF )
		{
			m_PointerX = ptrX;
			m_PointerY = ptrY;
			m_PointerZ = ptrZ;
			m_PointerF = ptrF;
			m_SizeX = sizeX;
			m_SizeY = sizeY;
			m_SizeZ = sizeZ;
			m_SizeF = sizeF;
		}
	}
}