using Microsoft.ML.OnnxRuntime;
using System.Diagnostics;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class LocalEmbeddingModel : IDisposable
{
    private readonly InferenceSession _session;
    private readonly ILogger<LocalEmbeddingModel> _logger;

    public LocalEmbeddingModel(
        IWebHostEnvironment environment,
        ILogger<LocalEmbeddingModel> logger)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

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

        using Process process = Process.GetCurrentProcess();

        process.Refresh();

        long workingSetBefore = process.WorkingSet64;

        long privateMemoryBefore = process.PrivateMemorySize64;

        long managedMemoryBefore =
            GC.GetTotalMemory(
                forceFullCollection: false);

        Stopwatch modelLoadStopwatch = Stopwatch.StartNew();

        // 3. ساخت InferenceSession
        _session = new InferenceSession(modelPath);

        modelLoadStopwatch.Stop();

        process.Refresh();

        long workingSetAfter =
            process.WorkingSet64;

        long privateMemoryAfter =
            process.PrivateMemorySize64;

        long managedMemoryAfter =
            GC.GetTotalMemory(
                forceFullCollection: false);

        double workingSetBeforeMb = ToMegabytes(workingSetBefore);

        double workingSetAfterMb = ToMegabytes(workingSetAfter);

        double privateMemoryBeforeMb = ToMegabytes(privateMemoryBefore);

        double privateMemoryAfterMb = ToMegabytes(privateMemoryAfter);

        double managedMemoryBeforeMb = ToMegabytes(managedMemoryBefore);

        double managedMemoryAfterMb = ToMegabytes(managedMemoryAfter);

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms. " +
            "WorkingSet: {WorkingSetBeforeMb} MB -> {WorkingSetAfterMb} MB. " +
            "PrivateMemory: {PrivateMemoryBeforeMb} MB -> {PrivateMemoryAfterMb} MB. " +
            "ManagedMemory: {ManagedMemoryBeforeMb} MB -> {ManagedMemoryAfterMb} MB.",
            "E5ModelLoad",
            modelLoadStopwatch.ElapsedMilliseconds,
            workingSetBeforeMb,
            workingSetAfterMb,
            privateMemoryBeforeMb,
            privateMemoryAfterMb,
            managedMemoryBeforeMb,
            managedMemoryAfterMb);
    }

    private static double ToMegabytes(
        long bytes)
    {
        return Math.Round(
            bytes / 1024d / 1024d,
            2);
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

    public EmbeddingModelRawOutput Run(
    EmbeddingModelInput input)
    {
        // inference را می‌نویسیم

        if (input.InputIds.Length != input.AttentionMask.Length ||
            input.InputIds.Length != input.TokenTypeIds.Length)
        {
            throw new ArgumentException(
                "Embedding model input arrays must have the same length.",
                nameof(input));
        }

        long[] inputShape =
        [
            1,
            input.InputIds.Length
        ];

        using OrtValue inputIdsValue =
            OrtValue.CreateTensorValueFromMemory(
                input.InputIds,
                inputShape);

        using OrtValue attentionMaskValue =
            OrtValue.CreateTensorValueFromMemory(
                input.AttentionMask,
                inputShape);

        using OrtValue tokenTypeIdsValue =
            OrtValue.CreateTensorValueFromMemory(
                input.TokenTypeIds,
                inputShape);

        var inputs = new Dictionary<string, OrtValue>
        {
            ["input_ids"] = inputIdsValue,
            ["attention_mask"] = attentionMaskValue,
            ["token_type_ids"] = tokenTypeIdsValue
        };

        using RunOptions runOptions = new();

        using IDisposableReadOnlyCollection<OrtValue> outputs =
            _session.Run(
                runOptions,
                inputs,
                _session.OutputNames);

        OrtValue output = outputs[0];

        OrtTensorTypeAndShapeInfo outputInfo = output.GetTensorTypeAndShape();

        long[] outputShape = outputInfo.Shape;

        float[] values = output.GetTensorDataAsSpan<float>().ToArray();

        return new EmbeddingModelRawOutput
        {
            Values = values,
            Shape = outputShape
        };
    }
}