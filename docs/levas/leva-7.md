# Leva 7 — Reset de senha ✅

**Concluída em:** 2026-10-23 · **Branch:** `leva-7` (PR para `main`)

## O que foi feito
- Modelo `PasswordResetToken` (`UserId`, `TokenHash`, `ExpiresAt`, `UsedAt`, `CreatedAt`, `DeletedAt`) + `PasswordResetTokenConfigurations` e migration `AddPasswordResetTokens` (tabela `PasswordResetTokens`).
- `IPasswordResetService` / `PasswordResetService`: cria o token e redefine a senha, com as regras de segurança abaixo.
- `IEmailSender` + `LoggingEmailSender`: abstração de envio; em desenvolvimento o link é registrado no log.
- DTOs `ForgotPasswordDto` (e-mail) e `ResetPasswordDto` (token escondido + nova senha + confirmação — o arquivo existia vazio e foi preenchido).
- `AccountController`: `ForgotPassword` (GET/POST) e `ResetPassword` (GET/POST), todas `[AllowAnonymous]`, POSTs com `[ValidateAntiForgeryToken]`.
- Views `Views/Account/ForgotPassword.cshtml` e `ResetPassword.cshtml`, e o link **"Esqueci minha senha"** na tela de login.
- `appsettings.json`: seção `PasswordReset` (`TokenLifetimeMinutes` = 60), ligada por `Services/PasswordResetOptions.cs` + `Configure<PasswordResetOptions>` no `Program.cs`. Registrados `IPasswordResetService` e `IEmailSender`.

## Fluxo
1. Na tela de login, "Esqueci minha senha" leva ao formulário de e-mail.
2. O usuário informa o e-mail; se houver conta ativa, um link com o token é "enviado" (no dev, aparece no log). A resposta é sempre a mesma mensagem genérica.
3. O link abre a tela de redefinição (token num campo escondido); o usuário define a nova senha.
4. Com o token válido, a senha é trocada e o token marcado como usado; o usuário entra com a nova senha.

## Decisões e segurança
- **Só o hash do token é guardado:** gera-se um token aleatório de 32 bytes (base64url); no banco fica só o SHA-256 em hexadecimal. O valor em claro vai no link e nunca é persistido — quem lê o banco não consegue redefinir a senha de ninguém. (Hash rápido basta: o token tem entropia alta, diferente de uma senha.)
- **Expiração:** o token vale por `TokenLifetimeMinutes` (padrão 60).
- **Uso único:** depois de redefinir, o token é marcado como usado; reusá-lo falha.
- **Um link por vez:** pedir um novo link invalida os anteriores ainda não usados; e trocar a senha invalida os outros tokens abertos do usuário.
- **Anti-enumeração:** `ForgotPassword` responde a mesma mensagem exista ou não a conta, e só manda e-mail quando há conta ativa. Na redefinição, token inexistente, expirado, já usado ou de usuário inativo dão todos a mesma mensagem ("Link inválido ou expirado").
- **Tabela, não Data Protection:** o `PLANO` dava as duas opções; a tabela segue o padrão do projeto (EF, soft delete, FK `Restrict`, CHECK constraints) e é auditável e testável contra o Postgres real.
- **Envio de e-mail:** nesta leva ficou a **abstração** (`IEmailSender`) com o remetente de desenvolvimento que registra o link. O remetente de produção (serviço externo lendo `EmailApiKey`) entra junto do Key Vault na Leva 8 — é o ponto de integração que falta e depende de uma conta/provedor escolhido.

## Validação
`dotnet build` limpo (0 avisos, 0 erros). Fluxo testado por HTTP contra um PostgreSQL 16 real (12/12):
- "Esqueci minha senha" redireciona para o login e gera exatamente um link.
- Redefinir com o link troca a senha; login com a nova funciona e com a antiga falha.
- Reusar o token já usado dá "inválido ou expirado".
- E-mail inexistente responde igual e **não** gera link (anti-enumeração).
- Token expirado (forçado no banco) é recusado e a senha não muda.
- Abrir `ResetPassword` sem token volta para "Esqueci minha senha".

## Pendências / observações
- Falta o `IEmailSender` de produção (serviço externo + `EmailApiKey`); hoje o link sai no log em desenvolvimento.
- Testes automatizados do `PasswordResetService` podem entrar quando o projeto de testes (Leva 6, PR #5) estiver no `main` — esta leva saiu a partir do `main`, sem o projeto de testes.
- Herdado: usuário desativado depois de logar mantém acesso até o cookie expirar (8h).

## Ponto de partida da Leva 8 (Azure)
- Key Vault no `Program.cs` fora de Development; `EmailApiKey` e a connection string saem para o Key Vault.
- App Service com Managed Identity + PostgreSQL Flexible Server; migrations via `dotnet ef migrations script --idempotent`.

## Como testar
```bash
git pull
docker start library-db
dotnet ef database update   # aplica a migration AddPasswordResetTokens
dotnet run                  # http://localhost:5080
```
Na tela de login, clique em "Esqueci minha senha", informe um e-mail cadastrado e copie o link que aparece no log da aplicação (`Link: http://localhost:5080/Account/ResetPassword?token=...`). Abra o link e defina a nova senha.
