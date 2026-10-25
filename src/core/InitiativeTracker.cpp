#include "openttrpg/core/InitiativeTracker.hpp"

namespace openttrpg::core {

InitiativeTracker::InitiativeTracker()
    : order_(InitiativeOrder::HighestFirst),
      currentIndex_(0),
      started_(false),
      currentRemoved_(false),
      round_(0),
      turnCount_(0),
      nextSequence_(0) {}

// ---------------------------------------------------------------------------
// Auxiliares internos
// ---------------------------------------------------------------------------

bool InitiativeTracker::comesBefore(const InitiativeEntry& a, const InitiativeEntry& b) const {
    if (a.initiative != b.initiative) {
        return order_ == InitiativeOrder::HighestFirst ? a.initiative > b.initiative
                                                       : a.initiative < b.initiative;
    }
    if (a.tiebreaker != b.tiebreaker) {
        return a.tiebreaker > b.tiebreaker; // Desempate: maior modificador age antes
    }
    return a.sequence < b.sequence;         // Desempate final: quem entrou primeiro
}

std::size_t InitiativeTracker::indexOf(EntityId id) const {
    return entries_.find_index([id](const InitiativeEntry& e) { return e.id == id; });
}

std::size_t InitiativeTracker::insertSorted(const InitiativeEntry& entry) {
    // Inserção ordenada em lista encadeada: primeira posição onde o novo vem antes do existente
    std::size_t pos = entries_.find_index([&](const InitiativeEntry& e) { return comesBefore(entry, e); });
    if (pos == npos) {
        pos = entries_.size();
    }
    entries_.insert_at(pos, entry);

    if (started_) {
        if (currentRemoved_) {
            // Cursor aponta para o "próximo pendente": se inserir exatamente nele, o novo age antes
            if (pos < currentIndex_) {
                ++currentIndex_;
            }
        } else if (pos <= currentIndex_) {
            // Entrou antes do atual: só age na próxima rodada
            ++currentIndex_;
        }
    }
    return pos;
}

void InitiativeTracker::removeAtIndex(std::size_t index) {
    entries_.remove_at(index);
    if (!started_) {
        return;
    }
    if (index < currentIndex_) {
        --currentIndex_;
    } else if (index == currentIndex_ && !currentRemoved_) {
        // O atual saiu: o cursor já aponta para quem vem depois dele
        currentRemoved_ = true;
    }
    if (entries_.is_empty()) {
        currentIndex_ = 0;
    }
}

// ---------------------------------------------------------------------------
// Configuração
// ---------------------------------------------------------------------------

bool InitiativeTracker::setOrder(InitiativeOrder order) {
    if (started_) {
        return false;
    }
    order_ = order;
    ds::LinkedList<InitiativeEntry> old = entries_;
    entries_.clear();
    old.for_each([this](const InitiativeEntry& e, std::size_t) { insertSorted(e); });
    return true;
}

InitiativeOrder InitiativeTracker::order() const { return order_; }

// ---------------------------------------------------------------------------
// Montagem da ordem
// ---------------------------------------------------------------------------

bool InitiativeTracker::add(EntityId id, int initiative, int tiebreaker) {
    if (id == kInvalidId || contains(id)) {
        return false;
    }
    InitiativeEntry entry;
    entry.id = id;
    entry.initiative = initiative;
    entry.tiebreaker = tiebreaker;
    entry.active = true;
    entry.sequence = nextSequence_++;
    insertSorted(entry);
    return true;
}

bool InitiativeTracker::remove(EntityId id) {
    const std::size_t idx = indexOf(id);
    if (idx == npos) {
        return false;
    }
    removeAtIndex(idx);
    return true;
}

bool InitiativeTracker::setInitiative(EntityId id, int initiative) {
    const std::size_t idx = indexOf(id);
    if (idx == npos) {
        return false;
    }
    return setInitiative(id, initiative, entries_.get(idx).tiebreaker);
}

bool InitiativeTracker::setInitiative(EntityId id, int initiative, int tiebreaker) {
    const std::size_t idx = indexOf(id);
    if (idx == npos) {
        return false;
    }

    InitiativeEntry entry = entries_.get(idx);
    const bool wasCurrent = started_ && !currentRemoved_ && idx == currentIndex_;

    removeAtIndex(idx); // se era o atual, currentRemoved_ = true e o cursor aponta para o próximo
    entry.initiative = initiative;
    entry.tiebreaker = tiebreaker;
    entry.sequence = nextSequence_++;
    const std::size_t pos = insertSorted(entry);

    if (wasCurrent && pos == currentIndex_) {
        // Ficou imediatamente antes do próximo pendente: não deve agir duas vezes seguidas
        ++currentIndex_;
    }
    return true;
}

bool InitiativeTracker::setActive(EntityId id, bool active) {
    InitiativeEntry* e = entries_.find_if([id](const InitiativeEntry& x) { return x.id == id; });
    if (e == nullptr) {
        return false;
    }
    e->active = active;
    return true;
}

void InitiativeTracker::clear() {
    entries_.clear();
    nextSequence_ = 0;
    reset();
}

// ---------------------------------------------------------------------------
// Consultas
// ---------------------------------------------------------------------------

bool InitiativeTracker::contains(EntityId id) const {
    return indexOf(id) != npos;
}

bool InitiativeTracker::isActive(EntityId id) const {
    const InitiativeEntry* e = entries_.find_if([id](const InitiativeEntry& x) { return x.id == id; });
    return e != nullptr && e->active;
}

int InitiativeTracker::initiativeOf(EntityId id) const {
    const InitiativeEntry* e = entries_.find_if([id](const InitiativeEntry& x) { return x.id == id; });
    return e != nullptr ? e->initiative : 0;
}

std::size_t InitiativeTracker::positionOf(EntityId id) const {
    return indexOf(id);
}

std::size_t InitiativeTracker::size() const {
    return entries_.size();
}

std::size_t InitiativeTracker::activeCount() const {
    std::size_t n = 0;
    entries_.for_each([&n](const InitiativeEntry& e, std::size_t) {
        if (e.active) {
            ++n;
        }
    });
    return n;
}

EntityId InitiativeTracker::at(std::size_t index) const {
    return index < entries_.size() ? entries_.get(index).id : kInvalidId;
}

const ds::LinkedList<InitiativeEntry>& InitiativeTracker::entries() const {
    return entries_;
}

// ---------------------------------------------------------------------------
// Fluxo de combate
// ---------------------------------------------------------------------------

bool InitiativeTracker::start() {
    const std::size_t first = entries_.find_index([](const InitiativeEntry& e) { return e.active; });
    if (first == npos) {
        return false;
    }
    started_ = true;
    currentRemoved_ = false;
    currentIndex_ = first;
    round_ = 1;
    turnCount_ = 1;
    return true;
}

EntityId InitiativeTracker::next() {
    if (!started_ || activeCount() == 0) {
        return kInvalidId;
    }

    const std::size_t n = entries_.size();
    std::size_t candidate = currentRemoved_ ? currentIndex_ : currentIndex_ + 1;
    currentRemoved_ = false;

    // Com ao menos 1 ativo, no máximo n+1 passos encontram alguém (dando no máximo uma volta)
    for (std::size_t steps = 0; steps <= n; ++steps, ++candidate) {
        if (candidate >= n) {
            candidate = 0;
            ++round_; // Passou do fim da lista: nova rodada
        }
        const InitiativeEntry& e = entries_.get(candidate);
        if (e.active) {
            currentIndex_ = candidate;
            ++turnCount_;
            return e.id;
        }
    }
    return kInvalidId; // inalcançável
}

void InitiativeTracker::reset() {
    started_ = false;
    currentRemoved_ = false;
    currentIndex_ = 0;
    round_ = 0;
    turnCount_ = 0;
}

bool InitiativeTracker::isStarted() const { return started_; }

EntityId InitiativeTracker::current() const {
    if (!started_ || currentRemoved_ || currentIndex_ >= entries_.size()) {
        return kInvalidId;
    }
    return entries_.get(currentIndex_).id;
}

EntityId InitiativeTracker::peekNext() const {
    if (!started_ || activeCount() == 0) {
        return kInvalidId;
    }
    const std::size_t n = entries_.size();
    std::size_t candidate = currentRemoved_ ? currentIndex_ : currentIndex_ + 1;
    for (std::size_t steps = 0; steps <= n; ++steps, ++candidate) {
        if (candidate >= n) {
            candidate = 0;
        }
        const InitiativeEntry& e = entries_.get(candidate);
        if (e.active) {
            return e.id;
        }
    }
    return kInvalidId;
}

int InitiativeTracker::round() const { return round_; }
int InitiativeTracker::turnCount() const { return turnCount_; }

} // namespace openttrpg::core
