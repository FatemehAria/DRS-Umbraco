using Microsoft.ML.OnnxRuntime;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class LocalEmbeddingModel : IDisposable
{
    private readonly InferenceSession _session;

    public LocalEmbeddingModel(
        IWebHostEnvironment environment)
    {
        // 1. ساخت مسیر model.onnx
        string modelPath = Path.Combine(
            environment.ContentRootPath,
            "AIModel",
            "multilingual-e5-small",
            "model.onnx");
        // 2. بررسی وجود فایل
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                "Embedding model was not found.",
                modelPath);
        }
        // 3. ساخت InferenceSession
        _session = new InferenceSession(modelPath);
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    public IReadOnlyCollection<string> GetInputNames()
    {
        return _session.InputMetadata.Keys.ToArray();
    }

    public IReadOnlyCollection<string> GetOutputNames()
    {
        return _session.OutputMetadata.Keys.ToArray();
    }
}