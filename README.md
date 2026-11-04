# Green & Tasty — Group Pre-Order

Ресторанний застосунок для EPAM IT Marathon 2026. Додаткова full-stack функція —
керування груповим передзамовленням власником бронювання.

**[Відкрити застосунок](https://dd9nzqrfa7inx.cloudfront.net/)** ·
**[Код проєкту](https://github.com/Xevon-dev/epam-marathon-2026-green-and-tasty)**

## Реалізована функція

- Гості приєднуються за запрошенням `/join/:token` без реєстрації, обирають страви та підтверджують кошики.
- Власник відкриває **My Reservations → Group order** для активного бронювання на 2+ гостей.
- Сторінка показує учасників, страви, кількість, статуси кошиків, суми учасників і загальну суму.
- Власник додає й видаляє власні страви, підтверджує свій кошик та може видалити гостя.
- **Confirm group order** об’єднує підтверджені кошики за `dishId` і надсилає спільний pre-order ресторану.
- Owner API перевіряє Cognito identity та належність бронювання; гостьові операції використовують токен сесії.

Перед підтвердженням групи потрібно створити базовий **Pre-order** у картці бронювання.
Редагування закривається за 30 хвилин до початку бронювання.

## Технології та структура

| Каталог | Призначення |
| --- | --- |
| `frontend/restaurant-frontend-app` | Angular 22, TypeScript, SCSS, Jasmine/Karma |
| `backend/restaurant-backend-app` | .NET 10, AWS Lambda, API Gateway, DynamoDB, Cognito, xUnit |
| `infra/terraform` | S3, CloudFront та спільна AWS-інфраструктура |
| `aqa/restaurant-aqa-framework` | Окремі AQA-матеріали: .NET, Reqnroll, Playwright |

Основні файли фічі:

- [Сторінка Group Order](frontend/restaurant-frontend-app/src/app/pages/group-order-page/group-order-page.component.ts)
- [Frontend API service](frontend/restaurant-frontend-app/src/app/services/guest-sessions/group-preorder.service.ts)
- [Backend service](backend/restaurant-backend-app/dnapp/lambdas/api-handler/Services/GuestSessions/GuestSessionService.cs)
- [Backend routes](backend/restaurant-backend-app/dnapp/lambdas/api-handler/Routing/RouteRegistry.cs)
- [Завдання EPAM](homework-backend.md)

## Перевірений стан — 03.11.2026

- Репозиторій публічний; HTTPS frontend, меню, зображення та API локацій/страв доступні.
- Backend unit-тести: **148 passed, 0 failed**.
- Цільові frontend-тести фічі та auth-регресій: **21 passed, 0 failed**.
- Production build проходить; є попередження про розмір bundle.
- Owner-only сценарій перевірено на розгорнутому застосунку.
- **Повний guest contribution → group submit → waiter view ще не підтверджено end-to-end.**
- Останній повний frontend suite: **209 passed, 46 failed із 255**; це попередній запуск до додавання regression-тестів. Повний suite не позначено як зелений.

## Локальні перевірки

Потрібні Node.js **24.16.0** (див. `.nvmrc`), npm та .NET SDK **10**.

Backend unit-тести з кореня репозиторію:

```powershell
dotnet test backend/restaurant-backend-app/Tests/APIHandler.Tests.csproj
```

Frontend:

```powershell
cd frontend/restaurant-frontend-app
npm ci
npm start
```

Development API задається в `src/environments/environment.development.ts`.
Вихідна development-конфігурація може посилатися на навчальний API, який не містить цієї фічі.
Для демонстрації використовуйте посилання на розгорнутий застосунок вище.

Цільові frontend-тести (потрібен Chrome):

```powershell
npm test -- --watch=false --browsers=ChromeHeadless --include="src/app/pages/group-order-page/group-order-page.component.spec.ts" --include="src/app/services/guest-sessions/group-preorder.service.spec.ts" --include="src/app/auth/token.interceptor.spec.ts" --include="src/app/components/reservations/card-reservation/card-reservation.component.spec.ts"
```

Production build без публікації:

```powershell
$env:BACKEND_URL = "https://kfhwimu435.execute-api.eu-north-1.amazonaws.com/develop"
npm run build
```

## Розгортання та CI

Поточний demo працює в **`eu-north-1`**, frontend — через CloudFront HTTPS.
Порядок ручного розгортання: Terraform → AWS Syndicate → frontend.
Параметри акаунта та Terraform backend задаються локально в gitignored файлах
`infra/terraform/marathon.auto.tfvars` і `infra/terraform/marathon.backend.hcl`.
Для Terraform init використовується `-backend-config=marathon.backend.hcl`.
Syndicate використовує локальну конфігурацію відповідного AWS-середовища.

GitHub Actions виконує перевірки коду; автоматичні AWS deploy/apply прибрано.
AQA-сценарії проти живого середовища запускаються лише вручну і можуть змінювати тестові дані.
Відомі legacy lint/test-проблеми frontend можуть давати невдалий CI — вони не приховані.

Застосунок створено на основі навчального проєкту **EPAM IT Marathon 2026**.
Авторські відомості та матеріали вихідного проєкту збережено.
