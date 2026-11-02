# Прогрес роботи

Останнє оновлення: 1 листопада 2026 року

## Поточне AWS-середовище

### Перевірки після входу (1 листопада)

- Користувач погодив продовження за сценарієм марафону з подарованими кредитами на Free plan. STS підтвердив ID `387143718645`; `freetier get-account-plan-state` повернув `FREE`, `ACTIVE`, залишок $100, термін до 30 квітня 2027. Повторна browser CLI авторизація успішна.
- Налаштовано локальний профіль `marathon-tools` через `credential_process`, який використовує браузерний профіль `marathon`. STS через CLI та boto3 Syndicate працює. Секрети в репозиторій не записувались.
- Виконано попередній Terraform plan в окремій копії конфігурації з локальним backend (`%LOCALAPPDATA%/Temp/opencode/marathon-terraform-plan`), щоб не створювати S3 state bucket до перегляду плану: **36 add, 0 change, 0 destroy**. Це лише Terraform-частина; Lambda/API/DynamoDB розгортає Syndicate окремо. Не застосовувати цей тимчасовий plan: перед реальним apply потрібен plan із робочим S3 backend.
- Виправлено `infra/terraform/monitoring.tf`: Budget більше не створює notifications із порожнім списком отримувачів. Без `alert_emails` бюджет видимий у Billing, але листи не надсилаються. `fmt -check`, `validate` і повторний plan успішні.
- План розглянуто: 36 Terraform ресурсів, `0 change`, `0 destroy`. План лише перелічує ресурси й не оцінює вартість. Виявлено, що Syndicate S3 resource повторно використовує наявний bucket; код Syndicate перевіряє існування й не викликає повторне створення. Terraform має налаштувати цей Swagger bucket до Syndicate.
- Після дозволу користувача створено приватний S3 state bucket `restaurant-terraform-state-387143718645` у `eu-north-1`; увімкнено versioning, блокування публічного доступу й AES256 encryption. Terraform init і робочий plan успішні: **36 add, 0 change, 0 destroy**.
- Terraform apply частково завершився: зафіксовано **34 ресурси** у remote state. AWS відхилив CloudFront Distribution із `AccessDenied: Your account must be verified before you can add new CloudFront resources`; distribution та залежна frontend bucket policy не створені. Інші 34 ресурси (Cognito, S3, SQS, IAM, schedule, monitoring, budget) у Terraform state.
- До вирішення CloudFront verification не повторювати apply без нового plan. Можливі шляхи: AWS Support account verification або перейти на публічний S3 static website лише для frontend bundle (HTTP, без CloudFront); це змінює hosting архітектуру й потребує окремого узгодження.
- AWS Support case `179360120700332`: користувач додав повідомлення, що номер підтверджено. AWS відповів, що передав CloudFront verification на внутрішню команду; review може тривати кілька робочих днів. Тариф Basic/Free не змінювався, upgrade не погоджувався.
- AWS Syndicate успішно розгорнув API Gateway (`kfhwimu435`, stage `develop`), три .NET 10 Lambda, 13 DynamoDB tables та Swagger UI. API URL: `https://kfhwimu435.execute-api.eu-north-1.amazonaws.com/develop`. `GET /locations` і `/dishes` спершу повертали `200 []`; owner sessions endpoint без Cognito token повертає `401` (очікувано).
- Через IAM/boto3 додано лише демонстраційні дані без персональних даних: локація `demo-location-nov-2026`, три столи (capacity 2/4/6), чотири доступні страви. Перевірено через `GET /locations`, `/locations/{id}/dishes` та `/available-tables`.
- Перша спроба бронювання показала, що сервіс призначення очікує waiter record для кожної локації. Додано synthetic waiter `demo-waiter-1` лише в DynamoDB для цієї тестової локації; Cognito-акаунт або персональні дані не створювались. GSI query підтвердила один waiter для локації; бронювання можна повторити.
- AWS Free plan API після backend deploy показав `ACTIVE`, $140 credits, дата завершення 30 квітня 2027. Раніше показував $100; зміна балансу може бути отриманими марафонськими кредитами, не можна трактувати як підтверджений нульовий usage cost.
- Production frontend build успішний із новим API URL `https://kfhwimu435.execute-api.eu-north-1.amazonaws.com/develop`; bundle check пройшов, конфігураційних placeholder немає. Є нефатальні наявні bundle/CSS/CommonJS budget warnings. Файли ще не завантажувалися до frontend bucket.
- Через CloudFront account verification тимчасово опубліковано frontend у наявному публічному S3 website bucket під префіксом `/app/`, не змінюючи Swagger index у корені. Для `base-href=/app/` окремо завантажено 13 статичних `/assets` шляхів і `error.html` SPA fallback. Посилання: `http://restaurant-backend-dev-swagger-ui-387143718645.s3-website.eu-north-1.amazonaws.com/app/` (HTTP, тимчасовий fallback, без CloudFront/HTTPS).
- Відкрито deployed SPA у browser; заголовок/сторінка відображаються, API `GET /locations` і `/dishes/popular` повернули 200. На глибокому маршруті S3 віддає 404 status з `error.html`, але Angular app завантажується; ще перевірити повний бронювальний та груповий сценарій з користувачем.
- Під час E2E тесту бронювання успішно створилось, але сторінка Group order лишалася на loading попри 200 від трьох API запитів. Рефакторено завантаження сторінки на `forkJoin` + `finalize`, додано `ChangeDetectorRef` та валідацію snapshot; тест перевіряє, що `isLoading` скидається при успішному завантаженні. Цільові frontend-тести повторно пройшли **20/20**.
- Оновлену production-збірку з base-href `/app/` повторно залито до S3 fallback. Користувача попросили зробити Ctrl+Shift+R і повторно перевірити. Повний group-order E2E ще не підтверджений.
- Повторно звірено вимоги рівнів 1–3, ресторанну тематику й фінальні посилання із субтитрами; уточнено приблизний формат фінального тесту в `docs/EPAM_REQUIREMENTS.md`.
- Backend розгорнутий. Frontend ще не опублікований через CloudFront verification; ще потрібно засіяти тестові дані, опублікувати frontend і провести E2E після готовності хостингу.

### Попередня підготовка

- Повторно розпаковано архів субтитрів користувача й перевірено пояснення про Free Tier у зустрічах 3–4. Організатори передбачають використання подарованих AWS-кредитів без переходу на платний план. Цитати й таймкоди — `docs/AWS_SUBTITLE_EVIDENCE.md`; AWS-ресурси під час перевірки не створювались.

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
