using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Interfaces;
using SharpToken;

namespace InsightBase.Infrastructure.Services
{
    public class TokenBasedChunkingService : IChunkingService
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
            return chunks;
        }
    }
}