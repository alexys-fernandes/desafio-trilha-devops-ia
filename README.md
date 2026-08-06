# Desafio Trilha DevOps IA

[![CI](https://github.com/alexys-fernandes/desafio-trilha-devops-ia/actions/workflows/ci.yml/badge.svg)](https://github.com/alexys-fernandes/desafio-trilha-devops-ia/actions/workflows/ci.yml)

Este repositório reúne o backend em .NET e o frontend em Angular para o projeto HabitApp, uma aplicação para acompanhamento de hábitos, recorrências, notificações e metas de desenvolvimento pessoal.

## Visão geral

O projeto foi organizado em duas partes principais:

- Backend: API RESTful construída com ASP.NET Core, seguindo uma estrutura em camadas.
- Frontend: aplicação web em Angular para consumir a API e oferecer uma interface interativa.

## Tecnologias

### Backend

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core / SQLite
- Swagger / OpenAPI

### Frontend

- Angular 21
- TypeScript
- RxJS
- Bootstrap
- Angular Material

## Estrutura do repositório

```text
backend/          # API e regras de negócio
frontend/         # aplicação Angular
.github/workflows/ # pipeline de CI/CD
```

## Pré-requisitos

Antes de rodar o projeto localmente, certifique-se de ter instalado:

- .NET SDK 10
- Node.js 22.x ou superior
- npm 10.x ou superior

## Como rodar localmente

### 1. Backend

Acesse a pasta do backend e restaure as dependências:

```bash
cd backend
 dotnet restore
```

Em seguida, execute a API:

```bash
 dotnet run --project HabitApp.API/HabitApp.API.csproj
```

A API ficará disponível em:

- http://localhost:5000
- https://localhost:7000

A documentação Swagger pode ser aberta em:

- http://localhost:5000/swagger
- https://localhost:7000/swagger

### 2. Frontend

Acesse a pasta do frontend e instale as dependências:

```bash
cd frontend
npm install
```

Depois, inicie a aplicação:

```bash
npm start
```

A aplicação ficará disponível em:

- http://localhost:4200

## Configuração de ambiente (.env.example)

No backend existe um arquivo de exemplo em [backend/.env.example](backend/.env.example) com as variáveis de configuração usadas pelo coach de IA. Como esse arquivo não é enviado para o repositório, você deve criar seu próprio arquivo local `.env` a partir dele.

### Variáveis disponíveis

- `AI_PROVIDER`: provedor da IA a ser utilizado, como `gemini`.
- `AI_ENABLED`: habilita ou desabilita o uso do coach de IA.
- `GEMINI_API_KEY`: chave de APIF do Google Gemini.
- `GEMINI_MODEL`: modelo da IA a ser usado.
- `GEMINI_API_BASE_URL`: URL base da API do Gemini.
- `AI_COACH_SYSTEM_PROMPT`: instruções do sistema para o comportamento do assistente.

### Como usar

1. Copie o arquivo para um novo arquivo `.env` dentro da pasta `backend`.
2. Ajuste os valores conforme o ambiente local ou de desenvolvimento.
3. Reinicie a API para que as variáveis sejam carregadas.

```bash
cp backend/.env.example backend/.env
```

## Testes

### Backend

```bash
cd backend
dotnet test ./HabitApp.Domain.Services.Tests/HabitApp.Domain.Services.Tests.csproj --collect:"XPlat Code Coverage" --results-directory ./TestResults -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura
```

### Frontend

```bash
cd frontend
npm run test:ci
```

## Visualizando os resultados dos testes

Os arquivos gerados pelos testes ficam organizados em pastas específicas para facilitar a análise.

### Resultados locais

- Backend: os relatórios de cobertura do .NET são salvos em [backend/HabitApp.TestResults](backend/HabitApp.TestResults), com uma subpasta por execução no formato `AAAA-MM-DD_HH-MM-SS`.
- Frontend: a cobertura do Angular fica em [frontend/coverage](frontend/coverage).

### Como abrir os arquivos

1. Entre na pasta do backend ou frontend conforme o tipo de resultado desejado.
2. Abra a subpasta com a data/hora da execução.
3. Veja o arquivo XML de cobertura do backend, por exemplo [backend/HabitApp.TestResults](backend/HabitApp.TestResults), ou abra o relatório HTML do backend em [backend/HabitApp.TestResults](backend/HabitApp.TestResults) dentro da pasta `html` da execução mais recente. Para o frontend, abra o relatório HTML em [frontend/coverage](frontend/coverage).

### Na pipeline do GitHub Actions

A cada execução da pipeline, os artefatos são publicados no GitHub Actions para download. Para visualizar:

1. Acesse a aba Actions do repositório.
2. Abra a execução desejada.
3. No final da página, baixe o artefato `backend-coverage` ou `frontend-coverage`.
4. Extraia o arquivo e abra o relatório HTML ou o XML correspondente.

## CI/CD

O projeto conta com uma pipeline no GitHub Actions responsável por:

- executar testes do backend;
- executar testes do frontend;
- gerar relatórios de cobertura;
- publicar artefatos de cobertura para análise posterior.
