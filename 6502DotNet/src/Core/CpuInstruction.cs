﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// A class that represents information about an instruction, including its 
    /// size, CPU and opcode.
    /// </summary>
    public class CpuInstruction
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of a <see cref="CpuInstruction"/>.
        /// </summary>
        public CpuInstruction() :
            this(string.Empty, 0x00, 0)
        {

        }

        /// <summary>
        /// Creates a new instance of a<see cref="CpuInstruction"/>.
        /// </summary>
        /// <param name="cpu">The CPU's name.</param>
        /// <param name="opcode">The instruction's opcode.</param>
        public CpuInstruction(string cpu, int opcode)
        {
            CPU = cpu;
            Opcode = opcode;
            Size = 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cpu">The CPU's name.</param>
        /// <param name="opcode">The instruction's opcode.</param>
        /// <param name="size">The total size of the instruction, including operand data.</param>
        public CpuInstruction(string cpu, int opcode, int size)
        {
            CPU = cpu;
            Opcode = opcode;
            Size = size;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the instruction size (including operands).
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the opcode of the instruction.
        /// </summary>
        public int Opcode { get; set; }

        /// <summary>
        /// Gets or sets the CPU of this instruction.
        /// </summary>
        /// <value>The cpu.</value>
        public string CPU { get; set; }

        #endregion
    }
}
