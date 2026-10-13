#pragma once

#include "Node.hpp"
#include <cstddef>
#include <stdexcept>
#include <functional>

namespace openttrpg::ds {

/**
 * @brief Lista Encadeada Simples para cadastros (inventário, habilidades, etc.) - AED1
 * @tparam T Tipo armazenado na lista.
 */
template <typename T>
class LinkedList {
private:
    Node<T>* head;
    std::size_t count;

public:
    LinkedList() : head(nullptr), count(0) {}

    ~LinkedList() {
        clear();
    }

    LinkedList(const LinkedList& other) : head(nullptr), count(0) {
        Node<T>* curr = other.head;
        while (curr != nullptr) {
            push_back(curr->data);
            curr = curr->next;
        }
    }

    LinkedList& operator=(const LinkedList& other) {
        if (this != &other) {
            clear();
            Node<T>* curr = other.head;
            while (curr != nullptr) {
                push_back(curr->data);
                curr = curr->next;
            }
        }
        return *this;
    }

    LinkedList(LinkedList&& other) noexcept : head(other.head), count(other.count) {
        other.head = nullptr;
        other.count = 0;
    }

    LinkedList& operator=(LinkedList&& other) noexcept {
        if (this != &other) {
            clear();
            head = other.head;
            count = other.count;
            other.head = nullptr;
            other.count = 0;
        }
        return *this;
    }

    void push_back(const T& value) {
        Node<T>* newNode = new Node<T>(value);
        if (head == nullptr) {
            head = newNode;
        } else {
            Node<T>* curr = head;
            while (curr->next != nullptr) {
                curr = curr->next;
            }
            curr->next = newNode;
        }
        count++;
    }

    void push_front(const T& value) {
        Node<T>* newNode = new Node<T>(value, head);
        head = newNode;
        count++;
    }

    bool remove_at(std::size_t index) {
        if (index >= count || head == nullptr) {
            return false;
        }

        Node<T>* toDelete = nullptr;
        if (index == 0) {
            toDelete = head;
            head = head->next;
        } else {
            Node<T>* curr = head;
            for (std::size_t i = 0; i < index - 1; ++i) {
                curr = curr->next;
            }
            toDelete = curr->next;
            curr->next = toDelete->next;
        }

        delete toDelete;
        count--;
        return true;
    }

    T& get(std::size_t index) {
        if (index >= count) {
            throw std::out_of_range("Índice fora dos limites da LinkedList.");
        }
        Node<T>* curr = head;
        for (std::size_t i = 0; i < index; ++i) {
            curr = curr->next;
        }
        return curr->data;
    }

    const T& get(std::size_t index) const {
        if (index >= count) {
            throw std::out_of_range("Índice fora dos limites da LinkedList.");
        }
        Node<T>* curr = head;
        for (std::size_t i = 0; i < index; ++i) {
            curr = curr->next;
        }
        return curr->data;
    }

    void for_each(const std::function<void(const T&, std::size_t)>& callback) const {
        Node<T>* curr = head;
        std::size_t idx = 0;
        while (curr != nullptr) {
            callback(curr->data, idx++);
            curr = curr->next;
        }
    }

    void clear() {
        Node<T>* curr = head;
        while (curr != nullptr) {
            Node<T>* nextNode = curr->next;
            delete curr;
            curr = nextNode;
        }
        head = nullptr;
        count = 0;
    }

    [[nodiscard]] std::size_t size() const {
        return count;
    }

    [[nodiscard]] bool is_empty() const {
        return count == 0;
    }
};

} // namespace openttrpg::ds
