using System;
using OpenTTRPG.Native;

namespace OpenTTRPG.Tormenta20
{
    public sealed class T20RestResult
    {
        public RestQuality Quality { get; set; }
        public int RecoveryRatePerNight { get; set; }
        public int HpRestored { get; set; }
        public int PmRestored { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mecânica de Descanso e Recuperação Entre Encontros de Tormenta20 (Jogo do Ano):
    /// Ruim (0,5x Nível), Normal (1x Nível), Confortável (2x Nível), Luxuoso (3x Nível).
    /// </summary>
    public static class Tormenta20RestRules
    {
        public static int CalculateRecoveryAmount(int level, RestQuality quality)
        {
            int safeLevel = Math.Max(1, level);
            return quality switch
            {
                RestQuality.Bad => Math.Max(1, safeLevel / 2),
                RestQuality.Normal => safeLevel,
                RestQuality.Comfortable => safeLevel * 2,
                RestQuality.Luxurious => safeLevel * 3,
                _ => safeLevel
            };
        }

        public static string GetQualityName(RestQuality quality) => quality switch
        {
            RestQuality.Bad => "Ruim (0,5x Nível)",
            RestQuality.Normal => "Normal (1x Nível)",
            RestQuality.Comfortable => "Confortável (2x Nível)",
            RestQuality.Luxurious => "Luxuoso (3x Nível)",
            _ => "Normal"
        };

        public static T20RestResult ApplyRest(CharacterSheetData sheet, RestQuality quality,
                                              EncounterSession? session = null, int combatantId = 0)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            int rate = CalculateRecoveryAmount(sheet.Level, quality);

            int oldHp = sheet.CurrentHp;
            int oldPm = sheet.CurrentPm;

            sheet.HealHp(rate);
            sheet.RecoverPm(rate);
            sheet.TempHp = 0;

            int hpRestored = sheet.CurrentHp - oldHp;
            int pmRestored = sheet.CurrentPm - oldPm;

            if (session != null && combatantId > 0)
            {
                session.SetResourceCurrent(combatantId, Tormenta20ActionRules.HealthResourceId, sheet.CurrentHp);
                session.SetResourceTemp(combatantId, Tormenta20ActionRules.HealthResourceId, 0);
                session.SetResourceCurrent(combatantId, Tormenta20ActionRules.ManaResourceId, sheet.CurrentPm);
            }

            return new T20RestResult
            {
                Quality = quality,
                RecoveryRatePerNight = rate,
                HpRestored = hpRestored,
                PmRestored = pmRestored,
                Message = $"[DESCANSO {GetQualityName(quality).ToUpperInvariant()}] {sheet.Name} (Nv.{sheet.Level}) recuperou +{hpRestored} PV e +{pmRestored} PM " +
                          $"(Taxa: {rate}/noite -> PV: {sheet.CurrentHp}/{sheet.MaxHp} | PM: {sheet.CurrentPm}/{sheet.MaxPm})."
            };
        }
    }
}

