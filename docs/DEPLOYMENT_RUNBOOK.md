# Runbook: розгортання Green & Tasty в AWS

> Статус: **не виконано**. AWS-команди не запускалися, ресурси не створювалися.
> Перед `terraform apply` і `syndicate deploy` потрібне явне підтвердження власника акаунта.
> Орієнтовний час: 30–60 хвилин. Орієнтовна вартість демо: кілька центів–кілька доларів
> залежно від часу роботи CloudFront, API Gateway, Lambda і DynamoDB; точну суму дасть
> `terraform plan`/AWS Budget після налаштування credentials.

## 0. Що вже готово

- Terraform: `terraform init -backend=false` і `terraform validate` пройшли; `terraform fmt -check` чистий.
- Backend: 148 unit-тестів проходять; нові маршрути зареєстровані в `RouteRegistry`.
- Frontend: production build проходить; 13 цільових тестів проходять.
- Syndicate: локальний `project_path` виправлено; artifacts bucket приведено до `marathon2026-restaurant-artifacts-747482682250`.
- AWS CLI 2.39.3 і Terraform 1.16.6 встановлені.

## 1. AWS credentials

Потрібен доступ до акаунта, який використовується в конфігурації (`747482682250`), або
заміна цього ID на свій у всіх місцях, перелічених нижче.

**Регіон:** для розгортання використовується `us-west-2` — це типовий регіон Terraform,
`backend.tf`, Syndicate-конфігурації та наявного dev API. Згадка `eu-west-1` у
`frontend/restaurant-frontend-app/CLAUDE.md` є застарілою і не використовується.

### Варіант A: AWS Academy / Learner Lab

1. Відкрий Learner Lab → **AWS Details** → **AWS CLI** → **Show**.
2. Скопіюй `aws_access_key_id`, `aws_secret_access_key`, `aws_session_token`.
3. Додай профіль:

```powershell
aws configure set aws_access_key_id "PASTE_ACCESS_KEY" --profile marathon
aws configure set aws_secret_access_key "PASTE_SECRET_KEY" --profile marathon
aws configure set aws_session_token "PASTE_SESSION_TOKEN" --profile marathon
aws configure set region us-west-2 --profile marathon
$env:AWS_PROFILE = "marathon"
aws sts get-caller-identity
```

Тимчасові credentials Academy діють кілька годин; після завершення lab їх треба оновити.

### Варіант B: власний акаунт

```powershell
aws configure --profile marathon
$env:AWS_PROFILE = "marathon"
aws sts get-caller-identity
```

Перевір, що `Account` збігається з `747482682250`. Якщо ні — заміни ID у:

- `infra/terraform/backend.tf` (bucket state)
- `infra/terraform/variables.tf` (`account_id`)
- `backend/restaurant-backend-app/.syndicate-config-restaurant-backend-app/syndicate.yml`
- `backend/restaurant-backend-app/.syndicate-config-restaurant-backend-app/syndicate_aliases.yml`

## 2. Terraform state bucket

```powershell
$ACCOUNT_ID = aws sts get-caller-identity --query Account --output text
aws s3api head-bucket --bucket "restaurant-terraform-state-$ACCOUNT_ID" 2>$null
if ($LASTEXITCODE -ne 0) {
  aws s3api create-bucket --bucket "restaurant-terraform-state-$ACCOUNT_ID" --region us-west-2 --create-bucket-configuration LocationConstraint=us-west-2
  aws s3api put-bucket-versioning --bucket "restaurant-terraform-state-$ACCOUNT_ID" --versioning-configuration Status=Enabled
  aws s3api put-public-access-block --bucket "restaurant-terraform-state-$ACCOUNT_ID" --public-access-block-configuration "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
}
```

## 3. Terraform plan (без створення ресурсів)

```powershell
cd infra/terraform
terraform init
terraform fmt -check -recursive
terraform validate
terraform plan -out=marathon.tfplan
```

Переглянь plan: CloudFront, S3, Cognito, SNS, CloudWatch, EventBridge, SQS, Budget.
`marathon.tfplan` не комітити (він у `.gitignore`).

## 4. Terraform apply — ЛИШЕ ПІСЛЯ ПІДТВЕРДЖЕННЯ

```powershell
terraform apply marathon.tfplan
terraform output
```

## 5. Backend через Syndicate

```powershell
cd ../../backend/restaurant-backend-app
python -m venv syndicate_venv
.\syndicate_venv\Scripts\Activate.ps1
pip install aws-syndicate
$env:SDCT_CONF = (Resolve-Path ".\.syndicate-config-restaurant-backend-app").Path
$env:AWS_PROFILE = "marathon"
syndicate create-deploy-target-bucket
syndicate build
syndicate deploy --deploy_name dev
```

Якщо `syndicate` не знайдено — перевір, що активовано venv і Python 3.11 доступний як `python`.

## 6. Frontend

```powershell
$API_ID = aws apigateway get-rest-apis --region us-west-2 --query "items[?name=='restaurant-backend-api'].id | [0]" --output text
$env:BACKEND_URL = "https://$API_ID.execute-api.us-west-2.amazonaws.com/develop"
cd ../../frontend/restaurant-frontend-app
npm ci
npm run build
aws s3 sync dist/restaurant-frontend-app/browser "s3://restaurant-frontend-$ACCOUNT_ID/" --delete
cd ../../infra/terraform
terraform output cloudfront_distribution_domain_name
```

Відкрий `https://<cloudfront-domain>` і перевір сценарій з `docs/EPAM_SUBMISSION.md`.

## 7. Що перевірити після деплою

1. Реєстрація/вхід клієнта.
2. Бронювання на 2+ гостей.
3. Картка бронювання → **Group order**.
4. Запрошення гостя та його кошик.
5. Власник бачить учасників, страви, суми, статуси.
6. Власник додає/видаляє свої страви, підтверджує свій кошик.
7. Власник видаляє гостьову сесію.
8. Власник підтверджує групове замовлення; офіціант бачить об’єднаний pre-order.
9. Перевірити, що не-власник отримує `403`, а гість не бачить owner-сторінку.

## 8. Відкат / прибирання

```powershell
# frontend
aws s3 rm "s3://restaurant-frontend-$ACCOUNT_ID/" --recursive
# backend
cd backend/restaurant-backend-app
$env:SDCT_CONF = (Resolve-Path ".\.syndicate-config-restaurant-backend-app").Path
syndicate clean
# terraform
cd ../../infra/terraform
terraform destroy
```

Не запускай `destroy`, поки не переконаєшся, що посилання для здачі більше не потрібні.
