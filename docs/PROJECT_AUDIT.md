# Аудит проєкту EPAM IT Marathon 2026

Дата аудиту: 31 жовтня 2026 року

## Загальний висновок

Це повноцінний застосунок ресторану Green & Tasty: Angular SPA, .NET AWS Lambda backend, інфраструктура Terraform і окремий AQA-проєкт на .NET/Playwright. Backend не є Python-сервером: Python тут потрібен переважно для AWS Syndicate.

## Структура

- `frontend/restaurant-frontend-app` — Angular-застосунок, маршрути, сторінки, сервіси HTTP, стилі та Jasmine/Karma тести.
- `backend/restaurant-backend-app` — .NET Lambda-функції, API-обробники, сервіси, репозиторії, моделі та xUnit тести.
- `infra/terraform` — спільна AWS-інфраструктура.
- `aqa/restaurant-aqa-framework` — окремий фреймворк автоматизації на .NET, Reqnroll і Playwright.
- `homework-backend.md`, `homework-aqa.md` — матеріали завдань марафону.

## Архітектура та технології

### Frontend

- Angular 22, TypeScript, standalone-компоненти, SCSS.
- npm lockfile; очікувана версія Node.js в `.nvmrc` — 24.16.0.
- Angular CLI, Jasmine/Karma; наявний окремий набір компонентних та сервісних тестів.
- Production build вимагає `BACKEND_URL`; локальний `ng serve` використовує development-конфігурацію.
- Статичний frontend розміщується в приватному S3 bucket та роздається через CloudFront.

### Backend

- .NET 10 Lambda-проєкти, AWS SDK, API Gateway proxy handler.
- AWS Syndicate збирає та розгортає Lambda stack.
- DynamoDB використовується як основне сховище; також код інтегрується з Cognito, S3 і SQS.
- Є xUnit/Moq unit-тести сервісів.
- Backend не має локального HTTP-хоста/емулятора, зафіксовано у `frontend/restaurant-frontend-app/CLAUDE.md`; unit-тести можна запускати локально.

### AWS / Terraform

У Terraform описані S3 buckets, CloudFront distribution/OAC, Cognito, SQS, EventBridge, IAM, CloudWatch alarms та dashboard. Lambda і API Gateway конфігуруються AWS Syndicate. Розгортання створює реальні ресурси та потенційні витрати; Free Plan не є гарантією нульової вартості. `terraform apply`, Syndicate deploy, а також зміни чи видалення ресурсів потребують окремого підтвердження користувача.

## Наявні можливості

- Меню страв, фільтрація/сортування, локації.
- Реєстрація, вхід, профіль користувача.
- Бронювання, перегляд і зміна бронювань, інтерфейси клієнта й офіціанта.
- Звичайні передзамовлення та відгуки.
- Групові запрошення за посиланням, гостьові сесії, гостьове меню/кошик, надсилання кошика.
- Звіти, повідомлення, профільні зображення.
- Інтерфейс обраного (`Favorites`) у фронтенді та відповідних API у backend під час пошуку не знайдено.

## Виявлені прогалини та ризики

1. **Owner-частина групового передзамовлення неповна.** Існують гостьовий flow і видалення сесії, але в `RouteRegistry.cs` не виявлено API отримання повного списку учасників, owner-session ID і підтвердження зведеного замовлення. Повного owner-екрана у frontend також не виявлено. Деталі очікуваних endpoint-ів є у `homework-backend.md`.
2. **Немає локального запуску backend як вебсервера.** Підтверджено локальні unit-тести, але не звичайний локальний API.
3. **Версії середовища.** Репозиторій очікує Node.js 24.16.0; README backend рекомендує Python 3.10–3.11 для інструментів Syndicate; Lambda-проєкти таргетять .NET 10.
4. **AWS-регіон вказівки суперечливі.** README/Terraform/workflow вказують `us-west-2`; frontend `CLAUDE.md` містить примітку про інший backend/dev API регіон. Потрібно звірити реальну deployment-конфігурацію перед використанням AWS.
5. **CI конфігурація має потенційну невідповідність:** `.github/workflows/deploy.yml` налаштовує .NET 8, тоді як backend csproj таргетять .NET 10. Не запускати цей workflow через публікацію коду без перевірки.
6. **Залежності backend мають security advisory.** `dotnet test` показав NU1901/NU1902/NU1903 для деяких NuGet-пакетів. Версії не змінювалися під час аудиту.
7. **Frontend має відому технічну заборгованість**, описану в `frontend/restaurant-frontend-app/CLAUDE.md`: lint-помилки, застарілі/stub-тести, великий bundle та npm audit issues. Не проводити масовий рефакторинг у межах термінової функції.
8. **S3 доступність:** Terraform свідомо робить frontend bucket приватним за CloudFront; окремі buckets для публічних зображень/Swagger мають публічний read policy. Перед розгортанням перевірити зміни плану.

## Локальний запуск і перевірки

### Frontend

У `frontend/restaurant-frontend-app`:

```powershell
npm ci
npm start
```

`npm start` має запускати development frontend на `http://localhost:4200`; API-запити спрямовуються на URL із development environment. Production build:

```powershell
$env:BACKEND_URL = "https://<api-id>.execute-api.<region>.amazonaws.com/develop"
npm run build
```

### Backend

Локальний сервісний хост не передбачений. Для тестів:

```powershell
dotnet test .\restaurant-backend-app.sln
```

## Фактично виконані перевірки

- Node.js 24.16.0 завантажено окремо у `%LOCALAPPDATA%\Programs\marathon-tools`; Node.js 24.11.0, що вже був у системі, не замінювався.
- .NET SDK 10.0.403 інстальовано локально в `%USERPROFILE%\.dotnet`.
- Python 3.11.9 встановлено поряд із Python 3.13.5.
- `dotnet test .\restaurant-backend-app.sln` після розробки group-order backend — **148 пройдено, 0 помилок**; залишаються попередження компілятора і вразливих NuGet залежностей.
- `npm ci` завершився: 635 пакетів встановлено; npm повідомив про 57 advisory (37 moderate, 17 high, 3 critical). `npm audit fix` без аналізу не запускався.
- Production build пройшов, bundle placeholder check успішний. Initial bundle близько 790 kB; перевищує Angular warning budget 500 kB, але нижче 1 MB error budget.
- Цільові frontend-тести group-order сторінки/API та входу з картки бронювання: 13 passed. Останній повний suite: 201/254 passed, 53 failed; основні залишкові проблеми — старі `spyOnProperty`-помилки та відсутні Angular test providers/inputs.
- AWS-команди, Terraform plan/apply і deployment не виконувалися.

## План для технічної частини 3-го рівня

1. Завершити перевірку frontend install/build і локального UI.
2. Завершити запропоновану EPAM функцію групового pre-order власника: захищений перегляд учасників/сум, власна сесія власника, керування гостями та підтвердження зведеного замовлення в Angular і backend.
3. Перевірити AWS-конфігурацію без розкриття credentials і підготувати план із оцінкою ресурсів/витрат.
4. Попросити підтвердження перед AWS deployment. Не виконувати `terraform apply` / `destroy` і не публікувати код без окремого дозволу.
5. Підготувати посилання та інструкції для здачі; кодовий репозиторій може бути GitHub або інший сервіс згідно з матеріалами зустрічей.
