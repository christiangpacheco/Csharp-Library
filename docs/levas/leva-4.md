# Leva 4 — Contas e login ✅

**Concluída em:** 2026-10-10 · **Commit:** `5bcfe9b` · **Merge no `main`:** `ccfd160`

## O que foi feito
- `DTOs/Account/RegisterUserDto.cs` (nome, e-mail, senha de 8 a 100 caracteres, confirmação) e `DTOs/Account/LoginDto.cs` (e-mail, senha, `ReturnUrl`).
- `Services/Interfaces/IAuthService.cs` + `Services/AuthService.cs`:
  - `RegisterAsync`: e-mail com `Trim().ToLowerInvariant()`, senha com `PasswordHasher<User>`, sempre `Role = User`. E-mail duplicado volta como erro de validação (checagem prévia + captura do `23505`).
  - `ValidateCredentialsAsync`: devolve o usuário só se a senha confere **e** a conta está ativa; refaz o hash quando o hasher pede (`SuccessRehashNeeded`).
  - `EnsureLibrarianAsync`: seed idempotente do primeiro bibliotecário.
- `Program.cs`: `IAuthService`, `IPasswordHasher<User>`, cookie auth (`LoginPath`, `AccessDeniedPath`, `HttpOnly`, `SameSite=Lax`, 8 h deslizantes) e `UseAuthentication()` antes de `UseAuthorization()`. O seed roda na inicialização se `SeedLibrarian:Email` e `SeedLibrarian:Password` estiverem configurados.
- `Controllers/AccountController.cs`: Register (já entra logado), Login, Logout (POST + antiforgery) e AccessDenied (403). Claims: `NameIdentifier`, `Name`, `Email`, `Role`.
- Views `Account/Login`, `Account/Register`, `Account/AccessDenied`. Menu mostra "Entrar / Criar conta" ou "Nome (selo Bibliotecário) / Sair".
- Autorização: `[Authorize(Roles = Librarian)]` em Create, Edit, Deactivate, Reactivate e Delete de `BooksController` e na classe inteira de `BookCopiesController`.
- Leitor (anônimo ou usuário comum): não vê botões de gestão, não vê livro inativo (404), não vê exemplares inativos, e o parâmetro `inactive=true` da busca é ignorado.
- README: seção "Primeiro bibliotecario" com os `user-secrets` do seed.

**Sem migration:** a tabela `Users` já tinha tudo desde a Leva 2.

## Decisões
- **Bibliotecário só pelo seed:** o cadastro público nunca cria `Librarian`. Promover usuários fica para uma leva futura, se for preciso.
- **Mensagem genérica no login** ("E-mail ou senha inválidos") para não revelar se o e-mail existe ou se a conta está inativa.
- **Open redirect:** `ReturnUrl` só é seguido se `Url.IsLocalUrl` for verdadeiro.
- **"X de Y disponíveis" nos detalhes** é calculado depois de filtrar os exemplares que cada perfil vê (achado dos testes: o leitor via "1 de 3" com só 2 linhas na tabela).

## Validação
- `dotnet build` sem erros nem avisos; `has-pending-model-changes` sem mudanças.
- Migration `AddActiveAndSoftDelete` (Leva 2) aplicada no banco local, que estava pendente.
- Agente de testes funcionais (curl com cookie jar e token antiforgery) contra o app em `http://localhost:5081`: **145 verificações, nenhum defeito**. Cobriu anônimo (302 para login, 403 no AccessDenied), cadastro (validações, 400 sem antiforgery, e-mail normalizado, duplicado), login/logout (senha errada, returnUrl local e externo, cookie adulterado), bibliotecário (CRUD de livros e exemplares, ativar/desativar, excluir) e usuário comum (403 em todas as ações de gestão, nada alterado no banco).

## Pendências / observações
- **Dados de teste no banco local `library`:** a conta `biblio.teste@example.test` (bibliotecário criado pelo seed via variáveis de ambiente, só para o teste), o usuário `leitor1.1791598147@example.test`, o livro "Livro Teste A editado 1791598147" com 3 exemplares e um livro excluído. Podem ser desativados/excluídos pelo app ou o banco recriado.
- Na **lista** de livros, o "Disponíveis X de Y" ainda conta exemplares inativos no total para o leitor (o X está certo). Ajustar junto com a Leva 5.
- Usuário desativado **depois** de logar continua com o cookie válido até expirar (8 h). Se for preciso, validar o usuário a cada requisição (`OnValidatePrincipal`).
- Link "Empréstimos" segue dando 404 (Leva 5).

## Ponto de partida da Leva 5 (Empréstimos)
- DTOs `CreateLoanDto` e `LoanResponseDto`; `ILoanService`/`LoanService` com as regras do `PLANO.md` (só `Available`, máx. 3 abertos, atraso bloqueia, 14 dias, transação, devolução dupla é erro).
- O usuário logado vem de `User.FindFirstValue(ClaimTypes.NameIdentifier)`; "Meus empréstimos" para `User`, Emprestar/Devolver/Atrasados para `Librarian`.
- Seção `LoanRules` no `appsettings.json`.

## Como testar
```bash
git pull && git checkout leva-4
dotnet user-secrets set "SeedLibrarian:Email" "voce@exemplo.com"   # e Name / Password
dotnet ef database update
dotnet run   # http://localhost:5080 (reinicie se já estiver rodando)
```
Entre como bibliotecário e cadastre um livro; saia, crie uma conta comum e tente abrir `/Books/Create` (deve mostrar "Acesso negado").
