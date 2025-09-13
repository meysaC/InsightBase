using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Interfaces;
using SharpToken;

namespace InsightBase.Infrastructure.Services
{
    public class TokenBasedChunkingService : IChunkingService //token bazlı yap (ör. tiktoken veya OpenAITokenizer kullanabilirsin)
                                                            //TiktokenSharp / SharpToken projelerinden birini dahil et (token bazlı chunking için).
    {
        private readonly GptEncoding _encoding;
        public TokenBasedChunkingService() =>  _encoding = GptEncoding.GetEncoding("cl100k_base"); // OpenAI'nin GPT-4 ve GPT-3.5 (text-embedding-3-*) modelleri için kullanılan encoding
        public List<(string Content, int StartToken, int EndToken)> ChunkText(string text, int maxTokens = 500)
        {
            var tokens = _encoding.Encode(text);
            var chunks = new List<(string, int, int)>();
            int start = 0;
            while (start < tokens.Count)
            {
                int end = Math.Min(start + maxTokens, tokens.Count);
                var tokenSlice = tokens.Skip(start).Take(end - start).ToList();
                var content = _encoding.Decode(tokenSlice);

                chunks.Add((content, start, end - 1));
                start = end;
            }
            


            // var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            // var chunks = new List<(string, int, int)>();
            // int tokenCount = 0;
            // int startToken = 0;
            // var current = new List<string>();
            // for (int i = 0; i < words.Length; i++)
            // {
            //     current.Add(words[i]);
            //     tokenCount++;
            //     var isLast = i == words.Length - 1;
            //     if (tokenCount >= maxTokens || isLast)
            //     {
            //         var content = string.Join(" ", current);
            //         chunks.Add((content, startToken, startToken + tokenCount - 1));
            //         startToken += tokenCount;
            //         tokenCount = 0;
            //         current.Clear();
            //     }
            // }

            return chunks;
        }
    }
}