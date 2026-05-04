using System;

namespace IPCSoftware.App.Bending.Models
{
    /// <summary>
    /// Represents the inspection data for a single product in the Post Bending Monitor.
    /// </summary>
    public class ProductData
    {
        // --- Basic Identification ---

        /// <summary> Gets or sets the display name of the product (e.g., Product 1). </summary>
        public string ProductName { get; set; }

        /// <summary> Gets or sets the unique QR Code scanned for this specific part. </summary>
        public string QRCode { get; set; }


        // --- Measurement Data: X-Axis ---

        public double X_Upper { get; set; } = 432.223;   // Maximum allowed limit for X
        public double X_Present { get; set; } = 123.333; // Actual measured value for X
        public double X_Lower { get; set; } = 231.675;   // Minimum allowed limit for X


        // --- Measurement Data: Y-Axis ---

        public double Y_Upper { get; set; } = 432.223;   // Maximum allowed limit for Y
        public double Y_Present { get; set; } = 123.333; // Actual measured value for Y
        public double Y_Lower { get; set; } = 231.675;   // Minimum allowed limit for Y


        // --- Measurement Data: Z-Axis ---

        public double Z_Upper { get; set; } = 432.223;   // Maximum allowed limit for Z
        public double Z_Present { get; set; } = 123.333; // Actual measured value for Z
        public double Z_Lower { get; set; } = 231.675;   // Minimum allowed limit for Z


        // --- Measurement Data: W-Axis ---

        public double W_Upper { get; set; } = 432.223;   // Maximum allowed limit for W
        public double W_Present { get; set; } = 123.333; // Actual measured value for W
        public double W_Lower { get; set; } = 231.675;   // Minimum allowed limit for W


        // --- Inspection Results ---

        /// <summary> Gets or sets the final inspection status (e.g., PASS, FAIL, or N/A). </summary>
        public string Result { get; set; }


        // --- Legacy / Additional Metadata ---

        /// <summary> The station identifier where the bending was performed. </summary>
        public string Station { get; set; }

        /// <summary> Timestamp or duration of the bending process. </summary>
        public string BendingTime { get; set; }
    }
}