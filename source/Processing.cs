using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.IO;

namespace anim2link
{
    public class Processing
    {
        public Form1 host = new Form1();
        public struct Animation
        {
            public string SourceFile { get; set; }
            public int SourceLine { get; set; }
            public string Name { get; set; }
            public int FrameCount { get; set; }

            public Animation(string _src, string name) : this()
            {
                Name = name;
                // Read Source File into Buffer
                SourceFile = _src;
                string[] _l = File.ReadAllLines(SourceFile);

                // Detrmine Line Position and Frame Count of Animation `name`
                for (int i = 0; i < _l.Length; i++)
                {
                    string[] _s = _l[i].Split(' ');
                    if (_s.Length < 4)
                    {
                        if (_s[0] == "frames")
                        {
                            if (_s[2] == Name)
                            {
                                SourceLine = i;
                                FrameCount = int.Parse(_s[1]);
                                break;
                            }
                        }
                    }
                }
            }
        }
        public double RadianToEuler(double _rad)
        {
            double _eul = (180 / Math.PI) * _rad;
            return _eul;
        }
        public float RadianToEuler(float _rad)
        {
            float _eul = (float)((180 / Math.PI) * _rad);
            return _eul;
        }

        public short EncodeRotation(float angle)
        {
            double _const = 182.044444444444;
            short _rot = (short)(angle * _const);
            return _rot;
        }

        public byte[] GetByteArray(byte[] src, int start, int size)
        {
            byte[] bytes = new byte[size];

            for (int i = 0; i < size; i++)
            {
                bytes[i] = src[start + i];
            }

            return bytes;
        }

        public void OverwriteByteArray(byte[] src, int pos, byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                src[pos + i] = data[i];
            }
        }

        public byte[] RotationFromString(string[] _r)
        {
            Vector3 VectorRot = new Vector3(
                RadianToEuler(float.Parse(_r[1])),
                RadianToEuler(float.Parse(_r[2])),
                RadianToEuler(float.Parse(_r[3]))
                );

            short[] ShortRot = new short[3] {
                EncodeRotation(VectorRot.X),
                EncodeRotation(VectorRot.Y),
                EncodeRotation(VectorRot.Z)
            };

            byte[] ByteArrayRot = new byte[6] {
                (byte)((ShortRot[0] >> 8) & 0xFF),
                (byte)((ShortRot[0]) & 0xFF),
                (byte)((ShortRot[1] >> 8) & 0xFF),
                (byte)((ShortRot[1]) & 0xFF),
                (byte)((ShortRot[2] >> 8) & 0xFF),
                (byte)((ShortRot[2]) & 0xFF),
            };

            return ByteArrayRot;
        }
        public byte[] TranslationFromString(string[] _l)
        {
            int _m = 1;
            if (host.FloorPlane)
                _m = 1000;
            else
                _m = 1;

            Vector3 VectorPos = new Vector3(
                float.Parse(_l[1]) * _m,
                float.Parse(_l[2]) * _m,
                float.Parse(_l[3]) * _m
                );

            short[] ShortPos = new short[3] {
                (short)(VectorPos.X),
                (short)(VectorPos.Y),
                (short)(VectorPos.Z)
            };

            byte[] ByteArrayPos = new byte[6] {
                (byte)((ShortPos[0] >> 8) & 0xFF),
                (byte)((ShortPos[0]) & 0xFF),
                (byte)((ShortPos[1] >> 8) & 0xFF),
                (byte)((ShortPos[1]) & 0xFF),
                (byte)((ShortPos[2] >> 8) & 0xFF),
                (byte)((ShortPos[2]) & 0xFF),
            };

            return ByteArrayPos;
        }

        public byte[] GetRaw(Animation _anim)
        {
            string[] _line = File.ReadAllLines(_anim.SourceFile);
            int _linepos = _anim.SourceLine;
            int _fc = _anim.FrameCount;
            List<byte[]> Frames = new List<byte[]>();
            // Initialize New Byte Array (Link's Frame Length * Frame Count)
            byte[] _out = new byte[0x86 * _fc];

            #region Parse
            // Start Parsing; Initiailize Line Buffer
            int _buf = _linepos + 1;

            // For Each Frame
            for (int i = 0; i < _fc; i++)
            {
                // Initiailize New Frame Array and Write Buffer Position
                byte[] _frame = new byte[0x86];
                int _writepos = 0;

                // Parse 1 Translation and 21 Rotations
                for (int j = _buf; j < _buf + 22; j++)
                {
                    // Split Current Line
                    string[] _s = _line[j].Split(' ');

                    // If Translation
                    if (_s[0] == "l")
                    {
                        byte[] _l = TranslationFromString(_s);
                        OverwriteByteArray(_frame, _writepos, _l);
                    }

                    // If Rotation
                    if (_s[0] == "r")
                    {
                        byte[] _r = RotationFromString(_s);
                        OverwriteByteArray(_frame, _writepos, _r);
                    }

                    _writepos += 6;
                }

                // Write Expression Bytes (Hardcoded to Automatic for now)
                OverwriteByteArray(_frame, _writepos, new byte[2] { 0x00, 0x00 });
                _writepos += 2;
                Frames.Add(_frame);

                // Reset and Advance Parse Buffer
                _frame = new byte[0x86];
                _buf += 22;
            }
            #endregion

            #region Write
            int _outbuf = 0;
            for (int i = 0; i < Frames.Count; i++)
            {
                OverwriteByteArray(_out, _outbuf, Frames[i]);
                _outbuf += Frames[i].Length;
            }
            #endregion

            return _out;
        }

    }
}
