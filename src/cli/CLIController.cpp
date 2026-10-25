#include "openttrpg/cli/CLIController.hpp"
#include <iostream>
#include <limits>
#include <string>
#include <regex>
#include <algorithm>

namespace openttrpg::cli {

namespace {

// Lê uma linha de texto limpa, removendo espaços em branco das extremidades
std::string getCleanLine() {
    std::string line;
    std::getline(std::cin, line);
    while (!line.empty() && (line.front() == ' ' || line.front() == '\t' || line.front() == '\r')) {
        line.erase(line.begin());
    }
    while (!line.empty() && (line.back() == ' ' || line.back() == '\t' || line.back() == '\r')) {
        line.pop_back();
    }
    return line;
}

// Faz o parsing robusto de fórmulas de dados como "1d8", "d8", "2d6", "1d12" ou "8"
bool parseDiceString(const std::string& input, int& count, int& sides) {
    std::regex rgx(R"((\d*)\s*d\s*(\d+))", std::regex::icase);
    std::smatch match;
    if (std::regex_search(input, match, rgx)) {
        count = match[1].str().empty() ? 1 : std::stoi(match[1].str());
        sides = std::stoi(match[2].str());
        if (count <= 0) count = 1;
        if (sides <= 0) sides = 6;
        return true;
    }
    // Suporte caso o usuário digite "1 8" ou apenas "8"
    std::regex numRgx(R"((\d+)(?:\s+(\d+))?)");
    if (std::regex_search(input, match, numRgx)) {
        if (match[2].matched) {
            count = std::stoi(match[1].str());
            sides = std::stoi(match[2].str());
        } else {
            count = 1;
            sides = std::stoi(match[1].str());
        }
        if (count <= 0) count = 1;
        if (sides <= 0) sides = 6;
        return true;
    }
    return false;
}

// Extrai o multiplicador de crítico (aceita "2", "x2", "X2", "*2", "3", "x3", etc.)
int parseMultiplier(const std::string& input) {
    if (input.empty()) return 1;
    std::regex rgx(R"([xX\*]?\s*(\d+))");
    std::smatch match;
    if (std::regex_search(input, match, rgx)) {
        int m = std::stoi(match[1].str());
        return std::max(1, m);
    }
    return 1;
}

// Extrai bônus numérico fixo (aceita "+3", "3", "-2", etc.)
int parseBonus(const std::string& input) {
    if (input.empty()) return 0;
    std::regex rgx(R"(([+-]?\s*\d+))");
    std::smatch match;
    if (std::regex_search(input, match, rgx)) {
        std::string s = match[1].str();
        s.erase(std::remove_if(s.begin(), s.end(), ::isspace), s.end());
        try {
            return std::stoi(s);
        } catch (...) {
            return 0;
        }
    }
    return 0;
}

} // namespace anônimo

CLIController::CLIController() : nextCharacterId(1) {
    // -------------------------------------------------------------
    // PERSONAGENS PRÉ-CARREGADOS (CANÔNICOS DE TORMENTA20 JOGO DO ANO)
    // -------------------------------------------------------------

    // 1. "Valeros de Valkaria" - Guerreiro Humano
    models::CharacterSheet warriorSheet("Valeros de Valkaria", "Guerreiro", "Humano", "Soldado", "Valkaria", 1, 24, 4);
    warriorSheet.setAttributeValue(core::AttributeType::Strength, 3);      // FOR +3
    warriorSheet.setAttributeValue(core::AttributeType::Dexterity, 1);     // DES +1
    warriorSheet.setAttributeValue(core::AttributeType::Constitution, 2);  // CON +2
    warriorSheet.setAttributeValue(core::AttributeType::Intelligence, 0);  // INT 0
    warriorSheet.setAttributeValue(core::AttributeType::Wisdom, 1);        // SAB +1
    warriorSheet.setAttributeValue(core::AttributeType::Charisma, 0);      // CAR 0

    warriorSheet.addItem(models::Item("Espada Longa", "Espada versátil de aço", core::ItemType::Weapon, 1, "1d8", "19/x2", 0, 0, 15, true));
    warriorSheet.addItem(models::Item("Cota de Malha", "Armadura pesada de metal", core::ItemType::Armor, 2, "", "20/x2", 5, -2, 150, true));
    warriorSheet.addItem(models::Item("Escudo Leve", "Escudo de madeira reforçada", core::ItemType::Shield, 1, "", "20/x2", 1, -1, 5, true));
    warriorSheet.addItem(models::Item("Bálsamo Restaurador", "Cura 2d4 PV", core::ItemType::Consumable, 1, "", "20/x2", 0, 0, 10, false));

    warriorSheet.addSkill(models::Skill("Ataque Especial", "Gasta 1 PM para receber +4 no ataque ou +1d6 no dano", 1, core::ActionType::Free, 4));
    warriorSheet.addSkill(models::Skill("Golpe Pessoal", "Ataque devastador personalizado de classe", 2, core::ActionType::Standard, 0));
    characters.emplace_back(nextCharacterId++, warriorSheet);

    // 2. "Sillith" - Arcanista (Maga) Qareen
    models::CharacterSheet mageSheet("Sillith", "Arcanista", "Qareen", "Mago Aprendiz", "Wynna", 1, 11, 10);
    mageSheet.setAttributeValue(core::AttributeType::Strength, -1);      // FOR -1
    mageSheet.setAttributeValue(core::AttributeType::Dexterity, 2);      // DES +2
    mageSheet.setAttributeValue(core::AttributeType::Constitution, 1);   // CON +1
    mageSheet.setAttributeValue(core::AttributeType::Intelligence, 4);   // INT +4
    mageSheet.setAttributeValue(core::AttributeType::Wisdom, 1);         // SAB +1
    mageSheet.setAttributeValue(core::AttributeType::Charisma, 3);       // CAR +3

    mageSheet.addItem(models::Item("Varinha Arcana", "Foco arcano para canalização", core::ItemType::Weapon, 1, "1d4", "20/x2", 0, 0, 20, true));
    mageSheet.addItem(models::Item("Poção de Mana", "Recupera 2d4 PM", core::ItemType::Consumable, 1, "", "20/x2", 0, 0, 30, false));

    mageSheet.addSkill(models::Skill("Mísseis Mágicos", "Projéteis de essência: 2d4+2 de dano sem teste de resistência", 1, core::ActionType::Standard, 0));
    mageSheet.addSkill(models::Skill("Curar Ferimentos", "Recupera 2d8+2 PV de um aliado tocado", 1, core::ActionType::Standard, 0));
    characters.emplace_back(nextCharacterId++, mageSheet);
}

void CLIController::printHeader() const {
    std::cout << "\n======================================================================\n";
    std::cout << "          OpenTTRPG — MOTOR DE TORMENTA20 (JOGO DO ANO)               \n";
    std::cout << "              Disciplina: AED1 - UFSCar (Fase 1)                      \n";
    std::cout << "======================================================================\n";
}

void CLIController::showMainMenu() const {
    std::cout << "\n[MENU PRINCIPAL - TORMENTA20]\n";
    std::cout << "  1. Criar Personagem (Ficha de Tormenta20)\n";
    std::cout << "  2. Listar e Visualizar Fichas de Personagens (PV, PM, Defesa, Carga)\n";
    std::cout << "  3. Gerenciar Ficha (Equipamentos, Poderes com PM, Recursos)\n";
    std::cout << "  4. Rolar Dados (Testes no d20 com Crítico e Dano de Armas)\n";
    std::cout << "  5. Combate de Tormenta20 com Fila de Iniciativa (AED1)\n";
    std::cout << "  0. Sair\n";
    std::cout << "Escolha uma opção: ";
}

void CLIController::handleCreateCharacter() {
    std::cout << "\n--- CRIAÇÃO DE PERSONAGEM (TORMENTA20 JOGO DO ANO) ---\n";
    std::cout << "Nome do Personagem: ";
    std::string name = getCleanLine();
    std::cout << "Raça (ex: Humano, Lefou, Qareen, Anão, Elfo, Goblin, Minotauro): ";
    std::string race = getCleanLine();
    std::cout << "Classe (ex: Guerreiro, Arcanista, Clérigo, Ladino, Paladino, Bárbaro): ";
    std::string charClass = getCleanLine();
    std::cout << "Origem (ex: Soldado, Acólito, Guarda, Herói Camponês): ";
    std::string origin = getCleanLine();
    std::cout << "Divindade (ex: Valkaria, Khalmyr, Wynna, Lena, Arsenal): ";
    std::string deity = getCleanLine();

    std::cout << "Pontos de Vida Máximos (PV): ";
    std::string hpStr = getCleanLine();
    int maxHp = parseMultiplier(hpStr);
    if (maxHp <= 0) maxHp = 20;

    std::cout << "Pontos de Mana Máximos (PM): ";
    std::string pmStr = getCleanLine();
    int maxPm = parseMultiplier(pmStr);
    if (maxPm < 0) maxPm = 3;

    models::CharacterSheet sheet(name, charClass, race, origin, deity, 1, maxHp, maxPm);

    std::cout << "\n[DEFINA OS 6 ATRIBUTOS DO T20 (ex: 3 para +3, -1 para -1)]:\n";
    std::cout << "  Força (FOR): "; sheet.setAttributeValue(core::AttributeType::Strength, parseBonus(getCleanLine()));
    std::cout << "  Destreza (DES): "; sheet.setAttributeValue(core::AttributeType::Dexterity, parseBonus(getCleanLine()));
    std::cout << "  Constituição (CON): "; sheet.setAttributeValue(core::AttributeType::Constitution, parseBonus(getCleanLine()));
    std::cout << "  Inteligência (INT): "; sheet.setAttributeValue(core::AttributeType::Intelligence, parseBonus(getCleanLine()));
    std::cout << "  Sabedoria (SAB): "; sheet.setAttributeValue(core::AttributeType::Wisdom, parseBonus(getCleanLine()));
    std::cout << "  Carisma (CAR): "; sheet.setAttributeValue(core::AttributeType::Charisma, parseBonus(getCleanLine()));

    characters.emplace_back(nextCharacterId++, sheet);
    std::cout << "\n[OK] Personagem '" << name << "' criado com sucesso com as regras de Tormenta20!\n";
}

void CLIController::handleListCharacters() const {
    std::cout << "\n--- PERSONAGENS CADASTRADOS EM ARTON (" << characters.size() << ") ---\n";
    if (characters.empty()) {
        std::cout << "Nenhum aventureiro cadastrado ainda.\n";
        return;
    }

    for (std::size_t i = 0; i < characters.size(); ++i) {
        std::cout << "\n[" << (i + 1) << "] ID " << characters[i].getId() << " - "
                  << characters[i].getName() << "\n";
        std::cout << characters[i].getSheet().formatSheetSummary() << "\n";
    }
}

void CLIController::handleManageCharacter() {
    if (characters.empty()) {
        std::cout << "\nNenhum personagem disponível para gerenciar.\n";
        return;
    }

    std::cout << "\n--- SELECIONE O PERSONAGEM ---\n";
    for (std::size_t i = 0; i < characters.size(); ++i) {
        std::cout << "  " << (i + 1) << ". " << characters[i].getName() << "\n";
    }
    std::cout << "Número do personagem: ";
    std::string choiceStr = getCleanLine();
    std::size_t choice = parseMultiplier(choiceStr);

    if (choice < 1 || choice > characters.size()) {
        std::cout << "Seleção inválida.\n";
        return;
    }

    models::Character& selected = characters[choice - 1];

    std::cout << "\nGerenciando Ficha de: " << selected.getName() << "\n";
    std::cout << "  1. Adicionar Equipamento / Item (Lista Cadastral)\n";
    std::cout << "  2. Adicionar Poder ou Magia com PM (Lista Cadastral)\n";
    std::cout << "  3. Equipar / Desequipar Item (Atualiza Defesa de T20)\n";
    std::cout << "  4. Remover Item do Inventário\n";
    std::cout << "  5. Remover Poder da Lista\n";
    std::cout << "  6. Gastar Pontos de Mana (PM)\n";
    std::cout << "  7. Recuperar Pontos de Mana (PM)\n";
    std::cout << "  8. Sofrer Dano em Pontos de Vida (PV)\n";
    std::cout << "  9. Curar Pontos de Vida (PV)\n";
    std::cout << "Opção: ";
    std::string subOptStr = getCleanLine();
    int subOpt = parseMultiplier(subOptStr);

    if (subOpt == 1) {
        std::cout << "Nome do Item: ";
        std::string itemName = getCleanLine();
        std::cout << "Descrição: ";
        std::string desc = getCleanLine();

        std::cout << "Tipo (1=Arma, 2=Armadura, 3=Escudo, 4=Consumível, 5=Outros): ";
        int typeChoice = parseMultiplier(getCleanLine());
        core::ItemType itType = core::ItemType::Weapon;
        if (typeChoice == 2) itType = core::ItemType::Armor;
        else if (typeChoice == 3) itType = core::ItemType::Shield;
        else if (typeChoice == 4) itType = core::ItemType::Consumable;
        else if (typeChoice == 5) itType = core::ItemType::Miscellaneous;

        std::cout << "Espaços de carga ocupados no T20 (padrão 1): ";
        int slots = parseMultiplier(getCleanLine());

        std::string damage, crit = "20/x2";
        int defBonus = 0, penalty = 0;

        if (itType == core::ItemType::Weapon) {
            std::cout << "Dano da Arma (ex: 1d8, 2d6, 1d10): ";
            damage = getCleanLine();
            std::cout << "Crítico (ex: 19/x2, x3, 20/x2): ";
            crit = getCleanLine();
            if (crit.empty()) crit = "20/x2";
        } else if (itType == core::ItemType::Armor || itType == core::ItemType::Shield) {
            std::cout << "Bônus na Defesa de T20 (ex: +5): ";
            defBonus = parseBonus(getCleanLine());
            std::cout << "Penalidade de Armadura (ex: -2 ou 0): ";
            penalty = parseBonus(getCleanLine());
        }

        std::cout << "Preço em Tibares (T$): ";
        int price = parseMultiplier(getCleanLine());

        selected.getSheet().addItem(models::Item(itemName, desc, itType, slots, damage, crit, defBonus, penalty, price, false));
        std::cout << "[OK] Item adicionado ao inventário de Tormenta20!\n";
    } else if (subOpt == 2) {
        std::cout << "Nome da Habilidade, Poder ou Magia: ";
        std::string skillName = getCleanLine();
        std::cout << "Descrição mecânica: ";
        std::string desc = getCleanLine();
        std::cout << "Custo em Pontos de Mana (PM): ";
        int pmCost = parseMultiplier(getCleanLine());

        std::cout << "Tipo de Ação (1=Padrão, 2=Movimento, 3=Livre, 4=Reação): ";
        int actionChoice = parseMultiplier(getCleanLine());
        core::ActionType actType = core::ActionType::Standard;
        if (actionChoice == 2) actType = core::ActionType::Movement;
        else if (actionChoice == 3) actType = core::ActionType::Free;
        else if (actionChoice == 4) actType = core::ActionType::Reaction;

        std::cout << "Modificador numérico associado (se houver, ou 0): ";
        int mod = parseBonus(getCleanLine());

        selected.getSheet().addSkill(models::Skill(skillName, desc, pmCost, actType, mod));
        std::cout << "[OK] Poder/Magia adicionado à lista de Tormenta20!\n";
    } else if (subOpt == 3) {
        std::cout << "Índice do item para alternar Equipado (0 a " << (selected.getSheet().getInventory().size() - 1) << "): ";
        std::size_t idx = parseMultiplier(getCleanLine());
        if (idx < selected.getSheet().getInventory().size()) {
            bool currentState = selected.getSheet().getInventory().get(idx).isEquipped();
            selected.getSheet().equipItem(idx, !currentState);
            std::cout << "[OK] Status do item alterado para: " << (!currentState ? "EQUIPADO" : "DESEQUIPADO") << "\n";
            std::cout << "Nova Defesa calculada: " << selected.getSheet().getDefense() << "\n";
        } else {
            std::cout << "[ERRO] Índice inválido.\n";
        }
    } else if (subOpt == 4) {
        std::cout << "Índice do item a remover: ";
        std::size_t idx = parseMultiplier(getCleanLine());
        if (selected.getSheet().removeItem(idx)) {
            std::cout << "[OK] Item removido.\n";
        } else {
            std::cout << "[ERRO] Índice inválido.\n";
        }
    } else if (subOpt == 5) {
        std::cout << "Índice da habilidade a remover: ";
        std::size_t idx = parseMultiplier(getCleanLine());
        if (selected.getSheet().removeSkill(idx)) {
            std::cout << "[OK] Habilidade removida.\n";
        } else {
            std::cout << "[ERRO] Índice inválido.\n";
        }
    } else if (subOpt == 6) {
        std::cout << "Quantidade de PM a gastar: ";
        int pm = parseMultiplier(getCleanLine());
        if (selected.getSheet().spendPm(pm)) {
            std::cout << "[OK] " << pm << " PM gastos. PM Atual: " << selected.getSheet().getCurrentPm() << "/" << selected.getSheet().getMaxPm() << "\n";
        } else {
            std::cout << "[ERRO] Pontos de Mana insuficientes!\n";
        }
    } else if (subOpt == 7) {
        std::cout << "Quantidade de PM a recuperar: ";
        int pm = parseMultiplier(getCleanLine());
        selected.getSheet().recoverPm(pm);
        std::cout << "[OK] PM recuperados. PM Atual: " << selected.getSheet().getCurrentPm() << "/" << selected.getSheet().getMaxPm() << "\n";
    } else if (subOpt == 8) {
        std::cout << "Dano sofrido em PV: ";
        int dmg = parseMultiplier(getCleanLine());
        selected.getSheet().takeDamage(dmg);
        std::cout << "[OK] Dano aplicado. PV Atual: " << selected.getSheet().getCurrentHp() << "/" << selected.getSheet().getMaxHp() << "\n";
    } else if (subOpt == 9) {
        std::cout << "Cura recebida em PV: ";
        int heal = parseMultiplier(getCleanLine());
        selected.getSheet().healHp(heal);
        std::cout << "[OK] Cura aplicada. PV Atual: " << selected.getSheet().getCurrentHp() << "/" << selected.getSheet().getMaxHp() << "\n";
    }
}

void CLIController::handleDiceRoll() {
    std::cout << "\n--- ROLAGEM DE DADOS (TORMENTA20 JOGO DO ANO) ---\n";
    std::cout << "  1. Teste de Perícia / Ataque no T20 (1d20 + Modificador com Crítico)\n";
    std::cout << "  2. Rolar Dano de Arma com Multiplicador de Crítico de T20 (ex: 1d8 x2 +3)\n";
    std::cout << "  3. Rolar Dano com uma Arma do Inventário de um Personagem Cadastrado\n";
    std::cout << "  4. Rolar Fórmula Livre (ex: 2d6+4, 1d12-1, 3d8)\n";
    std::cout << "Opção: ";

    std::string optStr = getCleanLine();
    int opt = parseMultiplier(optStr);

    if (opt == 1) {
        std::cout << "Digite o modificador total da perícia/ataque (ex: +5, -1, ou 0): ";
        std::string modStr = getCleanLine();
        int mod = parseBonus(modStr);
        auto res = diceRoller.rollTest(mod);
        std::cout << "\nResultado do Teste de T20: " << res.format() << "\n";
    } else if (opt == 2) {
        std::cout << "\n[ROLAGEM DE DANO COM CRÍTICO DE T20]\n";
        std::cout << "Você pode digitar tudo na mesma linha (ex: 1d8 x2 +3) ou responder item por item.\n\n";
        std::cout << "Digite os dados de dano base da arma (ex: 1d8, 2d6, 1d12): ";
        std::string line = getCleanLine();

        int count = 1, sides = 8;
        if (!parseDiceString(line, count, sides)) {
            std::cout << "[AVISO] Entrada não reconhecida como dado. Usando padrão 1d8.\n";
            count = 1;
            sides = 8;
        }

        // Verifica se o usuário já especificou o multiplicador na mesma linha (ex: "1d8 x2" ou "1d8*3")
        int critMult = 1;
        std::regex multInLine(R"([xX\*]\s*(\d+))");
        std::smatch mMatch;
        if (std::regex_search(line, mMatch, multInLine)) {
            critMult = std::max(1, std::stoi(mMatch[1].str()));
        } else {
            std::cout << "Multiplicador de Crítico de T20 (ex: x2, 2, x3, 3, ou Enter para x1 normal): ";
            std::string multStr = getCleanLine();
            critMult = parseMultiplier(multStr);
        }

        // Verifica se o usuário já especificou o bônus na mesma linha (ex: "1d8+3" ou "1d8 x2 +3")
        int bonus = 0;
        std::regex bonusInLine(R"(([+-]\s*\d+)(?!\s*d))");
        std::smatch bMatch;
        if (std::regex_search(line, bMatch, bonusInLine)) {
            std::string s = bMatch[1].str();
            s.erase(std::remove_if(s.begin(), s.end(), ::isspace), s.end());
            bonus = std::stoi(s);
        } else {
            std::cout << "Bônus numérico fixo de dano (ex: +3, 3, -1, ou Enter para 0): ";
            std::string bonusStr = getCleanLine();
            bonus = parseBonus(bonusStr);
        }

        std::cout << "\nConfiguração: " << count << "d" << sides
                  << " com crítico x" << critMult
                  << (bonus >= 0 ? " +" : " ") << bonus
                  << " (Total de dados rolados no T20: " << (count * critMult) << "d" << sides << ")\n";

        auto res = diceRoller.rollDamage(count, sides, critMult, bonus);
        std::cout << "Resultado do Dano: " << res.format() << "\n";
    } else if (opt == 3) {
        if (characters.empty()) {
            std::cout << "[AVISO] Nenhum personagem cadastrado.\n";
            return;
        }
        std::cout << "\nSelecione o personagem que vai atacar:\n";
        for (std::size_t i = 0; i < characters.size(); ++i) {
            std::cout << "  " << (i + 1) << ". " << characters[i].getName() << "\n";
        }
        std::cout << "Número: ";
        std::string charChoiceStr = getCleanLine();
        std::size_t cIdx = parseMultiplier(charChoiceStr);
        if (cIdx < 1 || cIdx > characters.size()) {
            std::cout << "[ERRO] Personagem inválido.\n";
            return;
        }

        auto& hero = characters[cIdx - 1];
        auto& inv = hero.getSheet().getInventory();
        std::vector<std::size_t> weaponIndices;
        inv.for_each([&weaponIndices](const models::Item& it, std::size_t idx) {
            if (it.getType() == core::ItemType::Weapon && !it.getDamage().empty()) {
                weaponIndices.push_back(idx);
            }
        });

        if (weaponIndices.empty()) {
            std::cout << "[AVISO] Este personagem não possui armas com dano cadastrado no inventário.\n";
            return;
        }

        std::cout << "\nArmas no inventário de " << hero.getName() << ":\n";
        for (std::size_t i = 0; i < weaponIndices.size(); ++i) {
            const auto& w = inv.get(weaponIndices[i]);
            std::cout << "  " << (i + 1) << ". " << w.getName()
                      << " (Dano: " << w.getDamage() << " | Crítico: " << w.getCritical() << ")\n";
        }
        std::cout << "Escolha a arma: ";
        std::string wChoiceStr = getCleanLine();
        std::size_t wChoice = parseMultiplier(wChoiceStr);
        if (wChoice < 1 || wChoice > weaponIndices.size()) {
            std::cout << "[ERRO] Arma inválida.\n";
            return;
        }

        const auto& weapon = inv.get(weaponIndices[wChoice - 1]);
        int count = 1, sides = 8;
        parseDiceString(weapon.getDamage(), count, sides);

        int forcaMod = hero.getSheet().getAttribute(core::AttributeType::Strength).getModifier();

        std::cout << "O ataque foi Acerto Crítico? (1 = Sim / 2 = Não, dano normal): ";
        std::string critChoice = getCleanLine();
        int mult = 1;
        if (critChoice == "1" || critChoice == "s" || critChoice == "S" || critChoice == "sim" || critChoice == "Sim") {
            mult = parseMultiplier(weapon.getCritical());
            if (mult <= 1) mult = 2;
            std::cout << ">>> CRÍTICO CONFIRMADO! Multiplicador " << mult << "x ativado!\n";
        }

        std::cout << "Bônus de dano (Padrão: Mod de Força +" << forcaMod << ", ou digite outro valor): ";
        std::string customBonus = getCleanLine();
        int bonus = customBonus.empty() ? forcaMod : parseBonus(customBonus);

        auto res = diceRoller.rollDamage(count, sides, mult, bonus);
        std::cout << "\n[ATAQUE COM " << weapon.getName() << "]\n";
        std::cout << "Resultado: " << res.format() << "\n";
    } else if (opt == 4) {
        std::cout << "Digite a expressão (ex: 2d6+3, 1d10+4, 3d8): ";
        std::string expr = getCleanLine();
        auto res = diceRoller.rollExpression(expr);
        std::cout << "\nResultado: " << res.format() << "\n";
    }
}

void CLIController::handleInitiativeCombat() {
    std::cout << "\n--- COMBATE DE TORMENTA20 COM FILA DE INICIATIVA (CORE ENCOUNTER) ---\n";
    if (characters.empty()) {
        std::cout << "Cadastre personagens antes de iniciar um combate.\n";
        return;
    }

    constexpr core::ResourceId kManaResource = 1;
    // Tipos de ação específicos de Tormenta20 (definidos pelo sistema consumidor sobre o Core genérico)
    constexpr core::ActionTypeId kT20FreeAction     = 0;
    constexpr core::ActionTypeId kT20MovementAction = 1;
    constexpr core::ActionTypeId kT20StandardAction = 2;

    core::Encounter encounter;

    // Mapa entre o EntityId do Encounter e o índice no vetor characters
    std::vector<core::EntityId> charEntityIds;
    charEntityIds.reserve(characters.size());

    std::cout << "\nPersonagens cadastrados disponíveis:\n";
    for (std::size_t i = 0; i < characters.size(); ++i) {
        auto& c = characters[i];
        int dexMod = c.getSheet().getAttribute(core::AttributeType::Dexterity).getModifier();
        core::EntityId eid = encounter.addCombatant(c.getName(), dexMod, c.getSheet().getMaxHp(), 1);
        if (auto* combatant = encounter.find(eid)) {
            if (auto* hp = combatant->resource(core::kHealthResource)) {
                hp->setCurrent(c.getSheet().getCurrentHp());
            }
            combatant->addResource(kManaResource, "PM", c.getSheet().getMaxPm(), 0);
            if (auto* pm = combatant->resource(kManaResource)) {
                pm->setCurrent(c.getSheet().getCurrentPm());
            }
            // Registra as ações de Tormenta20 Jogo do Ano no Core genérico
            combatant->actions().defineActionType(kT20FreeAction, "Ação Livre", 10, -1);
            combatant->actions().defineActionType(kT20MovementAction, "Ação de Movimento", 20, 1);
            combatant->actions().defineActionType(kT20StandardAction, "Ação Padrão", 30, 1);
            combatant->actions().addFallback(kT20MovementAction, kT20StandardAction);
        }
        charEntityIds.push_back(eid);

        std::cout << "  [" << (i + 1) << "] " << c.getName()
                  << " (PV: " << c.getSheet().getCurrentHp() << "/" << c.getSheet().getMaxHp()
                  << " | PM: " << c.getSheet().getCurrentPm() << "/" << c.getSheet().getMaxPm()
                  << " | DES: " << (dexMod >= 0 ? "+" : "") << dexMod << ")\n";
    }

    std::cout << "\nQuais personagens vão participar da lista de iniciativa?\n";
    std::cout << "Digite os números separados por espaço/vírgula (ex: 1 2) ou Enter/T para Todos: ";
    std::string selLine = getCleanLine();

    ds::LinkedList<core::EntityId> selectedIds;
    std::vector<std::size_t> selectedCharIndices;

    if (selLine.empty() || selLine == "t" || selLine == "T" || selLine == "todos" || selLine == "Todos") {
        for (std::size_t i = 0; i < charEntityIds.size(); ++i) {
            selectedIds.push_back(charEntityIds[i]);
            selectedCharIndices.push_back(i);
        }
    } else {
        std::regex numRgx(R"(\d+)");
        auto begin = std::sregex_iterator(selLine.begin(), selLine.end(), numRgx);
        auto end = std::sregex_iterator();
        for (auto it = begin; it != end; ++it) {
            std::size_t idx = static_cast<std::size_t>(std::stoul(it->str()));
            if (idx >= 1 && idx <= charEntityIds.size()) {
                core::EntityId eid = charEntityIds[idx - 1];
                if (selectedIds.find_if([eid](core::EntityId id) { return id == eid; }) == nullptr) {
                    selectedIds.push_back(eid);
                    selectedCharIndices.push_back(idx - 1);
                }
            } else {
                std::cout << "  [AVISO] Índice " << idx << " ignorado (fora do intervalo).\n";
            }
        }
    }

    if (selectedIds.is_empty()) {
        std::cout << "[AVISO] Nenhum combatente selecionado para a iniciativa. Combate cancelado.\n";
        return;
    }

    std::cout << "\nRolando Iniciativa (1d20 + Destreza) apenas para os " << selectedIds.size()
              << " participantes selecionados...\n";

    // Rola a iniciativa de uma vez para a lista selecionada no Core
    encounter.rollInitiativeForList(selectedIds, 1, 20);

    for (std::size_t charIdx : selectedCharIndices) {
        core::EntityId eid = charEntityIds[charIdx];
        int initVal = encounter.tracker().initiativeOf(eid);
        characters[charIdx].setInitiativeRoll(initVal);
        std::cout << "  - " << characters[charIdx].getName() << " entrou na iniciativa com total: " << initVal << "\n";
    }

    if (!encounter.startCombat()) {
        std::cout << "[ERRO] Não há combatentes ativos(as) com PV > 0 para iniciar o combate.\n";
        return;
    }

    auto findCharIndexByEntity = [&](core::EntityId eid) -> std::size_t {
        for (std::size_t i = 0; i < charEntityIds.size(); ++i) {
            if (charEntityIds[i] == eid) return i;
        }
        return characters.size();
    };

    auto syncBackToSheets = [&]() {
        for (std::size_t i = 0; i < charEntityIds.size(); ++i) {
            if (const auto* c = encounter.find(charEntityIds[i])) {
                if (const auto* hp = c->resource(core::kHealthResource)) {
                    int diff = hp->current() - characters[i].getSheet().getCurrentHp();
                    if (diff < 0) characters[i].getSheet().takeDamage(-diff);
                    else if (diff > 0) characters[i].getSheet().healHp(diff);
                }
                if (const auto* pm = c->resource(kManaResource)) {
                    int diff = pm->current() - characters[i].getSheet().getCurrentPm();
                    if (diff < 0) characters[i].getSheet().spendPm(-diff);
                    else if (diff > 0) characters[i].getSheet().recoverPm(diff);
                }
            }
        }
    };

    auto selectParticipantTarget = [&](const std::string& prompt) -> core::EntityId {
        std::cout << "\n" << prompt << "\n";
        const auto& entries = encounter.tracker().entries();
        std::vector<core::EntityId> orderIds;
        entries.for_each([&](const core::InitiativeEntry& e, std::size_t idx) {
            const auto* comb = encounter.find(e.id);
            if (!comb) return;
            const auto* hp = comb->resource(core::kHealthResource);
            orderIds.push_back(e.id);
            std::cout << "  " << (idx + 1) << ". [ID " << e.id << "] " << comb->name()
                      << " (PV: " << (hp ? hp->current() : 0) << "/" << (hp ? hp->max() : 0)
                      << (comb->isDefeated() ? " - FORA DE COMBATE/CAÍDO" : "") << ")\n";
        });
        std::cout << "Escolha o número na lista (1 a " << orderIds.size() << "): ";
        std::size_t choice = static_cast<std::size_t>(parseMultiplier(getCleanLine()));
        if (choice < 1 || choice > orderIds.size()) {
            std::cout << "[ERRO] Alvo inválido.\n";
            return core::kInvalidId;
        }
        return orderIds[choice - 1];
    };

    std::cout << "\nCombate iniciado! Ações durante o turno NÃO avançam a vez automaticamente.\n";

    bool inCombat = true;
    while (inCombat && encounter.isCombatActive()) {
        core::EntityId currentId = encounter.currentTurn();
        core::Combatant* current = encounter.find(currentId);
        if (current == nullptr) {
            break;
        }

        std::size_t charIdx = findCharIndexByEntity(currentId);
        const auto* hp = current->resource(core::kHealthResource);
        const auto* pm = current->resource(kManaResource);
        int defense = (charIdx < characters.size()) ? characters[charIdx].getSheet().getDefense() : 10;
        int initVal = encounter.tracker().initiativeOf(currentId);

        auto actionTypeName = [](core::ActionTypeId t) -> std::string {
            if (t == kT20FreeAction) return "Ação Livre";
            if (t == kT20MovementAction) return "Ação de Movimento";
            if (t == kT20StandardAction) return "Ação Comum/Padrão";
            return "Ação";
        };

        auto mapModelActionToCore = [](core::ActionType act) -> core::ActionTypeId {
            switch (act) {
                case core::ActionType::Free: return kT20FreeAction;
                case core::ActionType::Movement: return kT20MovementAction;
                case core::ActionType::Standard: return kT20StandardAction;
                case core::ActionType::Full: return kT20StandardAction;
                case core::ActionType::Reaction: return kT20FreeAction;
            }
            return kT20StandardAction;
        };

        const auto* stdSlot = current->actions().slot(kT20StandardAction);
        const auto* movSlot = current->actions().slot(kT20MovementAction);

        std::cout << "\n======================================================================\n";
        std::cout << ">> [RODADA " << encounter.round() << "]\n";
        std::cout << ">> VEZ DE: " << current->name() << " (ID " << currentId << ")"
                  << " | PV: " << (hp ? hp->current() : 0) << "/" << (hp ? hp->max() : 0)
                  << " | PM: " << (pm ? pm->current() : 0) << "/" << (pm ? pm->max() : 0)
                  << " | DEFESA: " << defense
                  << " | INICIATIVA: " << initVal << "\n";
        std::cout << ">> AÇÕES NO TURNO: Comum/Padrão: " << (stdSlot ? stdSlot->current : 0) << "/" << (stdSlot ? stdSlot->maxPerTurn : 0)
                  << " | Movimento: " << (movSlot ? movSlot->current : 0) << "/" << (movSlot ? movSlot->maxPerTurn : 0)
                  << " | Livre: Ilimitadas\n";

        if (!current->actions().isQueueEmpty()) {
            std::cout << ">> FILA DE PRIORIDADES DO TURNO (Ordem: Livre -> Movimento -> Comum):\n";
            current->actions().queue().for_each([&](const core::QueuedAction& qa, int, std::size_t idx) {
                std::cout << "     " << (idx + 1) << "º. [" << actionTypeName(qa.requiredType) << "] " << qa.name;
                if (qa.requiredType != qa.consumedType) {
                    std::cout << " (Pagou usando " << actionTypeName(qa.consumedType) << ")";
                }
                std::cout << "\n";
            });
        }
        std::cout << "======================================================================\n";

        std::cout << "Ações de Tormenta20 no Turno:\n";
        std::cout << "  1. Encerrar Turno (Executa a Fila de Prioridades: Livre -> Movimento -> Comum e passa a vez)\n";
        std::cout << "  2. Realizar Ataque Padrão (Consome Ação Comum/Padrão; bloqueia se só tiver Movimento)\n";
        std::cout << "  3. Realizar Ação de Movimento (Consome Movimento, ou usa Ação Comum/Padrão se não tiver Movimento)\n";
        std::cout << "  4. Usar Poder / Habilidade / Magia (Enfileira por Prioridade: Livre -> Movimento -> Comum)\n";
        std::cout << "  5. Executar Próxima Ação da Fila de Prioridades Agora\n";
        std::cout << "  6. Causar Dano a um Alvo (Selecionar por ID)\n";
        std::cout << "  7. Curar um Alvo (Selecionar por ID)\n";
        std::cout << "  8. Alternar Estado Fora de Combate / Morrido de um Combatente\n";
        std::cout << "  9. Encerrar Combate\n";
        std::cout << "Escolha uma ação: ";

        std::string actionStr = getCleanLine();
        int action = parseMultiplier(actionStr);

        if (action == 1) {
            if (!current->actions().isQueueEmpty()) {
                std::cout << "\n>> Executando Fila de Prioridades do turno (Livre -> Movimento -> Comum):\n";
                std::size_t step = 1;
                core::QueuedAction qa;
                while (current->actions().popNextAction(qa)) {
                    std::cout << "   [" << step++ << "º] (" << actionTypeName(qa.requiredType) << ") " << qa.name << " executada!\n";
                }
            }
            core::EntityId nextId = encounter.nextTurn();
            if (nextId == core::kInvalidId) {
                std::cout << "\n[FIM] Nenhum combatente ativo restante na iniciativa.\n";
                inCombat = false;
            }
        } else if (action == 2) {
            core::ActionTypeId consumed = core::kInvalidActionType;
            int actId = encounter.enqueueAction(currentId, kT20StandardAction, "Agredir (Ataque Padrão)", 0, &consumed);
            if (actId <= 0) {
                std::cout << "[BLOQUEADO] Sem Ação Comum/Padrão disponível neste turno (Ação de Movimento NÃO pode pagar Ação Padrão)!\n";
            } else {
                std::cout << "[FILA DE PRIORIDADES] 'Agredir' enfileirada como Ação Comum/Padrão.\n";
                std::cout << "Modificador do ataque/perícia (ex: +6, 0): ";
                int mod = parseBonus(getCleanLine());
                auto roll = diceRoller.rollTest(mod);
                std::cout << "Resultado: " << roll.format() << "\n";
            }
        } else if (action == 3) {
            std::cout << "Descrição do Movimento (ex: Deslocar 9m, Sacar Arma): ";
            std::string movDesc = getCleanLine();
            if (movDesc.empty()) movDesc = "Movimentar-se";

            core::ActionTypeId consumed = core::kInvalidActionType;
            int actId = encounter.enqueueAction(currentId, kT20MovementAction, movDesc, 0, &consumed);
            if (actId <= 0) {
                std::cout << "[BLOQUEADO] Sem Ação de Movimento nem Ação Comum/Padrão disponível neste turno!\n";
            } else {
                std::cout << "[FILA DE PRIORIDADES] '" << movDesc << "' enfileirada como Ação de Movimento";
                if (consumed == kT20StandardAction) {
                    std::cout << " (Conversão: utilizou a Ação Comum/Padrão como Ação de Movimento!)";
                }
                std::cout << ".\n";
            }
        } else if (action == 4) {
            int pmCost = 0;
            std::string skName = "Poder/Magia";
            core::ActionTypeId reqType = kT20StandardAction;

            if (charIdx < characters.size() && !characters[charIdx].getSheet().getSkills().is_empty()) {
                const auto& skills = characters[charIdx].getSheet().getSkills();
                std::cout << "\nHabilidades de " << current->name() << ":\n";
                skills.for_each([&](const models::Skill& sk, std::size_t idx) {
                    std::cout << "  " << (idx + 1) << ". " << sk.getName()
                              << " [" << actionTypeName(mapModelActionToCore(sk.getActionType()))
                              << " | " << sk.getPmCost() << " PM] - " << sk.getDescription() << "\n";
                });
                std::cout << "  " << (skills.size() + 1) << ". Digitar poder/magia avulso\n";
                std::cout << "Escolha a habilidade: ";
                std::size_t skChoice = static_cast<std::size_t>(parseMultiplier(getCleanLine()));

                if (skChoice >= 1 && skChoice <= skills.size()) {
                    const auto& chosenSkill = skills.get(skChoice - 1);
                    pmCost = chosenSkill.getPmCost();
                    skName = chosenSkill.getName();
                    reqType = mapModelActionToCore(chosenSkill.getActionType());
                } else {
                    std::cout << "Tipo de Ação (1=Livre, 2=Movimento, 3=Comum/Padrão): ";
                    int tChoice = parseMultiplier(getCleanLine());
                    if (tChoice == 1) reqType = kT20FreeAction;
                    else if (tChoice == 2) reqType = kT20MovementAction;
                    else reqType = kT20StandardAction;

                    std::cout << "Custo em PM: ";
                    pmCost = parseMultiplier(getCleanLine());
                }
            } else {
                std::cout << "Tipo de Ação (1=Livre, 2=Movimento, 3=Comum/Padrão): ";
                int tChoice = parseMultiplier(getCleanLine());
                if (tChoice == 1) reqType = kT20FreeAction;
                else if (tChoice == 2) reqType = kT20MovementAction;
                else reqType = kT20StandardAction;

                std::cout << "Custo em PM do poder/magia: ";
                pmCost = parseMultiplier(getCleanLine());
            }

            if (pm && pm->current() < pmCost) {
                std::cout << "[AVISO] PM insuficiente para usar '" << skName << "' (" << pmCost << " PM necessários)!\n";
            } else {
                core::ActionTypeId consumed = core::kInvalidActionType;
                int actId = encounter.enqueueAction(currentId, reqType, skName, 0, &consumed);
                if (actId <= 0) {
                    std::cout << "[BLOQUEADO] Ações insuficientes no turno para realizar '" << skName
                              << "' (" << actionTypeName(reqType) << ")!\n";
                } else {
                    if (pmCost > 0) {
                        encounter.spendResource(currentId, kManaResource, pmCost);
                        syncBackToSheets();
                    }
                    std::cout << "[FILA DE PRIORIDADES] '" << skName << "' [" << actionTypeName(reqType)
                              << "] enfileirada! (" << pmCost << " PM gastos)";
                    if (reqType != consumed) {
                        std::cout << " [Conversão: usou " << actionTypeName(consumed) << " como " << actionTypeName(reqType) << "]";
                    }
                    std::cout << "\n";
                }
            }
        } else if (action == 5) {
            core::QueuedAction nextAct;
            if (encounter.popNextAction(currentId, nextAct)) {
                std::cout << "[EXECUTADA] (" << actionTypeName(nextAct.requiredType) << ") " << nextAct.name
                          << " foi consumida da frente da Fila de Prioridades!\n";
            } else {
                std::cout << "[AVISO] A Fila de Prioridades de ações deste turno está vazia.\n";
            }
        } else if (action == 6) {
            core::EntityId targetId = selectParticipantTarget("Selecione o ALVO que sofrerá o dano:");
            if (targetId != core::kInvalidId) {
                std::cout << "Quantidade de dano: ";
                int dmgAmt = parseMultiplier(getCleanLine());
                auto res = encounter.dealDamage(targetId, dmgAmt);
                syncBackToSheets();
                const auto* targetComb = encounter.find(targetId);
                std::cout << "[OK] " << (targetComb ? targetComb->name() : "Alvo")
                          << " sofreu " << res.finalAmount << " de dano!"
                          << (res.depleted ? " >>> O ALVO CAIU (0 PV) E SERÁ PULADO NA INICIATIVA!" : "") << "\n";
            }
        } else if (action == 7) {
            core::EntityId targetId = selectParticipantTarget("Selecione o ALVO que receberá a cura:");
            if (targetId != core::kInvalidId) {
                std::cout << "Quantidade de cura (PV): ";
                int healAmt = parseMultiplier(getCleanLine());
                int healed = encounter.heal(targetId, healAmt);
                syncBackToSheets();
                const auto* targetComb = encounter.find(targetId);
                std::cout << "[OK] " << (targetComb ? targetComb->name() : "Alvo")
                          << " recuperou " << healed << " PV!\n";
            }
        } else if (action == 8) {
            core::EntityId targetId = selectParticipantTarget("Selecione o combatente para alternar Fora de Combate:");
            if (targetId != core::kInvalidId) {
                if (auto* targetComb = encounter.find(targetId)) {
                    bool newState = !targetComb->isForcedOut();
                    targetComb->setForcedOut(newState);
                    encounter.tracker().setActive(targetId, !targetComb->isDefeated());
                    std::cout << "[OK] " << targetComb->name() << " agora está "
                              << (targetComb->isDefeated() ? "FORA DE COMBATE" : "ATIVO NO COMBATE") << ".\n";
                }
            }
        } else if (action == 9) {
            syncBackToSheets();
            encounter.endCombat();
            std::cout << "Combate de Tormenta20 encerrado.\n";
            inCombat = false;
        }
    }
    syncBackToSheets();
}

void CLIController::run() {
    printHeader();
    int option = -1;

    while (option != 0) {
        showMainMenu();
        std::string optStr = getCleanLine();
        if (optStr.empty()) {
            continue;
        }

        try {
            option = std::stoi(optStr);
        } catch (...) {
            std::cout << "\nOpção inválida. Digite um número do menu.\n";
            continue;
        }

        switch (option) {
            case 1:
                handleCreateCharacter();
                break;
            case 2:
                handleListCharacters();
                break;
            case 3:
                handleManageCharacter();
                break;
            case 4:
                handleDiceRoll();
                break;
            case 5:
                handleInitiativeCombat();
                break;
            case 0:
                std::cout << "\nEncerrando a sessão de Tormenta20 no OpenTTRPG. Glória a Arton!\n";
                break;
            default:
                std::cout << "\nOpção inválida. Tente novamente.\n";
                break;
        }
    }
}

} // namespace openttrpg::cli
