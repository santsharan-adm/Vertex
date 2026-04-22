using System;
using System.Collections.Generic;

namespace IPCSoftware.App.Bending.Models
{
    /// <summary>
    /// Manages lot creation and parameter generation for all 10 lots
    /// </summary>
    public static class LotManager
    {
        private static Dictionary<string, LotModel> _lots;

        public static Dictionary<string, LotModel> GetAllLots()
        {
            if (_lots != null)
                return _lots;

            _lots = new Dictionary<string, LotModel>();

            // Create 10 lots with unique QR codes and parameters
            for (int i = 1; i <= 10; i++)
            {
                string lotId = i.ToString().PadLeft(3, '0');
                LotModel lot = new LotModel(lotId);

                // Generate 4 unique QR codes for each lot
                for (int j = 0; j < 4; j++)
                {
                    lot.QRCodes.Add($"ABCD{i}2345A{(j + 1).ToString().PadLeft(3, '0')}");
                }

                // Generate temperature values (varying by lot)
                lot.Temperature[0] = 80 + (i % 5);
                lot.Temperature[1] = 80.1 + (i % 4);
                lot.Temperature[2] = 80.2 + (i % 3);
                lot.Temperature[3] = 80 + (i % 2);

                // Generate pressure values (varying by lot)
                lot.Pressure[0] = 7 + (i % 2) * 0.1;
                lot.Pressure[1] = 7.1 + (i % 2) * 0.1;
                lot.Pressure[2] = 6.8 + (i % 2) * 0.1;
                lot.Pressure[3] = 6.9 + (i % 2) * 0.1;

                _lots[lotId] = lot;
            }

            return _lots;
        }

        public static LotModel GetLot(string lotId)
        {
            var lots = GetAllLots();
            return lots.ContainsKey(lotId) ? lots[lotId] : null;
        }
    }
}
