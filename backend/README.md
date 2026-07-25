# HabitApp API

API RESTful para gerenciamento de hábitos, desenvolvida com ASP.NET Core 10 e arquitetura em camadas (Clean Architecture).

---

## 🚀 Tecnologias

- **.NET 10**
- **ASP.NET Core Web API**
- **Entity Framework Core 10** com **SQLite**
- **AutoMapper**
- **Swagger / OpenAPI**

---

## 🏗️ Arquitetura

O projeto é organizado em camadas seguindo os princípios de Clean Architecture:

```
HabitApp-API/
├── HabitApp.API                    # Camada de apresentação (Controllers, configuração)
├── HabitApp.Application            # Camada de aplicação (Services, DTOs, Mappers)
├── HabitApp.Domain                 # Camada de domínio (Entidades)
├── HabitApp.Domain.Services        # Serviços de domínio
├── HabitApp.Infrastructure.Data    # Camada de infraestrutura (Repositórios, DbContext)
└── HabitApp.Infrastructure.IOC     # Injeção de dependência
```

### Diagrama de dependências

```
API → Application → Domain
API → Infrastructure.IOC → Infrastructure.Data → Domain
```

---

## 📦 Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

---

## ⚙️ Configuração

1. Clone o repositório:
   ```bash
   git clone https://github.com/seu-usuario/HabitApp-API.git
   cd HabitApp-API
   ```

2. Configure a string de conexão em `HabitApp.API/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "SqliteConnection": "Data Source=habitapp.db"
     }
   }
   ```

3. Execute o projeto:
   ```bash
   dotnet run --project HabitApp.API
   ```

O banco de dados SQLite é criado automaticamente na primeira execução via `EnsureCreated()`.

---

## 🌐 Endpoints

A API estará disponível em `https://localhost:7000`. A documentação interativa via Swagger pode ser acessada em:

```
https://localhost:7000/swagger
```

### Hábitos (`/api/habit`)

| Método | Rota              | Descrição                  |
|--------|-------------------|----------------------------|
| GET    | `/api/habit`      | Lista todos os hábitos     |
| GET    | `/api/habit/{id}` | Busca um hábito por ID     |
| POST   | `/api/habit`      | Cria um novo hábito        |
| PUT    | `/api/habit/{id}` | Atualiza um hábito         |
| DELETE | `/api/habit/{id}` | Remove um hábito           |

### Usuários (`/api/user`)

| Método | Rota               | Descrição                       |
|--------|--------------------|---------------------------------|
| GET    | `/api/user`        | Lista todos os usuários         |
| GET    | `/api/user/{id}`   | Busca um usuário por ID         |
| POST   | `/api/user`        | Cria um novo usuário            |
| PUT    | `/api/user/{id}`   | Atualiza um usuário             |
| DELETE | `/api/user/{id}`   | Remove um usuário               |
| POST   | `/api/user/login`  | Autentica um usuário (login)    |

---

## 📋 Modelos

### Habit (Hábito)

| Campo            | Tipo      | Descrição                                        |
|------------------|-----------|--------------------------------------------------|
| `id`             | int       | Identificador único                              |
| `title`          | string    | Título do hábito                                 |
| `icon`           | string    | Ícone representativo                             |
| `streak`         | int       | Sequência de dias consecutivos                   |
| `completedDays`  | bool[7]   | Array com os dias da semana concluídos           |
| `userId`         | int       | ID do usuário dono do hábito                     |

### User (Usuário)

| Campo      | Tipo   | Descrição           |
|------------|--------|---------------------|
| `id`       | int    | Identificador único |
| `name`     | string | Nome                |
| `email`    | string | E-mail (único)      |
| `password` | string | Senha               |

### Login Request

```json
{
  "email": "usuario@email.com",
  "password": "senha123"
}
```

### Login Response

```json
{
  "id": 1,
  "name": "João Silva",
  "email": "usuario@email.com"
}
```

---

## 🔧 Funcionalidades

- **CRUD completo** para Usuários e Hábitos
- **Autenticação simples** por e-mail e senha
- **Soft delete** — registros deletados não são removidos fisicamente do banco
- **Timestamps automáticos** (`createdAt`, `modifiedAt`) com fuso horário de Brasília
- **CORS** configurado para `http://localhost:4200` (Angular)
- **Cascade delete** — ao remover um usuário, seus hábitos são removidos automaticamente

---

## 🔌 CORS

A API está configurada para aceitar requisições do frontend Angular rodando em `http://localhost:4200`. Para alterar a origem permitida, edite `Program.cs`:

```csharp
policy.WithOrigins("http://localhost:4200")
```

---

## 📁 Estrutura de camadas em detalhe

**`HabitApp.Domain`** — entidades puras sem dependências externas (`User`, `Habit`, `BaseEntity`).

**`HabitApp.Domain.Services`** — regras de negócio (`UserService`, `HabitService`) com interfaces para desacoplamento.

**`HabitApp.Application`** — orquestra os serviços de domínio, expõe DTOs e realiza o mapeamento via AutoMapper.

**`HabitApp.Infrastructure.Data`** — implementa os repositórios com EF Core e o `SqliteContext`.

**`HabitApp.Infrastructure.IOC`** — centraliza o registro de dependências (DI container).

**`HabitApp.API`** — controllers HTTP que delegam para a camada de aplicação.
