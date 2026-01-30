using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HoverTanks.Networking
{
    public class Packet : IDisposable
    {
        private List<byte> _buffer;
        private byte[] _readableBuffer;
        private int _readPos;

        /// <summary>Creates a new empty packet (without an ID).</summary>
        public Packet()
        {
            _buffer = new List<byte>(); // Intitialize buffer
            _readPos = 0; // Set readPos to 0
        }

        /// <summary>Creates a new packet with a given ID. Used for sending.</summary>
        /// <param name="id">The packet ID.</param>
        public Packet(byte id)
            : this()
        {
            Write(id); // Write packet id to the buffer
        }

        public Packet(Enum id)
            : this(Convert.ToByte(id)) { }

        /// <summary>Creates a packet from which data can be read. Used for receiving.</summary>
        /// <param name="data">The bytes to add to the packet.</param>
        public Packet(byte[] data)
            : this()
        {
            SetBytes(data);
        }

        #region Functions

        /// <summary>Sets the packet's content and prepares it to be read.</summary>
        /// <param name="data">The bytes to add to the packet.</param>
        public void SetBytes(byte[] data)
        {
            Write(data);
            _readableBuffer = _buffer.ToArray();
        }

        /// <summary>Inserts the length of the packet's content at the start of the buffer.</summary>
        public void WriteLength()
        {
            Debug.Assert(_buffer.Count < 255);
            _buffer.Insert(0, (byte)_buffer.Count); // Insert the byte length of the packet at the very beginning
        }

        /// <summary>Inserts the given int at the start of the buffer.</summary>
        /// <param name="value">The int to insert.</param>
        public void InsertInt(int value)
        {
            _buffer.InsertRange(0, BitConverter.GetBytes(value)); // Insert the int at the start of the buffer
        }

        /// <summary>Gets the packet's content in array form.</summary>
        public byte[] ToArray()
        {
            _readableBuffer = _buffer.ToArray();
            return _readableBuffer;
        }

        /// <summary>Gets the length of the packet's content.</summary>
        public int Length()
        {
            return _buffer.Count; // Return the length of buffer
        }

        /// <summary>Gets the length of the unread data contained in the packet.</summary>
        public int UnreadLength()
        {
            return Length() - _readPos; // Return the remaining length (unread)
        }

        /// <summary>Resets the packet instance to allow it to be reused.</summary>
        /// <param name="shouldReset">Whether or not to reset the packet.</param>
        public void Reset(bool shouldReset = true)
        {
            if (shouldReset)
            {
                _buffer.Clear(); // Clear buffer
                _readableBuffer = null;
                _readPos = 0; // Reset readPos
            }
            else
            {
                _readPos -= 1; // "Unread" the last read int
            }
        }
        #endregion

        #region Write Data

        /// <summary>Adds a byte to the packet.</summary>
        /// <param name="value">The byte to add.</param>
        public void Write(byte value)
        {
            _buffer.Add(value);
        }

        /// <summary>Adds an array of bytes to the packet.</summary>
        /// <param name="value">The byte array to add.</param>
        public void Write(byte[] value)
        {
            _buffer.AddRange(value);
        }

        /// <summary>Adds a short to the packet.</summary>
        /// <param name="value">The short to add.</param>
        public void Write(short value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds an int to the packet.</summary>
        /// <param name="value">The int to add.</param>
        public void Write(int value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a long to the packet.</summary>
        /// <param name="value">The long to add.</param>
        public void Write(long value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a float to the packet.</summary>
        /// <param name="value">The float to add.</param>
        public void Write(float value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a bool to the packet.</summary>
        /// <param name="value">The bool to add.</param>
        public void Write(bool value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a string to the packet.</summary>
        /// <param name="value">The string to add.</param>
        public void Write(string value)
        {
            Write(value.Length); // Add the length of the string to the packet
            _buffer.AddRange(Encoding.ASCII.GetBytes(value)); // Add the string itself
        }

        public void Write(Vector2 value)
        {
            Write(value.x);
            Write(value.y);
        }

        public void Write(Vector3 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }

        public void Write(Quaternion value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }

        public void Write(ClientId value)
        {
            Write((byte)value);
        }

        public void Write(PlayerId value)
        {
            Write((byte)value);
        }

        public void Write(EntityId value)
        {
            Write((int)value);
        }

        public void Write(TeamId value)
        {
            Write((byte)value);
        }

        public void Write(ProjectileClass value)
        {
            Write((byte)value);
        }

        public void Write(AccoladeType value)
        {
            Write((byte)value);
        }

        #endregion // Write Data

        #region Read Data

        /// <summary>Reads a byte from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte ReadByte(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                byte value = _readableBuffer[_readPos]; // Get the byte at readPos' position

                if (moveReadPos)
                {
                    _readPos += 1; // Increase readPos by 1
                }

                return value; // Return the byte
            }
            else
            {
                throw new Exception("Could not read value of type 'byte'!");
            }
        }

        /// <summary>Reads an array of bytes from the packet.</summary>
        /// <param name="length">The length of the byte array.</param>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte[] ReadBytes(int length, bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                byte[] value = _buffer.GetRange(_readPos, length).ToArray(); // Get the bytes at readPos' position with a range of length

                if (moveReadPos)
                {
                    _readPos += length; // Increase readPos by length
                }

                return value; // Return the bytes
            }
            else
            {
                throw new Exception("Could not read value of type 'byte[]'!");
            }
        }

        /// <summary>Reads a short from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public short ReadShort(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                short value = BitConverter.ToInt16(_readableBuffer, _readPos); // Convert the bytes to a short

                if (moveReadPos)
                {
                    _readPos += 2; // Increase readPos by 2
                }

                return value; // Return the short
            }
            else
            {
                throw new Exception("Could not read value of type 'short'!");
            }
        }

        /// <summary>Reads an int from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public int ReadInt(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                int value = BitConverter.ToInt32(_readableBuffer, _readPos); // Convert the bytes to an int

                if (moveReadPos)
                {
                    _readPos += sizeof(int); // Increase readPos by 4
                }

                return value; // Return the int
            }
            else
            {
                throw new Exception("Could not read value of type 'int'!");
            }
        }

        /// <summary>Reads a long from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public long ReadLong(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                long value = BitConverter.ToInt64(_readableBuffer, _readPos); // Convert the bytes to a long

                if (moveReadPos)
                {
                    _readPos += sizeof(long); // Increase readPos by 8
                }

                return value; // Return the long
            }
            else
            {
                throw new Exception("Could not read value of type 'long'!");
            }
        }

        /// <summary>Reads a float from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public float ReadFloat(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                float value = BitConverter.ToSingle(_readableBuffer, _readPos); // Convert the bytes to a float

                if (moveReadPos)
                {
                    _readPos += sizeof(float); // Increase readPos by 4
                }

                return value; // Return the float
            }
            else
            {
                throw new Exception("Could not read value of type 'float'!");
            }
        }

        /// <summary>Reads a bool from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public bool ReadBool(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                bool value = BitConverter.ToBoolean(_readableBuffer, _readPos); // Convert the bytes to a bool

                if (moveReadPos)
                {
                    _readPos += 1; // Increase readPos by 1
                }

                return value; // Return the bool
            }
            else
            {
                throw new Exception("Could not read value of type 'bool'!");
            }
        }

        /// <summary>Reads a string from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public string ReadString(bool moveReadPos = true)
        {
            try
            {
                int length = ReadInt(); // Get the length of the string
                string value = Encoding.ASCII.GetString(_readableBuffer, _readPos, length); // Convert the bytes to a string

                if (moveReadPos && value.Length > 0)
                {
                    _readPos += length; // Increase readPos by the length of the string
                }

                return value; // Return the string
            }
            catch
            {
                throw new Exception("Could not read value of type 'string'!");
            }
        }

        public Vector2 ReadVector2(bool moveReadPos = true)
        {
            return new Vector2(ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }

        public Vector3 ReadVector3(bool moveReadPos = true)
        {
            return new Vector3(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }

        public Quaternion ReadQuaternion(bool moveReadPos = true)
        {
            return new Quaternion(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }

        public ClientId ReadClientId(bool moveReadPos = true)
        {
            return (ClientId)ReadByte(moveReadPos);
        }

        public PlayerId ReadPlayerId(bool moveReadPos = true)
        {
            return (PlayerId)ReadByte(moveReadPos);
        }

        public EntityId ReadEntityId(bool moveReadPos = true)
        {
            return (EntityId)ReadInt(moveReadPos);
        }

        public TeamId ReadTeamId(bool moveReadPos = true)
        {
            return (TeamId)ReadByte(moveReadPos);
        }

        public ProjectileClass ReadProjectileType(bool moveReadPos = true)
        {
            return (ProjectileClass)ReadByte(moveReadPos);
        }

        public AccoladeType ReadAccoladeType(bool moveReadPos = true)
        {
            return (AccoladeType)ReadByte(moveReadPos);
        }

        #endregion // Read Data

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _buffer = null;
                    _readableBuffer = null;
                    _readPos = 0;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
