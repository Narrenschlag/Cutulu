using BW = System.IO.BinaryWriter;
using BR = System.IO.BinaryReader;
using MS = System.IO.MemoryStream;
using ENC = System.Text.Encoding;

using System;

namespace Cutulu
{
	public static class Buffer
	{
		#region Translation
		public static BufferType getTypeId<T>()
		{
			Type t = typeof(T);

			// Boolean + Byte
			if (t == typeof(bool)) return BufferType.Boolean;

			else if (t == typeof(byte)) return BufferType.Byte;
			else if (t == typeof(byte[])) return BufferType.ByteArray;

			// Short
			if (t == typeof(short)) return BufferType.Short;
			else if (t == typeof(ushort)) return BufferType.UnsignedShort;

			// Integer
			if (t == typeof(int)) return BufferType.Integer;
			else if (t == typeof(uint)) return BufferType.UnsignedInteger;

			// Float
			else if (t == typeof(float)) return BufferType.Float;

			// Double
			else if (t == typeof(double)) return BufferType.Double;

			// String + Char
			else if (t == typeof(string)) return BufferType.String;
			else if (t == typeof(char)) return BufferType.Char;

			// Maybe json
			return typeof(T).IsSerializable ? BufferType.Json : BufferType.None;
		}

		public static Type getType(this BufferType type)
		{
			// Boolean + Byte
			switch (type)
			{
				case BufferType.Boolean: return typeof(bool);

				case BufferType.Byte: return typeof(byte);
				case BufferType.ByteArray: return typeof(byte[]);

				case BufferType.Short: return typeof(short);
				case BufferType.UnsignedShort: return typeof(ushort);

				case BufferType.Integer: return typeof(int);
				case BufferType.UnsignedInteger: return typeof(uint);

				case BufferType.Float: return typeof(float);
				case BufferType.Double: return typeof(double);

				case BufferType.String: return typeof(string);
				case BufferType.Char: return typeof(char);

				default: return typeof(object);
			}
		}
		#endregion

		#region Sending
		/// <summary> assignLength is for udp packets </summary>
		/// <returns> Buffer with byte[] length, type, key and then the bytes </returns>
		public static byte[] package<T>(this T value, byte key, bool assignLength = true)// => encodeBytes(value.toBytes(out BufferType typeId), typeId, key, assignLength);
		{
			// Convert to bytes
			byte[] bytes = value.toBytes(out BufferType typeId);

			// Prepare buffer
			byte[] buffer = new byte[bytes.Length + (assignLength ? 6 : 2)];

			// Writes length of array into size array
			using (MS mem = new MS(buffer))
			{
				using (BW BW = new BW(mem))
				{
					if (assignLength) BW.Write(bytes.Length);
					BW.Write((byte)typeId);
					BW.Write(key);
				}
			}

			Array.Copy(bytes, 0, buffer, assignLength ? 6 : 2, bytes.Length);
			return buffer;
		}

		public static byte[] toBytes<T>(this T value, out BufferType typeId)
		{
			if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>) && value == null)
			{
				typeId = BufferType.None;
				return @default();
			}

			typeId = getTypeId<T>();
			object obj = value;

			if (value != null)
				switch (typeId)
				{
					case BufferType.Boolean: return BitConverter.GetBytes((bool)obj);

					case BufferType.Byte: return new byte[1] { (byte)obj };
					case BufferType.ByteArray: return (byte[])obj;

					case BufferType.Short: return BitConverter.GetBytes((short)obj);
					case BufferType.UnsignedShort: return BitConverter.GetBytes((ushort)obj);

					case BufferType.Integer: return BitConverter.GetBytes((int)obj);
					case BufferType.UnsignedInteger: return BitConverter.GetBytes((uint)obj);

					case BufferType.Float: return BitConverter.GetBytes((float)obj);
					case BufferType.Double: return BitConverter.GetBytes((double)obj);

					case BufferType.String: return ENC.UTF8.GetBytes((string)obj);
					case BufferType.Char: return BitConverter.GetBytes((char)obj);

					case BufferType.Json: return ENC.UTF8.GetBytes(obj.json());
					default: return @default();
				}
			else return @default();

			byte[] @default() => new byte[0];
		}
		#endregion

		#region Receiving
		/// <summary> hasLength is for udp packets </summary>
		/// <returns> Length, type, key and bytes, transmitted by buffer </returns>
		public static BufferType unpack(this byte[] buffer, out byte[] bytes, out byte key, bool hasLength = true)
		{
			// Buffer could not be read
			if (buffer == null || buffer.Length < (hasLength ? 6 : 2))
			{
				bytes = null;
				key = 0;

				return BufferType.None;
			}

			// Assign bytes, type and length
			bytes = new byte[buffer.Length - (hasLength ? 6 : 2)];
			BufferType type = BufferType.None;
			ushort length = 0;

			// Read bytes, type and key
			using (MS mem = new MS(buffer))
			{
				using (BR BR = new BR(mem))
				{
					if (hasLength) length = (ushort)BR.ReadInt32();
					else length = (ushort)(buffer.Length - 2);

					type = (BufferType)BR.ReadByte();
					key = BR.ReadByte();
				}
			}

			// Copy array
			Array.Copy(buffer, hasLength ? 6 : 2, bytes, 0, length);
			return type;
		}

		private static object fromBytes<IfJsonType>(this byte[] bytes, BufferType type)
		{
			if (bytes == null)
			{
				"!!! WARNING !!!\nWas unable to fetch data from package due to it being emtpy".LogError();
				return default;
			}

			switch (type)
			{
				case BufferType.Boolean: return BitConverter.ToBoolean(bytes);

				case BufferType.Byte: return bytes[0];
				case BufferType.ByteArray: return bytes;

				case BufferType.Short: return BitConverter.ToInt16(bytes);
				case BufferType.UnsignedShort: return BitConverter.ToUInt16(bytes);

				case BufferType.Integer: return BitConverter.ToInt32(bytes);
				case BufferType.UnsignedInteger: return BitConverter.ToUInt32(bytes);

				case BufferType.Float: return BitConverter.ToSingle(bytes);
				case BufferType.Double: return BitConverter.ToDouble(bytes);

				case BufferType.String: return ENC.UTF8.GetString(bytes);
				case BufferType.Char: return BitConverter.ToChar(bytes);

				case BufferType.Json: return ENC.UTF8.GetString(bytes).json<IfJsonType>();
				default: return default;
			}
		}

		/// <summary> Returns byte data as type </summary>
		public static T fromBytes<T>(this byte[] bytes)
		{
			BufferType type = getTypeId<T>();

			object obj = fromBytes<T>(bytes, type);

			return (T)obj;
		}

		/// <summary> Returns byte data as type </summary>
		/// <summary> always true for disposable types </summary>
		public static bool As<T>(this byte[] bytes, out T value)
		{
			value = fromBytes<T>(bytes);

			if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>) && value == null)
				return false;

			return true;
		}

		/// <summary> Returns byte data as type </summary>
		public static T As<T>(this byte[] bytes) => fromBytes<T>(bytes);
		#endregion
	}

	public enum Method
	{
		Tcp, Udp
	}

	public enum BufferType
	{
		None = 0,

		Boolean, Byte, ByteArray,
		Short, UnsignedShort,
		Integer, UnsignedInteger,
		Float, Double,
		String, Char,
		Json
	}
}