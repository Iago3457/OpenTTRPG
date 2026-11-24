CXX = g++
CXXFLAGS = -std=c++17 -Wall -Wextra -O2 -fPIC -MMD -MP -Iinclude
ASAN_FLAGS = -std=c++17 -Wall -Wextra -O1 -g -fsanitize=address,leak -fno-omit-frame-pointer -Iinclude

SRC_DIR = src
INC_DIR = include
BUILD_DIR = build
BIN_DIR = bin
TEST_DIR = tests
CSHARP_PROJ = bindings/csharp/OpenTTRPG.CLI.csproj

# A biblioteca compartilhada (DLL / .so) compila EXCLUSIVAMENTE o Core genérico e a C API
CORE_SRCS = $(wildcard $(SRC_DIR)/core/*.cpp) $(wildcard $(SRC_DIR)/api/*.cpp)
CORE_OBJS = $(patsubst $(SRC_DIR)/%.cpp, $(BUILD_DIR)/%.o, $(CORE_SRCS))

DEPS = $(CORE_OBJS:.o=.d)

TEST_TARGET = $(BIN_DIR)/test_data_structures
TEST_GENERIC_TARGET = $(BIN_DIR)/test_generic_core
TEST_ASAN_TARGET = $(BIN_DIR)/test_asan
SHARED_TARGET = $(BIN_DIR)/libopenttrpg_core.so

.PHONY: all clean run run-csharp run-ui release release-windows release-all test test_generic test_asan test_csharp lib dirs

all: dirs $(SHARED_TARGET)

dirs:
	@mkdir -p $(BUILD_DIR) $(BIN_DIR)

$(BUILD_DIR)/%.o: $(SRC_DIR)/%.cpp
	@mkdir -p $(dir $@)
	$(CXX) $(CXXFLAGS) -c $< -o $@

lib: $(SHARED_TARGET)

$(SHARED_TARGET): $(CORE_OBJS)
	@mkdir -p $(BIN_DIR)
	$(CXX) -shared $(CXXFLAGS) $(CORE_OBJS) -o $@
	@echo "==> Biblioteca compartilhada agnóstica (DLL/.so) atualizada: $(SHARED_TARGET)"

test: dirs $(TEST_TARGET) $(TEST_GENERIC_TARGET) $(SHARED_TARGET)
	@echo "==> Executando bateria de testes AED1 (Estruturas de Dados Manuais)..."
	@./$(TEST_TARGET)
	@echo "\n==> Executando bateria de testes do Core Genérico e C API (DLL)..."
	@./$(TEST_GENERIC_TARGET)
	@if command -v dotnet >/dev/null 2>&1; then \
		echo "\n==> Executando bateria de testes automatizados em C# (Tormenta20 + P/Invoke)..."; \
		[ -w "$$HOME" ] || export HOME=/tmp; \
		[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
		LD_LIBRARY_PATH="$(abspath $(BIN_DIR)):$$LD_LIBRARY_PATH" dotnet run --project $(CSHARP_PROJ) -- --test; \
	fi

test_generic: dirs $(TEST_GENERIC_TARGET)
	@./$(TEST_GENERIC_TARGET)

test_asan: dirs
	@echo "==> Compilando e auditando memória com AddressSanitizer (-fsanitize=address,leak)..."
	$(CXX) $(ASAN_FLAGS) $(TEST_DIR)/test_data_structures.cpp $(CORE_SRCS) -o $(BIN_DIR)/test_ds_asan
	@./$(BIN_DIR)/test_ds_asan
	$(CXX) $(ASAN_FLAGS) $(TEST_DIR)/test_generic_core.cpp $(CORE_SRCS) -o $(TEST_ASAN_TARGET)
	@./$(TEST_ASAN_TARGET)
	@echo "==> Auditoria AddressSanitizer concluída: 0 memory leaks!"

test_csharp: $(SHARED_TARGET)
	@[ -w "$$HOME" ] || export HOME=/tmp; \
	[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
	LD_LIBRARY_PATH="$(abspath $(BIN_DIR)):$$LD_LIBRARY_PATH" dotnet run --project $(CSHARP_PROJ) -- --test

$(TEST_TARGET): $(CORE_OBJS) $(TEST_DIR)/test_data_structures.cpp
	@mkdir -p $(BIN_DIR)
	$(CXX) $(CXXFLAGS) $(TEST_DIR)/test_data_structures.cpp $(CORE_OBJS) -o $@

$(TEST_GENERIC_TARGET): $(CORE_OBJS) $(TEST_DIR)/test_generic_core.cpp
	@mkdir -p $(BIN_DIR)
	$(CXX) $(CXXFLAGS) $(TEST_DIR)/test_generic_core.cpp $(CORE_OBJS) -o $@

run: $(SHARED_TARGET)
	@echo "==> Executando cliente oficial via C# (.NET + $(SHARED_TARGET))..."
	@[ -w "$$HOME" ] || export HOME=/tmp; \
	[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
	LD_LIBRARY_PATH="$(abspath $(BIN_DIR)):$$LD_LIBRARY_PATH" dotnet run --project $(CSHARP_PROJ)

run-csharp: run

run-ui: $(SHARED_TARGET)
	@cp -f $(SHARED_TARGET) godot_ui/open-ttrpg/libopenttrpg_core.so
	@echo "==> Iniciando Interface Gráfica Godot 4 (.NET + $(SHARED_TARGET))..."
	@if command -v godot-mono >/dev/null 2>&1; then \
		godot-mono --path godot_ui/open-ttrpg; \
	elif command -v godot >/dev/null 2>&1; then \
		godot --path godot_ui/open-ttrpg; \
	else \
		echo "Erro: 'godot-mono' ou 'godot' não encontrado no PATH."; exit 1; \
	fi

release: $(SHARED_TARGET)
	@mkdir -p $(BIN_DIR)/release/linux-x64/saves $(BIN_DIR)/release/linux-x64/data_OpenTTRPG_linuxbsd_x86_64
	@cp -f $(SHARED_TARGET) godot_ui/open-ttrpg/libopenttrpg_core.so
	@cp -f $(SHARED_TARGET) $(BIN_DIR)/release/linux-x64/libopenttrpg_core.so
	@cp -f $(SHARED_TARGET) $(BIN_DIR)/release/linux-x64/data_OpenTTRPG_linuxbsd_x86_64/libopenttrpg_core.so
	@if [ -f saves/characters.json ]; then cp -f saves/characters.json $(BIN_DIR)/release/linux-x64/saves/characters.json; fi
	@if [ -f saves/enemies.json ]; then cp -f saves/enemies.json $(BIN_DIR)/release/linux-x64/saves/enemies.json; fi
	@set -e; \
	ORIG_HOME="$$HOME"; \
	[ -w "$$HOME" ] || export HOME=/tmp; \
	[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
	if [ "$$HOME" = "/tmp" ] && [ -d "$$ORIG_HOME/.nuget/packages" ]; then \
		mkdir -p /tmp/nuget_cache /tmp/.nuget/NuGet; \
		cp -rn "$$ORIG_HOME/.nuget/packages/"* /tmp/nuget_cache/ 2>/dev/null || true; \
		printf '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /><add key="local" value="/tmp/nuget_cache" /></packageSources></configuration>\n' > /tmp/.nuget/NuGet/NuGet.Config; \
		export NUGET_PACKAGES=/tmp/nuget_cache; \
	fi; \
	dotnet publish $(CSHARP_PROJ) -c Release -o $(BIN_DIR)/release/linux-x64 --no-self-contained; \
	echo "==> Compilando scripts C# da Interface Gráfica (OpenTTRPG.csproj)..."; \
	dotnet build godot_ui/open-ttrpg/OpenTTRPG.csproj -c ExportRelease; \
	GODOT_BIN=""; \
	if command -v godot-mono >/dev/null 2>&1; then GODOT_BIN="godot-mono"; \
	elif command -v godot >/dev/null 2>&1; then GODOT_BIN="godot"; fi; \
	if [ -z "$$GODOT_BIN" ]; then \
		echo "Erro: 'godot-mono' ou 'godot' (Godot 4 .NET) não encontrado no PATH."; \
		exit 1; \
	fi; \
	rm -f $(BIN_DIR)/release/linux-x64/OpenTTRPG.pck; \
	echo "==> Exportando release Godot Linux (x86_64)..."; \
	if $$GODOT_BIN --headless --path godot_ui/open-ttrpg --export-release "Linux" ../../$(BIN_DIR)/release/linux-x64/OpenTTRPG.x86_64 && [ -f $(BIN_DIR)/release/linux-x64/OpenTTRPG.x86_64 ] && [ -f $(BIN_DIR)/release/linux-x64/OpenTTRPG.pck ]; then \
		echo "==> Executável Linux e pacote (.pck) exportados via --export-release."; \
	else \
		echo "==> Aviso: --export-release Linux indisponível (verifique se os Export Templates do Godot .NET estão instalados). Usando --export-pack..."; \
		$$GODOT_BIN --headless --path godot_ui/open-ttrpg --export-pack "Linux" ../../$(BIN_DIR)/release/linux-x64/OpenTTRPG.pck; \
		if [ ! -f $(BIN_DIR)/release/linux-x64/OpenTTRPG.x86_64 ]; then \
			echo "================================================================================"; \
			echo "  AVISO: $(BIN_DIR)/release/linux-x64/OpenTTRPG.x86_64 não foi gerado porque"; \
			echo "  os Export Templates do Godot 4 (.NET) não foram encontrados neste PC."; \
			echo "  Para gerar o executável standalone:"; \
			echo "    1. Abra o Godot 4 (.NET) -> Editor -> Gerenciar Modelos de Exportação -> Baixar e Instalar"; \
			echo "    2. Execute 'make release-all' novamente."; \
			echo "  Enquanto isso, você pode abrir a UI com './$(BIN_DIR)/release/linux-x64/OpenTTRPG.sh' ou 'make run-ui'."; \
			echo "================================================================================"; \
		fi; \
	fi; \
	if [ -d godot_ui/open-ttrpg/.godot/mono/temp/bin/ExportRelease/linux-x64 ]; then \
		cp -rf godot_ui/open-ttrpg/.godot/mono/temp/bin/ExportRelease/linux-x64/* $(BIN_DIR)/release/linux-x64/data_OpenTTRPG_linuxbsd_x86_64/; \
	fi; \
	cp -f $(SHARED_TARGET) $(BIN_DIR)/release/linux-x64/data_OpenTTRPG_linuxbsd_x86_64/libopenttrpg_core.so
	@printf '%s\n' \
		'#!/usr/bin/env bash' \
		'set -e' \
		'SCRIPT_DIR="$$(cd "$$(dirname "$${BASH_SOURCE[0]}")" && pwd)"' \
		'ROOT_DIR="$$(cd "$$SCRIPT_DIR/../../.." && pwd)"' \
		'cp -f "$$ROOT_DIR/bin/libopenttrpg_core.so" "$$ROOT_DIR/godot_ui/open-ttrpg/libopenttrpg_core.so" 2>/dev/null || true' \
		'if command -v godot-mono >/dev/null 2>&1; then' \
		'    exec godot-mono --path "$$ROOT_DIR/godot_ui/open-ttrpg" "$$@"' \
		'elif command -v godot >/dev/null 2>&1; then' \
		'    exec godot --path "$$ROOT_DIR/godot_ui/open-ttrpg" "$$@"' \
		'else' \
		'    echo "Erro: Executável godot-mono ou godot não encontrado no PATH."' \
		'    exit 1' \
		'fi' > $(BIN_DIR)/release/linux-x64/OpenTTRPG_GodotUI.sh
	@printf '%s\n' \
		'#!/bin/sh' \
		'base_path="$$(dirname "$$(realpath "$$0")")"' \
		'if [ -x "$$base_path/OpenTTRPG.x86_64" ]; then' \
		'    printf "\033c\033]0;%s\a" OpenTTRPG' \
		'    exec "$$base_path/OpenTTRPG.x86_64" "$$@"' \
		'else' \
		'    exec "$$base_path/OpenTTRPG_GodotUI.sh" "$$@"' \
		'fi' > $(BIN_DIR)/release/linux-x64/OpenTTRPG.sh
	@chmod +x $(BIN_DIR)/release/linux-x64/OpenTTRPG.sh $(BIN_DIR)/release/linux-x64/OpenTTRPG_GodotUI.sh
	@if [ -f $(BIN_DIR)/release/linux-x64/OpenTTRPG.x86_64 ]; then chmod +x $(BIN_DIR)/release/linux-x64/OpenTTRPG.x86_64; fi
	@echo "==> Build Linux completa gerada em $(BIN_DIR)/release/linux-x64/"

release-windows:
	@mkdir -p $(BIN_DIR)/release/win-x64/saves $(BIN_DIR)/release/win-x64/data_OpenTTRPG_windows_x86_64
	@if command -v x86_64-w64-mingw32-g++ >/dev/null 2>&1; then \
		echo "==> Cross-compilando openttrpg_core.dll para Windows (x86_64) com linkagem estática do runtime MinGW..."; \
		x86_64-w64-mingw32-g++ -std=c++17 -O2 -shared -Iinclude $(CORE_SRCS) \
			-o $(BIN_DIR)/release/win-x64/openttrpg_core.dll -static -static-libgcc -static-libstdc++; \
		cp -f $(BIN_DIR)/release/win-x64/openttrpg_core.dll godot_ui/open-ttrpg/openttrpg_core.dll; \
		cp -f $(BIN_DIR)/release/win-x64/openttrpg_core.dll $(BIN_DIR)/release/win-x64/data_OpenTTRPG_windows_x86_64/openttrpg_core.dll; \
		echo "==> DLL Windows gerada: $(BIN_DIR)/release/win-x64/openttrpg_core.dll"; \
	else \
		echo "Aviso: 'x86_64-w64-mingw32-g++' (mingw-w64-gcc) não instalado. No Windows execute 'build_windows.bat' ou no Linux instale 'mingw-w64-gcc'."; \
	fi
	@if [ -f saves/characters.json ]; then cp -f saves/characters.json $(BIN_DIR)/release/win-x64/saves/characters.json; fi
	@if [ -f saves/enemies.json ]; then cp -f saves/enemies.json $(BIN_DIR)/release/win-x64/saves/enemies.json; fi
	@set -e; \
	ORIG_HOME="$$HOME"; \
	[ -w "$$HOME" ] || export HOME=/tmp; \
	[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
	if [ "$$HOME" = "/tmp" ] && [ -d "$$ORIG_HOME/.nuget/packages" ]; then \
		mkdir -p /tmp/nuget_cache /tmp/.nuget/NuGet; \
		cp -rn "$$ORIG_HOME/.nuget/packages/"* /tmp/nuget_cache/ 2>/dev/null || true; \
		printf '<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear /><add key="local" value="/tmp/nuget_cache" /></packageSources></configuration>\n' > /tmp/.nuget/NuGet/NuGet.Config; \
		export NUGET_PACKAGES=/tmp/nuget_cache; \
	fi; \
	echo "==> Compilando scripts C# da Interface Gráfica (OpenTTRPG.csproj)..."; \
	dotnet build godot_ui/open-ttrpg/OpenTTRPG.csproj -c ExportRelease; \
	GODOT_BIN=""; \
	if command -v godot-mono >/dev/null 2>&1; then GODOT_BIN="godot-mono"; \
	elif command -v godot >/dev/null 2>&1; then GODOT_BIN="godot"; fi; \
	if [ -z "$$GODOT_BIN" ]; then \
		echo "Erro: 'godot-mono' ou 'godot' (Godot 4 .NET) não encontrado no PATH."; \
		exit 1; \
	fi; \
	rm -f $(BIN_DIR)/release/win-x64/OpenTTRPG.pck; \
	echo "==> Exportando release Godot Windows (x86_64)..."; \
	if $$GODOT_BIN --headless --path godot_ui/open-ttrpg --export-release "Windows Desktop" ../../$(BIN_DIR)/release/win-x64/OpenTTRPG.exe && [ -f $(BIN_DIR)/release/win-x64/OpenTTRPG.exe ] && [ -f $(BIN_DIR)/release/win-x64/OpenTTRPG.pck ]; then \
		echo "==> Executável Windows (.exe) e pacote (.pck) exportados via --export-release."; \
	else \
		echo "==> Aviso: --export-release Windows Desktop indisponível (verifique se os Export Templates do Godot .NET estão instalados). Usando --export-pack..."; \
		$$GODOT_BIN --headless --path godot_ui/open-ttrpg --export-pack "Windows Desktop" ../../$(BIN_DIR)/release/win-x64/OpenTTRPG.pck; \
		if [ ! -f $(BIN_DIR)/release/win-x64/OpenTTRPG.exe ]; then \
			echo "================================================================================"; \
			echo "  AVISO: $(BIN_DIR)/release/win-x64/OpenTTRPG.exe não foi gerado porque"; \
			echo "  os Export Templates do Godot 4 (.NET) não foram encontrados neste PC."; \
			echo "================================================================================"; \
		fi; \
	fi; \
	if [ -d godot_ui/open-ttrpg/.godot/mono/temp/bin/ExportRelease/win-x64 ]; then \
		cp -rf godot_ui/open-ttrpg/.godot/mono/temp/bin/ExportRelease/win-x64/* $(BIN_DIR)/release/win-x64/data_OpenTTRPG_windows_x86_64/; \
	fi; \
	if [ -f $(BIN_DIR)/release/win-x64/openttrpg_core.dll ]; then \
		cp -f $(BIN_DIR)/release/win-x64/openttrpg_core.dll $(BIN_DIR)/release/win-x64/data_OpenTTRPG_windows_x86_64/openttrpg_core.dll; \
	fi
	@echo "==> Build Windows completa gerada em $(BIN_DIR)/release/win-x64/"

release-all: release release-windows
	@echo "==> Executando auto-teste da Release Linux (Godot UI <-> C# <-> C++ Core)..."
	@[ -w "$$HOME" ] || export HOME=/tmp; \
	[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
	if [ -x ./$(BIN_DIR)/release/linux-x64/OpenTTRPG.x86_64 ]; then \
		timeout 30s ./$(BIN_DIR)/release/linux-x64/OpenTTRPG.x86_64 --headless -- --self-test; \
	elif command -v godot-mono >/dev/null 2>&1; then \
		timeout 30s godot-mono --headless --path godot_ui/open-ttrpg -- --self-test; \
	elif command -v godot >/dev/null 2>&1; then \
		timeout 30s godot --headless --path godot_ui/open-ttrpg -- --self-test; \
	else \
		echo "Erro: Nem OpenTTRPG.x86_64 nem godot-mono/godot encontrados para rodar --self-test."; \
		exit 1; \
	fi

clean:
	@rm -rf $(BUILD_DIR) $(BIN_DIR)
	@echo "==> Diretórios de build limpos."

-include $(DEPS)
