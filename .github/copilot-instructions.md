Copilot Instructions
🏗️ Architecture Overview
This is a .NET 9 PWA.

src/                    # Application code
├── Controllers/        # REST endpoints (CuentasController, EspaciosController)
├── Models/             # EF Core entities + DTOs (TransaccionOcrResult)
├── Data/             	# DbContext (PresupuestoContext)
├── Servicios/       	# Servicios (EmailService,PushNotificationService)
├── Helpers/         	# Common functions (MonedaHelper)
├── Documentacion/      # Md Files (LOGIN-FIX.md)
├── Html/         		# Email Templates (reset_password)
├── PoweshellScripts/   # Scripts Powershell (verify-requirements)
├── SqlScripts/   		# Script SQL (IHeroRepository)
├── Views/         		# Abstractions (DiagnosticoNotificaciones)
└── Program.cs          # DI, middleware, OpenTelemetry config

tests/                  # xUnit tests with Moq

🔐 Security First
You are a cybersecurity-focused agent. Every recommendation must include:

Input validation and sanitization
Secure configuration (no hardcoded secrets)
Proper error handling without leaking internals
All generated code must be tested and documented


📁 Code Organization
Type	Location	Example
Controllers	src/Controllers/	HeroController.cs
Models/Entities	src/Models/	Hero.cs, HeroContext.cs
Tests	tests/	HeroControllerTests.cs

✍️ Naming Conventions
Classes/Methods: PascalCase → HeroController, GetAllHeroes()
Variables/Parameters: camelCase → heroId, connectionString
Interfaces: Prefix with I → IHeroRepository
Files: Match class name → HeroController.cs

🧪 Testing Pattern
Use xUnit + Moq. Always include happy and sad paths:

Run tests: dotnet test from root or tests/ directory.

⚠️ Pre-Commit Checklist
ALWAYS before commit or push:

🏗️ Build: dotnet build - Verify compilation succeeds
🧪 Test: dotnet test - Run ALL tests and verify they pass
✅ New tests: Add unit tests for any new code (happy + sad paths)
🔍 Review: Check for warnings or errors in build output
# Quick validation before commit
dotnet build && dotnet test
Never commit code that:

❌ Doesn't compile
❌ Has failing tests
❌ Lacks unit tests for new functionality
📝 Commit Messages
Use conventional commits with emojis:

feat: ✨ add PostgreSQL support
fix: 🐛 correct CORS configuration
docs: 📖 update README
ci: 🔄 update workflow to .NET 9
chore: 🔧 update dependencies
refactor: ♻️ extract database factory
Max 100 characters, be concise.

🌿 Branch Naming
Use standard prefixes:

feature/ → New features (feature/add-postgresql-support)
fix/ → Bug fixes (fix/cors-configuration)
docs/ → Documentation (docs/update-readme)
refactor/ → Code refactoring (refactor/async-repository)
ci/ → CI/CD changes (ci/update-workflows)
🛡️ Rate Limiting
The API uses ASP.NET Core Rate Limiting with Fixed Window policy:

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 10;
    });
});

// Apply to controllers with attribute
[EnableRateLimiting("fixed")]
public class HeroController : ControllerBase

🔄 GitHub Workflows
Workflow	Trigger	Purpose
ci.yml	Push/PR to main	Build, test, coverage
release.yml	Tags v*	Create GitHub release
github-packages-docker.yml	Push to main	Build & push Docker image
docker-scans.yml	Push to main	Security scans (Trivy, Checkov, Grype)
iac-scans.yml	Changes to infrastructure/	Terraform security scans
