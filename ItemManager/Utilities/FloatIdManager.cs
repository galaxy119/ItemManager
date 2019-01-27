using System;

namespace ItemManager.Utilities
{
	internal class FloatIdManager
	{
		private readonly byte[] bytes;

		/// <summary>
		/// Creates a new ID manager to dish out float-based IDs.
		/// </summary>
		public FloatIdManager()
		{
			bytes = new byte[4];
			bytes[0] = 1; //1 so the float is not 0
			bytes[3] = 128; //leading digit, must have 4th (128) set to 1
		}

		/// <summary>
		/// Creates a new ID manager to dish out float-based IDs.
		/// </summary>
		/// <param name="startingId">Bytes to start the float-generation (ascending) with.</param>
		public FloatIdManager(byte[] startingId)
		{
			bytes = startingId;
		}

		/// <summary>
		/// Generates a new, unique, ID out of 2.147E9 possible IDs
		/// </summary>
		public float NewId()
		{
			if (++bytes[0] == 0)
			{
				if (++bytes[1] == 0)
				{
					if (++bytes[2] == 0)
					{
						bytes[3]++; //preserve leading (-) sign
					}
				}
			}

			return BitConverter.ToSingle(bytes, 0);
		}
	}
}
