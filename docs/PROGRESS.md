# Прогрес роботи

Останнє оновлення: 1 листопада 2026 року

## Поточне AWS-середовище

- Користувач увійшов у новий AWS Projects: `Caffeine Driven Development`, ID `387143718645`, регіон `eu-north-1`. На скриншоті Free plan, $100 кредитів, 182 дні.
- За документацією нового experience регіональні ресурси обмежені регіоном проєкту. Попередній план для `us-west-2` потребує адаптації.
- Підготовлено локальний `marathon.auto.tfvars`, `marathon.backend.hcl` і виправлено локальні Syndicate region/account/bucket names. Інструкція — `docs/AWS_PROJECT_SETUP.md`.
- Вхід `aws login` відкладено до повернення користувача. AWS-ресурси не створювались, тариф не змінювався. Перевірка сумісності Terraform/Syndicate з login credentials та E2E ще попереду.
- Перевірки конфігурації: `terraform fmt -check` і `terraform validate` успішні; обидва локальні YAML Syndicate парсяться, ID та регіон відповідають проєкту. Це локальна перевірка, не AWS plan.

## Виконано

- Переглянуто структуру проєкту, README компонентів, Terraform, ключові API-маршрути та матеріали backend/AQA homework.
- Проаналізовано надані субтитри зустрічей 1–8 і окремий файл субтитрів 9-ї зустрічі.
- Створено `docs/PROJECT_AUDIT.md`, `docs/EPAM_REQUIREMENTS.md`, `docs/EPAM_SUBMISSION.md`.
- Ініціалізовано локальний Git; початковий стан зафіксовано в коміті `7f245a3`. Remote не налаштований, `git push` не виконувався.
- У `.gitignore` додано виключення для локальних Python-середовищ, AWS/Syndicate-конфігурації, `.env`-варіантів і Terraform plan-файлів.
- Підготовлено сумісні інструменти без заміни глобальних Node/Python: Node.js 24.16.0 у `%LOCALAPPDATA%\Programs\marathon-tools`, .NET SDK 10.0.403 у `%USERPROFILE%\.dotnet`, Python 3.11.9 поруч із Python 3.13.5.
- Backend group pre-order розширено owner API, власною сесією, меню, редагуванням owner-кошика, видаленням guest і консолідованим submit.
- Frontend має нову Group Order сторінку, маршрут із картки customer reservation, учасників/суми/статуси, додавання й видалення власних страв, видалення guest і submit.
- Локальний Angular dev server запущено на `http://127.0.0.1:4200/`; головна сторінка відкривається в браузері. Наявний dev API `https://eknqcvq3l1.execute-api.us-west-2.amazonaws.com/develop` відповідає `200` на `GET /locations`, тому базовий застосунок можна переглядати локально.
- Виправлено локальний `project_path` у `.syndicate-config-restaurant-backend-app/syndicate.yml` та очевидний зайвий символ в імені artifacts bucket.
- Terraform: `init -backend=false`, `validate` і `fmt -check` пройшли; створено `docs/DEPLOYMENT_RUNBOOK.md` з командами, перевірками та застереженнями щодо витрат.
- Створено `docs/DEMO_SCRIPT.md` — покроковий сценарій показу фічі для себе та перевіряючих.
- Виявлено й виправлено дублікати трьох API Gateway-маршрутів у `deployment_resources.json`; `ApiRoutes.md` узгоджено з фактичними response-моделями.
- Виявлено й виправлено auth-регресію: `tokenInterceptor` пропускав усі URL із `/guest/`, включно з owner-маршрутами `/guest/groups/.../sessions`, які захищені Cognito. Тепер ці owner-маршрути отримують `Authorization`, а справжні гостьові — ні; додано regression-тести.
- CI workflow `.github/workflows/deploy.yml` оновлено з .NET 8 на .NET 10 відповідно до цільової версії backend.
- AWS Syndicate 1.21.3 встановлено в локальний `syndicate_venv` (Python 3.11). Спроба `syndicate build` зупинилася на `NoCredentialsError`: Syndicate потребує AWS-доступ навіть для build, тому пакування перевіримо після надання credentials.
- `dotnet publish` для `APIHandler.csproj` у конфігурації Release успішний: згенеровано `APIHandler.dll` і залежності, придатні для Lambda-пакета. Залишилися лише попередження NuGet/компілятора, не помилки.
- Усі зміни зафіксовано локальними комітами в гілці `feature/group-preorder-owner`; remote і push відсутні.

## Перевірки

- Backend: **148 passed, 0 failed** (`dotnet test`). Є NuGet security warnings для `SixLabors.ImageSharp` 3.1.12.
- Цільові frontend-тести group-order сервісу, сторінки, входу з картки бронювання та auth-регресії: **20 passed**.
- Production build Angular проходить; initial bundle ~790 kB, перевищує warning budget 500 kB, але нижче error budget 1 MB.
- Повний frontend suite (останній повний запуск до додавання 4 regression-тестів): **209 passed, 46 failed із 255**. 34 з 46 — це той самий `spyOnProperty(FileReader.prototype, 'result'/'readyState')`, який падає на non-configurable native getter; решта — давні `should create` без потрібних providers/inputs і застарілий `AppComponent` title. У попередніх запусках було 53 і 46 failed, тобто частина legacy-тестів нестабільна; повний suite не можна позначати як зелений.
- `deployment_resources.json` і `lambda_config.json` коректно парсяться як JSON; нові маршрути зареєстровані в `RouteRegistry`.

## Не виконувалося

- AWS identity/profile check успішно не пройшов: `aws sts get-caller-identity` повертає `NoCredentials`. AWS CLI 2.39.3 і Terraform 1.16.6 встановлені.
- Terraform init із S3 backend, plan, apply, destroy, Syndicate build/deploy, створення AWS-ресурсів не виконувалися. `syndicate build` локально впав на `NoCredentialsError`.
- GitHub repo creation, git remote або push не виконувалися.
- Реальний end-to-end виклик нових API не перевірений: локального HTTP-хоста backend немає, а AWS API ще не оновлений нашим кодом.

## Наступні кроки

1. Отримати від користувача AWS-доступ (профіль/credentials) і підтвердження на створення ресурсів.
2. Виконати `terraform init/plan`, оцінити витрати, після підтвердження — `apply`.
3. Задеплоїти backend через Syndicate, отримати API URL, зібрати frontend і завантажити в S3/CloudFront.
4. Перевірити груповий сценарій end-to-end на розгорнутому середовищі.
5. Додати посилання на код і deployed app у `docs/EPAM_SUBMISSION.md`; узгодити публікацію коду.
6. За потреби окремо виправити legacy frontend test stubs; 20 цільових тестів group-order і auth-регресії вже проходять.
