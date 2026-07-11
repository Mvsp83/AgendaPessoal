# Minha Agenda

**Versão 1.0.1** — Desenvolvido por: eMeVe83

Sistema web pessoal para centralizar lembretes, anotações, tarefas e assuntos gerais em um lugar só,
com agenda agrupada, temas, tags, busca e lembretes via WhatsApp.

**Stack:** Blazor Server (.NET 9) + MudBlazor + EF Core (SQLite local / PostgreSQL na nuvem).

**Visual:** tema escuro "preto profundo + amarelo" como identidade (definido em `TemaDoApp.cs`),
com variante clara. Segue o modo do sistema automaticamente e tem botão de alternar na barra superior.

## Conceito

Tudo é um **Item**: título + conteúdo + tema + tags + data/hora opcional + lembrete opcional.

- "Médico quarta às 14h" → item com data/hora e lembrete via WhatsApp
- "Comandos úteis de git" → nota sem data, tags `git`, `dev`
- "Vídeo para assistir" → nota com link, tag `assistir`

**Aniversários** têm cadastro próprio: dia/mês obrigatórios, ano opcional (calcula a idade),
grupo, observações (ideias de presente...), aviso no WhatsApp no dia (a partir de um horário
configurável) e aviso antecipado opcional (1 a 15 dias antes). Nascidos em 29/02 são avisados
em 28/02 nos anos não bissextos. A agenda inicial destaca os aniversários dos próximos 7 dias.

## Rodar localmente

```bash
dotnet run
```

O banco SQLite é criado automaticamente em `dados/agenda.db`. Acesse http://localhost:5002.

## Login

O app é protegido por login (usuário único). No **primeiro acesso** ele pede para criar a conta
(usuário + senha); depois, exige login — a sessão dura 30 dias. A senha é armazenada como hash
PBKDF2 na tabela de configurações. Troca de senha em **Configurações → Conta**; botão **Sair**
na barra superior.

Esqueceu a senha? Apague as linhas `login_usuario` e `login_senha_hash` da tabela `Configuracoes`
do banco — o app volta ao modo "criar conta".

> Nota: após um redeploy no Render, as chaves de proteção de cookie são recriadas e a sessão
> é invalidada — basta fazer login de novo.

## Ativar o WhatsApp (CallMeBot — gratuito)

1. Acesse https://www.callmebot.com/blog/free-api-whatsapp-messages/
2. Adicione o número do bot aos seus contatos e envie a mensagem de autorização pelo WhatsApp
3. Você receberá sua **API key**
4. No app, abra **Configurações**, preencha telefone (com DDI, ex.: `+5511999999999`) e a API key
5. Clique em **Enviar mensagem de teste** para validar

## Hospedagem gratuita (Render + Neon)

A combinação gratuita recomendada: **Render** (app) + **Neon** (banco PostgreSQL) + **cron-job.org** (mantém os lembretes disparando).

### 1. Banco — Neon (https://neon.tech)

1. Crie uma conta gratuita e um projeto
2. Copie a **connection string** no formato `postgresql://usuario:senha@host/banco`

### 2. App — Render (https://render.com)

1. Suba este repositório para o GitHub
2. No Render: **New → Web Service**, conecte o repositório
3. Runtime: **Docker** (o `Dockerfile` já está pronto)
4. Instance type: **Free**
5. Em **Environment Variables**, adicione:
   - `DATABASE_URL` = a connection string do Neon
6. Deploy. O app cria as tabelas sozinho na primeira execução

### 3. Cron — cron-job.org (gratuito)

O plano gratuito do Render "hiberna" o app após ~15 min ocioso — e app dormindo não dispara lembrete.
O cron externo resolve isso:

1. No app publicado, abra **Configurações → Verificação externa (cron)** e clique em **Gerar token**
2. Copie a URL gerada (`https://seuapp.onrender.com/api/lembretes/verificar?token=...`)
3. Em https://cron-job.org, crie um job gratuito chamando essa URL **a cada 5 minutos**

Assim, mesmo com o app hibernando, os lembretes disparam com no máximo ~5–6 min de atraso.

> **Importante:** o plano gratuito do Render tem disco efêmero — por isso o banco fica no Neon
> (persistente), não em SQLite. Localmente, sem `DATABASE_URL`, o app usa SQLite sem configurar nada.

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `Models/` | Item, Tema, Tag, Notificação (histórico), Configuração |
| `Data/` | `AgendaDbContext` (EF Core) |
| `Services/` | Serviços de itens/temas, motor de lembretes, notificador CallMeBot, fuso do Brasil |
| `Components/Pages/` | Agenda, Itens (busca/filtros), Editor, Aniversários, Temas, Configurações |

Todas as datas usam o fuso **America/Sao_Paulo**, independente do fuso do servidor.
