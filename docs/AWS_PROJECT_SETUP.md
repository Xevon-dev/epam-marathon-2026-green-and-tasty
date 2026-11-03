# Поточний AWS Projects-проєкт

Оновлено: 1 листопада 2026 року. Це актуальна підготовка для проєкту користувача;
старі команди з `DEPLOYMENT_RUNBOOK.md` для Oregon не виконувати без адаптації.

## Підтверджено зі скриншотів

- Проєкт: **Caffeine Driven Development**.
- Account ID: `387143718645` (ще звірити через STS після входу).
- Регіон: **`eu-north-1` (Stockholm)**.
- Новий AWS experience, Free plan; на скриншоті залишок $100 та 182 дні.
- Новий AWS experience обмежує регіональні ресурси регіоном проєкту.
- Наявність кредитів не означає відсутності їх витрачання.

## Локальна підготовка

- `infra/terraform/marathon.auto.tfvars` задає ID і регіон проєкту (gitignored).
- `infra/terraform/marathon.backend.hcl` перевизначає старі bucket/region S3 backend.
- Локальні `syndicate.yml` і `syndicate_aliases.yml` налаштовані на цей проєкт,
  включно з artifacts і Swagger bucket names (gitignored).
- Для відтворення на іншому комп'ютері створити `marathon.auto.tfvars`:

```hcl
aws_region = "eu-north-1"
account_id = "387143718645"
```

## Коли користувач повернеться: вхід

AWS CLI уже встановлено. Запустити в локальному PowerShell:

```powershell
aws configure set region eu-north-1 --profile marathon
aws login --region eu-north-1 --profile marathon
```

У браузері вибрати **Caffeine Driven Development** і підтвердити вхід.
Назва `marathon` — запропонована назва локального профілю; можна погодити іншу.
Ключі вручну не створювати й не надсилати в чат.

Після успішного входу AWS CLI використовує `marathon`. Terraform AWS provider
не прочитав browser `login_session`, тому для SDK-інструментів налаштувати сумісний
локальний профіль `marathon-tools`:

```powershell
aws configure set credential_process "aws configure export-credentials --profile marathon --format process" --profile marathon-tools
aws configure set region eu-north-1 --profile marathon-tools
$env:AWS_PROFILE = "marathon-tools"
$env:AWS_REGION = "eu-north-1"
$env:AWS_DEFAULT_REGION = "eu-north-1"
aws sts get-caller-identity --profile marathon-tools
```

Перевірити `Account = 387143718645`. Вхід лише налаштовує локальний доступ.
Agent Toolkit/MCP не є обов'язковим для наявного Terraform/Syndicate deployment.
Не копіювати тимчасові токени в репозиторій.

## Подальше розгортання

1. Звірити доступні сервіси, IAM-дозволи та ресурси в поточному проєкті.
   Lambda, API Gateway, DynamoDB, Cognito, S3 та CloudFront є у списку підтримуваних
   сервісів Free plan нового experience; це не доводить доступність усіх операцій.
2. Погодити створення state bucket і можливе витрачання кредитів. Навіть цей bucket
   є AWS-ресурсом; до погодження не запускати команду створення.
3. Коли state bucket `restaurant-terraform-state-387143718645` створено в `eu-north-1`,
   з каталогу `infra/terraform` виконати:

   ```powershell
   terraform init -reconfigure -backend-config=marathon.backend.hcl
   terraform validate
   terraform plan -out=marathon.tfplan
   ```

   До першого init перевірити, що немає старого state з ресурсами іншого проєкту.
   Переглянути plan і оцінити витрати окремо. Apply — після явного підтвердження.
4. Перевірити Syndicate build, потім погоджене розгортання backend у `eu-north-1`.
5. Отримати URL нового API в `eu-north-1`, передати його через `BACKEND_URL` під час
   production build frontend. Старий dev API у `us-west-2` не містить нової фічі.
6. Завантажити frontend у фактичний bucket із Terraform і перевірити CloudFront.
7. Підготувати тестові дані ресторану та пройти `docs/DEMO_SCRIPT.md` end-to-end.

Оновлений статус: вхід і STS успішні. API підтвердив активний Free plan із $140 кредитів (перевірено після deployment; раніше API показував $100).
Для сумісності створено локальний профіль `marathon-tools` із:

```ini
credential_process = aws configure export-credentials --profile marathon --format process
region = eu-north-1
```

Для Terraform і Syndicate використовувати `$env:AWS_PROFILE = "marathon-tools"`.
Terraform provider не зміг читати `marathon` напряму; сумісний profile пройшов STS.
Створено приватний state bucket `restaurant-terraform-state-387143718645` у
`eu-north-1` із versioning, блокуванням публічного доступу та AES256 encryption.
Початковий plan: 36 створень, 0 змін, 0 видалень. Після AWS Support case
`179360120700332` AWS схвалив account verification. Новий plan додав 2 ресурси;
Terraform apply успішно створив CloudFront distribution і frontend bucket policy.
Всі Terraform ресурси створено, підтримка Basic/Free лишилась без змін.
Backend Syndicate розгорнуто: API `https://kfhwimu435.execute-api.eu-north-1.amazonaws.com/develop`;
API перевірений, demo location, 3 tables, 4 dishes і waiter record засіяні.
Production frontend побудований і завантажений у приватний CloudFront origin;
invalidation завершена. HTTPS URL: `https://dd9nzqrfa7inx.cloudfront.net`.
Тимчасовий S3 website fallback може лишатися до рішення прибрати його.
Budget без email не надсилає сповіщення й не є лімітом витрат. Тариф не змінювався.

## Джерела

- https://docs.aws.amazon.com/accounts/latest/reference/connect-ai-coding-tool.html
- https://docs.aws.amazon.com/accounts/latest/reference/project-regions.html
- https://docs.aws.amazon.com/accounts/latest/reference/supported-services-sign-up-new.html
- https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sign-in.html
