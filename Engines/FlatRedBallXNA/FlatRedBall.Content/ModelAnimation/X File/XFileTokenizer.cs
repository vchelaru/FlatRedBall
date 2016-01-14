/*
 * XFileTokenizer.cs
 * Copyright (c) 2006, 2007 David Astle
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Text;
using System.IO;
using System.Globalization;
#endregion


namespace FlatRedBall.Graphics.Model.Animation.Content
{
    /// <summary>
    /// Tokenizes a .X file and provides methods to parse those tokens.
    /// </summary>
    public sealed class XFileTokenizer
    {
        #region Member Variables
        // Use an invariant number formatter so the parse methods will work on computers from all
        // nations
        // Used to build each token so that we don't have to deal with the inefficiency of
        // string concatenation.
        private StringBuilder sb = new StringBuilder();
        // Designates the starting index of the stringbuilder for the current token
        // we are building.  This allows us to not clear the string builder every time
        // we start building a new token, and increases performance.
        private int index = 0;
        // The length of the current token.
        private int length = 0;
        // Stores the index of the current token when other classes are using this to read a
        // a file.
        private int tokenIndex = 0;
        private Stack<long> parseIndices = new Stack<long>();
        private List<string> tokens;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of XFileTokenizer.
        /// </summary>
        /// <param name="fileName">The .X file to tokenize.</param>
        public XFileTokenizer(string fileName)
        {
            string s = "";

            using (System.IO.StreamReader sr = new System.IO.StreamReader(fileName))
            {
                s = sr.ReadToEnd();
                sr.Close();
            }
            //string s = File.ReadAllText(fileName);
            tokens = TokensFromString(s);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Returns current token while ITERATING through the tokens
        /// </summary>
        public string CurrentToken
        { get { return tokens[tokenIndex - 1]; } }

        /// <summary>
        /// Returns the next token without advancing the stream index
        /// </summary>
        public string Peek
        { get { return tokens[tokenIndex]; } }

        // Returns current string while BUILDING the tokens
        private string CurrentString
        { get { return sb.ToString(index, length); } }

        /// <summary>
        /// True if the index is at the end of the stream.
        /// </summary>
        public bool AtEnd
        { get { return tokenIndex >= tokens.Count - 1; } }

        /// <summary>
        /// The number of tokens in the stream.
        /// </summary>
        public int Count
        { get { return tokens.Count; } }

        #endregion

        #region Methods
        // Adds a character to our current token.
        private void AddChar(int c)
        {
            sb.Append((char)c);
            length++;
        }

        // Tells the stringbuilder that we are starting a new token.
        private void ResetString()
        {
            index = index + length;
            // clear sb if it is getting too big; allows us to read long files
            if (index > int.MaxValue / 2)
            {
                sb.Remove(0, sb.Length);
                index = 0;
            }
            length = 0;
        }

        // When parsing a token fails, throws an error that says
        // what tokens surround the ill-parsed token
        private void Throw(Type type, Exception innerException, String inputString)
        {
            string error = "Failed to parse " + type.ToString() +
                " from string \"" + inputString + "\" at token number " +
                tokenIndex.ToString() + "\nvalue: " +
                tokens[tokenIndex] + "\nSurrounding tokens: \n";
            int start = tokenIndex - 15;
            if (start < 0)
                start = 0;
            long end = tokenIndex + 15;
            if (end > Count - 1)
                end = Count - 1;
            for (int i = start; i <= end; i++)
            {
                if (i == tokenIndex)
                    error += "*" + tokens[i];
                else
                    error += tokens[i];
                error += "|||";
            }

            throw new Exception(error, innerException);
        }


        /// <summary>
        /// Skips a node in a .X file and all child nodes; should be called after the opening
        /// brace, "{", has been read.
        /// </summary>
        public void SkipNode()
        {
            string next = NextToken();
            while (next != "}")
                if ((next = NextToken()) == "{")
                    SkipNode();

        }


        /// <summary>
        /// Parses an integer from a .X file
        /// </summary>
        /// <returns>The integer represented by the next token</returns>
        public int NextInt()
        {
            int x = 0;
            try
            {
                x = int.Parse(tokens[tokenIndex++]);
            }
            catch (Exception e)
            {
                Throw(typeof(int), e, tokens[tokenIndex]);

            }

            tokenIndex++;
            return x;
        }

        /// <summary>
        /// Parses a float from a .X file
        /// </summary>
        /// <returns>The float represented by the next token</returns>
        public float NextFloat()
        {
            float x = 0;
            try
            {
                x = float.Parse(tokens[tokenIndex++], System.Globalization.NumberStyles.Float);
            }
            catch (Exception e)
            {
                
                Throw(typeof(float), e, tokens[tokenIndex]);
            }
            finally
            {
                tokenIndex++;
            }
            return x;
        }

 
        /// <summary>
        /// The current token index of the tokenizer.
        /// </summary>
        public long CurrentIndex
        {
            get { return tokenIndex; }
        }

        


        /// <summary>
        /// Parses a string from a .X file
        /// </summary>
        /// <returns>The string represented by the next token</returns>
        public string NextString()
        {
            string s = NextToken().Trim('"');
            SkipToken();
            return s;
        }

        /// <summary>
        /// Reads a generic token from a .X file
        /// </summary>
        /// <returns>The next token</returns>
        public string NextToken()
        {
            string s = null;

            try
            {
                s = tokens[tokenIndex];
            }
            catch
            {
                string error = "Tried to read token when there were " +
                    " no more tokens left.";

                error += "\n";

                error += "Token Index: " + tokenIndex;

                throw new IndexOutOfRangeException(error);
            }
            finally
            {
                tokenIndex++;
            }

            return s;
        }

        /// <summary>
        /// Reads the next Vector2 in the stream.
        /// </summary>
        /// <returns>The parsed Vector2</returns>
        public Vector2 NextVector2()
        {
            try
            {
                Vector2 v = new Vector2(
                    float.Parse(tokens[tokenIndex]),
                    float.Parse(tokens[tokenIndex + 2]));
                return v;
            }
            catch (Exception e)
            {
                Throw(typeof(Vector2), e, tokens[tokenIndex]);
            }
            finally
            {
                tokenIndex += 5;
            }
            return Vector2.Zero;
        }

        /// <summary>
        /// Reads the next Vector3 in the stream.
        /// </summary>
        /// <returns>The parsed Vector3</returns>
        public Vector3 NextVector3()
        {

            try
            {
                Vector3 v = new Vector3(
                    float.Parse(tokens[tokenIndex]),
                    float.Parse(tokens[tokenIndex + 2]),
                    float.Parse(tokens[tokenIndex + 4]));
                return v;
            }
            catch (Exception e)
            {
                Throw(typeof(Vector3), e, tokens[tokenIndex]);
            }
            finally
            {
                tokenIndex += 7;
            }
            return Vector3.Zero;
        }

        /// <summary>
        /// Reads the next Vector4 in the stream.
        /// </summary>
        /// <returns>The parsed Vector4</returns>
        public Vector4 NextVector4()
        {
            tokenIndex += 9;
            try
            {
                Vector4 v = new Vector4(
                    float.Parse(tokens[tokenIndex - 9]),
                    float.Parse(tokens[tokenIndex - 7]),
                    float.Parse(tokens[tokenIndex - 5]),
                    float.Parse(tokens[tokenIndex - 3]));
                return v;
            }
            catch (Exception e)
            {
                Throw(typeof(Vector4), e, tokens[tokenIndex]);
            }
            return Vector4.Zero;
        }

        /// <summary>
        /// Reads the next Matrix in the stream.
        /// </summary>
        /// <returns>The parsed Matrix</returns>
        public Matrix NextMatrix()
        {

            try
            {
                Matrix m = new Matrix(
                    float.Parse(tokens[tokenIndex]), float.Parse(tokens[tokenIndex + 2]),
                    float.Parse(tokens[tokenIndex + 4]), float.Parse(tokens[tokenIndex + 6]),
                    float.Parse(tokens[tokenIndex + 8]), float.Parse(tokens[tokenIndex + 10]),
                    float.Parse(tokens[tokenIndex + 12]), float.Parse(tokens[tokenIndex + 14]),
                    float.Parse(tokens[tokenIndex + 16]), float.Parse(tokens[tokenIndex + 18]),
                    float.Parse(tokens[tokenIndex + 20]), float.Parse(tokens[tokenIndex + 22]),
                    float.Parse(tokens[tokenIndex + 24]), float.Parse(tokens[tokenIndex + 26]),
                    float.Parse(tokens[tokenIndex + 28]), float.Parse(tokens[tokenIndex + 30]));
                return m;
            }
            catch (Exception e)
            {
                Throw(typeof(Matrix), e, tokens[tokenIndex]);
            }
            finally
            {
                tokenIndex += 33;
            }
            return new Matrix();
        }



        /// <summary>
        /// Skips tokens in the stream.
        /// </summary>
        /// <returns>The number of tokens to skip.</returns>
        public void SkipTokens(int numToSkip)
        { tokenIndex += numToSkip; }




        /// <summary>
        /// Skips a nodes name and its opening curly bracket.
        /// </summary>
        /// <returns>The current instance for cascaded calls.</returns>
        public XFileTokenizer SkipName()
        {
            ReadName();
            return this;
        }

        /// <summary>
        /// Reads a nodes name and its opening curly bracket.
        /// </summary>
        /// <returns>Null if the node does not contain a name, the nodes name otherwise.</returns>
        public string ReadName()
        {
            string next = tokens[tokenIndex++];
            if (next != "{")
            {
                tokenIndex++;
                return next;
            }
            return null;
        }

        /// <summary>
        /// Skips a token in the stream.
        /// </summary>
        /// <returns>The current tokenizer for cascaded calls.</returns>
        public XFileTokenizer SkipToken()
        {
            tokenIndex++;
            return this;
        }

        /// <summary>
        /// Resets the tokenizer to the beginning of the stream.
        /// </summary>
        public void Reset()
        {
            tokenIndex = 0;
            sb.Remove(0, sb.Length);
            index = 0;
            length = 0;
        }

        // Takes a string and turns it into an array of tokens.  This is created for performance
        // over readability.  It is far longer than it *needs* to be and 
        // uses a finite state machine to parse the tokens.
        private List<string> TokensFromString(string ms)
        {
            // If we are currently in a state of the FSM such that we are building a token,
            // this is set to a positive value indicating the state.
            int groupnum = -1;
            // Each state in which we build a token is further broken up into substates.
            // This indicates the location in our current state.
            int groupLoc = 1;
            // Since we dont know before hand how big our token array is, and we want
            // it to pack nicely into an array, we can use is a list.
            List<string> strings = new List<string>();
            // The length of the string
            long msLength = ms.Length;

            for (int i = 0; i < msLength; i++)
            {
                // Current character
                int c = ms[i];
            // Yes, I used a goto.  They are generally ok in switch statements, although
            // I'm extending my welcome here.  The code goes to FSMSTART whenever
            // we have broken out of a state and want to transition to the start state
            // (that is, we are not currently building a token).  
            FSMSTART:
                switch (groupnum)
                {
                    // State in which we are building a number token
                    case 0:
                        switch (groupLoc)
                        {
                            // check if it has - sign
                            case 1:
                                if (c == '-')
                                {
                                    AddChar(c);
                                    groupLoc = 2;
                                    break;
                                }
                                else if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    groupLoc = 3;
                                    break;
                                }
                                goto default;
                            // we are passed minus sign but before period
                            // A number most proceed a minus sign, but not necessarily
                            // precede a period without a minus sign.
                            case 2:
                                if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    groupLoc = 3;
                                    break;
                                }
                                goto default;
                            // It is alright to accept a period now
                            case 3:
                                if (c >= '0' && c <= '9' || c=='E' || c=='e')
                                {
                                    AddChar(c);
                                    break;
                                }
                                else if (c == '.')
                                {
                                    AddChar(c);
                                    groupLoc = 4;
                                    break;
                                }
                                // we are done with the token because the next char
                                // is not part of a number
                                strings.Add(CurrentString);
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                goto FSMSTART;
                            // we are just passed period, waiting for a number
                            case 4:
                                if (c >= '0' && c <= '9')
                                {
                                    groupLoc = 5;
                                    AddChar(c);
                                    break;
                                }
                                goto default;
                            // we are passed period and the number after it
                            case 5:
                                if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    break;
                                }
                                strings.Add(CurrentString);
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                goto FSMSTART;
                            // token does not make a valid number, ignore it and
                            // move on
                            default:
                                groupLoc = 1;
                                groupnum = -1;
                                ResetString();
                                break;

                        }
                        break;
                    // a string (may or may not start with " and ends with ") 
                    case 1:
                        switch (groupLoc)
                        {
                            // first  character
                            case 1:
                                AddChar(c);
                                if (c == '"')
                                    groupLoc = 3;
                                else
                                    groupLoc = 2;
                                break;
                            // not a string
                            case 2:
                                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                                    || c == '_' || (c >= '0' && c <= '9') || c == '.' || c == '-'
                                    || c== '/' || c=='\\' || c==':')
                                {
                                    AddChar(c);
                                    break;
                                }


                                strings.Add(CurrentString);
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                goto FSMSTART;
                            // is a string
                            case 3:
                                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                                    || c == '_' || (c >= '0' && c <= '9') || c == '.' || c == '-'
                                    || c == '/' || c == '\\' || c == ':' || c==' ')
                                {
                                    AddChar(c);
                                    break;
                                }
                                // end of string
                                else if (c == '"')
                                {
                                    AddChar(c);
                                    strings.Add(CurrentString);
                                    ResetString();
                                    groupLoc = 1;
                                    groupnum = -1;
                                    break;
                                }

                                strings.Add(CurrentString);
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                goto FSMSTART;

                            // token does not make a valid string; ignore and move on
                            default:
                                groupLoc = 1;
                                groupnum = -1;
                                ResetString();
                                break;
                        }
                        break;
                    // A constraint identifier OR array.  Read about X file format
                    // to see what this is (i.e.,  [...])
                    case 2:
                        switch (groupLoc)
                        {
                            // Read first char ([)
                            case 1:
                                AddChar(c);
                                groupLoc = 2;
                                break;
                            // can now accept letters or periods
                            case 2:
                                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                                {
                                    AddChar(c);
                                    groupLoc = 3;
                                    break;
                                }
                                else if (c == ' ' || c == '.')
                                {
                                    if (c != ' ')
                                        AddChar(c);
                                    groupLoc = 5;
                                    break;
                                }
                                // Since first token after [ is a #, this is an array
                                else if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    groupLoc = 4;
                                    break;
                                }
                                goto default;
                            // passed first letter.  Can now accept a variety
                            // of chars
                            case 3:
                                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                                    || c == '_' || (c >= '0' && c <= '9'))
                                {
                                    AddChar(c);
                                    break;
                                }
                                else if (c == ']')
                                    goto case 10;
                                goto default;
                            // we are reading an array and can at this point only accept
                            // numbers
                            case 4:
                                if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    break;
                                }
                                else if (c == ']')
                                    goto case 10;
                                goto default;
                            // can acept periods or spaces (open constraint identifier)
                            case 5:
                                if (c == '.' || c == ' ')
                                {
                                    AddChar(c);
                                    break;
                                }
                                else if (c == ']')
                                    goto case 10;
                                goto default;
                            // we have finished a valid token
                            case 10:
                                AddChar(c);
                                strings.Add(CurrentString);
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                break;
                            // token is invalid
                            default:
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                break;

                        }
                        break;
                    // A guid (starts with < ends with >)
                    case 3:
                        switch (groupLoc)
                        {
                            // first char (<)
                            case 1:
                                AddChar(c); ;
                                groupLoc = 2;
                                break;
                            // after first character can accept alphanumeric chars, spaces, and hyphens,
                            // but there must be at leaast on char between < and >
                            case 2:
                                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                                    || c == '-' || c == ' ')
                                {
                                    if (c != ' ')
                                        AddChar(c);
                                    groupLoc = 3;
                                    break;
                                }
                                goto default;
                            // same as case 2 except we have read one token after start
                            case 3:
                                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                                    || c == '-' || c == ' ')
                                {
                                    if (c != ' ')
                                        AddChar(c);
                                    break;
                                }
                                // valid GUID
                                else if (c == '>')
                                {
                                    AddChar(c);
                                    strings.Add(CurrentString);
                                    ResetString();
                                    groupLoc = 1;
                                    groupnum = -1;
                                    break;
                                }
                                goto default;
                            // invalid token
                            default:
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                break;

                        }
                        break;
                    // reset group location on new line after reading a comment
                    case 5:
                        if (c == '\n')
                        {
                            groupLoc = 1;
                            groupnum = -1;
                        }
                        break;
                    case -1:
                        // characters that comprise tokens alone
                        if (c == ';' || c == ',' || c == '{' || c == '}')
                        {
                            strings.Add(((char)c).ToString());
                            ResetString();
                            groupLoc = 1;
                            groupnum = -1;
                            break;
                        }
                        // an array or constraint identfiier
                        else if (c == '[')
                        {
                            groupnum = 2;
                            goto case 2;
                        }
                        // a guid
                        else if (c == '<')
                        {
                            groupnum = 3;
                            goto case 3;
                        }
                        // a string or name
                        else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                            || c == '"' || c=='_')
                        {
                            groupnum = 1;
                            goto case 1;
                        }
                        // a number
                        else if ((c == '-') || (c >= '0' && c <= '9'))
                        {
                            groupnum = 0;
                            goto case 0;
                        }
                        // a comment
                        else if (c == '/' || c == '#')
                        {
                            groupnum = 5;
                            goto case 5;
                        }
                        break;
                    default:
                        break;
                }
            }
            return strings;
        }

        #endregion
    }
}