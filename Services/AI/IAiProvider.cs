using System.Threading.Tasks;

namespace VedionScreenShare.Services.AI
{
    public interface IAiProvider
    {
        /// <summary>
        /// Send a screen frame (JPEG bytes) and get an AI response
        /// </summary>
        Task<string> AnalyzeFrameAsync(byte[] jpegFrame, string systemPrompt);

        /// <summary>
        /// Provider display name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Check if provider is configured
        /// </summary>
        bool IsConfigured { get; }
    }
}
