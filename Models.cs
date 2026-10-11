using Microsoft.ML.Data;

namespace PatientVitalsPrediction.Simple;

[Flags]
public enum VitalType
{
    None = 0,
    BloodPressure = 1,
    BloodGlucose = 2,
    Oxygen = 4,
    Weight = 8
}

public class JsonVitalReading
{
    public DateTime RecordedAt { get; set; }
    public float? Systolic { get; set; }
    public float? Diastolic { get; set; }
    public float? Glucose { get; set; }
    public float? Oxygen { get; set; }
    public float? WeightKg { get; set; }
}

public class PatientInputFile
{
    public string PatientName { get; set; } = string.Empty;
    public List<VitalType> TrackedVitals { get; set; } = new();
    public List<JsonVitalReading> Readings { get; set; } = new();
}

public record VitalReading(
    int Day,
    DateTime RecordedAt,
    float? Systolic,
    float? Diastolic,
    float? Glucose,
    float? Oxygen,
    float? WeightKg);

public class PatientTrendFeatures
{
    public float AvgSystolic { get; set; }
    public float AvgDiastolic { get; set; }
    public float AvgGlucose { get; set; }
    public float AvgOxygen { get; set; }
    public float WeightChange { get; set; }
    public float SystolicTrend { get; set; }
    public float GlucoseTrend { get; set; }
    public float OxygenTrend { get; set; }

    [ColumnName("Label")]
    public bool HasDiseaseRisk { get; set; }
}

public class RiskPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}

public class EndUserInsight
{
    public string RiskBand { get; set; } = string.Empty;
    public string CertaintyLevel { get; set; } = string.Empty;
    public List<string> Contributors { get; set; } = new();
    public List<string> AlertFlags { get; set; } = new();
    public List<string> TrendLines { get; set; } = new();
}

public class PredictionResult
{
    public PatientTrendFeatures Feature { get; set; } = new();
    public RiskPrediction Prediction { get; set; } = new();
    public EndUserInsight Insight { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
    public double RiskPercent { get; set; }
    public double ModelAccuracy { get; set; }
    public double ModelAuc { get; set; }
}
