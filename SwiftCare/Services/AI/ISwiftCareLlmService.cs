using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services.AI
{
    public interface ISwiftCareLlmService
    {
        Task<string> GenerateAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default);

        Task<string> GenerateAsync(
            string prompt,
            CancellationToken cancellationToken = default);

        Task<string> GenerateStructuredAsync(
            string systemPrompt,
            string userPrompt,
            string jsonSchema,
            CancellationToken cancellationToken = default);

        Task<bool> TestConnectionAsync(
            CancellationToken cancellationToken = default);
    }
}