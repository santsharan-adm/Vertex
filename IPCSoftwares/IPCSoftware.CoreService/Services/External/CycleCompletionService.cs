using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.External
{
    /// <summary>
    /// Handles cycle completion logic with external interface integration.
    /// Applies NG override from Mac Mini and manages image quarantine.
    /// </summary>
/*    public class CycleCompletionService : BaseService
    {
        private readonly ExternalInterfaceService _externalIf;
        private readonly string _quarantineFolder;

        public CycleCompletionService(
            ExternalInterfaceService externalIfService,
            IAppLogger logger) : base(logger)
        {
            _externalIf = externalIfService;
            _quarantineFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Quarantine");

            if (!Directory.Exists(_quarantineFolder))
                Directory.CreateDirectory(_quarantineFolder);
        }

        /// <summary>
        /// Applies final status decision: checks Mac Mini NG override and moves images accordingly.
        /// Returns: True if status remains OK, False if overridden to NG.
        /// </summary>
        public async Task<bool> ApplyFinalStatusAsync(
            int sequenceIndex,
            string ccdStatus,
            string imagePath)
        {
            try
            {
                // 1. Check Mac Mini Override
                bool isNgByMacMini = _externalIf.IsStatusOverriddenToNG(sequenceIndex);

                if (isNgByMacMini)
                {
                    _logger.LogWarning(
                        $"[CycleCompletion] Seq {sequenceIndex + 1}: Mac Mini Override to NG. Moving image to quarantine.",
                        LogType.Audit);

                    // 2. Move image to quarantine
                    if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                    {
                        try
                        {
                            string fileName = Path.GetFileName(imagePath);
                            string quarantineFile = Path.Combine(_quarantineFolder, $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}");
                            File.Move(imagePath, quarantineFile, overwrite: true);

                            _logger.LogInfo($"[CycleCompletion] Image moved to quarantine: {quarantineFile}", LogType.Diagnostics);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[CycleCompletion] Failed to move image: {ex.Message}", LogType.Diagnostics);
                        }
                    }

                    return false; // Final status: NG
                }

                // 3. CCD status is OK and no Mac Mini override
                if (ccdStatus == "OK")
                {
                    return true; // Final status: OK
                }

                // 4. CCD status is NG (Mac Mini says OK but CCD says NG)
                // This should still keep image in production but marked as NG
                _logger.LogInfo(
                    $"[CycleCompletion] Seq {sequenceIndex + 1}: CCD NG (Mac Mini OK). Status: NG",
                    LogType.Diagnostics);

                return false; // Final status: NG
            }
            catch (Exception ex)
            {
                _logger.LogError($"[CycleCompletion] ApplyFinalStatus error: {ex.Message}", LogType.Diagnostics);
                return false; // Safe to NG on error
            }
        }
    }*/
}
