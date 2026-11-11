using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTTRPG.Tormenta20
{
    // =========================================================================
    // 1. DTOs (Data Transfer Objects) - Estruturas puras { get; set; } para JSON
    // =========================================================================

    public sealed class SpellEnhancementDto
    {
        public int ExtraPmCost { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ExtraDice { get; set; } = string.Empty;
    }

    public sealed class CharacterSkillDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PmCost { get; set; }
        public T20ActionType Action { get; set; }
        public int Modifier { get; set; }
        public string TreeKey { get; set; } = string.Empty;
        public bool IsSpell { get; set; }
        public string BaseFormula { get; set; } = string.Empty;
        public List<SpellEnhancementDto> Enhancements { get; set; } = new();
    }

    public sealed class CharacterItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ItemType Type { get; set; }
        public int Slots { get; set; }
        public string Damage { get; set; } = string.Empty;
        public string Critical { get; set; } = "20/x2";
        public T20DamageType DamageType { get; set; }
        public bool IsRanged { get; set; }
        public int DefenseBonus { get; set; }
        public int ArmorPenalty { get; set; }
        public int Price { get; set; }
        public bool Equipped { get; set; }
    }

    public sealed class CharacterSheetDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CharacterClass { get; set; } = string.Empty;
        public string Race { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Deity { get; set; } = string.Empty;
        public int Level { get; set; } = 1;

        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        public int TempHp { get; set; }
        public int MaxPm { get; set; }
        public int CurrentPm { get; set; }

        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }

        public int DefenseBonusMisc { get; set; }
        public int Tibares { get; set; }
        public AttributeType SpellKeyAttribute { get; set; }

        public List<CharacterItemDto> Inventory { get; set; } = new();
        public List<CharacterSkillDto> Skills { get; set; } = new();
        public List<string> UnlockedTreeKeys { get; set; } = new();
        public List<string> TrainedSkills { get; set; } = new();
        public Dictionary<string, int> SkillMiscBonuses { get; set; } = new();
    }

    // =========================================================================
    // 2. Classe de Persistência (Mapeamento + Salvar/Carregar em Arquivo)
    // =========================================================================

    public static class Tormenta20Persistence
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static CharacterSheetDto ToDto(CharacterSheetData sheet)
        {
            var dto = new CharacterSheetDto
            {
                Id = sheet.Id,
                Name = sheet.Name,
                CharacterClass = sheet.CharacterClass,
                Race = sheet.Race,
                Origin = sheet.Origin,
                Deity = sheet.Deity,
                Level = sheet.Level,
                MaxHp = sheet.MaxHp,
                CurrentHp = sheet.CurrentHp,
                TempHp = sheet.TempHp,
                MaxPm = sheet.MaxPm,
                CurrentPm = sheet.CurrentPm,
                Strength = sheet.Strength,
                Dexterity = sheet.Dexterity,
                Constitution = sheet.Constitution,
                Intelligence = sheet.Intelligence,
                Wisdom = sheet.Wisdom,
                Charisma = sheet.Charisma,
                DefenseBonusMisc = sheet.DefenseBonusMisc,
                Tibares = sheet.Tibares,
                SpellKeyAttribute = sheet.SpellKeyAttribute,
                UnlockedTreeKeys = new List<string>(sheet.UnlockedTreeKeys),
                TrainedSkills = new List<string>(sheet.TrainedSkills),
                SkillMiscBonuses = new Dictionary<string, int>(sheet.SkillMiscBonuses)
            };

            foreach (var item in sheet.Inventory)
            {
                dto.Inventory.Add(new CharacterItemDto
                {
                    Name = item.Name,
                    Description = item.Description,
                    Type = item.Type,
                    Slots = item.Slots,
                    Damage = item.Damage,
                    Critical = item.Critical,
                    DamageType = item.DamageType,
                    IsRanged = item.IsRanged,
                    DefenseBonus = item.DefenseBonus,
                    ArmorPenalty = item.ArmorPenalty,
                    Price = item.Price,
                    Equipped = item.Equipped
                });
            }

            foreach (var sk in sheet.Skills)
            {
                var skDto = new CharacterSkillDto
                {
                    Name = sk.Name,
                    Description = sk.Description,
                    PmCost = sk.PmCost,
                    Action = sk.Action,
                    Modifier = sk.Modifier,
                    TreeKey = sk.TreeKey,
                    IsSpell = sk.IsSpell,
                    BaseFormula = sk.BaseFormula
                };
                foreach (var enh in sk.Enhancements)
                {
                    skDto.Enhancements.Add(new SpellEnhancementDto
                    {
                        ExtraPmCost = enh.ExtraPmCost,
                        Description = enh.Description,
                        ExtraDice = enh.ExtraDice
                    });
                }
                dto.Skills.Add(skDto);
            }

            return dto;
        }

        public static CharacterSheetData FromDto(CharacterSheetDto dto)
        {
            var sheet = new CharacterSheetData(
                dto.Id, dto.Name, dto.CharacterClass, dto.Race, dto.Origin, dto.Deity,
                dto.Level, dto.MaxHp, dto.MaxPm)
            {
                CurrentHp = dto.CurrentHp,
                TempHp = dto.TempHp,
                CurrentPm = dto.CurrentPm,
                Strength = dto.Strength,
                Dexterity = dto.Dexterity,
                Constitution = dto.Constitution,
                Intelligence = dto.Intelligence,
                Wisdom = dto.Wisdom,
                Charisma = dto.Charisma,
                DefenseBonusMisc = dto.DefenseBonusMisc,
                Tibares = dto.Tibares,
                SpellKeyAttribute = dto.SpellKeyAttribute
            };

            foreach (var itemDto in dto.Inventory)
            {
                sheet.Inventory.Add(new CharacterItem(
                    itemDto.Name, itemDto.Description, itemDto.Type, itemDto.Slots,
                    itemDto.Damage, itemDto.Critical, itemDto.DefenseBonus,
                    itemDto.ArmorPenalty, itemDto.Price, itemDto.Equipped,
                    itemDto.DamageType, itemDto.IsRanged));
            }

            foreach (var skDto in dto.Skills)
            {
                var skill = new CharacterSkill(
                    skDto.Name, skDto.Description, skDto.PmCost, skDto.Action,
                    skDto.Modifier, skDto.TreeKey, skDto.IsSpell, skDto.BaseFormula);
                foreach (var enhDto in skDto.Enhancements)
                {
                    skill.Enhancements.Add(new SpellEnhancement(
                        enhDto.ExtraPmCost, enhDto.Description, enhDto.ExtraDice));
                }
                sheet.Skills.Add(skill);
            }

            sheet.UnlockedTreeKeys.AddRange(dto.UnlockedTreeKeys);
            foreach (string tr in dto.TrainedSkills)
            {
                sheet.TrainedSkills.Add(tr);
            }
            foreach (var kv in dto.SkillMiscBonuses)
            {
                sheet.SkillMiscBonuses[kv.Key] = kv.Value;
            }

            return sheet;
        }

        public static void SaveCharactersToFile(IEnumerable<CharacterSheetData> sheets, string filePath)
        {
            var dtoList = new List<CharacterSheetDto>();
            foreach (var sheet in sheets)
            {
                dtoList.Add(ToDto(sheet));
            }

            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(dtoList, JsonOptions);
            File.WriteAllText(filePath, json);
        }

        public static List<CharacterSheetData> LoadCharactersFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<CharacterSheetData>();
            }

            string json = File.ReadAllText(filePath);
            var dtoList = JsonSerializer.Deserialize<List<CharacterSheetDto>>(json, JsonOptions)
                          ?? new List<CharacterSheetDto>();

            var result = new List<CharacterSheetData>(dtoList.Count);
            foreach (var dto in dtoList)
            {
                result.Add(FromDto(dto));
            }
            return result;
        }
    }
}