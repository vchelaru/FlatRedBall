using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Utilities;
using ICSharpCode.NRefactory.CSharp;

namespace GlueViewOfficialPlugins.Scripting
{
    #region Enums

    public enum BlockType
    {
        None,
        If,
        ElseIf,
        Else,
        While,
        For,
        Foreach
    }

    #endregion

    public static class BlockTypeExtensionMethods
    {
        public static bool LinksFromPreviousType(this BlockType blockType, BlockType previousBlockType)
        {
            if (blockType == BlockType.None)
            {
                return false;
            }

            if (blockType == BlockType.If || blockType == BlockType.While ||
                blockType == BlockType.For || blockType == BlockType.Foreach)
            {
                return previousBlockType == BlockType.None;
            }

            if (blockType == BlockType.ElseIf || blockType == BlockType.Else)
            {
                return previousBlockType == BlockType.If || previousBlockType == BlockType.ElseIf;
            }
            return false;
        }
    }

    public class ConditionalCodeBlock
    {
        #region Fields

        string[] mLines;

        #endregion

        public BlockType BlockType
        {
            get;
            private set;
        }

        public int LineWithConditionIndex
        {
            get;
            private set;
        }
        public int FirstLineOfBlockIndex
        {
            get;
            private set;
        }
        public int BlockLength
        {
            get;
            private set;
        }
        public int LineCountIncludingConditionLine
        {
            get
            {
                return 1 + BlockLength;
            }
        }

        public bool IsBlockWrappedInBrackets
        {
            get
            {
                return BlockLength > 1 && mLines[FirstLineOfBlockIndex].Trim() == "{" &&
                    mLines[FirstLineOfBlockIndex + BlockLength - 1].Trim() == "}";
            }
        }

        public string ConditionExpression
        {
            get
            {
                string conditionLine = mLines[LineWithConditionIndex];


                int startOfExpression = conditionLine.IndexOf('(') + 1;
                int endOfExpression = conditionLine.LastIndexOf(')');
                int expressionCount = endOfExpression - startOfExpression;
                
                return conditionLine.Substring(startOfExpression, expressionCount);
            }
        }

        public static ConditionalCodeBlock GetConditionalBlockFrom(string[] lines, int index)
        {
            BlockType blockType = GetBlockTypeStartingAt(lines, index);

            if (blockType != BlockType.None)
            {
                ConditionalCodeBlock ccb = new ConditionalCodeBlock();
                ccb.BlockType = blockType;

                ccb.mLines = lines;

                ccb.LineWithConditionIndex = index;
                ccb.FirstLineOfBlockIndex = index + 1;
                ccb.BlockLength = 0;

                if (ccb.FirstLineOfBlockIndex < lines.Length)
                {
                    bool isBlockWithBrackets = lines[ccb.FirstLineOfBlockIndex].Trim().StartsWith("{");

                    if (!isBlockWithBrackets)
                    {
                        ccb.BlockLength = 1;
                    }
                    else
                    {
                        int numberOfBrackets = 0;

                        for (int i = ccb.FirstLineOfBlockIndex; i < lines.Length; i++)
                        {
                            numberOfBrackets += lines[i].CountOf('{');
                            numberOfBrackets -= lines[i].CountOf('}');

                            if (numberOfBrackets == 0)
                            {
                                ccb.BlockLength = 1 + i - ccb.FirstLineOfBlockIndex;
                                break;
                            }
                        }
                    }
                }

                return ccb;
            }
            return null;

        }

        public static BlockType GetBlockTypeStartingAt(string[] lines, int index)
        {
            /////////////////////EARLY OUT////////////////////////
            if (index >= lines.Length)
            {
                return BlockType.None;
            }
            //////////////////END EARLY OUT//////////////////////
            BlockType blockType = BlockType.None;

            var statements = new CSharpParser().ParseStatements(lines[index]);
            var firstStatement = statements.FirstOrDefault();
            if (firstStatement != null)
            {
                if (firstStatement is WhileStatement)
                {
                    blockType = BlockType.While;
                }
                else if (firstStatement is ForeachStatement)
                {
                    blockType = BlockType.Foreach;
                }
                else if (firstStatement is ForStatement)
                {
                    blockType = BlockType.For;
                }
                else if (firstStatement is IfElseStatement)
                {
                    string line = lines[index].Trim();
                    if (line.StartsWith("if"))
                    {
                        blockType = BlockType.If;
                    }
                    else if (line.StartsWith("else if"))
                    {
                        blockType = BlockType.ElseIf;
                    }
                    else
                    {
                        blockType = BlockType.Else;
                    }
                }
            }

            return blockType;
        }

        public override string ToString()
        {
            return BlockType.ToString() + "  " + this.ConditionExpression;
        }

    }
}
