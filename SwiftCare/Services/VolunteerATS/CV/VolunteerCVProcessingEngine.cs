using SwiftCare.Models.VolunteerCV;

namespace SwiftCare.Services.VolunteerATS.CV
{
    public class VolunteerCVProcessingEngine
        : IVolunteerCVProcessingEngine
    {
        private readonly IVolunteerCVDocumentEngine
            _documentEngine;

        private readonly IVolunteerCVExtractionEngine
            _extractionEngine;

        private readonly IVolunteerCVParsingEngine
            _parsingEngine;

        public VolunteerCVProcessingEngine(
            IVolunteerCVDocumentEngine documentEngine,
            IVolunteerCVExtractionEngine extractionEngine,
            IVolunteerCVParsingEngine parsingEngine)
        {
            _documentEngine =
                documentEngine;

            _extractionEngine =
                extractionEngine;

            _parsingEngine =
                parsingEngine;
        }

        public async Task<ParsedCv> ProcessAsync(
            Stream documentStream,
            string fileName,
            int volunteerApplicationId = 0)
        {
            if (!_documentEngine.IsSupported(fileName))
            {
                return new ParsedCv
                {
                    VolunteerApplicationId =
                        volunteerApplicationId,

                    ExtractionSuccessful = false,

                    ExtractionMessage =
                        "Unsupported CV format."
                };
            }

            var rawText =
                await _extractionEngine
                    .ExtractTextAsync(
                        documentStream,
                        fileName);

            return await _parsingEngine
                .ParseAsync(
                    rawText,
                    volunteerApplicationId);
        }
    }
}