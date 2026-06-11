using System;
using System.Collections.Generic;

namespace IPCSoftware.Shared.Models.Bending
{
    /// <summary>
    /// Lightweight production record used for logging / export.
    /// </summary>
    public class ProductionDataRecordBending
    {
        // Identity / coding
        public string TwoDCode { get; set; } = string.Empty;

        // Batch identity and stage
        public int? BatchNumber { get; set; }
        public int? Stage { get; set; }

        // When the record was created
        public DateTime TimestampUtc { get; set; }

        // Bending measurements (nullable arrays to keep payload small when unused)
        public float[]? Bending1Temperatures { get; set; }
        public float[]? Bending1Loads { get; set; }

        public float[]? Bending2Temperatures { get; set; }
        public float[]? Bending2Loads { get; set; }

        public float[]? Bending3Temperatures { get; set; }
        public float[]? Bending3Loads { get; set; }
        public float[]? Bending3X { get; set; }
        public float[]? Bending3Y { get; set; }
        public float[]? Bending3Z { get; set; }
        public float[]? Bending3W { get; set; }

        // Final inspection / tearing results (per-part OK/NG)
        public bool[]? TearingStatus { get; set; }

        // Flexible metadata for any additional info (e.g. station trace, operator, notes)
        public Dictionary<string, object>? Meta { get; set; }

        public ProductionDataRecordBending()
        {
            TimestampUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Build a ProductionDataRecord from a BatchModel by mapping commonly used properties.
        /// This method is best-effort and will copy values if the corresponding properties are present.
        /// </summary>
        public static ProductionDataRecordBending FromBatch(BatchModel batch)
        {
            var rec = new ProductionDataRecordBending();
            if (batch == null) return rec;

            try
            {
                // Direct mappings where property names are known
                rec.BatchNumber = batch.BatchNumber;
                rec.Stage = batch.Stage;

                // Prefer explicit QR parts (use first non-empty as TwoDCode; concat if needed)
                string qr = null;
                if (!string.IsNullOrEmpty(batch.QrCode1)) qr = batch.QrCode1;
                if (string.IsNullOrEmpty(qr) && !string.IsNullOrEmpty(batch.QrCode2)) qr = batch.QrCode2;
                if (string.IsNullOrEmpty(qr) && !string.IsNullOrEmpty(batch.QrCode3)) qr = batch.QrCode3;
                if (string.IsNullOrEmpty(qr) && !string.IsNullOrEmpty(batch.QrCode4)) qr = batch.QrCode4;

                // If multiple parts exist, prefer a concatenated representation (common pattern)
                if (string.IsNullOrEmpty(qr))
                {
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(batch.QrCode1)) parts.Add(batch.QrCode1);
                    if (!string.IsNullOrEmpty(batch.QrCode2)) parts.Add(batch.QrCode2);
                    if (!string.IsNullOrEmpty(batch.QrCode3)) parts.Add(batch.QrCode3);
                    if (!string.IsNullOrEmpty(batch.QrCode4)) parts.Add(batch.QrCode4);
                    if (parts.Count > 0) qr = string.Join("", parts);
                }

                rec.TwoDCode = qr ?? string.Empty;

                // Copy arrays (shallow copy reference; clone if isolation required)
                rec.Bending1Temperatures = batch.Bending1Temperatures;
                rec.Bending1Loads = batch.Bending1Loads;

                rec.Bending2Temperatures = batch.Bending2Temperatures;
                rec.Bending2Loads = batch.Bending2Loads;

                rec.Bending3Temperatures = batch.Bending3Temperatures;
                rec.Bending3Loads = batch.Bending3Loads;
                rec.Bending3X = batch.Bending3X;
                rec.Bending3Y = batch.Bending3Y;
                rec.Bending3Z = batch.Bending3Z;
                rec.Bending3W = batch.Bending3W;

                rec.TearingStatus = batch.TearingStatus;

                // Metadata: include flag whether data already logged if available
                rec.Meta = new Dictionary<string, object>();
                try
                {
                    rec.Meta["DataLogged"] = batch.DataLogged;
                }
                catch { /* ignore if not accessible */ }
            }
            catch
            {
                // Best effort mapping; swallow to keep creation robust
            }

            return rec;
        }

        public override string ToString()
        {
            return $"ProductionDataRecord[TwoD:'{TwoDCode}', Batch:{BatchNumber?.ToString() ?? "n/a"}, Stage:{Stage?.ToString() ?? "n/a"}, Time:{TimestampUtc:O}]";
        }
    }
}