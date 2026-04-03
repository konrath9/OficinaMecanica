# ?? Guia Completo - Conexão DBeaver com PostgreSQL (Docker + WSL2)

## ? Testes Realizados

Executamos `test-connection.ps1` e confirmamos:
- ? Container rodando e saudável
- ? Porta 5433 acessível
- ? PostgreSQL respondendo
- ? Conexão funcionando perfeitamente

## ?? Configuração no DBeaver

### Opção 1: Conexão via localhost (RECOMENDADO)

1. **Abrir DBeaver**
2. **Nova Conexão** ? PostgreSQL
3. **Preencher campos:**

```
???????????????????????????????????????????
? Main                                    ?
???????????????????????????????????????????
? Host:          localhost                ?
? Port:          5433          ?? IMPORTANTE
? Database:      oficina_mecanica_dev     ?
? Username:      postgres                 ?
? Password:      postgres                 ?
? ? Save password                         ?
? ? Show all databases                    ?
???????????????????????????????????????????
```

4. **Clicar em "Test Connection"**
5. **Se pedir drivers:** Clicar em "Download"

### Opção 2: Conexão via IP (se localhost falhar)

Use `127.0.0.1` em vez de `localhost`:

```
Host:          127.0.0.1
Port:          5433
Database:      oficina_mecanica_dev
Username:      postgres
Password:      postgres
```

### Opção 3: Conexão via IP do WSL (última alternativa)

Se nenhuma das anteriores funcionar:

1. **Descobrir IP do WSL:**
```powershell
wsl hostname -I
```

2. **Usar esse IP no DBeaver:**
```
Host:          172.x.x.x    # IP retornado pelo comando acima
Port:          5433
Database:      oficina_mecanica_dev
Username:      postgres
Password:      postgres
```

## ?? Problemas Comuns e Soluções

### ? Erro: "Connection refused" ou "Can't connect to server"

**Causa:** Usando porta errada ou container não está rodando

**Solução:**
```powershell
# Verificar se container está rodando
docker ps | findstr postgres

# Se não estiver, subir
docker-compose up -d

# Executar teste
.\test-connection.ps1
```

### ? Erro: "FATAL: password authentication failed"

**Causa:** Credenciais incorretas ou conectando ao PostgreSQL local (porta 5432)

**Solução:**
- ? Verifique se a porta é **5433** (não 5432!)
- ? Username: `postgres`
- ? Password: `postgres`

### ? Erro: "Driver class not found"

**Causa:** Driver PostgreSQL não instalado no DBeaver

**Solução:**
1. Quando aparecer a mensagem, clique em **"Download"**
2. Aguarde download dos drivers
3. Tente novamente

### ? DBeaver conecta mas não vê tabelas

**Causa:** Pode estar conectando no banco errado

**Solução:**
1. No DBeaver, expanda a conexão
2. Verifique se está no database: `oficina_mecanica_dev`
3. Navegue: `oficina_mecanica_dev` ? `Schemas` ? `public` ? `Tables`

## ?? Comandos Úteis para Debug

### Verificar se container está rodando
```powershell
docker ps
```

### Ver logs do PostgreSQL
```powershell
docker logs oficina_mecanica_postgres
```

### Testar conexão manualmente
```powershell
.\test-connection.ps1
```

### Conectar via linha de comando
```powershell
docker exec -it oficina_mecanica_postgres psql -U postgres -d oficina_mecanica_dev
```

Dentro do psql:
```sql
-- Listar tabelas
\dt

-- Ver estrutura de uma tabela
\d work_orders

-- Executar query
SELECT * FROM work_orders;

-- Sair
\q
```

## ?? Alternativas ao DBeaver

Se continuar com problemas no DBeaver, você pode usar:

### 1. **PgAdmin (Web - já instalado!)**
```
URL:      http://localhost:8080
Email:    admin@oficina.com
Password: admin
```

**Adicionar Servidor no PgAdmin:**
1. Login no PgAdmin
2. **Add New Server**
3. **General Tab:**
   - Name: `Oficina Mecanica Docker`
4. **Connection Tab:**
   - Host: `postgres` (nome do container)
   - Port: `5432` (porta interna)
   - Database: `oficina_mecanica_dev`
   - Username: `postgres`
   - Password: `postgres`
5. **Save**

### 2. **Azure Data Studio**
1. Instalar extensão PostgreSQL
2. Nova conexão com as mesmas configurações
3. Connection type: PostgreSQL

### 3. **VSCode + PostgreSQL Extension**
1. Instalar extensão "PostgreSQL" by Chris Kolkman
2. Adicionar conexão:
```json
{
    "host": "localhost",
    "port": 5433,
    "database": "oficina_mecanica_dev",
    "user": "postgres",
    "password": "postgres"
}
```

## ?? Checklist de Troubleshooting

Antes de tentar conectar no DBeaver, verifique:

- [ ] Container está rodando: `docker ps`
- [ ] Porta 5433 está mapeada: `docker port oficina_mecanica_postgres`
- [ ] Teste de conexão passou: `.\test-connection.ps1`
- [ ] Usando **porta 5433** (não 5432)
- [ ] Database: `oficina_mecanica_dev` (não `postgres`)
- [ ] Username e password: ambos `postgres`

## ?? String de Conexão Completa

Para ferramentas que pedem connection string:

```
Server=localhost;Port=5433;Database=oficina_mecanica_dev;User Id=postgres;Password=postgres;
```

ou

```
Host=localhost;Port=5433;Database=oficina_mecanica_dev;Username=postgres;Password=postgres;
```

## ?? Debug Avançado

Se nada funcionar, execute estes comandos e me envie os resultados:

```powershell
# 1. Status do container
docker ps -a | findstr postgres

# 2. Porta mapeada
docker port oficina_mecanica_postgres

# 3. Teste de conexão
.\test-connection.ps1

# 4. Versão do Docker
docker --version

# 5. Contexto do Docker
docker context ls

# 6. IP do WSL (se aplicável)
wsl hostname -I
```

## ? Resumo Final

**Configuração que DEVE funcionar:**

| Campo | Valor |
|-------|-------|
| **Tipo** | PostgreSQL |
| **Host** | `localhost` ou `127.0.0.1` |
| **Port** | `5433` |
| **Database** | `oficina_mecanica_dev` |
| **Username** | `postgres` |
| **Password** | `postgres` |

Se seguir exatamente isso e o teste `test-connection.ps1` passar (como passou!), o DBeaver **DEVE** conectar.

---

**?? Qualquer problema, execute `.\test-connection.ps1` primeiro!**
