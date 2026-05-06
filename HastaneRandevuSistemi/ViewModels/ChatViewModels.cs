using System.Collections.Generic;
using HastaneRandevuSistemi.ViewModels;

namespace HastaneRandevuSistemi.ViewModels
{
    public class ChatMessageRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<string> History { get; set; } = new();
    }

    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool IsFinished { get; set; }
        public List<DepartmentSuggestion> Suggestions { get; set; } = new();
        public List<string> QuickReplies { get; set; } = new();
    }
}
