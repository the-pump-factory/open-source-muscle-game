using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using Miniscript;

using static Swole.Script.SwoleScriptSemantics;

namespace Swole.Script
{

	public class SwoleScriptStyling : CodeStyling
	{

        [Header("SwoleScript Specific Styles")]
        public Style keywordImport = new Style("<color=#811bbb>", "</color>");
        public Style keywordInsert = new Style("<color=#cf7c17>", "</color>");
        public Style identifierImport = new Style("<color=#16cab8>", "</color>");
        public Style identifierInsert = new Style("<color=#166dca>", "</color>");

        /// <summary> 
        /// Majority copied from CodeStyling.cs
        /// </summary>
        public override string Markup(string code)
		{

			var sb = new StringBuilder();
			var lexer = new Lexer(code);
			int parenDepth = 0, squareDepth = 0;

			Token prevToken = null;
			bool isBeginning = true;
			bool isImport = false;
            bool isInsert = false;
			bool isVersion = false;
			int versionDotCount = 0;
			void ResetVersion()
			{
				isVersion = false;
				versionDotCount = 0;
			}

            Token peekToken = null;
            int peekPos = -1;

			Token Peek()
			{
				if (peekToken != null) return peekToken;
				peekPos = lexer.position;
				peekToken = lexer.Peek();
				if (peekToken == null) return Token.EOL;
				return peekToken;
			}

            while (!lexer.AtEnd)
			{
				int state = lexer.position;
				int start = (peekPos >= 0 ? peekPos : state);
                int initialStart = lexer.position = start;

                //Debug.Log(peekPos + " " + start + " " + code.Length + " : " + isVersion + " : " + versionDotCount);

                // grab whitespace (normally skipped by the lexer)
                if (Lexer.IsWhitespace(code[lexer.position]))
				{
					while (/*!lexer.AtEnd*/lexer.position < code.Length && Lexer.IsWhitespace(code[lexer.position])) lexer.position++;
					sb.Append(code.Substring(start, lexer.position - start));
					if (/*lexer.AtEnd*/lexer.position >= code.Length) break;
					start = lexer.position;
				}

				// also check for a comment (which the lexer would also skip over)
				if (lexer.position < code.Length - 2 && code[lexer.position] == '/' && code[lexer.position + 1] == '/')
				{
					while (/*!lexer.AtEnd*/lexer.position < code.Length && code[lexer.position] != '\n') lexer.position++;
					sb.Append(comment.startTags);
					sb.Append(code.Substring(start, lexer.position - start));
					sb.Append(comment.endTags);
					if (/*lexer.AtEnd*/lexer.position >= code.Length) break;
					start = lexer.position; 
				}

				if (peekPos >= 0 || lexer.position <= initialStart) lexer.position = state;

                // now, grab and process the next token (being sure to catch and handle lexer exceptions)
                Token tok = null;
				try
				{
					tok = lexer.Dequeue();

                }
				catch (LexerException exc)
				{
					tok = new Token();
					lexer.position = code.Length;
				}

				peekToken = null;
                peekPos = -1;

                if (tok.text == "self") tok.type = Token.Type.Keyword;  // (special case)
				if (tok.text == ssVersionPrefix) tok.type = Token.Type.AddressOf;

				switch (tok.type)
				{
					case Token.Type.Keyword:
						isBeginning = false;
                        isImport = isInsert = false;
                        sb.Append(keyword.startTags);
						sb.Append(tok.text);
						sb.Append(keyword.endTags);
						break;
					case Token.Type.Colon:
                        isBeginning = false;
                        isImport = isInsert = false;
                        sb.Append(colon.startTags);
						sb.Append(":");
						sb.Append(colon.endTags);
						break;
					case Token.Type.Identifier:
						if (isVersion && prevToken != null && prevToken.type == Token.Type.Number && !prevToken.text.EndsWith('.'))
						{
                            sb.Append(openString.startTags);
                            sb.Append(tok.text);
                            sb.Append(openString.endTags);
							break;
                        }

						if (isVersion) ResetVersion();

						if (!isImport && !isInsert)
						{

							if (tok.text == ssKeyword_Import)
							{
								isImport = true;
								if (isBeginning)
								{
									sb.Append(keywordImport.startTags);
									sb.Append(tok.text);
									sb.Append(keywordImport.endTags);
								}
								else
								{
									sb.Append(openString.startTags);
									sb.Append(tok.text);
									sb.Append(openString.endTags);
								}
								break;
							}
							else if (tok.text == ssKeyword_Insert)
							{
								isInsert = true;
								isBeginning = false;
								sb.Append(keywordInsert.startTags);
								sb.Append(tok.text);
								sb.Append(keywordInsert.endTags);
								break;
							}

						}

						if (isInsert)
						{
							isBeginning = false;

							if ((isVersion && versionDotCount < 2) || (prevToken != null && ((prevToken.type == Token.Type.AddressOf || (prevToken.type == Token.Type.Identifier && prevToken.text == ssVersionPrefix)))))
							{
                                sb.Append(openString.startTags);
                                sb.Append(tok.text);
                                sb.Append(openString.endTags);
                                break;
							}

							Peek();
                            if (peekToken.type == Token.Type.Dot || peekToken.type == Token.Type.AddressOf || (peekToken.type == Token.Type.Identifier && peekToken.text == ssVersionPrefix))
							{
								sb.Append(identifierImport.startTags);
								sb.Append(tok.text);
								sb.Append(identifierImport.endTags);
							}
							else
							{
								sb.Append(identifierInsert.startTags);
								sb.Append(tok.text);
								sb.Append(identifierInsert.endTags);
							}
						}
						else if (isImport)
						{
							if (isBeginning)
							{
								if (isVersion || (prevToken != null && ((prevToken.type == Token.Type.Number && prevToken.text.EndsWith('.')) || prevToken.type == Token.Type.AddressOf || (prevToken.type == Token.Type.Identifier && prevToken.text == ssVersionPrefix))))
								{
                                    sb.Append(openString.startTags);
                                    sb.Append(tok.text);
                                    sb.Append(openString.endTags);
                                }
								else
								{
									sb.Append(identifierImport.startTags);
									sb.Append(tok.text);
									sb.Append(identifierImport.endTags);
								}
							}
							else
							{
								sb.Append(openString.startTags);
								sb.Append(tok.text);
								sb.Append(openString.endTags);
							}
						}
						else
						{
							isBeginning = false;
							sb.Append(identifier.startTags);
							sb.Append(tok.text);
							sb.Append(identifier.endTags);
						}
						break;
					case Token.Type.String:
                        isBeginning = false;
                        isImport = isInsert = false;
                        sb.Append(stringLiteral.startTags);
						sb.Append("\"");    // (note that lexer strips the surrounding quotes)
						sb.Append(tok.text.Replace("\"", "\"\""));  // and un-doubles internal quotes
						sb.Append("\"");
						sb.Append(stringLiteral.endTags);
						break;
					case Token.Type.Number:
						if (isVersion) // Handle version numbers in package names
						{
                            for (int i = 0; i < tok.text.Length; i++) if (tok.text[i] == '.') versionDotCount++;

							if (((!tok.text.EndsWith('.') || isImport) && Peek().type != Token.Type.EOL) || (isImport && (tok.text.EndsWith('.') || versionDotCount < 1)))
							{
                                sb.Append(openString.startTags);
                                sb.Append(tok.text);
                                sb.Append(openString.endTags);
								break;
                            }

							if (versionDotCount > 3)
							{
								if (versionDotCount == 4)
								{
									if (tok.text.EndsWith('.') && isInsert)
									{
                                        sb.Append(numericLiteral.startTags);
                                        sb.Append(tok.text.Substring(0, tok.text.Length - 1));
                                        sb.Append(numericLiteral.endTags);
                                        sb.Append(operators.startTags);
                                        sb.Append('.');
                                        sb.Append(operators.endTags);
                                    } 
									else
									{
                                        sb.Append(openString.startTags);
                                        sb.Append(tok.text);
                                        sb.Append(openString.endTags);
                                    }
								}
								else
								{
									sb.Append(openString.startTags);
									sb.Append(tok.text);
									sb.Append(openString.endTags);
								}
								break;
                            }

                            if (versionDotCount <= 1 && Peek().type != Token.Type.EOL)
							{
                                sb.Append(openString.startTags);
                                sb.Append(tok.text); 
                                sb.Append(openString.endTags);
                            }
							else
							{
								if (tok.text.EndsWith('.') && isInsert)
								{
                                    sb.Append(numericLiteral.startTags);
                                    sb.Append(tok.text.Substring(0, tok.text.Length - 1));
                                    sb.Append(numericLiteral.endTags);
                                    sb.Append(operators.startTags);
                                    sb.Append('.');
                                    sb.Append(operators.endTags);
                                } 
								else
								{
                                    sb.Append(numericLiteral.startTags);
                                    sb.Append(tok.text);
                                    sb.Append(numericLiteral.endTags);
                                }
                            }
                        }
						else
						{
                            isBeginning = false;
                            isImport = isInsert = false;
                            sb.Append(numericLiteral.startTags);
							sb.Append(tok.text);
							sb.Append(numericLiteral.endTags);
						}
						break;
					case Token.Type.LParen:
					case Token.Type.RParen:
                        isBeginning = false;
                        isImport = isInsert = false;
                        if (tok.type == Token.Type.LParen) parenDepth++;
						if (rotatingParenColors)
						{
							float h, s, v;
							Color.RGBToHSV(baseParenColor, out h, out s, out v);
							h = Mathf.Repeat(h + 0.22f * (parenDepth - 1), 1);
							Color color = Color.HSVToRGB(h, s, v);
							if (parenDepth < 1) color = Color.red;
							sb.Append("<color=#");
							sb.Append(ColorUtility.ToHtmlStringRGB(color));
							sb.Append(tok.type == Token.Type.LParen ? ">(</color>" : ">)</color>");
						}
						else
						{
							sb.Append(tok.type == Token.Type.LParen ? ">(</color>" : ">)</color>");
						}
						if (tok.type == Token.Type.RParen) parenDepth--;
						break;
					case Token.Type.LSquare:
					case Token.Type.RSquare:
                        isBeginning = false;
                        isImport = isInsert = false;
                        if (tok.type == Token.Type.LSquare) squareDepth++;
						if (rotatingSquareColors)
						{
							float h, s, v;
							Color.RGBToHSV(baseSquareColor, out h, out s, out v);
							h = Mathf.Repeat(h + 0.22f * (squareDepth - 1), 1);
							Color color = Color.HSVToRGB(h, s, v);
							if (squareDepth < 1) color = Color.red;
							sb.Append("<color=#");
							sb.Append(ColorUtility.ToHtmlStringRGB(color));
							sb.Append(tok.type == Token.Type.LSquare ? ">[</color>" : ">]</color>");
						}
						else
						{
							sb.Append(tok.type == Token.Type.LSquare ? ">[</color>" : ">]</color>");
						}
						if (tok.type == Token.Type.RSquare) squareDepth--;
						break;
                    case Token.Type.Dot:
						if (isVersion) versionDotCount++;
						sb.Append(operators.startTags);
						sb.Append(code.Substring(start, lexer.position - start));
						sb.Append(operators.endTags);
                        break;
					case Token.Type.AddressOf:
						if (isImport)
						{
							if (isBeginning)
							{
                                ResetVersion();
                                isVersion = true;
                                sb.Append(keyword.startTags);
                                sb.Append(code.Substring(start, lexer.position - start));
                                sb.Append(keyword.endTags);
                            } 
							else
							{
                                sb.Append(openString.startTags);
                                sb.Append(code.Substring(start, lexer.position - start));
                                sb.Append(openString.endTags);
                            }
						} 
						else if (isInsert)
						{
							ResetVersion();
                            isVersion = true;
                            sb.Append(keyword.startTags);
                            sb.Append(code.Substring(start, lexer.position - start));
                            sb.Append(keyword.endTags);
                        }
						else
						{
                            sb.Append(operators.startTags);
                            sb.Append(code.Substring(start, lexer.position - start));
                            sb.Append(operators.endTags);
                        }
                        break;
                    case Token.Type.Unknown:
                        isImport = isInsert = false;
						ResetVersion();
                        if (code[start] == '"')
						{
							sb.Append(openString.startTags);
							sb.Append(code.Substring(start, lexer.position - start));
							sb.Append(openString.endTags);
						}
						else
						{
							sb.Append(code.Substring(start, lexer.position - start));
						}
						break;
					default:
                        isImport = isInsert = false;
						ResetVersion();
                        sb.Append(operators.startTags);
						sb.Append(code.Substring(start, lexer.position - start));
						sb.Append(operators.endTags);
						break;
				}
				prevToken = tok;
			}
			return sb.ToString();
		}
	}

}