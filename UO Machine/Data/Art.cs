using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows.Interop;
using System.Windows;
using UOMachine.Utility;

namespace UOMachine.Data
{
    public struct ArtData
    {
        public int Width;
        public int Height;
        public Bitmap Bitmap;
        public BitmapSource BitmapSource;
        public Rectangle Bounds;

        public ArtData( int width, int height, Bitmap bmp, BitmapSource bitmapSource, Rectangle bounds )
        {
            this.Width = width;
            this.Height = height;
            this.Bitmap = bmp;
            this.BitmapSource = bitmapSource;
            this.Bounds = bounds;
        }
    }

    public class Art
    {
        private static string m_DataPath;
        private static bool m_isUOPFormat;
        private static Dictionary<int, Entry3D> m_Index;
        private static Dictionary<UInt64, Int32> m_Hashes;
        private static Dictionary<int, ArtData> m_Cache;

        #region Structures
        [StructLayout( LayoutKind.Explicit )]
        private struct Entry3D
        {
            [FieldOffset( 0 )]
            public int Lookup;
            [FieldOffset( 4 )]
            public int Length;
            [FieldOffset( 8 )]
            public int Extra;

            public Entry3D( int lookup, int length, int extra )
            {
                this.Lookup = lookup;
                this.Length = length;
                this.Extra = extra;
            }
        }

        [StructLayout( LayoutKind.Explicit, Size = 28 )]
        private struct UOPFormatHeader
        {
            [FieldOffset( 0 ), MarshalAs( UnmanagedType.ByValArray, SizeConst = 4 )]
            public byte[] Magic;
            [FieldOffset( 4 )]
            public UInt32 Version;
            [FieldOffset( 8 )]
            public UInt32 Signature;
            [FieldOffset( 12 )]
            public Int64 FirstAddress;
            [FieldOffset( 20 )]
            public UInt32 MaximumFiles;
            [FieldOffset( 24 )]
            public UInt32 NumberOfFiles;
        }

        [StructLayout( LayoutKind.Explicit, Size = 12 )]
        private struct UOPBlockHeader
        {
            [FieldOffset( 0 )]
            public Int32 NumberOfFiles;
            [FieldOffset( 4 )]
            public Int64 NextAddress;
        }

        [StructLayout( LayoutKind.Explicit, Pack = 0, Size = 34 )]
        private struct UOPFileHeader
        {
            [FieldOffset( 0 )]
            public Int64 DataHeaderAddress;
            [FieldOffset( 8 )]
            public Int32 Length;
            [FieldOffset( 12 )]
            public Int32 CompressedSize;
            [FieldOffset( 16 )]
            public Int32 DecompressedSize;
            [FieldOffset( 20 )]
            public UInt64 Hash;
            [FieldOffset( 28 )]
            public Int32 Unknown;
            [FieldOffset( 32 )]
            public Int16 IsCompressed;
        }
        #endregion

        public static bool Initialize( string dataPath )
        {
            m_DataPath = dataPath;

            m_Index = new Dictionary<int, Entry3D>();

            if (File.Exists( Path.Combine( dataPath, "artLegacyMUL.uop" ) ))
            {
                m_isUOPFormat = true;
                m_Hashes = new Dictionary<UInt64, int>();
                LoadUOPIndex();
            }
            else
            if (File.Exists( Path.Combine( dataPath, "artidx.mul" ) ))
            {
                m_isUOPFormat = false;
                LoadMULIndex();
            }

            return false;
        }

        private static void LoadMULIndex()
        {
            int entrySize = Marshal.SizeOf( typeof( Entry3D ) );
            byte[] buffer = new byte[entrySize];
            GCHandle pinnedBuffer = GCHandle.Alloc( buffer, GCHandleType.Pinned );
            int index = 0, bytesRead = 0;

            using (FileStream reader = File.Open( Path.Combine( m_DataPath, "artidx.mul" ), FileMode.Open, FileAccess.Read, FileShare.ReadWrite ))
            {
                do
                {
                    bytesRead = reader.Read( buffer, 0, entrySize );
                    m_Index[index++] = (Entry3D) Marshal.PtrToStructure( pinnedBuffer.AddrOfPinnedObject(), typeof( Entry3D ) );

                }
                while (bytesRead > 0);
            }
        }

        private static void LoadUOPIndex()
        {
            UOPFormatHeader formatHeader;

            using (FileStream reader = File.Open( Path.Combine( m_DataPath, "artLegacyMUL.uop" ), FileMode.Open, FileAccess.Read, FileShare.ReadWrite ))
            {
                BinaryReader binaryReader = new BinaryReader( reader );

                formatHeader = reader.ReadStruct<UOPFormatHeader>();

                for (int i = 0; i < formatHeader.NumberOfFiles; i++)
                {
                    string entryName = string.Format( "build/artlegacymul/{0:D8}.tga", i );
                    UInt64 hash = HashFileName( entryName );

                    if (!m_Hashes.ContainsKey( hash ))
                        m_Hashes.Add( hash, i );

                }

                Int64 nextAddress = formatHeader.FirstAddress;

                do
                {
                    UOPBlockHeader blockHeader;

                    reader.Seek( nextAddress, SeekOrigin.Begin );
                    blockHeader = reader.ReadStruct<UOPBlockHeader>();

                    nextAddress = blockHeader.NextAddress;

                    for (int i = 0; i < blockHeader.NumberOfFiles; i++)
                    {
                        UOPFileHeader fileHeader;
                        fileHeader = reader.ReadStruct<UOPFileHeader>();

                        if (fileHeader.DataHeaderAddress == 0)
                            continue;

                        if (m_Hashes.ContainsKey( fileHeader.Hash ))
                        {
                            int index = m_Hashes[fileHeader.Hash];

                            m_Index[index] = new Entry3D( (int) fileHeader.DataHeaderAddress + fileHeader.Length, fileHeader.IsCompressed == 1 ? fileHeader.CompressedSize : fileHeader.DecompressedSize, 0 );
                        }
                    }
                } while (nextAddress > 0);
            }
        }

        public static Rectangle Measure(int itemid)
        {
            ArtData data = GetStatic( itemid );
            return new Rectangle( 0, 0, data.Width, data.Height );
        }

        public unsafe static ArtData GetStatic( int itemId )
        {
            FileStream artFile;
            itemId += 0x4000;

            if (m_Cache == null)
                m_Cache = new Dictionary<int, ArtData>();

            if (m_Cache.ContainsKey( itemId ))
                return m_Cache[itemId];

            string fileName = Path.Combine( m_DataPath, "art.mul" );

            if (m_isUOPFormat)
                fileName = Path.Combine( m_DataPath, "artLegacyMUL.uop" );

            artFile = File.Open( fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );

            if (artFile == null)
                return new ArtData( 0, 0, null, null, new Rectangle(0, 0, 0, 0) );

            Entry3D entry = m_Index[itemId];

            artFile.Seek( entry.Lookup, SeekOrigin.Begin );

            using (BinaryReader reader = new BinaryReader( artFile ))
            {
                reader.ReadInt32();

                int width = reader.ReadInt16();
                int height = reader.ReadInt16();

                if (width <= 0 || height <= 0)
                    return new ArtData( 0, 0, null, null, new Rectangle(0, 0, 0, 0) );

                int[] lookups = new int[height];

                int start = (int) reader.BaseStream.Position + ( height * 2 );

                for (int i = 0; i < height; i++)
                    lookups[i] = ( start + reader.ReadUInt16() * 2 );

                Bitmap bmp = new Bitmap( width, height, PixelFormat.Format16bppArgb1555 );

                BitmapData bd = bmp.LockBits( new Rectangle( 0, 0, width, height ), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555 );

                ushort* line = (ushort*) bd.Scan0;
                int delta = bd.Stride >> 1;

                int smallestX, largestX = 0;
                smallestX = width;
                int smallestY, largestY = 0;
                smallestY = height;

                for (int y = 0; y < height; y++, line += delta)
                {
                    reader.BaseStream.Seek( lookups[y], SeekOrigin.Begin );

                    ushort* cur = line;
                    ushort* end;

                    int xOffset, xRun;
                    int x = 0;

                    if (y > largestY)
                        largestY = y + 1;

                    if (y < smallestY)
                        smallestY = y;

                    while (( ( xOffset = reader.ReadUInt16() ) + ( xRun = reader.ReadUInt16() ) ) != 0)
                    {
                        cur += xOffset;
                        end = cur + xRun;
                        x += xOffset;

                        if (x < smallestX)
                            smallestX = x;

                        while (cur < end)
                        {
                            *cur++ = (ushort) ( reader.ReadUInt16() ^ 0x8000 );
                            x++;
                        }

                        if (x > largestX)
                            largestX = x;
                    }
                }

                bmp.UnlockBits( bd );

                ArtData imageData = new ArtData( width, height, bmp, Imaging.CreateBitmapSourceFromHBitmap( bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight( bmp.Width, bmp.Height ) ), new Rectangle(smallestX, smallestY, largestX, largestY) );
                if (!m_Cache.ContainsKey( itemId ))
                    m_Cache.Add( itemId, imageData );

                return imageData;
            }
        }

        private static ulong HashFileName( string s )
        {
            uint eax, ecx, edx, ebx, esi, edi;
            eax = ecx = edx = ebx = esi = edi = 0;
            ebx = edi = esi = (uint) s.Length + 0xDEADBEEF;
            int i = 0;
            for (i = 0; i + 12 < s.Length; i += 12)
            {
                edi = (uint) ( ( s[i + 7] << 24 ) | ( s[i + 6] << 16 ) | ( s[i + 5] << 8 ) | s[i + 4] ) + edi;
                esi = (uint) ( ( s[i + 11] << 24 ) | ( s[i + 10] << 16 ) | ( s[i + 9] << 8 ) | s[i + 8] ) + esi;
                edx = (uint) ( ( s[i + 3] << 24 ) | ( s[i + 2] << 16 ) | ( s[i + 1] << 8 ) | s[i] ) - esi;
                edx = ( edx + ebx ) ^ ( esi >> 28 ) ^ ( esi << 4 );
                esi += edi;
                edi = ( edi - edx ) ^ ( edx >> 26 ) ^ ( edx << 6 );
                edx += esi;
                esi = ( esi - edi ) ^ ( edi >> 24 ) ^ ( edi << 8 );
                edi += edx;
                ebx = ( edx - esi ) ^ ( esi >> 16 ) ^ ( esi << 16 );
                esi += edi;
                edi = ( edi - ebx ) ^ ( ebx >> 13 ) ^ ( ebx << 19 );
                ebx += esi;
                esi = ( esi - edi ) ^ ( edi >> 28 ) ^ ( edi << 4 );
                edi += ebx;
            }
            if (s.Length - i > 0)
            {
                switch (s.Length - i)
                {
                    case 12:
                        esi += (uint) s[i + 11] << 24;
                        goto case 11;
                    case 11:
                        esi += (uint) s[i + 10] << 16;
                        goto case 10;
                    case 10:
                        esi += (uint) s[i + 9] << 8;
                        goto case 9;
                    case 9:
                        esi += (uint) s[i + 8];
                        goto case 8;
                    case 8:
                        edi += (uint) s[i + 7] << 24;
                        goto case 7;
                    case 7:
                        edi += (uint) s[i + 6] << 16;
                        goto case 6;
                    case 6:
                        edi += (uint) s[i + 5] << 8;
                        goto case 5;
                    case 5:
                        edi += (uint) s[i + 4];
                        goto case 4;
                    case 4:
                        ebx += (uint) s[i + 3] << 24;
                        goto case 3;
                    case 3:
                        ebx += (uint) s[i + 2] << 16;
                        goto case 2;
                    case 2:
                        ebx += (uint) s[i + 1] << 8;
                        goto case 1;
                    case 1:
                        ebx += (uint) s[i];
                        break;
                }
                esi = ( esi ^ edi ) - ( ( edi >> 18 ) ^ ( edi << 14 ) );
                ecx = ( esi ^ ebx ) - ( ( esi >> 21 ) ^ ( esi << 11 ) );
                edi = ( edi ^ ecx ) - ( ( ecx >> 7 ) ^ ( ecx << 25 ) );
                esi = ( esi ^ edi ) - ( ( edi >> 16 ) ^ ( edi << 16 ) );
                edx = ( esi ^ ecx ) - ( ( esi >> 28 ) ^ ( esi << 4 ) );
                edi = ( edi ^ edx ) - ( ( edx >> 18 ) ^ ( edx << 14 ) );
                eax = ( esi ^ edi ) - ( ( edi >> 8 ) ^ ( edi << 24 ) );
                return ( (ulong) edi << 32 ) | eax;
            }
            return ( (ulong) esi << 32 ) | eax;
        }
    }
}
