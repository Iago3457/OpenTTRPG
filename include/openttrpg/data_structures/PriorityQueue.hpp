#pragma once

#include "Node.hpp"
#include <cstddef>
#include <stdexcept>
#include <utility>

namespace openttrpg::ds {

/**
 * @brief Entrada interna da Fila de Prioridades (AED1).
 *
 * Ordena por `priority` (menor valor numérico = maior prioridade, ou seja, sai primeiro da fila)
 * e mantém estabilidade FIFO por `sequence` em caso de empate de prioridade.
 */
template <typename T>
struct PriorityEntry {
    T data;
    int priority = 0;
    long sequence = 0;
};

/**
 * @brief Fila de Prioridades encadeada e estável (AED1).
 *
 * Elementos com menor valor de `priority` ficam na frente da fila:
 * Ex.: Ação Livre (prioridade 10) -> Ação de Movimento (prioridade 20) -> Ação Comum/Padrão (prioridade 30).
 * Elementos de mesma prioridade respeitam a ordem de chegada (FIFO).
 */
template <typename T>
class PriorityQueue {
private:
    Node<PriorityEntry<T>>* head_;
    std::size_t count_;
    long nextSequence_;

    static bool hasHigherPriority(const PriorityEntry<T>& a, const PriorityEntry<T>& b) {
        if (a.priority != b.priority) {
            return a.priority < b.priority;
        }
        return a.sequence < b.sequence;
    }

public:
    PriorityQueue() : head_(nullptr), count_(0), nextSequence_(0) {}

    ~PriorityQueue() {
        clear();
    }

    PriorityQueue(const PriorityQueue& other) : head_(nullptr), count_(0), nextSequence_(0) {
        Node<PriorityEntry<T>>* curr = other.head_;
        while (curr != nullptr) {
            enqueue(curr->data.data, curr->data.priority);
            curr = curr->next;
        }
    }

    PriorityQueue& operator=(const PriorityQueue& other) {
        if (this != &other) {
            clear();
            Node<PriorityEntry<T>>* curr = other.head_;
            while (curr != nullptr) {
                enqueue(curr->data.data, curr->data.priority);
                curr = curr->next;
            }
        }
        return *this;
    }

    PriorityQueue(PriorityQueue&& other) noexcept
        : head_(other.head_), count_(other.count_), nextSequence_(other.nextSequence_) {
        other.head_ = nullptr;
        other.count_ = 0;
        other.nextSequence_ = 0;
    }

    PriorityQueue& operator=(PriorityQueue&& other) noexcept {
        if (this != &other) {
            clear();
            head_ = other.head_;
            count_ = other.count_;
            nextSequence_ = other.nextSequence_;
            other.head_ = nullptr;
            other.count_ = 0;
            other.nextSequence_ = 0;
        }
        return *this;
    }

    /**
     * @brief Insere um elemento na fila de acordo com sua prioridade.
     * @param value Elemento a inserir.
     * @param priority Prioridade (menor valor sai primeiro: ex. 0 = Livre, 1 = Movimento, 2 = Comum).
     */
    void enqueue(const T& value, int priority) {
        PriorityEntry<T> entry{value, priority, nextSequence_++};
        auto* newNode = new Node<PriorityEntry<T>>(entry);

        if (head_ == nullptr || hasHigherPriority(entry, head_->data)) {
            newNode->next = head_;
            head_ = newNode;
        } else {
            Node<PriorityEntry<T>>* curr = head_;
            while (curr->next != nullptr && !hasHigherPriority(entry, curr->next->data)) {
                curr = curr->next;
            }
            newNode->next = curr->next;
            curr->next = newNode;
        }
        ++count_;
    }

    /**
     * @brief Remove e retorna o elemento de maior prioridade (frente da fila).
     */
    T dequeue() {
        if (is_empty()) {
            throw std::underflow_error("Tentativa de dequeue em PriorityQueue vazia.");
        }
        Node<PriorityEntry<T>>* temp = head_;
        T val = temp->data.data;
        head_ = head_->next;
        delete temp;
        --count_;
        return val;
    }

    [[nodiscard]] T& front() {
        if (is_empty()) {
            throw std::underflow_error("Tentativa de acessar front em PriorityQueue vazia.");
        }
        return head_->data.data;
    }

    [[nodiscard]] const T& front() const {
        if (is_empty()) {
            throw std::underflow_error("Tentativa de acessar front em PriorityQueue vazia.");
        }
        return head_->data.data;
    }

    [[nodiscard]] int frontPriority() const {
        if (is_empty()) {
            throw std::underflow_error("Tentativa de acessar frontPriority em PriorityQueue vazia.");
        }
        return head_->data.priority;
    }

    [[nodiscard]] const T& get(std::size_t index) const {
        if (index >= count_) {
            throw std::out_of_range("Índice fora dos limites na PriorityQueue.");
        }
        Node<PriorityEntry<T>>* curr = head_;
        for (std::size_t i = 0; i < index; ++i) {
            curr = curr->next;
        }
        return curr->data.data;
    }

    template <typename Predicate>
    bool remove_if(Predicate pred, T* outRemoved = nullptr) {
        Node<PriorityEntry<T>>* prev = nullptr;
        Node<PriorityEntry<T>>* curr = head_;
        while (curr != nullptr) {
            if (pred(curr->data.data)) {
                if (outRemoved != nullptr) {
                    *outRemoved = curr->data.data;
                }
                if (prev == nullptr) {
                    head_ = curr->next;
                } else {
                    prev->next = curr->next;
                }
                delete curr;
                --count_;
                return true;
            }
            prev = curr;
            curr = curr->next;
        }
        return false;
    }

    template <typename Func>
    void for_each(Func fn) const {
        Node<PriorityEntry<T>>* curr = head_;
        std::size_t idx = 0;
        while (curr != nullptr) {
            fn(curr->data.data, curr->data.priority, idx++);
            curr = curr->next;
        }
    }

    void clear() {
        while (head_ != nullptr) {
            Node<PriorityEntry<T>>* temp = head_;
            head_ = head_->next;
            delete temp;
        }
        count_ = 0;
        nextSequence_ = 0;
    }

    [[nodiscard]] std::size_t size() const {
        return count_;
    }

    [[nodiscard]] bool is_empty() const {
        return count_ == 0;
    }
};

} // namespace openttrpg::ds

