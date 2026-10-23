# Pós-Leva 8 — E-mail real e projeto só local ✅

**Concluído em:** 2026-10-23 · **Commits:** `b04d622` (merge `37a989b`), `976a2a7`, `8257e49`

## Decisões do Christian
- **Sem deploy:** o projeto não será publicado no Azure. Ele roda e é testado só localmente (PostgreSQL e Mailpit em Docker).
- **E-mail de verdade com Brevo** (plano gratuito: 300 e-mails/dia, sem cartão, sem prazo).

## O que foi feito
- **Envio por SMTP (MailKit):** `Services/SmtpEmailSender.cs` + `Services/SmtpOptions.cs` (seção `Smtp`). Sem `Smtp:Host`, o app volta ao `LoggingEmailSender` (link só no log).
- **Mailpit em dev:** serviço `mailpit` no `docker-compose.yml` (SMTP `1025`, caixa web http://localhost:8025). O `appsettings.Development.json` aponta para ele.
- **Brevo para envio real:** `Smtp:Host/Port/UseStartTls/Username/Password/FromEmail` nos User Secrets (têm prioridade sobre o Mailpit). Passo a passo na seção "E-mail" do README.
- **Segurança:** `System.Security.Cryptography.Pkcs` 10.0.13 referenciado direto (o MailKit trazia a 10.0.0, com vulnerabilidades conhecidas).
- **Deploy desligado:** `.github/workflows/deploy.yml` roda só manualmente (`workflow_dispatch`). Decisão registrada no `PLANO.md`, no `leva-8.md` e no README. O código do Key Vault e o `docs/azure-deploy.md` ficam como referência.
- **Correções achadas no teste com o Brevo (`8257e49`):**
  - O MailKit rejeitava o certificado válido do Brevo por não conseguir baixar a CRL (intermediários do Let's Encrypt não publicam mais CRL). Nova opção `Smtp:CheckCertificateRevocation`, padrão `false`; nome do host, validade e cadeia continuam verificados.
  - Falha de envio no "Esqueci a senha" virava 500 só para e-mails cadastrados (revelava contas). Agora vai para o log e a resposta é sempre a mesma.

## Validação
- `dotnet build` sem erros e sem avisos; `dotnet test`: 9/9 (banco separado `library_test`).
- Mailpit: "Esqueci a senha" → e-mail capturado; o link abre `/Account/ResetPassword` (200).
- Brevo: envio aceito pelo SMTP para o remetente verificado, sem erro no log.

## Estado do ambiente local
- Containers: `library-db` (PostgreSQL 17, porta 5433) e `library-mailpit` (1025/8025).
- Banco `library` com todas as migrations, inclusive `AddPasswordResetTokens`.
- User Secrets: connection string e `Smtp:*` do Brevo. `SeedLibrarian:*` ainda **não** configurado (sem bibliotecário próprio).
- Dados de teste no banco: contas `biblio.teste@example.test`, `leitor1.*@example.test`, `mail.teste.*@example.test`, a conta de leitor do e-mail do Brevo ("Christian (teste e-mail)") e livros/exemplares de teste.

## Pendências
- Configurar o `SeedLibrarian` nos User Secrets para ter o próprio bibliotecário.
- Limpar os dados de teste do banco local, se quiser (desativar/excluir pelo app ou recriar o banco).
- Herdadas: na lista de livros, o total do "X de Y" para o leitor conta exemplares inativos; usuário desativado mantém o cookie até expirar (8 h).

## Como rodar
```bash
docker start library-db
POSTGRES_PASSWORD=nao-usado docker compose up -d mailpit   # só se for usar o Mailpit
dotnet ef database update
dotnet run   # http://localhost:5080
```
