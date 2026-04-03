# ?? Docker Setup - PostgreSQL para Oficina Mecânica

## ?? Pré-requisitos

- Docker Desktop instalado e rodando
- Docker Compose instalado (já vem com Docker Desktop)

## ?? Como usar

### 1. Subir o container PostgreSQL

```bash
docker-compose up -d
```

Este comando irá:
- ? Baixar a imagem do PostgreSQL 16 (Alpine - mais leve)
- ? Criar o container `oficina_mecanica_postgres`
- ? Criar o banco de dados `oficina_mecanica_dev`
- ? Expor a porta 5432 (PostgreSQL)
- ? Subir o PgAdmin na porta 8080 (opcional)

### 2. Verificar se está rodando

```bash
docker ps
```

Você deve ver:
- `oficina_mecanica_postgres` - PostgreSQL
- `oficina_mecanica_pgadmin` - PgAdmin (interface web)

### 3. Aplicar as migrations

Depois que o container estiver rodando, execute:

```bash
dotnet ef database update --project OficinaMecanica.Infrastructure --startup-project OficinaMecanica.API
```

## ?? Credenciais

### PostgreSQL
- **Host**: localhost
- **Port**: 5433 (evitando conflito com PostgreSQL local)
- **Database**: oficina_mecanica_dev
- **Username**: postgres
- **Password**: postgres

### PgAdmin (Interface Web)
- **URL**: http://localhost:8080
- **Email**: admin@oficina.com
- **Password**: admin

#### Conectar ao PostgreSQL via PgAdmin:
1. Acesse http://localhost:8080
2. Faça login com as credenciais acima
3. Clique em "Add New Server"
4. Na aba "General", dê um nome (ex: Oficina Local)
5. Na aba "Connection":
   - Host: postgres (nome do container)
   - Port: 5432
   - Database: oficina_mecanica_dev
   - Username: postgres
   - Password: postgres

## ?? Comandos úteis

### Parar os containers
```bash
docker-compose down
```

### Parar e REMOVER os dados (cuidado!)
```bash
docker-compose down -v
```

### Ver logs do PostgreSQL
```bash
docker-compose logs -f postgres
```

### Ver logs do PgAdmin
```bash
docker-compose logs -f pgadmin
```

### Acessar o terminal do PostgreSQL
```bash
docker exec -it oficina_mecanica_postgres psql -U postgres -d oficina_mecanica_dev
```

### Backup do banco
```bash
docker exec -t oficina_mecanica_postgres pg_dump -U postgres oficina_mecanica_dev > backup.sql
```

### Restaurar backup
```bash
docker exec -i oficina_mecanica_postgres psql -U postgres oficina_mecanica_dev < backup.sql
```

## ?? Troubleshooting

### Porta 5432 já está em uso
**IMPORTANTE:** Este projeto está configurado para usar a porta **5433** para evitar conflito com PostgreSQL local.

Se você já tem PostgreSQL instalado localmente na porta 5432, o Docker está configurado para usar a porta 5433.

Se ainda assim tiver problemas, você pode:

**Opção 1:** Parar o PostgreSQL local
```bash
# Windows (como admin)
net stop postgresql-x64-16
# ou
Stop-Service postgresql-x64-16
```

**Opção 2:** Mudar a porta do Docker
No arquivo `docker-compose.yml`, altere:
```yaml
ports:
  - "5434:5432"  # Use outra porta no host
```

E no `appsettings.Development.json`:
```json
"DefaultConnection": "Host=localhost;Port=5434;Database=oficina_mecanica_dev;Username=postgres;Password=postgres;..."
```

### Container não inicia
```bash
# Ver logs detalhados
docker-compose logs postgres

# Remover tudo e começar do zero
docker-compose down -v
docker-compose up -d
```

### Erro de autenticação
Se tiver erro de autenticação, recrie o container:
```bash
docker-compose down -v
docker-compose up -d
# Aguarde uns 10 segundos para o PostgreSQL inicializar
dotnet ef database update --project OficinaMecanica.Infrastructure --startup-project OficinaMecanica.API
```

## ?? Estrutura de volumes

Os dados são persistidos em volumes Docker:
- `postgres_data` - Dados do banco de dados
- `pgadmin_data` - Configurações do PgAdmin

Para ver onde estão fisicamente:
```bash
docker volume inspect oficina_mecanica_postgres_data
```

## ?? Workflow Completo

```bash
# 1. Subir o banco
docker-compose up -d

# 2. Aguardar estar pronto (opcional)
docker-compose logs -f postgres
# Aguarde ver: "database system is ready to accept connections"
# Pressione Ctrl+C para sair dos logs

# 3. Aplicar migrations
dotnet ef database update --project OficinaMecanica.Infrastructure --startup-project OficinaMecanica.API

# 4. Rodar a aplicação
dotnet run --project OficinaMecanica.API

# 5. Quando terminar
docker-compose down
```

## ?? Ambientes

### Desenvolvimento (padrão)
- Banco: `oficina_mecanica_dev`
- Configuração: `appsettings.Development.json`

### Produção
Para produção, você deve:
1. Usar variáveis de ambiente para connection string
2. NÃO usar docker-compose em produção (use serviços gerenciados)
3. Usar senhas fortes
4. Configurar SSL/TLS

## ? Próximos passos

Depois de tudo configurado:
1. ? Container rodando
2. ? Migrations aplicadas
3. ? Banco criado

Você pode:
- Rodar a API: `dotnet run --project OficinaMecanica.API`
- Acessar Swagger: http://localhost:5000/swagger (ou porta configurada)
- Gerenciar banco via PgAdmin: http://localhost:8080
