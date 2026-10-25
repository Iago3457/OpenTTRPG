CXX = g++
CXXFLAGS = -std=c++17 -Wall -Wextra -O2 -fPIC -Iinclude

SRC_DIR = src
INC_DIR = include
BUILD_DIR = build
BIN_DIR = bin
TEST_DIR = tests

# Fontes da aplicação (excluindo main para poder compilar testes e DLL separadamente)
CORE_SRCS = $(filter-out $(SRC_DIR)/main.cpp, $(shell find $(SRC_DIR) -name '*.cpp' 2>/dev/null))
CORE_OBJS = $(patsubst $(SRC_DIR)/%.cpp, $(BUILD_DIR)/%.o, $(CORE_SRCS))

MAIN_SRC = $(SRC_DIR)/main.cpp
MAIN_OBJ = $(BUILD_DIR)/main.o

CLI_TARGET = $(BIN_DIR)/openttrpg_cli
TEST_TARGET = $(BIN_DIR)/test_data_structures
TEST_GENERIC_TARGET = $(BIN_DIR)/test_generic_core
SHARED_TARGET = $(BIN_DIR)/libopenttrpg_core.so

.PHONY: all clean run test test_generic lib dirs

all: dirs $(CLI_TARGET) $(SHARED_TARGET)

dirs:
	@mkdir -p $(BUILD_DIR) $(BIN_DIR)

$(BUILD_DIR)/%.o: $(SRC_DIR)/%.cpp
	@mkdir -p $(dir $@)
	$(CXX) $(CXXFLAGS) -c $< -o $@

$(CLI_TARGET): $(CORE_OBJS) $(MAIN_OBJ)
	@mkdir -p $(BIN_DIR)
	$(CXX) $(CXXFLAGS) $(CORE_OBJS) $(MAIN_OBJ) -o $@
	@echo "==> Executável gerado: $(CLI_TARGET)"

lib: $(SHARED_TARGET)

$(SHARED_TARGET): $(CORE_OBJS)
	@mkdir -p $(BIN_DIR)
	$(CXX) -shared $(CXXFLAGS) $(CORE_OBJS) -o $@
	@echo "==> Biblioteca compartilhada (DLL/.so) gerada: $(SHARED_TARGET)"

test: dirs $(TEST_TARGET) $(TEST_GENERIC_TARGET)
	@echo "==> Executando bateria de testes AED1 (Tormenta20)..."
	@./$(TEST_TARGET)
	@echo "\n==> Executando bateria de testes do Core Genérico e C API (DLL)..."
	@./$(TEST_GENERIC_TARGET)

test_generic: dirs $(TEST_GENERIC_TARGET)
	@./$(TEST_GENERIC_TARGET)

$(TEST_TARGET): $(CORE_OBJS) $(TEST_DIR)/test_data_structures.cpp
	@mkdir -p $(BIN_DIR)
	$(CXX) $(CXXFLAGS) $(TEST_DIR)/test_data_structures.cpp $(CORE_OBJS) -o $@

$(TEST_GENERIC_TARGET): $(CORE_OBJS) $(TEST_DIR)/test_generic_core.cpp
	@mkdir -p $(BIN_DIR)
	$(CXX) $(CXXFLAGS) $(TEST_DIR)/test_generic_core.cpp $(CORE_OBJS) -o $@

run: all
	@./$(CLI_TARGET)

clean:
	@rm -rf $(BUILD_DIR) $(BIN_DIR)
	@echo "==> Diretórios de build limpos."

