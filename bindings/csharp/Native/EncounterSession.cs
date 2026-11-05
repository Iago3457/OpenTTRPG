using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenTTRPG.Native
{
    /// <summary>
    /// Gerenciador orientado a objetos em C# para interagir de forma segura e fluida com a DLL nativa.
    /// </summary>
    public sealed class EncounterSession : IDisposable
    {
        private IntPtr _handle;

        public EncounterSession(uint seed = 0)
        {
            _handle = NativeMethods.ottrpg_encounter_create(seed);
            if (_handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Falha ao inicializar encontro nativo: {GetLastError()}");
            }
        }

        public static string GetLastError()
        {
            IntPtr ptr = NativeMethods.ottrpg_last_error();
            return ptr == IntPtr.Zero ? string.Empty : (Marshal.PtrToStringUTF8(ptr) ?? string.Empty);
        }

        // ----- Combatentes e Iniciativa -----

        public int AddCombatant(string name, int initiativeModifier, int maxHealth, int team = 0)
        {
            EnsureNotDisposed();
            int id = NativeMethods.ottrpg_combatant_add(_handle, name, initiativeModifier, maxHealth, team);
            if (id <= 0)
            {
                throw new InvalidOperationException($"Erro ao adicionar combatente: {GetLastError()}");
            }
            return id;
        }

        public void SetInitiative(int combatantId, int value)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_initiative_set(_handle, combatantId, value);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao definir iniciativa: {GetLastError()}");
            }
        }

        public int RollInitiative(int combatantId, int diceCount = 1, int diceSides = 20)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_initiative_roll(_handle, combatantId, diceCount, diceSides, out int total);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao rolar iniciativa: {GetLastError()}");
            }
            return total;
        }

        public void RollInitiativeForList(int[] combatantIds, int diceCount = 1, int diceSides = 20)
        {
            EnsureNotDisposed();
            if (combatantIds == null)
            {
                throw new ArgumentNullException(nameof(combatantIds));
            }
            if (combatantIds.Length == 0)
            {
                return;
            }
            int res = NativeMethods.ottrpg_initiative_roll_list(_handle, combatantIds, combatantIds.Length, diceCount, diceSides);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao rolar iniciativa para a lista selecionada: {GetLastError()}");
            }
        }

        public void RollInitiativeForAll(int diceCount = 1, int diceSides = 20)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_initiative_roll_all(_handle, diceCount, diceSides);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao rolar iniciativa para todos: {GetLastError()}");
            }
        }

        public void RemoveFromInitiative(int combatantId)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_initiative_remove(_handle, combatantId);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao remover da iniciativa: {GetLastError()}");
            }
        }

        public int[] GetInitiativeOrder()
        {
            EnsureNotDisposed();
            NativeMethods.ottrpg_initiative_get_order(_handle, null!, 0, out int count);
            if (count <= 0)
            {
                return Array.Empty<int>();
            }
            int[] ids = new int[count];
            NativeMethods.ottrpg_initiative_get_order(_handle, ids, count, out _);
            return ids;
        }

        public CombatantInfo GetCombatantInfo(int combatantId)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combatant_get_info(_handle, combatantId, out CombatantInfo info);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao consultar combatente: {GetLastError()}");
            }
            return info;
        }

        public string GetCombatantName(int combatantId)
        {
            EnsureNotDisposed();
            NativeMethods.ottrpg_combatant_get_name(_handle, combatantId, null!, 0, out int required);
            if (required <= 0)
            {
                return string.Empty;
            }
            byte[] buffer = new byte[required];
            int res = NativeMethods.ottrpg_combatant_get_name(_handle, combatantId, buffer, required, out _);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao obter nome do combatente: {GetLastError()}");
            }
            return Encoding.UTF8.GetString(buffer, 0, required - 1);
        }

        public void SetForcedOut(int combatantId, bool forcedOut)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combatant_set_forced_out(_handle, combatantId, forcedOut ? 1 : 0);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao alterar estado fora de combate: {GetLastError()}");
            }
        }

        public void StartCombat()
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combat_start(_handle);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao iniciar combate: {GetLastError()}");
            }
        }

        public int NextTurn()
        {
            EnsureNotDisposed();
            int nextId = NativeMethods.ottrpg_combat_next_turn(_handle);
            if (nextId <= 0)
            {
                throw new InvalidOperationException($"Erro ao avançar turno: {GetLastError()}");
            }
            return nextId;
        }

        public void EndCombat()
        {
            EnsureNotDisposed();
            NativeMethods.ottrpg_combat_end(_handle);
        }

        // ----- Recursos, Dano, Cura e Condições -----

        public void AddResource(int combatantId, int resourceId, string name, int max, int minimum = 0)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_add(_handle, combatantId, resourceId, name, max, minimum);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao adicionar recurso: {GetLastError()}");
            }
        }

        public ResourceInfo GetResource(int combatantId, int resourceId)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_get(_handle, combatantId, resourceId, out ResourceInfo info);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao obter recurso: {GetLastError()}");
            }
            return info;
        }

        public void SetResourceCurrent(int combatantId, int resourceId, int value)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_set_current(_handle, combatantId, resourceId, value);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao atualizar recurso: {GetLastError()}");
            }
        }

        public void SetResourceMax(int combatantId, int resourceId, int value)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_set_max(_handle, combatantId, resourceId, value);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao atualizar máximo do recurso: {GetLastError()}");
            }
        }

        public void SetResourceTemp(int combatantId, int resourceId, int value)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_set_temp(_handle, combatantId, resourceId, value);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao atualizar recurso temporário: {GetLastError()}");
            }
        }

        public bool SpendResource(int combatantId, int resourceId, int amount)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_spend(_handle, combatantId, resourceId, amount);
            if (res == (int)OttrpgStatus.ErrInsufficientResource)
            {
                return false;
            }
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao gastar recurso: {GetLastError()}");
            }
            return true;
        }

        public int RestoreResource(int combatantId, int resourceId, int amount)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_resource_restore(_handle, combatantId, resourceId, amount, out int restored);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao restaurar recurso: {GetLastError()}");
            }
            return restored;
        }

        public void SetDamageResponse(int combatantId, int damageType, DamageResponse response)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_damage_set_response(_handle, combatantId, damageType, (int)response);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao configurar resposta a dano: {GetLastError()}");
            }
        }

        public void SetDamageReduction(int combatantId, int damageType, int amount)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_damage_set_reduction(_handle, combatantId, damageType, amount);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao configurar redução de dano: {GetLastError()}");
            }
        }

        public void SetGlobalDamageReduction(int combatantId, int amount)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_damage_set_global_reduction(_handle, combatantId, amount);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao configurar redução de dano global: {GetLastError()}");
            }
        }

        public DamageResult PreviewDamage(int targetId, int amount, int damageType = 0, DamageFlags flags = DamageFlags.None)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_damage_preview(_handle, targetId, amount, damageType, (int)flags, out DamageResult result);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao simular dano: {GetLastError()}");
            }
            return result;
        }

        public DamageResult DealDamage(int targetId, int amount, int damageType = 0, DamageFlags flags = DamageFlags.None)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_damage_deal(_handle, targetId, amount, damageType, (int)flags, out DamageResult result);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao aplicar dano: {GetLastError()}");
            }
            return result;
        }

        public int Heal(int targetId, int amount)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_heal(_handle, targetId, amount, out int healed);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao curar: {GetLastError()}");
            }
            return healed;
        }

        public void AddCondition(int targetId, string key, int duration = -1, int intensity = 1,
                                 TickTiming timing = TickTiming.TurnEnd, StackPolicy policy = StackPolicy.Refresh, int sourceId = 0)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_condition_add(_handle, targetId, key, duration, intensity, (int)timing, (int)policy, sourceId);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao aplicar condição '{key}': {GetLastError()}");
            }
        }

        public bool RemoveCondition(int targetId, string key)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_condition_remove(_handle, targetId, key);
            return res == (int)OttrpgStatus.Ok;
        }

        public bool HasCondition(int targetId, string key)
        {
            EnsureNotDisposed();
            return NativeMethods.ottrpg_condition_has(_handle, targetId, key) == 1;
        }

        public bool TryGetCondition(int targetId, string key, out ConditionInfo info)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_condition_get(_handle, targetId, key, out info);
            return res == (int)OttrpgStatus.Ok;
        }

        public RollResult Roll(string expression)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_dice_roll_expression(_handle, expression, out RollResult result);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new ArgumentException($"Expressão de dados inválida: {GetLastError()}");
            }
            return result;
        }

        // ----- Economia de Ações e Fila de Prioridades (Genérico) -----

        public void DefineActionType(int combatantId, int actionType, string name, int priority, int maxPerTurn)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_define_type(_handle, combatantId, actionType, name, priority, maxPerTurn);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao definir tipo de ação: {GetLastError()}");
            }
        }

        public void RemoveActionType(int combatantId, int actionType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_remove_type(_handle, combatantId, actionType);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao remover tipo de ação: {GetLastError()}");
            }
        }

        public void AddActionFallback(int combatantId, int requiredType, int providerType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_add_fallback(_handle, combatantId, requiredType, providerType);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao adicionar regra de conversão de ação: {GetLastError()}");
            }
        }

        public void RemoveActionFallback(int combatantId, int requiredType, int providerType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_remove_fallback(_handle, combatantId, requiredType, providerType);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao remover regra de conversão de ação: {GetLastError()}");
            }
        }

        public void ClearActionFallbacks(int combatantId, int requiredType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_clear_fallbacks(_handle, combatantId, requiredType);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao limpar regras de conversão de ação: {GetLastError()}");
            }
        }

        public ActionSlotInfo GetActionSlot(int combatantId, int actionType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_get_slot(_handle, combatantId, actionType, out ActionSlotInfo info);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao consultar slot de ação: {GetLastError()}");
            }
            return info;
        }

        public void SetActionSlotMax(int combatantId, int actionType, int maxPerTurn, bool refillCurrent = true)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_set_max(_handle, combatantId, actionType, maxPerTurn, refillCurrent ? 1 : 0);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao definir máximo de ações: {GetLastError()}");
            }
        }

        public void SetActionSlotCurrent(int combatantId, int actionType, int current)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_set_current(_handle, combatantId, actionType, current);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao definir ações atuais: {GetLastError()}");
            }
        }

        public void ModifyActionSlotCurrent(int combatantId, int actionType, int delta)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_modify_current(_handle, combatantId, actionType, delta);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao modificar ações atuais: {GetLastError()}");
            }
        }

        public void ResetTurnActions(int combatantId)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_reset_turn(_handle, combatantId);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao resetar ações do turno: {GetLastError()}");
            }
        }

        public bool CanUseAction(int combatantId, int requiredType, out int consumedType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_can_use(_handle, combatantId, requiredType, out consumedType);
            if (res < 0)
            {
                throw new InvalidOperationException($"Erro ao verificar ação: {GetLastError()}");
            }
            return res == 1;
        }

        public bool SpendAction(int combatantId, int requiredType, out int consumedType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_spend(_handle, combatantId, requiredType, out consumedType);
            if (res == (int)OttrpgStatus.ErrInsufficientResource)
            {
                return false;
            }
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao gastar ação: {GetLastError()}");
            }
            return true;
        }

        public bool SpendCompoundAction(int combatantId, int[] requiredTypes)
        {
            EnsureNotDisposed();
            if (requiredTypes == null || requiredTypes.Length == 0)
            {
                return true;
            }
            int res = NativeMethods.ottrpg_action_spend_compound(_handle, combatantId, requiredTypes, requiredTypes.Length);
            if (res == (int)OttrpgStatus.ErrInsufficientResource)
            {
                return false;
            }
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao gastar ação composta: {GetLastError()}");
            }
            return true;
        }

        public bool EnqueueAction(int combatantId, int requiredType, string name, int targetId,
                                  out int actionId, out int consumedType)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_enqueue(_handle, combatantId, requiredType, name ?? string.Empty,
                                                          targetId, out actionId, out consumedType);
            if (res == (int)OttrpgStatus.ErrInsufficientResource)
            {
                return false;
            }
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao enfileirar ação: {GetLastError()}");
            }
            return true;
        }

        public bool EnqueueCompoundAction(int combatantId, int[] requiredTypes, int executionPriority,
                                          string name, int targetId, out int actionId)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_enqueue_compound(
                _handle, combatantId, requiredTypes, requiredTypes?.Length ?? 0,
                executionPriority, name ?? string.Empty, targetId, out actionId);
            if (res == (int)OttrpgStatus.ErrInsufficientResource)
            {
                return false;
            }
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao enfileirar ação composta: {GetLastError()}");
            }
            return true;
        }

        public int GetActionQueueCount(int combatantId)
        {
            EnsureNotDisposed();
            return NativeMethods.ottrpg_action_queue_count(_handle, combatantId);
        }

        public QueuedActionEntry[] GetActionQueue(int combatantId)
        {
            EnsureNotDisposed();
            int count = NativeMethods.ottrpg_action_queue_count(_handle, combatantId);
            if (count <= 0)
            {
                return Array.Empty<QueuedActionEntry>();
            }
            var list = new QueuedActionEntry[count];
            for (int i = 0; i < count; i++)
            {
                NativeMethods.ottrpg_action_queue_get(_handle, combatantId, i, out QueuedActionInfo info, null!, 0, out int req);
                byte[] buf = new byte[Math.Max(1, req)];
                NativeMethods.ottrpg_action_queue_get(_handle, combatantId, i, out info, buf, buf.Length, out _);
                string name = req > 1 ? Encoding.UTF8.GetString(buf, 0, req - 1) : string.Empty;
                list[i] = new QueuedActionEntry(info, name);
            }
            return list;
        }

        public bool PopNextAction(int combatantId, out QueuedActionEntry entry)
        {
            EnsureNotDisposed();
            int count = NativeMethods.ottrpg_action_queue_count(_handle, combatantId);
            if (count <= 0)
            {
                entry = null!;
                return false;
            }
            NativeMethods.ottrpg_action_queue_get(_handle, combatantId, 0, out _, null!, 0, out int req);
            byte[] buf = new byte[Math.Max(1, req)];
            int res = NativeMethods.ottrpg_action_queue_pop(_handle, combatantId, out QueuedActionInfo info, buf, buf.Length, out _);
            if (res != (int)OttrpgStatus.Ok)
            {
                entry = null!;
                return false;
            }
            string name = req > 1 ? Encoding.UTF8.GetString(buf, 0, req - 1) : string.Empty;
            entry = new QueuedActionEntry(info, name);
            return true;
        }

        public bool CancelQueuedAction(int combatantId, int actionId, bool refundSlot = true)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_action_queue_cancel(_handle, combatantId, actionId, refundSlot ? 1 : 0);
            return res == (int)OttrpgStatus.Ok;
        }

        public void ClearActionQueue(int combatantId, bool refundSlots = false)
        {
            EnsureNotDisposed();
            NativeMethods.ottrpg_action_queue_clear(_handle, combatantId, refundSlots ? 1 : 0);
        }

        // ----- Árvore de Habilidades, Estatísticas e Pré-requisitos (Genérico) -----

        public void SetStat(int combatantId, string key, int value)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combatant_set_stat(_handle, combatantId, key, value);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao definir estatística '{key}': {GetLastError()}");
            }
        }

        public int GetStat(int combatantId, string key, int defaultValue = 0)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combatant_get_stat(_handle, combatantId, key, defaultValue, out int val);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao consultar estatística '{key}': {GetLastError()}");
            }
            return val;
        }

        public void GrantAbilityUnchecked(int combatantId, string abilityKey)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_combatant_unlock_ability(_handle, combatantId, abilityKey);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao conceder habilidade '{abilityKey}': {GetLastError()}");
            }
        }

        public bool HasAbility(int combatantId, string abilityKey)
        {
            EnsureNotDisposed();
            return NativeMethods.ottrpg_combatant_has_ability(_handle, combatantId, abilityKey) == 1;
        }

        public void RecordActionInTurn(int combatantId, string actionKey)
        {
            EnsureNotDisposed();
            NativeMethods.ottrpg_combatant_record_action_in_turn(_handle, combatantId, actionKey);
        }

        public bool HasUsedActionInTurn(int combatantId, string actionKey)
        {
            EnsureNotDisposed();
            return NativeMethods.ottrpg_combatant_has_used_action_in_turn(_handle, combatantId, actionKey) == 1;
        }

        public void RegisterAbilityInTree(string key, string name, string parentKey = "")
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_register(_handle, key, name, parentKey ?? string.Empty);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao registrar habilidade '{key}' na árvore: {GetLastError()}");
            }
        }

        public void SetAbilityRequirementRoot(string abilityKey, bool isUsageRequirement, RequirementKind logicalKind, int threshold = 1)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_set_req_root(_handle, abilityKey, isUsageRequirement ? 1 : 0, (int)logicalKind, threshold);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao configurar raiz de pré-requisitos de '{abilityKey}': {GetLastError()}");
            }
        }

        public int AddAbilityRequirementNode(string abilityKey, bool isUsageRequirement, int parentNodeId,
                                             RequirementKind kind, string targetKey = "", int threshold = 1, int resourceId = 0)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_add_req_node(
                _handle, abilityKey, isUsageRequirement ? 1 : 0, parentNodeId,
                (int)kind, targetKey ?? string.Empty, threshold, resourceId, out int nodeId);
            if (res != (int)OttrpgStatus.Ok)
            {
                throw new InvalidOperationException($"Erro ao adicionar nó de pré-requisito em '{abilityKey}': {GetLastError()}");
            }
            return nodeId;
        }

        public bool CanLearnAbility(int combatantId, string abilityKey, out string unmetReasons)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_can_learn(_handle, combatantId, abilityKey);
            if (res < 0)
            {
                throw new InvalidOperationException($"Erro ao avaliar concessão de '{abilityKey}': {GetLastError()}");
            }
            unmetReasons = res == 1 ? string.Empty : GetLastError();
            return res == 1;
        }

        public bool TryLearnAbility(int combatantId, string abilityKey, out string unmetReasons)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_learn(_handle, combatantId, abilityKey);
            if (res == (int)OttrpgStatus.Ok)
            {
                unmetReasons = string.Empty;
                return true;
            }
            unmetReasons = GetLastError();
            return false;
        }

        public bool CanUseAbility(int combatantId, string abilityKey, out string unmetReasons)
        {
            EnsureNotDisposed();
            int res = NativeMethods.ottrpg_ability_can_use(_handle, combatantId, abilityKey);
            if (res < 0)
            {
                throw new InvalidOperationException($"Erro ao avaliar uso de '{abilityKey}': {GetLastError()}");
            }
            unmetReasons = res == 1 ? string.Empty : GetLastError();
            return res == 1;
        }

        public int CurrentTurn => NativeMethods.ottrpg_combat_current(_handle);
        public int Round => NativeMethods.ottrpg_combat_round(_handle);
        public bool IsActive => NativeMethods.ottrpg_combat_is_active(_handle) != 0;

        private void EnsureNotDisposed()
        {
            if (_handle == IntPtr.Zero)
            {
                throw new ObjectDisposedException(nameof(EncounterSession));
            }
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.ottrpg_encounter_destroy(_handle);
                _handle = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        ~EncounterSession()
        {
            Dispose();
        }
    }
}

