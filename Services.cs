namespace PatientVitalsPrediction.Simple;

public static class TrendFeatureBuilder
{
    public static PatientTrendFeatures Build(IReadOnlyList<VitalReading> readings, VitalType trackedVitals)
    {
        var first = readings.First();
        var last = readings.Last();
        var hasBp = trackedVitals.HasFlag(VitalType.BloodPressure);
        var hasGlucose = trackedVitals.HasFlag(VitalType.BloodGlucose);
        var hasOxygen = trackedVitals.HasFlag(VitalType.Oxygen);
        var hasWeight = trackedVitals.HasFlag(VitalType.Weight);

        return new PatientTrendFeatures
        {
            AvgSystolic = hasBp ? readings.Average(r => r.Systolic!.Value) : 120f,
            AvgDiastolic = hasBp ? readings.Average(r => r.Diastolic!.Value) : 80f,
            AvgGlucose = hasGlucose ? readings.Average(r => r.Glucose!.Value) : 100f,
            AvgOxygen = hasOxygen ? readings.Average(r => r.Oxygen!.Value) : 98f,
            WeightChange = hasWeight ? last.WeightKg!.Value - first.WeightKg!.Value : 0f,
            SystolicTrend = hasBp ? (last.Systolic!.Value - first.Systolic!.Value) / readings.Count : 0f,
            GlucoseTrend = hasGlucose ? (last.Glucose!.Value - first.Glucose!.Value) / readings.Count : 0f,
            OxygenTrend = hasOxygen ? (last.Oxygen!.Value - first.Oxygen!.Value) / readings.Count : 0f,
            HasDiseaseRisk = false
        };
    }
}

public static class SyntheticTrainingDataFactory
{
    public static IEnumerable<PatientTrendFeatures> Generate(int sampleCount)
    {
        var random = new Random(7);

        for (var i = 0; i < sampleCount; i++)
        {
            var avgSystolic = RandomRange(random, 95, 180);
            var avgDiastolic = RandomRange(random, 60, 115);
            var avgGlucose = RandomRange(random, 70, 280);
            var avgOxygen = RandomRange(random, 85, 100);
            var weightChange = RandomRange(random, -8, 10);
            var systolicTrend = RandomRange(random, -1.5f, 2.5f);
            var glucoseTrend = RandomRange(random, -2.5f, 4.0f);
            var oxygenTrend = RandomRange(random, -0.8f, 0.4f);

            var riskScore =
                (avgSystolic > 140 ? 1.2f : 0f) +
                (avgDiastolic > 90 ? 0.8f : 0f) +
                (avgGlucose > 170 ? 1.4f : 0f) +
                (avgOxygen < 93 ? 1.8f : 0f) +
                (weightChange > 5 ? 0.5f : 0f) +
                (systolicTrend > 1.2f ? 0.7f : 0f) +
                (glucoseTrend > 1.8f ? 0.9f : 0f) +
                (oxygenTrend < -0.3f ? 0.9f : 0f) +
                RandomRange(random, -0.4f, 0.4f);

            yield return new PatientTrendFeatures
            {
                AvgSystolic = avgSystolic,
                AvgDiastolic = avgDiastolic,
                AvgGlucose = avgGlucose,
                AvgOxygen = avgOxygen,
                WeightChange = weightChange,
                SystolicTrend = systolicTrend,
                GlucoseTrend = glucoseTrend,
                OxygenTrend = oxygenTrend,
                HasDiseaseRisk = riskScore >= 2.1f
            };
        }
    }

    private static float RandomRange(Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}

public static class EndUserInsightEngine
{
    public static EndUserInsight Build(
        PatientTrendFeatures feature,
        IReadOnlyList<VitalReading> readings,
        VitalType trackedVitals,
        float probability)
    {
        var contributors = new List<string>();
        var alerts = new List<string>();
        var trends = new List<string>();

        if (trackedVitals.HasFlag(VitalType.BloodPressure))
        {
            trends.Add($"Blood pressure trend: {TrendLabel(feature.SystolicTrend, higherIsWorse: true)} ({feature.SystolicTrend:+0.00;-0.00;0.00} mmHg/day)");
            if (feature.AvgSystolic >= 140 || feature.AvgDiastolic >= 90)
            {
                alerts.Add($"Average BP is elevated at {feature.AvgSystolic:F1}/{feature.AvgDiastolic:F1} mmHg.");
                contributors.Add("Elevated blood pressure levels.");
            }
            if (feature.SystolicTrend > 1.0f)
            {
                contributors.Add("Blood pressure is rising over time.");
            }
        }

        if (trackedVitals.HasFlag(VitalType.BloodGlucose))
        {
            trends.Add($"Glucose trend: {TrendLabel(feature.GlucoseTrend, higherIsWorse: true)} ({feature.GlucoseTrend:+0.00;-0.00;0.00} mg/dL/day)");
            if (feature.AvgGlucose >= 140)
            {
                alerts.Add($"Average glucose is high at {feature.AvgGlucose:F1} mg/dL.");
                contributors.Add("Elevated glucose readings.");
            }
            if (feature.GlucoseTrend > 1.5f)
            {
                contributors.Add("Glucose trend is increasing.");
            }
        }

        if (trackedVitals.HasFlag(VitalType.Oxygen))
        {
            trends.Add($"SpO2 trend: {TrendLabel(feature.OxygenTrend, higherIsWorse: false)} ({feature.OxygenTrend:+0.00;-0.00;0.00} %/day)");
            if (feature.AvgOxygen < 94)
            {
                alerts.Add($"Average SpO2 is low at {feature.AvgOxygen:F1}%.");
                contributors.Add("Lower oxygen saturation.");
            }
            if (feature.OxygenTrend < -0.2f)
            {
                contributors.Add("Oxygen levels are trending down.");
            }
        }

        if (trackedVitals.HasFlag(VitalType.Weight))
        {
            trends.Add($"Weight trend: {(feature.WeightChange >= 0 ? "Increasing" : "Decreasing")} ({feature.WeightChange:+0.00;-0.00;0.00} kg over period)");
            if (feature.WeightChange > 3.0f)
            {
                contributors.Add("Noticeable weight gain during the observation period.");
            }
        }

        if (contributors.Count == 0 && probability >= 0.60f)
        {
            contributors.Add("Combined pattern across vitals appears concerning even without a single dominant vital.");
        }

        return new EndUserInsight
        {
            RiskBand = RiskBand(probability),
            CertaintyLevel = CertaintyLabel(probability),
            Contributors = contributors,
            AlertFlags = alerts,
            TrendLines = trends
        };
    }

    private static string RiskBand(float probability)
    {
        if (probability >= 0.80f) return "Very High";
        if (probability >= 0.60f) return "High";
        if (probability >= 0.35f) return "Moderate";
        return "Low";
    }

    private static string CertaintyLabel(float probability)
    {
        var distanceFromBoundary = Math.Abs(probability - 0.5f);
        if (distanceFromBoundary >= 0.35f) return "High";
        if (distanceFromBoundary >= 0.20f) return "Medium";
        return "Low";
    }

    private static string TrendLabel(float slope, bool higherIsWorse)
    {
        if (Math.Abs(slope) < 0.05f) return "Stable";
        if (higherIsWorse) return slope > 0 ? "Increasing" : "Improving";
        return slope > 0 ? "Improving" : "Decreasing";
    }
}

public static class RecommendationEngine
{
    public static string BuildRecommendation(PatientTrendFeatures feature, double riskPercent, VitalType trackedVitals)
    {
        if (riskPercent >= 80)
        {
            return "High risk. Consult a physician within 24 hours; consider urgent care if symptoms exist.";
        }

        if (riskPercent >= 60)
        {
            return "Moderate-high risk. Schedule doctor consultation this week and repeat vitals daily.";
        }

        if (riskPercent >= 35)
        {
            return "Moderate risk. Improve diet/activity, monitor for 1-2 weeks, and discuss with primary doctor.";
        }

        if (trackedVitals.HasFlag(VitalType.Oxygen) && feature.AvgOxygen < 92)
        {
            return "Low predicted disease risk but low oxygen trend detected. Seek clinician advice soon.";
        }

        return "Low risk. Continue lifestyle tracking and routine preventive checkups.";
    }
}
