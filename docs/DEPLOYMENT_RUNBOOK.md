# Ручне розгортання Green & Tasty

Demo вже розгорнуто: https://dd9nzqrfa7inx.cloudfront.net/.
Регіон backend та регіональних ресурсів — **`eu-north-1`**.
Для перегляду фічі повторний deployment не потрібен.

## Передумови

- .NET SDK 10, Node.js 24.16.0, Python 3.11, AWS CLI, Terraform >= 1.10, AWS Syndicate.
- Локальний AWS-профіль, Terraform variables/backend config і Syndicate config з [AWS_PROJECT_SETUP.md](AWS_PROJECT_SETUP.md).
- Наявний S3 state bucket із versioning, encryption та блокуванням public access.
  Для нового середовища його створюють окремо до `terraform init`.
- Для підтримки існуючого середовища збережіть його state і назви ресурсів.

Команди нижче виконуються вручну власником середовища. `apply`, `deploy`, `update`,
S3 sync та invalidation змінюють AWS-ресурси; GitHub Actions їх автоматично не виконує.

## 1. Terraform

З кореня репозиторію, після налаштування AWS-профілю:

```powershell
cd infra/terraform
terraform init -reconfigure -backend-config=marathon.backend.hcl
terraform fmt -check -recursive
terraform validate
terraform plan -out=marathon.tfplan
```

Перегляньте plan. Для погоджених змін:

```powershell
terraform apply marathon.tfplan
terraform output
```

Plan і state не комітяться. Budget відстежує витрати, але не є жорстким лімітом.

## 2. Backend / AWS Syndicate

З кореня репозиторію, з налаштованими локальними Syndicate-файлами:

```powershell
cd backend/restaurant-backend-app
python -m venv syndicate_venv
.\syndicate_venv\Scripts\Activate.ps1
pip install aws-syndicate
$env:SDCT_CONF = (Resolve-Path ".\.syndicate-config-restaurant-backend-app").Path
syndicate build
```

Syndicate потребує AWS-доступ навіть для build. Для **першого** розгортання спочатку
створіть artifacts bucket командою `syndicate create-deploy-target-bucket`, потім
виконайте build та `syndicate deploy --deploy_name dev`.
Для оновлення вже розгорнутого середовища після build використовуйте `syndicate update`.

## 3. Frontend

З кореня репозиторію:

```powershell
cd frontend/restaurant-frontend-app
$env:BACKEND_URL = "https://kfhwimu435.execute-api.eu-north-1.amazonaws.com/develop"
npm ci
npm run build
```

Для іншого середовища замініть `BACKEND_URL` на його API. Build генерує gitignored
`environment.production.ts`; `postbuild` перевіряє bundle на placeholder.

Для публікації задайте **фактичні** bucket і CloudFront distribution вашого середовища:

```powershell
$FRONTEND_BUCKET = "<YOUR_FRONTEND_BUCKET>"
$DISTRIBUTION_ID = "<YOUR_CLOUDFRONT_DISTRIBUTION_ID>"
aws s3 sync dist/restaurant-frontend-app/browser "s3://$FRONTEND_BUCKET/" --delete
aws cloudfront create-invalidation --distribution-id $DISTRIBUTION_ID --paths "/*"
```

`--delete` прибирає зі вказаного bucket файли, яких немає у build.
CloudFront domain/id також доступні через Terraform outputs
`cloudfront_distribution_domain_name` і `cloudfront_distribution_id`.

## 4. Перевірки

1. Головна сторінка, меню та зображення відкриваються по HTTPS.
2. `/dishes` і `/locations` повертають 200, frontend отримує дані без CORS-помилок.
3. Без входу owner API відхиляє запити, Group Order перенаправляє на login.
4. Пройдіть [DEMO_SCRIPT.md](DEMO_SCRIPT.md), включно з гостьовим кошиком і waiter view.

Повний E2E змінює тестові дані; read-only перевірка доступності його не замінює.

## CI

- Backend: локальні unit-тести у GitHub Actions.
- Frontend: lint, type-check та production build без завантаження до S3.
- Terraform: fmt/validate з `init -backend=false`, без AWS credentials і apply.
- AQA quality: статичні перевірки; AQA live tests — лише ручний запуск.

Поточний стан тестів та відомі проблеми — [PROJECT_AUDIT.md](PROJECT_AUDIT.md).
