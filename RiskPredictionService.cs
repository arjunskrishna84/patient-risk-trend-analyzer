using Microsoft.ML;

namespace PatientVitalsPrediction.Simple;

public class RiskPredictionService
{
    private readonly MLContext _ml;
    private readonly ITransformer _model;
    private readonly object _predictLock = new();

    public double Accuracy { get; }
    public double Auc { get; }

    public RiskPredictionService()
    {
        _ml = new MLContext(seed: 1);

        var trainingData = SyntheticTrainingDataFactory.Generate(sampleCount: 2000);
        var dataView = _ml.Data.LoadFromEnumerable(trainingData);
        var split = _ml.Data.TrainTestSplit(dataView, testFraction: 0.2);

        var pipeline = _ml.Transforms.Concatenate(
                "Features",
                nameof(PatientTrendFeatures.AvgSystolic),
                nameof(PatientTrendFeatures.AvgDiastolic),
                nameof(PatientTrendFeatures.AvgGlucose),
                nameof(PatientTrendFeatures.AvgOxygen),
                nameof(PatientTrendFeatures.WeightChange),
                nameof(PatientTrendFeatures.SystolicTrend),
                nameof(PatientTrendFeatures.GlucoseTrend),
                nameof(PatientTrendFeatures.OxygenTrend))
            .Append(_ml.BinaryClassification.Trainers.LbfgsLogisticRegression(labelColumnName: "Label"));

        _model = pipeline.Fit(split.TrainSet);

        var predictions = _model.Transform(split.TestSet);
        var metrics = _ml.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");
        Accuracy = metrics.Accuracy;
        Auc = metrics.AreaUnderRocCurve;
    }

    public PredictionResult RunFullPrediction(PatientInputFile input)
    {
        var trackedVitals = VitalType.None;
        foreach (var v in input.TrackedVitals)
        {
            trackedVitals |= v;
        }

        Validate(input, trackedVitals);

        var readings = input.Readings
            .Select((r, i) => new VitalReading(
                Day: i + 1,
                RecordedAt: r.RecordedAt,
                Systolic: r.Systolic,
                Diastolic: r.Diastolic,
                Glucose: r.Glucose,
                Oxygen: r.Oxygen,
                WeightKg: r.WeightKg))
            .ToList();

        var feature = TrendFeatureBuilder.Build(readings, trackedVitals);

        RiskPrediction patientPrediction;
        lock (_predictLock)
        {
            var engine = _ml.Model.CreatePredictionEngine<PatientTrendFeatures, RiskPrediction>(_model);
            patientPrediction = engine.Predict(feature);
        }

        var riskPercent = Math.Round(patientPrediction.Probability * 100, 2);
        var insight = EndUserInsightEngine.Build(feature, readings, trackedVitals, patientPrediction.Probability);
        var recommendation = RecommendationEngine.BuildRecommendation(feature, riskPercent, trackedVitals);

        return new PredictionResult
        {
            Feature = feature,
            Prediction = patientPrediction,
            Insight = insight,
            Recommendation = recommendation,
            RiskPercent = riskPercent,
            ModelAccuracy = Accuracy,
            ModelAuc = Auc
        };
    }

    private static void Validate(PatientInputFile input, VitalType trackedVitals)
    {
        if (string.IsNullOrWhiteSpace(input.PatientName))
        {
            throw new InvalidOperationException("Patient name is required.");
        }

        if (trackedVitals == VitalType.None)
        {
            throw new InvalidOperationException("Select at least one tracked vital.");
        }

        if (input.Readings == null || input.Readings.Count < 5)
        {
            throw new InvalidOperationException("At least 5 readings are required.");
        }

        for (var i = 0; i < input.Readings.Count; i++)
        {
            var reading = input.Readings[i];
            var label = $"Reading #{i + 1}";

            if (trackedVitals.HasFlag(VitalType.BloodPressure) && (!reading.Systolic.HasValue || !reading.Diastolic.HasValue))
            {
                throw new InvalidOperationException($"{label} is missing systolic or diastolic blood pressure.");
            }

            if (trackedVitals.HasFlag(VitalType.BloodGlucose) && !reading.Glucose.HasValue)
            {
                throw new InvalidOperationException($"{label} is missing glucose.");
            }

            if (trackedVitals.HasFlag(VitalType.Oxygen) && !reading.Oxygen.HasValue)
            {
                throw new InvalidOperationException($"{label} is missing oxygen.");
            }

            if (trackedVitals.HasFlag(VitalType.Weight) && !reading.WeightKg.HasValue)
            {
                throw new InvalidOperationException($"{label} is missing weight.");
            }
        }
    }
}
