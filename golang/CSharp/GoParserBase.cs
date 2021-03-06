using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;

    public abstract class GoParserBase : Parser
    {
        protected GoParserBase(ITokenStream input)
            : base(input)
        {
        }

        protected GoParserBase(ITokenStream input, TextWriter output, TextWriter errorOutput)
            : base(input, output, errorOutput)
        {
        }

        /// <summary>
        /// Returns `true` if on the current index of the parser's
        /// token stream a token exists on the `HIDDEN` channel which
        /// either is a line terminator, or is a multi line comment that
        /// contains a line terminator.
        /// </summary>
        protected bool lineTerminatorAhead()
        {
            // Get the token ahead of the current index.
            int possibleIndexEosToken = CurrentToken.TokenIndex - 1;

            if (possibleIndexEosToken == -1)
            {
                return true;
            }

            IToken ahead = tokenStream.Get(possibleIndexEosToken);
            if (ahead.Channel != Lexer.Hidden)
            {
                // We're only interested in tokens on the HIDDEN channel.
                return false;
            }

            if (ahead.Type == GoLexer.TERMINATOR)
            {
                // There is definitely a line terminator ahead.
                return true;
            }

            if (ahead.Type == GoLexer.WS)
            {
                // Get the token ahead of the current whitespaces.
                possibleIndexEosToken = CurrentToken.TokenIndex - 2;

                if (possibleIndexEosToken == -1)
                {
                    return true;
                }

                ahead = tokenStream.Get(possibleIndexEosToken);
            }

            // Get the token's text and type.
            String text = ahead.Text;
            int type = ahead.Type;

            // Check if the token is, or contains a line terminator.
            return type == GoLexer.COMMENT && (text.Contains("\r") || text.Contains("\n")) ||
                   type == GoLexer.TERMINATOR;
        }

        /// <summary>
        /// Returns `true` if no line terminator exists between the specified
        /// token offset and the prior one on the `HIDDEN` channel.
        /// </summary>
        protected bool noTerminatorBetween(int tokenOffset)
        {
            BufferedTokenStream stream = (BufferedTokenStream)tokenStream;
            IList<IToken> tokens = stream.GetHiddenTokensToLeft(LT(stream, tokenOffset).TokenIndex);

            if (tokens == null)
            {
                return true;
            }

            foreach (IToken token in tokens)
            {
                if (token.Text.Contains("\n"))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns `true` if no line terminator exists after any encountered
        /// parameters beyond the specified token offset and the next on the
        /// `HIDDEN` channel.
        /// </summary>
        protected bool noTerminatorAfterParams(int tokenOffset)
        {
            BufferedTokenStream stream = (BufferedTokenStream) tokenStream;
            int leftParams = 1;
            int rightParams = 0;

            if (LT(stream, tokenOffset).Type == GoLexer.L_PAREN)
            {
                // Scan past parameters
                while (leftParams != rightParams)
                {
                    tokenOffset++;
                    int tokenType = LT(stream, tokenOffset).Type;

                    if (tokenType == GoLexer.L_PAREN)
                    {
                        leftParams++;
                    }
                    else if (tokenType == GoLexer.R_PAREN)
                    {
                        rightParams++;
                    }
                }

                tokenOffset++;
                return noTerminatorBetween(tokenOffset);
            }

            return true;
        }

        protected bool checkPreviousTokenText(string text)
        {
            return LT(tokenStream, 1).Text?.Equals(text) ?? false;
        }

        private IToken LT(ITokenStream stream, int k)
        {
            return stream.LT(k);
        }

        private ITokenStream tokenStream
        {
            get
            {
                return TokenStream;
            }
        }
    }
