﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .repeat/.endrepeat blocks.
    /// </summary>
    public class RepeatBlock : BlockProcessorBase
    {
        #region Members

        double _repetition;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of a repeat block processor.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> containing the instruction
        /// and operands invoking or creating the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public RepeatBlock(SourceLine line, BlockType type)
            : base(line, type)
        {
            _repetition = Evaluator.Evaluate(Line.Operand.Children, 1, uint.MaxValue);
            if (!_repetition.IsInteger())
                throw new ExpressionException(Line.Operand.Position, $"Repetition must be an integer");
        }

        /// <summary>
        /// Creates a new instance of a repeat block processor.
        /// </summary>
        /// <param name="iterator">The <see cref="SourceLine"/> iterator to traverse when
        /// processing the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public RepeatBlock(RandomAccessIterator<SourceLine> iterator,
                           BlockType type)
            : base(iterator, type)
        {
            _repetition = Evaluator.Evaluate(Line.Operand.Children, 1, uint.MaxValue);
            if (!_repetition.IsInteger())
                throw new ExpressionException(Line.Operand.Position, $"Repetition must be an integer");
        }

        #endregion

        #region Methods

        public override bool ExecuteDirective()
        {
            SourceLine line = LineIterator.Current;
            if (line.InstructionName.Equals(".endrepeat"))
            {
                if (_repetition < 1)
                    throw new ExpressionException(line.Instruction.Position, $"Missing matching \".repeat\" directive.");

                if (--_repetition > 0)
                    LineIterator.Rewind(Index);
            }
            return line.InstructionName.Equals(".repeat");
        }

        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => true;

        #endregion
    }
}
