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
            // var tokens = _encoding.Encode(text);
            // var chunks = new List<(string, int, int)>();
            // int start = 0;
            // while (start < tokens.Count)
            // {
            //     int end = Math.Min(start + maxTokens, tokens.Count);
            //     var tokenSlice = tokens.Skip(start).Take(end - start).ToList();
            //     var content = _encoding.Decode(tokenSlice);

            //     chunks.Add((content, start, end - 1));
            //     start = end;
            // }
            // return chunks;
            var results = new List<(string, int, int)>();

            //metni önce paragraflara böl
            var paragraphs = text
                                .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .Where(p => !string.IsNullOrWhiteSpace(p))
                                .ToList();

            int globalStart = 0;
            foreach (var paragraph in paragraphs)
            {
                //Paragrafları cümlelere böl
                var sentences = paragraph
                                .Split(new[] { ".", "!", "?" }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .ToList();

                var buffer = new List<string>();
                var bufferTokens = new List<int>();

                foreach (var sentence in sentences)
                {
                    var sentenceTokens = _encoding.Encode(sentence);

                    //eğer tek cümle bile limiti aşarsa direk split et
                    if (sentenceTokens.Count > maxTokens)
                    {
                        results.AddRange(SplitLongSentence(sentence, globalStart, maxTokens));
                        globalStart += maxTokens;
                        continue;
                    }

                    //eğer buffer + sentence toplamı limiti aşarsa -> bufferı chunk olarak kaydet
                    if (bufferTokens.Count + sentenceTokens.Count > maxTokens)
                    {
                        var content = string.Join(" ", buffer);
                        results.Add((content, globalStart, globalStart + bufferTokens.Count - 1));
                        globalStart += bufferTokens.Count;

                        buffer.Clear();
                        bufferTokens.Clear();
                    }
                    buffer.Add(sentence);
                    bufferTokens.AddRange(sentenceTokens);
                }

                //buffer da kalan chunk olarak varsa ekle
                if (buffer.Any())
                {
                    var content = string.Join(" ", buffer);
                    results.Add((content, globalStart, globalStart + bufferTokens.Count - 1));
                    globalStart += bufferTokens.Count;
                }
            }
            return results;
        }

        private IEnumerable<(string, int, int)> SplitLongSentence(string sentence, int globalStart, int maxTokens)
        {
            var tokens = _encoding.Encode(sentence);
            var chunks = new List<(string, int, int)>();

            int start = 0;
            while (start < tokens.Count)
            {
                int end = Math.Min(start + maxTokens, tokens.Count);
                var slice = tokens.Skip(start).Take(end - start).ToList();
                var content = _encoding.Decode(slice);

                chunks.Add((content, globalStart + start, globalStart + end - 1));
                start = end;
            }
            return chunks;
        }
    }
}