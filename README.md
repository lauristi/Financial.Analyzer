# Financial Analyzer - Intelligent Statement Processor

O **Financial Analyzer** é uma solução de engenharia de software desenvolvida em **.NET 8** e **Blazor** voltada para a gestão financeira pessoal e empresarial. O sistema automatiza a leitura de extratos bancários, aplica categorização inteligente via IA e exporta relatórios formatados em Excel.

## 🚀 Tecnologias Utilizadas

* **Framework:** .NET 8 (C#)
* **Front-end:** Blazor (Componentes Reutilizáveis e Code-behind)
* **Inteligência Artificial:** Google Gemini 1.5 Flash API
* **Processamento de Planilhas:** EPPlus (Excel)
* **Arquitetura:** Camadas de Serviço, Orquestração e Injeção de Dependência (DI)

---

## 🏗️ Evolução e Implementação Técnica

### 1. Modelagem de Dados e Categorização Híbrida
A base do projeto foi construída sobre uma lógica de categorização em dois níveis:
* **Categorização Determinística (Local):** Uso de heurísticas e regras de negócio baseadas em descrições conhecidas para garantir velocidade e consistência.
* **Categorização Probabilística (IA):** Integração com o modelo Gemini para interpretar descrições ambíguas, atribuindo categoria, score de relevância e justificativas textuais.

### 2. Engenharia de Prompts e Respostas Estruturadas
Para garantir a integridade da integração com a IA, aplicamos:
* **System Prompting:** Definição de persona de analista financeiro para o modelo.
* **Strict JSON Output:** Uso de esquemas JSON para garantir que a resposta da IA fosse mapeada diretamente para os objetos `SpendingData` sem erros de parsing.

### 3. Resiliência e Graceful Degradation (SOLID)
Aplicamos princípios de resiliência para garantir que o software nunca deixe o usuário sem uma resposta:
* **Pattern Orchestrator:** Implementação do método `ExecuteOrchestrationAsync` para coordenar o fluxo entre Parser, IA e Excel.
* **Fallback Automático:** Uso de `WaitAsync` e `CancellationToken` (15s). Caso a IA apresente latência ou falha, o sistema degrada graciosamente, gerando o relatório apenas com as regras locais e auditando a falha no campo de origem.

### 4. Geração de Relatórios com UX (Excel)
A exportação via EPPlus foi refinada para máxima legibilidade:
* **AutoFit & Text Wrap:** Ajuste dinâmico de colunas e quebra automática de texto para justificativas longas da IA.
* **Formatação Condicional:** Aplicação de cores dinâmicas baseadas no tipo de transação (Crédito/Débito) e nível de score.

### 5. Interface de Usuário e Feedback (UX/UI)
No front-end Blazor, priorizamos a transparência do estado do sistema:
* **Bloqueio de Interação:** Prevenção de submissões duplicadas (*Double-click*) através de estados de carregamento (`IsLoading`).
* **Mensageria Centralizada:** Integração com um `AlertService` baseado no padrão **Observer**, permitindo que mensagens de progresso, sucesso ou erro sejam exibidas em tempo real para o usuário.

---

## 🛠️ Princípios de Desenvolvimento Aplicados

* **S (Single Responsibility Principle):** Cada serviço possui uma responsabilidade única e bem definida.
* **D (Dependency Inversion):** Uso extensivo de interfaces e injeção de dependência para facilitar testes e manutenibilidade.
* **Clean Code:** Métodos privados auxiliares para tratamento de erros e formatação, mantendo o código principal legível e conciso.
* **Asynchronous Programming (TAP):** Uso de tarefas assíncronas em toda a cadeia de chamadas para garantir a escalabilidade do servidor.

---

*Desenvolvido em 2026 como parte de uma solução robusta para análise de dados financeiros.*
