﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Core6502DotNet.z80
{
    /// <summary>
    /// A class to handle disk/tape formats for several popular Z80-based systems.
    /// </summary>
    public class Z80FormatProvider : IBinaryFormatProvider
    {
        public IEnumerable<byte> GetFormat()
        {
            var arch = Assembler.Options.Architecture.ToLower();
            var progstart = Convert.ToUInt16(Assembler.Output.ProgramStart);
            var progend = Convert.ToUInt16(Assembler.Output.ProgramCounter);
            var size = Assembler.Output.GetCompilation().Count;
            var name = Assembler.Options.OutputFile;

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    if (arch.Equals("zx"))
                    {
                        if (name.Length > 10)
                            name = name.Substring(0, 10);
                        else
                            name = name.PadLeft(10);

                        var buffer = new List<byte>
                    {
                        // header
                        0x00,
                        // file type - code
                        0x03
                    };
                        // file name
                        buffer.AddRange(Encoding.ASCII.GetBytes(name));
                        // file size
                        buffer.AddRange(BitConverter.GetBytes(size));
                        // program start
                        buffer.AddRange(BitConverter.GetBytes(progstart));
                        // unused
                        buffer.AddRange(BitConverter.GetBytes(0x8000));

                        // calculate checksum
                        byte checksum = 0x00;
                        buffer.ForEach(b => { checksum ^= b; });

                        // add checksum
                        buffer.Add(checksum);

                        // write the buffer
                        writer.Write(buffer.ToArray());
                    }
                    else if (arch.Equals("amsdos") || arch.Equals("amstap"))
                    {
                        var buffer = new List<byte>();
                        if (arch.Equals("amsdos"))
                        {
                            if (name.Length > 8)
                                name = name.Substring(0, 8);
                            else
                                name = name.PadRight(8);

                            name = string.Format("{0}$$$", name);

                            // user number 0
                            buffer.Add(0);

                        }
                        else
                        {
                            if (name.Length > 16)
                                name = name.Substring(0, 16);
                            else
                                name = name.PadRight(16, '\0');
                        }

                        // name
                        buffer.AddRange(Encoding.ASCII.GetBytes(name));

                        if (arch.Equals("amsdos"))
                        {
                            // block
                            buffer.Add(0);

                            // last block
                            buffer.Add(0);
                        }
                        else
                        {
                            buffer.Add(1);
                            buffer.Add(2);
                        }

                        // binary type
                        buffer.Add(2);

                        // size
                        buffer.AddRange(BitConverter.GetBytes(size));

                        // start address
                        buffer.AddRange(BitConverter.GetBytes(progstart));

                        // first block
                        buffer.Add(0xff);

                        // logical size
                        buffer.AddRange(BitConverter.GetBytes(size));

                        // logical start
                        buffer.AddRange(BitConverter.GetBytes(progstart));

                        // unallocated
                        buffer.AddRange(new byte[36]);

                        if (arch.Equals("amsdos"))
                        {
                            // file size (24-bit number)
                            buffer.AddRange(BitConverter.GetBytes(size));
                            buffer.Add(0);

                            byte checksum = 0;
                            buffer.ForEach(b =>
                            {
                                checksum = (byte)(checksum + b);
                            });
                            buffer.Add(checksum);

                            // bytes 69 - 127 undefined
                            buffer.AddRange(new byte[60]);
                        }
                        writer.Write(buffer.ToArray());
                    }
                    else if (arch.Equals("msx"))
                    {
                        // ID byte
                        writer.Write(0xfe);

                        // start address
                        writer.Write(BitConverter.GetBytes(progstart));

                        // end address
                        writer.Write(BitConverter.GetBytes(progend));

                        // start address
                        writer.Write(BitConverter.GetBytes(progstart));
                    }
                    else if (string.IsNullOrEmpty(arch) || arch.Equals("flat"))
                    {
                        // do nothing
                    }
                    else
                    {
                        throw new Exception($"Unknown architecture specified \"{arch}\".");
                    }
                    writer.Write(Assembler.Output.GetCompilation().ToArray());
                    return ms.ToArray();
                }
            }
        }
    }
}