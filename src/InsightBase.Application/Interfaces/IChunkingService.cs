using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsightBase.Application.Interfaces
{
    public interface IChunkingService
    {
        List<(string Content, int StartToken, int EndToken)> ChunkText(string text, int maxTokens = 500);
    }
}