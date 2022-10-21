using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace APT_DPC_Tool.Core
{
	
	public enum Lz4Mode
	{
		/// <summary>
		/// The very fast Lz4 algorithm implemtation.
		/// </summary>
		Fast,
		/// <summary>
		/// A High Compression mode that is slower but with a better compression rate.
		/// </summary>
		HighCompression
	}

	public static class Lz4
    {



		[DllImport("x86\\lz4X86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dll_LZ4_compress")]
		private unsafe static extern int LZ4_compress_x86(byte* source, byte* destination, int size);

		[DllImport("x86\\lz4X86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dll_LZ4_compressHC")]
		private unsafe static extern int LZ4_compressHC_x86(byte* source, byte* destination, int size);

		[DllImport("x86\\lz4X86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dll_LZ4_uncompress")]
		private unsafe static extern int LZ4_uncompress_x86(byte* source, byte* destination, int size);

		[DllImport("x64\\lz4X64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dll_LZ4_compress")]
		private unsafe static extern int LZ4_compress_x64(byte* source, byte* destination, int size);

		[DllImport("x64\\lz4X64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dll_LZ4_compressHC")]
		private unsafe static extern int LZ4_compressHC_x64(byte* source, byte* destination, int size);

		[DllImport("x64\\lz4X64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dll_LZ4_uncompress")]
		private unsafe static extern int LZ4_uncompress_x64(byte* source, byte* destination, int size);

		/// <summary>
		/// Native method call (PInvoke) to the LZ4_compress method.
		/// The platform target (X86 or X64) is chosen at runtime. Description:
		/// Compresses 'isize' bytes from 'source' into 'dest'.
		/// Destination buffer must be already allocated,
		/// and must be sized to handle worst cases situations (input data not compressible)
		/// Worst case size evaluation is provided by function LZ4_compressBound().
		/// note : destination buffer must be already allocated. 
		/// To avoid any problem, size it to handle worst cases situations (input data not compressible)
		/// Worst case size evaluation is provided by function LZ4_compressBound() (see "lz4.h")
		/// </summary>
		/// <param name="source">The source (input).</param>
		/// <param name="destination">The destination (output). Its memory must alredy be allocated, use LZ4_compressBound for the size hint.</param>
		/// <param name="size">The source size. Max supported value is ~1.9GB.</param>
		/// <returns>The number of bytes written in buffer destination.</returns>
		public unsafe static int LZ4_compress(byte* source, byte* destination, int size)
		{
			if (IntPtr.Size != 4)
			{
				return LZ4_compress_x64(source, destination, size);
			}
			return LZ4_compress_x86(source, destination, size);
		}

		/// <summary>
		/// Native method call (PInvoke) to the LZ4_compressHC method. 
		/// It provide a High Compression mode that is slower but with a better compression rate.
		/// The platform target (X86 or X64) is chosen at runtime. Description:
		/// Compresses 'isize' bytes from 'source' into 'dest'.
		/// Destination buffer must be already allocated,
		/// and must be sized to handle worst cases situations (input data not compressible)
		/// Worst case size evaluation is provided by function LZ4_compressBound().
		/// note : destination buffer must be already allocated. 
		/// To avoid any problem, size it to handle worst cases situations (input data not compressible)
		/// Worst case size evaluation is provided by function LZ4_compressBound() (see "lz4.h")
		/// </summary>
		/// <param name="source">The source (input).</param>
		/// <param name="destination">The destination (output). Its memory must alredy be allocated, use LZ4_compressBound for the size hint.</param>
		/// <param name="size">The source size. Max supported value is ~1.9GB.</param>
		/// <returns>The number of bytes written in buffer destination.</returns>
		public unsafe static int LZ4_compressHC(byte* source, byte* destination, int size)
		{
			if (IntPtr.Size != 4)
			{
				return LZ4_compressHC_x64(source, destination, size);
			}
			return LZ4_compressHC_x86(source, destination, size);
		}

		/// <summary>
		/// Native method call (PInvoke) to the LZ4_uncompress method.
		/// The platform target (X86 or X64) is chosen at runtime. Description:
		/// note : destination buffer must be already allocated.
		/// its size must be a minimum of 'osize' bytes.
		/// </summary>
		/// <param name="source">The source (input).</param>
		/// <param name="destination">The destination (output). Its memory must alredy be allocated, use LZ4_compressBound for the size hint.</param>
		/// <param name="originalSize">Size of the original buffer. Is the output size, therefore the original size.</param>
		/// <returns>the number of bytes read in the source buffer (in other words, the compressed size)
		/// If the source stream is malformed, the function will stop decoding and return a negative result, indicating the byte position of the faulty instruction
		/// This function never writes outside of provided buffers, and never modifies input buffer.</returns>
		public unsafe static int LZ4_uncompress(byte* source, byte* destination, int originalSize)
		{
			if (IntPtr.Size != 4)
			{
				return LZ4_uncompress_x64(source, destination, originalSize);
			}
			return LZ4_uncompress_x86(source, destination, originalSize);
		}

		/// <summary>
		/// Provides the maximum size that LZ4 may output in a "worst case" scenario (input data not compressible)
		/// primarily useful for memory allocation of output buffer.
		/// </summary>
		/// <param name="isize">is the input size. Max supported value is ~1.9GB.</param>
		/// <returns>maximum output size in a "worst case" scenario.</returns>
		public static int LZ4_compressBound(int isize)
		{
			return isize + isize / 255 + 16;
		}

		/// <summary>
		/// Compresses the byte buffer.
		/// This method stores a 8-byte header to store the original and compressed buffer size.
		/// </summary>
		/// <param name="data">The data to be compressed.</param>
		/// <param name="mode">The compression mode [Fast, HighCompression].</param>
		/// <returns>The compressed byte array</returns>
		public static byte[] CompressBytes(byte[] data, Lz4Mode mode = Lz4Mode.Fast)
		{
			return CompressBytes(data, 0, data.Length, mode);
		}

		/// <summary>
		/// Compresses the byte buffer.
		/// This method stores a 8-byte header to store the original and compressed buffer size.
		/// </summary>
		/// <param name="data">The data to be compressed.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="length">The length.</param>
		/// <param name="mode">The compression mode [Fast, HighCompression].</param>
		/// <returns>The compressed byte array</returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">If length if outside data array bounds</exception>
		public static byte[] CompressBytes(byte[] data, int offset, int length, Lz4Mode mode)
		{
			if (length > data.Length)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			byte[] buffer = null;
			int sz = Compress(data, 0, length, ref buffer, mode);
			byte[] finalBuffer = new byte[sz];
			Buffer.BlockCopy(buffer, 0, finalBuffer, 0, sz);
			return finalBuffer;
		}

		/// <summary>
		/// Compresses the byte buffer.
		/// This method stores a 8-byte header to store the original and compressed buffer size.
		/// </summary>
		/// <param name="data">The data to be compressed.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="length">The length.</param>
		/// <param name="buffer">The compression buffer. If the buffer is null or the size is insuficient, a new array will be created.</param>
		/// <param name="mode">The compression mode [Fast, HighCompression].</param>
		/// <returns>The compressed byte array</returns>
		public unsafe static int Compress(byte[] data, int offset, int length, ref byte[] buffer, Lz4Mode mode)
		{
			int minLen = LZ4_compressBound(length) + 8;
			if (buffer == null || buffer.Length < minLen)
			{
				buffer = new byte[minLen];
			}
			int compressedSize;
			int compressedSize2;
			fixed (byte* pData = &data[offset])
			{
				fixed (byte* pBuffer = &buffer[8])
				{
					compressedSize = ((mode == Lz4Mode.Fast) ? LZ4_compress(pData, pBuffer, length) : LZ4_compressHC(pData, pBuffer, length));

					byte* ptr = (byte*)(&length);
					buffer[0] = *ptr;
					buffer[1] = ptr[1];
					buffer[2] = ptr[2];
					buffer[3] = ptr[3];

					compressedSize2 = compressedSize + 8;
					ptr = (byte*)(&compressedSize2);
					buffer[4] = *ptr;
					buffer[5] = ptr[1];
					buffer[6] = ptr[2];
					buffer[7] = ptr[3];
				}
			}
			return compressedSize + 8;
		}

		/// <summary>
		/// Decompresses the byte buffer compressed by a Lz4.CompressBytes or Lz4.Compress method.
		/// This method uses the byte array header info to correctly prepare the output buffer.
		/// </summary>
		/// <param name="data">The compressed data returned by a Lz4.CompressBytes or Lz4.Compress method.</param>
		/// <returns>The uncompressed buffer</returns>
		public static byte[] DecompressBytes(byte[] data)
		{
			byte[] uncompressed = null;
			Decompress(data, 0, ref uncompressed);
			return uncompressed;
		}

		/// <summary>
		/// Decompresses the byte buffer compressed by a Lz4.CompressBytes or Lz4.Compress method.
		/// This method uses the byte array header info to correctly prepare the output buffer.
		/// </summary>
		/// <param name="data">The compressed data returned by a Lz4.CompressBytes or Lz4.Compress method.</param>
		/// <param name="offset">The data buffer offset.</param>
		/// <param name="buffer">The decompression buffer. If the buffer is null or the size is insuficient, a new array will be created.</param>
		/// <returns>Uncompressed data size</returns>
		/// <exception cref="T:System.Exception">Input data is incomplete. Data header info size is lesser than total array size</exception>
		public unsafe static int Decompress(byte[] data, int offset, ref byte[] buffer)
		{
			int originalSize;
			fixed (byte* pSrc = &data[offset])
			{
				if (data.Length < *(int*)(pSrc + 4) - 8)
				{
					throw new Exception("Input data is incomplete. Total data array size is lesser than header info size. Data array could be incomplete or was not generated by 'CompressBytes' or 'Compress'.");
				}
				originalSize = *(int*)pSrc - 8;
				if (buffer == null || buffer.Length < originalSize)
				{
					buffer = new byte[originalSize];
				}
				fixed (byte* pDst = &buffer[0])
				{
					LZ4_uncompress(pSrc + 8, pDst, originalSize);
				}
			}
			return originalSize;
		}

		/// <summary>
		/// Compresses the specified text and return a Base64 enconded string.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="mode">The compression mode [Fast, HighCompression].</param>
		/// <returns>The compressed text as a Base64 enconded string</returns>
		public static string CompressString(string text, Lz4Mode mode = Lz4Mode.Fast)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(text);
			byte[] compressed = CompressBytes(buffer, 0, buffer.Length, mode);
			return Convert.ToBase64String(compressed);
		}

		/// <summary>
		/// Decompresses the specified compressed text by the Lz4.CompressString method.
		/// </summary>
		/// <param name="compressedText">The compressed text.</param>
		/// <returns>The decompressed string</returns>
		/// <exception cref="T:System.Exception">Input data is incomplete or was not generated by 'CompressString'.</exception>
		public static string DecompressString(string compressedText)
		{
			byte[] buffer = Convert.FromBase64String(compressedText);
			byte[] uncompressed = DecompressBytes(buffer);
			return Encoding.UTF8.GetString(uncompressed);
		}

		/// <summary>
		/// Size of the compressed buffer returned by a Lz4.CompressBytes or Lz4.CompressBytesHC method.
		/// </summary>
		/// <param name="data">The buffer returned by a Lz4.CompressBytes or Lz4.CompressBytesHC method.</param>
		/// <returns></returns>
		public unsafe static int GetCompressedSize(byte[] data)
		{
			if (data == null || data.Length < 8)
			{
				return 0;
			}
			fixed (byte* pSrc = &data[4])
			{
				return *(int*)pSrc;
			}
		}

		/// <summary>
		/// Size of the original (uncompressed) buffer returned by a Lz4.CompressBytes or Lz4.CompressBytesHC method.
		/// </summary>
		/// <param name="data">The buffer returned by a Lz4.CompressBytes or Lz4.CompressBytesHC method.</param>
		/// <returns></returns>
		public unsafe static int GetUncompressedSize(byte[] data)
		{
			if (data == null || data.Length < 8)
			{
				return 0;
			}
			fixed (byte* pSrc = &data[0])
			{
				return *(int*)pSrc;
			}
		}
	}
}
