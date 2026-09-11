using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Images;

public sealed class NovaCanvasImageGenerator(
    IAmazonBedrockRuntime bedrockRuntime) : IImageGenerator
{
    public string Provider => "bedrock-nova-canvas";

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            taskType = "TEXT_IMAGE",
            textToImageParams = new
            {
                text = request.Prompt
            },
            imageGenerationConfig = new
            {
                quality = "standard",
                width = request.Route.Width,
                height = request.Route.Height,
                numberOfImages = 1,
                seed = Random.Shared.Next(0, 858_993_460)
            }
        });
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var response = await bedrockRuntime.InvokeModelAsync(new InvokeModelRequest
        {
            ModelId = request.Route.Model,
            ContentType = "application/json",
            Accept = "application/json",
            Body = body
        }, cancellationToken);
        using var document = await JsonDocument.ParseAsync(response.Body, cancellationToken: cancellationToken);
        var base64Image = document.RootElement.GetProperty("images")[0].GetString();
        if (string.IsNullOrWhiteSpace(base64Image))
        {
            throw new InvalidOperationException("Image provider returned no image.");
        }

        return new ImageGenerationResult(
            Convert.FromBase64String(base64Image),
            "image/png",
            "Amazon Bedrock",
            request.Route.Model);
    }
}
