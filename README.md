# 🚀 RTI.TradingPlatform

Uma plataforma de simulação de ordens de trading utilizando o protocolo **FIX 4.4** com **QuickFIX/N**.

O projeto é composto por duas aplicações principais:

- **RTI.OrderGenerator** → Interface web para criação e envio de ordens (`NewOrderSingle`)
- **RTI.OrderAccumulator** → Motor que recebe, valida e acumula exposição financeira por símbolo

---

## 🎯 Sobre o Projeto

Este sistema simula um fluxo real de trading onde:

- O **OrderGenerator** envia ordens via FIX para o **OrderAccumulator**.
- O acumulador calcula a **exposição financeira** por ativo em tempo real.
- Existe um limite de **R$ 100.000.000** (cem milhões) por símbolo.
- Ordens que ultrapassarem o limite são rejeitadas automaticamente com `ExecutionReport` de rejeição.

---

## 🏗️ Arquitetura

| Projeto                | Função                                          | Tecnologia                  |
|------------------------|-------------------------------------------------|-----------------------------|
| **RTI.Shared**         | Modelos, Enums, DTOs e constantes compartilhadas | .NET 8                      |
| **RTI.OrderGenerator** | Frontend + Cliente FIX                          | ASP.NET Core + HTML/JS      |
| **RTI.OrderAccumulator** | Servidor FIX + Lógica de risco/exposição      | ASP.NET Core + QuickFIX/N   |

---

## ✨ Funcionalidades

### OrderGenerator
- Interface web simples e intuitiva
- Formulário para envio de `NewOrderSingle`
- Validações no frontend e backend
- Visualização em tempo real das respostas (`ExecutionReport`)
- Suporte aos ativos: **PETR4**, **VALE3**, **VIIA4**

### OrderAccumulator
- Recebimento e processamento de ordens via FIX 4.4
- Cálculo de exposição financeira por símbolo:
  - `Exposição = Σ (Preço × Quantidade) compras - Σ (Preço × Quantidade) vendas`
- Controle rigoroso de limite por ativo (R$ 100 milhões)
- Rejeição automática de ordens que violarem o limite

---

## 🚀 Como Executar

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 ou superior (recomendado)

### Ordem de Inicialização (Importante!)

1. **Primeiro** inicie o **OrderAccumulator** (servidor FIX)
2. **Depois** inicie o **OrderGenerator** (cliente FIX)


### Terminal 1 - OrderAccumulator (Servidor)
cd RTI.OrderAccumulator
dotnet run

### Terminal 2 - OrderGenerator (Cliente)
cd RTI.OrderGenerator
dotnet run

## Tecnologias Utilizadas

* .NET 10
* QuickFIX/N (FIX 4.4)
* ASP.NET Core
* C#
* HTML + JavaScript (frontend)


## Próximos Passos

* Persistência de exposição (SQLite / EF Core)
* Dashboard de exposição em tempo real
* Logging avançado e métricas
* Testes unitários e de integração
* Dockerização das aplicações